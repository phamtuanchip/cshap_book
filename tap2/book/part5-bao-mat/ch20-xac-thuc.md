# Chương 20 — Xác thực và phân quyền

## Mục tiêu học

Sau chương này, bạn sẽ:

- Phân biệt **xác thực (authentication)** và **phân quyền (authorization)**, và **401 vs 403**.
- Cài xác thực bằng **cookie** (ứng dụng web) và **JWT Bearer** (API/mobile/SPA).
- Dùng **claims, roles, policy**, và **resource-based authorization**.
- Lưu mật khẩu đúng cách; biết vai trò của **ASP.NET Core Identity** và **OpenID Connect**.

Code mẫu: [`code/ch20-xac-thuc/`](../../code/ch20-xac-thuc/) — một ứng dụng chạy cả hai cơ chế, đã kiểm tra bằng `curl` (mọi kết quả trong chương lấy từ lần chạy thật).

## Hai câu hỏi khác nhau

| | Xác thực (**Authentication**) | Phân quyền (**Authorization**) |
|---|---|---|
| Câu hỏi | **Bạn là ai?** | **Bạn được làm gì?** |
| Kết quả | danh tính (`ClaimsPrincipal`) | cho phép / từ chối |
| Thất bại → | **`401 Unauthorized`** (chưa biết bạn là ai) | **`403 Forbidden`** (biết rồi nhưng không đủ quyền) |
| Middleware | `UseAuthentication()` | `UseAuthorization()` |

```csharp
app.UseAuthentication();     // 1) xác định danh tính → HttpContext.User
app.UseAuthorization();      // 2) kiểm tra quyền theo policy của endpoint     (thứ tự QUAN TRỌNG — Chương 3)
```

## Danh tính, claim, role

Danh tính trong .NET là **`ClaimsPrincipal`** chứa một hay nhiều **`ClaimsIdentity`**, mỗi identity là tập **claim** (khẳng định về người dùng: "tên là an", "vai trò là Admin", "tuổi là 20", "email…").

```csharp
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, "2"),
    new(ClaimTypes.Name, "an"),
    new(ClaimTypes.Role, "User"),
    new("tuoi", "20"),
};
var identity = new ClaimsIdentity(claims, authenticationType: "Cookies", ClaimTypes.Name, ClaimTypes.Role);
```

Trong endpoint: `ClaimsPrincipal user` (tham số handler) → `user.Identity.Name`, `user.IsInRole("Admin")`, `user.FindFirstValue("tuoi")`. **Role** chỉ là một loại claim đặc biệt (`ClaimTypes.Role`).

## Mật khẩu

Không lưu mật khẩu gốc; dùng băm chậm có muối (Chương 17):

```csharp
var bam = new PasswordHasher<object>();
string luu = bam.HashPassword(new object(), "Admin@123");
bam.VerifyHashedPassword(new object(), luu, matKhauNhap) != PasswordVerificationResult.Failed;
```

Hai chi tiết bảo mật đã có trong `TaiKhoanStore.XacThuc`:

- **Thông báo lỗi chung** "Tên đăng nhập hoặc mật khẩu không đúng" cho cả hai trường hợp (không tiết lộ tài khoản nào tồn tại).
- Khi tên đăng nhập **không tồn tại**, vẫn thực hiện một phép băm giả để **thời gian phản hồi giống nhau** (chống dò tài khoản qua *timing attack*).

## Xác thực bằng cookie (ứng dụng web có trình duyệt)

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.HttpOnly = true;                        // JavaScript không đọc được → XSS khó đánh cắp phiên
        o.Cookie.SameSite = SameSiteMode.Lax;            // giảm CSRF
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;   // production: chỉ gửi qua HTTPS
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        o.SlidingExpiration = true;                      // hoạt động thì gia hạn
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };   // API: 401 thay vì redirect
    });

