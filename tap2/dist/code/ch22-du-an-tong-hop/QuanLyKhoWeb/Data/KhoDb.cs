using Microsoft.EntityFrameworkCore;

namespace QuanLyKhoWeb.Data;

public class SanPham
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string Nhom { get; set; } = "";
    public decimal DonGia { get; set; }
    public int TonKho { get; set; }
    public int MucCanhBao { get; set; } = 5;
}

public enum LoaiGiaoDich { Nhap, Xuat }

public class GiaoDich
{
    public int Id { get; set; }
    public int SanPhamId { get; set; }
    public SanPham SanPham { get; set; } = null!;
    public LoaiGiaoDich Loai { get; set; }
    public int SoLuong { get; set; }
    public string? GhiChu { get; set; }
    public string NguoiThucHien { get; set; } = "";
    public DateTime ThoiGian { get; set; }
}

public class KhoDb(DbContextOptions<KhoDb> options) : DbContext(options)
{
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<GiaoDich> GiaoDichs => Set<GiaoDich>();

    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
        => cfg.Properties<decimal>().HaveConversion<double>();       // SQLite; CSDL khac dung decimal(18,2)

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<SanPham>(e =>
        {
            e.ToTable("san_pham", t =>
            {
                t.HasCheckConstraint("ck_ton_khong_am", "TonKho >= 0");      // CSDL la ranh gioi cuoi cung cua bat bien nghiep vu
                t.HasCheckConstraint("ck_gia_khong_am", "DonGia >= 0");
            });
            e.HasIndex(s => s.Ma).IsUnique();
            e.HasIndex(s => s.Nhom);
            e.Property(s => s.Ma).HasMaxLength(20);
            e.Property(s => s.Ten).HasMaxLength(200);
            e.Property(s => s.Nhom).HasMaxLength(100);
        });

        mb.Entity<GiaoDich>(e =>
        {
            e.ToTable("giao_dich");
            e.HasIndex(g => new { g.SanPhamId, g.ThoiGian });
            e.Property(g => g.Loai).HasConversion<string>().HasMaxLength(10);
            e.Property(g => g.NguoiThucHien).HasMaxLength(50);
        });
    }

    public static async Task NapMauAsync(KhoDb db)
    {
        if (await db.SanPhams.AnyAsync()) return;
        db.SanPhams.AddRange(
            new SanPham { Ma = "LT001", Ten = "Laptop Dell", Nhom = "Thiet bi", DonGia = 18_000_000, TonKho = 4 },
            new SanPham { Ma = "CH001", Ten = "Chuot khong day", Nhom = "Phu kien", DonGia = 150_000, TonKho = 50 },
            new SanPham { Ma = "BP001", Ten = "Ban phim co", Nhom = "Phu kien", DonGia = 500_000, TonKho = 12 },
            new SanPham { Ma = "MH001", Ten = "Man hinh 24 inch", Nhom = "Thiet bi", DonGia = 3_500_000, TonKho = 2 });
        await db.SaveChangesAsync();
    }
}
