using Kho.Application.Abstractions;
using Kho.Infrastructure.Outbox;
using Kho.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kho.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<KhoDbContext>(o => o.UseSqlite(cfg.GetConnectionString("Kho") ?? "Data Source=kho.db"));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<KhoDbContext>());      // cung MOT DbContext cho repository va UoW
        services.AddScoped<ISanPhamRepository, SanPhamRepository>();
        services.AddScoped<IDonHangRepository, DonHangRepository>();
        services.AddScoped<IKhoDocDuLieu, KhoDocDuLieu>();
        services.AddSingleton<IPhatHanhSuKien, LogPhatHanhSuKien>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<Kho.Infrastructure.Idempotency.IdempotencyStore>();

        if (cfg.GetValue("Outbox:Bat", true))
        {
            services.AddSingleton<OutboxProcessor>();
            services.AddHostedService(sp => sp.GetRequiredService<OutboxProcessor>());
        }
        return services;
    }
}
