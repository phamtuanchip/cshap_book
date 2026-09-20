using Kho.Application.DonHangs;
using Kho.Application.Messaging;

namespace Kho.Web.Endpoints;

public record DatHangRequest(string KhachHang, List<DongDatHang> Dong);
public record HuyDonRequest(string? LyDo);

public static class DonHangEndpoints
{
    public static void MapDonHang(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/don-hang").WithTags("Don hang");

        g.MapPost("/", async (DatHangRequest r, ISender sender, CancellationToken ct) =>
        {
            var kq = await sender.Send(new DatHangCommand(r.KhachHang, r.Dong ?? []), ct);
            return kq.ThanhCong ? Results.Created($"/api/don-hang/{kq.GiaTri}", new { ma = kq.GiaTri }) : kq.ToHttp();
        });

        g.MapGet("/{ma}", async (string ma, ISender sender, CancellationToken ct)
            => (await sender.Send(new LayDonHangQuery(ma), ct)).ToHttp());

        g.MapPost("/{ma}/huy", async (string ma, HuyDonRequest r, ISender sender, CancellationToken ct)
            => (await sender.Send(new HuyDonHangCommand(ma, r.LyDo), ct)).ToHttp(noContent: true));
    }
}
