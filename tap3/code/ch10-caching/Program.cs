using System.Collections.Concurrent;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddHybridCache(o =>                                   // HybridCache: L1 (bo nho) + L2 (Redis...) + CHONG STAMPEDE
{
    o.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(30),                         // thoi gian song tren L2 / tong the
        LocalCacheExpiration = TimeSpan.FromSeconds(10),               // thoi gian song tren L1 (bo nho cuc bo)
    };
});
builder.Services.AddOutputCache(o =>
{
    o.AddPolicy("BaoCao", p => p.Expire(TimeSpan.FromSeconds(15)).Tag("bao-cao"));
});
builder.Services.AddSingleton<NguonDuLieuCham>();

var app = builder.Build();
app.UseOutputCache();

// ---- 1. Khong cache: moi lan deu "cham" (dem so lan chay that) ----
app.MapGet("/khong-cache/{id:int}", async (int id, NguonDuLieuCham nguon) => await nguon.LayAsync(id));

// ---- 2. IMemoryCache: thu cong (cache-aside), de vuong loi "stampede" ----
app.MapGet("/memory/{id:int}", async (int id, IMemoryCache cache, NguonDuLieuCham nguon) =>
{
    if (cache.TryGetValue($"sp:{id}", out SanPhamCache? co)) return co!;                // co trong cache
    var moi = await nguon.LayAsync(id);                                                   // khong co: di lay (nhieu request CUNG LUC cung di lay!)
    cache.Set($"sp:{id}", moi, TimeSpan.FromSeconds(30));
    return moi;
});

// ---- 3. HybridCache: cache-aside + hop nhat cac request dong thoi (khong stampede) ----
app.MapGet("/hybrid/{id:int}", async (int id, HybridCache cache, NguonDuLieuCham nguon, CancellationToken ct) =>
    await cache.GetOrCreateAsync(
        $"sp:{id}",
        async token => await nguon.LayAsync(id),                          // chi CHAY MOT LAN du co 100 request cung luc
        tags: ["san-pham", $"sp:{id}"],                                    // gan the de vo hieu hoa theo nhom
        cancellationToken: ct));

app.MapPost("/hybrid/{id:int}/cap-nhat", async (int id, HybridCache cache, NguonDuLieuCham nguon) =>
{
    nguon.Sua(id);                                                        // ghi vao nguon that
    await cache.RemoveByTagAsync($"sp:{id}");                             // VO HIEU HOA cache lien quan ngay lap tuc
    return Results.NoContent();
});

// ---- 4. Output caching: cache CA RESPONSE HTTP theo chinh sach, vo hieu theo the ----
app.MapGet("/bao-cao", (NguonDuLieuCham nguon) => new { taoLuc = DateTime.UtcNow.ToString("HH:mm:ss.fff"), soLanChayThat = nguon.TangBaoCao() })
   .CacheOutput("BaoCao");

app.MapPost("/bao-cao/lam-moi", async (IOutputCacheStore store, CancellationToken ct) =>
{
    await store.EvictByTagAsync("bao-cao", ct);
    return Results.NoContent();
});

app.MapGet("/thong-ke", (NguonDuLieuCham nguon) => new { soLanGoiNguon = nguon.SoLanGoi, soLanBaoCao = nguon.SoLanBaoCao });
app.MapPost("/dat-lai", (NguonDuLieuCham nguon) => { nguon.DatLai(); return Results.NoContent(); });

app.Run();

public record SanPhamCache(int Id, string Ten, decimal Gia, int PhienBan);

// Nguon du lieu "cham" (gia lap CSDL/API): dem so lan bi goi de THAY hieu qua cua cache
public class NguonDuLieuCham
{
    private readonly ConcurrentDictionary<int, int> _phienBan = new();
    private int _soLanGoi, _baoCao;

    public int SoLanGoi => Volatile.Read(ref _soLanGoi);
    public int SoLanBaoCao => Volatile.Read(ref _baoCao);
    public int TangBaoCao() => Interlocked.Increment(ref _baoCao);

    public async Task<SanPhamCache> LayAsync(int id)
    {
        Interlocked.Increment(ref _soLanGoi);
        await Task.Delay(300);                                            // 300 ms nhu mot truy van CSDL cham
        int pb = _phienBan.GetValueOrDefault(id, 1);
        return new SanPhamCache(id, $"San pham {id}", 100_000 * id, pb);
    }

    public void Sua(int id) => _phienBan.AddOrUpdate(id, 2, (_, cu) => cu + 1);

    public void DatLai() { Volatile.Write(ref _soLanGoi, 0); Volatile.Write(ref _baoCao, 0); _phienBan.Clear(); }
}
