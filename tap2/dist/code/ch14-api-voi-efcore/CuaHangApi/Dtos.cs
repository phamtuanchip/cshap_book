using System.ComponentModel.DataAnnotations;

namespace CuaHangApi;

public record SanPhamDto(int Id, string Ma, string Ten, decimal Gia, int Ton, string Nhom);

public record TaoSanPhamRequest
{
    [Required, RegularExpression("^[A-Z]{2}[0-9]{3}$")] public string? Ma { get; init; }
    [Required, StringLength(200, MinimumLength = 2)] public string? Ten { get; init; }
    [Range(0, 1_000_000_000)] public decimal Gia { get; init; }
    [Range(0, 1_000_000)] public int Ton { get; init; }
    [Range(1, int.MaxValue)] public int NhomId { get; init; }
}

public record CapNhatSanPhamRequest
{
    [Required, StringLength(200, MinimumLength = 2)] public string? Ten { get; init; }
    [Range(0, 1_000_000_000)] public decimal Gia { get; init; }
}

public record NhapKhoRequest([property: Range(1, 100_000)] int SoLuong);

// Ket qua phan trang chuan
public record TrangKetQua<T>(IReadOnlyList<T> Muc, int Trang, int KichThuoc, int TongSo)
{
    public int TongTrang => (int)Math.Ceiling(TongSo / (double)KichThuoc);
    public bool CoTrangSau => Trang < TongTrang;
}

public record DongDonRequest(int SanPhamId, int SoLuong);
public record TaoDonRequest(int KhachHangId, List<DongDonRequest>? Dong);

public record DongDonDto(string Ma, string Ten, int SoLuong, decimal DonGia, decimal ThanhTien);
public record DonHangDto(int Id, string KhachHang, DateOnly Ngay, decimal TongTien, IReadOnlyList<DongDonDto> Dong);

public record NhomDto(int Id, string Ten, int SoSanPham, int TongTon);

// Exception nghiep vu (Chuong 9)
public abstract class LoiNghiepVu(string message) : Exception(message);
public class KhongTimThayException(string message) : LoiNghiepVu(message);
public class XungDotException(string message) : LoiNghiepVu(message);
public class KhongDuHangException(string ma, int can, int con)
    : LoiNghiepVu($"San pham {ma} khong du hang: can {can}, con {con}");
