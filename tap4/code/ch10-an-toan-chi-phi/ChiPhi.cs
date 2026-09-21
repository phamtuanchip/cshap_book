using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ChuongAnToan;

// Gia vi du (USD / 1 trieu token) - CHI DE MINH HOA CACH TINH, khong phai gia that (tra bang gia cua nha cung cap ban dung).
public sealed record BangGia(decimal UsdMoiTrieuTokenVao, decimal UsdMoiTrieuTokenRa)
{
    public decimal Tinh(long vao, long ra) => vao / 1_000_000m * UsdMoiTrieuTokenVao + ra / 1_000_000m * UsdMoiTrieuTokenRa;
}

public sealed class VuotNganSachException(string message) : Exception(message);

// Theo doi chi phi THEO NGUOI DUNG, tich luy trong bo nho (thuc te: luu CSDL/cache phan tan de song sot qua nhieu instance - Tap 3 Chuong 10).
public sealed class BoTheoDoiChiPhi(BangGia gia)
{
    private readonly ConcurrentDictionary<string, decimal> _daDung = new();

    public decimal ChiPhiCuaNguoiDung(string nguoiDung) => _daDung.GetValueOrDefault(nguoiDung);

    public decimal GhiNhan(string nguoiDung, long tokenVao, long tokenRa)
    {
        decimal chiPhi = gia.Tinh(tokenVao, tokenRa);
        _daDung.AddOrUpdate(nguoiDung, chiPhi, (_, cu) => cu + chiPhi);
        return chiPhi;
    }
}

// Middleware CHAN request khi mot nguoi dung da vuot ngan sach - kiem tra TRUOC khi goi (khong the kiem tra sau, vi da ton tien).
// Ngan sach uoc luong theo DO DAI PROMPT (xap xi 4 ky tu/token) vi CHUA BIET truoc token thuc te se dung bao nhieu.
public sealed class GioiHanNganSachClient(IChatClient inner, BoTheoDoiChiPhi theoDoi, decimal tranUsdMoiNguoiDung, string nguoiDung, ILogger? log = null)
    : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        decimal daDung = theoDoi.ChiPhiCuaNguoiDung(nguoiDung);
        if (daDung >= tranUsdMoiNguoiDung)
        {
            log?.LogWarning("Tu choi request cua {NguoiDung}: da dung ${DaDung:F4}, tran ${Tran:F4}", nguoiDung, daDung, tranUsdMoiNguoiDung);
            throw new VuotNganSachException($"Nguoi dung {nguoiDung} da vuot ngan sach (${daDung:F4}/${tranUsdMoiNguoiDung:F4})");
        }

        var res = await base.GetResponseAsync(messages, options, ct);
        long vao = res.Usage?.InputTokenCount ?? 0, ra = res.Usage?.OutputTokenCount ?? 0;
        decimal chiPhiLanNay = theoDoi.GhiNhan(nguoiDung, vao, ra);
        log?.LogInformation("Request cua {NguoiDung}: {TokenVao} vao / {TokenRa} ra, ${ChiPhi:F5}, tong da dung ${Tong:F4}",
            nguoiDung, vao, ra, chiPhiLanNay, theoDoi.ChiPhiCuaNguoiDung(nguoiDung));
        return res;
    }
}
