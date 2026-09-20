# Chương 20 — Lớp `object`, `struct` và `record`

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu `object` là gốc của mọi kiểu và các phương thức nó cung cấp (`ToString`, `Equals`, `GetHashCode`).
- Định nghĩa **bằng nhau** đúng cách: `Equals` + `GetHashCode`, `IEquatable<T>`.
- Phân biệt **`struct` (kiểu giá trị)** và **`class` (kiểu tham chiếu)**.
- Dùng **`record`** cho dữ liệu bất biến: so sánh theo giá trị, `with`, deconstruct.
- Sắp xếp bằng `IComparable<T>` và `IComparer<T>`.

Code mẫu: [`code/ch20-object-struct-record/`](../../code/ch20-object-struct-record/).

## `object` — gốc của mọi thứ

Mọi kiểu trong C# (kể cả `int`) đều kế thừa trực tiếp/gián tiếp từ `System.Object`. Ba phương thức bạn hay ghi đè:

| Phương thức | Mặc định | Ghi đè để |
|-------------|----------|-----------|
| `ToString()` | tên kiểu | in dạng dễ đọc |
| `Equals(object?)` | so sánh **tham chiếu** (class) | so sánh theo nội dung |
| `GetHashCode()` | dựa trên tham chiếu | băm theo nội dung, nhất quán với `Equals` |

Ngoài ra `GetType()` cho kiểu thật lúc chạy.

## Bằng nhau: tham chiếu hay giá trị?

```csharp
var s1 = new SinhVienClass("An", 1);
var s2 = new SinhVienClass("An", 1);
s1.Equals(s2);   // false: hai đối tượng khác nhau (mặc định so sánh tham chiếu)
```

Để hai đối tượng "cùng nội dung thì bằng nhau", phải tự định nghĩa:

```csharp
class DiemTuDinhNghia(int x, int y) : IEquatable<DiemTuDinhNghia>
{
    public int X { get; } = x;
    public int Y { get; } = y;

    public bool Equals(DiemTuDinhNghia? other) => other is not null && X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => Equals(obj as DiemTuDinhNghia);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}
```

### Quy tắc vàng: `Equals` và `GetHashCode` đi đôi

Nếu `a.Equals(b)` là `true` thì **bắt buộc** `a.GetHashCode() == b.GetHashCode()`. Vi phạm sẽ làm `HashSet` và
`Dictionary` hoạt động sai (không tìm thấy phần tử vốn đã có) — Chương 26 giải thích cơ chế. Vì vậy **luôn ghi đè cả hai**.
`HashCode.Combine(...)` là cách viết mã băm gọn và đúng. Chỉ dùng các trường **bất biến** để tính mã băm; nếu trường đổi giá trị
sau khi đối tượng nằm trong `HashSet`, nó sẽ "lạc" mất.

Viết tay như vậy rất dài dòng — đó là lý do `record` ra đời (bên dưới).

## `struct` — kiểu giá trị

```csharp
struct ToaDo { public int X; public int Y; }

var p1 = new ToaDo { X = 1, Y = 2 };
var p2 = p1;      // SAO CHÉP toàn bộ dữ liệu
p2.X = 99;        // p1.X vẫn là 1
```

| | `class` | `struct` |
|---|---|---|
| Loại | tham chiếu | giá trị |
| Gán / truyền | sao chép địa chỉ | sao chép dữ liệu |
| Có thể `null` | có | không (trừ `Nullable<T>`, `?`) |
| Kế thừa | có | không (chỉ cài interface) |
| Bộ nhớ | heap (có GC) | thường trên stack / nằm trong đối tượng chứa nó |

Dùng `struct` cho kiểu **nhỏ, bất biến, hành xử như một giá trị** (toạ độ, tiền, màu, `DateTime`, `Guid`). Kích
thước nên ≲ 16 byte. Dùng `readonly struct` để bảo đảm bất biến. Không dùng `struct` cho đối tượng lớn hoặc cần
danh tính riêng — sao chép sẽ tốn kém và gây nhầm lẫn.

