# Chương 19 — Gọi API ngoài với HttpClient

## Mục tiêu học

Sau chương này, bạn sẽ:

- Gọi API bên ngoài đúng cách bằng **`IHttpClientFactory`** và **typed client** — tránh cạm bẫy cổ điển của `new HttpClient()`.
- Thêm hành vi xuyên suốt bằng **`DelegatingHandler`** (API key, log).
- Làm chương trình **chịu lỗi** với **resilience**: retry, timeout, circuit breaker.
- Xử lý `HttpRequestException`, timeout, huỷ (`CancellationToken`) và ánh xạ sang mã HTTP của bạn (`502`, `504`).

Code mẫu: [`code/ch19-httpclient/`](../../code/ch19-httpclient/) — ứng dụng tự chứa một "dịch vụ thời tiết bên ngoài" hay lỗi để bạn thấy retry/timeout hoạt động.

## Vì sao phải gọi API khác?

Ứng dụng thật hiếm khi đứng một mình: cổng thanh toán, gửi email/SMS, bản đồ, dịch vụ nội bộ khác (microservices). **Mạng không đáng tin**: chậm, mất gói, dịch vụ bên kia tạm sập, quá tải. Mã gọi ngoài phải giả định **lỗi là chuyện bình thường** (*"fallacies of distributed computing"*).

## Cạm bẫy của `new HttpClient()`

```csharp
// ✘ SAI khi làm trong mỗi request
using var http = new HttpClient();
var s = await http.GetStringAsync(url);
```

- Mỗi `HttpClient` mới mở **socket mới**; `Dispose` không trả socket ngay (giữ ở trạng thái `TIME_WAIT`) → dưới tải cao dẫn tới **cạn kiệt cổng (socket exhaustion)**.
- Ngược lại, giữ **một `HttpClient` static mãi mãi** thì **không cập nhật DNS** (khi dịch vụ đổi địa chỉ IP, ứng dụng vẫn nói chuyện với IP cũ).

**Giải pháp chuẩn:** `IHttpClientFactory` — quản lý vòng đời và **gộp/luân phiên các kết nối** (pool `HttpMessageHandler`, làm mới định kỳ), đồng thời cho phép cấu hình tập trung.

## Ba cách dùng factory

### 1. Typed client (khuyến nghị)

```csharp
builder.Services.AddHttpClient<ThoiTietClient>(c =>
{
    c.BaseAddress = new Uri(baseUrl);
    c.Timeout = TimeSpan.FromSeconds(10);
    c.DefaultRequestHeaders.Add("User-Agent", "CuaHangApp/1.0");
});

public class ThoiTietClient(HttpClient http)                     // HttpClient được tiêm, đã cấu hình sẵn
{
    public async Task<ThoiTiet?> LayAsync(string thanhPho, CancellationToken ct)
    {
        var res = await http.GetAsync($"/ngoai/thoi-tiet/{Uri.EscapeDataString(thanhPho)}", ct);
        res.EnsureSuccessStatusCode();                           // không phải 2xx → HttpRequestException
        return await res.Content.ReadFromJsonAsync<ThoiTiet>(ct);
    }
}

app.MapGet("/thoi-tiet/{tp}", async (string tp, ThoiTietClient client, CancellationToken ct) => ...);
```

Gói toàn bộ chi tiết HTTP (URL, tên header, JSON) vào **một lớp có tên nghiệp vụ** (`ThoiTietClient`) — phần còn lại của ứng dụng chỉ thấy `LayAsync(...)`. Dễ test (mock lớp này), dễ đổi nhà cung cấp.

### 2. Named client

```csharp
builder.Services.AddHttpClient("tran", c => c.BaseAddress = new Uri(baseUrl));
var http = factory.CreateClient("tran");           // IHttpClientFactory
```

### 3. Basic: `factory.CreateClient()` không cấu hình

Dùng khi chỉ cần client mặc định. Tốt hơn `new HttpClient()`.

## Gửi request và đọc kết quả

```csharp
await http.GetFromJsonAsync<ThoiTiet>("/ngoai/thoi-tiet/hanoi", ct);            // GET + đọc JSON (ném lỗi nếu != 2xx)
await http.PostAsJsonAsync("/api/don-hang", new { khachHangId = 1 }, ct);       // POST JSON
await http.PutAsJsonAsync(...);  await http.DeleteAsync(...);

using var req = new HttpRequestMessage(HttpMethod.Post, "/x") { Content = JsonContent.Create(dto) };
req.Headers.Authorization = new("Bearer", token);                              // header theo từng request
var res = await http.SendAsync(req, ct);

res.StatusCode; res.IsSuccessStatusCode; res.Headers; res.Content.Headers
await res.Content.ReadAsStringAsync(ct);   await res.Content.ReadFromJsonAsync<T>(ct);
```

Lưu ý:

