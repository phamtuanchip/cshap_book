// Khai bao & khoi tao
int[] diem = [8, 6, 9, 7, 10];
Console.WriteLine($"Phan tu dau: {diem[0]}, cuoi: {diem[^1]}, so luong: {diem.Length}");

// Duyet & tinh toan
int tong = 0;
foreach (int d in diem) tong += d;
Console.WriteLine($"Tong = {tong}, TB = {(double)tong / diem.Length:F2}");

// Mang moi co gia tri mac dinh
int[] rong = new int[3];        // 0, 0, 0
string?[] ten = new string?[2]; // null, null
Console.WriteLine($"{rong[0]} {(ten[0] is null ? "null" : ten[0])}");

// Sap xep, dao, tim kiem
Array.Sort(diem);
Console.WriteLine(string.Join(", ", diem));
Array.Reverse(diem);
Console.WriteLine(string.Join(", ", diem));
Console.WriteLine($"Vi tri cua 9: {Array.IndexOf(diem, 9)}");

// Lat cat (range)
int[] ba = diem[..3];
int[] giua = diem[1..^1];
Console.WriteLine($"{string.Join(",", ba)} | {string.Join(",", giua)}");

// Mang 2 chieu (hinh chu nhat)
int[,] luoi = { { 1, 2, 3 }, { 4, 5, 6 } };
for (int r = 0; r < luoi.GetLength(0); r++)
{
    for (int c = 0; c < luoi.GetLength(1); c++)
        Console.Write($"{luoi[r, c]} ");
    Console.WriteLine();
}

// Jagged array (mang cua cac mang, moi hang dai khac nhau)
int[][] tamGiac = new int[4][];
for (int i = 0; i < tamGiac.Length; i++)
{
    tamGiac[i] = new int[i + 1];
    for (int j = 0; j <= i; j++) tamGiac[i][j] = j + 1;
}
foreach (var hang in tamGiac) Console.WriteLine(string.Join(" ", hang));

// Mang la kieu tham chieu!
int[] a = [1, 2, 3];
int[] b = a;   // b tro cung mang voi a
b[0] = 99;
Console.WriteLine($"a[0] = {a[0]}");
int[] c2 = (int[])a.Clone();   // ban sao doc lap
c2[0] = 1;
Console.WriteLine($"a[0] = {a[0]}, c2[0] = {c2[0]}");

// IndexOutOfRangeException
try
{
    int vt = 5;
    Console.WriteLine(a[vt]);
}
catch (IndexOutOfRangeException e)
{
    Console.WriteLine($"Loi: {e.Message}");
}
