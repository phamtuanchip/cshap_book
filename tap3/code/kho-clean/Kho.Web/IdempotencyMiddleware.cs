using System.Security.Cryptography;
using System.Text;
using Kho.Infrastructure.Idempotency;

namespace Kho.Web;

// POST co header "Idempotency-Key": xu ly DUNG MOT LAN, gui lai thi tra ket qua cu. Khong co header -> di thang (hanh vi cu).
public class IdempotencyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, IdempotencyStore store)
    {
        if (!HttpMethods.IsPost(ctx.Request.Method) || !ctx.Request.Headers.TryGetValue("Idempotency-Key", out var giaTri) || string.IsNullOrWhiteSpace(giaTri))
        {
            await next(ctx);
            return;
        }

        string khoa = giaTri.ToString();
        if (khoa.Length > 100) { await Loi(ctx, 400, "Idempotency-Key qua dai"); return; }

        ctx.Request.EnableBuffering();
        using var doc = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
        string body = await doc.ReadToEndAsync(ctx.RequestAborted);
        ctx.Request.Body.Position = 0;
        string dauVan = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{ctx.Request.Method} {ctx.Request.Path}\n{body}")));

        var kq = await store.ChiemAsync(khoa, dauVan, ctx.RequestAborted);
        switch (kq.TrangThai)
        {
            case TrangThaiChiem.KhacNoiDung: await Loi(ctx, 422, "Idempotency-Key da dung cho yeu cau khac"); return;
            case TrangThaiChiem.DangXuLy: await Loi(ctx, 409, "Yeu cau cung khoa dang duoc xu ly"); return;
            case TrangThaiChiem.DaXongTraLai:
                ctx.Response.StatusCode = kq.MaTrangThai;
                ctx.Response.Headers["Idempotency-Replayed"] = "true";
                if (kq.NoiDung is not null)
                {
                    ctx.Response.ContentType = kq.LoaiNoiDung;
                    await ctx.Response.WriteAsync(kq.NoiDung);
                }
                return;
        }

        // Chiem duoc -> xu ly that, "bat" response de luu lai
        var goc = ctx.Response.Body;
        using var dem = new MemoryStream();
        ctx.Response.Body = dem;
        try
        {
            await next(ctx);
        }
        catch
        {
            ctx.Response.Body = goc;
            await store.NhaAsync(khoa, CancellationToken.None);
            throw;
        }

        ctx.Response.Body = goc;
        if (ctx.Response.StatusCode >= 500)
        {
            await store.NhaAsync(khoa, CancellationToken.None);          // that bai tam thoi: cho phep thu lai
        }
        else
        {
            string? noiDung = dem.Length > 0 ? Encoding.UTF8.GetString(dem.ToArray()) : null;
            await store.LuuKetQuaAsync(khoa, ctx.Response.StatusCode, noiDung, ctx.Response.ContentType, CancellationToken.None);
        }
        dem.Position = 0;
        await dem.CopyToAsync(goc);
    }

    private static Task Loi(HttpContext ctx, int ma, string moTa)
    {
        ctx.Response.StatusCode = ma;
        return ctx.Response.WriteAsJsonAsync(new { status = ma, title = moTa });
    }
}
