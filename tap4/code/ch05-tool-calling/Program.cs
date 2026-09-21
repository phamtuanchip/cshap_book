using System.ComponentModel;
using Microsoft.Extensions.AI;

// Kho du lieu gia (thay cho CSDL / API kho that)
var kho = new KhoGia();

// ===== Bien phuong thuc C# thanh "cong cu" (AIFunction) cho mo hinh. Mo ta (Description) chinh la "tai lieu" mo hinh doc de biet khi nao dung. =====
var congCu = new List<AITool>
{
    AIFunctionFactory.Create(kho.TraTon, "tra_ton_kho"),
    AIFunctionFactory.Create(kho.TimSanPham, "tim_san_pham"),
    AIFunctionFactory.Create(kho.XuatKho, "xuat_kho"),
};

Console.WriteLine("=== Luoc do cong cu gui cho mo hinh (rut gon) ===");
foreach (AIFunction f in congCu.Cast<AIFunction>())
    Console.WriteLine($"- {f.Name}: {f.Description}\n    tham so: {f.JsonSchema.GetProperty("properties")}");

// ===== Mo hinh gia: doi ra yeu cau goi tool, doc ket qua, roi tra loi =====
IChatClient goc = new MoHinhGia();

// UseFunctionInvocation: middleware tu dong VONG LAP "mo hinh yeu cau tool -> ta chay -> gui ket qua -> mo hinh tiep tuc"
IChatClient client = new ChatClientBuilder(goc)
    .UseFunctionInvocation(configure: c =>
    {
        c.MaximumIterationsPerRequest = 5;              // CHONG VONG LAP VO HAN (mo hinh cu goi tool mai)
        c.AllowConcurrentInvocation = false;
    })
    .Build();

var opt = new ChatOptions { Tools = congCu, Temperature = 0 };

Console.WriteLine("\n=== Hoi 1: tra ton (chi doc) ===");
var r1 = await client.GetResponseAsync("San pham LT001 con bao nhieu?", opt);
Console.WriteLine($"Tra loi: {r1.Text}");

Console.WriteLine("\n=== Hoi 2: nhieu tool lien tiep ===");
var r2 = await client.GetResponseAsync("Tim laptop roi cho biet ton", opt);
Console.WriteLine($"Tra loi: {r2.Text}");

Console.WriteLine("\n=== Hoi 3: tool CO TAC DUNG PHU (xuat kho) - nguoi dung khong xac nhan ===");
var r3 = await client.GetResponseAsync("Xuat 2 cai LT001", opt);
Console.WriteLine($"Tra loi: {r3.Text}");

Console.WriteLine("\n=== Hoi 4: mo hinh truyen tham so SAI (so luong am) - tool tu bao ve ===");
var r4 = await client.GetResponseAsync("Xuat -5 cai LT001", opt);
Console.WriteLine($"Tra loi: {r4.Text}");

Console.WriteLine("\n=== Nhat ky cac lan chay tool (phia UNG DUNG) ===");
foreach (var l in kho.NhatKy) Console.WriteLine("  " + l);
Console.WriteLine($"Ton LT001 cuoi cung: {kho.Ton["LT001"]}");

// ========================= Cac cong cu =========================
class KhoGia
{
    public Dictionary<string, int> Ton { get; } = new() { ["LT001"] = 7, ["CH002"] = 40 };
    public List<string> NhatKy { get; } = [];

    [Description("Tra ve so luong ton kho hien tai cua mot san pham theo ma. Chi doc, khong thay doi du lieu.")]
    public string TraTon([Description("Ma san pham, dang 2 chu cai + 3 chu so, vi du LT001")] string ma)
    {
        NhatKy.Add($"tra_ton_kho(ma={ma})");
        return Ton.TryGetValue(ma.ToUpperInvariant(), out int t) ? $"{{\"ma\":\"{ma.ToUpperInvariant()}\",\"ton\":{t}}}" : "{\"loi\":\"khong tim thay san pham\"}";
    }

