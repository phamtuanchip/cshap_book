int a = 17, b = 5;
Console.WriteLine($"{a} + {b} = {a + b}");
Console.WriteLine($"{a} / {b} = {a / b}   (chia nguyen)");
Console.WriteLine($"{a} % {b} = {a % b}   (chia lay du)");
Console.WriteLine($"{a} / {b}.0 = {a / (double)b}");

// Tang/giam
int i = 5;
int x = i++;   // x = 5, i = 6
int y = ++i;   // i = 7, y = 7
Console.WriteLine($"x={x}, y={y}, i={i}");

// Gan ket hop
int tong = 10;
tong += 5;
tong *= 2;
Console.WriteLine($"tong = {tong}");

// So sanh & logic
int tuoi = 20;
bool nguoiLon = tuoi >= 18;
bool coThe = true;
Console.WriteLine($"{nguoiLon && coThe}, {nguoiLon || !coThe}");

// Do uu tien
Console.WriteLine(2 + 3 * 4);     // 14
Console.WriteLine((2 + 3) * 4);   // 20

// Ngan mach (short-circuit)
int[] mang = [];
bool ok = mang.Length > 0 && mang[0] > 0;   // khong loi vi ve trai da sai
Console.WriteLine($"ok = {ok}");

// Toan tu ba ngoi, null
string? ten = null;
Console.WriteLine(ten ?? "(khong ten)");
ten ??= "Mac dinh";
Console.WriteLine(ten);
Console.WriteLine(tuoi >= 18 ? "Nguoi lon" : "Tre em");

// Toan tu bit
Console.WriteLine($"6 & 3 = {6 & 3}, 6 | 3 = {6 | 3}, 6 ^ 3 = {6 ^ 3}, 1 << 4 = {1 << 4}");
