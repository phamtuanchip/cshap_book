// Kieu so nguyen va so thuc
int tuoi = 25;
long danSo = 8_000_000_000L;
double chieuCao = 1.75;
decimal gia = 19.99m;   // dung cho tien te
bool daDangKy = true;
char kyTu = 'A';
string ten = "Tuan";

Console.WriteLine($"{ten}, {tuoi} tuoi, cao {chieuCao} m, dan so {danSo}");
Console.WriteLine($"Gia: {gia}, da dang ky: {daDangKy}, ky tu: {kyTu}");

// Gioi han cua kieu
Console.WriteLine($"int: {int.MinValue} .. {int.MaxValue}");

// var: trinh bien dich tu suy ra kieu
var soLuong = 3;                // int
var thanhTien = soLuong * gia;  // decimal
Console.WriteLine($"Thanh tien: {thanhTien}");

// Hang so
const double Pi = 3.14159;
Console.WriteLine($"Chu vi hinh tron r=2: {2 * Pi * 2}");

// Ep kieu
double d = 9.99;
int a = (int)d;   // ep kieu tuong minh: cat phan thap phan -> 9
long b = tuoi;    // ep kieu ngam dinh: an toan
Console.WriteLine($"(int)9.99 = {a}, long = {b}");

// Chuyen chuoi -> so
int n1 = int.Parse("123");
Console.WriteLine(int.TryParse("abc", out int n2) ? $"OK {n2}" : "abc khong phai so");
Console.WriteLine(n1 + 1);

// Tran so (mac dinh KHONG bao loi!)
int lon = int.MaxValue;
Console.WriteLine($"int.MaxValue + 1 = {unchecked(lon + 1)}");
try
{
    Console.WriteLine(checked(lon + 1));
}
catch (OverflowException)
{
    Console.WriteLine("checked: phat hien tran so");
}

// Sai so so thuc
Console.WriteLine($"0.1 + 0.2 == 0.3 ? {0.1 + 0.2 == 0.3}");
Console.WriteLine($"0.1m + 0.2m == 0.3m ? {0.1m + 0.2m == 0.3m}");
