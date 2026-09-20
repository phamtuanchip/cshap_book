# Phụ lục C — Cheat sheet C#

Tra cứu nhanh cú pháp và lệnh thường dùng. Nhớ đây là **tóm tắt**; giải thích nằm trong các chương tương ứng (ghi trong ngoặc).

## C.1 Lệnh `dotnet` (Ch. 2–4, 40)

```
dotnet --version                  dotnet --info                 dotnet new list
dotnet new console -n Ten         dotnet new classlib -n Ten    dotnet new xunit -n Ten.Tests
dotnet new sln -n Ten             dotnet sln add Proj           dotnet add A reference B
dotnet add package X [--version v]      dotnet remove package X      dotnet list package [--outdated]
dotnet run [--project P] [-- args]      dotnet build     dotnet test     dotnet publish -c Release
dotnet user-secrets set "K" "V"         dotnet format    dotnet new gitignore
```

## C.2 Kiểu dữ liệu (Ch. 5)

| Kiểu | Ví dụ | Ghi chú |
|------|-------|---------|
| `int`, `long`, `byte` | `25`, `8_000L`, `200` | số nguyên 32/64/8 bit |
| `double`, `float`, `decimal` | `1.5`, `1.5f`, `19.99m` | `decimal` cho tiền |
| `bool`, `char`, `string` | `true`, `'A'`, `"chao"` | |
| `var` | `var x = 5;` | suy kiểu lúc biên dịch |
| `T?` | `int? x = null;`, `string? s` | nullable value / reference type |
| `const`, `readonly` | `const double Pi = 3.14;` | hằng lúc biên dịch / lúc chạy |

Ép kiểu: `(int)9.9` (cắt) · `Convert.ToInt32(x)` · `int.Parse(s)` · `int.TryParse(s, out int n)` · `x as T` · `x is T t`.

## C.3 Toán tử (Ch. 6)

```
+ - * / %    ++ --    += -= *= /=    == != < > <= >=    && || !    & | ^ ~ << >>
c ? a : b    x ?? d    x ??= d    x?.Y    x?[i]    x!    a..b    ^1    is not null    nameof(x)
```

Độ ưu tiên (cao → thấp): `! ++ --` → `* / %` → `+ -` → so sánh → `== !=` → `&&` → `||` → `??` → `?:` → gán. Khi nghi ngờ: dùng ngoặc.

## C.4 Điều khiển (Ch. 7–8)

```csharp
if (a) { } else if (b) { } else { }
switch (x) { case 1: ...; break; default: ...; break; }
var s = x switch { 1 => "a", >= 2 and <= 5 => "b", _ => "c" };

for (int i = 0; i < n; i++) { }       while (dk) { }       do { } while (dk);
foreach (var x in ds) { }              break;  continue;   return;
```

Pattern: `is int n`, `is { Length: > 3 }`, `is [1, .., var last]`, `not null`, `x and > 0`, `x or 1`.

## C.5 Chuỗi (Ch. 11)

```csharp
$"Xin chao {ten}, {gia:N2}, {ngay:dd/MM/yyyy}, [{s,10}], [{s,-10}]"
s.Length  s.Trim()  s.ToUpper()  s.Contains("x")  s.IndexOf("x")  s.Replace("a","b")  s.Substring(i,n)  s[i]  s[..3]
s.Split(',')   string.Join(", ", ds)   string.IsNullOrWhiteSpace(s)   s.StartsWith("x")
string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
"""raw multi-line"""     @"C:\verbatim"     new StringBuilder().Append(x).ToString()
```

## C.6 Mảng và collection (Ch. 9, 24–27)

