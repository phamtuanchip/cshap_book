using CuaHangApi;
using CuaHangApi.Data;
using CuaHangApi.Endpoints;
using CuaHangApi.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext dang ky Scoped (mac dinh cua AddDbContext): mot DbContext cho moi HTTP request
builder.Services.AddDbContext<CuaHangDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("CuaHang") ?? "Data Source=cuahang.db"));

builder.Services.AddScoped<DonHangService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<LoiNghiepVuHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Ap dung migration va nap du lieu mau luc khoi dong (dung cho phat trien/demo; production thuong chay migration trong pipeline deploy)
if (app.Configuration.GetValue("MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CuaHangDbContext>();
    await db.Database.MigrateAsync();
    await DuLieuMau.NapAsync(db);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapGet("/", () => "Cua Hang API (EF Core + SQLite). Thu: /api/san-pham?trang=1&kichThuoc=3&sapXep=-gia");
app.MapSanPham();
app.MapDonHang();

app.Run();

public partial class Program;

// ---------------- xu ly loi toan cuc (Chuong 9) ----------------
public class LoiNghiepVuHandler(IProblemDetailsService problemDetails, ILogger<LoiNghiepVuHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception e, CancellationToken ct)
    {
        int status = e switch
        {
            KhongTimThayException => StatusCodes.Status404NotFound,
            XungDotException or KhongDuHangException => StatusCodes.Status409Conflict,
            LoiNghiepVu => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };
        if (status == 500) log.LogError(e, "Loi khong xu ly");

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

// ---------------- du lieu mau ----------------
public static class DuLieuMau
{
    public static async Task NapAsync(CuaHangDbContext db)
    {
        if (await db.Nhoms.AnyAsync()) return;

        var phuKien = new Nhom { Ten = "Phu kien" };
        var thietBi = new Nhom { Ten = "Thiet bi" };
        db.SanPhams.AddRange(
            new SanPham { Ma = "CH001", Ten = "Chuot khong day", Gia = 150_000, Ton = 30, Nhom = phuKien },
            new SanPham { Ma = "BP001", Ten = "Ban phim co", Gia = 500_000, Ton = 12, Nhom = phuKien },
            new SanPham { Ma = "TN001", Ten = "Tai nghe", Gia = 200_000, Ton = 0, Nhom = phuKien },
            new SanPham { Ma = "MH001", Ten = "Man hinh 24 inch", Gia = 3_500_000, Ton = 5, Nhom = thietBi },
            new SanPham { Ma = "LT001", Ten = "Laptop Dell", Gia = 18_000_000, Ton = 3, Nhom = thietBi });
        db.KhachHangs.AddRange(
            new KhachHang { Ten = "Nguyen An", Email = "an@example.com" },
            new KhachHang { Ten = "Tran Binh", Email = "binh@example.com" });
        await db.SaveChangesAsync();
    }
}
