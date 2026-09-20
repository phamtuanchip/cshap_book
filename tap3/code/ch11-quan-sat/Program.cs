using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

const string TenDichVu = "kho-dat-hang";

// ============ OpenTelemetry: TRACE + METRICS + LOGS, cung mot chuan (OTLP) cho moi cong cu ============
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(TenDichVu, serviceVersion: "1.0.0"))        // "ai la nguon phat": ten dich vu, phien ban
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()                                               // tu dong: span cho moi request den
        .AddHttpClientInstrumentation()                                               // tu dong: span cho moi request di + truyen traceparent
        .AddSource(DatHangTelemetry.TenNguon)                                         // nguon span TU VIET
        .AddConsoleExporter())                                                        // demo: in ra console. Thuc te: .AddOtlpExporter() -> Jaeger/Tempo/Application Insights...
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddMeter(DatHangTelemetry.TenNguon)                                          // meter TU VIET
        .AddConsoleExporter((_, r) => r.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 3000));

builder.Logging.AddOpenTelemetry(o =>                                                 // log cung di qua OTel: tu dong kem TraceId/SpanId
{
    o.IncludeFormattedMessage = true;
    o.ParseStateValues = true;
});

builder.Services.AddHttpClient("ngoai", c => c.BaseAddress = new Uri(builder.Configuration["Ngoai:BaseUrl"] ?? "http://localhost:5250"));
builder.Services.AddSingleton<DatHangTelemetry>();

// ============ Health checks: LIVENESS khac READINESS ============
builder.Services.AddHealthChecks()
    .AddCheck("tien-trinh", () => HealthCheckResult.Healthy(), tags: ["live"])                          // "toi con song": khong kiem tra phu thuoc
    .AddCheck("csdl", () => HealthCheckResult.Healthy("Ket noi CSDL on"), tags: ["ready"])              // "toi san sang nhan tai": kiem tra phu thuoc
    .AddCheck("hang-doi", () => HealthCheckResult.Degraded("Hang doi dang day"), tags: ["ready"]);      // Degraded: van nhan tai nhung canh bao

var app = builder.Build();

app.MapHealthChecks("/health/live", new() { Predicate = c => c.Tags.Contains("live") });    // orchestrator dung de KHOI DONG LAI khi treo
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });  // load balancer dung de NGUNG GUI TAI khi chua san sang

// ---- Dich vu "ben ngoai": thanh toan (goi chinh app nay de thay traceparent lan qua HTTP) ----
app.MapPost("/ngoai/thanh-toan", async (HttpContext ctx) =>
{
    await Task.Delay(40);
    return Results.Ok(new { traceparentNhanDuoc = ctx.Request.Headers["traceparent"].ToString() });
});

// ---- Luong nghiep vu co nhieu buoc: moi buoc mot span con ----
app.MapPost("/dat-hang/{ma}", async (string ma, int soLuong, DatHangTelemetry tele, IHttpClientFactory http, ILogger<Program> log) =>
{
    var dong = Stopwatch.StartNew();

    using var span = tele.Nguon.StartActivity("DatHang");                          // span cha (nam duoi span cua ASP.NET Core)
    span?.SetTag("san_pham.ma", ma);
    span?.SetTag("so_luong", soLuong);

    using (tele.Nguon.StartActivity("KiemTraTon"))
        await Task.Delay(15);

    string ketQua = "thanh_cong";
    try
    {
        using (tele.Nguon.StartActivity("ThanhToan"))
        {
            var res = await http.CreateClient("ngoai").PostAsync("/ngoai/thanh-toan", null);   // span client + traceparent tu dong
            res.EnsureSuccessStatusCode();
        }
        if (soLuong > 100) throw new InvalidOperationException("Vuot han muc dat hang");
    }
    catch (Exception e)
    {
        ketQua = "that_bai";
        span?.SetStatus(ActivityStatusCode.Error, e.Message);                        // danh dau span LOI: hien do trong cong cu trace
        span?.AddException(e);
        log.LogError(e, "Dat hang {Ma} that bai", ma);
    }

    tele.DonHang.Add(1, new KeyValuePair<string, object?>("ket_qua", ketQua));         // counter, gan nhan (tag)
    tele.ThoiGian.Record(dong.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("ket_qua", ketQua));   // histogram
    log.LogInformation("Dat hang {Ma} x{SoLuong} -> {KetQua}", ma, soLuong, ketQua);   // log tu dong mang TraceId/SpanId
    return ketQua == "thanh_cong" ? Results.Ok(new { ma, soLuong, traceId = Activity.Current?.TraceId.ToString() }) : Results.UnprocessableEntity(new { loi = "Dat hang that bai" });
});

app.Run();

// Gom "cong cu do" cua ung dung: mot nguon span (ActivitySource) + mot meter
public sealed class DatHangTelemetry : IDisposable
{
    public const string TenNguon = "Kho.DatHang";

    public ActivitySource Nguon { get; } = new(TenNguon);
    private readonly Meter _meter = new(TenNguon);
    public Counter<long> DonHang { get; }
    public Histogram<double> ThoiGian { get; }

    public DatHangTelemetry()
    {
        DonHang = _meter.CreateCounter<long>("don_hang_dat", unit: "{don}", description: "So don hang da xu ly, theo ket qua");
        ThoiGian = _meter.CreateHistogram<double>("thoi_gian_dat_hang", unit: "ms", description: "Thoi gian xu ly mot don hang");
    }

    public void Dispose() { Nguon.Dispose(); _meter.Dispose(); }
}
