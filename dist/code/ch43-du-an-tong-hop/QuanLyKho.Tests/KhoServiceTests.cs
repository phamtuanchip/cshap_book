using QuanLyKho.Core.Abstractions;
using QuanLyKho.Core.Models;
using QuanLyKho.Core.Services;
using Xunit;

namespace QuanLyKho.Tests;

// Repository gia trong bo nho: test khong cham vao file that (nho interface IKhoRepository)
class RepoTrongBoNho : IKhoRepository
{
    public DuLieuKho DuLieu { get; private set; } = new();
    public int SoLanLuu { get; private set; }

    public Task<DuLieuKho> TaiAsync(CancellationToken ct = default) => Task.FromResult(DuLieu);

    public Task LuuAsync(DuLieuKho duLieu, CancellationToken ct = default)
    {
        DuLieu = duLieu;
        SoLanLuu++;
        return Task.CompletedTask;
    }
}

public class KhoServiceTests
{
    private readonly RepoTrongBoNho _repo = new();
    private readonly KhoService _kho;

    public KhoServiceTests() => _kho = new KhoService(_repo, TimeProvider.System);

    private static SanPham SanPhamMau(string ma = "A1", int ton = 10, string nhom = "N1", decimal gia = 1000m)
        => new(ma, "San pham " + ma, nhom, gia, ton);

    [Fact]
    public async Task ThemSanPham_HopLe_DuocLuuVaGhiGiaoDichTonDau()
    {
        await _kho.ThemSanPhamAsync(SanPhamMau("A1", ton: 10));

        Assert.Single(_kho.TatCa);
        Assert.Equal(1, _repo.SoLanLuu);
        var gd = Assert.Single(_kho.LichSu());
        Assert.Equal(LoaiGiaoDich.Nhap, gd.Loai);
        Assert.Equal(10, gd.SoLuong);
    }

    [Fact]
    public async Task ThemSanPham_TrungMa_NemKhoException()
    {
        await _kho.ThemSanPhamAsync(SanPhamMau("A1"));

        await Assert.ThrowsAsync<KhoException>(() => _kho.ThemSanPhamAsync(SanPhamMau("a1")));   // khong phan biet hoa thuong
    }

    [Fact]
    public async Task NhapKho_TangTon()
    {
        await _kho.ThemSanPhamAsync(SanPhamMau("A1", ton: 10));

        await _kho.NhapKhoAsync("A1", 5);

        Assert.Equal(15, _kho.Tim("A1")!.TonKho);
    }

    [Fact]
    public async Task XuatKho_DuHang_GiamTon()
    {
        await _kho.ThemSanPhamAsync(SanPhamMau("A1", ton: 10));

        await _kho.XuatKhoAsync("A1", 4);

        Assert.Equal(6, _kho.Tim("A1")!.TonKho);
    }

    [Fact]
    public async Task XuatKho_KhongDuHang_NemLoiVaTonKhongDoi()
    {
        await _kho.ThemSanPhamAsync(SanPhamMau("A1", ton: 3));

        var loi = await Assert.ThrowsAsync<KhongDuHangException>(() => _kho.XuatKhoAsync("A1", 10));

        Assert.Equal(10, loi.Can);
        Assert.Equal(3, loi.Con);
        Assert.Equal(3, _kho.Tim("A1")!.TonKho);
    }

    [Fact]
    public async Task NhapKho_MaKhongTonTai_NemKhongTimThay()
    {
        await Assert.ThrowsAsync<KhongTimThayException>(() => _kho.NhapKhoAsync("KHONG-CO", 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task XuatKho_SoLuongKhongDuong_NemKhoException(int soLuong)
    {
        await _kho.ThemSanPhamAsync(SanPhamMau("A1"));

        await Assert.ThrowsAsync<KhoException>(() => _kho.XuatKhoAsync("A1", soLuong));
    }

    [Fact]
    public async Task SapHet_ChiTraSanPhamDuoiMucCanhBao_SapXepTheoTon()
    {
        await _kho.ThemSanPhamAsync(new SanPham("A", "A", "N", 1m, 3, MucCanhBao: 5));
        await _kho.ThemSanPhamAsync(new SanPham("B", "B", "N", 1m, 100, MucCanhBao: 5));
        await _kho.ThemSanPhamAsync(new SanPham("C", "C", "N", 1m, 1, MucCanhBao: 5));

        var ketQua = _kho.SapHet().Select(s => s.Ma).ToList();

        Assert.Equal(["C", "A"], ketQua);
    }

    [Fact]
    public async Task BaoCaoTheoNhom_GomNhomVaTinhGiaTri()
    {
        await _kho.ThemSanPhamAsync(SanPhamMau("A1", ton: 2, nhom: "X", gia: 100m));
        await _kho.ThemSanPhamAsync(SanPhamMau("A2", ton: 3, nhom: "X", gia: 200m));
        await _kho.ThemSanPhamAsync(SanPhamMau("B1", ton: 1, nhom: "Y", gia: 50m));

        var bc = _kho.BaoCaoTheoNhom().ToList();

        Assert.Equal(2, bc.Count);
        Assert.Equal("X", bc[0].Nhom);          // gia tri lon hon dung truoc
        Assert.Equal(800m, bc[0].GiaTri);       // 2*100 + 3*200
        Assert.Equal(5, bc[0].TongTon);
        Assert.Equal(50m, bc[1].GiaTri);
    }

    [Fact]
    public async Task TimKiem_KhongPhanBietHoaThuong_TimTheoTenHoacMa()
    {
        await _kho.ThemSanPhamAsync(new SanPham("LT01", "Laptop Dell", "TB", 1m, 1));
        await _kho.ThemSanPhamAsync(new SanPham("CH01", "Chuot", "PK", 1m, 1));

        Assert.Single(_kho.TimKiem("laptop"));
        Assert.Single(_kho.TimKiem("ch01"));
        Assert.Empty(_kho.TimKiem("xyz"));
    }
}
