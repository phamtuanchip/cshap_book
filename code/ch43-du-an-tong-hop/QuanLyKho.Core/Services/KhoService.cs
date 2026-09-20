using QuanLyKho.Core.Abstractions;
using QuanLyKho.Core.Models;

namespace QuanLyKho.Core.Services;

public class KhoService(IKhoRepository repo, TimeProvider dongHo)
{
    private DuLieuKho _du = new();

    public IReadOnlyList<SanPham> TatCa => _du.SanPham;
    public decimal TongGiaTri => _du.SanPham.Sum(s => s.GiaTriTon);

    public async Task KhoiTaoAsync(CancellationToken ct = default) => _du = await repo.TaiAsync(ct);

    public async Task ThemSanPhamAsync(SanPham sp, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sp.Ma)) throw new KhoException("Ma san pham khong duoc rong");
        if (sp.DonGia < 0) throw new KhoException("Don gia khong duoc am");
        if (sp.TonKho < 0) throw new KhoException("Ton kho khong duoc am");
        if (Tim(sp.Ma) is not null) throw new KhoException($"Ma san pham {sp.Ma} da ton tai");

        _du.SanPham.Add(sp);
        if (sp.TonKho > 0) GhiGiaoDich(sp.Ma, LoaiGiaoDich.Nhap, sp.TonKho, "Ton dau ky");
        await repo.LuuAsync(_du, ct);
    }

    public async Task NhapKhoAsync(string ma, int soLuong, string? ghiChu = null, CancellationToken ct = default)
    {
        if (soLuong <= 0) throw new KhoException("So luong nhap phai lon hon 0");
        var sp = LayHoacNem(ma);

        CapNhat(sp with { TonKho = sp.TonKho + soLuong });
        GhiGiaoDich(sp.Ma, LoaiGiaoDich.Nhap, soLuong, ghiChu);
        await repo.LuuAsync(_du, ct);
    }

    public async Task XuatKhoAsync(string ma, int soLuong, string? ghiChu = null, CancellationToken ct = default)
    {
        if (soLuong <= 0) throw new KhoException("So luong xuat phai lon hon 0");
        var sp = LayHoacNem(ma);
        if (sp.TonKho < soLuong) throw new KhongDuHangException(sp.Ma, soLuong, sp.TonKho);

        CapNhat(sp with { TonKho = sp.TonKho - soLuong });
        GhiGiaoDich(sp.Ma, LoaiGiaoDich.Xuat, soLuong, ghiChu);
        await repo.LuuAsync(_du, ct);
    }

    public SanPham? Tim(string ma)
        => _du.SanPham.FirstOrDefault(s => string.Equals(s.Ma, ma, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<SanPham> TimKiem(string tuKhoa)
        => _du.SanPham.Where(s => s.Ten.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)
                               || s.Ma.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<SanPham> SapHet()
        => _du.SanPham.Where(s => s.TonKho <= s.MucCanhBao).OrderBy(s => s.TonKho);

    public IEnumerable<NhomBaoCao> BaoCaoTheoNhom()
        => _du.SanPham
            .GroupBy(s => s.Nhom)
            .Select(g => new NhomBaoCao(g.Key, g.Count(), g.Sum(s => s.TonKho), g.Sum(s => s.GiaTriTon)))
            .OrderByDescending(x => x.GiaTri);

    public IEnumerable<GiaoDich> LichSu(string? ma = null)
        => _du.GiaoDich
            .Where(g => ma is null || string.Equals(g.MaSanPham, ma, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(g => g.ThoiGian);

    private SanPham LayHoacNem(string ma) => Tim(ma) ?? throw new KhongTimThayException(ma);

    private void CapNhat(SanPham moi)
    {
        int vt = _du.SanPham.FindIndex(s => s.Ma == moi.Ma);
        _du.SanPham[vt] = moi;
    }

    private void GhiGiaoDich(string ma, LoaiGiaoDich loai, int soLuong, string? ghiChu)
        => _du.GiaoDich.Add(new GiaoDich(dongHo.GetUtcNow().LocalDateTime, ma, loai, soLuong, ghiChu));
}

public record NhomBaoCao(string Nhom, int SoSanPham, int TongTon, decimal GiaTri);
