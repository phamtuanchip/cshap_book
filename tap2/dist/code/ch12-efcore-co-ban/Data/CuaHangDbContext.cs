using CuaHang.Models;
using Microsoft.EntityFrameworkCore;

namespace CuaHang.Data;

// DbContext = "phien lam viec" voi CSDL: doi tuong trung tam cua EF Core (Unit of Work + Repository)
public class CuaHangDbContext : DbContext
{
    public DbSet<Nhom> Nhoms => Set<Nhom>();          // moi DbSet ~ mot bang
    public DbSet<SanPham> SanPhams => Set<SanPham>();

    public CuaHangDbContext() { }                              // dung cho dotnet ef (design time) va demo console
    public CuaHangDbContext(DbContextOptions<CuaHangDbContext> options) : base(options) { }   // dung khi co DI (web)

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
            options.UseSqlite("Data Source=cuahang.db");
    }

    // SQLite khong co kieu decimal that su -> chuyen sang double khi luu (SQL Server/PostgreSQL dung decimal(18,2))
    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
        => cfg.Properties<decimal>().HaveConversion<double>();

    // Fluent API: cau hinh chi tiet (uu tien hon quy uoc va attribute)
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Nhom>(e =>
        {
            e.ToTable("nhom");
            e.HasIndex(n => n.Ten).IsUnique();
            e.Property(n => n.Ten).IsRequired().HasMaxLength(100);
        });

        mb.Entity<SanPham>(e =>
        {
            e.ToTable("san_pham");
            e.HasIndex(s => s.Ma).IsUnique();
            e.Property(s => s.Ma).IsRequired().HasMaxLength(20);
            e.Property(s => s.Ten).IsRequired().HasMaxLength(200);
            e.ToTable(t => t.HasCheckConstraint("ck_san_pham_gia", "Gia >= 0"));
            e.HasOne(s => s.Nhom)
             .WithMany(n => n.SanPhams)
             .HasForeignKey(s => s.NhomId)
             .OnDelete(DeleteBehavior.Restrict);        // khong cho xoa nhom con san pham
        });

        // Du lieu khoi tao (di kem migration)
        mb.Entity<Nhom>().HasData(
            new Nhom { Id = 1, Ten = "Phu kien" },
            new Nhom { Id = 2, Ten = "Thiet bi" });
    }
}
