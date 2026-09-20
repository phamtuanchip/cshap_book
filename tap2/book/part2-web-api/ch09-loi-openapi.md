# Chương 9 — Xử lý lỗi, versioning và OpenAPI

## Mục tiêu học

Sau chương này, bạn sẽ:

- Xử lý lỗi **tập trung** bằng `IExceptionHandler` và `ProblemDetails`, ánh xạ exception nghiệp vụ → mã HTTP.
- Thiết kế **versioning** cho API để đổi hợp đồng mà không làm vỡ client cũ.
- Sinh và tuỳ biến tài liệu **OpenAPI**, dùng giao diện **Scalar** để thử API.
- Bật **CORS** đúng cách và **health checks**.

Code mẫu: [`code/ch09-loi-openapi/`](../../code/ch09-loi-openapi/).

## Xử lý lỗi tập trung

Nếu mỗi action tự `try/catch` và tự dựng response lỗi, code lặp, định dạng lỗi mỗi nơi một kiểu, dễ lộ chi tiết nội bộ. Giải pháp: để exception **nổi lên** và có **một nơi duy nhất** biến nó thành response.

### 1. Exception nghiệp vụ có ý nghĩa

```csharp
public abstract class LoiNghiepVu(string message) : Exception(message);
public class KhongTimThayException(string loai, object khoa) : LoiNghiepVu($"Khong tim thay {loai} '{khoa}'");
public class XungDotException(string message) : LoiNghiepVu(message);

// Trong dịch vụ / endpoint:
var sp = kho.Tim(id) ?? throw new KhongTimThayException("San pham", id);
```

Tầng nghiệp vụ nói bằng ngôn ngữ **nghiệp vụ** (không thấy HTTP); tầng web dịch sang mã HTTP.

### 2. `IExceptionHandler` (.NET 8+)

```csharp
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception e, CancellationToken ct)
    {
        var (status, title) = e switch
        {
            KhongTimThayException => (404, "Khong tim thay"),
            XungDotException => (409, "Xung dot du lieu"),
            OperationCanceledException => (499, "Client huy yeu cau"),
            _ => (500, "Loi may chu"),
        };

        if (status >= 500) log.LogError(e, "Loi khong xu ly tai {Path}", ctx.Request.Path);
        else log.LogWarning("Loi nghiep vu {Loai}: {Message}", e.GetType().Name, e.Message);

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            Exception = e,
            ProblemDetails = new ProblemDetails
            {
                Status = status, Title = title,
                Detail = status < 500 ? e.Message : null,        // KHÔNG lộ chi tiết lỗi 500
                Instance = ctx.Request.Path,
            },
        });
    }
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
app.UseExceptionHandler();          // sớm nhất trong pipeline (Chương 3)
app.UseStatusCodePages();           // 404/405 không body → cũng dạng ProblemDetails
```

Kết quả thực tế khi chạy code mẫu:

```
GET /loi/khong-tim-thay   →  404  application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Khong tim thay","status":404,
 "detail":"Khong tim thay San pham '999'","instance":"/loi/khong-tim-thay","traceId":"00-a16ba7f..."}

GET /loi/bat-ngo          →  500  (KHÔNG có detail — chi tiết chỉ nằm trong log)
{"type":"...#section-15.6.1","title":"Loi may chu","status":500,"instance":"/loi/bat-ngo","traceId":"00-69f22e..."}
```

Điểm mấu chốt:

- **Lỗi nghiệp vụ (4xx)**: hiển thị thông điệp cho client (họ cần biết để sửa), log mức `Warning` không kèm stack trace.
- **Lỗi bất ngờ (5xx)**: **không** trả `e.Message`/stack trace (lộ cấu trúc nội bộ, câu SQL, đường dẫn máy chủ — thông tin có giá trị cho kẻ tấn công); trả thông điệp chung + `traceId` để người dùng báo lại và bạn tra log. Log `Error` đầy đủ.
- Mọi lỗi cùng **một định dạng** (ProblemDetails) — client xử lý thống nhất.
- Trong `Development` `UseDeveloperExceptionPage` (mặc định) hiện trang lỗi chi tiết cho lập trình viên; production dùng handler trên.

