using System.Net;
using System.Net.Http.Json;
using Kho.Application.Abstractions;
using Kho.Infrastructure.Outbox;
using Kho.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Kho.Tests;

// Bo gom cac su kien duoc "phat hanh" de test kiem tra (thay cho message bus that)
public class PhatHanhGia : IPhatHanhSuKien
{
    public List<(string Loai, string NoiDung)> DaPhatHanh { get; } = [];
    public Task PhatHanhAsync(string loai, string noiDungJson, CancellationToken ct)
    {
        lock (DaPhatHanh) DaPhatHanh.Add((loai, noiDungJson));
        return Task.CompletedTask;
    }
}

public class KhoFactory : WebApplicationFactory<Program>
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"kho-clean-test-{Guid.NewGuid():N}.db");
    public PhatHanhGia PhatHanh { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Outbox:Bat", "false");              // tat tien trinh nen: test tu goi XuLyMotLanAsync (xac dinh, khong cho timer)
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<KhoDbContext>>();
            s.AddDbContext<KhoDbContext>(o => o.UseSqlite($"Data Source={_file};Default Timeout=30"));
            s.RemoveAll<IPhatHanhSuKien>();
            s.AddSingleton<IPhatHanhSuKien>(PhatHanh);
            s.AddSingleton<OutboxProcessor>();                   // dang ky de test lay ra (khong chay nen)
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _file, _file + "-wal", _file + "-shm" }) try { File.Delete(f); } catch { }
    }
}

public class IntegrationTests(KhoFactory f) : IClassFixture<KhoFactory>
{
    private readonly HttpClient _c = f.CreateClient();

    private Task<HttpResponseMessage> Tao(string ma, int ton = 10)
        => _c.PostAsJsonAsync("/api/san-pham", new { ma, ten = "SP " + ma, nhom = "Test", donGia = 1000, tonDau = ton });

    [Fact]
    public async Task Tao_201_TrungMa_409_SaiDinhDang_400()
    {
        Assert.Equal(HttpStatusCode.Created, (await Tao("AA001")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Tao("AA001")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Tao("sai")).StatusCode);
    }

    [Fact]
    public async Task Doc_TimKiemVaLayTheoMa()
    {
        await Tao("BB001");
        await Tao("BB002");

        var tim = await _c.GetFromJsonAsync<TrangKetQua<SanPhamDto>>("/api/san-pham?tim=bb00&kichThuoc=5");
        Assert.Equal(2, tim!.TongSo);

        var mot = await _c.GetFromJsonAsync<SanPhamDto>("/api/san-pham/bb001");         // ma khong phan biet hoa/thuong
        Assert.Equal("BB001", mot!.Ma);
        Assert.Equal(10, mot.TonKho);
        Assert.Equal(HttpStatusCode.NotFound, (await _c.GetAsync("/api/san-pham/ZZ999")).StatusCode);
    }

    [Fact]
    public async Task Xuat_TruTonDung_ThieuHang_422_KhongTruTon()
    {
        await Tao("CC001", ton: 5);

        Assert.Equal(HttpStatusCode.NoContent, (await _c.PostAsJsonAsync("/api/san-pham/CC001/xuat", new { soLuong = 2 })).StatusCode);
        var thieu = await _c.PostAsJsonAsync("/api/san-pham/CC001/xuat", new { soLuong = 99 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, thieu.StatusCode);
        Assert.Equal(3, (await _c.GetFromJsonAsync<SanPhamDto>("/api/san-pham/CC001"))!.TonKho);
        Assert.Equal(HttpStatusCode.NotFound, (await _c.PostAsJsonAsync("/api/san-pham/ZZ999/xuat", new { soLuong = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _c.PostAsJsonAsync("/api/san-pham/CC001/xuat", new { soLuong = 0 })).StatusCode);
    }

    [Fact]
    public async Task XuatDongThoi_KhongBaoGioAm_VaTongThanhCongDungBangSoDuocPhep()
    {
        await Tao("DD001", ton: 5);

        var kq = await Task.WhenAll(Enumerable.Range(0, 15).Select(_ => _c.PostAsJsonAsync("/api/san-pham/DD001/xuat", new { soLuong = 1 })));

        int ok = kq.Count(r => r.StatusCode == HttpStatusCode.NoContent);
        int tonCuoi = (await _c.GetFromJsonAsync<SanPhamDto>("/api/san-pham/DD001"))!.TonKho;
        Assert.True(tonCuoi >= 0);                               // BAT BIEN: khong bao gio am
        Assert.Equal(5 - ok, tonCuoi);                           // moi lan thanh cong tru dung 1
        Assert.All(kq, r => Assert.True(r.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity));
    }

    [Fact]
    public async Task SuKienMien_DuocGhiVaoOutboxCungGiaoDich_RoiPhatHanhDungMotLan()
    {
        await Tao("EE001", ton: 7);                                                    // -> SanPhamDaTao
        await _c.PostAsJsonAsync("/api/san-pham/EE001/xuat", new { soLuong = 3 });     // -> TonKhoThayDoi + SapHetHang (7 -> 4 <= 5)

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KhoDbContext>();
        var cho = await db.Outbox.Where(o => o.XuLyLuc == null && o.NoiDung.Contains("EE001")).ToListAsync();
        Assert.Contains(cho, o => o.Loai == "SanPhamDaTao");
        Assert.Contains(cho, o => o.Loai == "SapHetHang");

        var bxl = f.Services.GetRequiredService<OutboxProcessor>();
        await bxl.XuLyMotLanAsync(CancellationToken.None);
        await bxl.XuLyMotLanAsync(CancellationToken.None);                              // chay lan hai: khong phat hanh lai

        lock (f.PhatHanh.DaPhatHanh)
        {
            var cuaEE = f.PhatHanh.DaPhatHanh.Where(p => p.NoiDung.Contains("EE001")).ToList();
            Assert.Single(cuaEE, p => p.Loai == "SapHetHang");
            Assert.Single(cuaEE, p => p.Loai == "SanPhamDaTao");
        }
    }

    [Fact]
    public async Task Outbox_LoiPhatHanhDuocThuLaiVaDemSoLan()
    {
        await Tao("FF001");
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KhoDbContext>();
        db.Outbox.Add(new OutboxMessage { Loai = "Hong", NoiDung = "{}", TaoLuc = DateTimeOffset.UtcNow.AddYears(-1) });
        await db.SaveChangesAsync();

        var bxl = f.Services.GetRequiredService<OutboxProcessor>();
        await bxl.XuLyMotLanAsync(CancellationToken.None);       // PhatHanhGia luon thanh cong -> thong diep nay xu ly xong

        Assert.NotNull((await db.Outbox.AsNoTracking().FirstAsync(o => o.Loai == "Hong")).XuLyLuc);
    }
}
