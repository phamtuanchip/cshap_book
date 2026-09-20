# Chương 13 — Quan hệ, truy vấn nâng cao và hiệu năng

## Mục tiêu học

Sau chương này, bạn sẽ:

- Mô hình hoá quan hệ **1–nhiều, nhiều–nhiều, kiểu sở hữu (owned)** trong EF Core.
- Chọn cách tải dữ liệu liên quan: `Include`, `AsSplitQuery`, **projection**, explicit loading — và **nhận ra/sửa N+1**.
- Dùng **query filter** (xoá mềm), **concurrency token** (xung đột đồng thời), SQL thuần, **phân trang** (offset và keyset).

Code mẫu: [`code/ch13-efcore-quan-he/`](../../code/ch13-efcore-quan-he/) — mỗi mục in ra **số câu SQL thực sự chạy** để bạn thấy khác biệt bằng số liệu.

## Quan hệ trong EF Core

### 1–nhiều

```csharp
public class Nhom     { public int Id { get; set; } public List<SanPham> SanPhams { get; set; } = []; }   // "một" (principal)
public class SanPham  { public int NhomId { get; set; } public Nhom Nhom { get; set; } = null!; }          // "nhiều" (dependent)
```

Cặp **khoá ngoại** (`NhomId`) + **navigation** (`Nhom`, `SanPhams`) được EF tự nhận theo quy ước; cấu hình rõ bằng Fluent API:

```csharp
e.HasOne(s => s.Nhom).WithMany(n => n.SanPhams).HasForeignKey(s => s.NhomId);
```

Thêm bằng navigation: `new SanPham { Nhom = phuKien }` — EF tự điền khoá ngoại khi `SaveChanges`.

### Nhiều–nhiều

Sản phẩm có nhiều thẻ, thẻ gắn nhiều sản phẩm. Từ EF Core 5 **không cần** class cho bảng nối:

```csharp
public class SanPham { public List<The> The { get; set; } = []; }
public class The     { public List<SanPham> SanPhams { get; set; } = []; }
e.HasMany(s => s.The).WithMany(t => t.SanPhams);        // EF tự tạo bảng nối SanPhamThe
```

Khi bảng nối **có dữ liệu riêng** (số lượng, đơn giá của `ChiTietDon`), hãy tạo entity nối tường minh với **khoá chính kết hợp**:

```csharp
e.HasKey(c => new { c.DonHangId, c.SanPhamId });
```

### Kiểu sở hữu (owned type) — value object

Một khái niệm không có danh tính riêng (địa chỉ) nhưng muốn đóng gói thành class:

```csharp
public class DiaChi { public string Duong { get; set; } = ""; public string ThanhPho { get; set; } = ""; }
public class KhachHang { public DiaChi? DiaChi { get; set; } }
mb.Entity<KhachHang>().OwnsOne(k => k.DiaChi);           // cột DiaChi_Duong, DiaChi_ThanhPho nằm CHUNG bảng khach_hang
```

Truy vấn tự nhiên: `db.KhachHangs.Count(k => k.DiaChi!.ThanhPho == "Ha Noi")`. Đây là cách ánh xạ **value object** của thiết kế hướng miền (Tập 3).

## Tải dữ liệu liên quan

Mặc định EF **không** nạp navigation — `don.ChiTiets` rỗng nếu bạn không yêu cầu. Bốn cách, đánh đổi khác nhau. Chạy code mẫu với 20 đơn hàng, mỗi đơn 2 dòng:

| Cách | Số câu SQL | Ghi chú |
|------|-----------|---------|
| Explicit loading trong vòng lặp | **21** | ✘ **N+1** |
| `Include` + `ThenInclude` | 1 | JOIN một lần |
| `Include` + `AsSplitQuery` | 2 | tách thành câu đơn giản |
| **Projection** (`Select`) | 1 | chỉ lấy cột cần, tính trong SQL |

### N+1 — vấn đề hiệu năng kinh điển

```csharp
var ds = await db.DonHangs.ToListAsync();                          // 1 câu: lấy 20 đơn
foreach (var d in ds)
    await db.Entry(d).Collection(x => x.ChiTiets).LoadAsync();      // + 20 câu: mỗi đơn một câu → tổng 21
```

"N+1": **1** câu lấy danh sách + **N** câu cho từng phần tử. Với 10.000 đơn là 10.001 lượt gọi CSDL — mỗi lượt tốn độ trễ mạng — ứng dụng chậm dần theo dữ liệu. Nó cũng xảy ra **ẩn** khi bạn truy cập `d.KhachHang.Ten` trong `foreach` với **lazy loading** (nên tắt/không dùng), hoặc khi gọi truy vấn trong vòng lặp.

