using Microsoft.EntityFrameworkCore;

namespace CuaHangApi.Data;

// Nhan DbContextOptions qua constructor: nhu vay DI (web) va test co the chon CSDL khac nhau
public class CuaHangDbContext(DbContextOptions<CuaHangDbContext> options) : DbContext(options)
{
    public DbSet<Nhom> Nhoms => Set<Nhom>();
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
    public DbSet<DonHang> DonHangs => Set<DonHang>();

    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
        => cfg.Properties<decimal>().HaveConversion<double>();          // SQLite; SQL Server/PostgreSQL dung decimal(18,2)

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Nhom>(e =>
        {
            e.ToTable("nhom");
            e.HasIndex(n => n.Ten).IsUnique();
            e.Property(n => n.Ten).HasMaxLength(100);
        });

        mb.Entity<SanPham>(e =>
        {
            e.ToTable("san_pham");
            e.HasIndex(s => s.Ma).IsUnique();
            e.Property(s => s.Ma).HasMaxLength(20);
            e.Property(s => s.Ten).HasMaxLength(200);
            e.Property(s => s.Ton).IsConcurrencyToken();                 // 2 nguoi cung tru kho -> nguoi sau bi loi, khong ghi de am tham
            e.HasQueryFilter(s => !s.DaXoa);
            e.ToTable(t => t.HasCheckConstraint("ck_san_pham_ton", "Ton >= 0"));
        });

        mb.Entity<KhachHang>(e =>
        {
            e.ToTable("khach_hang");
            e.HasIndex(k => k.Email).IsUnique();
        });

        mb.Entity<DonHang>().ToTable("don_hang");

        mb.Entity<ChiTietDon>(e =>
        {
            e.ToTable("chi_tiet_don");
            e.HasKey(c => new { c.DonHangId, c.SanPhamId });
            e.HasOne<DonHang>().WithMany(d => d.ChiTiets).HasForeignKey(c => c.DonHangId);
            e.HasOne(c => c.SanPham).WithMany().HasForeignKey(c => c.SanPhamId);
        });
    }
}
