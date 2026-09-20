using System.ComponentModel.DataAnnotations;

namespace WebMvc.Models;

// ---- Model (du lieu) ----
public class SanPham
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public decimal Gia { get; set; }
    public int Ton { get; set; }
    public string GiaVon { get; set; } = "bi mat";       // truong KHONG bao gio duoc hien/nhan tu form
}

// ---- ViewModel: hinh dang du lieu danh rieng cho MOT view ----
public class SanPhamForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã")]
    [RegularExpression("^[A-Z]{2}[0-9]{3}$", ErrorMessage = "Mã gồm 2 chữ HOA và 3 số, ví dụ LT001")]
    [Display(Name = "Mã sản phẩm")]
    public string? Ma { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên từ 2 đến 100 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string? Ten { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Giá không hợp lệ")]
    [Display(Name = "Giá (đồng)")]
    public decimal Gia { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Tồn kho không hợp lệ")]
    [Display(Name = "Tồn kho")]
    public int Ton { get; set; }
}

public class DanhSachSanPhamViewModel
{
    public string? TuKhoa { get; set; }
    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; } = 1;
    public IReadOnlyList<SanPham> SanPhams { get; set; } = [];
}

public class SanPhamStore
{
    private readonly Lock _khoa = new();
    private readonly List<SanPham> _ds =
    [
        new() { Id = 1, Ma = "CH001", Ten = "Chuột không dây", Gia = 150_000, Ton = 30 },
        new() { Id = 2, Ma = "BP001", Ten = "Bàn phím cơ", Gia = 500_000, Ton = 12 },
        new() { Id = 3, Ma = "MH001", Ten = "Màn hình 24 inch", Gia = 3_500_000, Ton = 5 },
        new() { Id = 4, Ma = "LT001", Ten = "Laptop Dell", Gia = 18_000_000, Ton = 3 },
    ];
    private int _id = 5;

    public (List<SanPham> Muc, int TongSo) Tim(string? tuKhoa, int trang, int kichThuoc)
    {
        lock (_khoa)
        {
            var q = _ds.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(tuKhoa))
                q = q.Where(s => s.Ten.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) || s.Ma.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase));
            var ds = q.OrderBy(s => s.Id).ToList();
            return (ds.Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToList(), ds.Count);
        }
    }

    public SanPham? Lay(int id) { lock (_khoa) return _ds.FirstOrDefault(s => s.Id == id); }
    public bool TonTaiMa(string ma, int? boQuaId = null) { lock (_khoa) return _ds.Any(s => s.Ma == ma && s.Id != boQuaId); }

    public SanPham Them(SanPhamForm f)
    {
        lock (_khoa)
        {
            var sp = new SanPham { Id = _id++, Ma = f.Ma!, Ten = f.Ten!.Trim(), Gia = f.Gia, Ton = f.Ton };
            _ds.Add(sp);
            return sp;
        }
    }

    public bool Sua(int id, SanPhamForm f)
    {
        lock (_khoa)
        {
            var sp = _ds.FirstOrDefault(s => s.Id == id);
            if (sp is null) return false;
            sp.Ma = f.Ma!; sp.Ten = f.Ten!.Trim(); sp.Gia = f.Gia; sp.Ton = f.Ton;
            return true;
        }
    }

    public bool Xoa(int id) { lock (_khoa) return _ds.RemoveAll(s => s.Id == id) > 0; }
}
