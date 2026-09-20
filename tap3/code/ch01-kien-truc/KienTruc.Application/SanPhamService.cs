using KienTruc.Domain;

namespace KienTruc.Application;

// Application: cac ca su dung (use case). Dinh nghia CONG (interface) ma ha tang phai cai dat.
// Application biet Domain, KHONG biet Infrastructure/Web.
public interface ISanPhamRepository
{
    Task<SanPham?> LayAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<SanPham>> TatCaAsync(CancellationToken ct);
}

public record SanPhamDto(int Id, string Ma, string Ten, decimal Gia);

public class SanPhamService(ISanPhamRepository repo)
{
    public async Task<SanPhamDto?> LayAsync(int id, CancellationToken ct)
        => await repo.LayAsync(id, ct) is { } s ? new SanPhamDto(s.Id, s.Ma, s.Ten, s.Gia) : null;

    public async Task<IReadOnlyList<SanPhamDto>> DanhSachAsync(CancellationToken ct)
        => (await repo.TatCaAsync(ct)).Select(s => new SanPhamDto(s.Id, s.Ma, s.Ten, s.Gia)).ToList();
}
