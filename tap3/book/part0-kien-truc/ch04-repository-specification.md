# Chương 4 — Repository, Unit of Work và Specification

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **Repository** và **Unit of Work** giải quyết vấn đề gì, và vì sao `DbContext` *đã là* cả hai.
- Quyết định **khi nào** cần tự viết repository (và khi nào là "lớp bọc thừa").
- Tách **phía ghi (aggregate)** khỏi **phía đọc (read model)**.
- Dùng **Specification** để đóng gói điều kiện truy vấn tái sử dụng.

Code: [`Repositories.cs`](../../code/kho-clean/Kho.Infrastructure/Persistence/Repositories.cs), [`Cong.cs`](../../code/kho-clean/Kho.Application/Abstractions/Cong.cs), [`KhoDbContext.cs`](../../code/kho-clean/Kho.Infrastructure/Persistence/KhoDbContext.cs).

## Repository — "bộ sưu tập aggregate"

**Repository** cho code nghiệp vụ cảm giác **thao tác trên bộ sưu tập đối tượng trong bộ nhớ**, che giấu cách lưu trữ:

```csharp
// Application/Abstractions — CỔNG, ngôn ngữ của nghiệp vụ
public interface ISanPhamRepository
{
    Task<SanPham?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task<bool> TonTaiMaAsync(string ma, CancellationToken ct);
    void Them(SanPham sanPham);
}

// Infrastructure — cài đặt bằng EF Core
public class SanPhamRepository(KhoDbContext db) : ISanPhamRepository
{
    public Task<SanPham?> LayTheoMaAsync(string ma, CancellationToken ct)
        => db.SanPhams.FirstOrDefaultAsync(s => s.Ma == MaSanPham.TuCsdl(ma.Trim().ToUpperInvariant()), ct);
    public void Them(SanPham sanPham) => db.SanPhams.Add(sanPham);
}
```

Nguyên tắc:

- **Một repository cho mỗi aggregate root** (không cho mỗi bảng): `ISanPhamRepository`, không có repository riêng cho "dòng đơn hàng".
- Phương thức mang **ngữ nghĩa nghiệp vụ** (`LayTheoMaAsync`, `TonTaiMaAsync`), không lộ `IQueryable` — để tầng ngoài không viết truy vấn tuỳ ý, và để Application không phụ thuộc EF.
- `Them` là **đồng bộ và chưa ghi CSDL**: chỉ đăng ký thay đổi; việc ghi thuộc Unit of Work.
- Chỉ thao tác trên **aggregate đầy đủ** (tải cả aggregate, lưu cả aggregate).

## Unit of Work

**Unit of Work** theo dõi các thay đổi trong một "cuộc trao đổi nghiệp vụ" và **ghi chúng một lần trong một giao dịch**.

```csharp
public interface IUnitOfWork { Task<int> LuuAsync(CancellationToken ct); }
```

Trong solution, UoW **do behavior gọi sau khi lệnh thành công** (`UnitOfWorkBehavior`, Chương 6) — handler không tự `Save`. Vì vậy handler đơn giản, và "một lệnh = một lần lưu = một giao dịch" được bảo đảm ở một chỗ.

## `DbContext` đã là Repository + Unit of Work

Đây là điểm tranh luận lâu năm. `DbContext` **là** một Unit of Work (change tracker + `SaveChanges` trong giao dịch) và mỗi `DbSet<T>` **là** một repository. Bọc thêm `IRepository<T>` generic lên trên thường **không thêm giá trị**:

```csharp
// ✘ Generic repository: lớp bọc mỏng, che mất sức mạnh EF (Include, projection, ExecuteUpdate...), lộ IQueryable hoặc phình to
public interface IRepository<T> { Task<T?> GetByIdAsync(int id); Task<IEnumerable<T>> GetAllAsync(); void Add(T e); void Update(T e); void Delete(T e); }
```

