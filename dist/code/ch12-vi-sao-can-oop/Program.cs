// ===== Cach 1: lap trinh thu tuc - du lieu roi rac =====
string[] tenSv = ["An", "Binh"];
double[] diemSv = [8.5, 6.0];

for (int i = 0; i < tenSv.Length; i++)
    Console.WriteLine($"[thu tuc] {tenSv[i]}: {diemSv[i]}");
// Them "ngay sinh", "lop"... => phai them mang moi va sua moi cho dung chung.
// Sap xep theo diem phai nho hoan doi CA HAI mang, rat de sai.

// ===== Cach 2: huong doi tuong - gom du lieu + hanh vi =====
var danhSach = new List<SinhVien>
{
    new("An", 8.5),
    new("Binh", 6.0),
};

foreach (var sv in danhSach)
    Console.WriteLine($"[oop] {sv.MoTa()} - {sv.XepLoai()}");

// Sap xep chi can mot danh sach
danhSach.Sort((x, y) => y.Diem.CompareTo(x.Diem));
Console.WriteLine($"Diem cao nhat: {danhSach[0].Ten}");

class SinhVien(string ten, double diem)
{
    public string Ten { get; } = ten;
    public double Diem { get; } = diem;

    public string MoTa() => $"{Ten}: {Diem}";

    public string XepLoai() => Diem switch
    {
        >= 8 => "Gioi",
        >= 5 => "Trung binh",
        _ => "Yeu",
    };
}
