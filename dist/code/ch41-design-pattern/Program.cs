// ============ 1. SINGLETON ============
Console.WriteLine("=== Singleton ===");
var c1 = CauHinhUngDung.Instance;
var c2 = CauHinhUngDung.Instance;
Console.WriteLine($"Cung mot doi tuong? {ReferenceEquals(c1, c2)}");

// ============ 2. FACTORY ============
Console.WriteLine("\n=== Factory ===");
foreach (var loai in new[] { "pdf", "excel", "csv" })
{
    IXuatBaoCao xuat = XuatBaoCaoFactory.Tao(loai);
    Console.WriteLine(xuat.Xuat("Doanh thu Q1"));
}
try { XuatBaoCaoFactory.Tao("docx"); }
catch (NotSupportedException e) { Console.WriteLine($"Loi: {e.Message}"); }

// ============ 3. STRATEGY ============
Console.WriteLine("\n=== Strategy ===");
decimal tienHang = 1_000_000m;
IChinhSachGiamGia[] chinhSach = [new KhongGiam(), new GiamPhanTram(10), new GiamCoDinh(50_000), new GiamKhachVip()];
foreach (var cs in chinhSach)
    Console.WriteLine($"{cs.Ten,-18}: {cs.TinhTien(tienHang):N0}");

// Strategy voi delegate: gon hon khi chi la mot ham
var donHang = new DonHang(tienHang, tien => tien * 0.85m);
Console.WriteLine($"Delegate (giam 15%): {donHang.TongCong():N0}");

// ============ 4. OBSERVER ============
Console.WriteLine("\n=== Observer ===");
var cuaHang = new CuaHangGiaCoPhieu();
cuaHang.GiaThayDoi += (s, e) => Console.WriteLine($"[Bang dien] {e.Ma}: {e.GiaCu} -> {e.GiaMoi}");
cuaHang.GiaThayDoi += (s, e) =>
{
    if (e.GiaMoi > e.GiaCu * 1.05m) Console.WriteLine($"[Canh bao] {e.Ma} tang manh!");
};
cuaHang.DatGia("FPT", 100m);
cuaHang.DatGia("FPT", 108m);

// ============ 5. DECORATOR ============
Console.WriteLine("\n=== Decorator ===");
IThongBao tb = new ThongBaoConsole();
tb = new ThongBaoCoThoiGian(tb);
tb = new ThongBaoVietHoa(tb);
tb.Gui("he thong da san sang");

// ---------------- Cai dat ----------------

// Singleton: mot the hien duy nhat. Thuc te trong ASP.NET Core dung DI lifetime "Singleton" thay vi tu viet.
sealed class CauHinhUngDung
{
    private static readonly Lazy<CauHinhUngDung> _instance = new(() => new CauHinhUngDung());
    public static CauHinhUngDung Instance => _instance.Value;   // Lazy<T>: an toan da luong, khoi tao khi can

    private CauHinhUngDung() { }   // cam new tu ben ngoai
}

// Factory: dong goi viec chon lop cu the
interface IXuatBaoCao { string Xuat(string tieuDe); }
class XuatPdf : IXuatBaoCao { public string Xuat(string t) => $"[PDF] {t}"; }
class XuatExcel : IXuatBaoCao { public string Xuat(string t) => $"[XLSX] {t}"; }
class XuatCsv : IXuatBaoCao { public string Xuat(string t) => $"[CSV] {t}"; }

static class XuatBaoCaoFactory
{
    public static IXuatBaoCao Tao(string loai) => loai.ToLowerInvariant() switch
    {
        "pdf" => new XuatPdf(),
        "excel" => new XuatExcel(),
        "csv" => new XuatCsv(),
        _ => throw new NotSupportedException($"Khong ho tro dinh dang '{loai}'"),
    };
}

// Strategy: cac thuat toan hoan doi duoc, chon luc chay
interface IChinhSachGiamGia
{
    string Ten { get; }
    decimal TinhTien(decimal tienHang);
}

class KhongGiam : IChinhSachGiamGia
{
    public string Ten => "Khong giam";
    public decimal TinhTien(decimal t) => t;
}

class GiamPhanTram(decimal phanTram) : IChinhSachGiamGia
{
    public string Ten => $"Giam {phanTram}%";
    public decimal TinhTien(decimal t) => t * (100 - phanTram) / 100;
}

class GiamCoDinh(decimal soTien) : IChinhSachGiamGia
{
    public string Ten => $"Giam {soTien:N0}";
    public decimal TinhTien(decimal t) => Math.Max(0, t - soTien);
}

class GiamKhachVip : IChinhSachGiamGia
{
    public string Ten => "Khach VIP";
    public decimal TinhTien(decimal t) => t >= 500_000 ? t * 0.8m : t * 0.95m;
}

class DonHang(decimal tienHang, Func<decimal, decimal> chinhSach)
{
    public decimal TongCong() => chinhSach(tienHang);
}

// Observer (bang event cua C#)
record GiaThayDoiEventArgs(string Ma, decimal GiaCu, decimal GiaMoi) : EventArgs;

class CuaHangGiaCoPhieu
{
    private readonly Dictionary<string, decimal> _gia = [];
    public event EventHandler<GiaThayDoiEventArgs>? GiaThayDoi;

    public void DatGia(string ma, decimal giaMoi)
    {
        decimal giaCu = _gia.GetValueOrDefault(ma);
        _gia[ma] = giaMoi;
        GiaThayDoi?.Invoke(this, new GiaThayDoiEventArgs(ma, giaCu, giaMoi));
    }
}

// Decorator: boc them hanh vi ma khong sua lop goc
interface IThongBao { void Gui(string noiDung); }

class ThongBaoConsole : IThongBao
{
    public void Gui(string noiDung) => Console.WriteLine(noiDung);
}

class ThongBaoCoThoiGian(IThongBao ben) : IThongBao
{
    public void Gui(string noiDung) => ben.Gui($"[08:30] {noiDung}");
}

class ThongBaoVietHoa(IThongBao ben) : IThongBao
{
    public void Gui(string noiDung) => ben.Gui(noiDung.ToUpperInvariant());
}
