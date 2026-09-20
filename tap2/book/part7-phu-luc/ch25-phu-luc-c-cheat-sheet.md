# Phụ lục C — Cheat sheet ASP.NET Core

## C.1 Lệnh

```
dotnet new web -n App      dotnet new webapi     dotnet new mvc     dotnet new razor     dotnet new blazor
dotnet run [--urls http://localhost:5000]    dotnet watch run     dotnet publish -c Release
dotnet dev-certs https --trust
dotnet user-secrets set "K" "V"
dotnet ef migrations add Ten      dotnet ef database update      dotnet ef migrations script      dotnet ef migrations remove
dotnet list package --vulnerable --include-transitive      dotnet list package --outdated
```

## C.2 `Program.cs` khung

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();               // hoặc AddControllersWithViews / AddRazorPages / AddRazorComponents
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddScoped<IDichVu, DichVu>();
builder.Services.AddOpenApi();  builder.Services.AddProblemDetails();  builder.Services.AddExceptionHandler<MyHandler>();
builder.Services.AddAuthentication().AddJwtBearer();  builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(...);  builder.Services.AddCors(...);  builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();  app.UseHsts(); app.UseHttpsRedirection();
app.UseStaticFiles();  app.UseRouting();  app.UseRateLimiter();  app.UseCors();
app.UseAuthentication();  app.UseAuthorization();  app.UseAntiforgery();

