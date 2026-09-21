using Kho.Application;
using Kho.Application.Abstractions;
using Kho.Application.Messaging;
using Kho.Application.SanPhams;
using Kho.Infrastructure;
using Kho.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TroLyKho;
using Xunit;

namespace ChuongDuAn;

// Kiem thu TAT DINH cho lop cong cu AI (Chuong 5+9): khong can mo hinh nao ca, chi goi truc tiep cac phuong thuc
// cua CongCuKho tren mot CSDL SQLite tam thoi that (cung ha tang voi kho-clean) - day la "logic xung quanh mo hinh"
// theo dung phan loai o Chuong 9: PHAN NAY phai dung 100%, khong duoc phep "thinh thoang sai".
public class CongCuKhoTests : IAsyncLifetime
{
    private ServiceProvider _sp = null!;
    private string _file = null!;
    private CongCuKho _congCu = null!;

    public async Task InitializeAsync()
    {
        _file = Path.Combine(Path.GetTempPath(), $"congcu-kho-test-{Guid.NewGuid():N}.db");
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Kho"] = $"Data Source={_file}",
            ["Outbox:Bat"] = "false",
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(cfg);
        _sp = services.BuildServiceProvider();

        await using (var scope = _sp.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<KhoDbContext>().Database.MigrateAsync();

        var sender = _sp.GetRequiredService<ISender>();
        await sender.Send(new ThemSanPhamCommand("LT001", "Laptop Dell XPS 13", "dien-tu", 25_000_000m, 3));
        await sender.Send(new ThemSanPhamCommand("BP001", "Ban phim co Keychron", "phu-kien", 1_500_000m, 40));

        _congCu = new CongCuKho(sender);
    }

    public async Task DisposeAsync()
    {
        await _sp.DisposeAsync();
        SqliteClearPools();
        try { File.Delete(_file); } catch { }
    }

    private static void SqliteClearPools() => Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

    [Fact]
    public async Task TraTonKho_SanPhamCoThat_TraDungDuLieuTuCSDL()
    {
        string json = await _congCu.TraTonKho("LT001");
        Assert.Contains("\"ton\":3", json);
        Assert.Contains("Laptop Dell XPS 13", json);
    }

    [Fact]
    public async Task TraTonKho_SanPhamKhongTonTai_TraLoiRoRang()
    {
        string json = await _congCu.TraTonKho("ZZ999");
        Assert.Contains("loi", json);
    }

    [Fact]
    public async Task TimSanPham_TheoNhom_ChiTraSanPhamDungNhom()
    {
        string json = await _congCu.TimSanPham(tuKhoa: null, nhom: "phu-kien");
        Assert.Contains("BP001", json);
        Assert.DoesNotContain("LT001", json);
    }

    [Fact]
    public async Task DeXuatXuatKho_TrongHanMuc_TraDeXuatChoDuyet_KHONG_TruTonThat()
    {
        string json = await _congCu.DeXuatXuatKho("LT001", 2);
        Assert.Contains("cho_nguoi_duyet", json);

        // Kiem chung QUAN TRONG NHAT cua ca chuong: du lieu THAT khong doi
        string sauDo = await _congCu.TraTonKho("LT001");
        Assert.Contains("\"ton\":3", sauDo);
    }

    [Fact]
    public async Task DeXuatXuatKho_VuotTonKho_BiTuChoi()
    {
        string json = await _congCu.DeXuatXuatKho("LT001", 999);
        Assert.Contains("loi", json);
        Assert.Contains("vuot ton kho", json);
    }

    [Fact]
    public async Task DeXuatXuatKho_SoLuongAmHoacKhong_BiTuChoiNgayTaiCongCu()
    {
        Assert.Contains("loi", await _congCu.DeXuatXuatKho("LT001", 0));
        Assert.Contains("loi", await _congCu.DeXuatXuatKho("LT001", -5));
    }
}
