// if / else if / else
int diem = 78;
if (diem >= 90) Console.WriteLine("Xuat sac");
else if (diem >= 75) Console.WriteLine("Gioi");
else if (diem >= 50) Console.WriteLine("Trung binh");
else Console.WriteLine("Yeu");

// switch statement
int thu = 3;
switch (thu)
{
    case 2:
    case 3:
    case 4:
    case 5:
    case 6:
        Console.WriteLine("Ngay lam viec");
        break;
    case 7:
        Console.WriteLine("Thu bay");
        break;
    default:
        Console.WriteLine("Khac");
        break;
}

// switch expression
string TenThu(int t) => t switch
{
    2 => "Thu hai",
    3 => "Thu ba",
    >= 4 and <= 6 => "Giua tuan",
    7 => "Thu bay",
    8 => "Chu nhat",
    _ => "Khong hop le",
};
Console.WriteLine(TenThu(3));
Console.WriteLine(TenThu(5));

// Pattern matching theo kieu
object?[] doiTuong = [42, 7, "chao", 3.14, null];
foreach (var o in doiTuong)
{
    string mota = o switch
    {
        int n when n > 40 => $"so nguyen lon: {n}",
        int n => $"so nguyen: {n}",
        string s => $"chuoi dai {s.Length}",
        null => "null",
        _ => $"kieu khac: {o.GetType().Name}",
    };
    Console.WriteLine(mota);
}

// Phan loai theo tuple
var (nong, am) = (true, false);
string thoiTiet = (nong, am) switch
{
    (true, true) => "Oi buc",
    (true, false) => "Nang nong",
    (false, true) => "Am uot",
    _ => "De chiu",
};
Console.WriteLine(thoiTiet);
