using System.Diagnostics;

// 1. async/await co ban
var sw = Stopwatch.StartNew();
Console.WriteLine("=== 1. Tuan tu vs song song ===");
await TaiDuLieuAsync("A", 300);
await TaiDuLieuAsync("B", 300);
Console.WriteLine($"Tuan tu: {sw.ElapsedMilliseconds / 100 * 100} ms (~600)");

sw.Restart();
var tA = TaiDuLieuAsync("A", 300);   // bat dau ca hai truoc
var tB = TaiDuLieuAsync("B", 300);
await Task.WhenAll(tA, tB);           // roi doi ca hai
Console.WriteLine($"Song song: {sw.ElapsedMilliseconds / 100 * 100} ms (~300)");

// 2. Task<T>: co ket qua tra ve
Console.WriteLine("=== 2. Task<T> ===");
int kq = await TinhToanAsync(6);
Console.WriteLine($"6! = {kq}");

// 3. WhenAny: lay ket qua cua task xong dau tien
Console.WriteLine("=== 3. WhenAny ===");
var nhanh = TaiDuLieuAsync("nhanh", 100);
var cham = TaiDuLieuAsync("cham", 500);
var xongTruoc = await Task.WhenAny(nhanh, cham);
Console.WriteLine($"Xong truoc: {await xongTruoc}");
await cham;   // doi not task con lai

// 4. Xu ly loi
Console.WriteLine("=== 4. Loi ===");
try
{
    await LoiAsync();
}
catch (InvalidOperationException e)
{
    Console.WriteLine($"Bat loi async: {e.Message}");
}

// 5. Huy bang CancellationToken
Console.WriteLine("=== 5. Huy ===");
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
try
{
    await ViecDaiAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Viec dai bi huy (timeout 250 ms)");
}

// 6. IAsyncEnumerable: luong bat dong bo
Console.WriteLine("=== 6. await foreach ===");
await foreach (var so in SinhSoAsync(3))
    Console.WriteLine($"  nhan {so}");

// 7. Task.Run: day viec CPU nang sang thread pool
Console.WriteLine("=== 7. Task.Run ===");
long tong = await Task.Run(() =>
{
    long t = 0;
    for (int i = 1; i <= 1_000_000; i++) t += i;
    return t;
});
Console.WriteLine($"Tong 1..1.000.000 = {tong}");

static async Task<string> TaiDuLieuAsync(string ten, int ms)
{
    await Task.Delay(ms);    // NHUONG thread trong luc cho, khong block
    return ten;
}

static async Task<int> TinhToanAsync(int n)
{
    await Task.Yield();
    int giaiThua = 1;
    for (int i = 2; i <= n; i++) giaiThua *= i;
    return giaiThua;
}

static async Task LoiAsync()
{
    await Task.Delay(10);
    throw new InvalidOperationException("Loi trong ham async");
}

static async Task ViecDaiAsync(CancellationToken ct)
{
    for (int i = 1; i <= 10; i++)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
    }
}

static async IAsyncEnumerable<int> SinhSoAsync(int n)
{
    for (int i = 1; i <= n; i++)
    {
        await Task.Delay(50);
        yield return i;
    }
}