// Đăng nhập
await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
// Đăng xuất
await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
```

Cách hoạt động: sau khi xác thực thành công, server **mã hoá và ký** danh tính vào một **cookie**; mỗi request sau trình duyệt tự gửi cookie, middleware giải mã dựng lại `User`. Không có "session id tra cứu trên server" (stateless, chỉ cần khoá bảo vệ dữ liệu — Data Protection, Chương 21).

Kết quả thử thực tế:

```
GET /toi (chưa đăng nhập)          → 401
POST /dang-nhap (sai mật khẩu)      → 401
POST /dang-nhap (an)                → 200, Set-Cookie
GET /toi                             → {"ten":"an","vaiTro":["User"],"tuoi":"20",...}
GET /quan-tri (an, role User)        → 403
POST /dang-xuat rồi GET /toi         → 401
```

Với cookie, form đăng nhập phải có **chống CSRF** (Chương 17) — mẫu tắt bằng `.DisableAntiforgery()` chỉ để thử bằng `curl`.

## Xác thực bằng JWT (API, mobile, SPA)

Khi client không phải trình duyệt dùng cookie (app di động, SPA gọi API khác miền, dịch vụ gọi dịch vụ), dùng **token** gửi trong header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**JWT** (JSON Web Token) là chuỗi 3 phần `header.payload.signature` (mỗi phần Base64URL): payload chứa claim (`sub`, `name`, `role`, `exp`...), chữ ký chứng minh **không bị sửa**. Server không lưu phiên: chỉ cần kiểm tra chữ ký và hạn.

```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,  ValidIssuer = "cuahang-api",
        ValidateAudience = true, ValidAudience = "cuahang-client",
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        ValidateIssuerSigningKey = true, IssuerSigningKey = khoaKy,
    });

// Cấp token sau khi xác thực thông tin đăng nhập
var token = new JwtSecurityToken(issuer: "cuahang-api", audience: "cuahang-client", claims: claims,
    expires: DateTime.UtcNow.AddMinutes(15),                               // NGẮN hạn
    signingCredentials: new SigningCredentials(khoaKy, SecurityAlgorithms.HmacSha256));
return new { accessToken = new JwtSecurityTokenHandler().WriteToken(token) };
```

```
GET /api/toi (không token)          → 401
GET /api/toi (token bị sửa 1 ký tự) → 401
GET /api/toi (token hợp lệ)          → {"ten":"an","vaiTro":["User"]}
GET /api/admin/bao-cao (token an)    → 403
GET /api/admin/bao-cao (token admin) → 200
```

Payload giải mã (Base64) của token thật:

```json
{"…/nameidentifier":"2","…/name":"an","tuoi":"20","…/role":"User","nbf":1789895649,"exp":1789896549,"iss":"cuahang-api","aud":"cuahang-client"}
```

> **Payload không được mã hoá, chỉ được ký** — ai cũng đọc được. Không đặt thông tin bí mật vào JWT.

Nguyên tắc JWT an toàn:

1. **Khoá ký** ≥ 256 bit, ngẫu nhiên, **lưu trong kho bí mật/biến môi trường** (Chương 6), không commit; đổi định kỳ. Với hệ thống nhiều dịch vụ dùng **chữ ký bất đối xứng** (RS256/ES256: dịch vụ cấp token giữ khoá riêng, các dịch vụ khác chỉ cần khoá công khai).
2. **Access token ngắn hạn** (5–15 phút) + **refresh token** dài hạn, lưu phía server (có thể thu hồi), xoay vòng mỗi lần dùng; hoặc dùng nhà cung cấp danh tính (bên dưới).
3. Luôn **kiểm tra `iss`, `aud`, `exp`**, ký hiệu thuật toán cố định (chống tấn công `alg: none`).
4. Nơi lưu token phía trình duyệt: `localStorage` dễ bị XSS đánh cắp; cookie `HttpOnly` an toàn hơn cho trình duyệt. Với web app thuần, **cookie thường là lựa chọn tốt hơn JWT**.
5. Token đã phát **không thu hồi được** trước khi hết hạn nếu chỉ dựa vào chữ ký — nên hạn ngắn, hoặc có danh sách thu hồi/`jti`.

## Cookie hay JWT?

| | Cookie | JWT Bearer |
|---|--------|-----------|
| Client | trình duyệt (tự động gửi) | bất kỳ (JS/mobile/dịch vụ) gắn header |
| Rủi ro chính | CSRF (đã có antiforgery/SameSite) | XSS đánh cắp token nếu lưu ở JS |
| Thu hồi | dễ (xoá phiên/đổi khoá) | khó (chờ hết hạn/danh sách đen) |
| Cross-domain | khó hơn | dễ |
| Hợp với | ứng dụng web server-render, Blazor | API công khai, mobile, microservice |

Có thể **dùng cả hai** trong một ứng dụng, mỗi endpoint chọn scheme: `RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Bearer" })` (nhóm `/api` trong mẫu).

