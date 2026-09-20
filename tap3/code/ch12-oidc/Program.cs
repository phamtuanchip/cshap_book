using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ===== API la RESOURCE SERVER: chi KIEM TRA token do Authorization Server (IdP) cap; khong tu dang nhap =====
string? authority = builder.Configuration["Oidc:Authority"];     // vd https://login.example.com/realms/kho hoac https://<tenant>.auth0.com/
string audience = builder.Configuration["Oidc:Audience"] ?? "kho-api";
var khoaDev = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes("khoa-chi-dung-de-demo-KHONG-dung-that")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;                              // giu ten claim goc ("sub", "scope") thay vi doi sang URI dai cua Microsoft
        o.TokenValidationParameters.NameClaimType = "name";
        o.TokenValidationParameters.RoleClaimType = "roles";
        if (!string.IsNullOrEmpty(authority))
        {
            o.Authority = authority;                             // tai discovery + JWKS (khoa cong khai) tu IdP, tu dong xoay khoa
            o.Audience = audience;                               // token phai DANH CHO API nay (aud)
        }
        else
        {
            // Che do demo: "IdP gia" ky bang khoa doi xung. Cac kiem tra khac VAN nguyen ven (issuer, audience, han dung)
            o.TokenValidationParameters.ValidIssuer = "https://idp-demo.local";
            o.TokenValidationParameters.ValidAudience = audience;
            o.TokenValidationParameters.IssuerSigningKey = khoaDev;
            o.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(5);
        }
    });

// ===== Phan quyen theo SCOPE (quyen cua UNG DUNG duoc uy quyen) khac voi ROLE (vai tro cua NGUOI DUNG) =====
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("DocKho", p => p.RequireAuthenticatedUser().RequireAssertion(c => CoScope(c.User, "kho.doc")))
    .AddPolicy("GhiKho", p => p.RequireAuthenticatedUser().RequireAssertion(c => CoScope(c.User, "kho.ghi")))
    .AddPolicy("QuanTri", p => p.RequireRole("quan-tri"));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/san-pham", (ClaimsPrincipal u) => new { nguoiDung = u.FindFirstValue("sub"), scope = u.FindFirstValue("scope"), du_lieu = new[] { "LT001", "CH002" } })
   .RequireAuthorization("DocKho");
app.MapPost("/api/san-pham", () => Results.Created("/api/san-pham/LT999", null)).RequireAuthorization("GhiKho");
app.MapDelete("/api/san-pham/{ma}", (string ma) => Results.NoContent()).RequireAuthorization("QuanTri");

// ---- CHI DE DEMO: cap token nhu mot IdP (khong co trong san pham that!) ----
if (string.IsNullOrEmpty(authority))
{
    app.MapGet("/dev/token", (string sub, string scope, string? role, int? hetHanSau) =>
    {
        var claims = new List<Claim> { new("sub", sub), new("name", sub), new("scope", scope) };
        if (role is not null) claims.Add(new("roles", role));
        var jwt = new JwtSecurityToken("https://idp-demo.local", audience, claims,
            expires: DateTime.UtcNow.AddSeconds(hetHanSau ?? 300), signingCredentials: new SigningCredentials(khoaDev, SecurityAlgorithms.HmacSha256));
        return new { access_token = new JwtSecurityTokenHandler().WriteToken(jwt), token_type = "Bearer" };
    });
}

// ---- PKCE (RFC 7636): code_verifier ngau nhien -> code_challenge = BASE64URL(SHA256(verifier)) ----
app.MapGet("/pkce", (string? verifier) =>
{
    verifier ??= Base64Url(RandomNumberGenerator.GetBytes(32));                      // 43 ky tu, entropy cao
    string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    return new { code_verifier = verifier, code_challenge = challenge, code_challenge_method = "S256" };
});

app.Run();

static bool CoScope(ClaimsPrincipal u, string can) => (u.FindFirstValue("scope") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(can);
static string Base64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
