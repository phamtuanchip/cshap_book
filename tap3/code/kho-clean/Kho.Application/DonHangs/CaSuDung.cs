using FluentValidation;
using Kho.Application.Abstractions;
using Kho.Application.Messaging;
using Kho.Domain.Common;
using Kho.Domain.DonHangs;

namespace Kho.Application.DonHangs;

public sealed record DongDatHang(string MaSanPham, int SoLuong);
public sealed record DongDonDto(string MaSanPham, int SoLuong, decimal DonGia, decimal ThanhTien);
public sealed record DonHangDto(string Ma, string KhachHang, string TrangThai, decimal TongTien, DateTimeOffset DatLuc, IReadOnlyList<DongDonDto> Dong);

// =================== LENH: Dat hang (nhieu san pham, MOT giao dich) ===================
public sealed record DatHangCommand(string KhachHang, IReadOnlyList<DongDatHang> Dong) : ICommand<Result<string>>;

public sealed class DatHangValidator : AbstractValidator<DatHangCommand>
{
    public DatHangValidator()
    {
        RuleFor(x => x.KhachHang).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dong).NotNull().Must(d => d is { Count: > 0 and <= DonHang.SoDongToiDa }).WithMessage($"Don hang can 1..{DonHang.SoDongToiDa} dong");
        RuleForEach(x => x.Dong).ChildRules(d =>
        {
            d.RuleFor(x => x.MaSanPham).NotEmpty();
            d.RuleFor(x => x.SoLuong).InclusiveBetween(1, 1000);
        });
    }
}

public sealed class DatHangHandler(ISanPhamRepository sanPhams, IDonHangRepository donHangs, TimeProvider dongHo)
    : IRequestHandler<DatHangCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DatHangCommand cmd, CancellationToken ct)
    {
        var bayGio = dongHo.GetUtcNow();
        var dong = new List<DongDon>();

        // Tru kho tung dong. Dong nao that bai -> tra loi NGAY; UnitOfWorkBehavior KHONG luu gi ca:
        // cac dong da tru trong bo nho bi bo (tat ca hoac khong co gi - tinh nguyen tu)
        foreach (var d in cmd.Dong)
        {
            var sp = await sanPhams.LayTheoMaAsync(d.MaSanPham, ct);
            if (sp is null) return Result<string>.Fail(Loi.KhongTimThay("SanPham.KhongTimThay", $"Khong tim thay san pham {d.MaSanPham}"));

            var xuat = sp.XuatKho(d.SoLuong, bayGio);
            if (!xuat.ThanhCong) return Result<string>.Fail(xuat.Loi!);

            dong.Add(new DongDon(sp.Ma.GiaTri, d.SoLuong, sp.DonGia));            // ban chup gia luc dat
        }

        string ma = "DH-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        var don = DonHang.Tao(ma, cmd.KhachHang, dong, bayGio);
        if (!don.ThanhCong) return Result<string>.Fail(don.Loi!);

        donHangs.Them(don.GiaTri);
        return ma;
    }
}

// =================== LENH: Huy don (tra ton kho) ===================
public sealed record HuyDonHangCommand(string Ma, string? LyDo) : ICommand<Result>;

public sealed class HuyDonHangValidator : AbstractValidator<HuyDonHangCommand>
{
    public HuyDonHangValidator()
    {
        RuleFor(x => x.Ma).NotEmpty();
        RuleFor(x => x.LyDo).MaximumLength(300);
    }
}

public sealed class HuyDonHangHandler(ISanPhamRepository sanPhams, IDonHangRepository donHangs, TimeProvider dongHo)
    : IRequestHandler<HuyDonHangCommand, Result>
{
    public async Task<Result> Handle(HuyDonHangCommand cmd, CancellationToken ct)
    {
        var don = await donHangs.LayTheoMaAsync(cmd.Ma, ct);
        if (don is null) return Result.Fail(Loi.KhongTimThay("DonHang.KhongTimThay", $"Khong tim thay don {cmd.Ma}"));

        var bayGio = dongHo.GetUtcNow();
        var huy = don.Huy(cmd.LyDo, bayGio);
        if (!huy.ThanhCong) return huy;

        // Bu tru: tra lai ton cho tung dong (cung giao dich voi viec doi trang thai don)
        foreach (var d in don.Dong)
        {
            var sp = await sanPhams.LayTheoMaAsync(d.MaSanPham, ct);
            var tra = sp!.NhapKho(d.SoLuong, bayGio);
            if (!tra.ThanhCong) return tra;
        }
        return Result.Ok();
    }
}

// =================== TRUY VAN ===================
public sealed record LayDonHangQuery(string Ma) : IQuery<Result<DonHangDto>>;

public sealed class LayDonHangHandler(IDonHangRepository donHangs) : IRequestHandler<LayDonHangQuery, Result<DonHangDto>>
{
    public async Task<Result<DonHangDto>> Handle(LayDonHangQuery q, CancellationToken ct)
    {
        var d = await donHangs.LayTheoMaAsync(q.Ma, ct);
        if (d is null) return Result<DonHangDto>.Fail(Loi.KhongTimThay("DonHang.KhongTimThay", $"Khong tim thay don {q.Ma}"));

        return new DonHangDto(d.Ma, d.KhachHang, d.TrangThai.ToString(), d.TongTien.SoTien, d.DatLuc,
            d.Dong.Select(x => new DongDonDto(x.MaSanPham, x.SoLuong, x.DonGia.SoTien, x.ThanhTien.SoTien)).ToList());
    }
}