## Phân quyền

`[Authorize]` / `.RequireAuthorization(...)` yêu cầu đăng nhập; thêm điều kiện:

```csharp
app.MapGet("/toi", ...).RequireAuthorization();                      // chỉ cần đăng nhập
app.MapGet("/quan-tri", ...).RequireAuthorization("QuanTri");        // theo policy
[Authorize(Roles = "Admin,Manager")]                                 // controller/Razor Pages: cách viết attribute
[AllowAnonymous]                                                     // ngoại lệ, cho phép ẩn danh
```

### Policy — quy tắc có tên

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("QuanTri", p => p.RequireRole("Admin"))
    .AddPolicy("TuoiTu18", p => p.RequireAssertion(ctx =>
        int.TryParse(ctx.User.FindFirstValue("tuoi"), out int tuoi) && tuoi >= 18))
    .AddPolicy("BearerAdmin", p => p.AddAuthenticationSchemes("Bearer").RequireRole("Admin"));
```

Policy dựa trên **role**, **claim** (`RequireClaim("department", "kho")`), điều kiện tuỳ ý (`RequireAssertion`) hoặc **requirement + handler** (bên dưới). Ưu tiên **policy có tên nghiệp vụ** thay vì rải `Roles = "Admin"` khắp nơi: đổi quy tắc một chỗ. Có thể đặt **chính sách dự phòng** cho mọi endpoint: `o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();` — mặc định *chặn hết*, phải `AllowAnonymous` tường minh (an toàn theo mặc định).

### Resource-based authorization — quyền phụ thuộc đối tượng

"Ai được xoá bài viết này?" — tuỳ **bài viết cụ thể** (tác giả hoặc admin), `[Authorize]` tĩnh không diễn tả được:

```csharp
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

