using ShopLib;
using Xunit;

namespace ShopLib.Tests;

public class GioHangTests
{
    // Mau dat ten: TenPhuongThuc_TinhHuong_KetQuaMongDoi
    // Mau viet: Arrange (chuan bi) - Act (thuc hien) - Assert (kiem tra)

    [Fact]
    public void TongTien_GioHangMoi_BangKhong()
    {
        var gio = new GioHang();

        Assert.Equal(0m, gio.TongTien);
    }

    [Fact]
    public void Them_HaiSanPhamKhacNhau_TinhDungTongTien()
    {
        var gio = new GioHang();

        gio.Them("A", "Chuot", 150_000m, 2);
        gio.Them("B", "Ban phim", 500_000m);

        Assert.Equal(800_000m, gio.TongTien);
        Assert.Equal(2, gio.Dong.Count);
    }

    [Fact]
    public void Them_CungSanPhamHaiLan_GopSoLuong()
    {
        var gio = new GioHang();

        gio.Them("A", "Chuot", 150_000m, 1);
        gio.Them("A", "Chuot", 150_000m, 3);

        var dong = Assert.Single(gio.Dong);
        Assert.Equal(4, dong.SoLuong);
    }

    // Theory: cung mot test chay voi nhieu bo du lieu
    [Theory]
    [InlineData(0, 1_000_000, 1_000_000)]
    [InlineData(10, 1_000_000, 900_000)]
    [InlineData(50, 1_000_000, 500_000)]
    [InlineData(100, 1_000_000, 0)]
    public void TinhTienSauGiamGia_PhanTramHopLe_TraVeDung(int phanTram, int gia, int mongDoi)
    {
        var gio = new GioHang();
        gio.Them("A", "Laptop", gia);   // int tu chuyen thanh decimal

        var ketQua = gio.TinhTienSauGiamGia(phanTram);

        Assert.Equal((decimal)mongDoi, ketQua);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void TinhTienSauGiamGia_PhanTramNgoaiKhoang_NemLoi(int phanTram)
    {
        var gio = new GioHang();

        Assert.Throws<ArgumentOutOfRangeException>(() => gio.TinhTienSauGiamGia(phanTram));
    }

    [Fact]
    public void Them_SoLuongAm_NemLoiVaKhongThemGi()
    {
        var gio = new GioHang();

        var loi = Assert.Throws<ArgumentOutOfRangeException>(() => gio.Them("A", "Chuot", 1000m, -1));

        Assert.Equal("soLuong", loi.ParamName);
        Assert.Empty(gio.Dong);
    }

    [Fact]
    public void Xoa_SanPhamCo_TraTrueVaBoKhoiGio()
    {
        var gio = new GioHang();
        gio.Them("A", "Chuot", 1000m);

        bool daXoa = gio.Xoa("A");

        Assert.True(daXoa);
        Assert.Empty(gio.Dong);
        Assert.False(gio.Xoa("A"));   // lan hai: khong con gi de xoa
    }
}
