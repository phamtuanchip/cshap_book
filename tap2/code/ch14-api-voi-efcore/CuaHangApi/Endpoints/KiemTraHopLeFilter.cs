using System.ComponentModel.DataAnnotations;

namespace CuaHangApi.Endpoints;

// Endpoint filter chung: kiem tra DataAnnotations cho tham so kieu T (Minimal API khong tu lam - Chuong 8)
public class KiemTraHopLeFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var doiTuong = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (doiTuong is not null)
        {
            var ketQua = new List<ValidationResult>();
            if (!Validator.TryValidateObject(doiTuong, new ValidationContext(doiTuong), ketQua, validateAllProperties: true))
            {
                var loi = ketQua
                    .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : [""]).Select(m => (Truong: m, Loi: r.ErrorMessage ?? "Khong hop le")))
                    .GroupBy(x => x.Truong)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Loi).ToArray());
                return TypedResults.ValidationProblem(loi);
            }
        }
        return await next(ctx);
    }
}
