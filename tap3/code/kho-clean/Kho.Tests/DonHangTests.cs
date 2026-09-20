using System.Net;
using System.Net.Http.Json;
using Kho.Application.Abstractions;
using Kho.Application.DonHangs;
using Kho.Domain.DonHangs;
using Kho.Domain.SanPhams;
using Kho.Infrastructure.Outbox;
using Kho.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kho.Tests;

// ---------- DOMAIN: nhanh, khong ha tang ----------
public class DonHangDomainTests
{
    private static readonly DateTimeOffset Gio = new(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);
    private static DongDon Dong(string ma, int sl, decimal gia) => new(ma, sl, Tien.Tao(gia).GiaTri);

    [Fact]
    public void Tao_TinhTongTien_VaPhatSuKienDonHangDaDat()
    {
        var don = DonHang.Tao("DH-1", " An ", [Dong("LT001", 2, 1000), Dong("CH002", 3, 500)], Gio).GiaTri;

        Assert.Equal(3500, don.TongTien.SoTien);
        Assert.Equal("An", don.KhachHang);
        Assert.Equal(TrangThaiDon.DaDat, don.TrangThai);
        var sk = Assert.IsType<DonHangDaDat>(Assert.Single(don.SuKienMien));
        Assert.Equal(3500, sk.TongTien);
    }

    [Fact]
    public void Tao_KhongDong_HoacTrungSanPham_HoacKhongKhach_ThatBai()
    {
        Assert.False(DonHang.Tao("D", "An", [], Gio).ThanhCong);
        Assert.False(DonHang.Tao("D", "An", [Dong("LT001", 1, 1), Dong("LT001", 2, 1)], Gio).ThanhCong);
        Assert.False(DonHang.Tao("D", " ", [Dong("LT001", 1, 1)], Gio).ThanhCong);
    }

    [Fact]
    public void Huy_HaiLan_LanHaiLoiNghiepVu()
    {
        var don = DonHang.Tao("D", "An", [Dong("LT001", 1, 1)], Gio).GiaTri;
        Assert.True(don.Huy("doi y", Gio).ThanhCong);
        var lan2 = don.Huy("lai", Gio);
        Assert.False(lan2.ThanhCong);
        Assert.Equal("DonHang.DaHuy", lan2.Loi!.Ma);
    }
}

// ---------- TICH HOP: HTTP -> mediator -> domain -> EF/SQLite -> outbox ----------
public class DonHangIntegrationTests(KhoFactory f) : IClassFixture<KhoFactory>
{
    private readonly HttpClient _c = f.CreateClient();

    private Task<HttpResponseMessage> TaoSp(string ma, int ton, int gia = 1000)
        => _c.PostAsJsonAsync("/api/san-pham", new { ma, ten = "SP " + ma, nhom = "T", donGia = gia, tonDau = ton });

    private Task<HttpResponseMessage> Dat(string khach, params (string ma, int sl)[] dong)
        => _c.PostAsJsonAsync("/api/don-hang", new { khachHang = khach, dong = dong.Select(d => new { maSanPham = d.ma, soLuong = d.sl }) });

    private async Task<int> Ton(string ma) => (await _c.GetFromJsonAsync<SanPhamDto>($"/api/san-pham/{ma}"))!.TonKho;

    [Fact]
    public async Task DatHang_TruKhoTatCaCacDong_VaDoiLaiDuocDonHang()
    {
        await TaoSp("DA001", 10, gia: 2000);
        await TaoSp("DA002", 10, gia: 500);

        var res = await Dat("An", ("DA001", 2), ("DA002", 4));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var ma = (await res.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["ma"];

        Assert.Equal(8, await Ton("DA001"));
        Assert.Equal(6, await Ton("DA002"));

        var don = await _c.GetFromJsonAsync<DonHangDto>($"/api/don-hang/{ma}");
        Assert.Equal("DaDat", don!.TrangThai);
        Assert.Equal(2 * 2000 + 4 * 500, don.TongTien);
        Assert.Equal(2, don.Dong.Count);
    }

    [Fact]
    public async Task DatHang_MotDongThieuHang_422_VaKhongDongNaoBiTru()      // tinh nguyen tu: tat ca hoac khong co gi
    {
        await TaoSp("DB001", 10);
        await TaoSp("DB002", 1);

        var res = await Dat("An", ("DB001", 5), ("DB002", 3));               // dong 2 thieu hang
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);

        Assert.Equal(10, await Ton("DB001"));                                 // dong 1 KHONG bi tru
        Assert.Equal(1, await Ton("DB002"));
    }

