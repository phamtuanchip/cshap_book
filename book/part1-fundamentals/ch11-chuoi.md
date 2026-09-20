# Chương 11 — Chuỗi (string)

## Mục tiêu học

Sau chương này, bạn sẽ:

- Thao tác chuỗi thành thạo: cắt, tìm, thay, tách, ghép, so sánh.
- Định dạng đầu ra bằng **nội suy chuỗi** (`$"..."`) và các định dạng số/ngày.
- Hiểu `string` **bất biến** và dùng `StringBuilder` khi ghép nhiều lần.
- Dùng chuỗi nguyên văn (`@""`) và **raw string** (`"""`).

Code mẫu: [`code/ch11-chuoi/`](../../code/ch11-chuoi/).

## `string` là gì?

`string` là chuỗi các ký tự Unicode (mỗi `char` là một đơn vị UTF-16). Là kiểu **tham chiếu** nhưng hoạt
động như giá trị vì nó **bất biến (immutable)**: một khi tạo ra, nội dung không đổi được. Mọi phương thức
"sửa" chuỗi thực chất trả về **chuỗi mới**.

```csharp
string a = "abc";
string b = a.ToUpper();   // "ABC" — chuỗi mới
Console.WriteLine(a);     // "abc" — a không đổi
```

Hệ quả thường gặp: viết `s.Trim();` mà không gán kết quả là **vô tác dụng**.

## Thao tác thường dùng

```csharp
string s = "  Xin chao, C# va .NET  ";
s.Trim()                     // bỏ khoảng trắng hai đầu (TrimStart/TrimEnd)
s.Length                     // độ dài
s.ToUpper() / s.ToLower()
s.Contains("C#")             // true
s.IndexOf("C#")              // vị trí đầu tiên, -1 nếu không có
s.Replace("chao", "tam biet")
s.Trim().Substring(4, 4)     // lấy 4 ký tự từ vị trí 4 → "chao"
s.StartsWith("Xin") / s.EndsWith("NET")
s[0]                         // ký tự tại vị trí 0 (kiểu char)
```

### Tách và ghép

```csharp
string[] ten = "an,binh,chi".Split(',');      // ["an","binh","chi"]
string ghep = string.Join(" | ", ten);        // "an | binh | chi"
```

### Kiểm tra rỗng

```csharp
string.IsNullOrEmpty(s)        // null hoặc ""
string.IsNullOrWhiteSpace(s)   // null, "" hoặc chỉ toàn khoảng trắng — thường dùng hơn
```

## Nội suy chuỗi và định dạng

```csharp
string ten = "Tuan"; int tuoi = 25;
Console.WriteLine($"{ten} {tuoi} tuổi");
```

Trong `{ }` có thể viết biểu thức, kèm **căn lề** và **định dạng** sau dấu `,` và `:`:

| Cú pháp | Ý nghĩa | Ví dụ |
|---------|---------|-------|
| `{x:N2}` | số có phân cách nghìn, 2 số lẻ | `1,234,567.89` |
| `{x:F1}` | 1 chữ số thập phân | `1234567.9` |
| `{x:P1}` | phần trăm | `0.256` → `25.6%` (tuỳ culture) |
| `{ngay:dd/MM/yyyy}` | định dạng ngày | `09/03/2025` |
| `{s,6}` / `{s,-6}` | căn phải / căn trái trong 6 ô | `[    ab]` / `[ab    ]` |

Kết quả của `N2`, `P1`, ngày tháng phụ thuộc **culture** (vùng ngôn ngữ) của máy: dấu thập phân có thể là
`.` hoặc `,`. Chương trình mẫu đặt `CultureInfo.InvariantCulture` để cho kết quả giống nhau trên mọi máy.
Khi ghi/đọc dữ liệu để trao đổi giữa các hệ thống (file, API), luôn dùng culture bất biến.

## So sánh chuỗi

```csharp
"abc" == "abc"                                         // true — so sánh nội dung
string.Equals("ABC", "abc", StringComparison.OrdinalIgnoreCase)   // true: bỏ qua hoa/thường
```

Với `string`, `==` so sánh **nội dung** (không phải tham chiếu). Khi cần chính xác về hoa/thường và culture,
chỉ rõ `StringComparison` (`Ordinal` cho so sánh kỹ thuật, `OrdinalIgnoreCase` cho không phân biệt hoa/thường).
Tránh tự `ToLower()` rồi so sánh — tạo chuỗi thừa và dễ sai với một số ngôn ngữ.

## `StringBuilder` — ghép chuỗi nhiều lần

Vì `string` bất biến, `s += x` trong vòng lặp tạo ra một chuỗi mới mỗi vòng → chậm và tốn bộ nhớ khi lặp
hàng nghìn lần. Dùng `StringBuilder`, một bộ đệm có thể sửa:

```csharp
var sb = new StringBuilder();
for (int i = 1; i <= 5; i++) sb.Append(i).Append(',');
sb.Length--;                    // bỏ dấu phẩy cuối
string kq = sb.ToString();      // "1,2,3,4,5"
```

Quy tắc: vài phép nối cố định → dùng `$""` hoặc `+`; **lặp nhiều lần** → `StringBuilder`.

## Chuỗi nguyên văn và raw string

```csharp
string duongDan = @"C:\Users\Tuan\file.txt";   // @: không cần escape dấu \

string json = """
    { "ten": "Tuan", "tuoi": 25 }
    """;                                        // raw string: nhiều dòng, dấu " tự do
```

Chuỗi thường phải escape: `"\n"` xuống dòng, `"\t"` tab, `"\\"`, `"\""`. Raw string (C# 11) rất hợp cho JSON, SQL,
HTML nhúng; phần thụt lề trước dấu `"""` đóng được tự động cắt bỏ.

## Duyệt ký tự

```csharp
foreach (char c in "Lap trinh C#")
    if ("aeiouAEIOU".Contains(c)) nguyenAm++;
```

`char` có các hàm tiện ích: `char.IsDigit(c)`, `char.IsLetter(c)`, `char.ToUpper(c)`. Một số ký tự (emoji)
chiếm hai `char` — khi xử lý văn bản đa ngôn ngữ chuyên sâu, cần dùng `System.Globalization.StringInfo`.

## Lỗi thường gặp

- Quên gán kết quả: `s.Trim();` (không đổi `s`).
- `ArgumentOutOfRangeException` từ `Substring` khi chỉ số/độ dài vượt quá.
- `NullReferenceException` khi gọi `.Length` trên chuỗi `null` → kiểm tra bằng `IsNullOrEmpty` hoặc `?.`.
- Nối chuỗi bằng `+=` trong vòng lặp lớn.
- So sánh không phân biệt hoa/thường bằng `ToLower()` thay vì `StringComparison`.
- Số/ngày in ra sai dấu do khác culture.

## Bài tập

1. Đếm số từ trong một câu (gợi ý: `Split(' ', StringSplitOptions.RemoveEmptyEntries)`).
2. Kiểm tra chuỗi có phải **palindrome** (đọc xuôi ngược giống nhau), bỏ qua hoa/thường và khoảng trắng.
3. Viết hàm viết hoa chữ cái đầu mỗi từ: `"le van tuan"` → `"Le Van Tuan"`.
4. Đếm số lần xuất hiện của mỗi ký tự trong chuỗi, in theo thứ tự chữ cái.
5. So sánh thời gian nối 100.000 số bằng `+=` và `StringBuilder` (dùng `System.Diagnostics.Stopwatch`).
