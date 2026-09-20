# Chương 21 — Bảo mật ứng dụng web

## Mục tiêu học

Sau chương này, bạn sẽ:

- Nắm các lỗ hổng phổ biến nhất (**OWASP Top 10**) và cách ASP.NET Core giúp phòng: **XSS, CSRF, SQL injection, IDOR, open redirect, SSRF**.
- Cấu hình **HTTPS/HSTS**, **header bảo mật**, **CSP**, **rate limiting**.
- Dùng **Data Protection** để ký/mã hoá dữ liệu ngắn hạn; xử lý bí mật, log và lỗi an toàn.
- Có **danh sách kiểm tra bảo mật** trước khi triển khai.

Code mẫu: [`code/ch21-bao-mat-web/`](../../code/ch21-bao-mat-web/) — mỗi lỗ hổng có một endpoint "sai" và "đúng" để bạn thử.

Bảo mật không phải một tính năng thêm vào cuối, mà là **thói quen ở từng dòng code**. Nguyên tắc nền: *đừng tin dữ liệu từ bên ngoài, giảm tối thiểu quyền, phòng thủ nhiều lớp, thất bại một cách an toàn.*

## OWASP Top 10 — bản đồ rủi ro

**OWASP** là tổ chức công bố danh sách 10 rủi ro bảo mật web phổ biến nhất (cập nhật định kỳ). Ánh xạ với nội dung sách:

| Rủi ro (rút gọn) | Ví dụ | Phòng ở đâu |
|------------------|-------|-------------|
| **Broken Access Control** (IDOR, thiếu kiểm tra quyền) | đổi `/don-hang/2` để xem đơn người khác | Ch. 20, mục IDOR dưới đây |
| **Cryptographic Failures** | lưu mật khẩu thô, dùng HTTP, khoá yếu | Ch. 17, 20; HTTPS/Data Protection |
| **Injection** (SQL, lệnh, XSS) | nối chuỗi vào SQL/HTML | Ch. 11, 13; XSS dưới đây |
| **Insecure Design** | thiếu giới hạn tốc độ, luồng nghiệp vụ hở | rate limiting, thiết kế |
| **Security Misconfiguration** | trang lỗi lộ stack trace, header thiếu, CORS quá rộng | Ch. 9; header |
| **Vulnerable Components** | thư viện NuGet có lỗ hổng | `dotnet list package --vulnerable` |
| **Authentication Failures** | brute-force, phiên yếu | Ch. 20; rate limit |
| **Software & Data Integrity** | deserialize dữ liệu lạ, CI/CD không an toàn | Tập 1 Ch. 37; Tập 3 |
| **Logging & Monitoring Failures** | không ghi/không theo dõi sự kiện bảo mật | Ch. 6, Tập 3 |
| **SSRF** | ép server gọi địa chỉ nội bộ | Ch. 19, dưới đây |

## HTTPS và HSTS

**HTTPS** mã hoá đường truyền (TLS): chặn nghe lén, sửa đổi giữa đường, xác thực máy chủ. **Bắt buộc** cho mọi ứng dụng có đăng nhập/dữ liệu người dùng (trình duyệt còn chặn nhiều tính năng nếu không có HTTPS).

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();                 // Strict-Transport-Security: trình duyệt CHỈ dùng HTTPS với tên miền này (mặc định 30 ngày)
    app.UseHttpsRedirection();     // 80 → 443
}
```

Chứng chỉ thật lấy từ CA (Let's Encrypt miễn phí) — thường cấu hình ở reverse proxy/load balancer (Tập 3). Đặt cookie `Secure`. Khi ứng dụng chạy **sau proxy** (Nginx, load balancer) hãy bật `UseForwardedHeaders` với **`KnownProxies`/`KnownNetworks`** rõ ràng, để biết IP thật/giao thức gốc — và *không* tin `X-Forwarded-For` từ nguồn lạ (ai cũng giả mạo được):

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownProxies.Add(IPAddress.Loopback);
});
app.UseForwardedHeaders();       // đặt SỚM
```

## Header bảo mật

```csharp
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    h["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; object-src 'none'; frame-ancestors 'none'";
    await next();
});
```

