using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

// ===== Muc tieu: bien VAN BAN TU DO thanh DOI TUONG C# co kieu, roi KIEM CHUNG (khong tin dau ra cua LLM) =====

// Kich ban: nhan vien dan mot tin nhan hon don, LLM trich xuat thanh phieu nhap kho
var kichBan = new KichBanClient(
    // Lan 1: mo hinh tra JSON dung dinh dang
    """{"maSanPham":"LT001","soLuong":5,"lyDo":"Nhap them hang","khan":false}""",
    // Lan 2 (khi ta yeu cau sua): mo hinh tra JSON sai nghiep vu (so luong am)
    """{"maSanPham":"lt-001","soLuong":-3,"lyDo":"","khan":true}""",
    // Lan 3 (sau khi gui thong bao loi): mo hinh sua dung
    """{"maSanPham":"LT001","soLuong":3,"lyDo":"Nhap bu","khan":true}""");

IChatClient client = kichBan;

Console.WriteLine("=== 1. Tra ve doi tuong C# bang GetResponseAsync<T> ===");
var r = await client.GetResponseAsync<PhieuNhap>("Nhap them 5 cai LT001 vao kho nhe");
Console.WriteLine($"Phieu: {r.Result.MaSanPham} x{r.Result.SoLuong} ({r.Result.LyDo}), khan={r.Result.Khan}");
Console.WriteLine($"Luoc do JSON da gui kem: {kichBan.LuocDoDaNhan is not null}");
Console.WriteLine(kichBan.LuocDoDaNhan);

Console.WriteLine("\n=== 2. Kiem chung + vong lap SUA LOI (toi da 2 lan) ===");
var kq = await TrichXuat.PhieuNhapAsync(client, "Nhap lai 3 cai laptop, gap");
Console.WriteLine(kq.ThanhCong ? $"OK sau {kq.SoLanThu} lan: {kq.GiaTri}" : $"That bai: {kq.Loi}");

Console.WriteLine("\n=== 3. Neu mo hinh khong tra ve JSON hop le ===");
var hong = new KichBanClient("Xin loi, toi khong hieu yeu cau.", "van khong phai JSON", "{ cung hong");
var kq3 = await TrichXuat.PhieuNhapAsync(hong, "...");
Console.WriteLine(kq3.ThanhCong ? "OK" : $"That bai co kiem soat: {kq3.Loi}");

// =================== Mo hinh du lieu: mo ta (Description) di kem luoc do gui cho mo hinh ===================
[Description("Phieu nhap kho trich xuat tu tin nhan cua nhan vien")]
public record PhieuNhap(
    [property: Description("Ma san pham dang 2 chu cai + 3 chu so, viet hoa, vi du LT001")] string MaSanPham,
    [property: Description("So luong nhap, so nguyen duong")] int SoLuong,
    [property: Description("Ly do nhap kho, ngan gon")] string LyDo,
    [property: Description("true neu nhan vien noi la gap/khan cap")] bool Khan);

public sealed record KetQuaTrichXuat<T>(bool ThanhCong, T? GiaTri, string? Loi, int SoLanThu);

public static class TrichXuat
{
    // KHONG BAO GIO tin dau ra: (1) parse duoc? (2) hop le theo QUY TAC NGHIEP VU? Neu sai -> bao loi cho mo hinh de tu sua, co gioi han.
    public static async Task<KetQuaTrichXuat<PhieuNhap>> PhieuNhapAsync(IChatClient client, string tinNhan, int toiDaSua = 2)
    {
        var hoiThoai = new List<ChatMessage> { new(ChatRole.User, tinNhan) };
        string? loi = null;

        for (int lan = 1; lan <= toiDaSua + 1; lan++)
        {
            PhieuNhap? phieu = null;
            try
            {
                var res = await client.GetResponseAsync<PhieuNhap>(hoiThoai);
                phieu = res.Result;
                loi = KiemTra(phieu);
                if (loi is null) return new(true, phieu, null, lan);
                hoiThoai.AddRange(res.Messages);                                   // dua cau tra loi sai vao lich su de mo hinh thay no da tra gi
            }
            catch (JsonException e)
            {
                loi = "Khong phai JSON hop le: " + e.Message.Split('.')[0];
            }
            catch (InvalidOperationException e)
            {
                loi = "Phan hoi khong doc duoc: " + e.Message;
            }
            hoiThoai.Add(new(ChatRole.User, $"Ket qua truoc khong dung: {loi}. Hay tra lai JSON dung luoc do."));
        }
        return new(false, null, loi, toiDaSua + 1);
    }

    private static string? KiemTra(PhieuNhap p)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(p.MaSanPham, "^[A-Z]{2}[0-9]{3}$")) return $"MaSanPham '{p.MaSanPham}' sai dinh dang (can 2 chu cai + 3 chu so viet hoa)";
        if (p.SoLuong is <= 0 or > 100_000) return $"SoLuong {p.SoLuong} ngoai khoang 1..100000";
        if (string.IsNullOrWhiteSpace(p.LyDo)) return "LyDo khong duoc rong";
        return null;
    }
}

// =================== Nha cung cap gia co kich ban ===================
sealed class KichBanClient(params string[] traLoi) : IChatClient
{
    private int _i;
    public string? LuocDoDaNhan { get; private set; }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        LuocDoDaNhan ??= options?.ResponseFormat is ChatResponseFormatJson j ? j.Schema?.ToString() : null;
        string text = traLoi[Math.Min(_i++, traLoi.Length - 1)];
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
