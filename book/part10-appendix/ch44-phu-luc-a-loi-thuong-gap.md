# Phụ lục A — Bảng lỗi thường gặp và cách xử lý

Tra cứu nhanh theo **triệu chứng** (thông báo lỗi). Có hai nhóm: **lỗi biên dịch** (mã `CSxxxx`, trình biên dịch báo ngay) và **ngoại lệ lúc
chạy** (chương trình đã chạy rồi mới ném lỗi). Cột cuối chỉ chương liên quan.

## A.1 Đọc thông báo lỗi thế nào

1. Đọc **dòng đầu**: mã lỗi + mô tả. Mã lỗi (`CS0103`...) tra được trên [learn.microsoft.com](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/).
2. Xem **vị trí**: `Program.cs(12,5)` = dòng 12, cột 5. Lỗi thật đôi khi nằm **ngay trước** dòng đó (thiếu `;`, thiếu `}`).
3. Sửa lỗi **đầu tiên** rồi biên dịch lại — các lỗi sau thường là hệ quả dây chuyền.
4. Với ngoại lệ lúc chạy: đọc **StackTrace**, tìm dòng đầu tiên thuộc code của bạn.

## A.2 Lỗi biên dịch thường gặp

| Thông báo | Nguyên nhân thường gặp | Cách xử lý | Chương |
|-----------|------------------------|-----------|--------|
| `CS1002: ; expected` | thiếu dấu `;` cuối lệnh | thêm `;` (kiểm tra cả dòng trước) | 3 |
| `CS1513: } expected` | thiếu/dư ngoặc nhọn | căn thụt lề, để IDE định dạng lại (`Ctrl+K,D`) | 3 |
| `CS0103: The name 'x' does not exist in the current context` | sai tên/hoa thường, biến ngoài phạm vi | kiểm tra chính tả; khai báo biến ở đúng scope | 5, 10 |
| `CS0246: type or namespace 'X' could not be found` | thiếu `using`/tham chiếu project/gói NuGet | thêm `using`; `dotnet add reference/package` | 4, 15 |
| `CS0266: Cannot implicitly convert 'double' to 'int'` | ép kiểu mất dữ liệu | ép tường minh `(int)` hoặc `Math.Round` | 5 |
| `CS0165: Use of unassigned local variable` | dùng biến chưa gán | gán giá trị khởi tạo | 5 |
| `CS0136: A local variable named 'x' cannot be declared in this scope` | trùng tên biến với scope ngoài | đổi tên | 10 |
| `CS0161: not all code paths return a value` | có nhánh không `return` | thêm `return`/`throw` cho mọi nhánh | 10 |
| `CS7036: There is no argument that corresponds to the required parameter` | gọi constructor/phương thức thiếu đối số | truyền đủ đối số hoặc thêm constructor | 13, 16 |
| `CS1061: 'X' does not contain a definition for 'Y'` | gọi thành viên không có (sai tên, sai kiểu, thiếu `using` cho extension method) | kiểm tra kiểu và tên; `using System.Linq;` | 17, 19 |
| `CS0122: 'X' is inaccessible due to its protection level` | truy cập thành viên `private`/`internal` | đổi access modifier hoặc dùng property | 14 |
| `CS0120: An object reference is required for the non-static field` | gọi thành viên thường từ `static` | tạo đối tượng, hoặc đổi thành `static` | 13 |
| `CS0534: does not implement inherited abstract member` | chưa `override` đủ thành viên abstract | cài đặt đủ | 18 |
| `CS0535: does not implement interface member` | chưa cài đủ interface | cài đủ (nhớ `public`) | 18 |
| `CS0144: Cannot create an instance of the abstract type or interface` | `new` một abstract class/interface | `new` lớp cụ thể | 18 |
| `CS0506: cannot override because it is not marked virtual` | lớp cha thiếu `virtual` | thêm `virtual` ở lớp cha | 16 |
| `CS0108: hides inherited member` | vô tình che thành viên cha | dùng `override` hoặc `new` có chủ đích | 16 |
| `CS0104: 'X' is an ambiguous reference` | hai namespace cùng có tên `X` | dùng tên đầy đủ/alias | 15 |
| `CS8600/CS8602/CS8604: possible null ...` | cảnh báo nullable | kiểm tra null, dùng `?.`/`??`, hoặc đổi kiểu `T?` | 23 |
| `CS8618: Non-nullable property must contain a non-null value` | property `string` chưa khởi tạo | khởi tạo, `required`, hoặc `?` | 14, 23 |
| `CS1503: cannot convert from 'X' to 'Y'` (đối số) | truyền sai kiểu | ép/chuyển đổi đúng kiểu | 10 |
| `CS0029: Cannot implicitly convert type` | gán sai kiểu | chuyển đổi tường minh | 5 |
| `CS4014: the call is not awaited` (cảnh báo) | gọi `async` mà quên `await` | thêm `await` (hoặc xử lý có chủ đích) | 33 |
| `CS4033: 'await' operator can only be used within an async method` | thiếu `async` ở phương thức | thêm `async Task` | 33 |
| `CS0019: Operator cannot be applied to operands` | toán tử với kiểu không phù hợp (vd `T > T`) | dùng `IComparable<T>`, ép kiểu | 6, 22 |
| `CS0029/CS0037: Cannot convert null to non-nullable value type` | gán `null` cho `int`... | dùng `int?` | 23 |
| `CS8509: switch expression does not handle all possible values` | thiếu nhánh `_` | thêm nhánh mặc định | 7 |
| `CS8120: The switch case is unreachable / already handled` | nhánh cụ thể đặt sau nhánh tổng quát | đổi thứ tự (cụ thể trước) | 7, 17 |
| `CS0017: more than one entry point` | nhiều `Main`/top-level statements | chỉ giữ một | 3 |
| `CS9035: Required member must be set` | quên gán property `required` | gán trong object initializer | 14, 32 |

