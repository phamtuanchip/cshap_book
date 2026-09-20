using KienTruc.Application;
using KienTruc.Domain;

namespace KienTruc.Infrastructure;

// Infrastructure: cai dat CHI TIET ky thuat cua cac cong. Biet Application (de cai dat interface), khong nguoc lai.
public class InMemorySanPhamRepository : ISanPhamRepository
{
    private readonly List<SanPham> _ds =
    [
        new(1, "LT001", "Laptop Dell", 18_000_000m),
        new(2, "CH001", "Chuot khong day", 150_000m),
    ];

    public Task<SanPham?> LayAsync(int id, CancellationToken ct) => Task.FromResult(_ds.FirstOrDefault(s => s.Id == id));
    public Task<IReadOnlyList<SanPham>> TatCaAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<SanPham>>(_ds);
}
