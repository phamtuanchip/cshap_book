# Chương 32 — Record, pattern matching và cú pháp C# hiện đại

## Mục tiêu học

Sau chương này, bạn sẽ:

- Tổng hợp và dùng thành thạo các tính năng C# hiện đại: `record`, `init`/`required`, **primary constructor**,
  **collection expression**, **raw string literal**.
- Dùng **pattern matching** nâng cao: property, relational, logical, **list pattern**.
- Viết code ngắn hơn mà vẫn rõ nghĩa, và biết khi nào **không** nên dùng.

Code mẫu: [`code/ch32-csharp-hien-dai/`](../../code/ch32-csharp-hien-dai/).

Nhiều tính năng đã xuất hiện rải rác ở các chương trước; chương này gom lại thành "bộ đồ nghề" để đọc và viết code kiểu
.NET hiện đại (thứ bạn sẽ gặp khắp nơi trong ASP.NET Core).

## Record và `with`

```csharp
record SinhVien(string Ten, int Tuoi, string Lop);

var a = new SinhVien("An", 20, "CNTT");
var b = a with { Tuoi = 21 };      // sao chép, đổi một phần → bản mới
a == b;                             // false: so sánh theo giá trị các thuộc tính
var (ten, tuoi, _) = a;             // deconstruct
```

Ôn (Chương 20): `record` tự sinh `Equals`, `GetHashCode`, `ToString`, `Deconstruct` và thuộc tính `init`. Dùng cho dữ liệu
bất biến: DTO, thông điệp, giá trị nghiệp vụ.

## `init` và `required`

```csharp
class SanPham
{
    public required string Ten { get; init; }
    public required decimal Gia { get; init; }
}

var sp = new SanPham { Ten = "Chuot", Gia = 150_000m };
// new SanPham { Ten = "x" };   // LỖI BIÊN DỊCH: thiếu Gia
```

`required` ép người dùng class gán giá trị lúc khởi tạo; `init` cho phép gán chỉ một lần. Kết hợp với nullable (Chương 23)
loại bỏ hàng loạt lỗi "quên gán".

## Primary constructor (C# 12)

Tham số constructor đặt ngay sau tên class, dùng được trong toàn thân class:

```csharp
class KhachHang(string ten, string email)
{
    public string Ten { get; } = ten;                       // gán vào property
    public string ChaoHoi() => $"Xin chao {Ten} <{email}>"; // dùng trực tiếp trong phương thức
}
```

Rất hợp cho **dependency injection** trong ASP.NET Core:
`class DonHangService(IRepository repo, ILogger<DonHangService> log) { ... }`.

Lưu ý: tham số primary constructor của `class`/`struct` là **biến bị bắt (captured)**, không tự thành property/field
public (khác `record`). Muốn chỉ đọc, nên gán vào property/`readonly` field.

## Collection expression (C# 12)

```csharp
int[] x = [1, 2, 3];
List<int> gop = [0, .. x, .. [4, 5], 99];   // ".." trải (spread) một dãy vào
IReadOnlyList<string> r = ["a", "b"];
List<int> rong = [];
```

Một cú pháp thống nhất cho mảng, `List`, `HashSet`, `Span`... Kiểu đích quyết định kết quả.

## Raw string literal (C# 11)

```csharp
string json = $$"""
    {
      "ten": "{{ten2}}",
      "tuoi": 20
    }
    """;
```

