using System.Text.Json;
using Kho.Application.Abstractions;
using Kho.Application.Behaviors;
using Kho.Domain.Common;
using Kho.Domain.SanPhams;
using Microsoft.EntityFrameworkCore;

namespace Kho.Infrastructure.Persistence;

// Thong diep outbox: "viec can lam sau khi giao dich commit". Ghi CUNG giao dich voi du lieu nghiep vu.
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Loai { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public DateTimeOffset TaoLuc { get; set; }
    public DateTimeOffset? XuLyLuc { get; set; }
    public int SoLanThu { get; set; }
    public string? LoiCuoi { get; set; }
}

public class SanPhamDoc
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public string Nhom { get; set; } = "";
    public decimal DonGia { get; set; }
    public int TonKho { get; set; }
    public int MucCanhBao { get; set; }
}

public class KhoDbContext(DbContextOptions<KhoDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<SanPhamDoc> SanPhamDocs => Set<SanPhamDoc>();          // mo hinh DOC (read model), khong qua aggregate

    // SQLite khong ORDER BY / so sanh duoc DateTimeOffset gia tri goc -> luu dang so nguyen nhi phan (sap xep dung thu tu thoi gian)
    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
        => cfg.Properties<DateTimeOffset>().HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<SanPham>(e =>
        {
            e.ToTable("san_pham", t =>
            {
                t.HasCheckConstraint("ck_ton_khong_am", "TonKho >= 0");
                t.HasCheckConstraint("ck_gia_khong_am", "DonGia >= 0");
            });
            e.HasKey(s => s.Id);
            e.Ignore(s => s.SuKienMien);                                        // su kien mien khong luu vao bang san pham

            // Value object <-> cot don gian (Domain khong biet CSDL; anh xa nam o Infrastructure)
            e.Property(s => s.Ma).HasConversion(m => m.GiaTri, v => MaSanPham.TuCsdl(v)).HasMaxLength(20).IsRequired();
            e.Property(s => s.DonGia).HasConversion(t => (double)t.SoTien, v => Tien.TuCsdl((decimal)v));
            e.HasIndex(s => s.Ma).IsUnique();
            e.Property(s => s.Ten).HasMaxLength(200);
            e.Property(s => s.Nhom).HasMaxLength(100);
            e.Property(s => s.TonKho).IsConcurrencyToken();                     // hai nguoi cung sua ton -> nguoi sau nhan DbUpdateConcurrencyException
        });

        // READ MODEL: thuc the KHONG khoa, chieu tu chinh bang san_pham; loc/sap xep tren cac cot don gian (khong dinh value object)
        mb.Entity<SanPhamDoc>(e =>
        {
            e.HasNoKey();
            e.ToSqlQuery("SELECT Id, Ma, Ten, Nhom, DonGia, TonKho, MucCanhBao FROM san_pham");
            e.Property(x => x.DonGia).HasConversion<double>();
        });

        mb.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox");
            e.HasIndex(o => new { o.XuLyLuc, o.TaoLuc });
            e.Property(o => o.Loai).HasMaxLength(200);
        });
    }

    // ---- IUnitOfWork: MOT giao dich gom du lieu nghiep vu + outbox ----
    public async Task<int> LuuAsync(CancellationToken ct)
    {
        // 1) Gom su kien mien tu MOI aggregate dang duoc theo doi, doi thanh thong diep outbox
        var aggregates = ChangeTracker.Entries<Entity<int>>().Select(e => e.Entity).Where(e => e.SuKienMien.Count > 0).ToList();
        foreach (var agg in aggregates)
        {
            foreach (var sk in agg.SuKienMien)
                Outbox.Add(new OutboxMessage { Loai = sk.GetType().Name, NoiDung = JsonSerializer.Serialize(sk, sk.GetType()), TaoLuc = sk.XayRaLuc });
            agg.XoaSuKien();
        }

        // 2) Luu tat ca (du lieu + outbox) trong MOT giao dich; dich loi ha tang sang ngon ngu Application
        try
        {
            return await SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException();
        }
        catch (DbUpdateException e) when (e.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new TrungLapException("Du lieu bi trung (vi pham rang buoc duy nhat)");
        }
    }
}