app.MapGet("/", () => "ok");  app.MapControllers();  app.MapRazorPages();  app.MapHealthChecks("/health");
app.Run();
public partial class Program;
```

## C.3 Minimal API

```csharp
var g = app.MapGroup("/api/todos").WithTags("Todos").RequireAuthorization();
g.MapGet("/", (AppDb db, string? tim, int trang = 1) => ...);
g.MapGet("/{id:int}", Results<Ok<Todo>, NotFound> (int id, AppDb db) => ...);
g.MapPost("/", (TaoTodo req, AppDb db) => TypedResults.Created($"/api/todos/{id}", todo));
g.MapPut("/{id:int}", ...);  g.MapDelete("/{id:int}", ...);
.AddEndpointFilter<MyFilter>()   .RequireAuthorization("Policy")   .AllowAnonymous()   .WithName("X")   .DisableAntiforgery()
```

Binding: route `{id}`, query (kiểu đơn giản), body (kiểu phức tạp), `[FromHeader]`, `[FromForm]`, `[FromServices]`, `[AsParameters]`, `CancellationToken`, `HttpContext`, `ClaimsPrincipal`.
Kết quả: `TypedResults.Ok / Created / NoContent / NotFound / BadRequest / Conflict / ValidationProblem / Problem / File / Redirect`.

## C.4 Controller

```csharp
[ApiController] [Route("api/[controller]")]
public class XController(IDichVu dv) : ControllerBase
{
    [HttpGet("{id:int}")] public async Task<ActionResult<Dto>> Get(int id, CancellationToken ct) => ...;
    [HttpPost] public async Task<ActionResult<Dto>> Post(TaoDto req) => CreatedAtAction(nameof(Get), new { id }, dto);
}
```

## C.5 Razor Pages / MVC / Blazor

```cshtml
@page "{id:int}"                       @model XModel        @* Razor Page *@
<form method="post"> <input asp-for="Input.Ten" /> <span asp-validation-for="Input.Ten"></span> </form>
<a asp-page="Details" asp-route-id="@x.Id">…</a>       <a asp-controller="X" asp-action="Y">…</a>
```
```csharp
[BindProperty] public Input Input { get; set; }        public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); ...; return RedirectToPage("X"); }
```
```razor
@page "/x"  @rendermode InteractiveServer  @inject IDichVu Dv
<button @onclick="Tang">@so</button>  <input @bind="ten" @bind:event="oninput" />
<EditForm Model="m" OnValidSubmit="Luu"><DataAnnotationsValidator /><InputText @bind-Value="m.Ten" /></EditForm>
@code { int so; void Tang() => so++; [Parameter] public int Id { get; set; } [Parameter] public EventCallback<int> OnX { get; set; } }
```

## C.6 DI

```csharp
AddTransient<I, C>()   AddScoped<I, C>()   AddSingleton<I, C>()   AddKeyedScoped<I, C>("khoa")   AddHttpClient<TClient>()
AddOptions<T>().Bind(cfg.GetSection("X")).ValidateDataAnnotations().ValidateOnStart()
IOptions<T> (cố định) · IOptionsSnapshot<T> (mỗi request) · IOptionsMonitor<T> (theo dõi thay đổi)
```
Quy tắc: dịch vụ sống lâu **không** phụ thuộc dịch vụ sống ngắn (Singleton ↛ Scoped). `DbContext` luôn Scoped.

## C.7 EF Core

```csharp
db.Set.Add(x); await db.SaveChangesAsync();                 db.Set.Remove(x);
await db.Set.AsNoTracking().Where(...).OrderBy(...).Select(x => new Dto(...)).ToListAsync(ct);
await db.Set.Where(...).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ton, x => x.Ton - n), ct);   await ...ExecuteDeleteAsync();
.Include(x => x.Con).ThenInclude(...)   .AsSplitQuery()   .IgnoreQueryFilters()   EF.Functions.Like(x.Ten, $"%{s}%")
await using var tx = await db.Database.BeginTransactionAsync(); ... await tx.CommitAsync();
e.HasIndex(x => x.Ma).IsUnique();  e.HasOne(x => x.A).WithMany(a => a.Bs).HasForeignKey(x => x.AId);  e.HasQueryFilter(x => !x.DaXoa);  e.Property(x => x.V).IsConcurrencyToken();
```

## C.8 Xác thực / phân quyền

```csharp
app.UseAuthentication(); app.UseAuthorization();                // đúng thứ tự
AddAuthorizationBuilder().SetFallbackPolicy(...RequireAuthenticatedUser()).AddPolicy("P", p => p.RequireRole("Admin"));
.RequireAuthorization("P")   [Authorize(Roles="A,B")]   [AllowAnonymous]
await auth.AuthorizeAsync(user, resource, new MyRequirement());  // resource-based
ctx.SignInAsync(scheme, principal)   ctx.SignOutAsync(scheme)
```

## C.9 Mã trạng thái nên dùng

| Tình huống | Mã |
|-----------|----|
| Đọc thành công | `200` |
| Tạo mới | `201` + `Location` |
| Thành công không body | `204` |
| Dữ liệu gửi sai | `400` (ValidationProblem) |
| Chưa xác thực / token sai | `401` |
| Không đủ quyền | `403` |
| Không tồn tại (hoặc không được biết là tồn tại) | `404` |
| Xung đột (trùng mã, hết hàng, phiên bản) | `409` |
| Dữ liệu hợp lệ về cú pháp nhưng sai nghiệp vụ | `422` |
| Quá giới hạn tốc độ | `429` + `Retry-After` |
| Lỗi máy chủ | `500` |
| Dịch vụ ngoài lỗi / chậm | `502` / `504` |
| Quá tải/bảo trì | `503` |

## C.10 Danh sách kiểm tra nhanh

- [ ] Pipeline đúng thứ tự (lỗi → HTTPS → static → routing → CORS → auth → endpoint).
- [ ] DTO vào ≠ ra ≠ entity; validation ở server; PRG với form; antiforgery.
- [ ] SQL luôn tham số hoá; không N+1; có phân trang và giới hạn kích thước.
- [ ] Fallback policy bắt đăng nhập; kiểm tra quyền sở hữu; rate limit đăng nhập.
- [ ] Bí mật ngoài Git; HTTPS; header bảo mật; log không lộ dữ liệu nhạy cảm.
- [ ] Test tích hợp cho luồng chính và nhánh lỗi; `dotnet list package --vulnerable`.
