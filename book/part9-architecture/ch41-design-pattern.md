# Chương 41 — Design pattern cơ bản

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **design pattern** là gì và dùng chúng để làm gì (và không phải để làm gì).
- Cài đặt và nhận ra 5 pattern nền tảng: **Singleton**, **Factory**, **Strategy**, **Observer**, **Decorator**.
- Biết cách .NET hiện đại thể hiện các pattern này (DI lifetime, `event`, delegate, middleware).
- Tránh lạm dụng pattern.

Code mẫu: [`code/ch41-design-pattern/`](../../code/ch41-design-pattern/).

## Design pattern là gì?

**Design pattern** (mẫu thiết kế) là **lời giải đã được kiểm chứng cho một vấn đề thiết kế thường gặp**, có tên gọi chung để lập
trình viên trao đổi ("dùng Strategy ở đây"). Nó **không phải** đoạn code copy-paste, mà là ý tưởng bạn hiện thực hoá trong ngữ cảnh
của mình. Bộ mẫu kinh điển là 23 pattern của "Gang of Four" (GoF), chia ba nhóm:

- **Creational** (khởi tạo): Singleton, Factory, Builder...
- **Structural** (cấu trúc): Decorator, Adapter, Facade...
- **Behavioral** (hành vi): Strategy, Observer, Command...

> **Cảnh báo:** pattern là công cụ, không phải mục tiêu. Nhồi pattern vào code đơn giản chỉ làm nó rối. Bắt đầu bằng code
> đơn giản nhất chạy đúng; áp dụng pattern khi thấy **vấn đề cụ thể** (lặp `switch`, cứng nhắc, khó test) mà pattern giải quyết.
> Nhiều pattern cổ điển đã có sẵn trong ngôn ngữ/framework hiện đại (event, delegate, DI, middleware, LINQ).

## 1. Singleton — một thể hiện duy nhất

**Vấn đề:** chỉ được có **một** đối tượng của class (cấu hình dùng chung, bộ nhớ đệm, kết nối tốn kém).

```csharp
sealed class CauHinhUngDung
{
    private static readonly Lazy<CauHinhUngDung> _instance = new(() => new CauHinhUngDung());
    public static CauHinhUngDung Instance => _instance.Value;
    private CauHinhUngDung() { }          // cấm new từ bên ngoài
}
```

`Lazy<T>` bảo đảm khởi tạo **an toàn đa luồng** và **chỉ khi cần**. Khuyết điểm của Singleton tự viết: là **trạng thái toàn cục
ẩn** — code nào cũng gọi `CauHinhUngDung.Instance` được, phụ thuộc bị giấu, **khó test** (không thay bằng đối tượng giả được) và dễ
gây lỗi đa luồng.

**Cách hiện đại:** để **DI container** quản lý thời gian sống: `builder.Services.AddSingleton<ICauHinh, CauHinh>();`. Bạn vẫn có
một thể hiện duy nhất, nhưng nhận nó qua constructor dưới dạng interface — test thay thế dễ dàng (Tập 2). Nên hiểu Singleton như
**một lifetime**, không phải chuyện tự viết `static Instance`.

## 2. Factory — che giấu việc tạo đối tượng

**Vấn đề:** chọn *lớp cụ thể* nào để `new` phụ thuộc vào điều kiện (tham số, cấu hình), và bạn không muốn logic đó rải khắp nơi.

```csharp
static class XuatBaoCaoFactory
{
    public static IXuatBaoCao Tao(string loai) => loai.ToLowerInvariant() switch
    {
        "pdf" => new XuatPdf(),
        "excel" => new XuatExcel(),
        "csv" => new XuatCsv(),
        _ => throw new NotSupportedException($"Khong ho tro '{loai}'"),
    };
}

IXuatBaoCao xuat = XuatBaoCaoFactory.Tao("pdf");    // người dùng chỉ biết interface
```

Bên gọi **chỉ phụ thuộc vào interface** `IXuatBaoCao`; thêm định dạng mới chỉ sửa nhà máy. Biến thể: **Abstract Factory** (nhà máy tạo
cả họ đối tượng liên quan), **Factory Method**, và **Builder** khi đối tượng có nhiều tham số tuỳ chọn. Trong DI, mẫu
tương đương là `Func<string, IXuatBaoCao>` hoặc "keyed services" (`[FromKeyedServices("pdf")]`).

## 3. Strategy — hoán đổi thuật toán

**Vấn đề:** cùng một việc có nhiều cách làm (tính giảm giá, tính phí ship, sắp xếp) và cách nào dùng được chọn lúc chạy. Đừng để
một `switch`/`if` khổng lồ:

```csharp
interface IChinhSachGiamGia { string Ten { get; } decimal TinhTien(decimal tienHang); }

class GiamPhanTram(decimal phanTram) : IChinhSachGiamGia
{
    public string Ten => $"Giam {phanTram}%";
    public decimal TinhTien(decimal t) => t * (100 - phanTram) / 100;
}
class GiamCoDinh(decimal soTien) : IChinhSachGiamGia { ... }

foreach (var cs in chinhSach)
    Console.WriteLine($"{cs.Ten}: {cs.TinhTien(1_000_000m):N0}");
```

Thêm chính sách mới = thêm class mới, không sửa code đang chạy (Open/Closed, Chương 42). Khi thuật toán chỉ là một hàm nhỏ, **delegate** gọn hơn
interface — chính là Strategy trong C#:

