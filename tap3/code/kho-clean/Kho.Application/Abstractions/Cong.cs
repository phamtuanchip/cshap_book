using Kho.Domain.SanPhams;

namespace Kho.Application.Abstractions;

// ======== CAC "CONG" (ports): Application dinh nghia, Infrastructure cai dat ========

public interface ISanPhamRepository
{
    Task<SanPham?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task<bool> TonTaiMaAsync(string ma, CancellationToken ct);
    void Them(SanPham sanPham);
}

// Don vi cong viec: gom moi thay doi thanh MOT giao dich
public interface IUnitOfWork
{
    Task<int> LuuAsync(CancellationToken ct);
}

// Phia DOC (query side): tra thang DTO, khong nap aggregate, khong di qua domain.
// Dinh nghia bang phuong thuc (khong lo IQueryable) de Application khong phu thuoc EF Core.
public interface IKhoDocDuLieu
{
    Task<TrangKetQua<SanPhamDto>> TimAsync(string? tuKhoa, string? nhom, int trang, int kichThuoc, CancellationToken ct);
    Task<SanPhamDto?> LayTheoMaAsync(string ma, CancellationToken ct);
}

public record SanPhamDto(int Id, string Ma, string Ten, string Nhom, decimal DonGia, int TonKho, bool SapHet);

public record TrangKetQua<T>(IReadOnlyList<T> Muc, int Trang, int KichThuoc, int TongSo)
{
    public int TongTrang => (int)Math.Ceiling(TongSo / (double)KichThuoc);
}

// Cong phat hanh su kien tich hop ra ben ngoai (message bus, webhook, email...). Ha tang quyet dinh cach gui.
public interface IPhatHanhSuKien
{
    Task PhatHanhAsync(string loai, string noiDungJson, CancellationToken ct);
}