// Trong endpoint: tải tài nguyên rồi hỏi
var kq = await auth.AuthorizeAsync(user, bv, new CungTacGiaRequirement());
if (!kq.Succeeded) return Results.Forbid();
```

Thử: An xoá bài của Admin → `403`; An xoá bài của An → `204`; Admin xoá bài của Admin → `204`. Đây là phòng thủ chính chống **IDOR** (Chương 21): *đừng chỉ kiểm tra "đã đăng nhập", phải kiểm tra "được truy cập đúng đối tượng này".*

## ASP.NET Core Identity

Tự viết đăng ký/đăng nhập như mẫu giúp hiểu cơ chế; **sản phẩm thật nên dùng ASP.NET Core Identity** — hệ thống thành viên đầy đủ:

- Lưu người dùng, vai trò, claim trong CSDL (qua EF Core): bảng `AspNetUsers`, `AspNetRoles`…
- Băm mật khẩu, chính sách mật khẩu, **khoá tài khoản** sau nhiều lần sai, xác nhận email, đổi/đặt lại mật khẩu, **xác thực hai yếu tố (2FA/TOTP)**, đăng nhập mạng xã hội (Google, Microsoft, GitHub).
- Giao diện có sẵn (scaffold) cho Razor Pages/MVC/Blazor; API endpoint có sẵn (`AddIdentityApiEndpoints<TUser>()`, `MapIdentityApi<TUser>()`) cho token/cookie.

```csharp
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddIdentityApiEndpoints<IdentityUser>().AddEntityFrameworkStores<AppDbContext>();
app.MapIdentityApi<IdentityUser>();       // /register, /login, /refresh, /manage/2fa, ...
```

## Dịch vụ danh tính bên ngoài: OpenID Connect / OAuth 2.0

Quản lý mật khẩu, MFA, khoá tài khoản, tuân thủ... là bài toán khó. Nhiều tổ chức giao cho **nhà cung cấp danh tính (IdP)** và dùng chuẩn **OpenID Connect** (lớp danh tính trên **OAuth 2.0**): **Microsoft Entra ID**, **Auth0**, **Keycloak**, **Duende IdentityServer**, Google… Ứng dụng của bạn chuyển hướng người dùng tới IdP, nhận **ID token/access token** về, chỉ cần `AddOpenIdConnect(...)` hoặc `AddJwtBearer(o => o.Authority = "https://idp...")`. Tập 3 trình bày chi tiết (flow Authorization Code + PKCE, refresh, scopes).

## Bảo vệ nhiều lớp

- **HTTPS** cho mọi thứ (Chương 21).
- Giới hạn tốc độ đăng nhập (Chương 21).
- Ghi log sự kiện bảo mật (đăng nhập thất bại, đổi quyền) — không ghi mật khẩu/token.
- Nguyên tắc **đặc quyền tối thiểu**; mặc định từ chối (fallback policy).
- Kiểm tra quyền ở **server** cho *mọi* thao tác (ẩn nút "Xoá" trên giao diện không phải là bảo mật).
- Blazor Server: `AuthorizeView`, `[Authorize]` trên component, nhưng vẫn kiểm tra ở tầng dịch vụ.

## Lỗi thường gặp

- Đặt `UseAuthorization` trước `UseAuthentication` → luôn 401/403 (Chương 3).
- Quên `UseAuthentication()` → `User` luôn ẩn danh.
- Nhầm `401` với `403`; redirect trang đăng nhập thay vì `401` cho API (cookie mặc định redirect).
- Khoá ký JWT yếu/viết cứng trong code/commit lên Git.
- Token sống quá lâu; không kiểm tra `aud/iss`.
- Đặt dữ liệu nhạy cảm vào JWT (không mã hoá).
- Chỉ kiểm tra "đã đăng nhập" mà không kiểm tra quyền sở hữu (IDOR).
- Lưu JWT ở `localStorage` trong ứng dụng có nguy cơ XSS.
- Thông báo đăng nhập tiết lộ "user không tồn tại".
- Tự viết mật mã/quản lý mật khẩu thay vì Identity/IdP.
- Cookie không `Secure`/`HttpOnly`/`SameSite` ở production.

## Bài tập

1. Thêm role `Manager` và policy `"QuanLyHoacAdmin"` (`RequireRole("Admin","Manager")`); gán cho một tài khoản mới và thử.
2. Đặt `FallbackPolicy` bắt buộc đăng nhập; thêm `.AllowAnonymous()` cho `/` và các endpoint đăng nhập.
3. Viết endpoint `POST /api/token/lam-moi` với refresh token lưu trong bộ nhớ (xoay vòng: mỗi refresh token dùng một lần).
4. Viết requirement `TuoiToiThieuRequirement(int tuoi)` + handler tái sử dụng cho nhiều policy.
5. Thêm `AddIdentityApiEndpoints` vào một dự án mới với EF Core SQLite và thử `/register`, `/login` bằng `curl`.
