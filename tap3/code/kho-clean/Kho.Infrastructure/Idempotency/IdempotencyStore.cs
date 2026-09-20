using Kho.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kho.Infrastructure.Idempotency;

public enum TrangThaiChiem { ChiemDuoc, DaXongTraLai, DangXuLy, KhacNoiDung }

public sealed record KetQuaChiem(TrangThaiChiem TrangThai, int MaTrangThai = 0, string? NoiDung = null, string? LoaiNoiDung = null);

// Kho luu "khoa idempotency -> ket qua da tra". Buoc CHIEM khoa la mot lenh INSERT duy nhat -> CSDL quyet dinh ai thang khi 2 request den cung luc.
public class IdempotencyStore(KhoDbContext db, TimeProvider time)
{
    public async Task<KetQuaChiem> ChiemAsync(string khoa, string dauVan, CancellationToken ct)
    {
        int them = await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT OR IGNORE INTO idempotency (Khoa, DauVan, MaTrangThai, TaoLucMs) VALUES ({khoa}, {dauVan}, 0, {time.GetUtcNow().ToUnixTimeMilliseconds()})", ct);
        if (them == 1) return new(TrangThaiChiem.ChiemDuoc);              // minh la nguoi dau tien -> duoc phep xu ly

        var cu = await db.Idempotency.AsNoTracking().Where(x => x.Khoa == khoa)     
            .Select(x => new { x.DauVan, x.MaTrangThai, x.NoiDung, x.LoaiNoiDung }).FirstAsync(ct);
        if (cu.DauVan != dauVan) return new(TrangThaiChiem.KhacNoiDung);   // cung khoa nhung yeu cau KHAC: loi lap trinh phia client
        if (cu.MaTrangThai == 0) return new(TrangThaiChiem.DangXuLy);      // ban kia chua xong
        return new(TrangThaiChiem.DaXongTraLai, cu.MaTrangThai, cu.NoiDung, cu.LoaiNoiDung);
    }

    public Task LuuKetQuaAsync(string khoa, int ma, string? noiDung, string? loai, CancellationToken ct)
        => db.Idempotency.Where(x => x.Khoa == khoa)
             .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaTrangThai, ma).SetProperty(x => x.NoiDung, noiDung).SetProperty(x => x.LoaiNoiDung, loai), ct);

    // Loi 5xx la "chua xu ly xong" -> nha khoa ra de client thu lai duoc
    public Task NhaAsync(string khoa, CancellationToken ct) => db.Idempotency.Where(x => x.Khoa == khoa).ExecuteDeleteAsync(ct);
}
