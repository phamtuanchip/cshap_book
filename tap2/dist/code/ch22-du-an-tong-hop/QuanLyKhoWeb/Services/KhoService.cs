using Microsoft.EntityFrameworkCore;
using QuanLyKhoWeb.Data;

namespace QuanLyKhoWeb.Services;

// ---- Loi nghiep vu (Chuong 9) ----
public abstract class LoiNghiepVu(string message) : Exception(message);
public class KhongTimThayException(string message) : LoiNghiepVu(message);
public class XungDotException(string message) : LoiNghiepVu(message);
public class KhongDuHangException(string ma, int can, int con)
    : LoiNghiepVu($"San pham {ma} khong du hang: can {can}, con {con}");

// ---- DTO ----
public record SanPhamDto(int Id, string Ma, string Ten, string Nhom, decimal DonGia, int TonKho, bool SapHet);
public record GiaoDichDto(DateTime ThoiGian, string Ma, string Loai, int SoLuong, string? GhiChu, string NguoiThucHien);
public record NhomBaoCao(string Nhom, int SoSanPham, int TongTon, decimal GiaTri);
public record TrangKetQua<T>(IReadOnlyList<T> Muc, int Trang, int KichThuoc, int TongSo)
{
    public int TongTrang => (int)Math.Ceiling(TongSo / (double)KichThuoc);
}