## `record` — kiểu dữ liệu hiện đại

`record` là class (hoặc struct) với **so sánh, `ToString`, sao chép, deconstruct tự sinh**, dùng cho dữ liệu bất biến:

```csharp
record NguoiRecord(string Ten, int Tuoi);   // 1 dòng thay cho cả chục dòng

var a = new NguoiRecord("An", 20);
var b = new NguoiRecord("An", 20);
Console.WriteLine(a == b);    // True — so sánh THEO GIÁ TRỊ
Console.WriteLine(a);         // NguoiRecord { Ten = An, Tuoi = 20 }

var c = a with { Tuoi = 21 };   // sao chép, đổi một phần → bản mới, a không đổi
var (ten, tuoi) = a;            // deconstruct
```

Trình biên dịch tự sinh: property `init`, `Equals`/`GetHashCode`/`==`, `ToString`, `Deconstruct`, và hỗ trợ `with`.

- `record` (= `record class`): kiểu tham chiếu, bất biến theo mặc định, hợp DTO, message, value object.
- `record struct` / `readonly record struct`: kiểu giá trị (`readonly record struct DiemRS(int X, int Y);`).

Trong ASP.NET Core (Tập 2) `record` là lựa chọn quen thuộc cho DTO/request/response. Khi cần đối tượng có **danh tính
và trạng thái thay đổi** (entity, `TaiKhoan`), vẫn dùng `class`.

## Sắp xếp: `IComparable<T>` và `IComparer<T>`

- **`IComparable<T>`**: đối tượng biết "so với chính mình" — **thứ tự tự nhiên** (một kiểu chỉ có một):

```csharp
record SanPham(string Ten, decimal Gia) : IComparable<SanPham>
{
    public int CompareTo(SanPham? other) => other is null ? 1 : Gia.CompareTo(other.Gia);
}
ds.Sort();   // theo giá
```

- **`IComparer<T>`**: lớp so sánh **bên ngoài**, cho phép nhiều cách sắp xếp khác nhau:

```csharp
class SoSanhTheoTen : IComparer<SanPham> { ... }
ds.Sort(new SoSanhTheoTen());
ds.Sort((x, y) => y.Gia.CompareTo(x.Gia));   // hoặc dùng lambda
```

Quy ước giá trị trả về: âm (nhỏ hơn), 0 (bằng), dương (lớn hơn). Chương 27 sẽ kết hợp với LINQ (`OrderBy`).

## Lỗi thường gặp

- Ghi đè `Equals` mà quên `GetHashCode` (cảnh báo `CS0659`) → `HashSet`/`Dictionary` lỗi ngầm.
- Mã băm dựa trên trường **thay đổi được**.
- Dùng `==` cho `class` thường và tưởng so sánh nội dung.
- Struct lớn có thể thay đổi (`mutable struct`): sao chép ngầm gây bug rất khó thấy. Ưu tiên `readonly struct`.
- Boxing: gán struct cho biến `object` hay interface sẽ bọc vào heap — tốn hiệu năng nếu lặp nhiều.
- Nhầm `record` với "class bất biến hoàn toàn": các thuộc tính kiểu tham chiếu bên trong (List, class khác) vẫn có thể đổi.

## Bài tập

1. Viết `record TienTe(decimal SoTien, string Loai)` và kiểm tra `==`, `with`, `ToString`.
2. Tạo `HashSet<DiemTuDinhNghia>` rồi cố tình bỏ `GetHashCode` và quan sát `Contains` — vì sao sai?
3. So sánh `struct` và `class` cho `ToaDo`: mảng 1 triệu phần tử, thử đo bộ nhớ/thời gian tạo (gợi ý `Stopwatch`, `GC.GetTotalMemory`).
4. Cài `IComparable<SinhVien>` sắp xếp theo điểm giảm dần, ưu tiên tên khi điểm bằng nhau.
5. Viết `record NguoiDung(string Ten, string Email)` rồi giải thích khi nào nên đổi thành `class`.
