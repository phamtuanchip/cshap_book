// Da hinh: cung mot loi goi, hanh vi tuy doi tuong that
Hinh[] cacHinh =
[
    new HinhTron(2),
    new HinhChuNhat(3, 4),
    new HinhVuong(5),
];

foreach (Hinh h in cacHinh)
    Console.WriteLine($"{h.Ten}: dien tich = {h.DienTich():F2}");

double tong = cacHinh.Sum(h => h.DienTich());
Console.WriteLine($"Tong dien tich: {tong:F2}");

// Upcasting (ngam dinh) & downcasting (tuong minh)
Hinh hinh = new HinhVuong(2);      // upcasting: HinhVuong -> Hinh
Console.WriteLine(hinh.GetType().Name);

// as: tra ve null neu khong ep duoc; is: kiem tra kieu
HinhTron? tron = hinh as HinhTron;
Console.WriteLine($"as HinhTron: {(tron is null ? "null" : "duoc")}");

if (hinh is HinhChuNhat hcn)   // HinhVuong ke thua HinhChuNhat
    Console.WriteLine($"Rong x Cao = {hcn.Rong} x {hcn.Cao}");

// Ep kieu sai => InvalidCastException
try
{
    var loi = (HinhTron)hinh;
    Console.WriteLine(loi);
}
catch (InvalidCastException e)
{
    Console.WriteLine($"Loi ep kieu: {e.Message}");
}

// Pattern matching voi da hinh
foreach (Hinh h in cacHinh)
{
    string mota = h switch
    {
        HinhVuong v => $"vuong canh {v.Rong}",     // dat truoc: kieu cu the hon
        HinhChuNhat r => $"chu nhat {r.Rong}x{r.Cao}",
        HinhTron t => $"tron ban kinh {t.BanKinh}",
        _ => "khac",
    };
    Console.WriteLine(mota);
}

abstract class Hinh
{
    public abstract string Ten { get; }
    public abstract double DienTich();
}

class HinhTron(double banKinh) : Hinh
{
    public double BanKinh { get; } = banKinh;
    public override string Ten => "Hinh tron";
    public override double DienTich() => Math.PI * BanKinh * BanKinh;
}

class HinhChuNhat(double rong, double cao) : Hinh
{
    public double Rong { get; } = rong;
    public double Cao { get; } = cao;
    public override string Ten => "Hinh chu nhat";
    public override double DienTich() => Rong * Cao;
}

class HinhVuong(double canh) : HinhChuNhat(canh, canh)
{
    public override string Ten => "Hinh vuong";
}