Vấn đề của repository generic: (1) không có chỗ cho truy vấn thực tế (lọc, phân trang, projection) → hoặc lộ `IQueryable` (rò rỉ) hoặc thêm hàng chục phương thức; (2) ép mọi entity có cùng bộ thao tác dù aggregate khác nhau; (3) `GetAll` kéo cả bảng; (4) khó nhận biết cái gì xảy ra ở CSDL.

**Vậy khi nào tự viết repository?**

| Tình huống | Khuyến nghị |
|-----------|-------------|
| CRUD đơn giản, ít nghiệp vụ (Tập 2) | dùng thẳng `DbContext` — đừng bọc |
| Clean Architecture / DDD với **Application không được biết EF** | **repository theo aggregate** như solution: cổng ở Application, cài đặt ở Infrastructure |
| Cần **thay** nguồn dữ liệu (CSDL ↔ API ↔ cache) hoặc **test nghiệp vụ không cần CSDL** | repository/cổng |
| Chỉ để "sau này có thể đổi ORM" | thường không đáng (YAGNI); đổi ORM hiếm khi xảy ra |

Quy tắc thực dụng: repository đáng giá khi nó **diễn đạt câu hỏi nghiệp vụ** và **bảo vệ ranh giới tầng**, không phải khi nó chỉ đổi tên `db.SanPhams`.

## Phía đọc khác phía ghi: Read Model

Phía **ghi** cần *aggregate đầy đủ* để bảo vệ bất biến. Phía **đọc** (danh sách, tìm kiếm, báo cáo) chỉ cần *dữ liệu phẳng để hiển thị* — nạp aggregate cho việc này là lãng phí và dính vào value object/quan hệ. Solution dùng cổng riêng:

```csharp
public interface IKhoDocDuLieu
{
    Task<TrangKetQua<SanPhamDto>> TimAsync(string? tuKhoa, string? nhom, int trang, int kichThuoc, CancellationToken ct);
    Task<SanPhamDto?> LayTheoMaAsync(string ma, CancellationToken ct);
}
```

Cài đặt đọc trực tiếp từ một **thực thể không khoá** ánh xạ vào cùng bảng bằng truy vấn SQL:

```csharp
mb.Entity<SanPhamDoc>(e =>
{
    e.HasNoKey();
    e.ToSqlQuery("SELECT Id, Ma, Ten, Nhom, DonGia, TonKho, MucCanhBao FROM san_pham");
    e.Property(x => x.DonGia).HasConversion<double>();
});

// Lọc/sắp xếp trên read model (cột đơn giản), chiếu sang DTO ở CUỐI
IQueryable<SanPhamDoc> q = db.SanPhamDocs.AsNoTracking();
if (!string.IsNullOrWhiteSpace(tuKhoa)) q = q.Where(s => EF.Functions.Like(s.Ten, $"%{tuKhoa}%") || EF.Functions.Like(s.Ma, $"%{tuKhoa}%"));
int tong = await q.CountAsync(ct);
var dong = await q.OrderBy(s => s.Id).Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToListAsync(ct);
```

Ưu điểm: truy vấn đọc **nhanh** (không tracking, không hydrate aggregate), **không bị ràng buộc** bởi value object, và có thể tối ưu độc lập (chỉ mục riêng, view, thậm chí CSDL/cache riêng — đó là CQRS ở Chương 5). Kinh nghiệm thật khi viết solution: lọc trên DTO đã `Select` từ thực thể có value object gây lỗi dịch LINQ; chuyển sang read model phẳng và chiếu DTO **sau cùng** giải quyết gọn.

## Specification — đóng gói điều kiện truy vấn

Khi cùng một điều kiện ("sản phẩm sắp hết", "khách VIP hoạt động") xuất hiện ở nhiều nơi, **Specification** gói nó thành đối tượng có tên, tái sử dụng và kết hợp được:

