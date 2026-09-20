// 1. Record + with
var a = new SinhVien("An", 20, "CNTT");
var b = a with { Tuoi = 21 };
Console.WriteLine($"{a}\n{b}\nBang nhau? {a == b}");

// 2. required + init
var sp = new SanPham { Ten = "Chuot", Gia = 150_000m };
Console.WriteLine(sp);
// var loi = new SanPham { Ten = "x" };   // LOI BIEN DICH: thieu Gia (required)

// 3. Primary constructor cho class
var kh = new KhachHang("Binh", "binh@mail.com");
Console.WriteLine(kh.ChaoHoi());

// 4. Pattern matching nang cao
object[] vatThe = [42, -7, "chao", new int[] { 1, 2, 3 }, new SinhVien("Chi", 19, "Toan"), 3.14, null!];
foreach (var v in vatThe)
    Console.WriteLine(MoTa(v));

// Property pattern
static string MoTa(object? o) => o switch
{
    null => "null",
    int n and > 0 => $"so duong {n}",
    int n => $"so khong duong {n}",
    string { Length: > 3 } s => $"chuoi dai '{s}'",
    string s => $"chuoi ngan '{s}'",
    int[] and [var dau, .., var cuoi] => $"mang tu {dau} den {cuoi}",     // list pattern
    SinhVien { Tuoi: < 20, Lop: var lop } => $"SV tre lop {lop}",     // property pattern
    SinhVien sv => $"SV {sv.Ten}",
    _ => $"khac: {o.GetType().Name}",
};

// 5. Relational + logical patterns
static string PhanLoaiNhietDo(double t) => t switch
{
    < 0 => "dong bang",
    >= 0 and < 15 => "lanh",
    >= 15 and < 30 => "de chiu",
    >= 30 => "nong",
    double.NaN => "loi",
};
Console.WriteLine(PhanLoaiNhietDo(-5));
Console.WriteLine(PhanLoaiNhietDo(22));

// 6. Collection expression + spread
int[] x = [1, 2, 3];
int[] y = [4, 5];
List<int> gop = [0, .. x, .. y, 99];
Console.WriteLine(string.Join(",", gop));

// 7. Tuple & deconstruct
var (ten, tuoi, _) = a;
Console.WriteLine($"{ten} - {tuoi}");
(int min, int max) = (gop.Min(), gop.Max());
Console.WriteLine($"min={min}, max={max}");

// 8. Raw string literal + noi suy
string ten2 = "An";
string json = $$"""
    {
      "ten": "{{ten2}}",
      "tuoi": 20
    }
    """;
Console.WriteLine(json);

// 9. Toan tu ngan gon: is not, ??=, index/range, default literal
int[] mang = [10, 20, 30, 40, 50];
Console.WriteLine($"{mang[^1]} {string.Join(",", mang[1..3])}");
string? tuyChon = null;
tuyChon ??= "mac dinh";
Console.WriteLine(tuyChon);
if (tuyChon is not null and { Length: > 3 }) Console.WriteLine("chuoi dai hon 3");

// 10. Local functions, static lambda
int tong = mang.Sum(static n => n * 2);
Console.WriteLine($"tong gap doi = {tong}");

record SinhVien(string Ten, int Tuoi, string Lop);

class SanPham
{
    public required string Ten { get; init; }
    public required decimal Gia { get; init; }
    public override string ToString() => $"{Ten}: {Gia:N0}";
}

class KhachHang(string ten, string email)
{
    public string Ten { get; } = ten;
    public string ChaoHoi() => $"Xin chao {Ten} <{email}>";
}
