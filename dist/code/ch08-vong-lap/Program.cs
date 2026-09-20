// for
for (int i = 1; i <= 5; i++) Console.Write($"{i} ");
Console.WriteLine();

// while: tinh tong cac chu so
int n = 12345, tong = 0;
while (n > 0)
{
    tong += n % 10;
    n /= 10;
}
Console.WriteLine($"Tong chu so: {tong}");

// do-while: chay it nhat 1 lan
int dem = 0;
do
{
    dem++;
} while (dem < 3);
Console.WriteLine($"dem = {dem}");

// foreach
string[] trai = ["tao", "cam", "xoai"];
foreach (string t in trai) Console.WriteLine(t);

// break / continue
for (int i = 1; i <= 10; i++)
{
    if (i % 2 == 0) continue;   // bo qua so chan
    if (i > 7) break;           // dung han
    Console.Write($"{i} ");
}
Console.WriteLine();

// Vong lap long nhau: bang cuu chuong
for (int a = 2; a <= 4; a++)
{
    for (int b = 1; b <= 5; b++)
        Console.Write($"{a}x{b}={a * b,-4}");
    Console.WriteLine();
}

// Tim so nguyen to nho hon 30
for (int so = 2; so < 30; so++)
{
    bool nguyenTo = true;
    for (int u = 2; u * u <= so; u++)
    {
        if (so % u == 0)
        {
            nguyenTo = false;
            break;
        }
    }
    if (nguyenTo) Console.Write($"{so} ");
}
Console.WriteLine();
