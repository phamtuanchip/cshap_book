using System.ComponentModel;
using Microsoft.Extensions.AI;

// ================= Muc tieu: vong lap AGENT = LLM tu quyet dinh MOT CHUOI hanh dong de dat muc tieu, =================
// =================            khong phai mot lan hoi-dap. Ung dung VAN kiem soat tung buoc thuc thi.  =================

var kho = new KhoGia();
var congCu = new List<AITool>
{
    AIFunctionFactory.Create(kho.TimSanPham, "tim_san_pham"),
    AIFunctionFactory.Create(kho.TraTon, "tra_ton_kho"),
    AIFunctionFactory.Create(kho.DeXuatNhapHang, "de_xuat_nhap_hang"),       // CHI DE XUAT: khong tu ghi du lieu (hanh dong that can NGUOI DUYET)
};

IChatClient client = new ChatClientBuilder(new MoHinhGiaAgent())
    .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = 6)
    .Build();

var agent = new AgentKho(client, congCu, toiDaBuoc: 6);

Console.WriteLine("=== Nhiem vu: 'Kiem tra cac san pham nhom dien tu va de xuat nhap them hang neu sap het' ===\n");
var ketQua = await agent.ChayAsync("Kiem tra ton kho nhom 'dien tu' va de xuat nhap them cho san pham nao sap het (ton <= 5).");

Console.WriteLine("\n--- Vet (trace) tung buoc suy luan - hanh dong ---");
foreach (var b in ketQua.CacBuoc) Console.WriteLine($"  [{b.Loai}] {b.NoiDung}");

Console.WriteLine($"\n--- Ket qua cuoi ({ketQua.SoBuoc} buoc, dung vi: {ketQua.LyDoDung}) ---");
Console.WriteLine(ketQua.TraLoiCuoi);

Console.WriteLine("\n=== Kich ban 2: nhiem vu khong bao gio 'xong' -> AGENT PHAI TU DUNG DUNG GIOI HAN BUOC ===");
var lap = new AgentKho(new ChatClientBuilder(new MoHinhLapVoHan()).UseFunctionInvocation().Build(), congCu, toiDaBuoc: 4);
var kqLap = await lap.ChayAsync("Nhiem vu se khong bao gio bao 'xong'");
Console.WriteLine($"Dung sau {kqLap.SoBuoc} buoc vi: {kqLap.LyDoDung}");

// ============================================================================================
// Agent: vong lap "quan sat -> suy nghi -> hanh dong" voi GIOI HAN BUOC va HANH DONG GHI CAN DUYET
// ============================================================================================
public sealed record BuocAgent(string Loai, string NoiDung);           // Loai: "SuyNghi" | "CongCu" | "DeXuat"
public sealed record KetQuaAgent(string TraLoiCuoi, int SoBuoc, string LyDoDung, IReadOnlyList<BuocAgent> CacBuoc);

public sealed class AgentKho(IChatClient client, IList<AITool> congCu, int toiDaBuoc)
{
    private const string HeThong = """
        Ban la agent kiem tra kho hang. Muc tieu: hoan thanh nhiem vu bang cach GOI CONG CU tung buoc.
        Quy tac:
        - Chi dung cong cu duoc cung cap. Khong bia du lieu ton kho.
        - "de_xuat_nhap_hang" CHI la de xuat cho nguoi duyet - khong phai hanh dong that.
        - Khi da co du thong tin de tra loi nhiem vu, hay tra loi bang van ban thuong (KHONG goi them cong cu).
        - Neu sau nhieu buoc van chua xong, hay tom tat nhung gi da lam duoc.
        """;

    public async Task<KetQuaAgent> ChayAsync(string nhiemVu, CancellationToken ct = default)
    {
        var lichSu = new List<ChatMessage> { new(ChatRole.System, HeThong), new(ChatRole.User, nhiemVu) };
        var buoc = new List<BuocAgent>();
        var opt = new ChatOptions { Tools = congCu, Temperature = 0 };

        for (int i = 1; i <= toiDaBuoc; i++)
        {
            var res = await client.GetResponseAsync(lichSu, opt, ct);
            lichSu.AddRange(res.Messages);

            var goiTool = res.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ToList();
            var ketQuaTool = res.Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().ToList();

            foreach (var g in goiTool) buoc.Add(new("CongCu", $"goi {g.Name}({string.Join(", ", g.Arguments?.Select(a => $"{a.Key}={a.Value}") ?? [])})"));
            foreach (var k in ketQuaTool.Where(k => k.Result?.ToString()?.Contains("de_xuat", StringComparison.OrdinalIgnoreCase) == true))
                buoc.Add(new("DeXuat", k.Result!.ToString()!));

            // KHONG con yeu cau goi tool nao trong luot nay -> agent coi la "xong", tra loi cuoi la van ban
            if (goiTool.Count == 0)
            {
                buoc.Add(new("SuyNghi", "khong con hanh dong nao can lam, ket luan"));
                return new(res.Text, i, "hoan thanh", buoc);
            }
        }

        // GIOI HAN BUOC la RAO CHAN BAT BUOC: mot agent bi ket (loop, hieu sai nhiem vu) khong duoc chay vo han (ton tien vo han)
        return new("(dung do vuot gioi han buoc) " + buoc.LastOrDefault(b => b.Loai == "DeXuat")?.NoiDung, toiDaBuoc, "vuot gioi han buoc toi da", buoc);
    }
}

