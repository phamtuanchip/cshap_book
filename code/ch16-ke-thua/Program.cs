var nv = new NhanVien("An", 1990, "IT", 20_000_000m);
var ql = new QuanLy("Binh", 1985, "IT", 30_000_000m, 3);

Console.WriteLine(nv.GioiThieu());
Console.WriteLine(ql.GioiThieu());
Console.WriteLine($"Luong thuc linh NV: {nv.TinhLuong():N0}");
Console.WriteLine($"Luong thuc linh QL: {ql.TinhLuong():N0}");

// Thu tu goi constructor: cha truoc, con sau
Console.WriteLine("--- thu tu constructor ---");
_ = new QuanLy("Chi", 1988, "HR", 25_000_000m, 1);

// new (che giau) khac override
Console.WriteLine("--- override vs new ---");
Cha cha = new Con();
Console.WriteLine(cha.Ma());        // override => "Con.Ma" (dua vao doi tuong that)
Console.WriteLine(cha.Ten());       // new      => "Cha.Ten" (dua vao kieu khai bao)
Console.WriteLine(((Con)cha).Ten()); // "Con.Ten"

// Composition: "co mot" thay vi "la mot"
var xe = new XeHoi();
xe.Chay();

class Nguoi
{
    public string Ten { get; }
    public int NamSinh { get; }

    public Nguoi(string ten, int namSinh)
    {
        Console.WriteLine($"  [Nguoi] constructor {ten}");
        Ten = ten;
        NamSinh = namSinh;
    }

    public virtual string GioiThieu() => $"Toi la {Ten}, sinh nam {NamSinh}";
}

class NhanVien : Nguoi
{
    public string PhongBan { get; }
    protected decimal LuongCoBan { get; }

    public NhanVien(string ten, int namSinh, string phongBan, decimal luongCoBan)
        : base(ten, namSinh)   // goi constructor cua lop cha
    {
        Console.WriteLine($"  [NhanVien] constructor {ten}");
        PhongBan = phongBan;
        LuongCoBan = luongCoBan;
    }

    public override string GioiThieu() => $"{base.GioiThieu()}, phong {PhongBan}";

    public virtual decimal TinhLuong() => LuongCoBan;
}

class QuanLy : NhanVien
{
    public int SoNguoiQuanLy { get; }

    public QuanLy(string ten, int namSinh, string phongBan, decimal luongCoBan, int soNguoi)
        : base(ten, namSinh, phongBan, luongCoBan)
    {
        Console.WriteLine($"  [QuanLy] constructor {ten}");
        SoNguoiQuanLy = soNguoi;
    }

    public override string GioiThieu() => $"{base.GioiThieu()}, quan ly {SoNguoiQuanLy} nguoi";

    public override decimal TinhLuong() => LuongCoBan + SoNguoiQuanLy * 2_000_000m;
}

class Cha
{
    public virtual string Ma() => "Cha.Ma";
    public string Ten() => "Cha.Ten";
}

class Con : Cha
{
    public override string Ma() => "Con.Ma";
    public new string Ten() => "Con.Ten";   // che giau, KHONG phai override
}

// Composition: XeHoi "co mot" DongCo, khong "la mot" DongCo
class DongCo
{
    public void Khoi() => Console.WriteLine("Dong co no may");
}

class XeHoi
{
    private readonly DongCo _dongCo = new();
    public void Chay()
    {
        _dongCo.Khoi();
        Console.WriteLine("Xe dang chay");
    }
}
