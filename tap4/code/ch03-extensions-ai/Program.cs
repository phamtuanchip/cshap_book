using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

// ---- Nha cung cap GIA: cai dat IChatClient ma khong goi mang. Doi sang that = doi MOT dong (xem chuong). ----
IChatClient goc = new TroLyGia();

using var logs = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Debug));      // Debug: ten thao tac. Trace: CA NOI DUNG hoi thoai (can than du lieu nhay cam)

// ---- Pipeline middleware: cai them DAU TIEN la NGOAI CUNG. Thu tu: log > cache > dem token > nha cung cap ----
IChatClient client = new ChatClientBuilder(goc)
    .UseLogging(logs)                                   // log request/response (mac dinh chi Trace; can bat Trace de thay)
    .Use(inner => new CacheClient(inner))               // TU VIET: trung cache thi KHONG xuong dem token / goi that
    .Use(inner => new DemTokenClient(inner))            // TU VIET: chi dem cac loi goi THAT
    .Build();

var lichSu = new List<ChatMessage>
{
    new(ChatRole.System, "Ban la tro ly kho hang, tra loi ngan."),
    new(ChatRole.User, "LT001 con bao nhieu?"),
};

Console.WriteLine("--- Lan 1 ---");
var r1 = await client.GetResponseAsync(lichSu, new ChatOptions { Temperature = 0, MaxOutputTokens = 200 });
Console.WriteLine($"Tra loi: {r1.Text}");

Console.WriteLine("\n--- Lan 2 (cung cau hoi -> cache) ---");
var r2 = await client.GetResponseAsync(lichSu, new ChatOptions { Temperature = 0, MaxOutputTokens = 200 });
Console.WriteLine($"Tra loi: {r2.Text}");

Console.WriteLine("\n--- Streaming ---");
await foreach (var manh in client.GetStreamingResponseAsync([new(ChatRole.User, "chao")]))
    Console.Write(manh.Text);
Console.WriteLine();

Console.WriteLine($"\nTong token da dung (theo DemTokenClient): {DemTokenClient.TongVao} vao / {DemTokenClient.TongRa} ra; so lan goi that: {TroLyGia.SoLanGoi}");

// =============== Nha cung cap gia ===============
sealed class TroLyGia : IChatClient
{
    public static int SoLanGoi;

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        Interlocked.Increment(ref SoLanGoi);
        string hoi = messages.Last(m => m.Role == ChatRole.User).Text;
        var res = new ChatResponse(new ChatMessage(ChatRole.Assistant, $"(gia lap) Ban hoi: \"{hoi}\". LT001 con 7 chiec."))
        {
            Usage = new UsageDetails { InputTokenCount = 30, OutputTokenCount = 12, TotalTokenCount = 42 },
        };
        return Task.FromResult(res);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var manh in new[] { "Xin ", "chao", "!" })
        {
            await Task.Delay(20, ct);
            yield return new ChatResponseUpdate(ChatRole.Assistant, manh);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}

// =============== Middleware tu viet: DelegatingChatClient boc "inner" ===============
sealed class DemTokenClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public static int TongVao, TongRa;

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        var res = await base.GetResponseAsync(messages, options, ct);          // goi client ben trong
        TongVao += (int)(res.Usage?.InputTokenCount ?? 0);
        TongRa += (int)(res.Usage?.OutputTokenCount ?? 0);
        return res;
    }
}

sealed class CacheClient(IChatClient inner) : DelegatingChatClient(inner)
{
    private readonly Dictionary<string, ChatResponse> _cache = [];

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        // Chi cache khi ket qua on dinh (Temperature = 0); khoa gom TOAN BO hoi thoai + tham so
        if (options?.Temperature is not 0f) return await base.GetResponseAsync(messages, options, ct);

        string khoa = string.Join("|", messages.Select(m => $"{m.Role}:{m.Text}")) + $"|max={options.MaxOutputTokens}";
        if (_cache.TryGetValue(khoa, out var co)) { Console.WriteLine("   [cache] trung"); return co; }
        return _cache[khoa] = await base.GetResponseAsync(messages, options, ct);
    }
}
