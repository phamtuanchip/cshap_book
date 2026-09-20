# Chương 2 — Dự án ASP.NET Core đầu tiên

## Mục tiêu học

Sau chương này, bạn sẽ:

- Tạo, chạy và **hot reload** một dự án ASP.NET Core bằng `dotnet` CLI.
- Đọc hiểu file `Program.cs` theo bốn bước: **builder → services → middleware/endpoint → run**.
- Biết các mẫu dự án (`web`, `webapi`, `mvc`, `razor`, `blazor`) và cấu trúc thư mục.
- Hiểu **Kestrel**, `launchSettings.json`, môi trường `Development`/`Production`, HTTPS dev certificate.

Code mẫu: [`code/ch02-du-an-dau-tien/`](../../code/ch02-du-an-dau-tien/).

## ASP.NET Core là gì?

**ASP.NET Core** là framework mã nguồn mở, đa nền tảng của Microsoft để xây dựng ứng dụng web, API, dịch vụ nền, ứng dụng thời gian thực… trên .NET. Chạy trên Windows, macOS, Linux, container; hiệu năng thuộc nhóm hàng đầu. Nó là **một** framework thống nhất với nhiều "kiểu ứng dụng" xây trên cùng nền:

| Kiểu | Dùng cho | Chương |
|------|---------|--------|
| **Minimal API** / **Web API (controller)** | API JSON cho web/mobile/dịch vụ khác | 4, 7 |
| **MVC** | web nhiều trang phía server (HTML) | 15 |
| **Razor Pages** | web trang-là-đơn-vị, đơn giản hơn MVC | 16 |
| **Blazor** | giao diện tương tác bằng **C#** (thay JavaScript) | 18 |
| **SignalR**, **gRPC**, **Worker Service** | thời gian thực, RPC, dịch vụ nền | Tập 3 |

Tất cả dùng chung: middleware pipeline, dependency injection, cấu hình, logging, xác thực — học một lần dùng mọi nơi.

## Tạo dự án

```
dotnet new list web                # xem các template web
dotnet new web -n WebDauTien       # ứng dụng rỗng tối thiểu
cd WebDauTien
dotnet run
```

Terminal hiển thị:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5102
```

Mở địa chỉ đó bằng trình duyệt: thấy `Hello World!`. Nhấn `Ctrl+C` để dừng.

| Template | Lệnh | Gồm |
|----------|------|-----|
| Rỗng | `dotnet new web` | chỉ `Program.cs` — nền tảng để học (dùng trong chương này) |
| Web API | `dotnet new webapi` | API mẫu + OpenAPI |
| MVC | `dotnet new mvc` | Controllers/Views/Models |
| Razor Pages | `dotnet new razor` | thư mục `Pages` |
| Blazor Web App | `dotnet new blazor` | components Razor |

Trong Visual Studio: **File → New → Project → ASP.NET Core Empty**. Chọn *"Configure for HTTPS"* và đừng bật *"Enable container support"* ở giai đoạn này.

## `Program.cs` — bốn bước

```csharp
var builder = WebApplication.CreateBuilder(args);   // (1) Cấu hình

// (2) Đăng ký dịch vụ vào DI container: builder.Services.AddXxx(...)

var app = builder.Build();                            // Dựng ứng dụng

// (3) Cấu hình middleware pipeline + endpoint: app.UseXxx(), app.MapGet(...)
app.MapGet("/", () => "Xin chao ASP.NET Core!");
app.MapGet("/chao/{ten}", (string ten) => $"Xin chao, {ten}!");
app.MapGet("/cong", (int a, int b) => new { a, b, tong = a + b });

app.Run();                                            // (4) Chạy máy chủ, chặn tới khi tắt
```

1. **`WebApplication.CreateBuilder(args)`**: tạo builder đã cấu hình sẵn: nạp `appsettings.json`, biến môi trường, tham số dòng lệnh, logging, Kestrel, DI. Bạn hầu như không cần tự cấu hình những thứ này.
2. **`builder.Services`**: đăng ký dịch vụ (Chương 5) — *trước* `Build()`; sau `Build()` không đăng ký thêm được.
3. **`app.Use…/Map…`**: xếp middleware theo thứ tự (Chương 3) và khai báo endpoint (Chương 4).
4. **`app.Run()`**: khởi động và **chặn** luồng hiện tại cho tới khi nhận tín hiệu tắt (Ctrl+C, SIGTERM).

Đây là **minimal hosting model**: không còn `Startup.cs` như ASP.NET Core 3.x/5 — một file duy nhất, nhờ *top-level statements* (Tập 1, Chương 3). Lambda `() => ...` là **route handler**; giá trị trả về được tự chuyển thành response: `string` → `text/plain`, object → **JSON**, `IResult` → tuỳ ý.

Kiểm tra các endpoint:

```
curl http://localhost:5102/
curl http://localhost:5102/chao/An
curl "http://localhost:5102/cong?a=2&b=3"      # {"a":2,"b":3,"tong":5}
```

Tham số handler được **tự động gắn (bind)**: `ten` lấy từ đường dẫn `{ten}`, `a`,`b` từ query string, kiểu `int` được chuyển đổi và kiểm tra (gửi `?a=abc` sẽ nhận `400`). Chương 4 sẽ nói kỹ.

## File `.csproj` và cấu trúc thư mục

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

Khác Tập 1: SDK là **`Microsoft.NET.Sdk.Web`** (thêm framework ASP.NET Core; `ImplicitUsings` thêm `Microsoft.AspNetCore.*`). Thư mục điển hình:

```
WebDauTien/
├── WebDauTien.csproj
├── Program.cs                     # điểm vào
├── appsettings.json               # cấu hình chung
├── appsettings.Development.json   # cấu hình riêng cho môi trường Development
├── Properties/launchSettings.json # cấu hình chạy khi phát triển (URL, biến môi trường)
├── wwwroot/                       # file tĩnh (css, js, ảnh) — nếu có UseStaticFiles
└── bin/  obj/                     # sinh ra khi build (không đưa vào Git)
```

## `launchSettings.json`, môi trường và HTTPS

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5102",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  }
}
```

