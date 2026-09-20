// static class + extension method
Console.WriteLine("hello world".VietHoaChuDau());
Console.WriteLine("abcba".LaDoiXung());
Console.WriteLine(5.LaSoChan());
Console.WriteLine(ToanHoc.BinhPhuong(9));

// enum
var tt = TrangThaiDon.DangGiao;
Console.WriteLine($"{tt} = {(int)tt}");
Console.WriteLine(Enum.Parse<TrangThaiDon>("DaGiao"));
Console.WriteLine(Enum.TryParse("KhongCo", out TrangThaiDon _));
foreach (var v in Enum.GetValues<TrangThaiDon>())
    Console.WriteLine($"  {(int)v}: {v} - {v.MoTa()}");

// enum [Flags]: to hop nhieu gia tri bang toan tu bit
var quyen = Quyen.Doc | Quyen.Ghi;
Console.WriteLine($"quyen = {quyen}");
Console.WriteLine($"co quyen ghi: {quyen.HasFlag(Quyen.Ghi)}");
Console.WriteLine($"co quyen xoa: {(quyen & Quyen.Xoa) != 0}");
quyen |= Quyen.Xoa;
quyen &= ~Quyen.Doc;   // bo quyen doc
Console.WriteLine($"sau khi sua: {quyen}");

// partial class
var sv = new SinhVien { Ten = "An", Diem = 8.5 };
Console.WriteLine(sv);

// lop long nhau
var dsl = new DanhSachLienKet();
dsl.Them(1);
dsl.Them(2);
dsl.Them(3);
Console.WriteLine(string.Join("->", dsl.DuyetTatCa()));

static class ToanHoc
{
    // static class: khong tao doi tuong duoc, chi chua thanh vien static
    public static int BinhPhuong(int x) => x * x;
}

// Extension method: them "phuong thuc" cho kieu co san. Phai o trong static class,
// tham so dau co tu khoa this.
static class MoRong
{
    public static string VietHoaChuDau(this string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var tu = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', tu.Select(t => char.ToUpper(t[0]) + t[1..]));
    }

    public static bool LaDoiXung(this string s) => s.SequenceEqual(s.Reverse());

    public static bool LaSoChan(this int n) => n % 2 == 0;

    public static string MoTa(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.ChoXacNhan => "cho xac nhan",
        TrangThaiDon.DangGiao => "dang giao hang",
        TrangThaiDon.DaGiao => "da giao xong",
        _ => "khong ro",
    };
}

enum TrangThaiDon
{
    ChoXacNhan = 1,
    DangGiao = 2,
    DaGiao = 3,
}

[Flags]
enum Quyen
{
    Khong = 0,
    Doc = 1,    // 0001
    Ghi = 2,    // 0010
    Xoa = 4,    // 0100
    TatCa = Doc | Ghi | Xoa,
}

// partial class: tach class thanh nhieu phan (trong cung project)
partial class SinhVien
{
    public string Ten { get; set; } = "";
    public double Diem { get; set; }
}

partial class SinhVien
{
    public override string ToString() => $"{Ten} ({Diem})";
}

class DanhSachLienKet
{
    private Nut? _dau;

    public void Them(int giaTri)
    {
        var moi = new Nut(giaTri);
        if (_dau is null) { _dau = moi; return; }
        var cur = _dau;
        while (cur.Tiep is not null) cur = cur.Tiep;
        cur.Tiep = moi;
    }

    public IEnumerable<int> DuyetTatCa()
    {
        for (var cur = _dau; cur is not null; cur = cur.Tiep)
            yield return cur.GiaTri;
    }

    // lop long nhau (nested), private: chi DanhSachLienKet dung duoc
    private class Nut(int giaTri)
    {
        public int GiaTri { get; } = giaTri;
        public Nut? Tiep { get; set; }
    }
}
