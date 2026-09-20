using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using QuanLyKhoWeb.Services;

namespace QuanLyKhoWeb.Endpoints;

public record DangNhapRequest([property: Required] string? TenDangNhap, [property: Required] string? MatKhau);

public record TaoSanPhamRequest
{
    [Required, RegularExpression("^[A-Z]{2}[0-9]{3}$", ErrorMessage = "Ma gom 2 chu HOA + 3 so")] public string? Ma { get; init; }
    [Required, StringLength(200, MinimumLength = 2)] public string? Ten { get; init; }
    [Required, StringLength(100, MinimumLength = 2)] public string? Nhom { get; init; }
    [Range(0, 1_000_000_000)] public decimal DonGia { get; init; }
    [Range(0, 1_000_000)] public int TonDau { get; init; }
}

public record SoLuongRequest
{
    [Range(1, 100_000, ErrorMessage = "So luong tu 1 den 100000")] public int SoLuong { get; init; }
    [StringLength(200)] public string? GhiChu { get; init; }
}

public static class KhoEndpoints
{
    public static void MapKho(this WebApplication app)
    {
        // ----- Dang nhap: cong khai, gioi han toc do (chong doan mat khau) -----
        app.MapPost("/api/dang-nhap", (DangNhapRequest req, TaiKhoanService tk, PhatHanhToken phat) =>
        {
            var t = tk.XacThuc(req.TenDangNhap!, req.MatKhau!);
            if (t is null) return Results.Problem("Ten dang nhap hoac mat khau khong dung", statusCode: 401);
            var (token, het) = phat.Tao(t);
            return Results.Ok(new { accessToken = token, loai = "Bearer", hetHan = het, vaiTro = t.VaiTro });
        }).AllowAnonymous().RequireRateLimiting("dang-nhap").AddEndpointFilter<KiemTraFilter<DangNhapRequest>>().WithTags("Xac thuc");

        var g = app.MapGroup("/api").WithTags("Kho");     // mac dinh: FallbackPolicy bat buoc dang nhap

        // ----- Doc: moi nguoi dung da dang nhap -----
        g.MapGet("/san-pham", async (KhoService kho, string? tim, string? nhom, CancellationToken ct, int trang = 1, int kichThuoc = 10)
            => TypedResults.Ok(await kho.TimAsync(tim, nhom, trang, kichThuoc, ct)));

        g.MapGet("/san-pham/{id:int}", async Task<Results<Ok<SanPhamDto>, NotFound>> (int id, KhoService kho, CancellationToken ct)
            => await kho.LayAsync(id, ct) is { } sp ? TypedResults.Ok(sp) : TypedResults.NotFound());

        g.MapGet("/bao-cao/nhom", async (KhoService kho, CancellationToken ct) => TypedResults.Ok(await kho.BaoCaoTheoNhomAsync(ct)));
        g.MapGet("/bao-cao/sap-het", async (KhoService kho, CancellationToken ct) => TypedResults.Ok(await kho.SapHetAsync(ct)));
        g.MapGet("/giao-dich", async (KhoService kho, string? ma, CancellationToken ct, int toiDa = 50)
            => TypedResults.Ok(await kho.LichSuAsync(ma, toiDa, ct)));

        // ----- Ghi: chi NhanVien/Admin -----
        g.MapPost("/san-pham/{id:int}/nhap", async (int id, SoLuongRequest req, KhoService kho, ClaimsPrincipal user, CancellationToken ct)
            => TypedResults.Ok(await kho.NhapAsync(id, req.SoLuong, req.GhiChu, user.Identity!.Name!, ct)))
            .RequireAuthorization("GhiKho").AddEndpointFilter<KiemTraFilter<SoLuongRequest>>();

        g.MapPost("/san-pham/{id:int}/xuat", async (int id, SoLuongRequest req, KhoService kho, ClaimsPrincipal user, CancellationToken ct)
            => TypedResults.Ok(await kho.XuatAsync(id, req.SoLuong, req.GhiChu, user.Identity!.Name!, ct)))
            .RequireAuthorization("GhiKho").AddEndpointFilter<KiemTraFilter<SoLuongRequest>>();

        // ----- Quan tri: chi Admin -----
        g.MapPost("/san-pham", async (TaoSanPhamRequest req, KhoService kho, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var sp = await kho.ThemAsync(req.Ma!, req.Ten!, req.Nhom!, req.DonGia, req.TonDau, user.Identity!.Name!, ct);
            return TypedResults.Created($"/api/san-pham/{sp.Id}", sp);
        }).RequireAuthorization("QuanTri").AddEndpointFilter<KiemTraFilter<TaoSanPhamRequest>>();
    }
}

// Validation dung chung (Chuong 8, 14)
public class KiemTraFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var doiTuong = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (doiTuong is not null)
        {
            var kq = new List<ValidationResult>();
            if (!Validator.TryValidateObject(doiTuong, new ValidationContext(doiTuong), kq, true))
                return TypedResults.ValidationProblem(kq
                    .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : [""]).Select(m => (m, r.ErrorMessage ?? "Khong hop le")))
                    .GroupBy(x => x.m).ToDictionary(x => x.Key, x => x.Select(y => y.Item2).ToArray()));
        }
        return await next(ctx);
    }
}
