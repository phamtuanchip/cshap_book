using System.Reflection;

// 1. Reflection: khao sat kieu luc chay
Type kieu = typeof(SanPham);
Console.WriteLine($"Kieu: {kieu.Name}, namespace: '{kieu.Namespace}'");
foreach (var pr in kieu.GetProperties())
    Console.WriteLine($"  property {pr.Name}: {pr.PropertyType.Name}");
foreach (var m in kieu.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    Console.WriteLine($"  method   {m.Name}()");

// 2. Tao doi tuong va goi thanh vien bang reflection
var sp = (SanPham)Activator.CreateInstance(kieu)!;
kieu.GetProperty("Ten")!.SetValue(sp, "Chuot");
kieu.GetProperty("Gia")!.SetValue(sp, 150_000m);
Console.WriteLine(kieu.GetMethod("MoTa")!.Invoke(sp, null));

// 3. Attribute tu dinh nghia + doc bang reflection
Console.WriteLine("--- Validator dua tren attribute ---");
var tot = new SanPham { Ten = "Ban phim", Gia = 500_000m, TonKho = 5 };
var xau = new SanPham { Ten = "", Gia = -10m, TonKho = 5000 };
foreach (var doiTuong in new[] { tot, xau })
{
    var loi = KiemTra(doiTuong);
    Console.WriteLine(loi.Count == 0 ? "Hop le" : "Loi: " + string.Join("; ", loi));
}

// 4. Attribute co san: Obsolete, doc thong tin tu attribute
var cu = typeof(Chao).GetMethod(nameof(Chao.CuaPhienBanCu))!;
var obs = cu.GetCustomAttribute<ObsoleteAttribute>();
Console.WriteLine($"Obsolete: {obs?.Message}");

// 5. Tim tat ca kieu cai dat interface trong assembly (mau "plugin")
var cacPlugin = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
    .Select(t => (IPlugin)Activator.CreateInstance(t)!)
    .OrderBy(p => p.Ten);
foreach (var p in cacPlugin) Console.WriteLine($"Plugin: {p.Ten}");

// Kiem tra doi tuong bang attribute: quet property, doc attribute, kiem tra gia tri
static List<string> KiemTra(object o)
{
    var loi = new List<string>();
    foreach (var pr in o.GetType().GetProperties())
    {
        var giaTri = pr.GetValue(o);
        if (pr.GetCustomAttribute<BatBuocAttribute>() is not null && (giaTri is null || (giaTri is string s && s.Length == 0)))
            loi.Add($"{pr.Name} la bat buoc");
        if (pr.GetCustomAttribute<KhoangAttribute>() is { } k && giaTri is IConvertible c)
        {
            double v = c.ToDouble(null);
            if (v < k.Min || v > k.Max) loi.Add($"{pr.Name} phai trong [{k.Min}, {k.Max}]");
        }
    }
    return loi;
}

// Attribute: metadata gan vao ma nguon
[AttributeUsage(AttributeTargets.Property)]
class BatBuocAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
class KhoangAttribute(double min, double max) : Attribute
{
    public double Min { get; } = min;
    public double Max { get; } = max;
}

class SanPham
{
    [BatBuoc] public string Ten { get; set; } = "";
    [Khoang(0, 1_000_000_000)] public decimal Gia { get; set; }
    [Khoang(0, 1000)] public int TonKho { get; set; }

    public string MoTa() => $"{Ten} - {Gia:N0}";
}

static class Chao
{
    [Obsolete("Dung Chao moi thay the")]
    public static void CuaPhienBanCu() { }
}

interface IPlugin { string Ten { get; } }
class PluginA : IPlugin { public string Ten => "A - dinh dang PDF"; }
class PluginB : IPlugin { public string Ten => "B - xuat Excel"; }
abstract class PluginTruuTuong : IPlugin { public abstract string Ten { get; } }
