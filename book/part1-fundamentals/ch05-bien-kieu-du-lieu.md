# Chương 5 — Biến, kiểu dữ liệu, ép kiểu

## Mục tiêu học

Sau chương này, bạn sẽ:

- Khai báo biến, hằng số và hiểu **kiểu dữ liệu** dùng để làm gì.
- Chọn đúng kiểu số (`int`, `long`, `double`, `decimal`), `bool`, `char`, `string`.
- Dùng `var` đúng lúc, ép kiểu ngầm định/tường minh, chuyển chuỗi thành số an toàn.
- Hiểu **tràn số** và **sai số số thực** — hai lỗi âm thầm rất hay gặp.

Code mẫu đầy đủ: [`code/ch05-bien-kieu-du-lieu/`](../../code/ch05-bien-kieu-du-lieu/).

## Biến là gì?

**Biến** là một ô nhớ có tên, dùng để lưu một giá trị. C# là ngôn ngữ **kiểu tĩnh**: mỗi biến có
một kiểu cố định ngay lúc biên dịch, và trình biên dịch sẽ báo lỗi nếu bạn dùng sai kiểu.

```csharp
int tuoi = 25;          // kiểu tên_biến = giá_trị;
string ten = "Tuan";
tuoi = 26;              // gán lại giá trị mới: OK
// tuoi = "hai muoi";   // LỖI biên dịch: không gán string cho int
```

Quy ước đặt tên: dùng chữ, số, `_`; không bắt đầu bằng số; phân biệt hoa/thường. Biến cục bộ dùng
`camelCase` (`soLuong`); tên kiểu, phương thức dùng `PascalCase` (`TinhTong`).

## Các kiểu dữ liệu cơ bản

| Kiểu | Kích thước | Dùng cho | Ví dụ |
|------|-----------|----------|-------|
| `int` | 32 bit | số nguyên thông thường (≈ ±2,1 tỷ) | `int tuoi = 25;` |
| `long` | 64 bit | số nguyên rất lớn | `long danSo = 8_000_000_000L;` |
| `byte` | 8 bit | 0..255 (dữ liệu nhị phân) | `byte b = 200;` |
| `double` | 64 bit | số thực (khoa học, đo lường) | `double h = 1.75;` |
| `float` | 32 bit | số thực ít chính xác hơn | `float f = 1.5f;` |
| `decimal` | 128 bit | số thực **chính xác thập phân** — tiền tệ | `decimal gia = 19.99m;` |
| `bool` | 1 bit logic | `true` / `false` | `bool ok = true;` |
| `char` | 16 bit | một ký tự Unicode | `char c = 'A';` |
| `string` | — | chuỗi ký tự | `string s = "chao";` |

Hậu tố quyết định kiểu của hằng số: `L` cho `long`, `f` cho `float`, `m` cho `decimal`. Dấu `_` chỉ
để dễ đọc (`8_000_000_000`). Mỗi kiểu số có `MinValue`/`MaxValue`: `int.MaxValue` là 2147483647.

**Chọn kiểu nào?** Mặc định `int` cho số nguyên, `double` cho số thực, `decimal` cho **tiền**,
`string` cho văn bản, `bool` cho điều kiện.

> Các kiểu trên (trừ `string`) là **kiểu giá trị** (value type): biến chứa trực tiếp giá trị.
> `string` và mảng là **kiểu tham chiếu**: biến chứa địa chỉ trỏ tới dữ liệu. Sự khác biệt này quan
> trọng, sẽ quay lại ở Chương 9 và 20.

## `var` — để trình biên dịch suy ra kiểu

```csharp
var soLuong = 3;                // int
var thanhTien = soLuong * 19.99m; // decimal
var ten = "Tuan";               // string
```

`var` **không** phải là kiểu động: kiểu vẫn cố định lúc biên dịch, chỉ là bạn không phải viết ra.
Dùng `var` khi kiểu đã rõ ràng từ vế phải (`var list = new List<int>();`); nếu đọc code mà không thấy
ngay kiểu là gì, hãy viết kiểu tường minh.

## Hằng số

```csharp
const double Pi = 3.14159;   // giá trị cố định, phải gán ngay lúc khai báo
// Pi = 3;                   // LỖI: không đổi được hằng
```

## Ép kiểu

**Ngầm định** (an toàn, không mất dữ liệu — tự động):

```csharp
int a = 10;
long b = a;        // int -> long: OK
double c = a;      // int -> double: OK
```

**Tường minh** (có thể mất dữ liệu — phải viết `(kiểu)`):

```csharp
double d = 9.99;
int n = (int)d;    // 9 — cắt bỏ phần thập phân, KHÔNG làm tròn
int m = (int)Math.Round(d);   // 10 — muốn làm tròn thì dùng Math.Round
```

**Chuyển chuỗi thành số:**

```csharp
int n1 = int.Parse("123");                 // ném FormatException nếu chuỗi sai
if (int.TryParse("abc", out int n2)) { }   // trả false thay vì ném lỗi
```

Với dữ liệu người dùng nhập, **luôn dùng `TryParse`**, vì người dùng có thể gõ bất kỳ thứ gì.

## Hai cái bẫy âm thầm

### 1. Tràn số (overflow)

```csharp
int lon = int.MaxValue;
Console.WriteLine(lon + 1);   // -2147483648 — không báo lỗi!
```

Mặc định C# **không** báo lỗi khi tràn số mà "quay vòng". Muốn phát hiện, bọc trong `checked`:

```csharp
checked { int x = lon + 1; }   // ném OverflowException
```

### 2. Sai số số thực

```csharp
Console.WriteLine(0.1 + 0.2 == 0.3);     // False !
Console.WriteLine(0.1m + 0.2m == 0.3m);  // True
```

`double`/`float` lưu số dạng nhị phân nên không biểu diễn chính xác `0.1`. Với **tiền tệ** hoặc mọi thứ
cần đúng thập phân, dùng `decimal`. Với `double`, không so sánh bằng `==` mà so sánh trong sai số:
`Math.Abs(a - b) < 1e-9`.

## Lỗi thường gặp

- `CS0266: Cannot implicitly convert type 'double' to 'int'` — cần ép kiểu tường minh (và tự hỏi có
  chấp nhận mất phần thập phân không).
- `CS0165: Use of unassigned local variable` — dùng biến chưa gán giá trị.
- `FormatException` khi `int.Parse("12a")` — dùng `TryParse`.
- Chia số nguyên: `1 / 2` bằng `0`, không phải `0.5` (Chương 6).
- Dùng `double` cho tiền rồi ra kết quả lẻ `0.30000000000000004`.

## Bài tập

1. Khai báo biến lưu tên, tuổi, chiều cao, đã kết hôn hay chưa — chọn kiểu phù hợp và in ra.
2. `int.MaxValue + 1` cho kết quả gì? Bọc `checked` để bắt lỗi.
3. Viết chương trình đọc một số từ bàn phím bằng `Console.ReadLine()` và `int.TryParse`; báo lỗi
   thân thiện nếu nhập sai.
4. Tính `100 * 1.1` bằng `double` và `decimal`, so sánh kết quả với `110`.
