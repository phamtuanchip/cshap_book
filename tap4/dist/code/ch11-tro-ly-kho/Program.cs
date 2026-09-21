using Kho.Application;
using Kho.Application.Abstractions;
using Kho.Application.Messaging;
using Kho.Application.SanPhams;
using Kho.Infrastructure;
using Kho.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TroLyKho;

// ================= DU AN TONG HOP: tro ly AI dung TREN CHINH he thong kho-clean that cua Tap 3 =================
// Khong viet lai nghiep vu: dang ky AddApplication()/AddInfrastructure() y het Kho.Web, chi them mot lop AI o tren.

string fileDb = Path.Combine(Path.GetTempPath(), $"tro-ly-kho-{Guid.NewGuid():N}.db");
var cfg = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Kho"] = $"Data Source={fileDb}",
        ["Outbox:Bat"] = "false",
    })
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Warning));
services.AddApplication();
services.AddInfrastructure(cfg);
await using var sp = services.BuildServiceProvider();

await using (var scope = sp.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<KhoDbContext>().Database.MigrateAsync();

// ---- Gieo du lieu THAT qua CHINH cac lenh nghiep vu (khong insert thang vao CSDL) ----
await using (var scope = sp.CreateAsyncScope())
{
    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
    foreach (var (ma, ten, nhom, gia, ton) in new[]
             {
                 ("LT001", "Laptop Dell XPS 13", "dien-tu", 25_000_000m, 3),
                 ("DT002", "Dien thoai Pixel 9", "dien-tu", 18_000_000m, 15),
                 ("BP001", "Ban phim co Keychron", "phu-kien", 1_500_000m, 40),
             })
        await sender.Send(new ThemSanPhamCommand(ma, ten, nhom, gia, ton));
}

Console.WriteLine($"Da khoi tao CSDL that (SQLite) tai: {fileDb}\n");

// ---- Lop AI: cong cu that + mo hinh gia (khong can khoa API) + gioi han vong lap (Chuong 5, 8) ----
await using var phienLamViec = sp.CreateAsyncScope();
var congCu = new CongCuKho(phienLamViec.ServiceProvider.GetRequiredService<ISender>());
IChatClient client = new ChatClientBuilder(new MoHinhGiaTroLyKho())
    .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = 5)
    .Build();
var opt = new ChatOptions { Tools = [.. congCu.ThanhDanhSachCongCu()], Temperature = 0 };

async Task Hoi(string cauHoi)
{
    Console.WriteLine($"Nguoi dung: {cauHoi}");
    var res = await client.GetResponseAsync(cauHoi, opt);
    Console.WriteLine($"Tro ly:     {res.Text}\n");
}

Console.WriteLine("=== 1. Truy van don gian (chi doc, du lieu THAT tu SQLite) ===");
await Hoi("LT001 con bao nhieu?");

Console.WriteLine("=== 2. Nhieu buoc: tim roi tra ton (nhu Chuong 5) ===");
await Hoi("Tim san pham dien tu roi cho biet san pham nao sap het hang");

Console.WriteLine("=== 3. Yeu cau xuat kho -> CHI DUOC DE XUAT, khong tu thuc hien ===");
await Hoi("Xuat 2 cai LT001 cho khach VIP");

// XAC MINH: du lieu THAT trong CSDL khong doi vi cong cu chi "de xuat"
await using (var scope = sp.CreateAsyncScope())
{
    var kq = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new LaySanPhamQuery("LT001"));
    Console.WriteLine($"=== Xac minh CSDL that: LT001 con {kq.GiaTri.TonKho} (KHONG doi vi de xuat chua duoc duyet) ===");
}

Console.WriteLine("\n=== 4. Yeu cau vuot qua so ton -> cong cu TU CHOI de xuat (kiem chung voi du lieu that) ===");
await Hoi("Xuat 999 cai LT001");

// =================== Mo hinh gia: quyet dinh tool dua tren tu khoa cau hoi (dai dien mo hinh that) ===================
sealed class MoHinhGiaTroLyKho : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        var ds = messages.ToList();
        string hoi = ds.Last(m => m.Role == ChatRole.User).Text;
        var ketQuaTool = ds.SelectMany(m => m.Contents).OfType<FunctionResultContent>().ToList();
        var loiGoi = ds.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ToDictionary(c => c.CallId, c => c.Name);

        if (ketQuaTool.Count == 0)
        {
            if (hoi.Contains("LT001 con")) return Goi("tra_ton_kho", new() { ["ma"] = "LT001" });
            if (hoi.Contains("Tim san pham dien tu")) return Goi("tim_san_pham", new() { ["tuKhoa"] = null, ["nhom"] = "dien-tu" });
            if (hoi.Contains("Xuat 2 cai LT001")) return Goi("de_xuat_xuat_kho", new() { ["ma"] = "LT001", ["soLuong"] = 2 });
            if (hoi.Contains("Xuat 999 cai LT001")) return Goi("de_xuat_xuat_kho", new() { ["ma"] = "LT001", ["soLuong"] = 999 });
        }

        // Da tim san pham dien tu -> tra ton tung ma de tim "sap het" (nhieu buoc, nhu Chuong 5/8)
        if (loiGoi.Values.Contains("tim_san_pham") && !loiGoi.Values.Contains("tra_ton_kho"))
        {
            var maDau = System.Text.Json.JsonDocument.Parse(ketQuaTool[0].Result!.ToString()!).RootElement[0].GetProperty("ma").GetString();
            return Goi("tra_ton_kho", new() { ["ma"] = maDau });
        }
        if (loiGoi.Count(kv => kv.Value == "tra_ton_kho") == 1 && hoi.Contains("Tim san pham dien tu"))
        {
            // gia lap: sau khi tra ton LT001 (dau tien), coi nhu du de tra loi (vi du don gian hoa)
            var kqLt = ketQuaTool.Last(k => loiGoi.GetValueOrDefault(k.CallId) == "tra_ton_kho");
            return Tra($"San pham dien tu sap het hang: {kqLt.Result}");
        }

        string tongHop = string.Join(" | ", ketQuaTool.Select(k => k.Result?.ToString()));
        return Tra(tongHop.Contains("loi") ? $"Khong the thuc hien: {tongHop}" : $"Da xu ly: {tongHop}");
    }

    private static Task<ChatResponse> Goi(string ten, Dictionary<string, object?> args) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent($"c{Guid.NewGuid():N}"[..8], ten, args)])));
    private static Task<ChatResponse> Tra(string s) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, s)));
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