```csharp
int[] a = [1, 2, 3];   int[,] m = new int[2,3];   int[][] j = new int[3][];   a[^1]   a[1..3]   a.Length
Array.Sort(a)  Array.Reverse(a)  Array.IndexOf(a, x)  Array.BinarySearch(a, x)

var l = new List<T> { }    l.Add  l.Insert(i,x)  l.Remove(x)  l.RemoveAt(i)  l.RemoveAll(p)  l.Contains  l.Sort()  l.Count
var d = new Dictionary<K,V>   d[k] = v   d.TryGetValue(k, out var v)   d.ContainsKey(k)   d.GetValueOrDefault(k)   d.TryAdd(k, v)
var h = new HashSet<T>        h.Add(x)   h.Contains(x)   h.UnionWith(o)  h.IntersectWith(o)  h.ExceptWith(o)
new Queue<T>()  .Enqueue .Dequeue .Peek          new Stack<T>()  .Push .Pop .Peek
```

| Cần | Dùng | Truy cập/tìm |
|-----|------|--------------|
| Danh sách | `List<T>` | `[i]` O(1) · tìm O(n) |
| Tra cứu theo khoá | `Dictionary<K,V>` | O(1) |
| Không trùng | `HashSet<T>` | O(1) |
| Có thứ tự khoá | `SortedDictionary/SortedSet` | O(log n) |

## C.7 LINQ (Ch. 30)

```csharp
.Where(x => ...)  .Select(x => ...)  .SelectMany(...)  .OrderBy(k)  .OrderByDescending(k)  .ThenBy(k)
.GroupBy(k)  .Join(o, a => a.K, b => b.K, (a, b) => ...)  .Distinct()  .Skip(n).Take(n)  .Chunk(n)  .Zip(o)
.First()  .FirstOrDefault()  .Single()  .Last()  .ElementAt(i)  .Any()  .All(p)  .Contains(x)
.Count()  .Sum(x => ...)  .Average(...)  .Min()  .Max()  .MinBy(k)  .MaxBy(k)  .Aggregate((a, b) => ...)
.ToList()  .ToArray()  .ToDictionary(k, v)  .ToHashSet()
```

Nhớ: thực thi trì hoãn — chạy khi duyệt/`ToList()`.

## C.8 Phương thức (Ch. 10)

```csharp
static int Cong(int a, int b = 0) => a + b;            // tham số mặc định, expression-bodied
static void F(ref int x) { }   static void G(out int y) { y = 1; }   static int H(params int[] xs) => xs.Length;
static (int Min, int Max) MinMax(int[] a) => (a.Min(), a.Max());
Cong(1, b: 2);   F(ref v);   G(out int r);   var (min, max) = MinMax(a);
```

## C.9 Class, interface, kế thừa (Ch. 13–20)

```csharp
public class SinhVien(string ten) : Nguoi(ten), IComparable<SinhVien>      // primary ctor, kế thừa, interface
{
    private readonly List<string> _mon = [];                                 // field
    public string Ten { get; } = ten;                                        // property chỉ đọc
    public required int Tuoi { get; init; }                                  // bắt buộc, gán 1 lần
    public decimal Diem { get; private set; }
    public override string ToString() => Ten;                                // ghi đè
    public virtual void Chao() { }                                           // cho phép ghi đè
    public int CompareTo(SinhVien? o) => Diem.CompareTo(o?.Diem);
}
abstract class Hinh { public abstract double DienTich(); }
interface IThanhToan { void Tra(decimal t); decimal Phi(decimal t) => 0; }   // default interface method
record Diem(int X, int Y);   readonly record struct P(int X, int Y);   struct S { }   enum Mau { Do, Xanh }   [Flags] enum Q { A = 1, B = 2 }
static class MoRong { public static bool Chan(this int n) => n % 2 == 0; }   // extension method
```

Access: `public` (mọi nơi) · `private` (trong class) · `protected` (class + con) · `internal` (cùng assembly). Mặc định thành viên: `private`; class: `internal`.

## C.10 Exception (Ch. 21)

```csharp
try { ... }
catch (FormatException e) when (e.Message.Length > 0) { ... }       // cụ thể trước
catch (Exception) { throw; }                                         // ném lại: giữ stack trace
finally { ... }
throw new ArgumentException("msg", nameof(x));       ArgumentNullException.ThrowIfNull(x);
using var f = File.OpenRead(p);                      await using var s = ...;
```

