# Chương 6 — Cấu hình, Options và Logging

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu cách ASP.NET Core **nạp cấu hình** từ nhiều nguồn và thứ tự ưu tiên.
- Dùng **Options pattern** (`IOptions`, `IOptionsSnapshot`, `IOptionsMonitor`) và **kiểm tra cấu hình lúc khởi động**.
- Cấu hình theo **môi trường**, giữ **bí mật** đúng cách.
- Ghi log có cấu trúc, cấp độ, scope, **source-generated logging**.

Code mẫu: [`code/ch06-cau-hinh-logging/`](../../code/ch06-cau-hinh-logging/).

Chương này áp dụng vào web những gì Tập 1 (Chương 39–40) đã giới thiệu — `WebApplication.CreateBuilder` đã **cấu hình sẵn** phần lớn cho bạn.

## Nguồn cấu hình mặc định

`WebApplication.CreateBuilder(args)` nạp theo thứ tự (**nguồn sau ghi đè nguồn trước**):

1. `appsettings.json`
2. `appsettings.{Environment}.json` (vd `appsettings.Development.json`)
3. **User Secrets** (chỉ ở `Development`)
4. **Biến môi trường**
5. **Tham số dòng lệnh**

Hợp nhất thành một cây khoá–giá trị (`builder.Configuration`, kiểu `IConfiguration`). Khoá lồng nhau dùng dấu `:`; biến môi trường dùng `__`:

```
CuaHang:Ten                     # trong JSON
CuaHang__Ten="Sach Do"          # biến môi trường
--CuaHang:Ten="Sach Xanh"       # dòng lệnh
```

`appsettings.json` của code mẫu:

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "CuaHang": { "Ten": "Sach Xanh", "ThueVat": 0.08, "PhiVanChuyenToiThieu": 15000, "PhiVanChuyenToiDa": 60000 },
  "ConnectionStrings": { "Mac_Dinh": "Data Source=app.db" }
}
```

`appsettings.Development.json` chỉ ghi **phần khác biệt** (đổi tên cửa hàng thành "(DEV)", log `Debug`).

## Đọc cấu hình

```csharp
builder.Configuration["CuaHang:Ten"]                          // chuỗi thô
builder.Configuration.GetValue<int>("CuaHang:SoNgay", 7)      // có kiểu + mặc định
builder.Configuration.GetConnectionString("Mac_Dinh")         // "ConnectionStrings:Mac_Dinh"
builder.Configuration.GetSection("CuaHang").Get<CuaHangOptions>()
```

Đọc chuỗi khoá rải rác rất dễ sai chính tả (trả `null` mà không báo lỗi). **Ưu tiên Options pattern.**

## Options pattern

Gom cấu hình liên quan vào một class, gắn vào một mục, tiêm vào nơi cần:

```csharp
class CuaHangOptions
{
    public const string TenMuc = "CuaHang";

    [Required, MinLength(2)] public string Ten { get; set; } = "";
    [Range(0, 1)] public decimal ThueVat { get; set; }
    [Range(0, 1_000_000)] public decimal PhiVanChuyenToiThieu { get; set; }
    [Range(0, 1_000_000)] public decimal PhiVanChuyenToiDa { get; set; }
    public List<string> KenhHoTro { get; set; } = [];
}

builder.Services
    .AddOptions<CuaHangOptions>()
    .Bind(builder.Configuration.GetSection(CuaHangOptions.TenMuc))
    .ValidateDataAnnotations()                                                      // kiểm tra [Required], [Range]...
    .Validate(o => o.PhiVanChuyenToiThieu <= o.PhiVanChuyenToiDa, "Phi toi thieu khong duoc lon hon phi toi da")
    .ValidateOnStart();                                                              // kiểm tra NGAY khi khởi động

app.MapGet("/cau-hinh", (IOptions<CuaHangOptions> opt) => opt.Value);
```

`ValidateOnStart()` là chi tiết rất đáng giá: thử chạy với `CuaHang__ThueVat=5`, ứng dụng **từ chối khởi động** với thông báo rõ ràng:

```
OptionsValidationException: DataAnnotation validation failed for 'CuaHangOptions' members: 'ThueVat' with the error: 'The field ThueVat must be between 0 and 1.'
```

Sự cố cấu hình lộ ra **lúc deploy**, không phải lúc một khách hàng đầu tiên gọi vào tính năng đó lúc 3 giờ sáng ("fail fast").

### Ba dạng `IOptions`

| Kiểu | Vòng đời | Nạp lại khi file đổi? | Dùng khi |
|------|---------|-----------------------|----------|
| `IOptions<T>` | Singleton | **Không** (đọc một lần) | cấu hình cố định suốt đời ứng dụng |
| `IOptionsSnapshot<T>` | Scoped | Có, **mỗi request** đọc lại | cấu hình đổi được, cần ổn định trong một request |
| `IOptionsMonitor<T>` | Singleton | Có, **theo dõi thay đổi** (`OnChange`) | dịch vụ Singleton cần giá trị mới nhất |

`appsettings.json` được nạp với `reloadOnChange: true`, nên đổi file khi đang chạy → `IOptionsSnapshot/IOptionsMonitor` thấy giá trị mới (ví dụ: bật/tắt cờ tính năng mà không khởi động lại).

## Môi trường

```csharp
app.Environment.IsDevelopment();   IsStaging();   IsProduction();   EnvironmentName
```

Đặt bằng `ASPNETCORE_ENVIRONMENT`. Dùng `if (env.IsDevelopment())` cho những thứ **chỉ dev** (trang lỗi chi tiết, OpenAPI/Swagger UI, seed dữ liệu). Đừng dùng môi trường làm "công tắc nghiệp vụ" — khác biệt hành vi nghiệp vụ nên là **cấu hình**, không phải rẽ nhánh theo tên môi trường.

## Bí mật (secrets)

Nguyên tắc từ Tập 1, Chương 40, giờ áp dụng vào web:

- **Không** để mật khẩu/khoá API/chuỗi kết nối có mật khẩu trong `appsettings.json` được commit.
- **Dev**: `dotnet user-secrets` (lưu ngoài repo).
- **Production**: biến môi trường do nền tảng cấp, hoặc kho bí mật (Azure Key Vault, AWS Secrets Manager, Docker/Kubernetes secrets).
- Không log giá trị bí mật; không trả chúng qua endpoint (code mẫu chỉ in "đã cấu hình" thay vì nội dung chuỗi kết nối).

## Logging

`ILogger<T>` được tiêm sẵn; `T` (thường là chính lớp đang dùng) trở thành **category** của dòng log:

```csharp
app.MapGet("/log", (ILogger<Program> log, string? nguoiDung) =>
{
    log.LogDebug("Debug: nguoi dung {NguoiDung}", nguoiDung);
    log.LogInformation("Information: nguoi dung {NguoiDung} vua goi /log", nguoiDung ?? "an danh");
    log.LogWarning("Warning: canh bao mau");
    log.LogError(new InvalidOperationException("loi mau"), "Error: mot loi minh hoa");
    return "ok";
});
```

Kết quả console (ở `Development`, mức `Debug`):

```
dbug: Program[0]
      Debug: nguoi dung an
