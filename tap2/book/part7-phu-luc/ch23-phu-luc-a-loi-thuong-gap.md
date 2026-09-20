# Phụ lục A — Lỗi thường gặp ASP.NET Core

Tra cứu theo **triệu chứng**. Cột "Chương" trỏ tới nội dung giải thích.

## A.1 Khởi động và cấu hình

| Triệu chứng / thông báo | Nguyên nhân | Cách xử lý | Ch. |
|-------------------------|-------------|-----------|-----|
| `Address already in use` | cổng đang bị dùng | `--urls http://localhost:5xxx`; tắt tiến trình cũ | 2 |
| Trình duyệt báo chứng chỉ không tin cậy | chưa tin dev certificate | `dotnet dev-certs https --trust` | 2 |
| `Cannot modify ServiceCollection after application started` | gọi `builder.Services.Add...` sau `Build()` | đăng ký trước `Build()` | 2, 5 |
| `OptionsValidationException` khi khởi động | cấu hình sai (`ValidateOnStart`) | sửa giá trị theo thông báo | 6 |
| Cấu hình đọc ra `null`/mặc định | sai khoá, sai cấp, sai môi trường | dùng Options + validate; kiểm tra `ASPNETCORE_ENVIRONMENT` | 6 |
| `FileNotFoundException: appsettings.json` | không copy ra thư mục output | `CopyToOutputDirectory` (SDK Web đã làm sẵn) | 6 |
| Biến môi trường không ghi đè | dùng `:` thay `__` | `CuaHang__Ten` | 6 |

## A.2 Pipeline, routing, DI

| Triệu chứng | Nguyên nhân | Cách xử lý | Ch. |
|-------------|-------------|-----------|-----|
| Mọi URL rơi vào cùng một handler, endpoint không chạy | `app.Run(...)` terminal đặt trước endpoint | dùng `MapFallback` | 3 |
| Luôn 401/403 dù đã đăng nhập | `UseAuthorization` trước `UseAuthentication`, hoặc thiếu `UseAuthentication` | đúng thứ tự | 3, 20 |
| `Headers are read-only, response has already started` | sửa header sau khi body bắt đầu gửi | `Response.OnStarting` | 3 |
| Request treo | quên `await next(ctx)` / `.Result` | `await` | 3 |
| `404` cho endpoint tồn tại | thiếu `MapControllers`/`MapRazorPages`, sai constraint (`{id:int}` với `abc`) | kiểm tra map và route | 4, 7 |
| `AmbiguousMatchException` | hai endpoint cùng route | đổi route / thêm constraint | 4, 7 |
| `Body was inferred but the method does not allow inferred body parameters` | tham số lớp không đăng ký DI bị hiểu là body | đăng ký dịch vụ, hoặc `[FromServices]`/`[FromBody]` | 4 |
| `Unable to resolve service for type 'X'` | quên đăng ký DI | `AddScoped<...>` | 5 |
| `Cannot consume scoped service from singleton` | captive dependency | hạ vòng đời / `IServiceScopeFactory` | 5 |
| `A circular dependency was detected` | A cần B, B cần A | thiết kế lại | 5 |
| `415 Unsupported Media Type` | thiếu `Content-Type: application/json` | thêm header | 7 |
| `400` bind lỗi kiểu | chuỗi vào số, JSON sai | xem `errors` trong ProblemDetails | 4, 8 |

## A.3 EF Core và dữ liệu

| Triệu chứng | Nguyên nhân | Cách xử lý | Ch. |
|-------------|-------------|-----------|-----|
| Dữ liệu không được lưu | quên `SaveChanges` | gọi (và `await`) | 12 |
| `A second operation was started on this context` | `DbContext` dùng chung nhiều luồng/quên `await` | mỗi request một context; `await` mọi lời gọi | 12 |
| `no such table` | chưa migrate/tạo DB | `Migrate()`/`dotnet ef database update` | 12 |
| Truy vấn chậm, hàng trăm câu SQL/request | **N+1** | `Include`/projection; đếm câu SQL | 13 |
| `NullReferenceException` trên navigation | quên `Include` | `Include` hoặc projection | 13 |
| `JsonException: possible object cycle` | serialize entity có vòng tham chiếu | trả DTO | 8, 13 |
| `DbUpdateException` UNIQUE/FK/CHECK | vi phạm ràng buộc CSDL | dịch sang `409`/`400` | 12, 14 |
| `DbUpdateConcurrencyException` | hai người sửa cùng dòng | tải lại, thử lại hoặc `409`; hoặc `UPDATE` có điều kiện | 13, 22 |
| `The LINQ expression could not be translated` | dùng hàm C# EF không dịch được | viết lại, tách phần chạy trong bộ nhớ có chủ ý | 12 |
| `SQLite does not support expressions of type 'decimal'` | `SUM/ORDER BY` trên decimal ở SQLite | converter `double`; hoặc dùng CSDL khác | 12, 22 |
| Tìm kiếm phân biệt hoa/thường bất ngờ | `Contains` → `instr` trên SQLite | `EF.Functions.Like` | 14 |
| `database is locked` | nhiều writer SQLite | `Default Timeout`, WAL, hoặc CSDL server | 22 |
| Migration lệch schema | sửa migration đã áp dụng / trộn `EnsureCreated` | thêm migration mới | 12 |

