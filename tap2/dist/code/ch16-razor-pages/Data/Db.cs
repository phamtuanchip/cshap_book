using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebRazor.Data;

public class Nhom
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
}

public class SanPham
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public decimal Gia { get; set; }
    public int Ton { get; set; }
    public int NhomId { get; set; }
    public Nhom Nhom { get; set; } = null!;
}

// Kieu dung de NHAN du lieu form (khong phai entity): tranh over-posting
public class SanPhamInput
{
    [Required(ErrorMessage = "Vui lòng nhập mã")]
    [RegularExpression("^[A-Z]{2}[0-9]{3}$", ErrorMessage = "Mã gồm 2 chữ HOA và 3 số, ví dụ LT001")]
    [Display(Name = "Mã sản phẩm")]
    public string? Ma { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên"), StringLength(100, MinimumLength = 2, ErrorMessage = "Tên từ 2 đến 100 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string? Ten { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Giá không hợp lệ")]
    [Display(Name = "Giá (đồng)")]
    public decimal Gia { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Tồn kho không hợp lệ")]
    [Display(Name = "Tồn kho")]
    public int Ton { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhóm")]
    [Display(Name = "Nhóm")]
    public int NhomId { get; set; }
}

public class CuaHangDb(DbContextOptions<CuaHangDb> options) : DbContext(options)
{
    public DbSet<Nhom> Nhoms => Set<Nhom>();
    public DbSet<SanPham> SanPhams => Set<SanPham>();

    protected override void ConfigureConventions(ModelConfigurationBuilder cfg) => cfg.Properties<decimal>().HaveConversion<double>();

    protected override void OnModelCreating(ModelBuilder mb)
        => mb.Entity<SanPham>().HasIndex(s => s.Ma).IsUnique();

    public static async Task NapMauAsync(CuaHangDb db)
    {
        if (await db.Nhoms.AnyAsync()) return;
        var pk = new Nhom { Ten = "Phụ kiện" };
        var tb = new Nhom { Ten = "Thiết bị" };
        db.SanPhams.AddRange(
            new SanPham { Ma = "CH001", Ten = "Chuột không dây", Gia = 150_000, Ton = 30, Nhom = pk },
            new SanPham { Ma = "BP001", Ten = "Bàn phím cơ", Gia = 500_000, Ton = 12, Nhom = pk },
            new SanPham { Ma = "MH001", Ten = "Màn hình 24 inch", Gia = 3_500_000, Ton = 5, Nhom = tb },
            new SanPham { Ma = "LT001", Ten = "Laptop Dell", Gia = 18_000_000, Ton = 3, Nhom = tb });
        await db.SaveChangesAsync();
    }
}