- File này **chỉ dùng khi phát triển** (`dotnet run`, F5) — không được đóng gói khi publish.
- **`ASPNETCORE_ENVIRONMENT`**: `Development`, `Staging`, `Production` (mặc định `Production` nếu không đặt). Quyết định file `appsettings.{Env}.json` nào được nạp và hành vi như trang lỗi chi tiết:

  ```csharp
  if (app.Environment.IsDevelopment()) { /* công cụ chỉ cho dev */ }
  ```

- **HTTPS khi phát triển**: `dotnet dev-certs https --trust` (một lần) để trình duyệt tin chứng chỉ cục bộ; thêm profile `https` với `applicationUrl: "https://localhost:7102;http://localhost:5102"`. Production luôn dùng HTTPS thật (Chương 22 và Tập 3).
- Đổi URL không cần sửa file: `dotnet run --urls http://localhost:6000` hoặc biến `ASPNETCORE_URLS`.

## Kestrel

**Kestrel** là máy chủ web tích hợp sẵn, đa nền tảng, rất nhanh; nhận kết nối TCP, phân tích HTTP/1.1, HTTP/2, HTTP/3 rồi chuyển vào pipeline của bạn. Khi triển khai thật thường đặt **sau một reverse proxy** (Nginx, IIS, YARP, load balancer đám mây, ingress Kubernetes) để lo TLS, nén, cân bằng tải (Tập 3). Không cần cài IIS/Apache khi phát triển.

## Hot reload — sửa code không cần khởi động lại

```
dotnet watch run
```

Lưu file → ứng dụng tự nạp lại (nhiều thay đổi được áp dụng **ngay khi đang chạy** — hot reload; thay đổi lớn thì tự khởi động lại). Trong Visual Studio dùng **Hot Reload** (🔥) khi F5. Tiết kiệm rất nhiều thời gian thử–sửa.

## Một endpoint thử nghiệm hữu ích

```csharp
app.MapGet("/moi-truong", (IWebHostEnvironment env) => new
{
    env.EnvironmentName,
    env.ApplicationName,
    Cwd = Environment.CurrentDirectory,
});
```

Tham số `IWebHostEnvironment` được **tiêm** (Dependency Injection) vào handler — bạn khai báo cần gì, framework đưa cho. Đây là ý tưởng trung tâm của ASP.NET Core, Chương 5 sẽ đi sâu.

## Lỗi thường gặp

- `Address already in use` — cổng đang bị chương trình khác/tiến trình cũ giữ; đổi `--urls` hoặc tắt tiến trình.
- Trình duyệt báo chứng chỉ không tin cậy: chạy `dotnet dev-certs https --trust`.
- Sửa code mà kết quả không đổi: đang chạy bản cũ; dừng rồi chạy lại hoặc dùng `dotnet watch`.
- `System.InvalidOperationException: Cannot modify ServiceCollection after application started` — gọi `builder.Services.Add...` sau `Build()`.
- Nhầm Development/Production: trang lỗi chi tiết chỉ hiện ở Development (an toàn — không lộ stack trace ra ngoài).
- Commit thư mục `bin/`, `obj/`, hoặc `launchSettings.json` chứa bí mật.

## Bài tập

1. Tạo dự án `dotnet new web`, thêm ba endpoint: `/gio`, `/nhan-doi/{so:int}`, `/chao?ten=...`. Thử bằng `curl`.
2. Thêm endpoint `/phien-ban` trả về `Environment.Version` và `RuntimeInformation.OSDescription`.
3. Chạy bằng `dotnet run --urls http://localhost:7000` rồi đặt `ASPNETCORE_ENVIRONMENT=Production` và so sánh `/moi-truong`.
4. Bật `dotnet watch`, sửa chuỗi trả về và quan sát ứng dụng tự cập nhật.
5. Tạo thêm dự án `dotnet new webapi` và so sánh `Program.cs` của nó với dự án rỗng: template đã thêm những gì?
