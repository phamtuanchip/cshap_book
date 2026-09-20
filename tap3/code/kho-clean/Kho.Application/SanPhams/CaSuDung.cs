using FluentValidation;
using Kho.Application.Abstractions;
using Kho.Application.Messaging;
using Kho.Domain.Common;
using Kho.Domain.SanPhams;

namespace Kho.Application.SanPhams;

// =================== LENH: Them san pham ===================
public sealed record ThemSanPhamCommand(string Ma, string Ten, string Nhom, decimal DonGia, int TonDau) : ICommand<Result<string>>;

public sealed class ThemSanPhamValidator : AbstractValidator<ThemSanPhamCommand>
{
    public ThemSanPhamValidator()
    {
        RuleFor(x => x.Ma).NotEmpty().Matches("^[A-Za-z]{2}[0-9]{3}$").WithMessage("Ma gom 2 chu cai + 3 chu so");
        RuleFor(x => x.Ten).NotEmpty().Length(2, 200);
        RuleFor(x => x.Nhom).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TonDau).InclusiveBetween(0, 1_000_000);
    }
}

public sealed class ThemSanPhamHandler(ISanPhamRepository repo, TimeProvider dongHo) : IRequestHandler<ThemSanPhamCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ThemSanPhamCommand cmd, CancellationToken ct)
    {
        var sp = SanPham.Tao(cmd.Ma, cmd.Ten, cmd.Nhom, cmd.DonGia, cmd.TonDau, dongHo.GetUtcNow());
        if (!sp.ThanhCong) return Result<string>.Fail(sp.Loi!);

        if (await repo.TonTaiMaAsync(sp.GiaTri.Ma.GiaTri, ct))
            return Result<string>.Fail(Loi.XungDot("SanPham.Ma.TonTai", $"Ma san pham {sp.GiaTri.Ma} da ton tai"));

        repo.Them(sp.GiaTri);
        return sp.GiaTri.Ma.GiaTri;                      // UnitOfWorkBehavior se luu; handler chi lo nghiep vu
    }
}

// =================== LENH: Nhap / Xuat kho ===================
public sealed record NhapKhoCommand(string Ma, int SoLuong) : ICommand<Result>;
public sealed record XuatKhoCommand(string Ma, int SoLuong) : ICommand<Result>;

public sealed class NhapKhoValidator : AbstractValidator<NhapKhoCommand>
{
    public NhapKhoValidator() => RuleFor(x => x.SoLuong).InclusiveBetween(1, 100_000);
}

public sealed class XuatKhoValidator : AbstractValidator<XuatKhoCommand>
{
    public XuatKhoValidator() => RuleFor(x => x.SoLuong).InclusiveBetween(1, 100_000);
}

public sealed class NhapKhoHandler(ISanPhamRepository repo, TimeProvider dongHo) : IRequestHandler<NhapKhoCommand, Result>
{
    public async Task<Result> Handle(NhapKhoCommand cmd, CancellationToken ct)
    {
        var sp = await repo.LayTheoMaAsync(cmd.Ma, ct);
        return sp is null ? Result.Fail(Loi.KhongTimThay("SanPham.KhongTimThay", $"Khong tim thay san pham {cmd.Ma}")) : sp.NhapKho(cmd.SoLuong, dongHo.GetUtcNow());
    }
}

public sealed class XuatKhoHandler(ISanPhamRepository repo, TimeProvider dongHo) : IRequestHandler<XuatKhoCommand, Result>
{
    public async Task<Result> Handle(XuatKhoCommand cmd, CancellationToken ct)
    {
        var sp = await repo.LayTheoMaAsync(cmd.Ma, ct);
        return sp is null ? Result.Fail(Loi.KhongTimThay("SanPham.KhongTimThay", $"Khong tim thay san pham {cmd.Ma}")) : sp.XuatKho(cmd.SoLuong, dongHo.GetUtcNow());
    }
}

// =================== TRUY VAN ===================
public sealed record TimSanPhamQuery(string? TuKhoa, string? Nhom, int Trang = 1, int KichThuoc = 10) : IQuery<Result<TrangKetQua<SanPhamDto>>>;

public sealed class TimSanPhamHandler(IKhoDocDuLieu doc) : IRequestHandler<TimSanPhamQuery, Result<TrangKetQua<SanPhamDto>>>
{
    public async Task<Result<TrangKetQua<SanPhamDto>>> Handle(TimSanPhamQuery q, CancellationToken ct)
        => await doc.TimAsync(q.TuKhoa, q.Nhom, Math.Max(1, q.Trang), Math.Clamp(q.KichThuoc, 1, 100), ct);
}

public sealed record LaySanPhamQuery(string Ma) : IQuery<Result<SanPhamDto>>;

public sealed class LaySanPhamHandler(IKhoDocDuLieu doc) : IRequestHandler<LaySanPhamQuery, Result<SanPhamDto>>
{
    public async Task<Result<SanPhamDto>> Handle(LaySanPhamQuery q, CancellationToken ct)
    {
        var dto = await doc.LayTheoMaAsync(q.Ma.Trim().ToUpperInvariant(), ct);
        return dto is null ? Result<SanPhamDto>.Fail(Loi.KhongTimThay("SanPham.KhongTimThay", $"Khong tim thay san pham {q.Ma}")) : dto;
    }
}
