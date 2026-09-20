using KienTruc.Application;
using KienTruc.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// COMPOSITION ROOT: noi DUY NHAT biet ca ba tang va noi day chung lai voi nhau
builder.Services.AddSingleton<ISanPhamRepository, InMemorySanPhamRepository>();
builder.Services.AddScoped<SanPhamService>();

var app = builder.Build();

app.MapGet("/api/san-pham", (SanPhamService dv, CancellationToken ct) => dv.DanhSachAsync(ct));
app.MapGet("/api/san-pham/{id:int}", async (int id, SanPhamService dv, CancellationToken ct)
    => await dv.LayAsync(id, ct) is { } s ? Results.Ok(s) : Results.NotFound());

app.Run();
