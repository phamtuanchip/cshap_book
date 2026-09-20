using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace DonHangApi.Tests;

// WebApplicationFactory<Program>: khoi dong CA UNG DUNG THAT trong bo nho (khong mo cong that),
// tra ve HttpClient goi thang vao pipeline: middleware, routing, binding, JSON... tat ca deu chay that.
public class ApiFactory : WebApplicationFactory<Program>
{
    // Cong thanh toan gia dung chung cho cac test trong class (khong goi mang that)
    public ICongThanhToan CongThanhToanGia { get; } = Substitute.For<ICongThanhToan>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Thay dang ky that bang ban gia
            services.AddScoped(_ => CongThanhToanGia);
        });
    }
}

public class DonHangApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public DonHangApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Api-Key", "khoa-thu-nghiem");

        // Mac dinh: thanh toan thanh cong
        factory.CongThanhToanGia
            .ThanhToanAsync(Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(new KetQuaThanhToan(true, "GD-TEST-01", null));
    }

    [Fact]
    public async Task TrangChu_TraVeChuoiXacNhan()
    {
        var res = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("dang chay", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task KhongCoApiKey_Tra401()
    {
        var khongKhoa = _factory.CreateClient();          // client moi, KHONG co header

        var res = await khongKhoa.GetAsync("/api/don-hang");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task TaoDonHang_HopLe_Tra201VaLocation()
    {
        var res = await _client.PostAsJsonAsync("/api/don-hang", new { khachHang = "An", soTien = 250_000 });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var don = await res.Content.ReadFromJsonAsync<DonHang>();
        Assert.NotNull(don);
        Assert.Equal("An", don.KhachHang);
        Assert.Equal("GD-TEST-01", don.MaGiaoDich);
        Assert.Equal($"/api/don-hang/{don.Id}", res.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task TaoRoiLay_DungDonHangVuaTao()
    {
        var tao = await _client.PostAsJsonAsync("/api/don-hang", new { khachHang = "Binh", soTien = 99_000 });
        var don = await tao.Content.ReadFromJsonAsync<DonHang>();

        var lay = await _client.GetFromJsonAsync<DonHang>($"/api/don-hang/{don!.Id}");

        Assert.Equal(don, lay);       // record: so sanh theo gia tri
    }

    [Fact]
    public async Task LayDonKhongTonTai_Tra404()
    {
        var res = await _client.GetAsync("/api/don-hang/99999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Theory]
    [InlineData("", 1000)]
    [InlineData("An", 0)]
    [InlineData("An", -5)]
    public async Task TaoDonHang_DuLieuSai_Tra400(string khachHang, decimal soTien)
    {
        var res = await _client.PostAsJsonAsync("/api/don-hang", new { khachHang, soTien });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ThanhToanThatBai_Tra402VaKhongLuuDon()
    {
        _factory.CongThanhToanGia
            .ThanhToanAsync(Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(new KetQuaThanhToan(false, null, "The bi tu choi"));
        int truoc = (await _client.GetFromJsonAsync<List<DonHang>>("/api/don-hang"))!.Count;

        var res = await _client.PostAsJsonAsync("/api/don-hang", new { khachHang = "Chi", soTien = 500_000 });
        int sau = (await _client.GetFromJsonAsync<List<DonHang>>("/api/don-hang"))!.Count;

        Assert.Equal(HttpStatusCode.PaymentRequired, res.StatusCode);
        Assert.Equal(truoc, sau);
    }
}
