using Kho.Application;
using Kho.Infrastructure;
using Kho.Infrastructure.Persistence;
using Kho.Web.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== COMPOSITION ROOT: noi duy nhat biet moi tang va noi chung lai =====
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Configuration.GetValue("MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<KhoDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();          // loi khong luong truoc -> 500 ProblemDetails (khong lo chi tiet)
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapHealthChecks("/health");
app.MapKho();

app.Run();

public partial class Program;
