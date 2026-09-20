# Chương 14 — Web API hoàn chỉnh với EF Core

## Mục tiêu học

Sau chương này, bạn sẽ:

- Tích hợp EF Core vào ASP.NET Core: `AddDbContext`, **DbContext theo request**, migration khi khởi động.
- Xây API CRUD có **lọc, sắp xếp, phân trang** an toàn, DTO/projection, xoá mềm.
- Viết nghiệp vụ **đặt hàng** dùng giao dịch và xử lý **xung đột đồng thời** → `409`.
- Kiểm thử API với **SQLite in-memory** bằng `WebApplicationFactory`.

Code mẫu: [`code/ch14-api-voi-efcore/`](../../code/ch14-api-voi-efcore/) — `CuaHangApi` (API) + `CuaHangApi.Tests` (10 test tích hợp).

Chương này ghép các mảnh: Minimal API (Ch. 4), DI (5), cấu hình (6), DTO/validation (8), xử lý lỗi (9), kiểm thử (10), EF Core (12–13).

## Cấu trúc dự án

```
CuaHangApi/
├── Program.cs                     # cấu hình DI, pipeline, migrate + seed
├── Dtos.cs                        # request/response + exception nghiệp vụ
├── Data/
│   ├── Entities.cs                # Nhom, SanPham, KhachHang, DonHang, ChiTietDon
│   └── CuaHangDbContext.cs        # cấu hình mô hình (Fluent API)
├── Endpoints/
│   ├── SanPhamEndpoints.cs        # /api/san-pham, /api/nhom
│   ├── DonHangEndpoints.cs        # /api/don-hang
│   └── KiemTraHopLeFilter.cs      # endpoint filter validation dùng chung
├── Services/DonHangService.cs     # nghiệp vụ đặt hàng
└── Migrations/                    # sinh từ `dotnet ef migrations add KhoiTao`
```

Nguyên tắc tổ chức: **endpoint mỏng** (HTTP), **nghiệp vụ phức tạp trong service** (`DonHangService`), **truy vấn đơn giản có thể để trong endpoint** (không cần bọc `Repository` thừa mỏng lên `DbContext` — Tập 3 bàn khi nào cần).

## Đăng ký `DbContext`

```csharp
builder.Services.AddDbContext<CuaHangDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("CuaHang") ?? "Data Source=cuahang.db"));
```

- Vòng đời **Scoped**: **một `DbContext` cho mỗi HTTP request**, hết request thì được dispose — đúng với mẫu **unit of work** (Chương 5).
- Chuỗi kết nối lấy từ cấu hình (`ConnectionStrings:CuaHang`), không viết cứng; bí mật để ngoài Git (Chương 6).
- `DbContext` nhận `DbContextOptions<T>` qua constructor để **DI và test thay CSDL** dễ dàng:

```csharp
public class CuaHangDbContext(DbContextOptions<CuaHangDbContext> options) : DbContext(options) { ... }
```

Đổi sang PostgreSQL/SQL Server chỉ thay dòng `UseSqlite` (và package) — code endpoint không đổi (lưu ý kiểu `decimal`, phân biệt hoa/thường, hàm ngày giờ).

`AddDbContextPool` tái sử dụng đối tượng để giảm cấp phát; `IDbContextFactory<T>` dành cho nơi cần tự tạo context (Blazor Server, tác vụ nền).

## Migration và dữ liệu mẫu khi khởi động

```csharp
if (app.Configuration.GetValue("MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();       // DbContext là Scoped → cần tự tạo scope ngoài request
    var db = scope.ServiceProvider.GetRequiredService<CuaHangDbContext>();
    await db.Database.MigrateAsync();
    await DuLieuMau.NapAsync(db);                                   // chỉ nạp khi chưa có dữ liệu
}
```

Tiện cho dev/demo/test. **Production:** tránh tự migrate khi nhiều instance khởi động cùng lúc và khi migration có thể phá dữ liệu — chạy migration ở bước triển khai riêng (`dotnet ef migrations bundle` hoặc script SQL đã duyệt), bảng cờ `MigrateOnStartup=false` để tắt. Khi tạo migration bằng công cụ, truyền cờ: `dotnet ef migrations add KhoiTao -- --MigrateOnStartup false` (để công cụ chạy `Program.cs` mà không mở CSDL).

## API danh sách: lọc, sắp xếp, phân trang

```
GET /api/san-pham?tim=laptop&nhomId=2&sapXep=-gia&trang=1&kichThuoc=10
```

