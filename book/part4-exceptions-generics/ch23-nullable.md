# Chương 23 — Nullable value type và nullable reference type

## Mục tiêu học

Sau chương này, bạn sẽ:

- Biểu diễn "không có giá trị" cho kiểu giá trị bằng `int?`, `DateTime?`...
- Hiểu **nullable reference types**: `string` vs `string?` và cảnh báo của trình biên dịch.
- Dùng thành thạo `?.`, `??`, `??=`, `is null`, `is not null`, `!`.
- Thiết kế API biểu đạt rõ ràng khi nào giá trị có thể vắng mặt.

Code mẫu: [`code/ch23-nullable/`](../../code/ch23-nullable/).

## "Tỷ đô": lỗi null

`null` nghĩa là "không tham chiếu tới gì". Gọi phương thức trên `null` → `NullReferenceException`, lỗi phổ biến nhất
của người viết C#. Người phát minh ra `null` gọi nó là "sai lầm tỷ đô". C# hiện đại có hai công cụ để chế ngự nó.

## Nullable value type: `T?`

Kiểu giá trị (`int`, `bool`, `DateTime`, `struct`) mặc định **không thể** null. Thêm `?` để cho phép:

```csharp
int? tuoi = null;                       // thực chất là Nullable<int>
tuoi.HasValue;                          // false
tuoi.GetValueOrDefault(-1);             // -1
tuoi = 30;
tuoi.Value;                             // 30 (nếu null thì ném InvalidOperationException)
```

Ví dụ thực tế: cột CSDL có thể trống (ngày sinh chưa nhập), tham số tuỳ chọn, kết quả "không tìm thấy" của phép tính.

**Lan truyền null**: phép toán trên `int?` cho kết quả `null` nếu một vế là null:

```csharp
int? a = 5, b = null;
var tong = a + b;    // null
```

## Nullable reference type (C# 8+)

Kiểu tham chiếu ngày trước luôn có thể null mà trình biên dịch không cảnh báo. Với `<Nullable>enable</Nullable>` trong
`.csproj` (mặc định trong template mới), quy ước đổi thành:

- `string` — **không** được null (cam kết).
- `string?` — **có thể** null (phải kiểm tra trước khi dùng).

```csharp
string ten = "An";         // OK
string ten2 = null;        // CẢNH BÁO CS8600: gán null cho kiểu không nullable
string? tuyChon = null;    // OK
int len = tuyChon.Length;  // CẢNH BÁO CS8602: dereference có thể null
```

Chỉ là **cảnh báo** lúc biên dịch (không đổi hành vi lúc chạy), nhưng nó phát hiện hàng loạt bug tiềm ẩn trước khi
chạy. Thói quen tốt: **coi cảnh báo nullable là lỗi**, hoặc bật `<TreatWarningsAsErrors>` cho nullable.

Quy tắc thiết kế:

- Property/tham số **bắt buộc** → kiểu không nullable (`string Ten`), đảm bảo gán trong constructor hoặc dùng `required`.
- Có thể vắng mặt → `string? GhiChu`, và người dùng buộc phải xử lý.
- Phương thức có thể "không tìm thấy" → trả `T?` (`NguoiDung? TimNguoi(...)`).

## Toán tử làm việc với null

| Toán tử | Tên | Ý nghĩa |
|---------|-----|---------|
| `x?.Y` | null-conditional | nếu `x` null trả `null`, ngược lại `x.Y` |
| `x?[i]` | null-conditional index | như trên cho chỉ mục |
| `x ?? d` | null-coalescing | `x` nếu khác null, ngược lại `d` |
| `x ??= d` | null-coalescing assignment | gán `d` cho `x` nếu `x` đang null |
| `x is null` / `x is not null` | kiểm tra bằng pattern | (nên dùng thay `== null`) |
| `x!` | null-forgiving | báo trình biên dịch "tin tôi, không null" |

```csharp
Console.WriteLine(nguoi.DiaChi?.ThanhPho ?? "chua co dia chi");   // chuỗi ?. an toàn nhiều tầng
int[]? mang = null;
Console.WriteLine(mang?[0]);                                        // không lỗi

string? s = null;
s ??= "moi gan";
if (s is not null) Console.WriteLine(s.Length);   // sau kiểm tra, trình biên dịch biết s không null
```

`?.` cắt cả chuỗi phía sau: `a?.B.C.D` — nếu `a` null, toàn biểu thức là `null` (không đánh giá `B.C.D`).
Kết quả của `?.` luôn là kiểu nullable (`int?`), nên thường đi cùng `??`.

### Toán tử `!` — dùng thận trọng

```csharp
string? chuaChac = LayChuoi();
Console.WriteLine(chuaChac!.Length);   // tắt cảnh báo, nhưng nếu thật sự null vẫn NullReferenceException
```

`!` chỉ **tắt cảnh báo**, không bảo vệ gì cả. Chỉ dùng khi bạn thật sự biết hơn trình biên dịch (ví dụ giá trị chắc
chắn được khởi tạo bởi framework). Nếu thấy mình rải `!` khắp nơi, thiết kế đang có vấn đề.

## Bảo vệ ở "ranh giới"

Với dữ liệu từ bên ngoài (người dùng, file, API), không tin vào kiểu không nullable — kiểm tra thật:

```csharp
static void XuLy(string dauVao)
{
    ArgumentNullException.ThrowIfNull(dauVao);
    ...
}
```

Trong nội bộ code đã bật nullable, các hàm `private` có thể tin vào kiểu.

## Thiết kế thay thế cho null

Đôi khi "trả về null" mơ hồ. Các lựa chọn:

- **Trả `T?`**: đơn giản, cho "có hoặc không" (`TimNguoi`).
- **`TryXxx(out T)`**: `if (dict.TryGetValue(k, out var v))` — mẫu quen thuộc của .NET.
- **Trả collection rỗng** thay vì `null` (`[]`, `Array.Empty<T>()`, `Enumerable.Empty<T>()`) — bên gọi khỏi kiểm tra null.
- **Giá trị mặc định có nghĩa** (Null Object pattern): một đối tượng "không làm gì".
- **Result/Option** kết quả có kiểu (Tập 3).

Đừng trả `null` cho collection; và đừng dùng `null` mang nhiều ý nghĩa ("chưa tải", "lỗi", "rỗng").

## Lỗi thường gặp

- `NullReferenceException` — nguyên nhân số 1; đọc `StackTrace` để biết biến nào null.
- `InvalidOperationException: Nullable object must have a value` — `.Value` trên `int?` đang null.
- Tắt nullable toàn cục để hết cảnh báo thay vì sửa.
- Rải `!` để "im lặng" trình biên dịch.
- Gán `null!` cho property để hết cảnh báo: dùng `required` hoặc khởi tạo mặc định tốt hơn.
- Quên rằng chuỗi `?.` cho ra kiểu nullable: `int len = s?.Length;` báo lỗi, phải `?? 0`.

## Bài tập

1. Viết hàm `int? TimViTri(int[] mang, int giaTri)` trả `null` nếu không có; dùng `??` để hiển thị "không thấy".
2. Cho `record NguoiDung(string Ten, DiaChi? DiaChi)` — in thành phố hoặc "N/A" bằng một biểu thức duy nhất.
3. Bật `<Nullable>enable</Nullable>`, viết class có `string Ten` không khởi tạo, đọc cảnh báo và sửa bằng 3 cách (`required`, constructor, giá trị mặc định).
4. Viết phương thức `TryDocSo(string? s, out int so)` xử lý cả `null`.
5. So sánh: phương thức trả `List<string>?` với trả `List<string>` rỗng — khác gì với người gọi?
