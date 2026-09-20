var sanPham = new List<SanPham>
{
    new("Chuot", "Phu kien", 150_000m, 30),
    new("Ban phim", "Phu kien", 500_000m, 12),
    new("Man hinh", "Thiet bi", 3_500_000m, 5),
    new("Laptop", "Thiet bi", 18_000_000m, 3),
    new("Tai nghe", "Phu kien", 200_000m, 0),
};

// Where + Select
var re = sanPham.Where(s => s.Gia < 1_000_000m).Select(s => s.Ten);
Console.WriteLine($"Re (<1tr): {string.Join(", ", re)}");

// Query syntax (tuong duong)
var re2 = from s in sanPham where s.Gia < 1_000_000m select s.Ten;
Console.WriteLine($"Query syntax: {string.Join(", ", re2)}");

// OrderBy / ThenBy
Console.WriteLine("Theo gia giam: " + string.Join(", ", sanPham.OrderByDescending(s => s.Gia).Select(s => s.Ten)));

// First / FirstOrDefault / Single / Any / All / Count
Console.WriteLine($"Dau tien het hang: {sanPham.FirstOrDefault(s => s.TonKho == 0)?.Ten}");
Console.WriteLine($"Co san pham > 10tr? {sanPham.Any(s => s.Gia > 10_000_000m)}");
Console.WriteLine($"Tat ca con hang? {sanPham.All(s => s.TonKho > 0)}");
Console.WriteLine($"So phu kien: {sanPham.Count(s => s.NhomHang == "Phu kien")}");

// Tong hop
Console.WriteLine($"Tong gia tri ton kho: {sanPham.Sum(s => s.Gia * s.TonKho):N0}");
Console.WriteLine($"Gia TB: {sanPham.Average(s => s.Gia):N0}");
Console.WriteLine($"Dat nhat: {sanPham.MaxBy(s => s.Gia)?.Ten}, re nhat: {sanPham.MinBy(s => s.Gia)?.Ten}");

// GroupBy
Console.WriteLine("--- GroupBy ---");
foreach (var nhom in sanPham.GroupBy(s => s.NhomHang))
    Console.WriteLine($"{nhom.Key}: {nhom.Count()} sp, tong ton {nhom.Sum(s => s.TonKho)}");

// Doi tuong an danh (anonymous type) + Select
var tomTat = sanPham.Select(s => new { s.Ten, ThanhTien = s.Gia * s.TonKho });
foreach (var t in tomTat.Take(2)) Console.WriteLine($"{t.Ten}: {t.ThanhTien:N0}");

// Join
var nhomInfo = new List<Nhom> { new("Phu kien", "Linh kien nho"), new("Thiet bi", "May moc lon") };
var join = sanPham.Join(nhomInfo, s => s.NhomHang, n => n.Ten, (s, n) => $"{s.Ten} ({n.MoTa})");
Console.WriteLine($"Join: {string.Join("; ", join.Take(3))}");

// Phan trang
int trang = 2, kichThuoc = 2;
var phanTrang = sanPham.OrderBy(s => s.Ten).Skip((trang - 1) * kichThuoc).Take(kichThuoc);
Console.WriteLine($"Trang {trang}: {string.Join(", ", phanTrang.Select(s => s.Ten))}");

// Chuyen doi ket qua
var tuDien = sanPham.ToDictionary(s => s.Ten, s => s.Gia);
Console.WriteLine($"ToDictionary: Laptop = {tuDien["Laptop"]:N0}");
var cacNhom = sanPham.Select(s => s.NhomHang).Distinct().ToList();
Console.WriteLine($"Distinct: {string.Join(", ", cacNhom)}");

// Aggregate
var ghep = new[] { "a", "b", "c" }.Aggregate((acc, x) => acc + "-" + x);
Console.WriteLine($"Aggregate: {ghep}");

// Deferred execution: truy van chay luc DUYET, khong phai luc tao
var danhSach = new List<int> { 1, 2, 3 };
var chan = danhSach.Where(x => x % 2 == 0);   // chua chay
danhSach.Add(4);
Console.WriteLine($"Deferred: {string.Join(",", chan)}");   // 2,4 (thay ca 4)

var daChot = danhSach.Where(x => x % 2 == 0).ToList();   // chay ngay, luu ket qua
danhSach.Add(6);
Console.WriteLine($"ToList: {string.Join(",", daChot)}");   // 2,4 (khong co 6)

record SanPham(string Ten, string NhomHang, decimal Gia, int TonKho);

record Nhom(string Ten, string MoTa);
