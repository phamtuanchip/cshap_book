using System.Globalization;
using System.Text;

// Dung culture bat bien de ket qua giong nhau tren moi may
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string s = "  Xin chao, C# va .NET  ";
Console.WriteLine($"[{s.Trim()}] dai {s.Trim().Length}");
Console.WriteLine(s.Trim().ToUpper());
Console.WriteLine(s.Contains("C#"));
Console.WriteLine(s.Trim().Replace("chao", "tam biet"));
Console.WriteLine(s.Trim().Substring(4, 4));   // "chao"
Console.WriteLine(s.IndexOf("C#"));

// Tach & ghep
string csv = "an,binh,chi";
string[] ten = csv.Split(',');
Console.WriteLine(string.Join(" | ", ten));

// Noi suy & dinh dang
double gia = 1234567.891;
DateTime ngay = new(2025, 3, 9);
Console.WriteLine($"Gia: {gia:N2} | {gia:F1} | {0.256:P1}");
Console.WriteLine($"Ngay: {ngay:dd/MM/yyyy}");
Console.WriteLine($"Canh le: [{"ab",6}] [{"ab",-6}]");

// Chuoi bat bien
string a = "abc";
string b = a.ToUpper();
Console.WriteLine($"{a} {b}");   // a khong doi

// So sanh
Console.WriteLine("abc" == "abc");
Console.WriteLine(string.Equals("ABC", "abc", StringComparison.OrdinalIgnoreCase));
Console.WriteLine(string.Compare("a", "b", StringComparison.Ordinal) < 0);

// Chuoi rong / null
Console.WriteLine(string.IsNullOrWhiteSpace("   "));

// Ghep nhieu lan: dung StringBuilder
var sb = new StringBuilder();
for (int i = 1; i <= 5; i++) sb.Append(i).Append(',');
sb.Length--;   // bo dau phay cuoi
Console.WriteLine(sb.ToString());

// Chuoi nguyen van va raw string
string duongDan = @"C:\Users\Tuan\file.txt";
string json = """
    { "ten": "Tuan", "tuoi": 25 }
    """;
Console.WriteLine(duongDan);
Console.WriteLine(json);

// Duyet ky tu
int nguyenAm = 0;
foreach (char c in "Lap trinh C#")
    if ("aeiouAEIOU".Contains(c)) nguyenAm++;
Console.WriteLine($"So nguyen am: {nguyenAm}");

// Dao nguoc chuoi
char[] arr = "hello".ToCharArray();
Array.Reverse(arr);
Console.WriteLine(new string(arr));
