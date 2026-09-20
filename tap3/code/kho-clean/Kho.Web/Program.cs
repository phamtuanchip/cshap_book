using Kho.Application;
using Kho.Infrastructure;
using Kho.Infrastructure.Persistence;
using Kho.Web.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== COMPOSITION ROOT: noi duy nhat biet moi tang va noi chung lai =====
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Configuration.GetValue("MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<KhoDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();          // loi khong luong truoc -> 500 ProblemDetails (khong lo chi tiet)
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// "Ai dang thuc hien": demo lay tu header X-Nguoi; thuc te lay tu User.Identity (JWT/cookie - Chuong 12)
app.Use(async (ctx, next) =>
{
    ctx.RequestServices.GetRequiredService<KhoDbContext>().NguoiThucHien = ctx.Request.Headers["X-Nguoi"].FirstOrDefault() ?? "vo-danh";
    await next();
});
app.UseMiddleware<Kho.Web.IdempotencyMiddleware>();          // sau exception handler, truoc endpoint

app.MapHealthChecks("/health");
app.MapKho();
app.MapDonHang();
app.MapGet("/api/nhat-ky", async (KhoDbContext db, CancellationToken ct) =>
    (await db.NhatKy.OrderByDescending(n => n.Id).Take(50).ToListAsync(ct)).Select(n => new { n.Id, n.Luc, n.Nguoi, n.HanhDong, n.DoiTuong, n.Truoc, n.Sau }));

app.Run();

public partial class Program;