```csharp
IQueryable<SanPham> q = db.SanPhams.AsNoTracking();
if (!string.IsNullOrWhiteSpace(tim))
    q = q.Where(s => EF.Functions.Like(s.Ten, $"%{tim}%") || EF.Functions.Like(s.Ma, $"%{tim}%"));
if (nhomId is not null) q = q.Where(s => s.NhomId == nhomId);

q = sapXep switch                                          // WHITELIST: không bao giờ đưa chuỗi client vào OrderBy
{
    "gia" => q.OrderBy(s => s.Gia),
    "-gia" => q.OrderByDescending(s => s.Gia),
    "ten" => q.OrderBy(s => s.Ten),
    _ => q.OrderBy(s => s.Id),
};

int tongSo = await q.CountAsync(ct);
var muc = await q.Skip((trang - 1) * kichThuoc).Take(kichThuoc)
    .Select(s => new SanPhamDto(s.Id, s.Ma, s.Ten, s.Gia, s.Ton, s.Nhom.Ten))   // projection: JOIN tên nhóm, không N+1
    .ToListAsync(ct);
return TypedResults.Ok(new TrangKetQua<SanPhamDto>(muc, trang, kichThuoc, tongSo));
```

Điểm đáng nhớ:

- **Xây `IQueryable` từng bước**, thêm điều kiện khi tham số có mặt — EF chỉ sinh **một** câu SQL cuối cùng.
- **`Math.Clamp(kichThuoc, 1, 100)`** chặn client đòi cả bảng.
- **Whitelist sắp xếp**: cho client gửi `sapXep=...` và ghép vào SQL/`OrderBy` động là lỗ hổng (SQL injection/lộ dữ liệu).
- **Tìm kiếm không phân biệt hoa/thường:** `Contains()` trên SQLite phân biệt (dịch sang `instr`), còn SQL Server mặc định không — chênh lệch giữa CSDL. `EF.Functions.Like` cho hành vi rõ ràng (đã phát hiện qua test thất bại đầu tiên khi viết chương này!). Với tìm kiếm văn bản thật sự dùng full-text search của CSDL.
- Trả `TrangKetQua<T>` chứa cả `TongSo`, `TongTrang`, `CoTrangSau` để client dựng phân trang.
- Trả `AsNoTracking` + projection: nhanh, nhẹ.

## Tạo, sửa, xoá

```csharp
// POST: 201 + Location, 404 nếu nhóm không có, 409 nếu trùng mã
var sp = new SanPham { Ma = req.Ma!, Ten = req.Ten!.Trim(), Gia = req.Gia, Ton = req.Ton, Nhom = nhom };
db.SanPhams.Add(sp);
try { await db.SaveChangesAsync(ct); }
catch (DbUpdateException) { return TypedResults.Conflict($"Ma san pham {req.Ma} da ton tai"); }   // UNIQUE(Ma)
```

Ở đây **CSDL là người canh gác cuối cùng**: dù ứng dụng có kiểm tra "mã đã tồn tại chưa" trước, hai request đồng thời vẫn có thể cùng vượt qua kiểm tra — chỉ ràng buộc `UNIQUE` chặn được. Bắt `DbUpdateException` và dịch sang `409`.

```csharp
// DELETE: xoá mềm bằng MỘT câu UPDATE, không nạp entity
int n = await db.SanPhams.Where(s => s.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.DaXoa, true), ct);
return n == 0 ? TypedResults.NotFound() : TypedResults.NoContent();

// POST /{id}/nhap-kho: tăng tồn NGUYÊN TỬ, không đọc-rồi-ghi → không mất cập nhật khi nhiều người nhập cùng lúc
await db.SanPhams.Where(s => s.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ton, x => x.Ton + req.SoLuong), ct);
```

Validation đầu vào dùng `KiemTraHopLeFilter<T>` (Chương 8) gắn lên endpoint: `.AddEndpointFilter<KiemTraHopLeFilter<TaoSanPhamRequest>>()`.

## Nghiệp vụ đặt hàng: giao dịch và xung đột

`POST /api/don-hang` với `{ "khachHangId": 1, "dong": [ { "sanPhamId": 3, "soLuong": 2 }, ... ] }`. Yêu cầu: **kiểm tra tồn kho, trừ kho và tạo đơn cùng thành công hoặc cùng thất bại**.

```csharp
var can = req.Dong.GroupBy(d => d.SanPhamId).ToDictionary(g => g.Key, g => g.Sum(d => d.SoLuong));   // gộp dòng trùng
var sanPhams = await db.SanPhams.Where(s => can.Keys.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct); // MỘT truy vấn, không N+1

var don = new DonHang { KhachHang = khach, Ngay = DateOnly.FromDateTime(dongHo.GetLocalNow().DateTime) };
foreach (var (id, soLuong) in can)
{
    var sp = sanPhams[id] ?? throw new KhongTimThayException(...);
    if (sp.Ton < soLuong) throw new KhongDuHangException(sp.Ma, soLuong, sp.Ton);
    sp.Ton -= soLuong;                                                                       // trừ kho (entity được theo dõi)
    don.ChiTiets.Add(new ChiTietDon { SanPham = sp, SoLuong = soLuong, DonGia = sp.Gia });  // chốt giá lúc đặt
}
db.DonHangs.Add(don);
await db.SaveChangesAsync(ct);          // MỘT giao dịch: mọi UPDATE kho + INSERT đơn
```