| Header | Chống |
|--------|------|
| `X-Content-Type-Options: nosniff` | trình duyệt "đoán" MIME (file tải lên bị thực thi như script) |
| `X-Frame-Options: DENY` / CSP `frame-ancestors` | **clickjacking** (trang khác nhúng trang bạn vào iframe ẩn để lừa bấm) |
| `Referrer-Policy` | rò rỉ URL nội bộ qua header `Referer` |
| `Permissions-Policy` | tính năng trình duyệt không cần (camera, mic) |
| **`Content-Security-Policy`** | **XSS**: chỉ cho phép tải script/style/ảnh từ nguồn liệt kê; script nội tuyến (`<script>alert(1)</script>`) bị chặn dù kẻ tấn công chèn được vào HTML |
| `Strict-Transport-Security` | hạ cấp về HTTP |

CSP là lớp phòng thủ mạnh nhưng cần chỉnh cẩn thận với ứng dụng dùng script/CDN (dùng `nonce` cho script nội tuyến; Blazor cần nới tối thiểu). Công cụ đánh giá: securityheaders.com, Mozilla Observatory. Trong thực tế đóng gói thành middleware/thư viện (như `NetEscapades.AspNetCore.SecurityHeaders`).

## XSS — Cross-Site Scripting

Kẻ tấn công cài `<script>` vào nội dung (bình luận, tên, tham số URL) → trình duyệt nạn nhân chạy nó dưới danh nghĩa trang của bạn → đánh cắp phiên, thực hiện hành động thay người dùng.

```csharp
// ✘ Nối chuỗi thẳng vào HTML
app.MapGet("/xss-sai", (string ten) => Results.Content($"<h1>Xin chao {ten}</h1>", "text/html"));
// ?ten=<script>alert(1)</script>  →  <h1>Xin chao <script>alert(1)</script></h1>   ← SCRIPT CHẠY

// ✔ Mã hoá đầu ra
app.MapGet("/xss-dung", (string ten) => Results.Content($"<h1>Xin chao {HtmlEncoder.Default.Encode(ten)}</h1>", "text/html"));
//  →  <h1>Xin chao &lt;script&gt;alert(1)&lt;/script&gt;</h1>                          ← chỉ là văn bản
```

Cả hai kết quả trên lấy từ lần chạy thật. Nguyên tắc: **mã hoá theo ngữ cảnh khi xuất** (HTML, thuộc tính, JavaScript, URL). Razor `@giaTri`, Blazor và tag helper **tự mã hoá HTML**, nên XSS xảy ra khi bạn:

- Dùng `@Html.Raw(...)` / `MarkupString` với dữ liệu người dùng.
- Tự nối chuỗi HTML như trên (Minimal API trả `text/html`).
- Nhúng dữ liệu vào `<script>` hoặc thuộc tính `onclick`/`href="javascript:..."`.
- Nhận HTML "giàu định dạng" (editor) — phải **lọc bằng thư viện** (HtmlSanitizer) theo whitelist thẻ.

Kèm **CSP** và cookie `HttpOnly` làm lớp phòng thủ thứ hai.

## CSRF

Đã học ở Chương 17: antiforgery token cho form/cookie, `SameSite`, `POST` cho thay đổi. API dùng Bearer token không cần antiforgery.

## SQL injection

Chương 11 và 13: **luôn tham số hoá**; EF Core LINQ và `FromSqlInterpolated` an toàn; **không** `FromSqlRaw` với chuỗi nối; tên cột/bảng động → whitelist.

## IDOR — Insecure Direct Object Reference

Nếu API nhận `id` từ URL và chỉ kiểm tra người dùng *đã đăng nhập*, ai cũng đổi `id` để xem dữ liệu người khác. Đây là lỗi **số 1** OWASP (Broken Access Control):

```csharp
app.MapGet("/don-hang/{id:int}", (int id, HttpContext ctx, DonHangKho kho) =>
{
    string nguoiDung = ...;                        // danh tính hiện tại
    var don = kho.Lay(id);
    if (don is null) return Results.NotFound();
    if (don.Chu != nguoiDung) return Results.NotFound();     // KHÔNG PHẢI CHỦ → 404 (không phải 403)
    return Results.Ok(don);
});
```