- **Kiểm tra mã trạng thái**: `HttpClient` **không** ném lỗi khi server trả `404/500` — chỉ ném khi không kết nối được. Dùng `EnsureSuccessStatusCode()` hoặc tự đọc `StatusCode`.
- **Mã hoá tham số** đưa vào URL: `Uri.EscapeDataString(x)` (hoặc `QueryHelpers.AddQueryString`). Đừng nối chuỗi thô từ dữ liệu người dùng (chèn tham số, SSRF).
- **Giải phóng** `HttpResponseMessage` bằng `using` khi bạn dùng `SendAsync` trực tiếp; các helper `GetFromJsonAsync` đã lo.
- Với body lớn dùng `HttpCompletionOption.ResponseHeadersRead` và đọc stream.
- Luôn truyền **`CancellationToken`** (từ request) — khi client huỷ, cuộc gọi ra ngoài cũng dừng.

## `DelegatingHandler` — "middleware" của HttpClient

Cũng như pipeline của ASP.NET Core (Chương 3), request đi ra qua chuỗi handler:

```csharp
public class ApiKeyHandler(IConfiguration cfg, ILogger<ApiKeyHandler> log) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Add("X-Api-Key", cfg["ThoiTiet:ApiKey"] ?? "khoa-thu-nghiem");
        var res = await base.SendAsync(request, ct);                    // chuyển tiếp xuống handler kế
        log.LogInformation("Goi {Method} {Uri} -> {Status}", request.Method, request.RequestUri?.PathAndQuery, (int)res.StatusCode);
        return res;
    }
}

builder.Services.AddTransient<ApiKeyHandler>();
builder.Services.AddHttpClient<ThoiTietClient>().AddHttpMessageHandler<ApiKeyHandler>();
```

Dùng cho: gắn token xác thực, correlation id, log, đo thời gian, làm mới token khi `401`. Bí mật (API key) lấy từ cấu hình/kho bí mật (Chương 6), không viết cứng.

## Resilience: sống chung với lỗi

Lỗi mạng chia hai loại:

- **Tạm thời (transient)**: `503`, `429`, `408`, mất kết nối, timeout — *thử lại là có thể thành công*.
- **Vĩnh viễn**: `400`, `401`, `404`, `500` do bug — thử lại vô ích.

Gói **`Microsoft.Extensions.Http.Resilience`** (xây trên **Polly**) cung cấp bộ chiến lược dựng sẵn:

```csharp
builder.Services.AddHttpClient<ThoiTietClient>(...)
    .AddStandardResilienceHandler(o =>
    {
        o.Retry.MaxRetryAttempts = 3;
        o.Retry.Delay = TimeSpan.FromMilliseconds(100);
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);            // mỗi LẦN THỬ tối đa 1 giây
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(6);       // cả chuỗi thử lại tối đa 6 giây
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);  // ≥ 2 × AttemptTimeout
    });
```

`AddStandardResilienceHandler` xếp sẵn (từ ngoài vào trong): **giới hạn đồng thời (rate limiter) → timeout tổng → retry → circuit breaker → timeout mỗi lần thử**.

- **Retry** (thử lại) với **backoff luỹ thừa + jitter** (giãn dần và ngẫu nhiên hoá để tránh mọi client thử lại cùng lúc — "thundering herd").
- **Timeout**: không bao giờ chờ vô hạn. Mặc định của `HttpClient` là 100 giây — quá dài với API người dùng đang chờ.
- **Circuit breaker** (cầu dao): khi tỉ lệ lỗi cao vượt ngưỡng, **ngắt mạch** — các lệnh gọi sau *thất bại ngay* (`BrokenCircuitException`) trong một khoảng thời gian, cho dịch vụ kia hồi phục thay vì dồn thêm tải vào nó, và giúp ứng dụng của bạn *nhanh chóng báo lỗi* thay vì treo.

Kết quả chạy code mẫu: dịch vụ giả trả `503` cho hai lần gọi đầu:

```
GET /thoi-tiet/hanoi          → 200 sau ~0.9 s   (log: OnRetry, OnRetry, rồi thành công ở lần 3)
GET /thoi-tiet-tran/hanoi     → 503              (client không có resilience: lỗi ngay lần đầu)
GET /cham (dịch vụ chậm 3 s)  → 504 sau ~4.6 s   (mỗi lần thử bị cắt ở 1 s; thử 4 lần; sau đó TimeoutRejectedException)
```

**Lưu ý quan trọng về retry:**

