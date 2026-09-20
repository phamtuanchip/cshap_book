using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

string baseUrl = builder.Configuration["ThoiTiet:BaseUrl"] ?? "http://localhost:5219";   // "dich vu ben ngoai" (o day chinh la /ngoai/* cua app nay)

builder.Services.AddTransient<ApiKeyHandler>();

// ---- 1. Typed client + resilience: cach khuyen nghi ----
builder.Services.AddHttpClient<ThoiTietClient>(c =>
    {
        c.BaseAddress = new Uri(baseUrl);
        c.Timeout = TimeSpan.FromSeconds(10);                          // tran cho toan bo (resilience se dat nguong chat hon)
        c.DefaultRequestHeaders.Add("User-Agent", "CuaHangApp/1.0");
    })
    .AddHttpMessageHandler<ApiKeyHandler>()                            // DelegatingHandler: them API key cho MOI request
    .AddStandardResilienceHandler(o =>                                 // retry + timeout + circuit breaker + rate limit san dung
    {
        o.Retry.MaxRetryAttempts = 3;
        o.Retry.Delay = TimeSpan.FromMilliseconds(100);
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);            // moi LAN THU toi da 1s
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(6);       // ca chuoi thu lai toi da 6s
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);  // phai >= 2 x AttemptTimeout
    });

// ---- 2. Named client KHONG co resilience de so sanh ----
builder.Services.AddHttpClient("tran", c => c.BaseAddress = new Uri(baseUrl));

var app = builder.Build();

// ================= "Dich vu ben ngoai" gia lap =================
var soLanGoi = new ConcurrentDictionary<string, int>();

app.MapGet("/ngoai/thoi-tiet/{thanhPho}", (string thanhPho, HttpRequest req) =>
{
    int lan = soLanGoi.AddOrUpdate(thanhPho, 1, (_, cu) => cu + 1);
    if (thanhPho == "hanoi" && lan <= 2)
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);     // loi TAM THOI: 2 lan dau that bai
    return Results.Ok(new ThoiTiet(thanhPho, 28.5, "Nang nhe", req.Headers["X-Api-Key"].ToString()));
});
app.MapGet("/ngoai/cham", async () => { await Task.Delay(3000); return "xong"; });
app.MapGet("/ngoai/loi-vinh-vien", () => Results.StatusCode(500));
app.MapPost("/ngoai/reset", () => { soLanGoi.Clear(); return Results.NoContent(); });

// ================= Endpoint cua ung dung, goi dich vu ngoai =================
app.MapGet("/thoi-tiet/{thanhPho}", async (string thanhPho, ThoiTietClient client, CancellationToken ct) =>
{
    try
    {
        var tt = await client.LayAsync(thanhPho, ct);
        return Results.Ok(tt);
    }
    catch (HttpRequestException e)                                       // ket noi loi / ma trang thai khong thanh cong
    {
        return Results.Problem($"Dich vu thoi tiet loi: {e.StatusCode?.ToString() ?? e.Message}", statusCode: StatusCodes.Status502BadGateway);
    }
    catch (OperationCanceledException) when (!ct.IsCancellationRequested)   // het gio (khong phai client huy)
    {
        return Results.Problem("Dich vu thoi tiet phan hoi qua cham", statusCode: StatusCodes.Status504GatewayTimeout);
    }
});

// Khong co resilience: thay loi ngay lan dau
app.MapGet("/thoi-tiet-tran/{thanhPho}", async (string thanhPho, IHttpClientFactory factory, CancellationToken ct) =>
{
    var http = factory.CreateClient("tran");
    var res = await http.GetAsync($"/ngoai/thoi-tiet/{thanhPho}", ct);
    return res.IsSuccessStatusCode ? Results.Ok(await res.Content.ReadFromJsonAsync<ThoiTiet>(ct)) : Results.StatusCode((int)res.StatusCode);
});

app.MapGet("/cham", async (ThoiTietClient client, CancellationToken ct) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    try { await client.GoiChamAsync(ct); return Results.Ok("khong bao gio toi day"); }
    catch (Exception e) when (e is Polly.Timeout.TimeoutRejectedException or OperationCanceledException)   // het gio do resilience
    { return Results.Problem($"Bi ngat sau {sw.ElapsedMilliseconds} ms: {e.GetType().Name}", statusCode: 504); }
});

app.MapGet("/", () => "Thu: /thoi-tiet/hanoi, /thoi-tiet-tran/hanoi, /cham (doi BASE URL bang ThoiTiet__BaseUrl neu doi cong)");

app.Run();

// ================= Typed client =================
public record ThoiTiet(string ThanhPho, double NhietDo, string MoTa, string? ApiKeyNhanDuoc);

public class ThoiTietClient(HttpClient http)      // HttpClient duoc tiem da cau hinh san (BaseAddress, handler...)
{
    public async Task<ThoiTiet?> LayAsync(string thanhPho, CancellationToken ct)
    {
        // Uri.EscapeDataString: ma hoa phan duong dan tu nguoi dung
        var res = await http.GetAsync($"/ngoai/thoi-tiet/{Uri.EscapeDataString(thanhPho)}", ct);
        res.EnsureSuccessStatusCode();                                   // != 2xx -> HttpRequestException (StatusCode co gia tri)
        return await res.Content.ReadFromJsonAsync<ThoiTiet>(ct);
    }

    public async Task GoiChamAsync(CancellationToken ct)
        => (await http.GetAsync("/ngoai/cham", ct)).EnsureSuccessStatusCode();
}

// DelegatingHandler: "middleware" cho HttpClient (Chuong 3)
public class ApiKeyHandler(IConfiguration cfg, ILogger<ApiKeyHandler> log) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Add("X-Api-Key", cfg["ThoiTiet:ApiKey"] ?? "khoa-thu-nghiem");
        var res = await base.SendAsync(request, ct);
        log.LogInformation("Goi {Method} {Uri} -> {Status}", request.Method, request.RequestUri?.PathAndQuery, (int)res.StatusCode);
        return res;
    }
}
