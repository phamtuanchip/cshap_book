using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using WebRazor.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();                                   // Razor Pages: moi trang = 1 file .cshtml + 1 PageModel
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddDbContext<CuaHangDb>(o => o.UseSqlite(builder.Configuration.GetConnectionString("CuaHang") ?? "Data Source=razor.db"));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CuaHangDb>();
    await db.Database.EnsureCreatedAsync();                         // demo: tao bang theo mo hinh (thuc te dung migration)
    await CuaHangDb.NapMauAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Loi");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();                                                // URL suy ra tu duong dan file trong Pages/

app.Run();
