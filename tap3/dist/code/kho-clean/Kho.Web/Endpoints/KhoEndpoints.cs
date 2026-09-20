using Kho.Application.Abstractions;
using Kho.Application.Messaging;
using Kho.Application.SanPhams;
using Kho.Domain.Common;

namespace Kho.Web.Endpoints;

public record ThemSanPhamRequest(string Ma, string Ten, string Nhom, decimal DonGia, int TonDau);
public record SoLuongRequest(int SoLuong);

public static class KhoEndpoints
{
    public static void MapKho(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/san-pham").WithTags("San pham");

        // Endpoint MONG: nhan HTTP -> tao lenh/truy van -> gui qua mediator -> dich Result thanh HTTP. Khong co nghiep vu o day.
        g.MapGet("/", async (ISender sender, string? tim, string? nhom, CancellationToken ct, int trang = 1, int kichThuoc = 10)
            => (await sender.Send(new TimSanPhamQuery(tim, nhom, trang, kichThuoc), ct)).ToHttp());

        g.MapGet("/{ma}", async (string ma, ISender sender, CancellationToken ct)
            => (await sender.Send(new LaySanPhamQuery(ma), ct)).ToHttp());

        g.MapPost("/", async (ThemSanPhamRequest r, ISender sender, CancellationToken ct) =>
        {
            var kq = await sender.Send(new ThemSanPhamCommand(r.Ma, r.Ten, r.Nhom, r.DonGia, r.TonDau), ct);
            return kq.ThanhCong ? Results.Created($"/api/san-pham/{kq.GiaTri}", new { ma = kq.GiaTri }) : kq.ToHttp();
        });

        g.MapPost("/{ma}/nhap", async (string ma, SoLuongRequest r, ISender sender, CancellationToken ct)
            => (await sender.Send(new NhapKhoCommand(ma, r.SoLuong), ct)).ToHttp(noContent: true));

        g.MapPost("/{ma}/xuat", async (string ma, SoLuongRequest r, ISender sender, CancellationToken ct)
            => (await sender.Send(new XuatKhoCommand(ma, r.SoLuong), ct)).ToHttp(noContent: true));
    }
}

public static class KetQuaHttp
{
    // Diem DUY NHAT dich "ngon ngu nghiep vu" (LoaiLoi) sang "ngon ngu HTTP" (ma trang thai)
    public static IResult ToHttp<T>(this Result<T> kq) => kq.ThanhCong ? Results.Ok(kq.GiaTri) : LoiSangHttp(kq.Loi!);

    public static IResult ToHttp(this Result kq, bool noContent = false)
        => kq.ThanhCong ? (noContent ? Results.NoContent() : Results.Ok()) : LoiSangHttp(kq.Loi!);

    private static IResult LoiSangHttp(Loi loi) => Results.Problem(
        title: loi.Ma,
        detail: loi.MoTa,
        statusCode: loi.Loai switch
        {
            LoaiLoi.DuLieuKhongHopLe => StatusCodes.Status400BadRequest,
            LoaiLoi.KhongTimThay => StatusCodes.Status404NotFound,
            LoaiLoi.XungDot => StatusCodes.Status409Conflict,
            LoaiLoi.NghiepVu => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        });
}
