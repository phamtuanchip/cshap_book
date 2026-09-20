using ApiSanPham.Dtos;

namespace ApiSanPham;

// ---- Entity noi bo: co truong khach KHONG DUOC thay ----
public class SanPham
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public decimal Gia { get; set; }
    public decimal GiaVon { get; set; }          // bi mat kinh doanh: tuyet doi khong tra ra API
    public string? EmailNhaCungCap { get; set; } // du lieu noi bo
    public List<string> The { get; set; } = [];
}

// Anh xa entity <-> DTO bang extension method (don gian, kiem soat tuyet doi, khong can thu vien)
public static class SanPhamMapping
{
    public static SanPhamResponse ToResponse(this SanPham s) => new(s.Id, s.Ma, s.Ten, s.Gia, s.The);

    public static SanPham ToEntity(this TaoSanPhamRequest r) => new()
    {
        Ma = r.Ma!,
        Ten = r.Ten!.Trim(),
        Gia = r.Gia,
        GiaVon = r.Gia * 0.7m,
        EmailNhaCungCap = r.EmailNhaCungCap,
        The = [.. r.The.Select(t => t.Trim().ToLowerInvariant()).Distinct()],
    };
}

public class KhoSanPham
{
    private readonly Lock _khoa = new();
    private readonly List<SanPham> _ds = [];
    private int _id = 1;

    public SanPham Them(SanPham sp)
    {
        lock (_khoa)
        {
            sp.Id = _id++;
            _ds.Add(sp);
            return sp;
        }
    }

    public SanPham? Tim(int id) { lock (_khoa) return _ds.FirstOrDefault(s => s.Id == id); }
    public bool TonTaiMa(string ma) { lock (_khoa) return _ds.Any(s => s.Ma == ma); }
    public List<SanPham> TatCa() { lock (_khoa) return [.. _ds]; }
}
