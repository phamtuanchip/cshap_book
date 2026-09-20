// 1. IEnumerator: foreach hoat dong ra sao
var ds = new List<int> { 1, 2, 3 };
using (var it = ds.GetEnumerator())
{
    while (it.MoveNext())
        Console.Write($"{it.Current} ");
}
Console.WriteLine();

// 2. yield return: tao day gia tri ma khong can luu tat ca
foreach (var f in Fibonacci().Take(10))
    Console.Write($"{f} ");
Console.WriteLine();

// 3. Deferred execution (thuc thi tri hoan)
Console.WriteLine("--- deferred ---");
var seq = DemSo(3);
Console.WriteLine("Da tao seq, chua chay gi ca");
foreach (var x in seq) Console.WriteLine($"nhan {x}");

// 4. Tu cai dat IEnumerable<T>
var danhSach = new DanhSachTen(["An", "Binh", "Chi"]);
foreach (var ten in danhSach) Console.Write($"{ten} ");
Console.WriteLine();

// 5. Sap xep
var sv = new List<SinhVien>
{
    new("Chi", 8.0),
    new("An", 9.0),
    new("Binh", 8.0),
};
sv.Sort((x, y) => y.Diem.CompareTo(x.Diem));   // Sort thay doi list goc, khong on dinh
Console.WriteLine($"Sort:    {string.Join(", ", sv.Select(s => s.Ten))}");

// OrderBy: on dinh, tra ve day moi, ho tro nhieu khoa (ThenBy)
var xep = sv.OrderByDescending(s => s.Diem).ThenBy(s => s.Ten);
Console.WriteLine($"OrderBy: {string.Join(", ", xep.Select(s => s.Ten))}");

// Comparer.Create
var theoTen = Comparer<SinhVien>.Create((x, y) => string.Compare(x.Ten, y.Ten, StringComparison.Ordinal));
sv.Sort(theoTen);
Console.WriteLine($"Theo ten: {string.Join(", ", sv.Select(s => s.Ten))}");

static IEnumerable<long> Fibonacci()
{
    long a = 0, b = 1;
    while (true)   // vo han, nhung chi tinh khi duoc yeu cau
    {
        yield return a;
        (a, b) = (b, a + b);
    }
}

static IEnumerable<int> DemSo(int n)
{
    for (int i = 1; i <= n; i++)
    {
        Console.WriteLine($"  sinh {i}");
        yield return i;
    }
}

class DanhSachTen(string[] ten) : IEnumerable<string>
{
    public IEnumerator<string> GetEnumerator()
    {
        foreach (var t in ten) yield return t;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

record SinhVien(string Ten, double Diem);
