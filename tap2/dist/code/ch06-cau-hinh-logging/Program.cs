using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 1. Options pattern + kiem tra hop le NGAY KHI KHOI DONG (loi cau hinh -> khong cho chay)
builder.Services
    .AddOptions<CuaHangOptions>()
    .Bind(builder.Configuration.GetSection(CuaHangOptions.TenMuc))
    .ValidateDataAnnotations()
    .Validate(o => o.PhiVanChuyenToiThieu <= o.PhiVanChuyenToiDa, "Phi toi thieu khong duoc lon hon phi toi da")
    .ValidateOnStart();

var app = builder.Build();

// 2. Doc cau hinh: IConfiguration (chuoi tho) va IOptions<T> (co kieu)
app.MapGet("/cau-hinh", (IConfiguration cfg, IOptions<CuaHangOptions> opt) => new
{
    MoiTruong = app.Environment.EnvironmentName,
    TenRaw = cfg["CuaHang:Ten"],
    Options = opt.Value,
    ChuoiKetNoi = cfg.GetConnectionString("Mac_Dinh") is { } cs ? "(da cau hinh, khong in ra)" : "(chua co)",
});

// 3. IOptionsSnapshot (Scoped): doc lai cau hinh moi request; IOptionsMonitor (Singleton): theo doi thay doi file
app.MapGet("/cau-hinh/monitor", (IOptionsMonitor<CuaHangOptions> monitor) => new { monitor.CurrentValue.Ten, monitor.CurrentValue.ThueVat });

// 4. Logging
app.MapGet("/log", (ILogger<Program> log, string? nguoiDung) =>
{
    log.LogTrace("Trace");
    log.LogDebug("Debug: nguoi dung {NguoiDung}", nguoiDung);
    log.LogInformation("Information: nguoi dung {NguoiDung} vua goi /log", nguoiDung ?? "an danh");
    log.LogWarning("Warning: canh bao mau");
    log.LogError(new InvalidOperationException("loi mau"), "Error: mot loi minh hoa");
    return "Da ghi log - xem cua so terminal";
});

// 5. Source-generated logging: nhanh hon, kiem tra kieu luc bien dich
app.MapGet("/log-nhanh", (ILogger<Program> log, int soDon) =>
{
    log.DatHangThanhCong(soDon, 250_000m);
    return "ok";
});

// 6. Scope: gan ngu canh chung cho moi dong log trong khoi
app.MapGet("/log-scope", (ILogger<Program> log) =>
{
    using (log.BeginScope("CorrelationId={CorrelationId}", Guid.NewGuid().ToString("N")[..8]))
    {
        log.LogInformation("Bat dau");
        log.LogInformation("Ket thuc");
    }
    return "ok";
});

app.Run();

class CuaHangOptions
{
    public const string TenMuc = "CuaHang";

    [Required, MinLength(2)]
    public string Ten { get; set; } = "";

    [Range(0, 1)]
    public decimal ThueVat { get; set; }

    [Range(0, 1_000_000)]
    public decimal PhiVanChuyenToiThieu { get; set; }

    [Range(0, 1_000_000)]
    public decimal PhiVanChuyenToiDa { get; set; }

    public List<string> KenhHoTro { get; set; } = [];
}

static partial class NhatKy
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Dat hang thanh cong: don {SoDon}, tong {TongTien}")]
    public static partial void DatHangThanhCong(this ILogger logger, int soDon, decimal tongTien);
}
