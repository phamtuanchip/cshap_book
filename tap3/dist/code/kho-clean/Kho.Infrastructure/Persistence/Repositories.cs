using Kho.Application.Abstractions;
using Kho.Domain.DonHangs;
using Kho.Domain.SanPhams;
using Microsoft.EntityFrameworkCore;

namespace Kho.Infrastructure.Persistence;

// Repository: chi cung cap thao tac tren AGGREGATE ROOT (khong lo IQueryable ra ngoai)
public class SanPhamRepository(KhoDbContext db) : ISanPhamRepository
{
    public Task<SanPham?> LayTheoMaAsync(string ma, CancellationToken ct)
    {
        var maChuan = ma.Trim().ToUpperInvariant();
        return db.SanPhams.FirstOrDefaultAsync(s => s.Ma == MaSanPham.TuCsdl(maChuan), ct);
    }

    public Task<bool> TonTaiMaAsync(string ma, CancellationToken ct)
    {
        var maChuan = ma.Trim().ToUpperInvariant();
        return db.SanPhams.AnyAsync(s => s.Ma == MaSanPham.TuCsdl(maChuan), ct);
    }

    public void Them(SanPham sanPham) => db.SanPhams.Add(sanPham);
}

public class DonHangRepository(KhoDbContext db) : IDonHangRepository
{
    public Task<DonHang?> LayTheoMaAsync(string ma, CancellationToken ct)
    {
        var maChuan = ma.Trim().ToUpperInvariant();
        return db.DonHangs.FirstOrDefaultAsync(d => d.Ma == maChuan, ct);          // aggregate + cac dong (owned) nap cung
    }

    public void Them(DonHang donHang) => db.DonHangs.Add(donHang);
}

// Phia DOC: chieu thang tu bang ra DTO bang projection, khong nap aggregate, khong theo doi
public class KhoDocDuLieu(KhoDbContext db) : IKhoDocDuLieu
{
    // Loc/sap xep tren THUC THE DOC truoc, chieu sang DTO o CUOI (EF dich de dang hon)
    public async Task<TrangKetQua<SanPhamDto>> TimAsync(string? tuKhoa, string? nhom, int trang, int kichThuoc, CancellationToken ct)
    {
        IQueryable<SanPhamDoc> q = db.SanPhamDocs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa)) q = q.Where(s => EF.Functions.Like(s.Ten, $"%{tuKhoa}%") || EF.Functions.Like(s.Ma, $"%{tuKhoa}%"));
        if (!string.IsNullOrWhiteSpace(nhom)) q = q.Where(s => s.Nhom == nhom);

        int tong = await q.CountAsync(ct);
        var dong = await q.OrderBy(s => s.Id).Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToListAsync(ct);
        return new TrangKetQua<SanPhamDto>(dong.Select(ChieuSangDto).ToList(), trang, kichThuoc, tong);
    }

    public async Task<SanPhamDto?> LayTheoMaAsync(string ma, CancellationToken ct)
    {
        var s = await db.SanPhamDocs.AsNoTracking().Where(x => x.Ma == ma).FirstOrDefaultAsync(ct);
        return s is null ? null : ChieuSangDto(s);
    }

    private static SanPhamDto ChieuSangDto(SanPhamDoc s) => new(s.Id, s.Ma, s.Ten, s.Nhom, s.DonGia, s.TonKho, s.TonKho <= s.MucCanhBao);
}
