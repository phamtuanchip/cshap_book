# Chương 13 — Idempotency, audit log và bảo vệ dữ liệu

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **idempotency** (tính lặp an toàn) và vì sao mạng không đáng tin buộc API ghi phải chịu được **gửi lặp**.
- Cài **`Idempotency-Key`** đúng cách: chiếm khoá bằng **một lệnh INSERT nguyên tử**, lưu và **phát lại** kết quả, xử lý đồng thời và nội dung khác.
- Ghi **nhật ký kiểm toán (audit log)**: ai, làm gì, lúc nào, giá trị trước/sau — **cùng giao dịch** với thay đổi nghiệp vụ.
- Áp dụng nguyên tắc **bảo vệ dữ liệu**: dữ liệu cá nhân (PII), mã hoá, quản lý bí mật, giữ/xoá dữ liệu.

Code: [`code/kho-clean/`](../../code/kho-clean/) — `Kho.Infrastructure/Idempotency/`, `Kho.Web/IdempotencyMiddleware.cs`, bảng `nhat_ky_kiem_toan` và lớp `IdempotencyAuditTests` (5 test mới; toàn solution **45 test đều đạt**).

## Vấn đề: "đã trừ kho chưa?"

Khách bấm "Xuất kho 3 cái". Request tới server, server trừ tồn, nhưng **phản hồi bị mất** (rớt mạng, timeout, proxy). Client không biết thành công hay không, và thử lại. Nếu API không được thiết kế cho việc này, tồn bị trừ **hai lần**. Retry còn đến từ tầng hạ tầng (Polly ở Tập 2 Chương 19, message broker giao "ít nhất một lần"), người dùng bấm đúp, trình duyệt gửi lại.

**Idempotent** nghĩa là: thực hiện một thao tác **nhiều lần cho cùng kết quả như một lần**.

| Phương thức HTTP | Idempotent theo đặc tả? | Ghi chú |
|------------------|-------------------------|---------|
| `GET`, `HEAD`, `OPTIONS` | có (an toàn) | chỉ đọc |
| `PUT`, `DELETE` | có | "đặt thành X", "xoá X" — lặp lại vẫn ra cùng trạng thái |
| **`POST`** | **không** | "tạo mới", "xuất 3 cái" — mỗi lần một hiệu ứng |
| `PATCH` | tuỳ | tăng tương đối (`+3`) không idempotent |

Thiết kế ưu tiên nhất là làm thao tác **idempotent tự nhiên**: `PUT /san-pham/LT001/ton {"ton": 7}` (đặt tuyệt đối) thay vì `POST .../xuat {"soLuong": 3}` (tương đối). Nhưng nhiều nghiệp vụ (thanh toán, xuất kho, đặt hàng) vốn là "tương đối" — với chúng, dùng **khoá idempotency**.

## `Idempotency-Key`

Quy ước (Stripe phổ biến, có bản nháp IETF): client sinh một **khoá duy nhất cho mỗi *ý định*** (thường GUID) và gửi ở header. Server nhớ khoá → kết quả:

```http
POST /api/san-pham/LT001/xuat
Idempotency-Key: 7d2c1a9e-7b0e-4e0e-9a51-2b0f6c1d0a11
Content-Type: application/json

{"soLuong": 3}
```

- Lần đầu: xử lý, lưu kết quả, trả `204`.
- Gửi lại **cùng khoá, cùng nội dung**: **không xử lý lại**, trả **y hệt** kết quả cũ (kèm `Idempotency-Replayed: true`).
- **Cùng khoá, nội dung khác**: lỗi lập trình phía client → `422`.
- Cùng khoá, bản đầu **đang chạy**: `409` (client thử lại sau).

Client sinh khoá **một lần cho một hành động** rồi dùng lại nó cho mọi lần thử lại của hành động đó; khoá mới cho hành động mới.

## Cài đặt: để CSDL phân xử

Sai lầm phổ biến: "đọc xem khoá có chưa; chưa có thì xử lý rồi ghi" — hai request đến cùng lúc **cùng thấy "chưa có"** và cùng xử lý (race condition, giống Tập 2 Chương 22). Cách đúng: **chiếm khoá bằng một lệnh nguyên tử** và để **ràng buộc duy nhất của CSDL** chọn người thắng.

