# Chương 18 — Abstract class và Interface

## Mục tiêu học

Sau chương này, bạn sẽ:

- Viết và dùng **abstract class**: khung chung, thành viên `abstract`, **template method**.
- Viết và cài đặt **interface**: hợp đồng, nhiều interface, **default interface method**, **explicit implementation**.
- Chọn đúng giữa abstract class và interface.
- Hiểu vì sao lập trình theo interface làm code linh hoạt, dễ kiểm thử.

Code mẫu: [`code/ch18-abstract-interface/`](../../code/ch18-abstract-interface/).

## Abstract class

**Abstract class** là lớp **không tạo được đối tượng trực tiếp** (`new BaoCao()` là lỗi), chỉ để kế thừa. Nó có thể
chứa cả phần **đã cài đặt** lẫn phần **chỉ khai báo** (`abstract`) buộc lớp con phải hoàn thành.

```csharp
abstract class BaoCao(string tieuDe)
{
    protected string TieuDe { get; } = tieuDe;

    // Template method: khung cố định, các bước con thay đổi
    public string Xuat() => $"{DauTrang()}\n{TieuDe}\n{ChanTrang()}";

    protected abstract string DauTrang();                    // BẮT BUỘC con cài đặt
    protected virtual string ChanTrang() => "-- het --";     // mặc định, con có thể ghi đè
}

class BaoCaoText(string tieuDe) : BaoCao(tieuDe)
{
    protected override string DauTrang() => "=== BAO CAO ===";
}
```

`Xuat()` là **Template Method**: quy trình định sẵn ở lớp cha, chỗ thay đổi do lớp con quyết định. Đây là một
design pattern quen thuộc (Chương 41).

Dùng abstract class khi các lớp con **thực sự chia sẻ trạng thái/code** và có quan hệ "là một" rõ ràng (`Hinh`, `BaoCao`).

## Interface

**Interface** là một **hợp đồng**: liệt kê "làm được gì" mà không nói "làm thế nào". Class nào cài đặt interface
cam kết cung cấp đủ các thành viên đó.

```csharp
interface IThanhToan
{
    void ThanhToan(decimal soTien);
    decimal TinhPhi(decimal soTien) => 0m;   // default interface method
}

class TheTinDung(string soThe) : IThanhToan
{
    public void ThanhToan(decimal soTien) => Console.WriteLine($"The {soThe}: tra {soTien:N0}");
    public decimal TinhPhi(decimal soTien) => soTien * 0.02m;
}
```

Quy ước đặt tên: bắt đầu bằng chữ **`I`** (`IThanhToan`). Thành viên interface mặc định là `public`. Interface
không chứa field, không có constructor.

### Nhiều interface

Một class chỉ kế thừa **một** lớp cha nhưng cài đặt **nhiều** interface:

```csharp
class ViDienTu : IThanhToan, ILuuVet { ... }
```

Đây là cách C# đạt "đa kế thừa hành vi" một cách an toàn.

### Default interface method (C# 8)

Interface có thể cung cấp cài đặt mặc định cho một thành viên (`TinhPhi` ở trên). Lớp cài đặt có thể dùng luôn
hoặc ghi đè. Hữu ích để **thêm phương thức mới vào interface mà không làm vỡ** các class đã cài đặt cũ. Lưu ý phương
thức mặc định chỉ gọi được **qua biến kiểu interface** (`IThanhToan c = ...; c.TinhPhi(...)`).

### Explicit implementation

Khi hai interface có phương thức trùng tên nhưng nghĩa khác nhau:

```csharp
interface IBay { string DiChuyen(); }
interface IBoi { string DiChuyen(); }

class ChimCanhCut : IBay, IBoi
{
    string IBay.DiChuyen() => "Bay";
    string IBoi.DiChuyen() => "Boi";
}

IBay may = new ChimCanhCut();
may.DiChuyen();   // "Bay"
```

Thành viên khai báo tường minh **chỉ gọi được qua kiểu interface**, không qua kiểu class.

## Abstract class hay Interface?

| | Abstract class | Interface |
|---|---|---|
| Quan hệ | "là một" (cùng họ) | "có khả năng" (cùng hợp đồng) |
| Kế thừa/cài đặt | 1 lớp cha | nhiều interface |
| Trạng thái (field) | có | không |
| Constructor | có | không |
| Code dùng chung | có | chỉ mặc định (hạn chế) |
| Dùng khi | chia sẻ code & trạng thái, có khung xử lý | định nghĩa hợp đồng, tách rời phụ thuộc |

**Quy tắc thực dụng:** bắt đầu bằng interface. Chỉ dùng abstract class khi có phần code/trạng thái dùng chung đáng kể.
Hai thứ dùng chung được: interface mô tả hợp đồng, abstract class cung cấp cài đặt gốc để tiện kế thừa.

## Lập trình theo interface

```csharp
class DonHang(IThanhToan thanhToan)
{
    public void ThanhToan(decimal soTien) => thanhToan.ThanhToan(soTien);
}
```

`DonHang` chỉ biết `IThanhToan`, không biết thẻ, ví hay tiền mặt. Hệ quả:

- **Linh hoạt**: thêm hình thức thanh toán mới không sửa `DonHang`.
- **Dễ kiểm thử**: khi test, truyền vào một đối tượng giả (mock) thay cho cổng thanh toán thật (Chương 38).
- **Nền của Dependency Injection** (ASP.NET Core, Tập 2): framework cấp phát đối tượng cài đặt interface cho bạn.

Đây là ý tưởng cốt lõi của kiến trúc hiện đại: *phụ thuộc vào trừu tượng, không phụ thuộc vào chi tiết* (Chương 42).

## Lỗi thường gặp

- `CS0534: 'X' does not implement inherited abstract member 'Y'` — chưa `override` đủ thành viên `abstract`.
- `CS0144: Cannot create an instance of the abstract type` — cố `new` một abstract class hoặc interface.
- `CS0737: does not implement interface member ... not public` — quên `public` khi cài đặt (implicit implementation
  phải `public`).
- Interface quá to ("fat interface") ép class phải cài đặt những thứ không cần — nên tách nhỏ.
- Gọi default interface method qua biến kiểu class: `new TienMat().TinhPhi(1)` báo lỗi vì class không "thừa hưởng" nó.

## Bài tập

1. Tạo interface `IDocDuLieu<T>`-đơn-giản: `string Doc()`, cài đặt `DocFile` và `DocMang` (trả dữ liệu cố định).
2. Viết abstract class `NhacCu` có `abstract string ChoiNhac()` và template method `BieuDien()` (chào + chơi + cảm ơn).
3. Thêm hình thức thanh toán `ChuyenKhoan` vào code mẫu mà **không sửa** vòng lặp gọi `ThanhToan`.
4. Cho `interface IHinh { double DienTich(); }` — cài đặt bằng `record` và bằng `class`; so sánh.
5. Thêm một phương thức mới vào `ILuuVet` bằng default method và xác nhận `ViDienTu` không cần sửa.