    [Fact]
    public async Task DatHang_SanPhamKhongTonTai_404_DuLieuSai_400()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Dat("An", ("ZZ999", 1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Dat("An")).StatusCode);                       // khong co dong
        Assert.Equal(HttpStatusCode.BadRequest, (await Dat("", ("ZZ999", 1))).StatusCode);           // khong khach
    }

    [Fact]
    public async Task HuyDon_TraLaiTon_VaKhongHuyDuocLanHai()
    {
        await TaoSp("DC001", 10);
        var ma = (await (await Dat("An", ("DC001", 4))).Content.ReadFromJsonAsync<Dictionary<string, string>>())!["ma"];
        Assert.Equal(6, await Ton("DC001"));

        var huy = await _c.PostAsJsonAsync($"/api/don-hang/{ma}/huy", new { lyDo = "khach doi y" });
        Assert.Equal(HttpStatusCode.NoContent, huy.StatusCode);
        Assert.Equal(10, await Ton("DC001"));                                                          // ton da tra
        Assert.Equal("DaHuy", (await _c.GetFromJsonAsync<DonHangDto>($"/api/don-hang/{ma}"))!.TrangThai);

        var lan2 = await _c.PostAsJsonAsync($"/api/don-hang/{ma}/huy", new { lyDo = "lai" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, lan2.StatusCode);
        Assert.Equal(10, await Ton("DC001"));                                                          // khong tra hai lan
    }

    [Fact]
    public async Task DatHangDongThoi_ChiDatDuocDungSoLuongCon_KhongBaoGioAm()
    {
        await TaoSp("DD001", 5);

        var kq = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Dat("An", ("DD001", 1))));
        int thanhCong = kq.Count(r => r.StatusCode == HttpStatusCode.Created);

        Assert.Equal(5, thanhCong);                                   // chi 5 don thanh cong; con lai bi tu choi (422 het hang / 409 xung dot)
        Assert.All(kq.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.True(r.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.Conflict));
        Assert.Equal(0, await Ton("DD001"));                          // ton = 0, khong bao gio am
    }

    [Fact]
    public async Task DatHang_CoIdempotencyKey_GuiLaiKhongTaoDonThuHai_VaSuKienDiQuaOutbox()
    {
        await TaoSp("DE001", 10);
        var body = new { khachHang = "Binh", dong = new[] { new { maSanPham = "DE001", soLuong = 3 } } };

        HttpRequestMessage Req() => new(HttpMethod.Post, "/api/don-hang") { Content = JsonContent.Create(body), Headers = { { "Idempotency-Key", "dat-DE001-binh" } } };
        var a = await _c.SendAsync(Req());
        var b = await _c.SendAsync(Req());                            // client gui lai vi nghi mat phan hoi

        Assert.Equal(HttpStatusCode.Created, a.StatusCode);
        Assert.Equal(HttpStatusCode.Created, b.StatusCode);
        Assert.Equal(await a.Content.ReadAsStringAsync(), await b.Content.ReadAsStringAsync());       // cung ma don
        Assert.Equal(7, await Ton("DE001"));                                                          // tru MOT lan

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KhoDbContext>();
        Assert.Equal(1, await db.DonHangs.CountAsync(d => d.KhachHang == "Binh"));
        Assert.Equal(1, await db.Outbox.CountAsync(o => o.Loai == "DonHangDaDat" && o.NoiDung.Contains("Binh")));

        await f.Services.GetRequiredService<OutboxProcessor>().XuLyMotLanAsync(CancellationToken.None);
        lock (f.PhatHanh.DaPhatHanh)
            Assert.Single(f.PhatHanh.DaPhatHanh, p => p.Loai == "DonHangDaDat" && p.NoiDung.Contains("Binh"));
    }
}