// Dich vu nghiep vu KHO: dang ky Scoped (moi request mot DbContext). Cac quy tac giong Tap 1 (Chuong 43)
// nhung du lieu nam trong CSDL nen phai xu ly DONG THOI dung cach.
public class KhoService(KhoDb db, TimeProvider dongHo)
{
    public async Task<TrangKetQua<SanPhamDto>> TimAsync(string? tim, string? nhom, int trang, int kichThuoc, CancellationToken ct)
    {
        trang = Math.Max(1, trang);
        kichThuoc = Math.Clamp(kichThuoc, 1, 100);

        IQueryable<SanPham> q = db.SanPhams.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tim)) q = q.Where(s => EF.Functions.Like(s.Ten, $"%{tim}%") || EF.Functions.Like(s.Ma, $"%{tim}%"));
        if (!string.IsNullOrWhiteSpace(nhom)) q = q.Where(s => s.Nhom == nhom);

        int tong = await q.CountAsync(ct);
        var muc = await q.OrderBy(s => s.Id).Skip((trang - 1) * kichThuoc).Take(kichThuoc)
            .Select(s => new SanPhamDto(s.Id, s.Ma, s.Ten, s.Nhom, s.DonGia, s.TonKho, s.TonKho <= s.MucCanhBao))
            .ToListAsync(ct);
        return new TrangKetQua<SanPhamDto>(muc, trang, kichThuoc, tong);
    }

    public async Task<SanPhamDto?> LayAsync(int id, CancellationToken ct)
        => await db.SanPhams.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new SanPhamDto(s.Id, s.Ma, s.Ten, s.Nhom, s.DonGia, s.TonKho, s.TonKho <= s.MucCanhBao))
            .FirstOrDefaultAsync(ct);

    public async Task<SanPhamDto> ThemAsync(string ma, string ten, string nhom, decimal donGia, int tonDau, string nguoi, CancellationToken ct)
    {
        if (await db.SanPhams.AnyAsync(s => s.Ma == ma, ct)) throw new XungDotException($"Ma san pham {ma} da ton tai");

        var sp = new SanPham { Ma = ma, Ten = ten.Trim(), Nhom = nhom.Trim(), DonGia = donGia, TonKho = tonDau };
        db.SanPhams.Add(sp);
        if (tonDau > 0) db.GiaoDichs.Add(TaoGiaoDich(sp, LoaiGiaoDich.Nhap, tonDau, "Ton dau ky", nguoi));

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new XungDotException($"Ma san pham {ma} da ton tai"); }   // hai request cung them mot ma
        return new SanPhamDto(sp.Id, sp.Ma, sp.Ten, sp.Nhom, sp.DonGia, sp.TonKho, sp.TonKho <= sp.MucCanhBao);
    }

    // Nhap kho: MOT cau UPDATE nguyen tu "TonKho = TonKho + n" (khong doc-roi-ghi) + ghi giao dich, cung MOT transaction
    public async Task<SanPhamDto> NhapAsync(int id, int soLuong, string? ghiChu, string nguoi, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        int n = await db.SanPhams.Where(s => s.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TonKho, x => x.TonKho + soLuong), ct);
        if (n == 0) throw new KhongTimThayException($"Khong tim thay san pham {id}");

        db.GiaoDichs.Add(new GiaoDich { SanPhamId = id, Loai = LoaiGiaoDich.Nhap, SoLuong = soLuong, GhiChu = ghiChu, NguoiThucHien = nguoi, ThoiGian = dongHo.GetLocalNow().DateTime });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await LayAsync(id, ct))!;
    }

    // Xuat kho: dieu kien "TonKho >= n" nam TRONG cau UPDATE => hai nguoi xuat dong thoi KHONG BAO GIO lam ton am.
    // Neu khong cap nhat duoc dong nao thi hoac khong ton tai, hoac khong du hang.
    public async Task<SanPhamDto> XuatAsync(int id, int soLuong, string? ghiChu, string nguoi, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        int n = await db.SanPhams.Where(s => s.Id == id && s.TonKho >= soLuong)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TonKho, x => x.TonKho - soLuong), ct);

        if (n == 0)
        {
            var sp = await db.SanPhams.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct)
                ?? throw new KhongTimThayException($"Khong tim thay san pham {id}");
            throw new KhongDuHangException(sp.Ma, soLuong, sp.TonKho);
        }

        db.GiaoDichs.Add(new GiaoDich { SanPhamId = id, Loai = LoaiGiaoDich.Xuat, SoLuong = soLuong, GhiChu = ghiChu, NguoiThucHien = nguoi, ThoiGian = dongHo.GetLocalNow().DateTime });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await LayAsync(id, ct))!;
    }

    public async Task<List<SanPhamDto>> SapHetAsync(CancellationToken ct)
        => await db.SanPhams.AsNoTracking().Where(s => s.TonKho <= s.MucCanhBao).OrderBy(s => s.TonKho)
            .Select(s => new SanPhamDto(s.Id, s.Ma, s.Ten, s.Nhom, s.DonGia, s.TonKho, true)).ToListAsync(ct);

    public async Task<List<NhomBaoCao>> BaoCaoTheoNhomAsync(CancellationToken ct)
        => await db.SanPhams.AsNoTracking().GroupBy(s => s.Nhom)
            // SQLite khong SUM duoc bieu thuc decimal (Chuong 12): tinh bang double trong SQL roi doi ve decimal.
            // Voi SQL Server/PostgreSQL co the viet thang g.Sum(s => s.TonKho * s.DonGia).
            .Select(g => new { g.Key, SoSp = g.Count(), TongTon = g.Sum(s => s.TonKho), GiaTri = g.Sum(s => (double)s.TonKho * (double)s.DonGia) })
            .OrderByDescending(x => x.GiaTri)
            .Select(x => new NhomBaoCao(x.Key, x.SoSp, x.TongTon, (decimal)x.GiaTri))
            .ToListAsync(ct);

    public async Task<List<GiaoDichDto>> LichSuAsync(string? ma, int toiDa, CancellationToken ct)
    {
        var q = db.GiaoDichs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(ma)) q = q.Where(g => g.SanPham.Ma == ma);
        return await q.OrderByDescending(g => g.Id).Take(Math.Clamp(toiDa, 1, 200))
            .Select(g => new GiaoDichDto(g.ThoiGian, g.SanPham.Ma, g.Loai.ToString(), g.SoLuong, g.GhiChu, g.NguoiThucHien))
            .ToListAsync(ct);
    }

    private GiaoDich TaoGiaoDich(SanPham sp, LoaiGiaoDich loai, int soLuong, string? ghiChu, string nguoi)
        => new() { SanPham = sp, Loai = loai, SoLuong = soLuong, GhiChu = ghiChu, NguoiThucHien = nguoi, ThoiGian = dongHo.GetLocalNow().DateTime };
}