// ================= Cong cu =================
class KhoGia
{
    private readonly Dictionary<string, (string Ten, string Nhom, int Ton)> _sp = new()
    {
        ["LT001"] = ("Laptop Dell XPS 13", "dien tu", 3),
        ["DT002"] = ("Dien thoai Pixel 9", "dien tu", 15),
        ["TV003"] = ("Tivi Samsung 55 inch", "dien tu", 2),
        ["BP001"] = ("Ban phim co Keychron", "phu-kien", 20),
    };

    [Description("Tim danh sach ma san pham theo nhom. Chi doc.")]
    public string TimSanPham([Description("Ten nhom, vi du 'dien tu'")] string nhom)
        => "[" + string.Join(",", _sp.Where(x => x.Value.Nhom == nhom).Select(x => $"\"{x.Key}\"")) + "]";

    [Description("Tra ve ton kho hien tai cua mot san pham theo ma. Chi doc.")]
    public string TraTon([Description("Ma san pham")] string ma)
        => _sp.TryGetValue(ma, out var s) ? $"{{\"ma\":\"{ma}\",\"ten\":\"{s.Ten}\",\"ton\":{s.Ton}}}" : "{\"loi\":\"khong tim thay\"}";

    [Description("DE XUAT nhap them hang cho mot san pham (CHUA thuc hien, chi tao de xuat cho nguoi duyet).")]
    public string DeXuatNhapHang([Description("Ma san pham")] string ma, [Description("So luong de xuat nhap")] int soLuongDeXuat)
        => $"{{\"de_xuat\":\"Nhap them {soLuongDeXuat} {ma}\",\"trangThai\":\"cho_duyet\"}}";
}

// ================= Mo hinh gia dong vai agent: 3 buoc co logic (tim -> tra ton tung ma -> de xuat khi can) =================
sealed class MoHinhGiaAgent : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        var ds = messages.ToList();
        var loiGoi = ds.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ToDictionary(c => c.CallId, c => c.Name);
        var ketQuaTool = ds.SelectMany(m => m.Contents).OfType<FunctionResultContent>().ToList();
        var daGoi = ds.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Select(c => c.Name).ToList();

        if (ketQuaTool.Count == 0)
            return Goi("tim_san_pham", new Dictionary<string, object?> { ["nhom"] = "dien tu" });

        var maSp = System.Text.Json.JsonDocument.Parse(ketQuaTool[0].Result!.ToString()!).RootElement.EnumerateArray().Select(x => x.GetString()!).ToList();
        int soLanTraTon = daGoi.Count(n => n == "tra_ton_kho");

        if (soLanTraTon < maSp.Count)
            return Goi("tra_ton_kho", new Dictionary<string, object?> { ["ma"] = maSp[soLanTraTon] });

        // Da tra ton tat ca -> tim san pham nao ton <= 5 trong CAC ket qua tra_ton_kho, de xuat nhap
        var canNhap = ketQuaTool.Where(k => loiGoi.GetValueOrDefault(k.CallId) == "tra_ton_kho")
            .Select(k => System.Text.Json.JsonDocument.Parse(k.Result!.ToString()!).RootElement)
            .Where(e => e.TryGetProperty("ton", out var t) && t.GetInt32() <= 5)
            .ToList();

        int soLanDeXuat = daGoi.Count(n => n == "de_xuat_nhap_hang");
        if (soLanDeXuat < canNhap.Count)
        {
            var sp = canNhap[soLanDeXuat];
            return Goi("de_xuat_nhap_hang", new Dictionary<string, object?> { ["ma"] = sp.GetProperty("ma").GetString(), ["soLuongDeXuat"] = 20 });
        }

        string tomTat = string.Join("; ", canNhap.Select(e => TomTatMot(e)));
        return Tra($"Da kiem tra {maSp.Count} san pham nhom dien tu. Da de xuat nhap hang cho: {tomTat}.");
    }

    private static string TomTatMot(System.Text.Json.JsonElement e) => $"{e.GetProperty("ma").GetString()} (ton {e.GetProperty("ton").GetInt32()})";

    private static Task<ChatResponse> Goi(string ten, Dictionary<string, object?> args) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent($"c{Guid.NewGuid():N}"[..8], ten, args)])));
    private static Task<ChatResponse> Tra(string s) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, s)));
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}

// Mo hinh "ket": luon goi lai cong cu, khong bao gio ket luan -> minh hoa vi sao PHAI co gioi han buoc phia UNG DUNG
sealed class MoHinhLapVoHan : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent($"c{Guid.NewGuid():N}"[..8], "tim_san_pham", new Dictionary<string, object?> { ["nhom"] = "dien tu" })])));

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
