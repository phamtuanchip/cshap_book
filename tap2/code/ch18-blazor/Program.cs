using WebBlazor.Components;
using WebBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor Web App: component Razor, chay tren SERVER, cap nhat giao dien qua ket noi SignalR (WebSocket)
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<SanPhamService>();                // dich vu nghiep vu dung chung (tiem vao component bang @inject)

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()                                    // App.razor la component goc
   .AddInteractiveServerRenderMode();

app.Run();