- **Luôn lọc theo chủ sở hữu ngay trong truy vấn** (`Where(d => d.KhachHangId == userId)`) hoặc dùng resource-based authorization (Chương 20).
- Trả **`404`** thay vì `403` để không tiết lộ đối tượng có tồn tại.
- ID khó đoán (GUID) **giảm** nhưng **không thay thế** kiểm tra quyền.

Thử: `An` xem `/don-hang/1` → 200; `An` xem `/don-hang/2` (của Bình) → 404.

## Open redirect

`/dang-nhap?returnUrl=https://evil.com` — sau đăng nhập, chuyển người dùng sang trang lừa đảo trông như xuất phát từ trang uy tín. Chỉ chuyển hướng tới **đường dẫn nội bộ**:

```csharp
static bool LaUrlNoiBo(string url)
    => !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));

Results.LocalRedirect(url);       // framework cũng ném lỗi nếu không phải URL cục bộ; MVC/Razor Pages có Url.IsLocalUrl(url)
```

Chú ý các biến thể `//evil.com` và `/\evil.com` (trình duyệt hiểu là tên miền khác). Kết quả thử: `/don-hang/1` → `302`; `https://evil.com`, `//evil.com`, `/\evil.com` → `400`.

## SSRF — Server-Side Request Forgery

Nếu server nhận **URL do người dùng cung cấp** rồi tự gọi (xem trước liên kết, webhook, tải ảnh từ URL), kẻ tấn công trỏ tới `http://169.254.169.254/` (metadata đám mây → lấy khoá truy cập), `http://localhost:8080/admin`, mạng nội bộ. Phòng: **whitelist host** cho phép, chặn dải IP nội bộ/loopback/link-local sau khi phân giải DNS, không theo redirect tự do, đặt timeout/giới hạn kích thước, chạy ở mạng cách ly. Đừng tin kiểm tra chỉ trên chuỗi URL.

## Rate limiting

Chống brute-force mật khẩu, cào dữ liệu, làm dụng API, DoS nhẹ. Từ .NET 7 có sẵn middleware:

```csharp
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "khong-ro",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromSeconds(10) }));

    o.AddPolicy("dang-nhap", ctx => RateLimitPartition.GetFixedWindowLimiter(ip,
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromSeconds(30) }));
});
app.UseRateLimiter();
app.MapPost("/dang-nhap", ...).RequireRateLimiting("dang-nhap");
```

Kết quả thử:

```
POST /dang-nhap ×5  →  401 401 401 429 429          (chính sách "dang-nhap": 3 lần / 30 giây)
GET /  ×25 liên tiếp →  200 ×20 rồi 429 ×5           (giới hạn chung 20 / 10 giây / IP)
```

Bốn thuật toán: **Fixed window**, **Sliding window**, **Token bucket** (cho phép bùng nổ ngắn), **Concurrency** (giới hạn đồng thời). Chia theo **IP**, **người dùng**, **API key**. Khi sau proxy phải có `UseForwardedHeaders` đúng, nếu không mọi request có cùng IP proxy. Với hệ thống nhiều instance dùng bộ đếm phân tán (Redis) hoặc rate limit ở API gateway/WAF. Phản hồi `429` kèm `Retry-After`.

## Data Protection — ký và mã hoá dữ liệu ngắn hạn

API `IDataProtector` dùng để bảo vệ dữ liệu bạn cần gửi ra ngoài rồi nhận lại: token kích hoạt/đặt lại mật khẩu, tham số trong URL, nội dung cookie (chính cookie xác thực dùng nó):

```csharp
var bv = dp.CreateProtector("kich-hoat-tai-khoan").ToTimeLimitedDataProtector();
string token = bv.Protect("user:42", lifetime: TimeSpan.FromMinutes(10));   // mã hoá + ký + hạn dùng
string goc = bv.Unprotect(token);                                            // ném CryptographicException nếu sai/hết hạn/bị sửa
```

Thử: token hợp lệ → `{"noiDung":"user:42"}`; sửa thêm 1 ký tự → "Token không hợp lệ hoặc hết hạn". Lưu ý: **purpose string** ("kich-hoat-tai-khoan") tách biệt miền sử dụng (token kích hoạt không dùng được làm token đặt lại mật khẩu). **Khoá** lưu ở đâu: mặc định thư mục hồ sơ người dùng — với nhiều instance/container phải dùng kho chung (`PersistKeysToFileSystem`, Redis, Azure Blob + `ProtectKeysWithAzureKeyVault`), nếu không mỗi instance có khoá riêng và token của instance này không giải mã được ở instance kia.