info: Program[0]
      Information: nguoi dung an vua goi /log
warn: Program[0]
      Warning: canh bao mau
fail: Program[0]
      Error: mot loi minh hoa
      System.InvalidOperationException: loi mau
```

Nguyên tắc (nhắc lại Tập 1, Chương 39): dùng **tham số đặt tên** `{NguoiDung}` chứ không nội suy `$"..."`; truyền `Exception` làm tham số **đầu** của `LogError`; không log dữ liệu nhạy cảm.

### Cấp độ và lọc theo category

Cấu hình trong `appsettings.json`, **không cần sửa code**:

```json
"Logging": { "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",                  // framework "ồn ào": chỉ Warning trở lên
    "Microsoft.EntityFrameworkCore.Database.Command": "Information",   // xem câu SQL
    "MyApp.DonHang": "Debug"
} }
```

Đặt thấp hơn cho vùng đang điều tra, cao hơn cho vùng ồn — giảm log rác trên production.

### Scope

```csharp
using (log.BeginScope("CorrelationId={CorrelationId}", id))
{
    log.LogInformation("Bat dau");    // mọi dòng trong khối đều kèm CorrelationId
}
```

Console mặc định **không hiển thị scope**; bật bằng `builder.Logging.AddSimpleConsole(o => o.IncludeScopes = true)`. Framework tự tạo scope cho mỗi request (có `RequestId`, `TraceId`) — công cụ log tập trung dùng để nối các dòng thuộc cùng một request.

### Source-generated logging

```csharp
static partial class NhatKy
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Dat hang thanh cong: don {SoDon}, tong {TongTien}")]
    public static partial void DatHangThanhCong(this ILogger logger, int soDon, decimal tongTien);
}

log.DatHangThanhCong(42, 250_000m);
```

Trình sinh mã tạo phương thức: **nhanh hơn** (không cấp phát/boxing khi mức log tắt), **an toàn kiểu** (tham số sai là lỗi biên dịch), mẫu thông điệp tập trung. Khuyến nghị cho log ở đường nóng và cho log nghiệp vụ quan trọng.

### Log ra đâu?

Mặc định: console, debug, event source. Cho production dùng **Serilog** hoặc NLog (cắm vào `ILogger`, ghi file/Seq/Elasticsearch) hoặc **OpenTelemetry** để gửi log/trace/metric tới hệ thống quan sát (Tập 3). Trong container, cứ ghi ra **console (stdout)** — nền tảng sẽ thu thập.

## Lỗi thường gặp

- Cấu hình đọc ra `null`/mặc định vì gõ sai khoá hoặc sai cấp lồng — dùng Options + `ValidateOnStart`.
- Tưởng `IOptions<T>` tự nạp lại khi file đổi (phải `IOptionsSnapshot`/`IOptionsMonitor`).
- Dùng `IOptionsSnapshot` (Scoped) trong dịch vụ Singleton → lỗi captive dependency.
- Commit `appsettings.Production.json` có mật khẩu.
- Biến môi trường dùng `:` thay `__` (một số shell/OS không cho `:`).
- Log bằng nội suy chuỗi, log mật khẩu/token, log mỗi vòng lặp ở mức `Information`.
- Để `Default: Debug/Trace` trên production → tràn dung lượng, lộ dữ liệu.

## Bài tập

1. Thêm mục `Email` (`MayChu`, `Cong`, `TenDangNhap`) vào cấu hình, bind vào `EmailOptions` với kiểm tra `[Required]`, `[Range(1, 65535)]` và `ValidateOnStart`.
2. Ghi đè `CuaHang:Ten` bằng biến môi trường và bằng tham số dòng lệnh; xác nhận thứ tự ưu tiên.
3. Đặt `Microsoft.AspNetCore` ở `Information` và xem log của từng request; sau đó đặt lại `Warning`.
4. Viết `[LoggerMessage]` cho ba sự kiện nghiệp vụ (`DonHangTao`, `KhoSapHet`, `ThanhToanLoi`) với `EventId` khác nhau.
5. Dùng `IOptionsMonitor<T>.OnChange` để in log mỗi khi bạn sửa `appsettings.json` khi ứng dụng đang chạy.
