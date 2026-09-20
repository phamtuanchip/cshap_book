using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// Moi truong: DOTNET_ENVIRONMENT (mac dinh Production)
string moiTruong = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

// Thu tu them nguon = do uu tien tang dan: nguon SAU ghi de nguon TRUOC
IConfiguration cauHinh = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{moiTruong}.json", optional: true)   // ghi de theo moi truong
    .AddUserSecrets<Program>(optional: true)                          // bi mat khi phat trien
    .AddEnvironmentVariables()                                        // bien moi truong: thang nhat
    .Build();

Console.WriteLine($"Moi truong: {moiTruong}");

// 1. Doc gia tri don le (khoa long nhau dung dau ':')
Console.WriteLine($"CuaHang:Ten = {cauHinh["CuaHang:Ten"]}");
int soNgay = cauHinh.GetValue<int>("CuaHang:SoNgayDoiTra", 7);
Console.WriteLine($"So ngay doi tra = {soNgay}");

// 2. Bind vao doi tuong co kieu (khuyen nghi)
var cuaHang = cauHinh.GetSection("CuaHang").Get<CuaHangOptions>() ?? new();
Console.WriteLine($"Ten: {cuaHang.Ten}, thue VAT: {cuaHang.ThueVat:P0}, ho tro: {string.Join(", ", cuaHang.KenhHoTro)}");

// 3. Options pattern: IOptions<T> (dung khi lam viec voi DI - Tap 2)
IOptions<CuaHangOptions> options = Options.Create(cuaHang);
Console.WriteLine($"Qua IOptions: {options.Value.Ten}");

// 4. Chuoi ket noi va bi mat: KHONG commit mat khau len Git!
string? matKhau = cauHinh["KetNoi:MatKhau"];
Console.WriteLine($"Mat khau da cau hinh? {(string.IsNullOrEmpty(matKhau) ? "chua (dung user-secrets hoac bien moi truong)" : "co (khong in ra man hinh)")}");

class CuaHangOptions
{
    public string Ten { get; set; } = "";
    public double ThueVat { get; set; }
    public int SoNgayDoiTra { get; set; }
    public List<string> KenhHoTro { get; set; } = [];
}
