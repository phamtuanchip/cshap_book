using System.Globalization;
using QuanLyKho.Core.Models;
using QuanLyKho.Core.Services;
using QuanLyKho.Core.Storage;

// Composition root: noi DUY NHAT lap rap cac doi tuong cu the (DIP - Chuong 42)
bool demo = args.Contains("--demo");
string duongDan = demo
    ? Path.Combine(Path.GetTempPath(), $"quanlykho-demo-{Guid.NewGuid():N}.json")
    : LayThamSo(args, "--file") ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuanLyKho", "kho.json");

var dichVuKho = new KhoService(new JsonKhoRepository(duongDan), TimeProvider.System);
await dichVuKho.KhoiTaoAsync();

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine($"=== QUAN LY KHO === (du lieu: {duongDan})");

try
{
    if (demo) await ChayDemoAsync(dichVuKho);
    else await ChayMenuAsync(dichVuKho);
}
finally
{
    if (demo && File.Exists(duongDan)) File.Delete(duongDan);
}

// ---------------------------------------------------------------- Che do demo (kich ban co san)
static async Task ChayDemoAsync(KhoService kho)
{
    await kho.ThemSanPhamAsync(new SanPham("LT01", "Laptop Dell", "Thiet bi", 18_000_000m, 4));
    await kho.ThemSanPhamAsync(new SanPham("CH01", "Chuot khong day", "Phu kien", 150_000m, 30));
    await kho.ThemSanPhamAsync(new SanPham("BP01", "Ban phim co", "Phu kien", 500_000m, 12));
    await kho.ThemSanPhamAsync(new SanPham("MH01", "Man hinh 24 inch", "Thiet bi", 3_500_000m, 2));

    await kho.NhapKhoAsync("CH01", 20, "Nhap them tu nha cung cap A");
    await kho.XuatKhoAsync("LT01", 3, "Ban cho khach B");

    try
    {
        await kho.XuatKhoAsync("MH01", 10);
    }
    catch (KhongDuHangException e)
    {
        Console.WriteLine($"[Loi nghiep vu] {e.Message}");
    }

    try
    {
        await kho.NhapKhoAsync("XX99", 1);
    }
    catch (KhongTimThayException e)
    {
        Console.WriteLine($"[Loi nghiep vu] {e.Message}");
    }

    InDanhSach(kho.TatCa);
    Console.WriteLine($"\nTong gia tri ton kho: {kho.TongGiaTri:N0}");

    Console.WriteLine("\n-- San pham sap het --");
    InDanhSach(kho.SapHet());

    Console.WriteLine("\n-- Bao cao theo nhom --");
    foreach (var n in kho.BaoCaoTheoNhom())
        Console.WriteLine($"{n.Nhom,-10} {n.SoSanPham} sp, ton {n.TongTon,4}, gia tri {n.GiaTri,15:N0}");

    Console.WriteLine("\n-- Lich su giao dich --");
    InLichSu(kho.LichSu());
}

// ---------------------------------------------------------------- Che do menu tuong tac
static async Task ChayMenuAsync(KhoService kho)
{
    while (true)
    {
        Console.WriteLine("""

            1. Danh sach san pham      5. Tim kiem
            2. Them san pham           6. San pham sap het
            3. Nhap kho                7. Bao cao theo nhom
            4. Xuat kho                8. Lich su giao dich
            0. Thoat
            """);
        Console.Write("Chon: ");
        string? chon = Console.ReadLine()?.Trim();

        try
        {
            switch (chon)
            {
                case "1":
                    InDanhSach(kho.TatCa);
                    Console.WriteLine($"Tong gia tri: {kho.TongGiaTri:N0}");
                    break;
                case "2":
                    await kho.ThemSanPhamAsync(new SanPham(
                        Hoi("Ma"), Hoi("Ten"), Hoi("Nhom"), DocSoThuc("Don gia"), DocSoNguyen("Ton kho", 0)));
                    Console.WriteLine("Da them.");
                    break;
                case "3":
                    await kho.NhapKhoAsync(Hoi("Ma san pham"), DocSoNguyen("So luong nhap", 1), Hoi("Ghi chu (co the bo trong)"));
                    Console.WriteLine("Da nhap kho.");
                    break;
                case "4":
                    await kho.XuatKhoAsync(Hoi("Ma san pham"), DocSoNguyen("So luong xuat", 1), Hoi("Ghi chu (co the bo trong)"));
                    Console.WriteLine("Da xuat kho.");
                    break;
                case "5":
                    InDanhSach(kho.TimKiem(Hoi("Tu khoa")));
                    break;
                case "6":
                    InDanhSach(kho.SapHet());
                    break;
                case "7":
                    foreach (var n in kho.BaoCaoTheoNhom())
                        Console.WriteLine($"{n.Nhom,-12} {n.SoSanPham} sp, ton {n.TongTon}, gia tri {n.GiaTri:N0}");
                    break;
                case "8":
                    string ma = Hoi("Ma san pham (bo trong = tat ca)");
                    InLichSu(kho.LichSu(string.IsNullOrWhiteSpace(ma) ? null : ma));
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Lua chon khong hop le.");
                    break;
            }
        }
        catch (KhoException e)   // loi nghiep vu: bao nguoi dung, khong lam sap chuong trinh
        {
            Console.WriteLine($"Loi: {e.Message}");
        }
    }
}

// ---------------------------------------------------------------- Nhap/xuat tien ich
static string? LayThamSo(string[] thamSo, string ten)
{
    int vt = Array.IndexOf(thamSo, ten);
    return vt >= 0 && vt + 1 < thamSo.Length ? thamSo[vt + 1] : null;
}

static string Hoi(string cauHoi)
{
    Console.Write($"{cauHoi}: ");
    return Console.ReadLine()?.Trim() ?? "";
}

static int DocSoNguyen(string cauHoi, int toiThieu)
{
    while (true)
    {
        if (int.TryParse(Hoi(cauHoi), out int so) && so >= toiThieu) return so;
        Console.WriteLine($"Vui long nhap so nguyen >= {toiThieu}.");
    }
}

static decimal DocSoThuc(string cauHoi)
{
    while (true)
    {
        if (decimal.TryParse(Hoi(cauHoi), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal so) && so >= 0)
            return so;
        Console.WriteLine("Vui long nhap so >= 0 (dung dau cham cho phan thap phan).");
    }
}

static void InDanhSach(IEnumerable<SanPham> ds)
{
    Console.WriteLine($"{"Ma",-6} {"Ten",-20} {"Nhom",-10} {"Don gia",12} {"Ton",5}");
    int dem = 0;
    foreach (var s in ds)
    {
        Console.WriteLine($"{s.Ma,-6} {s.Ten,-20} {s.Nhom,-10} {s.DonGia,12:N0} {s.TonKho,5}");
        dem++;
    }
    if (dem == 0) Console.WriteLine("(khong co san pham nao)");
}

static void InLichSu(IEnumerable<GiaoDich> ds)
{
    foreach (var g in ds)
        Console.WriteLine($"{g.ThoiGian:dd/MM HH:mm} {g.Loai,-4} {g.MaSanPham,-6} x{g.SoLuong,-4} {g.GhiChu}");
}
