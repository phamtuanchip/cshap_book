using CuaHangApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CuaHangApi.Services;

// Nghiep vu dat hang: mot don gom nhieu dong, phai TRU KHO va TAO DON cung thanh cong hoac cung that bai.
// SaveChanges gom moi thay doi vao MOT giao dich CSDL -> khong can tu mo transaction.
public class DonHangService(CuaHangDbContext db, TimeProvider dongHo)
{
    public async Task<DonHangDto> TaoDonAsync(TaoDonRequest req, CancellationToken ct)
    {
        if (req.Dong is not { Count: > 0 }) throw new LoiNghiepVuDuLieu("Don hang phai co it nhat mot dong");
        if (req.Dong.Any(d => d.SoLuong <= 0)) throw new LoiNghiepVuDuLieu("So luong moi dong phai lon hon 0");

        var khach = await db.KhachHangs.FindAsync([req.KhachHangId], ct)
            ?? throw new KhongTimThayException($"Khong tim thay khach hang {req.KhachHangId}");

        // Gop cac dong trung san pham; lay tat ca san pham can dung trong MOT truy van
        var can = req.Dong.GroupBy(d => d.SanPhamId).ToDictionary(g => g.Key, g => g.Sum(d => d.SoLuong));
        var sanPhams = await db.SanPhams.Where(s => can.Keys.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);

        var don = new DonHang { KhachHang = khach, Ngay = DateOnly.FromDateTime(dongHo.GetLocalNow().DateTime) };
        foreach (var (id, soLuong) in can)
        {
            if (!sanPhams.TryGetValue(id, out var sp))
                throw new KhongTimThayException($"Khong tim thay san pham {id}");
            if (sp.Ton < soLuong)
                throw new KhongDuHangException(sp.Ma, soLuong, sp.Ton);

            sp.Ton -= soLuong;                                                      // tru kho (bi theo doi -> UPDATE)
            don.ChiTiets.Add(new ChiTietDon { SanPham = sp, SoLuong = soLuong, DonGia = sp.Gia });   // chot gia luc dat
        }
        db.DonHangs.Add(don);

        try
        {
            await db.SaveChangesAsync(ct);                                          // 1 giao dich: tru kho + tao don
        }
        catch (DbUpdateConcurrencyException)
        {
            // Co nguoi khac vua doi ton kho giua luc ta doc va ghi -> khong ghi de, bao client thu lai
            throw new XungDotException("Ton kho vua thay doi boi nguoi khac, vui long dat lai");
        }

        return ChuyenSangDto(don);
    }

    public async Task<DonHangDto?> LayAsync(int id, CancellationToken ct)
    {
        // IgnoreQueryFilters: don cu van phai hien du dong ke ca khi san pham da bi xoa mem
        var don = await db.DonHangs.AsNoTracking().IgnoreQueryFilters()
            .Include(d => d.KhachHang)
            .Include(d => d.ChiTiets).ThenInclude(c => c.SanPham)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        return don is null ? null : ChuyenSangDto(don);
    }

    private static DonHangDto ChuyenSangDto(DonHang d) => new(
        d.Id, d.KhachHang.Ten, d.Ngay,
        d.ChiTiets.Sum(c => c.SoLuong * c.DonGia),
        [.. d.ChiTiets.Select(c => new DongDonDto(c.SanPham.Ma, c.SanPham.Ten, c.SoLuong, c.DonGia, c.SoLuong * c.DonGia))]);
}

public class LoiNghiepVuDuLieu(string message) : LoiNghiepVu(message);
