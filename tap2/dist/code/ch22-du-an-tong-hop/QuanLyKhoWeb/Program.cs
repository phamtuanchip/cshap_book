using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyKhoWeb.Data;
using QuanLyKhoWeb.Endpoints;
using QuanLyKhoWeb.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Cau hinh co kiem tra khi khoi dong (Chuong 6) ----------
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Configure(o => { if (string.IsNullOrEmpty(o.Khoa) && builder.Environment.IsDevelopment()) o.Khoa = "khoa-phat-trien-khong-dung-cho-production-1234567890"; })
    .Validate(o => o.Khoa.Length >= 32, "Jwt:Khoa phai >= 32 ky tu (dat bang bien moi truong Jwt__Khoa)")
    .ValidateOnStart();

// ---------- Du lieu (Chuong 12-14) ----------
builder.Services.AddDbContext<KhoDb>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Kho") ?? "Data Source=quanlykho.db"));
builder.Services.AddScoped<KhoService>();
builder.Services.AddSingleton(TimeProvider.System);

// ---------- Xac thuc & phan quyen (Chuong 20) ----------
builder.Services.AddSingleton<TaiKhoanService>();
builder.Services.AddSingleton<PhatHanhToken>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)       // dung cung JwtOptions (mot nguon su that)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((bearer, jwt) =>
    {
        var j = jwt.Value;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = j.PhatHanh, ValidAudience = j.DoiTuong,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(j.Khoa)),
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Name, RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())       // MAC DINH: phai dang nhap
    .AddPolicy("GhiKho", p => p.RequireRole(VaiTro.NhanVien, VaiTro.Admin))
    .AddPolicy("QuanTri", p => p.RequireRole(VaiTro.Admin));

// ---------- Chong lam dung (Chuong 21) ----------
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("dang-nhap", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "khong-ro",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue("GioiHan:DangNhap", 10), Window = TimeSpan.FromMinutes(1) }));
});

// ---------- Loi va tai lieu (Chuong 9) ----------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<LoiNghiepVuHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Configuration.GetValue("MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<KhoDb>();
    await db.Database.MigrateAsync();
    await KhoDb.NapMauAsync(db);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();                       // wwwroot/index.html: giao dien don gian goi API
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();          // /scalar/v1
}
app.MapHealthChecks("/health").AllowAnonymous();
app.MapKho();

app.Run();

public partial class Program;

public class LoiNghiepVuHandler(IProblemDetailsService problemDetails, ILogger<LoiNghiepVuHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception e, CancellationToken ct)
    {
        int status = e switch
        {
            KhongTimThayException => 404,
            XungDotException or KhongDuHangException => 409,
            LoiNghiepVu => 422,
            _ => 500,
        };
        if (status == 500) log.LogError(e, "Loi khong xu ly tai {Path}", ctx.Request.Path);

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            Exception = e,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status == 500 ? "Loi may chu" : "Yeu cau khong the thuc hien",
                Detail = status == 500 ? null : e.Message,
            },
        });
    }
}