```csharp
// bảng idempotency: khoá chính = Khoa
int them = await db.Database.ExecuteSqlInterpolatedAsync(
    $"INSERT OR IGNORE INTO idempotency (Khoa, DauVan, MaTrangThai, TaoLucMs) VALUES ({khoa}, {dauVan}, 0, {nowMs})", ct);
if (them == 1) return ChiemDuoc;                         // mình là người đầu tiên
// ngược lại: đã có -> đọc bản ghi để quyết định
var cu = await db.Idempotency.AsNoTracking().Where(x => x.Khoa == khoa).Select(...).FirstAsync(ct);
if (cu.DauVan != dauVan)   return KhacNoiDung;           // cùng khoá, yêu cầu khác
if (cu.MaTrangThai == 0)   return DangXuLy;              // bản kia chưa xong
return DaXong(cu.MaTrangThai, cu.NoiDung);               // phát lại
```

(`INSERT OR IGNORE` là cú pháp SQLite; PostgreSQL dùng `INSERT ... ON CONFLICT DO NOTHING`, SQL Server dùng bắt lỗi vi phạm khoá duy nhất. Nguyên tắc y hệt.)

**Dấu vân tay `DauVan`** = SHA-256 của `phương thức + đường dẫn + nội dung` để phát hiện "cùng khoá, khác nội dung".

Middleware bao quanh endpoint:

```csharp
var kq = await store.ChiemAsync(khoa, dauVan, ct);
switch (kq.TrangThai) { /* KhacNoiDung -> 422; DangXuLy -> 409; DaXongTraLai -> ghi lại status + body cũ */ }

// Chiếm được -> xử lý thật, "bắt" response vào bộ đệm để lưu lại
ctx.Response.Body = dem;
await next(ctx);
if (ctx.Response.StatusCode >= 500) await store.NhaAsync(khoa, ...);   // lỗi tạm thời: thả khoá để client thử lại được
else await store.LuuKetQuaAsync(khoa, ctx.Response.StatusCode, body, contentType, ...);
```

Điểm tinh tế cần nhớ:

- **Lỗi 5xx** không được "đóng đinh" kết quả: thả khoá (xoá) để lần thử lại được xử lý. Lỗi nghiệp vụ 4xx (ví dụ hết hàng 422) *có thể* lưu — cùng khoá cho cùng đáp án.
- Khoá có **hạn** (ví dụ 24–48 giờ) và job dọn dẹp bảng — không để bảng phình vô hạn.
- **Phạm vi khoá theo người dùng/tenant** (ghép `sub` vào khoá) để người này không "va" khoá người kia hoặc dò kết quả người khác.
- Thực sự an toàn khi tác dụng phụ và bản ghi khoá **cùng một giao dịch**. Ví dụ này lưu kết quả sau khi handler commit; nếu process chết giữa hai bước, khoá vẫn ở trạng thái "đang xử lý" (`MaTrangThai = 0`) dù việc đã xong — hệ thống thanh toán nghiêm túc đặt bản ghi khoá vào **cùng transaction** với nghiệp vụ hoặc có bước đối soát/hết hạn khoá đang chạy dở. Hãy nêu rõ giới hạn này khi chọn thiết kế.
- Trên cụm nhiều instance phải dùng **CSDL chung** (như trên) — bộ nhớ cục bộ không đủ.

### Kết quả kiểm thử (đã chạy)

| Test | Điều được chứng minh |
|------|----------------------|
| `GuiLaiCungKhoa_ChiXuLyMotLan...` | gửi hai lần cùng khoá → tồn chỉ trừ **một lần** (10 → 7), lần hai có `Idempotency-Replayed` |
| `KhongCoKhoa_MoiLanDeuXuLy` | không header → hành vi cũ (10 → 4) |
| `CungKhoaNhungNoiDungKhac_422` | cùng khoá, số lượng khác → 422 |
| `HaiRequestCungKhoaDongThoi...` | **8 request song song cùng khoá** → chỉ một xử lý, tồn 10 → 8 |