**Cách phát hiện:** đếm câu SQL (như mẫu, hoặc log `Microsoft.EntityFrameworkCore.Database.Command`), MiniProfiler, Application Insights. Quy tắc: *số câu SQL của một request phải là hằng số nhỏ, không tăng theo số dòng.*

### `Include` — nạp kèm bằng JOIN

```csharp
var ds = await db.DonHangs.Include(d => d.ChiTiets).ThenInclude(c => c.SanPham).ToListAsync();   // 1 câu
```

Đơn giản nhưng nạp **toàn bộ cột** của mọi entity liên quan, và các collection lồng nhau tạo **cartesian explosion**: đơn hàng × dòng × thẻ… thành hàng trăm nghìn dòng trùng lặp. Khi có ≥ 2 collection cùng `Include`, cân nhắc `.AsSplitQuery()` — EF chạy **một câu cho mỗi collection** (ít dữ liệu truyền hơn, đổi lại nhiều lượt gọi hơn và có thể không nhất quán tuyệt đối nếu dữ liệu đổi giữa các câu).

### Projection — cách tốt nhất khi chỉ đọc

```csharp
var tong = await db.DonHangs
    .Select(d => new
    {
        d.Id,
        KhachHang = d.KhachHang.Ten,
        SoDong = d.ChiTiets.Count,
        TongTien = d.ChiTiets.Sum(c => c.SoLuong * c.DonGia),
    })
    .OrderByDescending(x => x.TongTien).Take(3).ToListAsync();          // 1 câu, EF tự JOIN + SUM trong SQL
```

Chỉ những cột bạn `Select` được lấy, kết quả không bị theo dõi (không tốn bộ nhớ tracking), phép tính chạy ở CSDL. **Quy tắc:** truy vấn cho **API/báo cáo (đọc)** → dùng projection sang DTO. Truy vấn để **sửa** entity → tải entity (kèm `Include` cần thiết).

### Explicit loading

`db.Entry(d).Collection(x => x.ChiTiets).LoadAsync()` — nạp có chủ đích cho **một** entity đã có. Hợp khi cần nạp tuỳ điều kiện, không hợp trong vòng lặp.

## Nhiều–nhiều, truy vấn qua quan hệ

```csharp
var coTheMoi = await db.SanPhams.Where(s => s.The.Any(t => t.Ten == "moi")).Select(s => s.Ma).ToListAsync();
```

EF dịch `Any` trên collection thành `EXISTS` trong SQL — viết theo cách nghĩ đối tượng, không cần tự viết JOIN bảng nối.

## Query filter — xoá mềm (soft delete)

Nhiều hệ thống không xoá thật mà đánh dấu `DaXoa = true` (giữ lịch sử, hoá đơn cũ vẫn tham chiếu được). **Global query filter** tự thêm điều kiện vào **mọi** truy vấn:

```csharp
e.HasQueryFilter(s => !s.DaXoa);

await db.SanPhams.CountAsync();                          // 3 (ẩn sản phẩm đã xoá mềm)
await db.SanPhams.IgnoreQueryFilters().CountAsync();     // 4 (bỏ qua filter khi cần, ví dụ trang quản trị)
```

Dùng nhiều cho **đa tenant** (`TenantId == hiện tại`). Cảnh báo quan trọng: filter trên entity là đầu **bắt buộc** của một quan hệ khiến các dòng phụ thuộc "biến mất" (EF cảnh báo khi tạo model). Ví dụ `ChiTietDon → SanPham` bị lọc làm đơn hàng cũ mất dòng — xử lý bằng `IgnoreQueryFilters()` ở truy vấn đơn hàng (như Chương 14) hoặc cấu hình navigation tuỳ chọn.

## Xung đột đồng thời (optimistic concurrency)

Hai người cùng mở sản phẩm có tồn kho 30, mỗi người bán vài cái rồi lưu — nếu không phòng ngừa, người lưu sau **ghi đè** thay đổi của người trước ("lost update"). **Optimistic concurrency**: không khoá dòng khi đọc (lạc quan là hiếm khi xung đột), nhưng khi ghi kiểm tra "dòng còn nguyên như lúc tôi đọc không?":

```csharp
e.Property(s => s.PhienBan).IsConcurrencyToken();      // hoặc [ConcurrencyCheck] / [Timestamp] (SQL Server: rowversion)
```

EF sinh: `UPDATE ... SET Ton=@moi, PhienBan=@pb WHERE Id=@id AND PhienBan=@pbCu`. Nếu không cập nhật được dòng nào (vì người khác đã đổi), ném **`DbUpdateConcurrencyException`**:

```csharp
try { await b.SaveChangesAsync(); }
catch (DbUpdateConcurrencyException) { /* tải lại dữ liệu mới, hoặc báo 409 để client thử lại */ }
```

