// 1. Generic class: dung cho moi kieu
var soNguyen = new NganXep<int>();
soNguyen.Day(1);
soNguyen.Day(2);
soNguyen.Day(3);
Console.WriteLine($"Lay ra: {soNguyen.Lay()}, con {soNguyen.SoLuong}");

var chuoi = new NganXep<string>();
chuoi.Day("a");
chuoi.Day("b");
Console.WriteLine($"Lay ra: {chuoi.Lay()}");
// chuoi.Day(5);   // LOI BIEN DICH: kieu an toan

// 2. Generic method + suy luan kieu
int x = 1, y = 2;
Tien.HoanDoi(ref x, ref y);
Console.WriteLine($"x={x}, y={y}");
Console.WriteLine(Tien.LonNhat(3, 9, 4));
Console.WriteLine(Tien.LonNhat("cam", "tao", "xoai"));
Console.WriteLine(Tien.LonNhat(2.5, 1.5));

// 3. Constraint: gioi han kieu duoc dung
var kho = new KhoTheoMa<Sach>();
kho.Them(new Sach("S1", "C# co ban"));
kho.Them(new Sach("S2", "ASP.NET"));
Console.WriteLine(kho.Tim("S2")?.Ten);

var moi = Tien.TaoMoi<Sach>();
Console.WriteLine($"Tao bang new(): '{moi.Ma}'");

// 4. Generic interface + generic record
IKetQua<int> kq = new KetQua<int>(true, 42, null);
Console.WriteLine(kq.ThanhCong ? $"OK {kq.GiaTri}" : $"Loi {kq.Loi}");
var loi = KetQua<string>.ThatBai("khong tim thay");
Console.WriteLine(loi.ThanhCong ? "OK" : $"Loi: {loi.Loi}");

// 5. Covariance: IEnumerable<out T>
IEnumerable<string> tenChuoi = ["a", "b"];
IEnumerable<object> tenObj = tenChuoi;   // OK vi IEnumerable<out T>
Console.WriteLine(string.Join(",", tenObj));
// List<object> ds = new List<string>();  // LOI: List<T> khong co bien thien

// 6. Contravariance: Action<in T>
Action<object> inRa = o => Console.WriteLine($"in: {o}");
Action<string> inChuoi = inRa;   // OK vi Action<in T>
inChuoi("hello");

class NganXep<T>
{
    private readonly List<T> _ds = [];
    public int SoLuong => _ds.Count;
    public void Day(T item) => _ds.Add(item);

    public T Lay()
    {
        if (_ds.Count == 0) throw new InvalidOperationException("Ngan xep rong");
        T item = _ds[^1];
        _ds.RemoveAt(_ds.Count - 1);
        return item;
    }
}

static class Tien
{
    public static void HoanDoi<T>(ref T a, ref T b) => (a, b) = (b, a);

    // where T : IComparable<T>  => T phai so sanh duoc
    public static T LonNhat<T>(params T[] ds) where T : IComparable<T>
    {
        T max = ds[0];
        foreach (var x in ds)
            if (x.CompareTo(max) > 0) max = x;
        return max;
    }

    // where T : new()  => T phai co constructor khong tham so
    public static T TaoMoi<T>() where T : new() => new();
}

interface ICoMa { string Ma { get; } }

class Sach : ICoMa
{
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public Sach() { }
    public Sach(string ma, string ten) { Ma = ma; Ten = ten; }
}

class KhoTheoMa<T> where T : class, ICoMa
{
    private readonly Dictionary<string, T> _ds = [];
    public void Them(T item) => _ds[item.Ma] = item;
    public T? Tim(string ma) => _ds.GetValueOrDefault(ma);
}

interface IKetQua<out T>
{
    bool ThanhCong { get; }
    T? GiaTri { get; }
    string? Loi { get; }
}

record KetQua<T>(bool ThanhCong, T? GiaTri, string? Loi) : IKetQua<T>
{
    public static KetQua<T> ThatBai(string loi) => new(false, default, loi);
}