## A.3 Ngoại lệ lúc chạy thường gặp

| Ngoại lệ | Nguyên nhân | Cách xử lý / phòng tránh | Chương |
|----------|-------------|--------------------------|--------|
| `NullReferenceException` | dùng đối tượng đang `null` | bật nullable; `?.`, `??`; kiểm tra `is null`; xem StackTrace | 13, 23 |
| `IndexOutOfRangeException` | chỉ số ngoài mảng | duyệt `< Length`; kiểm tra biên | 9 |
| `ArgumentOutOfRangeException` | chỉ số/giá trị ngoài phạm vi (`List`, `Substring`) | kiểm tra `Count`, độ dài trước | 11, 25 |
| `KeyNotFoundException` | `dict[key]` khi khoá chưa có | `TryGetValue`, `ContainsKey` | 26 |
| `InvalidCastException` | ép kiểu sai | dùng `is`/`as` | 17 |
| `FormatException` | `int.Parse("abc")` | `TryParse` | 5 |
| `OverflowException` | tràn số trong `checked` / chuyển kiểu | dùng kiểu lớn hơn, kiểm tra | 5 |
| `DivideByZeroException` | chia số nguyên cho 0 | kiểm tra mẫu số | 6 |
| `InvalidOperationException: Collection was modified` | thêm/xoá khi đang `foreach` | duyệt ngược, `RemoveAll`, duyệt bản sao `ToList()` | 25, 26 |
| `InvalidOperationException: Sequence contains no elements` | `First()`/`Max()` trên dãy rỗng | `FirstOrDefault`, `Any()` | 30 |
| `InvalidOperationException: Nullable object must have a value` | `.Value` trên `int?` đang null | kiểm tra `HasValue`/`??` | 23 |
| `ArgumentNullException` | truyền `null` vào tham số không cho phép | kiểm tra đầu vào; `ThrowIfNull` | 21 |
| `ArgumentException` | đối số không hợp lệ / khoá trùng trong `Dictionary.Add` | kiểm tra trước, dùng `TryAdd`/`dict[k]=v` | 21, 26 |
| `StackOverflowException` | đệ quy không điều kiện dừng / quá sâu | thêm điều kiện dừng, chuyển sang vòng lặp/`Stack` | 10, 29 |
| `OutOfMemoryException` | nạp quá nhiều dữ liệu vào bộ nhớ | xử lý theo luồng/lười (`ReadLines`, `yield`) | 27, 35 |
| `FileNotFoundException` / `DirectoryNotFoundException` | đường dẫn sai, cwd khác dự kiến | kiểm tra đường dẫn tuyệt đối, `Path.Combine` | 35 |
| `UnauthorizedAccessException` | thiếu quyền, hoặc đường dẫn là thư mục | ghi vào thư mục được phép | 35 |
| `IOException` (file in use) | file đang bị chương trình khác/ chính bạn khoá | đóng bằng `using`; `FileShare` | 35 |
| `JsonException` | JSON sai cú pháp/sai kiểu | kiểm tra đầu vào; bắt và báo lỗi | 36 |
| `OperationCanceledException` | tác vụ bị huỷ (`CancellationToken`) | bắt riêng; thường là hợp lệ | 33 |
| `TaskCanceledException`/`TimeoutException` | hết hạn chờ | thử lại/báo người dùng | 33 |
| `AggregateException` | lỗi gộp từ nhiều Task / `.Result` | `await` (lỗi được "mở" ra); xem `InnerExceptions` | 33 |
| `ObjectDisposedException` | dùng đối tượng đã `Dispose` | giữ phạm vi `using` đủ rộng | 21 |
| `NotImplementedException` | phần chưa viết | cài đặt | 18 |
| `NotSupportedException` | thao tác không hỗ trợ (vd sửa collection chỉ đọc) | dùng cấu trúc phù hợp | 18 |
| `SwitchExpressionException` | switch expression không khớp nhánh nào | thêm nhánh `_` | 7 |
| `TypeInitializationException` | lỗi trong hàm khởi tạo tĩnh (`static`) | xem `InnerException` | 13 |

