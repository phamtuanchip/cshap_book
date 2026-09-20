using System.Net;
using System.Net.Http.Json;
using CuaHangApi;
using CuaHangApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CuaHangApi.Tests;

// CSDL SQLite TRONG BO NHO: that (co rang buoc, SQL that) nhung nhanh va tu bien mat.
// Ket noi phai giu MO thi CSDL :memory: moi ton tai.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _conn.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<CuaHangDbContext>>();
            s.AddDbContext<CuaHangDbContext>(o => o.UseSqlite(_conn));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conn.Dispose();
    }
}

public class CuaHangApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<SanPhamDto> TaoSanPhamAsync(string ma, int ton = 10, decimal gia = 1000)
    {
        var res = await _client.PostAsJsonAsync("/api/san-pham", new { ma, ten = "SP " + ma, gia, ton, nhomId = 1 });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<SanPhamDto>())!;
    }

    [Fact]
    public async Task DanhSach_PhanTrangVaSapXep()
    {
        var kq = await _client.GetFromJsonAsync<TrangKetQua<SanPhamDto>>("/api/san-pham?trang=1&kichThuoc=2&sapXep=-gia");

        Assert.NotNull(kq);
        Assert.Equal(2, kq.Muc.Count);
        Assert.True(kq.Muc[0].Gia >= kq.Muc[1].Gia);
        Assert.True(kq.TongSo >= 5);
        Assert.True(kq.CoTrangSau);
    }

    [Fact]
    public async Task DanhSach_LocTheoTuKhoa()
    {
        var kq = await _client.GetFromJsonAsync<TrangKetQua<SanPhamDto>>("/api/san-pham?tim=laptop");

        var sp = Assert.Single(kq!.Muc);
        Assert.Equal("LT001", sp.Ma);
    }

    [Fact]
    public async Task TaoSanPham_TrungMa_Tra409()
    {
        await TaoSanPhamAsync("TR001");

        var res = await _client.PostAsJsonAsync("/api/san-pham", new { ma = "TR001", ten = "Trung", gia = 1, ton = 1, nhomId = 1 });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task TaoSanPham_MaSai_Tra400()
    {
        var res = await _client.PostAsJsonAsync("/api/san-pham", new { ma = "sai", ten = "X", gia = 1, ton = 1, nhomId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task XoaMem_SanPhamBienMatKhoiDanhSachNhungKhongMatDuLieu()
    {
        var sp = await TaoSanPhamAsync("XM001");

        var xoa = await _client.DeleteAsync($"/api/san-pham/{sp.Id}");
        var lay = await _client.GetAsync($"/api/san-pham/{sp.Id}");

        Assert.Equal(HttpStatusCode.NoContent, xoa.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, lay.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CuaHangDbContext>();
        Assert.True(await db.SanPhams.IgnoreQueryFilters().AnyAsync(s => s.Ma == "XM001"));   // van con trong CSDL
    }

    [Fact]
    public async Task NhapKho_TangTon()
    {
        var sp = await TaoSanPhamAsync("NK001", ton: 5);

        var res = await _client.PostAsJsonAsync($"/api/san-pham/{sp.Id}/nhap-kho", new { soLuong = 7 });
        var dto = await res.Content.ReadFromJsonAsync<SanPhamDto>();

        Assert.Equal(12, dto!.Ton);
    }

    [Fact]
    public async Task DatHang_DuHang_TruKhoVaTinhTien()
    {
        var sp = await TaoSanPhamAsync("DH001", ton: 10, gia: 2000);

        var res = await _client.PostAsJsonAsync("/api/don-hang",
            new { khachHangId = 1, dong = new[] { new { sanPhamId = sp.Id, soLuong = 3 } } });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var don = await res.Content.ReadFromJsonAsync<DonHangDto>();
        Assert.Equal(6000m, don!.TongTien);
        var saiDatHang = await _client.GetFromJsonAsync<SanPhamDto>($"/api/san-pham/{sp.Id}");
        Assert.Equal(7, saiDatHang!.Ton);
    }

    [Fact]
    public async Task DatHang_KhongDuHang_Tra409VaKhongTruKho()
    {
        var sp = await TaoSanPhamAsync("DH002", ton: 2);

        var res = await _client.PostAsJsonAsync("/api/don-hang",
            new { khachHangId = 1, dong = new[] { new { sanPhamId = sp.Id, soLuong = 5 } } });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var sau = await _client.GetFromJsonAsync<SanPhamDto>($"/api/san-pham/{sp.Id}");
        Assert.Equal(2, sau!.Ton);
    }

    [Fact]
    public async Task DatHang_NhieuDong_MotDongThieuHang_KhongDongNaoBiTru()
    {
        var a = await TaoSanPhamAsync("DH003", ton: 10);
        var b = await TaoSanPhamAsync("DH004", ton: 1);

        var res = await _client.PostAsJsonAsync("/api/don-hang",
            new { khachHangId = 1, dong = new[] { new { sanPhamId = a.Id, soLuong = 4 }, new { sanPhamId = b.Id, soLuong = 9 } } });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(10, (await _client.GetFromJsonAsync<SanPhamDto>($"/api/san-pham/{a.Id}"))!.Ton);   // hang A khong bi tru
    }

    [Fact]
    public async Task DatHang_KhachKhongTonTai_Tra404()
    {
        var res = await _client.PostAsJsonAsync("/api/don-hang",
            new { khachHangId = 999, dong = new[] { new { sanPhamId = 1, soLuong = 1 } } });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
