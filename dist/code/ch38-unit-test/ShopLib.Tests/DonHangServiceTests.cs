using NSubstitute;
using ShopLib;
using Xunit;

namespace ShopLib.Tests;

public class DonHangServiceTests
{
    // Constructor chay truoc MOI test: moi test co doi tuong moi, doc lap voi nhau
    private readonly IKhoHang _kho = Substitute.For<IKhoHang>();
    private readonly IGuiEmail _email = Substitute.For<IGuiEmail>();
    private readonly DonHangService _service;

    public DonHangServiceTests() => _service = new DonHangService(_kho, _email);

    [Fact]
    public void DatHang_DuHang_GiamTonVaGuiEmail()
    {
        var gio = new GioHang();
        gio.Them("A", "Chuot", 100_000m, 2);
        _kho.SoLuongTon("A").Returns(10);      // dan ket qua tra ve cua doi tuong gia

        var kq = _service.DatHang(gio, "khach@mail.com");

        Assert.True(kq.OK);
        Assert.Equal(200_000m, kq.TongTien);
        _kho.Received(1).GiamTon("A", 2);      // kiem tra ham DA duoc goi dung 1 lan voi tham so nay
        _email.Received(1).Gui("khach@mail.com", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void DatHang_KhongDuHang_BaoLoiVaKhongGiamTonKhongGuiEmail()
    {
        var gio = new GioHang();
        gio.Them("A", "Chuot", 100_000m, 5);
        _kho.SoLuongTon("A").Returns(3);

        var kq = _service.DatHang(gio, "khach@mail.com");

        Assert.False(kq.OK);
        Assert.Contains("Khong du hang", kq.LoiMoTa);
        _kho.DidNotReceive().GiamTon(Arg.Any<string>(), Arg.Any<int>());
        _email.DidNotReceive().Gui(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void DatHang_GioHangTrong_BaoLoi()
    {
        var kq = _service.DatHang(new GioHang(), "khach@mail.com");

        Assert.False(kq.OK);
        Assert.Equal("Gio hang trong", kq.LoiMoTa);
    }
}