## Consumer idempotent (thông điệp "ít nhất một lần")

Outbox (Chương 8) và broker giao thông điệp **ít nhất một lần** — nghĩa là **có thể trùng**. Bên nhận phải xử lý lặp an toàn: lưu `MessageId` đã xử lý (bảng *inbox*) trong **cùng giao dịch** với tác dụng phụ, hoặc thiết kế thao tác tự nhiên idempotent (upsert theo khoá). Cùng nguyên lý với `Idempotency-Key`: **khoá duy nhất + ràng buộc CSDL**.

## Nhật ký kiểm toán (audit log)

**Audit log** trả lời "ai đã làm gì, khi nào, trước/sau ra sao" — phục vụ điều tra sự cố, tuân thủ (tài chính, y tế), giải quyết tranh chấp. Khác **log ứng dụng** (chẩn đoán, có thể xoay vòng/xoá): audit là **bản ghi nghiệp vụ, chỉ thêm, giữ lâu**.

Ở `kho-clean`, `KhoDbContext.LuuAsync` ghi nhật ký **trong cùng một giao dịch** với thay đổi nghiệp vụ:

```csharp
foreach (var e in ChangeTracker.Entries<SanPham>().Where(e => e.State is EntityState.Added or EntityState.Modified))
{
    var doi = e.State == EntityState.Modified ? e.Properties.Where(p => p.IsModified).ToList() : [];
    NhatKy.Add(new NhatKyKiemToan
    {
        Luc = luc, Nguoi = NguoiThucHien,
        HanhDong = e.State == EntityState.Added ? "Them" : "Sua",
        DoiTuong = $"SanPham:{e.Entity.Ma.GiaTri}",
        Truoc = doi.Count == 0 ? null : JsonSerializer.Serialize(doi.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue?.ToString())),
        Sau   = doi.Count == 0 ? null : JsonSerializer.Serialize(doi.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString())),
    });
}
```

Vì cùng giao dịch: **nghiệp vụ đổi thành công ⇔ có dòng nhật ký** — không bao giờ lệch. Test `NhatKy_GhiAiLamGi_KemGiaTriTruocSau` xác nhận: an tạo sản phẩm, bình xuất 4 → nhật ký có `Nguoi=binh`, `Truoc={"TonKho":"10"}`, `Sau={"TonKho":"6"}`.

"Ai" đến từ lớp ngoài (`NguoiThucHien` do web đặt từ `User`/`X-Nguoi`; thực tế lấy từ claim `sub` của JWT — Chương 12), **không** để Domain biết HTTP.

Nguyên tắc thiết kế audit:

- **Chỉ thêm**, quyền ghi bảng bị hạn chế; không cho ứng dụng `UPDATE/DELETE` (dùng người dùng CSDL khác hoặc bảng *append-only*/immutable storage khi yêu cầu pháp lý).
- Ghi **cả hành động bị từ chối** với thao tác nhạy cảm (đăng nhập thất bại, truy cập trái phép).
- **Đừng ghi bí mật** (mật khẩu, số thẻ) vào giá trị trước/sau; che/loại các cột nhạy cảm.
- Kèm `TraceId` (Chương 11) để nối audit với trace.
- Với hệ thống lớn: **event sourcing** hoặc **temporal tables** (SQL Server) / **trigger** là phương án khác; hoặc phát sự kiện miền qua outbox sang kho audit riêng.
- Tách **audit (nghiệp vụ)** khỏi **log kỹ thuật**; đặt chính sách lưu giữ riêng.

## Bảo vệ dữ liệu

### Phân loại và giảm thiểu

- Xác định dữ liệu **cá nhân/nhạy cảm (PII)**: tên, email, số điện thoại, địa chỉ, ID cá nhân, dữ liệu tài chính/sức khoẻ.
- **Thu thập tối thiểu**: không giữ những gì không cần. Dữ liệu không có thì không thể rò rỉ.
- Tuân thủ quy định áp dụng (Việt Nam: **Nghị định 13/2023/NĐ-CP** về bảo vệ dữ liệu cá nhân; nếu phục vụ người dùng EU: GDPR): cần **mục đích, sự đồng ý, quyền truy cập/xoá**, thông báo khi rò rỉ. Hãy tham vấn pháp chế — sách này không phải tư vấn pháp lý.

