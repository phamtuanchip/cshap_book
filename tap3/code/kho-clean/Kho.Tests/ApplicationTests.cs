using Kho.Application;
using Kho.Application.Abstractions;
using Kho.Application.Behaviors;
using Kho.Application.Messaging;
using Kho.Application.SanPhams;
using Kho.Domain.Common;
using Kho.Domain.SanPhams;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kho.Tests;

// ---- Ban gia (fake) trong bo nho cho cac CONG: Application test khong can EF, HTTP, CSDL ----
class RepoGia : ISanPhamRepository
{
    public List<SanPham> Ds { get; } = [];
    public Task<SanPham?> LayTheoMaAsync(string ma, CancellationToken ct) => Task.FromResult(Ds.FirstOrDefault(s => s.Ma.GiaTri == ma.ToUpperInvariant()));
    public Task<bool> TonTaiMaAsync(string ma, CancellationToken ct) => Task.FromResult(Ds.Any(s => s.Ma.GiaTri == ma.ToUpperInvariant()));
    public void Them(SanPham sp) => Ds.Add(sp);
}

class UowGia : IUnitOfWork
{
    public int SoLanLuu { get; private set; }
    public Exception? Nem { get; set; }
    public Task<int> LuuAsync(CancellationToken ct) { if (Nem is not null) throw Nem; SoLanLuu++; return Task.FromResult(1); }
}

class DocGia : IKhoDocDuLieu
{
    public Task<TrangKetQua<SanPhamDto>> TimAsync(string? t, string? n, int trang, int kt, CancellationToken ct)
        => Task.FromResult(new TrangKetQua<SanPhamDto>([], trang, kt, 0));
    public Task<SanPhamDto?> LayTheoMaAsync(string ma, CancellationToken ct) => Task.FromResult<SanPhamDto?>(null);
}

// Behavior ghi vet de kiem chung THU TU thuc thi cua pipeline
class GhiVetBehavior<TRequest, TResponse>(List<string> vet, string ten) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> where TResponse : IKetQua<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        vet.Add($"{ten}:vao");
        var kq = await next();
        vet.Add($"{ten}:ra");
        return kq;
    }
}

public class ApplicationTests
{
    private readonly RepoGia _repo = new();
    private readonly UowGia _uow = new();
    private readonly ISender _sender;

    public ApplicationTests()
    {
        var s = new ServiceCollection();
        s.AddSingleton<ISanPhamRepository>(_repo).AddSingleton<IUnitOfWork>(_uow).AddSingleton<IKhoDocDuLieu>(new DocGia());
        s.AddSingleton(TimeProvider.System);
        s.AddLogging();
        s.AddApplication();
        _sender = s.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<ISender>();
    }

    [Fact]
    public async Task ThemSanPham_HopLe_ThemVaLuuDungMotLan()
    {
        var kq = await _sender.Send(new ThemSanPhamCommand("LT001", "Laptop", "Thiet bi", 1000, 5));

        Assert.True(kq.ThanhCong);
        Assert.Equal("LT001", kq.GiaTri);
        Assert.Single(_repo.Ds);
        Assert.Equal(1, _uow.SoLanLuu);                    // UnitOfWorkBehavior luu sau lenh thanh cong
    }

    [Fact]
    public async Task ThemSanPham_SaiDinhDang_ValidationBehaviorChanTruocHandler()
    {
        var kq = await _sender.Send(new ThemSanPhamCommand("sai", "", "", -1, 5));

        Assert.False(kq.ThanhCong);
        Assert.Equal(LoaiLoi.DuLieuKhongHopLe, kq.Loi!.Loai);
        Assert.Empty(_repo.Ds);                            // handler khong chay
        Assert.Equal(0, _uow.SoLanLuu);                    // khong luu
    }

    [Fact]
    public async Task ThemSanPham_TrungMa_XungDotVaKhongLuu()
    {
        await _sender.Send(new ThemSanPhamCommand("LT001", "Laptop", "Thiet bi", 1000, 5));

        var kq = await _sender.Send(new ThemSanPhamCommand("lt001", "Khac", "Thiet bi", 1, 1));

        Assert.Equal(LoaiLoi.XungDot, kq.Loi!.Loai);
        Assert.Single(_repo.Ds);
        Assert.Equal(1, _uow.SoLanLuu);                    // chi lan dau
    }

    [Fact]
    public async Task XuatKho_KhongTimThay_LoiKhongTimThay()
    {
        var kq = await _sender.Send(new XuatKhoCommand("ZZ999", 1));
        Assert.Equal(LoaiLoi.KhongTimThay, kq.Loi!.Loai);
    }

    [Fact]
    public async Task XuatKho_KhongDuHang_LoiNghiepVuVaKhongLuu()
    {
        await _sender.Send(new ThemSanPhamCommand("LT001", "Laptop", "Thiet bi", 1000, 3));
        int luuTruoc = _uow.SoLanLuu;

        var kq = await _sender.Send(new XuatKhoCommand("LT001", 10));

        Assert.Equal(LoaiLoi.NghiepVu, kq.Loi!.Loai);
        Assert.Equal(luuTruoc, _uow.SoLanLuu);
    }

    [Fact]
    public async Task XuatKho_SoLuongSai_ValidationBehavior()
    {
        var kq = await _sender.Send(new XuatKhoCommand("LT001", 0));
        Assert.Equal(LoaiLoi.DuLieuKhongHopLe, kq.Loi!.Loai);
    }

    [Fact]
    public async Task XungDotDongThoi_HaTangNemException_ThanhLoiXungDotChoNguoiDung()
    {
        await _sender.Send(new ThemSanPhamCommand("LT001", "Laptop", "Thiet bi", 1000, 10));
        _uow.Nem = new ConcurrencyException();             // gia lap: luc luu co nguoi khac vua sua

        var kq = await _sender.Send(new XuatKhoCommand("LT001", 1));

        Assert.Equal(LoaiLoi.XungDot, kq.Loi!.Loai);
        Assert.Equal("Kho.XungDotDongThoi", kq.Loi.Ma);
    }

    [Fact]
    public async Task TruyVan_KhongDiQuaUnitOfWorkBehavior()
    {
        await _sender.Send(new TimSanPhamQuery(null, null));
        Assert.Equal(0, _uow.SoLanLuu);                    // query khong bao gio luu
    }

    [Fact]
    public async Task Pipeline_ChayTheoThuTuNgoaiVaoTrongRoiTrongRaNgoai()
    {
        var vet = new List<string>();
        var sp = new ServiceCollection().AddSingleton<ISanPhamRepository>(new RepoGia()).AddSingleton(TimeProvider.System)
            .AddMediator(typeof(ThemSanPhamHandler).Assembly)                // chi handler, KHONG behavior mac dinh
            .AddScoped<IPipelineBehavior<XuatKhoCommand, Result>>(_ => new GhiVetBehavior<XuatKhoCommand, Result>(vet, "A"))
            .AddScoped<IPipelineBehavior<XuatKhoCommand, Result>>(_ => new GhiVetBehavior<XuatKhoCommand, Result>(vet, "B"))
            .BuildServiceProvider();

        await sp.CreateScope().ServiceProvider.GetRequiredService<ISender>().Send(new XuatKhoCommand("ZZ999", 1));

        Assert.Equal(["A:vao", "B:vao", "B:ra", "A:ra"], vet);               // dang ky truoc = boc ngoai cung
    }
}
