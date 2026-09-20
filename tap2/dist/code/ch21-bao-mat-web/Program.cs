using System.Net;
using System.Text.Encodings.Web;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Rate limiting: chan lam dung va brute-force ----------
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Chinh sach chung: moi dia chi IP toi da 20 request / 10 giay
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "khong-ro",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromSeconds(10), QueueLimit = 0 }));

    // Chinh sach RIENG, chat hon cho dang nhap: 3 lan / 30 giay (chong doan mat khau)
    o.AddPolicy("dang-nhap", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "khong-ro",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromSeconds(30), QueueLimit = 0 }));

    o.OnRejected = async (ctx, ct) =>
    {
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var doiSau))
            ctx.HttpContext.Response.Headers.RetryAfter = ((int)doiSau.TotalSeconds).ToString();
        await ctx.HttpContext.Response.WriteAsync("Qua nhieu yeu cau. Vui long thu lai sau.", ct);
    };
});

// ---------- 2. Data Protection: ma hoa/ky du lieu ngan han (token, tham so) ----------
builder.Services.AddDataProtection().SetApplicationName("cuahang-demo");

// ---------- 3. Sau reverse proxy (Nginx, load balancer): tin X-Forwarded-* cua proxy DA BIET ----------
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownProxies.Add(IPAddress.Loopback);          // CHI tin proxy trong danh sach, neu khong ai cung gia mao IP duoc
});

builder.Services.AddSingleton<DonHangKho>();

var app = builder.Build();

app.UseForwardedHeaders();

// ---------- 4. Header bao mat cho MOI response ----------
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";                          // khong cho trinh duyet "doan" MIME (tan cong upload)
    h["X-Frame-Options"] = "DENY";                                    // chong clickjacking (nhung trang khac chen trang minh vao iframe)
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // CSP: chi cho tai script/style tu CHINH minh -> XSS bi giam manh du co bi chen the <script>
    h["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; object-src 'none'; frame-ancestors 'none'";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();                     // dan trinh duyet CHI dung HTTPS voi ten mien nay tu nay ve sau
    app.UseHttpsRedirection();
}
app.UseRateLimiter();

app.MapGet("/", () => "Bao mat web: thu /xss-sai?ten=..., /xss-dung?ten=..., /don-hang/1, /chuyen-huong?url=...");

// ---------- 5. XSS: dua du lieu nguoi dung vao HTML ----------
app.MapGet("/xss-sai", (string ten) => Results.Content($"<h1>Xin chao {ten}</h1>", "text/html"));     // ✘ NOI CHUOI: <script> chay duoc

app.MapGet("/xss-dung", (string ten) =>
    Results.Content($"<h1>Xin chao {HtmlEncoder.Default.Encode(ten)}</h1>", "text/html"));              // ✔ MA HOA HTML

// ---------- 6. IDOR: kiem tra QUYEN SO HUU, khong chi kiem tra "da dang nhap" ----------
app.MapGet("/don-hang/{id:int}", (int id, HttpContext ctx, DonHangKho kho) =>
{
    string nguoiDung = ctx.Request.Headers["X-Demo-User"].ToString();          // (thay bang ClaimsPrincipal that - Chuong 20)
    var don = kho.Lay(id);
    if (don is null) return Results.NotFound();
    if (don.Chu != nguoiDung) return Results.NotFound();                        // 404 (khong 403): khong tiet lo don ton tai
    return Results.Ok(don);
});

// ---------- 7. Open redirect ----------
app.MapGet("/chuyen-huong", (string url) =>
    LaUrlNoiBo(url)
        ? Results.LocalRedirect(url)                                           // chi cho phep duong dan NOI BO
        : Results.BadRequest("URL chuyen huong khong hop le"));

// ---------- 8. Data Protection: token co han dung, khong the gia mao ----------
app.MapGet("/lien-ket-kich-hoat", (IDataProtectionProvider dp) =>
{
    var bv = dp.CreateProtector("kich-hoat-tai-khoan").ToTimeLimitedDataProtector();
    string token = bv.Protect("user:42", lifetime: TimeSpan.FromMinutes(10));
    return new { token, tokenHopLe = bv.Unprotect(token) };
});

app.MapGet("/lien-ket-kich-hoat/{token}", (string token, IDataProtectionProvider dp) =>
{
    try { return Results.Ok(new { noiDung = dp.CreateProtector("kich-hoat-tai-khoan").ToTimeLimitedDataProtector().Unprotect(token) }); }
    catch (System.Security.Cryptography.CryptographicException) { return Results.BadRequest("Token khong hop le hoac het han"); }
});

// ---------- 9. Dang nhap (voi gioi han toc do) ----------
app.MapPost("/dang-nhap", () => Results.Problem("Sai ten dang nhap hoac mat khau", statusCode: 401))
   .RequireRateLimiting("dang-nhap");

// ---------- 10. Log forging: dung du lieu nguoi dung NGUYEN VAN trong log ----------
app.MapGet("/ghi-log", (string ten, ILogger<Program> log) =>
{
    log.LogInformation("Nguoi dung {Ten} vua truy cap", ten);                  // tham so dat ten: bi "khoa" thanh mot truong, khong tao dong log gia
    return "da ghi";
});

app.Run();

// Chi chap nhan duong dan tuong doi bat dau bang MOT dau "/" (khong phai "//host" hay "/\host": trinh duyet hieu la ten mien khac)
static bool LaUrlNoiBo(string url)
    => !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));

public record DonHang(int Id, string Chu, decimal Tien);

public class DonHangKho
{
    private readonly List<DonHang> _ds = [new(1, "an", 100_000), new(2, "binh", 999_000)];
    public DonHang? Lay(int id) => _ds.FirstOrDefault(d => d.Id == id);
}