    [Description("Tim san pham theo tu khoa trong ten. Tra ve danh sach ma san pham phu hop. Chi doc.")]
    public string TimSanPham([Description("Tu khoa, vi du 'laptop'")] string tuKhoa)
    {
        NhatKy.Add($"tim_san_pham(tuKhoa={tuKhoa})");
        return tuKhoa.Contains("laptop", StringComparison.OrdinalIgnoreCase) ? "[\"LT001\"]" : "[]";
    }

    [Description("XUAT KHO: tru so luong ton cua san pham. CO TAC DUNG PHU, khong hoan tac duoc de dang.")]
    public string XuatKho([Description("Ma san pham")] string ma, [Description("So luong xuat, phai la so nguyen duong")] int soLuong)
    {
        NhatKy.Add($"xuat_kho(ma={ma}, soLuong={soLuong})");
        // LUON kiem tra lai dau vao trong cong cu: mo hinh co the truyen bat cu gi. Tra loi ro rang de mo hinh (va nguoi) hieu.
        if (soLuong <= 0) return "{\"loi\":\"so luong phai lon hon 0\"}";
        if (!Ton.TryGetValue(ma.ToUpperInvariant(), out int t)) return "{\"loi\":\"khong tim thay san pham\"}";
        if (soLuong > t) return $"{{\"loi\":\"khong du hang, con {t}\"}}";
        Ton[ma.ToUpperInvariant()] = t - soLuong;
        return $"{{\"ma\":\"{ma.ToUpperInvariant()}\",\"tonMoi\":{Ton[ma.ToUpperInvariant()]}}}";
    }
}

// ========================= Mo hinh gia =========================
// Dong vai LLM: quyet dinh dua tren cau hoi va tren KET QUA TOOL da co trong hoi thoai.
sealed class MoHinhGia : IChatClient
{
    private int _id;

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        var ds = messages.ToList();
        string hoi = ds.Last(m => m.Role == ChatRole.User).Text;
        var ketQuaTool = ds.SelectMany(m => m.Contents).OfType<FunctionResultContent>().ToList();

        // Chua co ket qua tool -> yeu cau goi tool
        if (ketQuaTool.Count == 0)
        {
            var goi = ChonTool(hoi);
            if (goi is not null) return Tra(goi);
        }
        // Da co ket qua nhung can them mot buoc (tim -> tra ton)
        if (hoi.Contains("Tim laptop") && ketQuaTool.Count == 1 && ketQuaTool[0].Result?.ToString()!.Contains("LT001") == true)
            return Tra(new FunctionCallContent($"c{++_id}", "tra_ton_kho", new Dictionary<string, object?> { ["ma"] = "LT001" }));

        string tong = string.Join(" | ", ketQuaTool.Select(k => k.Result?.ToString()));
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"(gia lap) Dua tren ket qua cong cu: {tong}")));
    }

    private FunctionCallContent? ChonTool(string hoi) => hoi switch
    {
        _ when hoi.StartsWith("San pham LT001") => new($"c{++_id}", "tra_ton_kho", new Dictionary<string, object?> { ["ma"] = "LT001" }),
        _ when hoi.StartsWith("Tim laptop") => new($"c{++_id}", "tim_san_pham", new Dictionary<string, object?> { ["tuKhoa"] = "laptop" }),
        _ when hoi.StartsWith("Xuat 2") => new($"c{++_id}", "xuat_kho", new Dictionary<string, object?> { ["ma"] = "LT001", ["soLuong"] = 2 }),
        _ when hoi.StartsWith("Xuat -5") => new($"c{++_id}", "xuat_kho", new Dictionary<string, object?> { ["ma"] = "LT001", ["soLuong"] = -5 }),
        _ => null,
    };

    private static Task<ChatResponse> Tra(FunctionCallContent goi)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [goi])));

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
