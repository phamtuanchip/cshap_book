using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CuaHang.Data;

public class CuaHangDbContext : DbContext
{
    // Dem so cau lenh SQL thuc su gui toi CSDL - de THAY N+1 bang so lieu
    public static int SoCauLenh;

    public DbSet<Nhom> Nhoms => Set<Nhom>();
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<The> Thes => Set<The>();
    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
    public DbSet<DonHang> DonHangs => Set<DonHang>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options
            .UseSqlite("Data Source=ch13.db")
            .LogTo(_ => Interlocked.Increment(ref SoCauLenh), [RelationalEventId.CommandExecuted]);

    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
        => cfg.Properties<decimal>().HaveConversion<double>();          // SQLite khong co decimal

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<SanPham>(e =>
        {
            e.HasIndex(s => s.Ma).IsUnique();
            e.Property(s => s.Ma).HasMaxLength(20);
            e.Property(s => s.PhienBan).IsConcurrencyToken();          // token xung dot
            e.HasQueryFilter(s => !s.DaXoa);                           // MOI truy van tu dong loai san pham da xoa mem
            e.HasOne(s => s.Nhom).WithMany(n => n.SanPhams).HasForeignKey(s => s.NhomId);
            e.HasMany(s => s.The).WithMany(t => t.SanPhams);           // nhieu-nhieu: EF tu tao bang noi
        });

        mb.Entity<KhachHang>().OwnsOne(k => k.DiaChi);                 // value object luu chung bang

        mb.Entity<ChiTietDon>(e =>
        {
            e.HasKey(c => new { c.DonHangId, c.SanPhamId });           // khoa chinh ket hop
            e.HasOne(c => c.DonHang).WithMany(d => d.ChiTiets).HasForeignKey(c => c.DonHangId);
            e.HasOne(c => c.SanPham).WithMany(s => s.ChiTiets).HasForeignKey(c => c.SanPhamId);
        });
    }
}
