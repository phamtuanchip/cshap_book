using ThuVien.Models;
using ThuVien.Services;
using Console = System.Console;   // alias: vi du cu phap, khong bat buoc

var kho = new KhoSach();
kho.Them(new Sach { TieuDe = "Lap trinh C#", TacGia = "Nguyen Van A", NamXuatBan = 2025 });
kho.Them(new Sach { TieuDe = "ASP.NET Core", TacGia = "Tran Thi B", NamXuatBan = 2026 });
kho.Them(new Sach { TieuDe = "Thuat toan", TacGia = "Nguyen Van A", NamXuatBan = 2024 });

Console.WriteLine($"Kho co {kho.SoLuong} sach");
foreach (var s in kho.TimTheoTacGia("nguyen"))
    Console.WriteLine(s);

// Ten day du (fully qualified) khi khong muon using
var s2 = new ThuVien.Models.Sach { TieuDe = "X", TacGia = "Y" };
Console.WriteLine(s2);
