// Moi nguyen ly co cap "Xau" (vi pham) va "Tot" (sau khi tai cau truc).

Console.WriteLine("=== S: Single Responsibility ===");
// Xau: lop lam TAT CA (tinh tien, dinh dang, luu tru) -> co nhieu ly do de sua
var xau = new HoaDonXau();
xau.InVaLuu(500_000m);
// Tot: moi lop mot trach nhiem
var tot = new HoaDonService(new TinhThue(0.1m), new InHoaDon(), new LuuTruTrongBoNho());
tot.XuLy(500_000m);

Console.WriteLine("\n=== O: Open/Closed ===");
// Xau: them loai khach phai SUA ham (switch) -> Tot: them lop moi, khong sua code cu
IGiamGia[] cacLoai = [new GiamThuong(), new GiamVip(), new GiamSinhVien()];
foreach (var g in cacLoai) Console.WriteLine($"{g.GetType().Name}: {g.Giam(1_000_000m):N0}");

Console.WriteLine("\n=== L: Liskov Substitution ===");
// Xau: Square : Rectangle lam vo hop dong -> Tot: dung interface chung
IHinh[] hinh = [new HCN(3, 4), new HinhVuong(5)];
foreach (var h in hinh) Console.WriteLine($"{h.GetType().Name}: dien tich {h.DienTich()}");
var hcnXau = new HCNXau { Rong = 2, Cao = 3 };
HCNXau vuongXau = new VuongXau();
vuongXau.Rong = 2;
vuongXau.Cao = 3;   // nguoi dung mong dien tich = 6 nhung...
Console.WriteLine($"HCN 2x3: {hcnXau.DienTich()}, 'Vuong' 2x3 (vi pham Liskov): {vuongXau.DienTich()}");

Console.WriteLine("\n=== I: Interface Segregation ===");
// Xau: IMayXau ep may in don gian phai cai dat fax, scan -> Tot: interface nho
IMayIn mayIn = new MayInDon();
mayIn.In("tai lieu");
var daNangLuc = new MayDaNangLuc();
daNangLuc.In("tai lieu 2");
daNangLuc.Quet("giay to");

Console.WriteLine("\n=== D: Dependency Inversion ===");
// Xau: DatHangXau tu new lop cu the -> Tot: phu thuoc interface, cai dat duoc "bom" tu ngoai (composition root)
var guiEmail = new GuiEmailGia();
var dichVu = new DatHangService(guiEmail);
dichVu.DatHang("Laptop");
Console.WriteLine($"Da gui {guiEmail.DaGui.Count} email");

// ================= S =================
class HoaDonXau
{
    public void InVaLuu(decimal tien)
    {
        decimal thue = tien * 0.1m;                       // logic thue
        string chuoi = $"Hoa don: {tien + thue:N0}";      // dinh dang
        Console.WriteLine($"[Xau] {chuoi}");               // in
        // ... roi lai luu CSDL, gui email ... (moi thu trong mot lop)
    }
}

interface ITinhThue { decimal Thue(decimal tien); }
interface IInHoaDon { void In(string noiDung); }
interface ILuuTru { void Luu(string noiDung); }

class TinhThue(decimal tyLe) : ITinhThue { public decimal Thue(decimal tien) => tien * tyLe; }
class InHoaDon : IInHoaDon { public void In(string noiDung) => Console.WriteLine($"[Tot] {noiDung}"); }
class LuuTruTrongBoNho : ILuuTru
{
    public List<string> DaLuu { get; } = [];
    public void Luu(string noiDung) => DaLuu.Add(noiDung);
}

class HoaDonService(ITinhThue thue, IInHoaDon inAn, ILuuTru luu)
{
    public void XuLy(decimal tien)
    {
        string noiDung = $"Hoa don: {tien + thue.Thue(tien):N0}";
        inAn.In(noiDung);
        luu.Luu(noiDung);
    }
}

// ================= O =================
interface IGiamGia { decimal Giam(decimal tien); }
class GiamThuong : IGiamGia { public decimal Giam(decimal t) => t; }
class GiamVip : IGiamGia { public decimal Giam(decimal t) => t * 0.8m; }
class GiamSinhVien : IGiamGia { public decimal Giam(decimal t) => t * 0.9m; }
// Them "GiamNhanVien"? Chi viet lop moi, khong sua vong lap o tren.

// ================= L =================
class HCNXau
{
    public virtual double Rong { get; set; }
    public virtual double Cao { get; set; }
    public double DienTich() => Rong * Cao;
}

class VuongXau : HCNXau
{
    public override double Rong { get => base.Rong; set { base.Rong = value; base.Cao = value; } }
    public override double Cao { get => base.Cao; set { base.Rong = value; base.Cao = value; } }
}

interface IHinh { double DienTich(); }
record HCN(double Rong, double Cao) : IHinh { public double DienTich() => Rong * Cao; }
record HinhVuong(double Canh) : IHinh { public double DienTich() => Canh * Canh; }

// ================= I =================
interface IMayIn { void In(string s); }
interface IMayQuet { void Quet(string s); }

class MayInDon : IMayIn
{
    public void In(string s) => Console.WriteLine($"[May in don] {s}");
}

class MayDaNangLuc : IMayIn, IMayQuet
{
    public void In(string s) => Console.WriteLine($"[Da nang] in {s}");
    public void Quet(string s) => Console.WriteLine($"[Da nang] quet {s}");
}

// ================= D =================
interface IGuiEmail { void Gui(string den, string noiDung); }

class GuiEmailGia : IGuiEmail
{
    public List<string> DaGui { get; } = [];
    public void Gui(string den, string noiDung) => DaGui.Add($"{den}: {noiDung}");
}

class DatHangService(IGuiEmail email)
{
    public void DatHang(string sanPham)
    {
        Console.WriteLine($"Dat hang {sanPham}");
        email.Gui("khach@mail.com", $"Da dat {sanPham}");
    }
}
