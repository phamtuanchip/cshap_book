// 1. Delegate: "kieu" cua phuong thuc
PhepTinh cong = (a, b) => a + b;
PhepTinh nhan = (a, b) => a * b;
Console.WriteLine($"cong(3,4) = {cong(3, 4)}, nhan(3,4) = {nhan(3, 4)}");
Console.WriteLine($"ApDung: {ApDung(10, 5, (x, y) => x - y)}");

// 2. Func / Action / Predicate co san
Func<int, int> binh = x => x * x;              // nhan int, tra int
Func<int, int, string> ghep = (a, b) => $"{a}-{b}";  // tham so cuoi cung la kieu tra ve
Action<string> inRa = s => Console.WriteLine($"in: {s}");
Predicate<int> laChan = x => x % 2 == 0;       // tra bool
inRa(binh(7).ToString());
inRa(ghep(1, 2));
Console.WriteLine($"laChan(4) = {laChan(4)}");

// 3. Multicast: mot delegate goi nhieu phuong thuc
Action thongBao = () => Console.WriteLine("  ghi log");
thongBao += () => Console.WriteLine("  gui email");
thongBao += () => Console.WriteLine("  cap nhat UI");
Console.WriteLine("Goi multicast:");
thongBao();

// 4. Lambda va closure (bat bien ben ngoai)
int heSo = 3;
Func<int, int> nhanHeSo = x => x * heSo;
Console.WriteLine($"nhanHeSo(5) = {nhanHeSo(5)}");
heSo = 10;   // closure doc bien LUC GOI, khong phai luc tao
Console.WriteLine($"nhanHeSo(5) sau khi doi heSo = {nhanHeSo(5)}");

// Ham tra ve ham
Func<int, int> TaoBoCong(int n) => x => x + n;
var cong5 = TaoBoCong(5);
Console.WriteLine($"cong5(1) = {cong5(1)}");

// 5. Truyen ham vao ham
int[] so = [1, 2, 3, 4, 5, 6];
Console.WriteLine($"Loc chan: {string.Join(",", Loc(so, x => x % 2 == 0))}");
Console.WriteLine($"Loc > 3:  {string.Join(",", Loc(so, x => x > 3))}");

// Method group: truyen ten phuong thuc thay vi lambda
Console.WriteLine($"Method group: {string.Join(",", so.Select(binh))}");

// 6. Event: mau publisher / subscriber
var may = new MayPha();
may.SanSang += (sender, e) => Console.WriteLine($"[Khach A] nhan: {e.TenDoUong} luc {e.ThoiGian:HH:mm}");
may.SanSang += DocThongBao;
may.Pha("ca phe sua");
may.SanSang -= DocThongBao;   // huy dang ky
may.Pha("tra dao");

static void DocThongBao(object? sender, DoUongEventArgs e)
    => Console.WriteLine($"[Khach B] {e.TenDoUong} xong");

static int ApDung(int a, int b, PhepTinh f) => f(a, b);

static IEnumerable<int> Loc(IEnumerable<int> ds, Predicate<int> dieuKien)
{
    foreach (var x in ds)
        if (dieuKien(x)) yield return x;
}

delegate int PhepTinh(int a, int b);

class DoUongEventArgs(string ten) : EventArgs
{
    public string TenDoUong { get; } = ten;
    public DateTime ThoiGian { get; } = new(2025, 1, 1, 8, 30, 0);
}

class MayPha
{
    // event: chi class nay moi goi (Invoke) duoc; ben ngoai chi += va -=
    public event EventHandler<DoUongEventArgs>? SanSang;

    public void Pha(string ten)
    {
        Console.WriteLine($"Dang pha {ten}...");
        OnSanSang(new DoUongEventArgs(ten));
    }

    protected virtual void OnSanSang(DoUongEventArgs e) => SanSang?.Invoke(this, e);
}