### Mã hoá

- **Đang truyền**: HTTPS/TLS mọi nơi (kể cả giữa dịch vụ nội bộ và tới CSDL).
- **Khi lưu**: mã hoá đĩa/CSDL do nhà cung cấp; với cột rất nhạy cảm dùng **mã hoá cấp ứng dụng** (ASP.NET Core **Data Protection** `IDataProtector.Protect/Unprotect`, hoặc AES-GCM với khoá lưu ở **Key Vault**). Khoá **tách khỏi** dữ liệu và **xoay vòng** được.
- **Mật khẩu**: không bao giờ mã hoá hai chiều — **băm chậm có muối** (`PasswordHasher<T>` của Identity dùng PBKDF2; hoặc Argon2/bcrypt) — Tập 2, Chương 20.
- Đừng tự phát minh thuật toán; dùng API chuẩn (`System.Security.Cryptography`).

### Bí mật (secrets)

- **Không bao giờ** commit vào Git: chuỗi kết nối, khoá API, `client_secret`.
- Phát triển: **`dotnet user-secrets`**. Triển khai: **biến môi trường**/**Key Vault** (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)/secret của Kubernetes; dùng **managed identity** để loại bỏ secret khi có thể.
- Quét secret tự động trong CI (ví dụ `gitleaks`, GitHub secret scanning); xoay vòng ngay khi nghi ngờ lộ.

### Log và dữ liệu cá nhân

Không log toàn bộ request/response body, token, mật khẩu, số thẻ. Che (`****1234`) hoặc băm. Phần nhật ký kiểm toán cũng thuộc dữ liệu cần bảo vệ và có thời hạn lưu.

### Lưu giữ và xoá

Đặt **thời hạn lưu** cho từng loại dữ liệu và job xoá/ẩn danh hoá. Nếu người dùng yêu cầu xoá: xoá (hoặc ẩn danh) dữ liệu cá nhân nhưng giữ được bản ghi kiểm toán/tài chính bắt buộc theo luật (tách định danh khỏi giao dịch: dùng mã giả danh).

## Lỗi thường gặp

- "Kiểm tra rồi ghi" khoá idempotency thay vì **chiếm nguyên tử** → hai request cùng thắng.
- Lưu kết quả lỗi 5xx bằng khoá → client không thể thử lại thành công.
- Không kiểm tra **nội dung** kèm khoá → dùng lại khoá cho yêu cầu khác trả nhầm kết quả.
- Idempotency chỉ ở bộ nhớ tiến trình (không chạy được khi có nhiều instance/restart).
- Audit ghi **ngoài** giao dịch nghiệp vụ → lệch khi một bên lỗi.
- Cho ứng dụng quyền sửa/xoá audit; ghi bí mật vào audit/log.
- Mã hoá mà khoá nằm cạnh dữ liệu (cùng repo/cùng bảng).
- Log token/PII; commit `appsettings` chứa mật khẩu.

## Bài tập

1. Thêm `Idempotency-Key` cho endpoint `POST /api/san-pham` (tạo mới) và viết test: hai request cùng khoá tạo đúng một sản phẩm (và không dựa vào ràng buộc trùng mã).
2. Thêm hạn 24 giờ cho khoá và một `BackgroundService` dọn các bản ghi `TaoLucMs` quá hạn; viết test dùng `FakeTimeProvider`.
3. Ghi thêm `TraceId` vào `NhatKyKiemToan` (lấy từ `Activity.Current`).
4. Mở rộng audit cho hành động **bị từ chối** (xuất thiếu hàng 422) — ghi ở đâu là hợp lý (behavior trong pipeline)? Cài thử.
5. Viết `ProtectedString` value converter dùng `IDataProtector` để mã hoá cột `Ghi chú` của sản phẩm; kiểm tra dữ liệu trong SQLite là chuỗi mã hoá.
6. Cấu hình `dotnet user-secrets` cho chuỗi kết nối và đọc ở `Program.cs`; giải thích vì sao không dùng `appsettings.json` cho bí mật.