- Chỉ retry thao tác **idempotent** (`GET`, `PUT`, `DELETE`). Retry `POST` tạo đơn hàng/thanh toán có thể tạo **hai lần** — cần **khoá idempotency** (header `Idempotency-Key` do client sinh; server lưu và trả lại kết quả cũ cho khoá trùng — Tập 3). `AddStandardResilienceHandler` mặc định retry cả `POST`; với API không idempotent hãy tắt retry (`o.Retry.DisableForUnsafeHttpMethods()`) hoặc dùng `AddStandardHedgingHandler` cho `GET`.
- Retry nhân đôi tải lên dịch vụ đang lỗi — cần đi cùng circuit breaker và giới hạn số lần.
- Tổng thời gian chờ nhân lên (1 s × 4 lần + trễ giữa các lần ≈ 4,6 s): đặt `TotalRequestTimeout` để có trần cứng.

## Chuyển lỗi ngoài thành lỗi của bạn

Client gọi API bên kia → bên kia lỗi → **bạn** là "cổng" (gateway). Chuẩn HTTP có mã riêng:

| Tình huống | Mã bạn trả |
|-----------|-----------|
| Không kết nối được / bên kia trả lỗi 5xx | **502 Bad Gateway** |
| Bên kia phản hồi quá chậm (timeout) | **504 Gateway Timeout** |
| Cầu dao đang mở / quá tải | **503 Service Unavailable** (kèm `Retry-After`) |

```csharp
try { return Results.Ok(await client.LayAsync(tp, ct)); }
catch (HttpRequestException e)
{ return Results.Problem($"Dich vu thoi tiet loi: {e.StatusCode}", statusCode: 502); }
catch (OperationCanceledException) when (!ct.IsCancellationRequested)          // hết giờ, KHÔNG phải client huỷ
{ return Results.Problem("Dich vu thoi tiet phan hoi qua cham", statusCode: 504); }
```

Phân biệt **hết giờ** với **client tự huỷ** bằng `ct.IsCancellationRequested`. Không lộ chi tiết nội bộ (URL, khoá) trong thông báo; log đầy đủ ở server (Chương 9). Nên gom xử lý này vào `IExceptionHandler` chung thay vì lặp ở từng endpoint.

## Phương án dự phòng (fallback) và bộ nhớ đệm

Khi dịch vụ ngoài lỗi, đôi khi trả **dữ liệu cũ đã cache** hoặc giá trị mặc định tốt hơn là báo lỗi (thời tiết cũ 10 phút vẫn hữu ích). `IMemoryCache`/`HybridCache` (Tập 3) + resilience `Fallback`. Quyết định theo nghiệp vụ: thanh toán không được "đoán", còn hiển thị thời tiết thì được.

## Kiểm thử code gọi HTTP

Đừng gọi dịch vụ thật trong test. Thay `HttpMessageHandler` bằng bản giả trả kết quả định sẵn:

```csharp
class HandlerGia(HttpStatusCode ma, string body) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        => Task.FromResult(new HttpResponseMessage(ma) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
}
var client = new ThoiTietClient(new HttpClient(new HandlerGia(HttpStatusCode.OK, "{...}")) { BaseAddress = new("http://x") });
```

Hoặc trong `WebApplicationFactory` (Chương 10) thay đăng ký typed client bằng `ConfigurePrimaryHttpMessageHandler`.

## Lỗi thường gặp

- `new HttpClient()` trong mỗi request (cạn socket) hoặc static vĩnh viễn (DNS cũ).
- Quên `EnsureSuccessStatusCode()` → xử lý body lỗi như dữ liệu hợp lệ.
- Không đặt timeout/không dùng `CancellationToken` → yêu cầu treo, tích tụ luồng.
- Retry thao tác không idempotent (`POST` thanh toán) → thanh toán hai lần.
- Retry không giới hạn/không backoff → tự "DDoS" dịch vụ đang yếu.
- Nối chuỗi dữ liệu người dùng vào URL không mã hoá; cho người dùng nhập URL tuỳ ý để server gọi (**SSRF** — kẻ tấn công bắt server truy cập mạng nội bộ; dùng whitelist host).
- Log/ trả ra ngoài header xác thực, token.
- Cấu hình `AttemptTimeout`/`SamplingDuration` mâu thuẫn → ném lỗi validation khi khởi động.
- Quên đăng ký `DelegatingHandler` là `Transient`.

## Bài tập

1. Thêm `Task<IReadOnlyList<ThoiTiet>> LayNhieuAsync(IEnumerable<string> thanhPho, ...)` gọi song song bằng `Task.WhenAll` có giới hạn đồng thời (`SemaphoreSlim`).
2. Đặt `Retry.DisableForUnsafeHttpMethods()` và thử với endpoint `POST` — giải thích lý do.
3. Thêm `IMemoryCache` để nếu dịch vụ lỗi thì trả kết quả gần nhất (đánh dấu `"cu": true`).
4. Viết unit test cho `ThoiTietClient` bằng `HandlerGia`: trường hợp `200`, `404`, JSON sai.
5. Gọi `/ngoai/loi-vinh-vien` nhiều lần liên tiếp và quan sát circuit breaker mở (`BrokenCircuitException`) — ghi lại số lần gọi tới khi mạch mở.
