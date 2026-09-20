namespace ShopLib;

public record DongHang(string MaSp, string Ten, decimal DonGia, int SoLuong)
{
    public decimal ThanhTien => DonGia * SoLuong;
}

public class GioHang
{
    private readonly List<DongHang> _dong = [];

    public IReadOnlyList<DongHang> Dong => _dong;
    public decimal TongTien => _dong.Sum(d => d.ThanhTien);

    public void Them(string maSp, string ten, decimal donGia, int soLuong = 1)
    {
        if (string.IsNullOrWhiteSpace(maSp)) throw new ArgumentException("Ma san pham khong duoc rong", nameof(maSp));
        if (donGia < 0) throw new ArgumentOutOfRangeException(nameof(donGia), "Don gia khong duoc am");
        if (soLuong <= 0) throw new ArgumentOutOfRangeException(nameof(soLuong), "So luong phai duong");

        int vt = _dong.FindIndex(d => d.MaSp == maSp);
        if (vt >= 0)
            _dong[vt] = _dong[vt] with { SoLuong = _dong[vt].SoLuong + soLuong };
        else
            _dong.Add(new DongHang(maSp, ten, donGia, soLuong));
    }

    public bool Xoa(string maSp) => _dong.RemoveAll(d => d.MaSp == maSp) > 0;

    // Ap dung giam gia theo phan tram (0-100)
    public decimal TinhTienSauGiamGia(decimal phanTramGiam)
    {
        if (phanTramGiam is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(phanTramGiam));
        return Math.Round(TongTien * (100 - phanTramGiam) / 100m, 0);
    }
}
