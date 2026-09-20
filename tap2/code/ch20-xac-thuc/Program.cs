using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Khoa ky JWT: >= 32 byte, LAY TU CAU HINH/KHO BI MAT (Chuong 6). Gia tri duoi day CHI de demo.
string khoaBiMat = builder.Configuration["Jwt:Khoa"] ?? "khoa-demo-chi-dung-khi-phat-trien-1234567890";
var khoaKy = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(khoaBiMat));
const string PhatHanh = "cuahang-api", DoiTuong = "cuahang-client";

// ================= XAC THUC (Authentication): "ban la ai?" =================
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)       // scheme mac dinh: cookie (ung dung web)
    .AddCookie(o =>
    {
        o.Cookie.Name = "cuahang.auth";
        o.Cookie.HttpOnly = true;                       // JavaScript KHONG doc duoc cookie (chong danh cap qua XSS)
        o.Cookie.SameSite = SameSiteMode.Lax;           // giam nguy co CSRF
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // production: Always (chi gui qua HTTPS)
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        o.SlidingExpiration = true;
        // API khong nen redirect toi trang dang nhap: tra 401/403 dung nghia
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>                   // scheme "Bearer" cho API/mobile/SPA
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = PhatHanh,
            ValidateAudience = true, ValidAudience = DoiTuong,
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true, IssuerSigningKey = khoaKy,
            NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role,
        };
    });

// ================= PHAN QUYEN (Authorization): "ban duoc lam gi?" =================
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("QuanTri", p => p.RequireRole("Admin"))
    .AddPolicy("TuoiTu18", p => p.RequireAssertion(ctx =>                        // policy dua tren claim
        int.TryParse(ctx.User.FindFirstValue("tuoi"), out int tuoi) && tuoi >= 18))
    .AddPolicy("BearerAdmin", p => p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireRole("Admin"));

builder.Services.AddSingleton<IAuthorizationHandler, CungTacGiaHandler>();       // resource-based authorization
builder.Services.AddSingleton<TaiKhoanStore>();
builder.Services.AddSingleton<BaiVietStore>();

var app = builder.Build();

app.UseAuthentication();     // 1) xac dinh danh tinh -> gan HttpContext.User        (PHAI truoc UseAuthorization)
app.UseAuthorization();      // 2) kiem tra quyen theo [Authorize]/policy cua endpoint

app.MapGet("/", () => "Thu: POST /dang-nhap, GET /toi, POST /api/token, GET /api/toi ...");

// ---------- A. Xac thuc bang COOKIE (trinh duyet) ----------
app.MapPost("/dang-nhap", async (DangNhapRequest req, TaiKhoanStore tk, HttpContext ctx) =>
{
    var nd = tk.XacThuc(req.TenDangNhap, req.MatKhau);
    if (nd is null) return Results.Problem("Ten dang nhap hoac mat khau khong dung", statusCode: 401);   // thong bao CHUNG

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(TaoDanhTinh(nd, "Cookies")));
    return Results.Ok(new { chao = $"Xin chao {nd.Ten}" });
}).DisableAntiforgery();          // demo bang curl; ung dung that co form: BAT antiforgery (Chuong 17)

app.MapPost("/dang-xuat", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).RequireAuthorization().DisableAntiforgery();

app.MapGet("/toi", (ClaimsPrincipal user) => new                                 // yeu cau DA dang nhap (cookie mac dinh)
{
    ten = user.Identity!.Name,
    vaiTro = user.FindAll(ClaimTypes.Role).Select(c => c.Value),
    tatCaClaim = user.Claims.ToDictionary(c => c.Type.Split('/').Last(), c => c.Value, StringComparer.Ordinal),
}).RequireAuthorization();

app.MapGet("/quan-tri", () => "Khu vuc quan tri (cookie)").RequireAuthorization("QuanTri");
app.MapGet("/nguoi-lon", () => "Noi dung cho 18+").RequireAuthorization("TuoiTu18");

