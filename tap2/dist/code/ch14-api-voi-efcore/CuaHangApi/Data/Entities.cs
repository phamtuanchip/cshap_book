namespace CuaHangApi.Data;

public class Nhom
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public List<SanPham> SanPhams { get; set; } = [];
}

public class SanPham
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public decimal Gia { get; set; }
    public int Ton { get; set; }
    public bool DaXoa { get; set; }
    public int NhomId { get; set; }
    public Nhom Nhom { get; set; } = null!;
}

public class KhachHang
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public string Email { get; set; } = "";
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
    public int SanPhamId { get; set; }
    public SanPham SanPham { get; set; } = null!;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
}