```csharp
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> DieuKien { get; }

    public Specification<T> Va(Specification<T> khac) => new VaSpec<T>(this, khac);
}

public sealed class SapHetHangSpec : Specification<SanPhamDoc>
{
    public override Expression<Func<SanPhamDoc, bool>> DieuKien => s => s.TonKho <= s.MucCanhBao;
}

public sealed class TrongNhomSpec(string nhom) : Specification<SanPhamDoc>
{
    public override Expression<Func<SanPhamDoc, bool>> DieuKien => s => s.Nhom == nhom;
}

// Dùng: q.Where(new SapHetHangSpec().Va(new TrongNhomSpec("Phu kien")).DieuKien)
```

(`VaSpec<T>` — nối hai cây biểu thức thành một — được để lại làm **bài tập 1**; đoạn trên là phác thảo ý tưởng, không nằm trong solution.)

Điểm quan trọng: điều kiện là **`Expression<Func<T,bool>>`** (cây biểu thức), không phải `Func<T,bool>` — EF dịch được sang SQL. Ghép `Va/Hoac` cần nối cây biểu thức (thư viện **Ardalis.Specification** làm sẵn, kèm `Include`, sắp xếp, phân trang). Đặt tên rõ ("SapHetHang") biến quy tắc kinh doanh thành khái niệm hạng nhất, thay vì rải `TonKho <= MucCanhBao` khắp nơi (và lệch nhau khi ai đó sửa một chỗ). Chỉ dùng khi thật sự có điều kiện lặp/phức tạp; với truy vấn đơn giản, LINQ trực tiếp trong read model là đủ.

## Giao dịch ngoài "một lệnh, một lần lưu"

- **Nhiều `SaveChanges` cần chung giao dịch**: `await using var tx = await db.Database.BeginTransactionAsync();` (đưa vào cổng `IUnitOfWork.BatDauGiaoDichAsync` nếu Application cần).
- **Xung đột đồng thời**: `IsConcurrencyToken` trên `TonKho` → `DbUpdateConcurrencyException` → Infrastructure dịch thành `ConcurrencyException` (Application hiểu) → behavior trả `Loi.XungDot` → HTTP `409`. Application **không bao giờ thấy** kiểu exception của EF.
- **Một lệnh sửa hai aggregate cùng lúc**: dấu hiệu ranh giới aggregate sai, hoặc cần **sự kiện** và nhất quán cuối cùng (Chương 8).

## Lỗi thường gặp

- Repository generic `IRepository<T>` + `GetAll()` rồi lọc trong bộ nhớ.
- Trả `IQueryable` từ repository → tầng trên viết truy vấn, phụ thuộc EF ngầm.
- Repository gọi `SaveChanges` trong từng phương thức → mất khả năng gom giao dịch.
- Nhiều repository cho các bảng con của cùng một aggregate.
- Dùng aggregate cho truy vấn danh sách/báo cáo (chậm, kéo theo cả đồ thị).
- Specification dùng `Func` thay vì `Expression` → EF tải cả bảng rồi lọc trong bộ nhớ (chỉ thấy khi dữ liệu lớn).
- Để exception EF (`DbUpdateException`) lọt tới tầng Web.

## Bài tập

1. Viết `Specification<SanPhamDoc>` thật (kể cả `Va`) dùng `Expression.Invoke`/thay tham số; kiểm tra bằng `ToQueryString()` rằng vẫn ra **một** mệnh đề `WHERE`.
2. Thêm truy vấn `SapHetAsync()` vào `IKhoDocDuLieu` dùng `SapHetHangSpec`.
3. Đo số câu SQL của `TimAsync` (dùng `LogTo`); so với việc nạp aggregate rồi map sang DTO.
4. Thêm `IUnitOfWork.BatDauGiaoDichAsync` và dùng trong một lệnh "chuyển kho" sửa hai aggregate; bàn về rủi ro.
5. Viết test: khi hai người cùng xuất và một bị `ConcurrencyException`, handler/behavior trả `409` chứ không phải `500` (đã có ở `ApplicationTests` — đọc và giải thích).
