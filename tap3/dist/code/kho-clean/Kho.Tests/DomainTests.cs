using Kho.Domain.Common;
using Kho.Domain.SanPhams;
using Xunit;

namespace Kho.Tests;

// Kiem thu DOMAIN: nhanh nhat (khong DI, khong CSDL, khong HTTP), bao ve quy tac cot loi
public class DomainTests
{
    private static readonly DateTimeOffset Gio = new(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);

    private static SanPham Tao(int ton = 10, int canhBao = 5)
        => SanPham.Tao("lt001", "Laptop", "Thiet bi", 18_000_000, ton, Gio, canhBao).GiaTri;

    [Theory]
    [InlineData("LT001", true)]
    [InlineData("lt001", true)]       // tu chuan hoa thanh chu hoa
    [InlineData("L001", false)]
    [InlineData("LT0011", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void MaSanPham_ChiChapNhanDungDinhDang(string? ma, bool hopLe)
        => Assert.Equal(hopLe, MaSanPham.Tao(ma).ThanhCong);

    [Fact]
    public void MaSanPham_SoSanhTheoGiaTri()
        => Assert.Equal(MaSanPham.Tao("LT001").GiaTri, MaSanPham.Tao("lt001").GiaTri);

    [Fact]
    public void Tien_KhongDuocAm()
    {
        var kq = Tien.Tao(-1);
        Assert.False(kq.ThanhCong);
        Assert.Equal(LoaiLoi.DuLieuKhongHopLe, kq.Loi!.Loai);
    }

    [Fact]
    public void Tao_HopLe_PhatSuKienDaTao()
    {
        var sp = Tao(ton: 7);
        var sk = Assert.Single(sp.SuKienMien);
        Assert.IsType<SanPhamDaTao>(sk);
        Assert.Equal("LT001", sp.Ma.GiaTri);
    }

    [Theory]
    [InlineData("", "Ten", "Nhom", 1, 0)]
    [InlineData("LT001", "x", "Nhom", 1, 0)]
    [InlineData("LT001", "Ten", "", 1, 0)]
    [InlineData("LT001", "Ten", "Nhom", -1, 0)]
    [InlineData("LT001", "Ten", "Nhom", 1, -5)]
    public void Tao_DuLieuSai_ThatBai(string ma, string ten, string nhom, int gia, int ton)
        => Assert.False(SanPham.Tao(ma, ten, nhom, gia, ton, Gio).ThanhCong);

    [Fact]
    public void XuatKho_DuHang_GiamTonVaPhatSuKien()
    {
        var sp = Tao(ton: 10);
        sp.XoaSuKien();

        var kq = sp.XuatKho(3, Gio);

        Assert.True(kq.ThanhCong);
        Assert.Equal(7, sp.TonKho);
        var sk = Assert.IsType<TonKhoThayDoi>(Assert.Single(sp.SuKienMien));
        Assert.Equal((10, 7), (sk.TonCu, sk.TonMoi));
    }

    [Fact]
    public void XuatKho_KhongDuHang_ThatBaiVaTonKhongDoi()
    {
        var sp = Tao(ton: 2);
        sp.XoaSuKien();

        var kq = sp.XuatKho(5, Gio);

        Assert.False(kq.ThanhCong);
        Assert.Equal("SanPham.KhongDuHang", kq.Loi!.Ma);
        Assert.Equal(2, sp.TonKho);
        Assert.Empty(sp.SuKienMien);                       // that bai thi KHONG co su kien nao
    }

    [Fact]
    public void XuatKho_ChamNguongCanhBao_PhatSapHetHangMotLan()
    {
        var sp = Tao(ton: 7, canhBao: 5);
        sp.XoaSuKien();

        sp.XuatKho(3, Gio);                                // 7 -> 4: cham nguong
        sp.XuatKho(1, Gio);                                // 4 -> 3: da duoi nguong, KHONG phat lai

        Assert.Single(sp.SuKienMien.OfType<SapHetHang>());
    }

    [Fact]
    public void NhapKho_SoLuongKhongDuong_ThatBai()
        => Assert.False(Tao().NhapKho(0, Gio).ThanhCong);

    [Fact]
    public void Result_BindDungChuoi_DungLaiKhiLoi()
    {
        var kq = MaSanPham.Tao("LT001").Bind(m => Tien.Tao(-5).Map(_ => m));
        Assert.False(kq.ThanhCong);
        Assert.Equal("Tien.Am", kq.Loi!.Ma);
    }

    [Fact]
    public void Result_TruyCapGiaTriKhiThatBai_LaBug()
        => Assert.Throws<InvalidOperationException>(() => MaSanPham.Tao("x").GiaTri);
}
