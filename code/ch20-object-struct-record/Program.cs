// 1. Equals mac dinh cua class: so sanh THAM CHIEU
var s1 = new SinhVienClass("An", 1);
var s2 = new SinhVienClass("An", 1);
Console.WriteLine($"class mac dinh: {s1.Equals(s2)}");

// 2. Tu dinh nghia Equals + GetHashCode
var d1 = new DiemTuDinhNghia(1, 2);
var d2 = new DiemTuDinhNghia(1, 2);
Console.WriteLine($"tu dinh nghia: {d1.Equals(d2)}, == : {d1 == d2}, hash bang: {d1.GetHashCode() == d2.GetHashCode()}");

var tap = new HashSet<DiemTuDinhNghia> { d1 };
Console.WriteLine($"HashSet chua d2? {tap.Contains(d2)}");

// 3. struct: kieu GIA TRI
var p1 = new ToaDo { X = 1, Y = 2 };
var p2 = p1;         // sao chep
p2.X = 99;
Console.WriteLine($"struct: p1.X = {p1.X}, p2.X = {p2.X}");

// 4. record: gon, so sanh theo gia tri, with-expression
var a = new NguoiRecord("An", 20);
var b = new NguoiRecord("An", 20);
Console.WriteLine($"record: {a == b}, {a}");
var c = a with { Tuoi = 21 };
Console.WriteLine($"with: {c}   (a van la {a})");

// Deconstruct
var (ten, tuoi) = a;
Console.WriteLine($"{ten} - {tuoi}");

// record struct
var t1 = new DiemRS(1, 2);
Console.WriteLine($"record struct: {t1}, {t1 == new DiemRS(1, 2)}");

// 5. IComparable / IComparer: sap xep
var ds = new List<SanPham>
{
    new("Chuot", 150_000m),
    new("Ban phim", 500_000m),
    new("Tai nghe", 200_000m),
};
ds.Sort();   // dung IComparable<SanPham> (theo gia)
Console.WriteLine(string.Join(", ", ds.Select(x => x.Ten)));

ds.Sort(new SoSanhTheoTen());   // dung IComparer<SanPham>
Console.WriteLine(string.Join(", ", ds.Select(x => x.Ten)));

ds.Sort((x, y) => y.Gia.CompareTo(x.Gia));   // lambda
Console.WriteLine(string.Join(", ", ds.Select(x => x.Ten)));

class SinhVienClass(string ten, int ma)
{
    public string Ten { get; } = ten;
    public int Ma { get; } = ma;
}

class DiemTuDinhNghia(int x, int y) : IEquatable<DiemTuDinhNghia>
{
    public int X { get; } = x;
    public int Y { get; } = y;

    public bool Equals(DiemTuDinhNghia? other) => other is not null && X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => Equals(obj as DiemTuDinhNghia);
    public override int GetHashCode() => HashCode.Combine(X, Y);   // phai nhat quan voi Equals

    public static bool operator ==(DiemTuDinhNghia? l, DiemTuDinhNghia? r) => l?.Equals(r) ?? r is null;
    public static bool operator !=(DiemTuDinhNghia? l, DiemTuDinhNghia? r) => !(l == r);
}

struct ToaDo
{
    public int X;
    public int Y;
}

record NguoiRecord(string Ten, int Tuoi);

readonly record struct DiemRS(int X, int Y);

record SanPham(string Ten, decimal Gia) : IComparable<SanPham>
{
    public int CompareTo(SanPham? other) => other is null ? 1 : Gia.CompareTo(other.Gia);
}

class SoSanhTheoTen : IComparer<SanPham>
{
    public int Compare(SanPham? x, SanPham? y) => string.Compare(x?.Ten, y?.Ten, StringComparison.Ordinal);
}
