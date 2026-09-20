using Kho.Application.Abstractions;
using Kho.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kho.Infrastructure.Outbox;

// Tien trinh nen: quet bang outbox, phat hanh tung thong diep, danh dau da xu ly.
// Dam bao "AT-LEAST-ONCE": co the phat hanh lap lai neu sap giua chung -> ben nhan phai IDEMPOTENT (Chuong 13).
public class OutboxProcessor(IServiceScopeFactory scopeFactory, TimeProvider dongHo, ILogger<OutboxProcessor> log) : BackgroundService
{
    public static readonly TimeSpan ChuKy = TimeSpan.FromMilliseconds(300);
    public const int ToiDaMoiLan = 20;
    public const int ToiDaSoLanThu = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(ChuKy);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await XuLyMotLanAsync(stoppingToken);
        }
        catch (OperationCanceledException) { /* tat ung dung */ }
    }

    // Tach ra public de test goi truc tiep, khong can cho timer
    public async Task<int> XuLyMotLanAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();          // DbContext la Scoped -> tu tao scope (Tap 2, Chuong 5)
        var db = scope.ServiceProvider.GetRequiredService<KhoDbContext>();
        var phatHanh = scope.ServiceProvider.GetRequiredService<IPhatHanhSuKien>();

        var cho = await db.Outbox.Where(o => o.XuLyLuc == null && o.SoLanThu < ToiDaSoLanThu)
            .OrderBy(o => o.TaoLuc).Take(ToiDaMoiLan).ToListAsync(ct);

        foreach (var m in cho)
        {
            try
            {
                await phatHanh.PhatHanhAsync(m.Loai, m.NoiDung, ct);
                m.XuLyLuc = dongHo.GetUtcNow();
            }
            catch (Exception e)
            {
                m.SoLanThu++;
                m.LoiCuoi = e.Message;
                log.LogWarning(e, "Phat hanh outbox {Id} that bai (lan {Lan})", m.Id, m.SoLanThu);
            }
        }
        if (cho.Count > 0) await db.SaveChangesAsync(ct);
        return cho.Count;
    }
}

// Cai dat mac dinh: ghi log. Thuc te: gui message bus (RabbitMQ/Azure Service Bus), webhook, email...
public class LogPhatHanhSuKien(ILogger<LogPhatHanhSuKien> log) : IPhatHanhSuKien
{
    public Task PhatHanhAsync(string loai, string noiDungJson, CancellationToken ct)
    {
        log.LogInformation("SU KIEN {Loai}: {NoiDung}", loai, noiDungJson);
        return Task.CompletedTask;
    }
}
