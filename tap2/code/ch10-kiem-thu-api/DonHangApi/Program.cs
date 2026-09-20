using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDonHangStore, DonHangStore>();
builder.Services.AddScoped<ICongThanhToan, CongThanhToanMo>();        // trong test se duoc THAY bang ban gia
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapGet("/", () => "Don Hang API dang chay");

var api = app.MapGroup("/api/don-hang").AddEndpointFilter<ApiKeyFilter>();

api.MapGet("/", (IDonHangStore store) => TypedResults.Ok(store.TatCa()));

api.MapGet("/{id:int}", Results<Ok<DonHang>, NotFound> (int id, IDonHangStore store)
    => store.Tim(id) is { } d ? TypedResults.Ok(d) : TypedResults.NotFound());

api.MapPost("/", async Task<Results<Created<DonHang>, ValidationProblem, ProblemHttpResult>> (
    TaoDonHangRequest req, ICongThanhToan thanhToan, IDonHangStore store, CancellationToken ct) =>
{
    var loi = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(req.KhachHang)) loi["khachHang"] = ["Ten khach hang la bat buoc"];
    if (req.SoTien <= 0) loi["soTien"] = ["So tien phai lon hon 0"];
    if (loi.Count > 0) return TypedResults.ValidationProblem(loi);

    var kq = await thanhToan.ThanhToanAsync(req.SoTien, ct);          // goi dich vu ben ngoai
    if (!kq.ThanhCong)
        return TypedResults.Problem(title: "Thanh toan that bai", detail: kq.LyDo, statusCode: StatusCodes.Status402PaymentRequired);

    var don = store.Them(req.KhachHang!.Trim(), req.SoTien, kq.MaGiaoDich!);
    return TypedResults.Created($"/api/don-hang/{don.Id}", don);
});

app.Run();

// Cho phep project test tham chieu lop Program (top-level statements tao lop an, internal)
public partial class Program;

// ---------------- mo hinh & dich vu ----------------
public record DonHang(int Id, string KhachHang, decimal SoTien, string MaGiaoDich);
public record TaoDonHangRequest(string? KhachHang, decimal SoTien);
public record KetQuaThanhToan(bool ThanhCong, string? MaGiaoDich, string? LyDo);

public interface ICongThanhToan
{
    Task<KetQuaThanhToan> ThanhToanAsync(decimal soTien, CancellationToken ct);
}

// Ban "that" (mo phong): trong san pham that se goi cong thanh toan ben ngoai qua mang
public class CongThanhToanMo : ICongThanhToan
{
    public async Task<KetQuaThanhToan> ThanhToanAsync(decimal soTien, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        return soTien > 100_000_000m
            ? new KetQuaThanhToan(false, null, "Vuot han muc giao dich")
            : new KetQuaThanhToan(true, $"GD-{Guid.NewGuid():N}"[..11], null);
    }
}

public interface IDonHangStore
{
    IReadOnlyList<DonHang> TatCa();
    DonHang? Tim(int id);
    DonHang Them(string khachHang, decimal soTien, string maGd);
}

public class DonHangStore : IDonHangStore
{
    private readonly Lock _khoa = new();
    private readonly List<DonHang> _ds = [];
    private int _id = 1;

    public IReadOnlyList<DonHang> TatCa() { lock (_khoa) return [.. _ds]; }
    public DonHang? Tim(int id) { lock (_khoa) return _ds.FirstOrDefault(d => d.Id == id); }

    public DonHang Them(string khachHang, decimal soTien, string maGd)
    {
        lock (_khoa)
        {
            var d = new DonHang(_id++, khachHang, soTien, maGd);
            _ds.Add(d);
            return d;
        }
    }
}

// Yeu cau API key trong header X-Api-Key (khoa lay tu cau hinh "ApiKey")
public class ApiKeyFilter(IConfiguration cfg) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        string khoaDung = cfg["ApiKey"] ?? "khoa-thu-nghiem";
        if (!ctx.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var khoa) || khoa != khoaDung)
            return TypedResults.Problem(title: "Chua xac thuc", statusCode: StatusCodes.Status401Unauthorized);
        return await next(ctx);
    }
}
