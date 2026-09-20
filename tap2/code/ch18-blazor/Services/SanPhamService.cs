using System.ComponentModel.DataAnnotations;

namespace WebBlazor.Services;

public class SanPham
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public decimal Gia { get; set; }
    public int Ton { get; set; }
}

// Model cho EditForm: Blazor dung chinh DataAnnotations (Chuong 8, 17)
public class SanPhamForm
{
    [Required(ErrorMessage = "Vui lòng nhập mã")]
    [RegularExpression("^[A-Z]{2}[0-9]{3}$", ErrorMessage = "Mã gồm 2 chữ HOA và 3 số, ví dụ LT001")]
    public string? Ma { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên"), StringLength(100, MinimumLength = 2, ErrorMessage = "Tên từ 2 đến 100 ký tự")]
    public string? Ten { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Giá không hợp lệ")]
    public decimal Gia { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Tồn kho không hợp lệ")]
    public int Ton { get; set; }
}

public class SanPhamService
{
    private readonly Lock _khoa = new();
    private readonly List<SanPham> _ds =
    [
        new() { Id = 1, Ma = "CH001", Ten = "Chuột không dây", Gia = 150_000, Ton = 30 },
        new() { Id = 2, Ma = "BP001", Ten = "Bàn phím cơ", Gia = 500_000, Ton = 12 },
        new() { Id = 3, Ma = "MH001", Ten = "Màn hình 24 inch", Gia = 3_500_000, Ton = 5 },
    ];
    private int _id = 4;

    // Gia lap do tre I/O (goi CSDL/API) de thay trang thai "Dang tai..." trong giao dien
    public async Task<List<SanPham>> TimAsync(string? tuKhoa, CancellationToken ct = default)
    {
        await Task.Delay(150, ct);
        lock (_khoa)
            return _ds.Where(s => string.IsNullOrWhiteSpace(tuKhoa)
                    || s.Ten.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)
                    || s.Ma.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Id).Select(Sao).ToList();
    }

    public Task<SanPham> ThemAsync(SanPhamForm f)
    {
        lock (_khoa)
        {
            if (_ds.Any(s => s.Ma == f.Ma)) throw new InvalidOperationException($"Mã {f.Ma} đã tồn tại");
            var sp = new SanPham { Id = _id++, Ma = f.Ma!, Ten = f.Ten!.Trim(), Gia = f.Gia, Ton = f.Ton };
            _ds.Add(sp);
            return Task.FromResult(Sao(sp));
        }
    }

    public Task XoaAsync(int id)
    {
        lock (_khoa) _ds.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }

    private static SanPham Sao(SanPham s) => new() { Id = s.Id, Ma = s.Ma, Ten = s.Ten, Gia = s.Gia, Ton = s.Ton };
}
