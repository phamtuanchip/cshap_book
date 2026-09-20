using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Xu ly loi toan cuc ----------
builder.Services.AddProblemDetails();                              // chuan hoa loi thanh ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();    // anh xa exception nghiep vu -> ma HTTP

// ---------- 2. OpenAPI + giao dien Scalar ----------
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Info = new OpenApiInfo
        {
            Title = "API Cua Hang",
            Version = "v1",
            Description = "Vi du xu ly loi, versioning va OpenAPI cua sach ASP.NET Core.",
        };
        return Task.CompletedTask;
    });
});

// ---------- 3. CORS: cho phep trinh duyet tu origin khac goi API ----------
builder.Services.AddCors(o => o.AddPolicy("web-app", p => p
    .WithOrigins("https://cuahang.example.com", "http://localhost:3000")
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .AllowAnyHeader()));

// ---------- 4. Health checks ----------
builder.Services.AddHealthChecks()
    .AddCheck("bo-nho", () => GC.GetTotalMemory(false) < 500_000_000
        ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Bo nho on dinh")
        : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Bo nho cao"));

builder.Services.AddSingleton<SanPhamKho>();

var app = builder.Build();

app.UseExceptionHandler();          // BAT LOI: dat SOM nhat trong pipeline (Chuong 3)
app.UseStatusCodePages();           // 404/405... khong co body -> tra ProblemDetails
app.UseCors("web-app");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                // /openapi/v1.json
    app.MapScalarApiReference();     // /scalar/v1 : giao dien doc va thu API
}

app.MapHealthChecks("/health");

// ---------- 5. Versioning bang duong dan (don gian, khong can thu vien) ----------
var v1 = app.MapGroup("/api/v1/san-pham").WithTags("San pham v1");
var v2 = app.MapGroup("/api/v2/san-pham").WithTags("San pham v2");

v1.MapGet("/{id:int}", Results<Ok<SanPhamV1>, NotFound> (int id, SanPhamKho kho)
    => kho.Tim(id) is { } s ? TypedResults.Ok(new SanPhamV1(s.Id, s.Ten, s.Gia)) : TypedResults.NotFound());

// v2: doi hinh dang response (them truong, doi ten) ma KHONG lam vo client dang dung v1
v2.MapGet("/{id:int}", Results<Ok<SanPhamV2>, NotFound> (int id, SanPhamKho kho)
    => kho.Tim(id) is { } s
        ? TypedResults.Ok(new SanPhamV2(s.Id, s.Ten, new TienTe(s.Gia, "VND"), s.TonKho))
        : TypedResults.NotFound());

// ---------- 6. Endpoint nem exception nghiep vu -> GlobalExceptionHandler xu ly ----------
app.MapGet("/loi/khong-tim-thay", () => { throw new KhongTimThayException("San pham", 999); });
app.MapGet("/loi/xung-dot", () => { throw new XungDotException("Ma san pham da ton tai"); });
app.MapGet("/loi/bat-ngo", () => { throw new InvalidOperationException("Chi tiet noi bo khong nen lo ra ngoai"); });

app.MapPost("/api/v1/san-pham/{id:int}/mua", Results<Ok<string>, Conflict<ProblemDetails>> (int id, int soLuong, SanPhamKho kho) =>
{
    var sp = kho.Tim(id) ?? throw new KhongTimThayException("San pham", id);
    if (sp.TonKho < soLuong)
        return TypedResults.Conflict(new ProblemDetails { Title = "Khong du hang", Detail = $"Con {sp.TonKho}, can {soLuong}", Status = 409 });
    return TypedResults.Ok($"Da mua {soLuong} {sp.Ten}");
}).WithTags("San pham v1");

app.Run();

// ================= Cac lop hotro =================
public record SanPhamV1(int Id, string Ten, decimal Gia);
public record TienTe(decimal SoTien, string DonVi);
public record SanPhamV2(int Id, string Ten, TienTe Gia, int TonKho);
public record SanPhamNoiBo(int Id, string Ten, decimal Gia, int TonKho);

public class SanPhamKho
{
    private readonly List<SanPhamNoiBo> _ds = [new(1, "Laptop", 18_000_000m, 3), new(2, "Chuot", 150_000m, 50)];
    public SanPhamNoiBo? Tim(int id) => _ds.FirstOrDefault(s => s.Id == id);
}

// Exception nghiep vu: mang y nghia "loai loi", khong mang chi tiet ha tang
public abstract class LoiNghiepVu(string message) : Exception(message);
public class KhongTimThayException(string loai, object khoa) : LoiNghiepVu($"Khong tim thay {loai} '{khoa}'");
public class XungDotException(string message) : LoiNghiepVu(message);

// IExceptionHandler (.NET 8+): mot noi DUY NHAT quyet dinh exception nao -> ma HTTP nao
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception e, CancellationToken ct)
    {
        var (status, title) = e switch
        {
            KhongTimThayException => (StatusCodes.Status404NotFound, "Khong tim thay"),
            XungDotException => (StatusCodes.Status409Conflict, "Xung dot du lieu"),
            OperationCanceledException => (499, "Client huy yeu cau"),
            _ => (StatusCodes.Status500InternalServerError, "Loi may chu"),
        };

        // Loi nghiep vu: log muc thap; loi bat ngo: log Error kem stack trace
        if (status >= 500) log.LogError(e, "Loi khong xu ly tai {Path}", ctx.Request.Path);
        else log.LogWarning("Loi nghiep vu {Loai}: {Message}", e.GetType().Name, e.Message);

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            Exception = e,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // CHI lo message chi tiet voi loi nghiep vu; loi 500 giu kin (tranh ro ri thong tin)
                Detail = status < 500 ? e.Message : null,
                Instance = ctx.Request.Path,
            },
        });
    }
}
