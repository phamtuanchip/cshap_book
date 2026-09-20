using CuaHangApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CuaHangApi.Endpoints;

public static class DonHangEndpoints
{
    public static IEndpointRouteBuilder MapDonHang(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/don-hang").WithTags("Don hang");

        g.MapPost("/", async Task<Created<DonHangDto>> (TaoDonRequest req, DonHangService dv, CancellationToken ct) =>
        {
            var don = await dv.TaoDonAsync(req, ct);         // loi nghiep vu -> exception -> handler toan cuc (404/409/422)
            return TypedResults.Created($"/api/don-hang/{don.Id}", don);
        });

        g.MapGet("/{id:int}", async Task<Results<Ok<DonHangDto>, NotFound>> (int id, DonHangService dv, CancellationToken ct)
            => await dv.LayAsync(id, ct) is { } don ? TypedResults.Ok(don) : TypedResults.NotFound());

        return app;
    }
}