## Bí mật, cấu hình, log, lỗi

- **Bí mật** (mật khẩu CSDL, khoá API, khoá ký): kho bí mật/biến môi trường, **không** commit (Chương 6). Đổi ngay nếu lỡ lộ.
- **Trang lỗi**: production không hiển thị stack trace (Chương 9); `UseDeveloperExceptionPage` chỉ ở Development.
- **Log**: không ghi mật khẩu/token/số thẻ; dùng tham số đặt tên để **chống log forging** (`log.LogInformation("Nguoi dung {Ten}", ten)` — giá trị chứa `\n` không tạo ra dòng log giả).
- **Thông báo lỗi** không lộ chi tiết nội bộ (tên bảng, đường dẫn).
- **Thư viện**: `dotnet list package --vulnerable --include-transitive`; bật Dependabot/GitHub security alerts; cập nhật .NET (bản vá bảo mật hằng tháng).
- **CORS**: liệt kê chính xác origin (Chương 9).
- **Giới hạn kích thước request** (`MaxRequestBodySize`), số lượng, độ sâu JSON.
- **Chạy với quyền tối thiểu**: tài khoản CSDL chỉ có quyền cần dùng; container không chạy `root`.

## Danh sách kiểm tra trước khi triển khai

- [ ] HTTPS + HSTS; cookie `Secure/HttpOnly/SameSite`.
- [ ] Xác thực đúng: mật khẩu băm (hoặc Identity/IdP), MFA nếu có thể; giới hạn tốc độ đăng nhập.
- [ ] Mọi endpoint nhạy cảm có `[Authorize]`/policy; `FallbackPolicy` chặn mặc định; kiểm tra **quyền sở hữu**.
- [ ] Validate mọi đầu vào ở server; tham số hoá SQL; mã hoá đầu ra; header bảo mật + CSP.
- [ ] Antiforgery cho form/cookie; CORS tối thiểu.
- [ ] Không có bí mật trong Git; kho bí mật ở production.
- [ ] Trang lỗi an toàn; log không chứa dữ liệu nhạy cảm; theo dõi cảnh báo.
- [ ] Thư viện được quét lỗ hổng; đã cập nhật .NET.
- [ ] Sao lưu dữ liệu, kế hoạch phản ứng sự cố.

## Lỗi thường gặp

- Tin tưởng kiểm tra ở client; ẩn nút thay cho kiểm tra quyền.
- `@Html.Raw`/`MarkupString`/nối HTML với dữ liệu người dùng.
- Kiểm tra "đã đăng nhập" nhưng bỏ sót quyền sở hữu (IDOR).
- Open redirect qua `returnUrl` không kiểm tra; SSRF qua URL người dùng.
- `AllowAnyOrigin` + `AllowCredentials`; CORS coi như cơ chế bảo mật API.
- Cấu hình rate limit trước proxy mà không `UseForwardedHeaders` → giới hạn theo IP proxy.
- Khoá Data Protection không dùng chung giữa các instance → lỗi giải mã lạ sau khi scale/redeploy.
- Không cập nhật thư viện/.NET; lộ header `Server`/phiên bản.

## Bài tập

1. Dùng `curl` gửi `?ten=<img src=x onerror=alert(1)>` tới `/xss-sai` và `/xss-dung`; giải thích khác biệt và thử CSP chặn script nội tuyến trong trình duyệt.
2. Thêm `GET /don-hang` chỉ trả đơn của **người dùng hiện tại** (lọc trong truy vấn, không lọc trong bộ nhớ).
3. Đổi chính sách "dang-nhap" sang **sliding window** và **token bucket**; đo hành vi khác nhau bằng vòng lặp `curl`.
4. Viết token đặt lại mật khẩu hết hạn sau 15 phút dùng Data Protection và chỉ dùng được **một lần** (lưu `jti` đã dùng).
5. Chạy `dotnet list package --vulnerable --include-transitive` cho một dự án của chương này và ghi nhận kết quả.
