using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuanLyKhoWeb.Data;
using QuanLyKhoWeb.Services;
using Xunit;

namespace QuanLyKhoWeb.Tests;

// Dung FILE SQLite tam (moi request mot ket noi rieng, nhu production) thay vi ":memory:" dung chung mot ket noi:
// neu chia se MOT ket noi thi cac transaction dong thoi de dam len nhau va test khong phan anh dung thuc te.
public class KhoFactory : WebApplicationFactory<Program>
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"quanlykho-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("GioiHan:DangNhap", "1000");           // test khong bi rate limit chan
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<KhoDb>>();
            s.AddDbContext<KhoDb>(o => o.UseSqlite($"Data Source={_file};Default Timeout=30"));   // cho khoa ghi toi da 30s thay vi loi "database is locked"
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        SqliteConnection.ClearAllPools();                          // nhac file de xoa duoc tren Windows
        foreach (var f in new[] { _file, _file + "-wal", _file + "-shm" }) try { File.Delete(f); } catch { }
    }
}

public class KhoApiTests(KhoFactory factory) : IClassFixture<KhoFactory>
{
    private async Task<HttpClient> DangNhapAsync(string ten, string matKhau)
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/dang-nhap", new { tenDangNhap = ten, matKhau });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!["accessToken"].GetString());
        return client;
    }

    private Task<HttpClient> NhanVienAsync() => DangNhapAsync("nhanvien", "NhanVien@123");
    private Task<HttpClient> AdminAsync() => DangNhapAsync("admin", "Admin@123");

    private static async Task<SanPhamDto> TaoAsync(HttpClient admin, string ma, int ton = 10)
    {
        var res = await admin.PostAsJsonAsync("/api/san-pham", new { ma, ten = "SP " + ma, nhom = "Test", donGia = 1000, tonDau = ton });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<SanPhamDto>())!;
    }

    // ---------- Xac thuc & phan quyen ----------
    [Fact]
    public async Task KhongDangNhap_MoiEndpointTra401()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/san-pham")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/bao-cao/nhom")).StatusCode);
    }

    [Fact]
    public async Task DangNhapSai_Tra401()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/dang-nhap", new { tenDangNhap = "admin", matKhau = "sai" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Health_CongKhaiKhongCanDangNhap()
        => Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync("/health")).StatusCode);

    [Fact]
    public async Task NhanVien_KhongDuocThemSanPham_Tra403()
    {
        var nv = await NhanVienAsync();
        var res = await nv.PostAsJsonAsync("/api/san-pham", new { ma = "AB123", ten = "X", nhom = "Y", donGia = 1, tonDau = 0 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ---------- Doc ----------
    [Fact]
    public async Task DanhSach_CoPhanTrangVaTimKiem()
    {
        var nv = await NhanVienAsync();
        var tatCa = await nv.GetFromJsonAsync<TrangKetQua<SanPhamDto>>("/api/san-pham?kichThuoc=2");
        Assert.Equal(2, tatCa!.Muc.Count);
        Assert.True(tatCa.TongSo >= 4);

        var laptop = await nv.GetFromJsonAsync<TrangKetQua<SanPhamDto>>("/api/san-pham?tim=laptop");
        Assert.Contains(laptop!.Muc, s => s.Ma == "LT001");
    }

    // ---------- Ghi ----------
    [Fact]
    public async Task Admin_ThemSanPham_201_TrungMa_409_MaSai_400()
    {
        var admin = await AdminAsync();
        await TaoAsync(admin, "TH001");

        var trung = await admin.PostAsJsonAsync("/api/san-pham", new { ma = "TH001", ten = "Trung", nhom = "Test", donGia = 1, tonDau = 0 });
        var sai = await admin.PostAsJsonAsync("/api/san-pham", new { ma = "sai", ten = "X", nhom = "Test", donGia = 1, tonDau = 0 });

        Assert.Equal(HttpStatusCode.Conflict, trung.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sai.StatusCode);
    }

    [Fact]
    public async Task NhapXuat_CapNhatTonVaGhiLichSu()
    {
        var admin = await AdminAsync();
        var nv = await NhanVienAsync();
        var sp = await TaoAsync(admin, "NX001", ton: 10);

        var nhap = await (await nv.PostAsJsonAsync($"/api/san-pham/{sp.Id}/nhap", new { soLuong = 5, ghiChu = "NCC A" })).Content.ReadFromJsonAsync<SanPhamDto>();
        var xuat = await (await nv.PostAsJsonAsync($"/api/san-pham/{sp.Id}/xuat", new { soLuong = 8 })).Content.ReadFromJsonAsync<SanPhamDto>();

        Assert.Equal(15, nhap!.TonKho);
        Assert.Equal(7, xuat!.TonKho);
        var ls = await nv.GetFromJsonAsync<List<GiaoDichDto>>("/api/giao-dich?ma=NX001");
        Assert.Equal(3, ls!.Count);                                   // ton dau + nhap + xuat
        Assert.Contains(ls, g => g.Loai == "Xuat" && g.NguoiThucHien == "nhanvien");
    }

    [Fact]
    public async Task Xuat_KhongDuHang_409_VaTonKhongDoi()
    {
        var admin = await AdminAsync();
        var sp = await TaoAsync(admin, "KD001", ton: 3);

        var res = await admin.PostAsJsonAsync($"/api/san-pham/{sp.Id}/xuat", new { soLuong = 10 });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(3, (await admin.GetFromJsonAsync<SanPhamDto>($"/api/san-pham/{sp.Id}"))!.TonKho);
    }

    [Fact]
    public async Task Xuat_SanPhamKhongTonTai_404()
    {
        var nv = await NhanVienAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await nv.PostAsJsonAsync("/api/san-pham/99999/xuat", new { soLuong = 1 })).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(1_000_000)]
    public async Task Xuat_SoLuongKhongHopLe_400(int soLuong)
    {
        var nv = await NhanVienAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await nv.PostAsJsonAsync("/api/san-pham/1/xuat", new { soLuong })).StatusCode);
    }

    // ---------- DONG THOI: bat bien "ton khong am" ----------
    [Fact]
    public async Task XuatDongThoi_TonKhongBaoGioAm_ChiDungSoLanDuHang()
    {
        var admin = await AdminAsync();
        var sp = await TaoAsync(admin, "DT001", ton: 5);

        // 20 yeu cau xuat 1 don vi dong thoi cho san pham chi con 5
        var tasks = Enumerable.Range(0, 20).Select(_ => admin.PostAsJsonAsync($"/api/san-pham/{sp.Id}/xuat", new { soLuong = 1 })).ToArray();
        var ketQua = await Task.WhenAll(tasks);

        int thanhCong = ketQua.Count(r => r.StatusCode == HttpStatusCode.OK);
        int thatBai = ketQua.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(5, thanhCong);                                    // dung 5 lan xuat duoc
        Assert.Equal(15, thatBai);
        Assert.Equal(0, (await admin.GetFromJsonAsync<SanPhamDto>($"/api/san-pham/{sp.Id}"))!.TonKho);
    }

    // ---------- Bao cao ----------
    [Fact]
    public async Task BaoCao_TheoNhomVaSapHet()
    {
        var nv = await NhanVienAsync();
        var nhom = await nv.GetFromJsonAsync<List<NhomBaoCao>>("/api/bao-cao/nhom");
        Assert.Contains(nhom!, n => n.Nhom == "Phu kien");
        var sapHet = await nv.GetFromJsonAsync<List<SanPhamDto>>("/api/bao-cao/sap-het");
        Assert.All(sapHet!, s => Assert.True(s.TonKho <= 5));
    }
}