// ---------- B. Xac thuc bang JWT (API) ----------
app.MapPost("/api/token", (DangNhapRequest req, TaiKhoanStore tk) =>
{
    var nd = tk.XacThuc(req.TenDangNhap, req.MatKhau);
    if (nd is null) return Results.Problem("Ten dang nhap hoac mat khau khong dung", statusCode: 401);

    var claims = TaoDanhTinh(nd, "jwt").Claims;
    var token = new JwtSecurityToken(
        issuer: PhatHanh, audience: DoiTuong, claims: claims,
        notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddMinutes(15),       // token NGAN han
        signingCredentials: new SigningCredentials(khoaKy, SecurityAlgorithms.HmacSha256));

    return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), loai = "Bearer", hetHanSau = 900 });
});

var api = app.MapGroup("/api").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Bearer" });

api.MapGet("/toi", (ClaimsPrincipal user) => new { ten = user.Identity!.Name, vaiTro = user.FindAll(ClaimTypes.Role).Select(c => c.Value) });

api.MapGet("/admin/bao-cao", () => new { doanhThu = 123_456_789 }).RequireAuthorization("BearerAdmin");

// Resource-based authorization: quyet dinh phu thuoc vao DOI TUONG cu the
api.MapDelete("/bai-viet/{id:int}", async (int id, BaiVietStore kho, ClaimsPrincipal user, IAuthorizationService auth) =>
{
    var bv = kho.Lay(id);
    if (bv is null) return Results.NotFound();

    var kq = await auth.AuthorizeAsync(user, bv, new CungTacGiaRequirement());   // tac gia HOAC admin
    if (!kq.Succeeded) return Results.Forbid();

    kho.Xoa(id);
    return Results.NoContent();
});

app.Run();

// ================= Ho tro =================
static ClaimsIdentity TaoDanhTinh(NguoiDung nd, string scheme)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, nd.Id.ToString()),
        new(ClaimTypes.Name, nd.Ten),
        new("tuoi", nd.Tuoi.ToString()),
    };
    claims.AddRange(nd.VaiTro.Select(v => new Claim(ClaimTypes.Role, v)));
    return new ClaimsIdentity(claims, scheme, ClaimTypes.Name, ClaimTypes.Role);
}

public record DangNhapRequest(string TenDangNhap, string MatKhau);
public record NguoiDung(int Id, string Ten, int Tuoi, string[] VaiTro, string MatKhauBam);

public class TaiKhoanStore
{
    private readonly PasswordHasher<object> _bam = new();
    private readonly List<NguoiDung> _ds;

    public TaiKhoanStore() => _ds =
    [
        new(1, "admin", 40, ["Admin", "User"], _bam.HashPassword(new object(), "Admin@123")),
        new(2, "an", 20, ["User"], _bam.HashPassword(new object(), "An@12345")),
        new(3, "be", 12, ["User"], _bam.HashPassword(new object(), "Be@12345")),
    ];

    public NguoiDung? XacThuc(string ten, string matKhau)
    {
        var nd = _ds.FirstOrDefault(n => string.Equals(n.Ten, ten, StringComparison.OrdinalIgnoreCase));
        if (nd is null)
        {
            _bam.HashPassword(new object(), matKhau);      // van ton thoi gian bam: tranh lo "user nay khong ton tai" qua do tre
            return null;
        }
        return _bam.VerifyHashedPassword(new object(), nd.MatKhauBam, matKhau) != PasswordVerificationResult.Failed ? nd : null;
    }
}

public record BaiViet(int Id, string TacGiaId, string TieuDe);

public class BaiVietStore
{
    private readonly List<BaiViet> _ds = [new(1, "2", "Bai cua An"), new(2, "1", "Bai cua Admin")];
    public BaiViet? Lay(int id) => _ds.FirstOrDefault(b => b.Id == id);
    public void Xoa(int id) => _ds.RemoveAll(b => b.Id == id);
}

public class CungTacGiaRequirement : IAuthorizationRequirement;

public class CungTacGiaHandler : AuthorizationHandler<CungTacGiaRequirement, BaiViet>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext ctx, CungTacGiaRequirement req, BaiViet bv)
    {
        bool laTacGia = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) == bv.TacGiaId;
        if (laTacGia || ctx.User.IsInRole("Admin")) ctx.Succeed(req);
        return Task.CompletedTask;
    }
}