## A.4 API, validation, lỗi

| Triệu chứng | Nguyên nhân | Cách xử lý | Ch. |
|-------------|-------------|-----------|-----|
| Trả `200` kèm nội dung lỗi | không dùng mã HTTP đúng | `404/400/409`; ProblemDetails | 1, 9 |
| Lộ stack trace ra client | trang lỗi dev ở production | `UseExceptionHandler` | 9 |
| `[Required]` không bắt được số vắng | kiểu giá trị mặc định `0` | `decimal?` hoặc `required` | 8 |
| `IValidatableObject` không chạy | còn lỗi validation thuộc tính | sửa lỗi thuộc tính trước (chủ ý) | 8 |
| Lỗi CORS chỉ trong trình duyệt | thiếu origin trong policy / sai thứ tự `UseCors` | liệt kê origin; `UseCors` trước auth | 9 |
| Test báo `The type 'Program' is inaccessible` | thiếu `public partial class Program;` | thêm | 10 |
| Test "xanh" nhưng không kiểm tra gì | quên `await` | `await` | 10 |
| Test lúc đạt lúc hỏng | trạng thái chung/thứ tự | dữ liệu riêng mỗi test | 10 |

## A.5 Giao diện web

| Triệu chứng | Nguyên nhân | Cách xử lý | Ch. |
|-------------|-------------|-----------|-----|
| `The view 'X' was not found` | sai tên/thư mục view | `Views/<Controller>/X.cshtml` | 15 |
| Model type mismatch (`ViewDataDictionary`) | truyền sai kiểu model | khớp `@model` | 15 |
| POST form trả `400` | thiếu antiforgery token | dùng tag helper form / gửi header | 17 |
| F5 gửi lại form | không PRG | redirect sau POST thành công | 17 |
| Razor Page `404` | thiếu `@page` | thêm `@page` | 16 |
| `[BindProperty]` rỗng khi GET | mặc định chỉ POST | `SupportsGet = true` | 16 |
| Lỗi validation không hiện cạnh ô | tên trường sai tiền tố (`Input.Ma`) | `AddModelError("Input.Ma", ...)` | 16 |
| Dropdown trống sau lỗi | quên nạp lại `SelectList` | nạp lại trước `return Page()` | 16 |
| Blazor: không tương tác | thiếu `@rendermode` | `@rendermode InteractiveServer` | 18 |
| Blazor: UI không cập nhật | đổi dữ liệu ngoài luồng sự kiện | `InvokeAsync(StateHasChanged)` | 18 |
| Tiếng Việt thành `&#x1ED9;` trong HTML | mã hoá mặc định | `WebEncoderOptions` với `UnicodeRanges.All` | 15 |

## A.6 HttpClient và bảo mật

| Triệu chứng | Nguyên nhân | Cách xử lý | Ch. |
|-------------|-------------|-----------|-----|
| Cạn socket / `SocketException` | `new HttpClient()` mỗi request | `IHttpClientFactory` | 19 |
| Xử lý body lỗi như dữ liệu | quên `EnsureSuccessStatusCode` | kiểm tra mã | 19 |
| Timeout lâu, treo | không đặt timeout/`CancellationToken` | resilience + token | 19 |
| Thanh toán bị trừ hai lần | retry `POST` không idempotent | idempotency key; tắt retry cho `POST` | 19 |
| Token JWT bị `401` dù đúng | sai `iss/aud`, hết hạn, sai khoá | kiểm tra `TokenValidationParameters`, đồng hồ | 20 |
| Token giải mã không được ở instance khác | Data Protection khoá không dùng chung | lưu khoá dùng chung | 21 |
| Rate limit chặn cả nhóm người dùng | giới hạn theo IP proxy | `UseForwardedHeaders` đúng | 21 |
| `429` khi test | rate limit bật | nới trong cấu hình test | 21, 22 |

## A.7 Cách tìm lỗi trong ASP.NET Core

1. Đọc **log** (mức `Information`/`Debug`): dòng `Request starting`/`Executing endpoint` cho biết request đã tới đâu.
2. Ở `Development` trang lỗi hiển thị stack trace; production dùng `traceId` trong ProblemDetails để tra log.
3. Bật log SQL: `Microsoft.EntityFrameworkCore.Database.Command = Information`.
4. Gọi bằng `curl -i`/`.http` để thấy header và body thật; DevTools → Network với trình duyệt.
5. Tái hiện bằng **integration test** rồi sửa — test ở lại làm hàng rào.
