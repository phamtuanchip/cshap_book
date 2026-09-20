// 1. Dictionary: cac cach dung
var diem = new Dictionary<string, double>
{
    ["An"] = 8.5,
    ["Binh"] = 7.0,
};
diem["Chi"] = 9.0;              // them hoac ghi de
diem.TryAdd("An", 1.0);         // khong ghi de neu da co
Console.WriteLine($"An: {diem["An"]}");

// diem["Zzz"] => KeyNotFoundException. Dung TryGetValue:
if (!diem.TryGetValue("Zzz", out var d)) Console.WriteLine("Khong co Zzz");
Console.WriteLine($"GetValueOrDefault: {diem.GetValueOrDefault("Zzz", -1)}");

// Dem tan suat tu
string[] tu = "a b a c b a".Split(' ');
var tanSuat = new Dictionary<string, int>();
foreach (var t in tu)
    tanSuat[t] = tanSuat.GetValueOrDefault(t) + 1;
Console.WriteLine(string.Join(", ", tanSuat.Select(kv => $"{kv.Key}={kv.Value}")));

// 2. HashSet: loai trung, phep toan tap hop
var a = new HashSet<int> { 1, 2, 3, 4 };
var b = new HashSet<int> { 3, 4, 5 };
var hop = new HashSet<int>(a); hop.UnionWith(b);
var giao = new HashSet<int>(a); giao.IntersectWith(b);
var hieu = new HashSet<int>(a); hieu.ExceptWith(b);
Console.WriteLine($"hop={string.Join(",", hop)} giao={string.Join(",", giao)} hieu={string.Join(",", hieu)}");
Console.WriteLine($"Add trung tra ve: {a.Add(2)}");   // false

// 3. Sorted: tu dong sap xep theo khoa
var sd = new SortedDictionary<string, int> { ["chuoi"] = 3, ["tao"] = 1, ["cam"] = 2 };
Console.WriteLine(string.Join(", ", sd.Select(kv => kv.Key)));
var ss = new SortedSet<int> { 5, 1, 3 };
Console.WriteLine($"SortedSet: {string.Join(",", ss)}, Min={ss.Min}, Max={ss.Max}");

// 4. Vai tro cua Equals / GetHashCode
var sai = new HashSet<DiemSai> { new(1, 2) };
Console.WriteLine($"DiemSai (thieu Equals/GetHashCode): Contains = {sai.Contains(new DiemSai(1, 2))}");

var dung = new HashSet<DiemDung> { new(1, 2) };
Console.WriteLine($"DiemDung (record): Contains = {dung.Contains(new DiemDung(1, 2))}");

// 5. Tuy chinh so sanh khoa
var khongPhanBietHoa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Abc"] = 1 };
Console.WriteLine($"\"ABC\" co khong? {khongPhanBietHoa.ContainsKey("ABC")}");

// 6. Them/xoa khoa khi dang duyet => InvalidOperationException
try
{
    foreach (var k in diem.Keys) diem[k + "_moi"] = 0;
}
catch (InvalidOperationException e)
{
    Console.WriteLine($"Loi: {e.Message}");
}
foreach (var k in diem.Keys.ToList()) diem[k + "_moi"] = 0;   // sao chep danh sach khoa truoc
Console.WriteLine($"So khoa sau khi them an toan: {diem.Count}");

class DiemSai(int x, int y)
{
    public int X { get; } = x;
    public int Y { get; } = y;
}

record DiemDung(int X, int Y);
