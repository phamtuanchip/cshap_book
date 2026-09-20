namespace CuaHang.Data;

public class Nhom
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public List<SanPham> SanPhams { get; set; } = [];            // 1 - nhieu
}

public class SanPham
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public decimal Gia { get; set; }
    public int Ton { get; set; }
    public bool DaXoa { get; set; }                               // xoa mem (soft delete)
    public int PhienBan { get; set; }                             // token kiem soat xung dot dong thoi

    public int NhomId { get; set; }
    public Nhom Nhom { get; set; } = null!;
    public List<The> The { get; set; } = [];                      // nhieu - nhieu
    public List<ChiTietDon> ChiTiets { get; set; } = [];
}

public class The
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public List<SanPham> SanPhams { get; set; } = [];
}

// Value object (kieu "so huu"): khong co Id rieng, luu chung bang voi KhachHang
public class DiaChi
{
    public string Duong { get; set; } = "";
    public string ThanhPho { get; set; } = "";
}

public class KhachHang
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public DiaChi? DiaChi { get; set; }
    public List<DonHang> DonHangs { get; set; } = [];
}

public class DonHang
{
    public int Id { get; set; }
    public int KhachHangId { get; set; }
    public KhachHang KhachHang { get; set; } = null!;
    public DateOnly Ngay { get; set; }
    public List<ChiTietDon> ChiTiets { get; set; } = [];
}

public class ChiTietDon
{
    public int DonHangId { get; set; }
    public DonHang DonHang { get; set; } = null!;
    public int SanPhamId { get; set; }
    public SanPham SanPham { get; set; } = null!;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
}
