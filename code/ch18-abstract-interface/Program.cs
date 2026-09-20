// Interface: hop dong "co the lam gi"
IThanhToan[] cach =
[
    new TheTinDung("1234-5678"),
    new ViDienTu("0909000111"),
    new TienMat(),
];

foreach (var c in cach)
{
    c.ThanhToan(150_000m);
    Console.WriteLine($"  phi: {c.TinhPhi(150_000m):N0}");
}

// Doi tuong cung luc trien khai nhieu interface
var vi = new ViDienTu("0909000222");
if (vi is ILuuVet luu) luu.GhiLog("Da tao vi");
Console.WriteLine($"Log: {string.Join(" | ", vi.NhatKy)}");

// Abstract class: khung chung + phan con phai tu hien thuc
Console.WriteLine("--- Abstract class ---");
BaoCao[] baoCao = [new BaoCaoText("Doanh thu thang 3"), new BaoCaoHtml("Doanh thu thang 3")];
foreach (var b in baoCao)
    Console.WriteLine(b.Xuat());

// Explicit implementation: giai quyet trung ten
IBay may = new ChimCanhCut();
Console.WriteLine(may.DiChuyen());
IBoi boi = new ChimCanhCut();
Console.WriteLine(boi.DiChuyen());

interface IThanhToan
{
    void ThanhToan(decimal soTien);

    // default interface method: co hien thuc san, lop nao khong can thi khoi viet
    decimal TinhPhi(decimal soTien) => 0m;
}

interface ILuuVet
{
    void GhiLog(string noiDung);
}

class TheTinDung(string soThe) : IThanhToan
{
    public void ThanhToan(decimal soTien) => Console.WriteLine($"The {soThe}: tra {soTien:N0}");
    public decimal TinhPhi(decimal soTien) => soTien * 0.02m;   // ghi de phi mac dinh
}

class ViDienTu(string sdt) : IThanhToan, ILuuVet
{
    private readonly List<string> _nhatKy = [];
    public IReadOnlyList<string> NhatKy => _nhatKy;

    public void ThanhToan(decimal soTien)
    {
        Console.WriteLine($"Vi {sdt}: tra {soTien:N0}");
        GhiLog($"tra {soTien:N0}");
    }

    public void GhiLog(string noiDung) => _nhatKy.Add(noiDung);
}

class TienMat : IThanhToan
{
    public void ThanhToan(decimal soTien) => Console.WriteLine($"Tien mat: tra {soTien:N0}");
    // khong khai bao TinhPhi => dung phien ban mac dinh
}

// Abstract class: template method
abstract class BaoCao(string tieuDe)
{
    protected string TieuDe { get; } = tieuDe;

    // Template method: khung co dinh, buoc con thay doi
    public string Xuat() => $"{DauTrang()}\n{TieuDe}\n{ChanTrang()}";

    protected abstract string DauTrang();
    protected virtual string ChanTrang() => "-- het --";
}

class BaoCaoText(string tieuDe) : BaoCao(tieuDe)
{
    protected override string DauTrang() => "=== BAO CAO ===";
}

class BaoCaoHtml(string tieuDe) : BaoCao(tieuDe)
{
    protected override string DauTrang() => "<h1>BAO CAO</h1>";
    protected override string ChanTrang() => "<footer>het</footer>";
}

interface IBay { string DiChuyen(); }
interface IBoi { string DiChuyen(); }

class ChimCanhCut : IBay, IBoi
{
    string IBay.DiChuyen() => "Bay";    // explicit implementation
    string IBoi.DiChuyen() => "Boi";
}
