# Chương 7 — Cấu trúc điều khiển: if/else và switch

## Mục tiêu học

Sau chương này, bạn sẽ:

- Điều khiển luồng chương trình bằng `if / else if / else`.
- Dùng `switch` statement và **switch expression** hiện đại.
- Dùng **pattern matching** (`when`, `and`, `or`, so khớp theo kiểu, tuple) để thay chuỗi `if` dài.
- Biết khi nào chọn `if`, khi nào chọn `switch`.

Code mẫu: [`code/ch07-cau-truc-dieu-khien/`](../../code/ch07-cau-truc-dieu-khien/).

## `if / else if / else`

```csharp
int diem = 78;
if (diem >= 90) Console.WriteLine("Xuất sắc");
else if (diem >= 75) Console.WriteLine("Giỏi");
else if (diem >= 50) Console.WriteLine("Trung bình");
else Console.WriteLine("Yếu");
```

Các điều kiện được xét **từ trên xuống**, nhánh đầu tiên đúng sẽ chạy rồi thoát. Vì vậy phải xếp
điều kiện từ **hẹp đến rộng** (kiểm `>= 90` trước `>= 75`). Điều kiện bắt buộc là `bool`
(khác C/C++ — số `1` không tự thành `true`).

Với khối lệnh nhiều dòng, dùng `{ }`. Ngay cả khi chỉ một dòng, nhiều đội vẫn bắt buộc ngoặc để tránh
lỗi khi sửa sau này.

## `switch` statement

Khi so sánh **một giá trị** với nhiều hằng số:

```csharp
switch (thu)
{
    case 2: case 3: case 4: case 5: case 6:
        Console.WriteLine("Ngày làm việc");
        break;
    case 7:
        Console.WriteLine("Thứ bảy");
        break;
    default:
        Console.WriteLine("Khác");
        break;
}
```

Khác C/C++: mỗi `case` **bắt buộc kết thúc** bằng `break` (hoặc `return`, `throw`...); C# không cho
"chạy tràn" sang case kế. Nhiều nhãn `case` xếp liền nhau (không có lệnh giữa chúng) là được phép.

## Switch expression — cách viết hiện đại

Khi mục đích là **tính ra một giá trị**, switch expression ngắn và an toàn hơn:

```csharp
string TenThu(int t) => t switch
{
    2 => "Thứ hai",
    3 => "Thứ ba",
    >= 4 and <= 6 => "Giữa tuần",
    7 => "Thứ bảy",
    8 => "Chủ nhật",
    _ => "Không hợp lệ",      // _ là "mọi trường hợp còn lại"
};
```

- Mỗi nhánh dạng `mẫu => giá trị`. Không cần `break`.
- Trình biên dịch cảnh báo nếu bạn **quên phủ hết** các trường hợp; nếu chạy vào giá trị không khớp
  nhánh nào sẽ ném `SwitchExpressionException`.

## Pattern matching

Mẫu (pattern) mạnh hơn so sánh `==`:

| Mẫu | Ví dụ | Ý nghĩa |
|-----|-------|---------|
| Hằng | `7` | bằng 7 |
| Quan hệ | `>= 4` | so sánh |
| Kết hợp | `>= 4 and <= 6`, `2 or 3`, `not 0` | `and` / `or` / `not` |
| Kiểu | `string s` | là `string`, gán vào `s` |
| `null` | `null` | là null |
| `when` | `int n when n > 40` | thêm điều kiện phụ |
| Tuple | `(true, false)` | so khớp nhiều giá trị |

```csharp
object?[] doiTuong = [42, 7, "chao", 3.14, null];
foreach (var o in doiTuong)
{
    string mota = o switch
    {
        int n when n > 40 => $"số nguyên lớn: {n}",
        int n => $"số nguyên: {n}",
        string s => $"chuỗi dài {s.Length}",
        null => "null",
        _ => $"kiểu khác: {o.GetType().Name}",
    };
    Console.WriteLine(mota);
}
```

Pattern cũng dùng được với `is`: `if (o is string s && s.Length > 3) { ... }`,
`if (x is >= 1 and <= 9)`, `if (o is not null)`. Chương 17 sẽ đi sâu hơn khi học đa hình.

## Chọn `if` hay `switch`?

- **`if`**: điều kiện phức tạp, dùng `&&`/`||`, so sánh nhiều biến khác nhau.
- **`switch`**: một giá trị được phân loại thành nhiều nhánh; nhất là khi cần trả về một giá trị.
- Chuỗi `if / else if` quá dài và cùng so một biến → đổi sang switch expression.

## Lỗi thường gặp

- `CS8070: Control cannot fall out of switch` — thiếu `break` ở `case`.
- Sắp xếp sai thứ tự điều kiện (`>= 50` trước `>= 90` khiến nhánh sau không bao giờ chạy).
- Switch expression thiếu nhánh `_` → cảnh báo `CS8509`, có thể lỗi lúc chạy.
- Dùng `=` thay `==` trong điều kiện.
- Lồng `if` quá sâu: đảo điều kiện và thoát sớm (`if (!hopLe) return;`) cho code phẳng và dễ đọc.

## Bài tập

1. Nhập điểm 0–10, in xếp loại bằng `if`, rồi viết lại bằng switch expression với `>=`/`and`.
2. Nhập số tháng 1–12, in số ngày của tháng (bỏ qua năm nhuận) bằng switch expression dùng `or`.
3. Viết hàm nhận `object` và mô tả kiểu (số nguyên chẵn/lẻ, chuỗi rỗng/không rỗng, null...).
4. Trò "kéo–búa–bao": nhận hai lựa chọn, dùng switch trên tuple để quyết định người thắng.
