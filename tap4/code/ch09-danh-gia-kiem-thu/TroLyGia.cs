using Microsoft.Extensions.AI;

namespace ChuongDanhGia;

// Tro ly "on dinh": tra loi DUNG HET cho bo cau hoi mau - dai dien cho phan LOGIC XUNG QUANH mo hinh (dung ung, khong phai chinh mo hinh)
public sealed class TroLyOnDinh : IChatClient
{
    private static readonly Dictionary<string, string> DapAn = new()
    {
        ["LT001 con bao nhieu?"] = "LT001 con 7 chiec trong kho.",
        ["Chinh sach doi tra the nao?"] = "San pham loi duoc doi tra trong vong 30 ngay ke tu ngay mua.",
        ["Muc canh bao ton kho mac dinh la bao nhieu?"] = "Muc canh bao ton kho mac dinh la 5 don vi.",
        ["Don hang toi da bao nhieu dong?"] = "Don hang toi da 20 dong.",
        ["San pham dien tu can bao hanh toi thieu bao lau?"] = "San pham dien tu can bao hanh toi thieu 12 thang.",
    };

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        string hoi = messages.Last(m => m.Role == ChatRole.User).Text;
        string tl = DapAn.TryGetValue(hoi, out var v) ? v : "Toi khong co thong tin nay.";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, tl)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}

// Tro ly "thuc te": dung 4/5, BIA MOT CAU (mo phong hallucination that su xay ra voi LLM that) - de minh hoa vi sao
// bai kiem thu phai KIEM TRA TI LE DAT (>= nguong), khong doi hoi 100% nhu ma tat dinh.
public sealed class TroLyThucTe : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        string hoi = messages.Last(m => m.Role == ChatRole.User).Text;
        string tl = hoi switch
        {
            "LT001 con bao nhieu?" => "LT001 con 7 chiec trong kho.",
            "Chinh sach doi tra the nao?" => "San pham loi duoc doi tra trong vong 30 ngay ke tu ngay mua.",
            "Muc canh bao ton kho mac dinh la bao nhieu?" => "Muc canh bao ton kho mac dinh la 5 don vi.",
            "Don hang toi da bao nhieu dong?" => "Don hang co the co toi da 50 dong tuy cau hinh he thong.",   // BIA - sai so voi chinh sach that (20 dong)
            "San pham dien tu can bao hanh toi thieu bao lau?" => "San pham dien tu can bao hanh toi thieu 12 thang.",
            _ => "Toi khong co thong tin nay.",
        };
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, tl)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}

// Tro ly "khong on dinh": voi CUNG mot cau hoi, doi khi dung doi khi sai - mo phong tinh KHONG XAC DINH thuc su cua LLM.
// Dung Random co SEED CO DINH de CHINH BAI TEST nay van tat dinh (chay lai luon ra cung ket qua) trong khi VAN the hien
// dac tinh "khong xac dinh" cua he thong duoc kiem thu.
public sealed class TroLyKhongOnDinh(int tiLeDungPhanTram, int seed) : IChatClient
{
    private readonly Random _rng = new(seed);

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        bool dung = _rng.Next(100) < tiLeDungPhanTram;
        string tl = dung ? "LT001 con 7 chiec trong kho." : "LT001 con khoang 10 chiec (uoc tinh).";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, tl)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}

// Giam khao gia: phan tich chuoi tra loi bang tu khoa de "cham diem" - dai dien cho mot mo hinh giam khao that
public sealed class GiamKhaoGia : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        string noiDung = messages.Last(m => m.Role == ChatRole.User).Text;
        string traLoi = noiDung[(noiDung.IndexOf("can cham:") + 9)..].Trim();
        bool coCanCu = noiDung.Contains("30 ngay") && traLoi.Contains("30 ngay") || noiDung.Contains("20 dong") && traLoi.Contains("20 dong");
        int diem = traLoi.Contains("Toi khong co thong tin") ? 2 : coCanCu ? 5 : traLoi.Contains("50 dong") || traLoi.Contains("uoc tinh") ? 1 : 4;
        string json = $$"""{"diem":{{diem}},"lyDo":"danh gia tu dong theo tu khoa","coCanCu":{{(coCanCu ? "true" : "false")}}}""";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
