using System.Collections.Concurrent;
using System.Threading.Channels;

// 1. Thread co ban
Console.WriteLine("=== 1. Thread ===");
var t1 = new Thread(() => Console.WriteLine($"  thread phu, id={Environment.CurrentManagedThreadId}"));
t1.Start();
t1.Join();   // doi thread ket thuc
Console.WriteLine($"  thread chinh, id={Environment.CurrentManagedThreadId}");

// 2. Race condition: nhieu thread cung sua bien
Console.WriteLine("=== 2. Race condition ===");
int khongAnToan = 0;
Parallel.For(0, 100_000, _ => khongAnToan++);
Console.WriteLine($"  khong dong bo: {khongAnToan} (mong doi 100000, thuong nho hon)");

// 3. lock
int coLock = 0;
object khoa = new();
Parallel.For(0, 100_000, _ => { lock (khoa) { coLock++; } });
Console.WriteLine($"  lock:          {coLock}");

// 4. Interlocked: phep nguyen tu, nhanh hon lock
int nguyenTu = 0;
Parallel.For(0, 100_000, _ => Interlocked.Increment(ref nguyenTu));
Console.WriteLine($"  Interlocked:   {nguyenTu}");

// 5. Collection an toan da luong
Console.WriteLine("=== 3. ConcurrentDictionary ===");
var dem = new ConcurrentDictionary<string, int>();
Parallel.ForEach(new[] { "a", "b", "a", "c", "a", "b" }, tu =>
    dem.AddOrUpdate(tu, 1, (_, cu) => cu + 1));
Console.WriteLine("  " + string.Join(", ", dem.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")));

// 6. Parallel.For chia viec tren nhieu core
Console.WriteLine("=== 4. Parallel ===");
long tongChan = 0;
Parallel.For(1, 1_000_001, () => 0L, (i, _, cucBo) => i % 2 == 0 ? cucBo + i : cucBo,
    cucBo => Interlocked.Add(ref tongChan, cucBo));
Console.WriteLine($"  Tong cac so chan <= 1.000.000: {tongChan}");

// 7. SemaphoreSlim: gioi han so luong dong thoi
Console.WriteLine("=== 5. SemaphoreSlim ===");
var cho = new SemaphoreSlim(2);   // toi da 2 viec cung luc
int dangChay = 0, toiDa = 0;
var viec = Enumerable.Range(1, 6).Select(async i =>
{
    await cho.WaitAsync();
    try
    {
        int n = Interlocked.Increment(ref dangChay);
        InterlockedMax(ref toiDa, n);
        await Task.Delay(50);
        Interlocked.Decrement(ref dangChay);
    }
    finally { cho.Release(); }
});
await Task.WhenAll(viec);
Console.WriteLine($"  So viec chay dong thoi toi da: {toiDa} (gioi han 2)");

// 8. Channel: producer / consumer
Console.WriteLine("=== 6. Channel ===");
var kenh = Channel.CreateBounded<int>(3);
var nguoiSanXuat = Task.Run(async () =>
{
    for (int i = 1; i <= 5; i++)
    {
        await kenh.Writer.WriteAsync(i);
        Console.WriteLine($"  san xuat {i}");
    }
    kenh.Writer.Complete();
});
var nguoiTieuThu = Task.Run(async () =>
{
    await foreach (var x in kenh.Reader.ReadAllAsync())
        Console.WriteLine($"  tieu thu {x}");
});
await Task.WhenAll(nguoiSanXuat, nguoiTieuThu);

static void InterlockedMax(ref int muc, int giaTri)
{
    int cu;
    do { cu = muc; }
    while (giaTri > cu && Interlocked.CompareExchange(ref muc, giaTri, cu) != cu);
}