## A.4 Lỗi logic — "không báo lỗi nhưng kết quả sai"

| Triệu chứng | Nguyên nhân | Chương |
|-------------|-------------|--------|
| `1 / 2` ra `0` | chia nguyên | 6 |
| `0.1 + 0.2 != 0.3` | sai số số thực | 5 |
| `int.MaxValue + 1` ra số âm | tràn số không báo lỗi | 5 |
| `s.Trim();` mà `s` không đổi | `string` bất biến, quên gán kết quả | 11 |
| Sửa `b` mà `a` cũng đổi | class/mảng là kiểu tham chiếu | 9, 13 |
| Hai đối tượng "giống nhau" nhưng `==` ra `false` | so sánh tham chiếu | 13, 20 |
| `HashSet.Contains` không tìm thấy phần tử vốn có | thiếu `Equals`/`GetHashCode` | 20, 26 |
| Truy vấn LINQ cho kết quả khác dự đoán / chạy lại nhiều lần | thực thi trì hoãn | 27, 30 |
| `dem++` từ nhiều thread ra kết quả nhỏ hơn | race condition | 34 |
| Lệnh sau `async` chạy trước khi xong | quên `await` | 33 |
| Số/ngày in ra dấu sai trên máy khác | khác culture | 11, 36 |
| Tiếng Việt vỡ dấu khi đọc/ghi file | sai mã hoá | 35 |
| Cấu hình đọc ra rỗng/mặc định | sai khoá, sai môi trường | 40 |
| Test lúc đạt lúc hỏng | phụ thuộc thứ tự/thời gian/trạng thái chung | 38 |

## A.5 Quy trình khi gặp lỗi mà bạn chưa hiểu

1. Copy **nguyên văn** thông báo lỗi (kèm mã) và tìm kiếm — thường đã có người gặp.
2. Thu nhỏ vấn đề: tạo project console mới, tái hiện lỗi bằng vài dòng.
3. Dùng debugger (Chương 39) thay vì đoán.
4. Hỏi (Stack Overflow, cộng đồng): nêu **phiên bản .NET**, đoạn code tối thiểu, thông báo lỗi đầy đủ, điều đã thử.