## C.11 Generics, delegate, event (Ch. 22, 31)

```csharp
class Kho<T> where T : class, IComparable<T>, new() { }     T Max<T>(IEnumerable<T> d) where T : IComparable<T>
Func<int,int> f = x => x * 2;   Action<string> a = Console.WriteLine;   Predicate<int> p = x => x > 0;
public event EventHandler<MyArgs>? DaXong;   DaXong?.Invoke(this, new MyArgs());   obj.DaXong += Handler;   obj.DaXong -= Handler;
```

## C.12 Async và đa luồng (Ch. 33–34)

```csharp
async Task<int> LayAsync(CancellationToken ct = default) { await Task.Delay(100, ct); return 1; }
var kq = await LayAsync();                 await Task.WhenAll(t1, t2);     await Task.WhenAny(t1, t2);
await foreach (var x in nguonAsync) { }    await Task.Run(() => TinhNang());
lock (khoa) { }    Interlocked.Increment(ref n);    Parallel.For(0, n, i => { });    new SemaphoreSlim(2)
new ConcurrentDictionary<K,V>()    Channel.CreateBounded<int>(10)
```

Quy tắc: async xuyên suốt; không `.Result`/`.Wait()`; tránh `async void`; luôn truyền `CancellationToken`.

## C.13 I/O và JSON (Ch. 35–36)

```csharp
File.ReadAllText(p)  File.WriteAllText(p, s)  File.ReadLines(p)  File.AppendAllText(p, s)  File.Exists(p)  File.Delete(p)
await File.ReadAllTextAsync(p)      Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories)
Path.Combine(a, b)  Path.GetFileName(p)  Path.GetExtension(p)  Path.GetTempPath()
using var w = new StreamWriter(p); w.WriteLine("x");        using var r = new StreamReader(p); r.ReadLine();

string json = JsonSerializer.Serialize(obj, opts);        var o = JsonSerializer.Deserialize<T>(json, opts);
[JsonPropertyName("ten")]  [JsonIgnore]     new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
```

## C.14 Test xUnit (Ch. 38)

```csharp
[Fact] public void Ten_TinhHuong_KetQua() { /* Arrange - Act - Assert */ }
[Theory][InlineData(1, 2, 3)] public void Cong(int a, int b, int mongDoi) { Assert.Equal(mongDoi, a + b); }
Assert.Equal(mongDoi, thucTe)  Assert.True(x)  Assert.Null(x)  Assert.Empty(ds)  Assert.Single(ds)  Assert.Throws<T>(() => ...)
var m = Substitute.For<IX>();  m.Ham().Returns(5);  m.Received(1).Ham();
```

## C.15 Quy ước đặt tên (Ch. 15)

| Thành phần | Quy ước |
|-----------|---------|
| Class, record, struct, enum, namespace, method, property, event | `PascalCase` |
| Interface | `IPascalCase` |
| Tham số, biến cục bộ | `camelCase` |
| Field private | `_camelCase` |
| Hằng | `PascalCase` |
| Phương thức async | hậu tố `Async` |
| Kiểu generic | `T`, `TKey`, `TValue` |

## C.16 Phím tắt IDE hay dùng

| Việc | Visual Studio | VS Code |
|------|---------------|---------|
| Chạy debug / không debug | `F5` / `Ctrl+F5` | `F5` / `Ctrl+F5` |
| Breakpoint | `F9` | `F9` |
| Step Over / Into / Out | `F10` / `F11` / `Shift+F11` | `F10` / `F11` / `Shift+F11` |
| Gợi ý nhanh / sửa lỗi | `Ctrl+.` | `Ctrl+.` |
| Định dạng code | `Ctrl+K, Ctrl+D` | `Shift+Alt+F` |
| Đổi tên (rename) | `F2` | `F2` |
| Đến định nghĩa | `F12` | `F12` |
| Tìm mọi tham chiếu | `Shift+F12` | `Shift+F12` |
| Chạy test | `Ctrl+R, T` | qua Test Explorer |