```csharp
class DonHang(decimal tienHang, Func<decimal, decimal> chinhSach) { public decimal TongCong() => chinhSach(tienHang); }
new DonHang(1_000_000m, tien => tien * 0.85m);
```

`List<T>.Sort(Comparison<T>)`, `Where(Func<T,bool>)` đều là Strategy.

## 4. Observer — thông báo cho nhiều bên

**Vấn đề:** khi một đối tượng (Subject) thay đổi, nhiều đối tượng khác (Observer) cần biết mà Subject **không nên phụ thuộc** vào từng
người nghe.

Trong C#, đây chính là **`event`** (Chương 31):

```csharp
class CuaHangGiaCoPhieu
{
    public event EventHandler<GiaThayDoiEventArgs>? GiaThayDoi;
    public void DatGia(string ma, decimal giaMoi) { ...; GiaThayDoi?.Invoke(this, new(ma, giaCu, giaMoi)); }
}

cuaHang.GiaThayDoi += (s, e) => Console.WriteLine($"[Bang dien] {e.Ma}: {e.GiaCu} -> {e.GiaMoi}");
cuaHang.GiaThayDoi += (s, e) => { if (e.GiaMoi > e.GiaCu * 1.05m) Console.WriteLine("Canh bao"); };
```

Publisher không biết ai đang nghe; thêm/bớt người nghe không đụng vào publisher. Lưu ý huỷ đăng ký (`-=`) để tránh rò rỉ bộ nhớ.
Với luồng sự kiện phức tạp/bất đồng bộ: `IObservable<T>` (Reactive Extensions), `Channel<T>` (Chương 34), hoặc message bus
(MediatR, hàng đợi thông điệp — Tập 3).

## 5. Decorator — bọc thêm hành vi

**Vấn đề:** thêm chức năng (log, cache, thời gian, mã hoá) cho một đối tượng mà **không sửa nó** và không nổ tung số lớp con.

```csharp
interface IThongBao { void Gui(string noiDung); }

class ThongBaoConsole : IThongBao { public void Gui(string s) => Console.WriteLine(s); }

class ThongBaoCoThoiGian(IThongBao ben) : IThongBao
{
    public void Gui(string s) => ben.Gui($"[08:30] {s}");     // thêm hành vi rồi giao cho đối tượng bên trong
}
class ThongBaoVietHoa(IThongBao ben) : IThongBao
{
    public void Gui(string s) => ben.Gui(s.ToUpperInvariant());
}

IThongBao tb = new ThongBaoVietHoa(new ThongBaoCoThoiGian(new ThongBaoConsole()));
tb.Gui("he thong da san sang");    // [08:30] HE THONG DA SAN SANG
```

Mỗi decorator **cài đúng interface** và **giữ một tham chiếu tới đối tượng cùng interface**; xếp chồng như búp bê Nga. Bạn sẽ gặp lại
ở **middleware pipeline** của ASP.NET Core, `Stream` (`GZipStream` bọc `FileStream`), decorator cache/log cho repository (Tập 3).

## Chọn pattern nào khi nào?

| Dấu hiệu trong code | Cân nhắc |
|---------------------|----------|
| Cần đúng một thể hiện dùng chung | Singleton (qua DI lifetime) |
| `new` rải rác kèm `if/switch` chọn lớp | Factory |
| `switch` khổng lồ theo "loại" cùng làm một việc | Strategy |
| Một thay đổi cần thông báo nhiều nơi | Observer (`event`) |
| Cần thêm hành vi xuyên suốt (log, cache) mà không sửa lớp gốc | Decorator |
| Chuẩn bị đối tượng phức tạp nhiều bước/tham số | Builder |
| Giao diện không khớp thư viện ngoài | Adapter |
| Che phức tạp của nhiều lớp sau một cửa | Facade |
| Đóng gói yêu cầu thành đối tượng (undo, hàng đợi) | Command |

## Lỗi thường gặp

- **Lạm dụng pattern**: ba lớp và hai interface cho việc một hàm làm được ("over-engineering").
- Singleton tự viết mang trạng thái thay đổi được → lỗi đa luồng, test không độc lập.
- Factory trả kiểu cụ thể thay vì interface → mất ý nghĩa.
- Strategy khi chỉ có một cách làm (không có biến thể thật).
- Observer không huỷ đăng ký; ngoại lệ trong một handler làm hỏng các handler sau.
- Decorator lồng quá sâu, thứ tự bọc thay đổi kết quả mà không ai để ý.
- Học thuộc tên pattern mà không hiểu **vấn đề** nó giải quyết.

## Bài tập

1. Thêm định dạng `json` vào Factory và chứng minh không phải sửa code gọi.
2. Thêm `GiamTheoNgayLe` (giảm 30% nếu tháng 12) vào Strategy; dùng `TimeProvider` để test được.
3. Viết `CacheDecorator` bọc `IKhoSanPham` (interface có `LaySanPham(int id)`) để lưu kết quả vào `Dictionary`.
4. Dùng Observer mô phỏng "nút Đặt hàng": khi bấm, một handler gửi email, một handler trừ kho, một handler ghi log.
5. Tìm ba chỗ trong .NET (không phải code bạn viết) thể hiện Strategy, Observer, Decorator.