Khi hai dịch vụ gọi nhau, `traceId` cho phép lần theo chuỗi request xuyên nhiều hệ thống (Tập 3: OpenTelemetry).

### Lỗi vs Result

Không phải mọi kết quả "thất bại" đều là ngoại lệ. Lỗi **dự kiến, thường xuyên** ("không đủ hàng") có thể trả về như **giá trị** (`Result`, `Conflict<ProblemDetails>`, như endpoint `/mua`), tránh chi phí và độ ồn của exception. Ngoại lệ dành cho **bất thường** hoặc khi nhiều tầng cần "bỏ dở". Tập 3 nói kỹ Result pattern.

## Versioning

API công khai là **hợp đồng**: khi client đã dùng, bạn không được tuỳ tiện đổi tên trường, xoá trường, đổi kiểu. Khi cần thay đổi phá vỡ tương thích (**breaking change**), phát hành **phiên bản mới** và giữ phiên bản cũ đủ lâu.

Ba cách phổ biến:

| Cách | Ví dụ | Ghi chú |
|------|-------|---------|
| **Đường dẫn** | `/api/v1/san-pham`, `/api/v2/san-pham` | rõ ràng, dễ cache/test, **phổ biến nhất** |
| **Query** | `/api/san-pham?api-version=2` | dễ dùng thử trên trình duyệt |
| **Header** | `X-Api-Version: 2` hoặc media type (`Accept: application/vnd.x.v2+json`) | URL sạch, khó thử |

Code mẫu dùng đường dẫn với `MapGroup`:

```csharp
var v1 = app.MapGroup("/api/v1/san-pham");
var v2 = app.MapGroup("/api/v2/san-pham");

v1.MapGet("/{id:int}", ...);   // {"id":1,"ten":"Laptop","gia":18000000}
v2.MapGet("/{id:int}", ...);   // {"id":1,"ten":"Laptop","gia":{"soTien":18000000,"donVi":"VND"},"tonKho":3}
```

v2 đổi `gia` từ số sang đối tượng (thêm đơn vị tiền) — nếu đổi trực tiếp, mọi client v1 vỡ. Cho dự án lớn dùng thư viện **`Asp.Versioning`** (versioning theo attribute, tự đưa vào OpenAPI, báo phiên bản lỗi thời qua header `api-supported-versions`, `Deprecation`/`Sunset`).

Quy tắc: **thay đổi tương thích thì không cần version mới** — thêm trường tuỳ chọn, thêm endpoint. **Phá vỡ** — xoá/đổi tên/đổi kiểu trường, đổi ý nghĩa, thêm tham số bắt buộc — mới cần. Thiết kế client theo "tolerant reader" (bỏ qua trường lạ) để dễ tiến hoá.

## OpenAPI

**OpenAPI** (trước kia Swagger) là chuẩn mô tả API dạng JSON/YAML: mọi endpoint, tham số, body, phản hồi, xác thực. Từ .NET 9, ASP.NET Core sinh sẵn bằng `Microsoft.AspNetCore.OpenApi`:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Info = new OpenApiInfo { Title = "API Cua Hang", Version = "v1", Description = "..." };
        return Task.CompletedTask;
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // GET /openapi/v1.json
    app.MapScalarApiReference();      // giao diện /scalar/v1  (package Scalar.AspNetCore)
}
```

Thông tin lấy từ code: route, kiểu tham số, kiểu trả về khai báo bằng `Results<...>`/`[ProducesResponseType]`, XML doc comment (bật `GenerateDocumentationFile`), attribute `[Tags]`, `WithSummary`, `WithDescription`.

Giá trị của tài liệu OpenAPI:

- **Giao diện thử API** (Scalar, Swagger UI, Postman nhập file) — người dùng API không cần đọc code.
- **Sinh code client** (C#, TypeScript, Java...) bằng NSwag/Kiota/openapi-generator — tránh viết tay DTO phía client.
- **Kiểm thử hợp đồng** và phát hiện breaking change trong CI.

Đặt `MapOpenApi`/UI ở `Development` hoặc sau xác thực khi production (không muốn công khai bản đồ API nội bộ).

## CORS

Trình duyệt chặn trang ở origin A gọi API ở origin B (**same-origin policy**) trừ khi server B cho phép bằng header CORS. `curl` và server-to-server **không bị** ảnh hưởng — nên lỗi CORS chỉ hiện trong trình duyệt.

```csharp
builder.Services.AddCors(o => o.AddPolicy("web-app", p => p
    .WithOrigins("https://cuahang.example.com", "http://localhost:3000")
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .AllowAnyHeader()));

