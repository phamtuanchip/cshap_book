# Chương 19 — Static, partial, extension method, enum

## Mục tiêu học

Sau chương này, bạn sẽ:

- Viết **static class** và hiểu khi nào phù hợp.
- Viết **extension method** để "mở rộng" kiểu có sẵn (nền tảng của LINQ).
- Dùng **enum** và `[Flags]` để biểu diễn tập giá trị có tên.
- Biết `partial class` và **lớp lồng nhau** (nested class).

Code mẫu: [`code/ch19-static-extension-enum/`](../../code/ch19-static-extension-enum/).

## Static class

Static class chỉ chứa thành viên `static`, không tạo đối tượng, không kế thừa. Hợp cho tiện ích không có trạng thái:

```csharp
static class ToanHoc
{
    public static int BinhPhuong(int x) => x * x;
}

Console.WriteLine(ToanHoc.BinhPhuong(9));
```

`Math`, `Console`, `File` đều là static class. **Đừng lạm dụng**: nhồi mọi thứ vào static class biến chương trình
thành lập trình thủ tục, khó kiểm thử vì không thay thế được (không có interface). Nghiệp vụ nên nằm trong đối
tượng; static chỉ dành cho hàm thuần tuý và tiện ích.

## Extension method

Cho phép "thêm" phương thức cho một kiểu **mà không sửa hay kế thừa** kiểu đó (kể cả `string`, `int`, kiểu của thư viện):

```csharp
static class MoRong
{
    public static bool LaSoChan(this int n) => n % 2 == 0;
    public static string VietHoaChuDau(this string s) { ... }
}

Console.WriteLine(5.LaSoChan());                  // false
Console.WriteLine("hello world".VietHoaChuDau());  // "Hello World"
```

Quy tắc: khai báo trong **static class** không lồng nhau; tham số đầu có từ khoá **`this`** chỉ ra kiểu được mở rộng;
namespace chứa nó phải được `using` để "thấy" phương thức. Thực chất chỉ là đường cú pháp cho lời gọi phương thức
static (`MoRong.LaSoChan(5)`).

Extension method là cách toàn bộ **LINQ** (`Where`, `Select`, `OrderBy`... — Chương 30) được viết, và cách cấu hình
`builder.Services.AddXxx()` trong ASP.NET Core. Nguyên tắc: dùng khi phương thức thật sự "thuộc về" kiểu đó về mặt
khái niệm và bạn không sở hữu mã nguồn; đừng tạo hàng loạt extension trên `object`/`string` cho mọi việc vặt.

Lưu ý: nếu kiểu đã có phương thức cùng chữ ký, phương thức của kiểu **luôn thắng** extension.

## Enum

**Enum** là tập hợp các hằng số có tên, thay cho "số ma thuật":

```csharp
enum TrangThaiDon
{
    ChoXacNhan = 1,
    DangGiao = 2,
    DaGiao = 3,
}

var tt = TrangThaiDon.DangGiao;
Console.WriteLine($"{tt} = {(int)tt}");       // DangGiao = 2
```

So với `int trangThai = 2;` thì `TrangThaiDon.DangGiao` tự mô tả, và trình biên dịch chặn gán giá trị lung tung
(`switch` có thể cảnh báo thiếu nhánh).

Thao tác hữu ích:

```csharp
Enum.Parse<TrangThaiDon>("DaGiao");                   // ném lỗi nếu sai
Enum.TryParse("KhongCo", out TrangThaiDon kq);         // false
foreach (var v in Enum.GetValues<TrangThaiDon>()) ...   // liệt kê
```

Enum mặc định là `int`, bắt đầu từ `0`, tăng dần nếu không gán. Ép số bất kỳ sang enum (`(TrangThaiDon)99`) **không
báo lỗi** — dùng `Enum.IsDefined` để kiểm tra. Gắn thêm hành vi cho enum bằng **extension method** (như `MoTa()` trong mã mẫu).

### `[Flags]` — tổ hợp nhiều giá trị

Khi các giá trị có thể **kết hợp** (quyền hạn, tuỳ chọn), dùng luỹ thừa của 2 và thuộc tính `[Flags]`:

```csharp
[Flags]
enum Quyen { Khong = 0, Doc = 1, Ghi = 2, Xoa = 4, TatCa = Doc | Ghi | Xoa }

var q = Quyen.Doc | Quyen.Ghi;          // bật 2 cờ  → "Doc, Ghi"
q.HasFlag(Quyen.Ghi);                    // true
(q & Quyen.Xoa) != 0;                    // false
q |= Quyen.Xoa;                          // bật thêm
q &= ~Quyen.Doc;                         // tắt Doc
```

Đây chính là toán tử bit đã giới thiệu ở Chương 6: `|` để bật, `&` để kiểm tra, `~` để đảo mặt nạ.

## Partial class

`partial` cho phép chia **một** class thành nhiều đoạn/file, gộp lại lúc biên dịch:

```csharp
partial class SinhVien { public string Ten { get; set; } = ""; }
partial class SinhVien { public override string ToString() => Ten; }
```

Ít dùng trong code bình thường. Nó chủ yếu để **tách code do công cụ sinh ra** khỏi code bạn viết tay (ví dụ
source generator, một số tệp do IDE/framework sinh). Điều quan trọng là nhận ra `partial` khi gặp.

## Lớp lồng nhau (nested class)

Class khai báo bên trong class khác. Nếu để `private`, chỉ class chứa nó thấy được — hợp cho chi tiết cài đặt:

```csharp
class DanhSachLienKet
{
    private Nut? _dau;
    private class Nut(int giaTri)          // chỉ DanhSachLienKet dùng được
    {
        public int GiaTri { get; } = giaTri;
        public Nut? Tiep { get; set; }
    }
}
```

Dùng khi lớp nhỏ chỉ có nghĩa trong ngữ cảnh lớp cha (nút của danh sách, bộ duyệt...). Gọi từ ngoài (nếu `public`):
`DanhSachLienKet.Nut`. Lớp lồng truy cập được cả thành viên `private` của lớp bao ngoài.

## Lỗi thường gặp

- Extension method không xuất hiện: thiếu `using` namespace, hoặc không đặt trong static class.
- Ép số vào enum không hợp lệ mà không kiểm tra (`Enum.IsDefined`).
- Quên gán giá trị luỹ thừa của 2 cho enum `[Flags]` (`Doc=1, Ghi=2, Xoa=3` sai → 3 = Doc|Ghi).
- Trộn tiện ích tĩnh và trạng thái toàn cục (`static` field thay đổi được) → lỗi khó truy vết, nhất là đa luồng.
- Đặt `partial` cho class mà chưa cần — chỉ làm code khó tìm.

## Bài tập

1. Viết extension `DemTu(this string s)` trả số từ trong chuỗi và `Chia<T>(this IEnumerable<T>, int n)` (tuỳ chọn).
2. Định nghĩa enum `NgayTrongTuan` và extension `LaCuoiTuan()`.
3. Tạo `[Flags] enum TuyChonIn { None = 0, DamNet = 1, Nghieng = 2, GachChan = 4 }`, in ra tổ hợp và kiểm tra từng cờ.
4. Viết `static class DinhDang` có `Tien(decimal)` và `Ngay(DateTime)`; cân nhắc: nên là static class hay class thường có interface? Vì sao?
5. Viết thêm nested class `DuyetVien` bên trong `DanhSachLienKet` để duyệt danh sách.
