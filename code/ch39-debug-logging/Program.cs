using Microsoft.Extensions.Logging;

// Cap do log doc tu bien moi truong LOG_LEVEL (mac dinh Information)
var capDo = Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("LOG_LEVEL"), true, out var l)
    ? l
    : LogLevel.Information;

using var factory = LoggerFactory.Create(builder => builder
    .SetMinimumLevel(capDo)
    .AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.IncludeScopes = true;
        o.TimestampFormat = "HH:mm:ss ";
    }));

ILogger<Program> log = factory.CreateLogger<Program>();

log.LogTrace("Rat chi tiet (Trace)");
log.LogDebug("Chi tiet cho lap trinh vien (Debug)");
log.LogInformation("Ung dung khoi dong, cap do log = {CapDo}", capDo);
log.LogWarning("Dung luong dia con thap: {PhanTram}%", 8);

// Structured logging: tham so dat ten {Ten}, KHONG dung noi suy chuoi $"..."
string nguoiDung = "an";
int soLuong = 3;
log.LogInformation("Nguoi dung {NguoiDung} them {SoLuong} san pham vao gio", nguoiDung, soLuong);

// Log kem exception
var dv = new DichVuTinhToan(factory.CreateLogger<DichVuTinhToan>());
Console.WriteLine($"Tong = {dv.TinhTong([1, 2, 3])}");
Console.WriteLine($"Chia = {dv.Chia(10, 2)}");
try
{
    dv.Chia(1, 0);
}
catch (DivideByZeroException)
{
    Console.WriteLine("(da bat loi chia cho 0, xem log o tren)");
}

// Log scope: gan ngu canh chung cho nhieu dong log
using (log.BeginScope("RequestId={RequestId}", Guid.NewGuid().ToString("N")[..8]))
{
    log.LogInformation("Bat dau xu ly yeu cau");
    log.LogInformation("Ket thuc xu ly yeu cau");
}

// Debug.Assert / Conditional: chi chay o Debug
System.Diagnostics.Debug.Assert(soLuong > 0, "soLuong phai duong");

class DichVuTinhToan(ILogger<DichVuTinhToan> log)
{
    public int TinhTong(int[] so)
    {
        log.LogDebug("TinhTong nhan {Dem} phan tu", so.Length);
        int tong = 0;
        foreach (var x in so) tong += x;   // dat breakpoint o day de thuc hanh debug
        return tong;
    }

    public int Chia(int a, int b)
    {
        try
        {
            return a / b;
        }
        catch (DivideByZeroException e)
        {
            log.LogError(e, "Khong the chia {A} cho {B}", a, b);
            throw;
        }
    }
}