Mục 5 của mẫu mô phỏng hai `DbContext`: người A lưu được, người B nhận `DbUpdateConcurrencyException`. Ở API trả `409 Conflict`. Trên SQL Server dùng cột `rowversion` (`[Timestamp] byte[]`) để DB tự đổi phiên bản mỗi lần sửa; ở PostgreSQL dùng `xmin`.

## SQL thuần khi cần

```csharp
decimal giaToiThieu = 200_000;
var ds = await db.SanPhams
    .FromSqlInterpolated($"SELECT * FROM SanPhams WHERE Gia >= {giaToiThieu} ORDER BY Gia")   // {..} thành THAM SỐ
    .ToListAsync();
```

`FromSqlInterpolated` **tham số hoá** các giá trị chèn qua `{}` — an toàn. **Không** dùng `FromSqlRaw` với chuỗi nối. Kết quả có thể ghép tiếp LINQ. Với truy vấn trả kiểu không phải entity: `db.Database.SqlQuery<T>(...)`. Câu SQL phải trả **đủ cột** của entity.

## Phân trang

```csharp
// Offset: Skip/Take (đơn giản; trang càng sâu càng chậm vì CSDL phải đếm bỏ qua)
var trang3 = await db.DonHangs.OrderBy(d => d.Id).Skip(2 * kichThuoc).Take(kichThuoc).ToListAsync();

// Keyset / cursor: "sau khoá cuối cùng đã xem" (nhanh, ổn định ở mọi trang)
var tiep = await db.DonHangs.Where(d => d.Id > idCuoi).OrderBy(d => d.Id).Take(kichThuoc).ToListAsync();
```

- **Luôn `OrderBy`** trước `Skip/Take` (không có thứ tự, kết quả không xác định).
- **Offset** cho phép nhảy tới trang bất kỳ, hiển thị "trang 3/12" (cần `Count`); nhược điểm chậm ở trang sâu và dữ liệu trùng/thiếu nếu có thêm/xoá giữa các lần gọi.
- **Keyset** hợp cho cuộn vô hạn/nguồn cấp dữ liệu lớn; chỉ đi tuần tự "trang kế".
- **Luôn giới hạn** `kichThuoc` phía server (`Math.Clamp`) — đừng để client xin "1 triệu dòng".

## Danh sách kiểm tra hiệu năng truy vấn

1. Số câu SQL mỗi request có phải hằng số nhỏ? (không N+1)
2. Truy vấn đọc dùng **projection**/`AsNoTracking`?
3. Có **chỉ mục** cho cột lọc/sắp xếp/khoá ngoại? (Chương 11 — đọc `EXPLAIN`)
4. Có **phân trang** và giới hạn kích thước?
5. Không kéo cả bảng về rồi lọc trong bộ nhớ (`ToList()` trước `Where`)?
6. Không gọi CSDL trong vòng lặp?
7. `Include` có kéo thừa quá nhiều? Xem xét split query hoặc projection.
8. Đo bằng số liệu (log, profiler), đừng đoán.

## Lỗi thường gặp

- N+1 do truy cập navigation/gọi truy vấn trong vòng lặp.
- `NullReferenceException` vì navigation chưa nạp (`don.ChiTiets` rỗng, `d.KhachHang` null) — quên `Include`.
- `Include` mọi thứ "cho chắc" rồi ngạc nhiên vì chậm/tốn bộ nhớ.
- Vòng tham chiếu khi serialize entity ra JSON (`Nhom → SanPham → Nhom`) — đừng trả entity, dùng DTO (Chương 8).
- Quên `OrderBy` khi phân trang.
- Dùng `FromSqlRaw` nối chuỗi (SQL injection).
- Quên concurrency token cho dữ liệu như số dư/tồn kho cập nhật đồng thời.
- Query filter làm dữ liệu liên quan "biến mất" ngoài ý muốn.

## Bài tập

1. Viết truy vấn projection trả về mỗi khách hàng cùng số đơn và tổng chi tiêu (một câu SQL); kiểm chứng bằng bộ đếm.
2. Cố tình tạo N+1 bằng lazy loading (`UseLazyLoadingProxies`) rồi sửa bằng `Include` và bằng projection.
3. Thêm thẻ vào sản phẩm bằng cách gán `sp.The.Add(the)` và kiểm tra bảng nối trong SQLite.
4. Viết phân trang keyset theo cặp `(Ngay, Id)` để ổn định khi nhiều đơn cùng ngày.
5. Tạo hai `DbContext` xung đột nhau theo mẫu mục 5, bắt `DbUpdateConcurrencyException` và viết vòng **thử lại** (tối đa 3 lần) tải lại giá trị mới rồi áp dụng phép trừ.