Đặt trong `"""..."""`, thoải mái dùng dấu `"` và `\` mà không escape; nhiều dòng; thụt lề của dấu đóng `"""` được tự cắt.
`$$"""` nghĩa là dấu chèn giá trị là **hai** ngoặc `{{ }}` (để một ngoặc `{ }` đơn của JSON không bị hiểu nhầm).

## Pattern matching nâng cao

Bạn đã biết mẫu hằng, kiểu, `when` (Chương 7). Đầy đủ hơn:

```csharp
static string MoTa(object? o) => o switch
{
    null => "null",
    int n and > 0 => $"so duong {n}",                          // kiểu + quan hệ + logic
    int n => $"so khong duong {n}",
    string { Length: > 3 } s => $"chuoi dai '{s}'",            // property pattern
    string s => $"chuoi ngan '{s}'",
    int[] [var dau, .., var cuoi] => $"mang tu {dau} den {cuoi}",   // list pattern
    SinhVien { Tuoi: < 20, Lop: var lop } => $"SV tre lop {lop}",   // property pattern lồng
    SinhVien sv => $"SV {sv.Ten}",
    _ => $"khac: {o.GetType().Name}",
};
```

| Mẫu | Ví dụ | Ý nghĩa |
|-----|-------|---------|
| Relational | `< 0`, `>= 15` | so sánh với hằng |
| Logical | `and`, `or`, `not` | kết hợp mẫu |
| Property | `{ Length: > 3 }` | kiểm tra thuộc tính |
| Positional | `(var x, var y)` | dùng `Deconstruct` |
| List | `[1, .., var cuoi]`, `[_, _]` | khớp cấu trúc dãy (`..` = "phần còn lại") |
| `var` | `var x` | luôn khớp, gán vào biến |

```csharp
static string PhanLoaiNhietDo(double t) => t switch
{
    < 0 => "dong bang",
    >= 0 and < 15 => "lanh",
    >= 15 and < 30 => "de chiu",
    >= 30 => "nong",
    double.NaN => "loi",
};
```

Trình biên dịch kiểm tra **tính đầy đủ**: quên trường hợp sẽ cảnh báo. Pattern matching hợp khi dữ liệu có nhiều dạng
(kiểu, cấu trúc, khoảng giá trị); nếu hành vi nên thuộc về chính đối tượng, đa hình (Chương 17) vẫn tốt hơn.

## Các cú pháp ngắn gọn khác

```csharp
int[] mang = [10, 20, 30, 40, 50];
mang[^1];              // 50 — chỉ số từ cuối
mang[1..3];            // [20, 30] — lát cắt
s ??= "mac dinh";      // gán nếu null
if (s is not null and { Length: > 3 }) ...
(int min, int max) = (list.Min(), list.Max());   // tuple deconstruct
```

- **Expression-bodied members**: `public string Ten => ten;`.
- **Target-typed `new`**: `List<int> ds = new();`, `Point p = new(1, 2);`.
- **`static` lambda** (`static n => n * 2`): cấm bắt biến ngoài — an toàn và không cấp phát closure.
- **Local functions**: hàm lồng trong hàm, hợp cho trợ giúp cục bộ.
- **File-scoped namespace** (`namespace X;`), **global usings**, **top-level statements** (Chương 15, 3).

## Còn gì mới?

Mỗi bản .NET đi cùng một bản C# mới, thường thêm vài đường cú pháp. Từ C# 13/14 (đi cùng .NET 9/10) có thêm các tính năng
như `params` cho collection, từ khoá `field` trong property (thân `get/set` dùng backing field tự sinh), **extension
members** (khai báo cả property/toán tử mở rộng), null-conditional assignment (`a?.B = 1`)... Xem mục "What's new in C#"
trên [learn.microsoft.com](https://learn.microsoft.com/dotnet/csharp/whats-new/) khi cập nhật lên phiên bản mới — luôn
kiểm tra phiên bản ngôn ngữ mà project của bạn đang dùng (`<LangVersion>`).

## Nguyên tắc dùng "cú pháp mới"

1. **Ưu tiên độ rõ ràng**, không phải độ ngắn. Chuỗi pattern lồng ba tầng khó đọc hơn hai `if`.
2. Đồng đội/CI phải dùng cùng phiên bản SDK, nếu không code sẽ không biên dịch.
3. Dùng công cụ: IDE gợi ý "convert to pattern matching/collection expression" và **`.editorconfig`** thống nhất phong cách.
4. Đừng viết lại hàng loạt code cũ chỉ để "cho mới" — thay khi sửa tới.

## Lỗi thường gặp

- Nhầm tham số primary constructor với property (không tự sinh property cho `class`).
- Dùng `record` cho entity có danh tính và trạng thái thay đổi (dễ gây nhầm khi `==` so sánh theo giá trị).
- Nhánh `switch` tổng quát đặt trước nhánh cụ thể → lỗi biên dịch "nhánh không bao giờ đạt".
- `with` chỉ sao chép **nông**: thuộc tính kiểu tham chiếu (List, class) vẫn dùng chung.
- Quên `$$"""` khi chuỗi JSON có `{ }` nhưng vẫn muốn nội suy.
- Dùng collection expression với kiểu không hỗ trợ.

## Bài tập

1. Viết lại một vòng `foreach` + `if/else if` đã có thành switch expression với property pattern.
2. Dùng list pattern viết hàm phân tích lệnh: `["go", var huong]`, `["take", var vat]`, `["quit"]`, còn lại là "không hiểu".
3. Viết `record TienTe(decimal SoTien, string Loai)` với phép cộng (`operator +`) chỉ cho cùng loại tiền.
4. Tạo chuỗi HTML nhiều dòng có nội suy bằng raw string.
5. Viết `class DonHangService(IRepo repo, ILogger log)` (interface tự định nghĩa) dùng primary constructor.
