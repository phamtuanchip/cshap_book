// Cac phuong thuc duoc dat trong class Ham (o cuoi file) de co the overload.
Console.WriteLine(Ham.Binh(7));
Console.WriteLine($"{Ham.Cong(1, 2)}, {Ham.Cong(1.5, 2.5)}, {Ham.Cong(1, 2, 3)}");
Console.WriteLine(Ham.ChaoHoi("Tuan"));
Console.WriteLine(Ham.ChaoHoi("Tuan", viet: true));
Console.WriteLine(Ham.ChaoHoi(loiChao: "Hi", ten: "An"));
Console.WriteLine(Ham.Tong(1, 2, 3, 4));

int p = 1, q = 2;
Ham.HoanDoi(ref p, ref q);
Console.WriteLine($"p={p}, q={q}");

if (Ham.ChiaCoDu(17, 5, out int th, out int du)) Console.WriteLine($"17 = 5*{th} + {du}");

var (min, max) = Ham.MinMax([4, 9, 1, 7]);
Console.WriteLine($"min={min}, max={max}");

int v = 10;
Ham.TangSai(v);
Console.WriteLine($"Sau TangSai: {v}");
Ham.TangDung(ref v);
Console.WriteLine($"Sau TangDung: {v}");

Console.WriteLine($"10! = {Ham.GiaiThua(10)}");

// Ham cuc bo (local function) dung duoc bien cua noi bao quanh
int dem = 0;
void Dem() => dem++;
Dem();
Dem();
Console.WriteLine($"dem = {dem}");

static class Ham
{
    public static int Binh(int x) => x * x;

    // Overloading: cung ten, khac danh sach tham so
    public static int Cong(int a, int b) => a + b;
    public static double Cong(double a, double b) => a + b;
    public static int Cong(int a, int b, int c) => a + b + c;

    // Tham so tuy chon & tham so dat ten
    public static string ChaoHoi(string ten, string loiChao = "Xin chao", bool viet = false)
        => viet ? $"{loiChao.ToUpper()}, {ten}!" : $"{loiChao}, {ten}!";

    // params: so luong tham so bat ky
    public static int Tong(params int[] so)
    {
        int t = 0;
        foreach (int x in so) t += x;
        return t;
    }

    // ref: truyen tham chieu (doc + ghi)
    public static void HoanDoi(ref int a, ref int b) => (a, b) = (b, a);

    // out: tra ve them gia tri
    public static bool ChiaCoDu(int a, int b, out int thuong, out int du)
    {
        if (b == 0)
        {
            thuong = 0;
            du = 0;
            return false;
        }
        thuong = a / b;
        du = a % b;
        return true;
    }

    // Tuple: tra ve nhieu gia tri gon hon
    public static (int Min, int Max) MinMax(int[] mang) => (mang.Min(), mang.Max());

    // Truyen theo gia tri vs theo tham chieu
    public static void TangSai(int x) => x++;   // chi tang ban sao
    public static void TangDung(ref int x) => x++;

    // De quy
    public static long GiaiThua(int n) => n <= 1 ? 1 : n * GiaiThua(n - 1);
}