app.UseCors("web-app");      // sau UseRouting, TRƯỚC UseAuthentication/UseAuthorization
```

Thử: request có `Origin: http://localhost:3000` nhận `Access-Control-Allow-Origin: http://localhost:3000`; `Origin: http://evil.com` **không** nhận header nào. Nguyên tắc: liệt kê **chính xác** origin được phép; **không** dùng `AllowAnyOrigin()` cùng `AllowCredentials()` (bị chặn và nguy hiểm). CORS là cơ chế của **trình duyệt bảo vệ người dùng**, không phải cơ chế bảo mật API (kẻ tấn công vẫn gọi được bằng công cụ khác — xác thực mới là cái bảo vệ).

## Health checks

Endpoint cho hệ thống giám sát/orchestrator (Docker, Kubernetes, load balancer) hỏi "ứng dụng còn khoẻ không?":

```csharp
builder.Services.AddHealthChecks().AddCheck("bo-nho", () => GC.GetTotalMemory(false) < 500_000_000
    ? HealthCheckResult.Healthy("Bo nho on dinh") : HealthCheckResult.Degraded("Bo nho cao"));
app.MapHealthChecks("/health");        // 200 "Healthy" / 503 "Unhealthy"
```

Thực tế thêm check CSDL, dịch vụ ngoài (`AspNetCore.HealthChecks.*`). Tách **liveness** (tiến trình còn sống) và **readiness** (đã sẵn sàng nhận tải — phụ thuộc kết nối được) bằng `Tags`/nhiều endpoint (Tập 3).

## Lỗi thường gặp

- Quên `app.UseExceptionHandler()` (đăng ký handler nhưng không bật middleware).
- Đặt middleware xử lý lỗi **sau** middleware khác nên không bắt được lỗi của chúng.
- Trả `e.Message`/stack trace ra client ở production.
- Bắt `Exception` chung chung trong từng action rồi trả `500` với thông điệp riêng lẻ.
- Đổi hợp đồng API ngay trên phiên bản đang dùng.
- Lỗi CORS trong trình duyệt nhưng `curl` chạy tốt — thiếu origin trong policy, hoặc `UseCors` đặt sai thứ tự.
- Công khai OpenAPI/Scalar trên production ngoài ý muốn.
- Health check gọi thao tác nặng → chính nó làm sập ứng dụng.

## Bài tập

1. Thêm `LoiDuLieuKhongHopLeException` → `422 Unprocessable Entity` vào `GlobalExceptionHandler`.
2. Thêm `TimeoutException` → `504 Gateway Timeout` với thông điệp chung.
3. Tạo `/api/v3/san-pham/{id}` thêm `hinhAnh` (tương thích) và giải thích vì sao không cần phiên bản mới nếu chỉ *thêm* trường.
4. Dùng `WithSummary/WithDescription` mô tả các endpoint; mở `/scalar/v1` và kiểm tra.
5. Thêm health check kiểm tra ghi được vào thư mục tạm; thử làm nó thất bại và quan sát `503`.
