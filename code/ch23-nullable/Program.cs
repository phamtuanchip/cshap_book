// 1. Nullable value type: int? = int hoac null
int? tuoi = null;
Console.WriteLine($"HasValue: {tuoi.HasValue}");
Console.WriteLine($"GetValueOrDefault: {tuoi.GetValueOrDefault(-1)}");
tuoi = 30;
Console.WriteLine($"Value: {tuoi.Value}");

int? a = 5, b = null;
Console.WriteLine($"5 + null = {(a + b)?.ToString() ?? "null"}");   // lan truyen null

// 2. Nullable reference type (bat boi <Nullable>enable</Nullable>)
string ten = "An";        // KHONG duoc null
string? tenTuyChon = null; // co the null

Console.WriteLine(ten.Length);
// Console.WriteLine(tenTuyChon.Length);   // CANH BAO CS8602: co the null
Console.WriteLine(tenTuyChon?.Length ?? 0);

// 3. Cac toan tu xu ly null
string? s = null;
Console.WriteLine(s ?? "mac dinh");     // ??  : gia tri du phong
s ??= "moi gan";                          // ??= : gan neu dang null
Console.WriteLine(s);
Console.WriteLine(s?.ToUpper());          // ?.  : an toan

var nguoi = new NguoiDung("Binh", null);
Console.WriteLine(nguoi.DiaChi?.ThanhPho ?? "chua co dia chi");
var nguoi2 = new NguoiDung("Chi", new DiaChi("Ha Noi"));
Console.WriteLine(nguoi2.DiaChi?.ThanhPho ?? "chua co dia chi");

int[]? mang = null;
Console.WriteLine(mang?[0]);   // ?[] : an toan voi mang/list null

// 4. Kiem tra null bang pattern
if (tenTuyChon is null) Console.WriteLine("tenTuyChon la null");
if (s is not null) Console.WriteLine("s khong null");

// 5. Tra ve null co chu dich: TryXxx hoac ket qua nullable
Console.WriteLine(TimNguoi("An")?.Ten ?? "khong tim thay");
Console.WriteLine(TimNguoi("Zzz")?.Ten ?? "khong tim thay");

// 6. Bao ve tham so
try
{
    XuLy(null!);
}
catch (ArgumentNullException e)
{
    Console.WriteLine($"ArgumentNullException: {e.ParamName}");
}

// 7. Toan tu ! (null-forgiving): "toi biet no khong null" - dung than trong!
string? chuaChac = LayChuoi();
Console.WriteLine(chuaChac!.Length);   // neu that su null se NullReferenceException

static NguoiDung? TimNguoi(string ten) => ten == "An" ? new NguoiDung("An", null) : null;
static string? LayChuoi() => "co gia tri";

static void XuLy(string dauVao)
{
    ArgumentNullException.ThrowIfNull(dauVao);
    Console.WriteLine(dauVao.Length);
}

record DiaChi(string ThanhPho);

record NguoiDung(string Ten, DiaChi? DiaChi);