- Kiểm tra **mọi dòng trước khi ghi**; một dòng thiếu hàng → ném exception → **không có gì được lưu** (test `DatHang_NhieuDong_MotDongThieuHang_KhongDongNaoBiTru` chứng minh dòng A không bị trừ).
- **Chốt `DonGia`** vào chi tiết đơn: giá sản phẩm đổi sau này không làm thay đổi hoá đơn cũ (Chương 11).
- **Concurrency token trên `Ton`**: hai đơn cùng đọc tồn 5 và cùng trừ 4 → lệnh `UPDATE ... WHERE Ton = 5` của đơn thứ hai không khớp dòng nào → `DbUpdateConcurrencyException`, bắt và chuyển thành `XungDotException` → `409 Conflict` ("Ton kho vua thay doi"). Không có nó, tồn kho có thể thành số âm hoặc sai (mất cập nhật). Ràng buộc `CHECK (Ton >= 0)` là lưới an toàn cuối.
- Exception nghiệp vụ (`KhongTimThay` → 404, `KhongDuHang`/`XungDot` → 409, `LoiNghiepVu` khác → 422) được **handler toàn cục** (Chương 9) biến thành ProblemDetails — endpoint không cần `try/catch`.

Bạn cũng có thể để client **tự thử lại** khi nhận `409` (idempotency: nếu mạng lỗi, gửi lại cùng đơn có thể tạo đơn thứ hai — thiết kế khoá idempotency ở Tập 3).

## Lưu ý về xoá mềm và quan hệ

Sản phẩm đã xoá mềm bị `HasQueryFilter` giấu khỏi mọi truy vấn — kể cả khi **hiển thị đơn hàng cũ có chứa nó**. Vì vậy `DonHangService.LayAsync` dùng `.IgnoreQueryFilters()` (Chương 13). Khi tạo migration EF cảnh báo đúng về điều này: đó là **thiết kế cần chủ động xử lý**, không phải lỗi.

## Kiểm thử với SQLite trong bộ nhớ

Test chạy API **thật** với CSDL SQLite **thật** (có ràng buộc, SQL thật), nhưng nằm trong bộ nhớ:

```csharp
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");    // giữ MỞ, nếu không CSDL mất

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _conn.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<CuaHangDbContext>>();
            s.AddDbContext<CuaHangDbContext>(o => o.UseSqlite(_conn));
        });
    }
}
```

Lúc khởi động app chạy `Migrate()` + seed trên CSDL bộ nhớ, nên test có dữ liệu mẫu sẵn. Ưu điểm so với "EF InMemory provider" (Chương 10): các ràng buộc `UNIQUE/CHECK/FK`, giao dịch, SQL thật đều hoạt động → test bắt được lỗi thật (ví dụ 409 khi trùng mã đến từ CSDL). Mười test bao phủ: phân trang/sắp xếp, tìm kiếm, trùng mã (409), mã sai (400), xoá mềm, nhập kho, đặt hàng thành công/thiếu hàng/nhiều dòng/khách không tồn tại.

Nhớ viết test **độc lập thứ tự** (mỗi test tạo mã sản phẩm riêng như `"DH001"`, `"DH002"`) vì factory chia sẻ một CSDL cho cả class.

## Chạy thử

```
cd CuaHangApi
dotnet run
curl "http://localhost:5000/api/san-pham?sapXep=-gia&kichThuoc=3"
curl -X POST http://localhost:5000/api/don-hang -H "Content-Type: application/json" \
     -d '{"khachHangId":1,"dong":[{"sanPhamId":1,"soLuong":2}]}'
dotnet test ../CuaHangApi.Tests
```

## Lỗi thường gặp

- `DbContext` dùng trong Singleton/`BackgroundService` mà không tạo scope (Chương 5).
- Trả entity (có navigation vòng) thay vì DTO → lỗi serialize/lộ dữ liệu.
- Quên `await` → request trả về trước khi `SaveChanges` xong.
- Tin kiểm tra "trùng mã" ở tầng ứng dụng mà quên ràng buộc UNIQUE.
- Đọc-rồi-ghi `Ton` không có concurrency token → âm kho / mất cập nhật.
- Migrate khi khởi động trên production với nhiều instance.
- Test dùng EF InMemory rồi "xanh giả".
- `Skip/Take` không `OrderBy`; không giới hạn `kichThuoc`.
- Đưa `sapXep` từ client thẳng vào `OrderBy` động.
- Quên `IgnoreQueryFilters` ở truy vấn lịch sử/đơn hàng cũ.

## Bài tập

1. Thêm `PUT /api/khach-hang/{id}` và `POST /api/khach-hang` (email duy nhất → `409`), kèm test.
2. Thêm `GET /api/don-hang?khachHangId=1&trang=1` trả `TrangKetQua<DonHangDto>` bằng **một** câu SQL (đo bằng `LogTo`).
3. Thêm endpoint `POST /api/don-hang/{id}/huy` hoàn lại tồn kho trong cùng một giao dịch; viết test.
4. Đổi provider sang PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`) bằng biến cấu hình và liệt kê những chỗ code/migration phải sửa (gợi ý: `decimal`, `Like`).
5. Viết test kiểm chứng xung đột đồng thời: hai request đặt hàng song song (`Task.WhenAll`) cho sản phẩm còn 1 cái; chỉ một thành công, một nhận `409`.
