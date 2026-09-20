using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 1. Middleware inline: app.Use(...). Thuc hien "truoc" va "sau" next().
app.Use(async (ctx, next) =>
{
    app.Logger.LogInformation("[1] vao  {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
    await next(ctx);
    app.Logger.LogInformation("[1] ra   -> {Status}", ctx.Response.StatusCode);
});

// 2. Middleware dang class (tai su dung, nhan phu thuoc qua DI)
app.UseMiddleware<DoThoiGianMiddleware>();

// 3. Middleware NGAN MACH: khong goi next -> yeu cau dung o day
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/bi-chan"))
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsync("Bi chan boi middleware (khong den duoc endpoint).");
        return;   // khong await next()
    }
    await next(ctx);
});

// 4. Middleware chi ap dung cho mot nhanh
app.MapWhen(ctx => ctx.Request.Query.ContainsKey("debug"), nhanh =>
{
    nhanh.Run(async ctx => await ctx.Response.WriteAsync("Che do debug (MapWhen): nhanh rieng, ket thuc tai day."));
});

// 5. Endpoint bang routing (cung la mot phan cua pipeline)
app.MapGet("/", () => "Trang chu");
app.MapGet("/cham", async () =>
{
    await Task.Delay(120);
    return "Da xu ly xong (co do thoi gian)";
});
app.MapGet("/loi", () => { throw new InvalidOperationException("Loi co y"); });
app.MapGet("/bi-chan/bat-ky", () => "khong bao gio thay dong nay");

// 6. Fallback: chay khi khong endpoint nao khop.
// LUU Y: KHONG dung app.Run(async ctx => ...) o day. Middleware "terminal" do se chan moi request
// truoc khi toi cac endpoint (endpoint routing chay o CUOI pipeline).
app.MapFallback(async ctx =>
{
    ctx.Response.StatusCode = 404;
    await ctx.Response.WriteAsync($"Khong tim thay {ctx.Request.Path} (MapFallback)");
});

app.Run();

// Middleware dang class: co constructor nhan RequestDelegate, va phuong thuc InvokeAsync
class DoThoiGianMiddleware(RequestDelegate next, ILogger<DoThoiGianMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var dong = Stopwatch.StartNew();

        // Header phai duoc them TRUOC khi response bat dau gui
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers["X-Thoi-Gian-Ms"] = dong.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        try
        {
            await next(ctx);
        }
        catch (Exception e)
        {
            log.LogError(e, "Loi khong xu ly tai {Path}", ctx.Request.Path);
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsync("Da co loi xay ra (middleware bat loi).");
        }
    }
}
