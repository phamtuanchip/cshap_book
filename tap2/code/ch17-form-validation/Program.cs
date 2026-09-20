using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.WebEncoders;
using WebForm.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddSingleton<NguoiDungStore>();

// Chong CSRF cho ca Minimal API (Razor Pages/MVC da co san khi dung tag helper form)
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

// Gioi han kich thuoc body cho MOI request (chan tai file khong lo)
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 5 * 1024 * 1024);

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();          // PHAI dat sau UseRouting va (neu co) sau UseAuthentication/UseAuthorization

app.MapRazorPages();

// ---- Minimal API nhan form + upload file: bat buoc antiforgery ----
// Lay token (dung cho JS/SPA). Token cookie + token request phai khop.
app.MapGet("/api/csrf", (IAntiforgery af, HttpContext ctx) =>
{
    var tokens = af.GetAndStoreTokens(ctx);       // ghi cookie, tra ve token trong body
    return new { header = "X-CSRF-TOKEN", token = tokens.RequestToken };
});

app.MapPost("/api/tai-len", async ([FromForm] IFormFile? tep, [FromForm] string? ghiChu, IWebHostEnvironment env, CancellationToken ct) =>
{
    if (tep is null || tep.Length == 0) return Results.BadRequest("Chua chon tep");
    if (tep.Length > 1_000_000) return Results.BadRequest("Tep toi da 1 MB");

    // KHONG tin ten tep va Content-Type do client gui
    string duoi = Path.GetExtension(tep.FileName).ToLowerInvariant();
    if (duoi is not (".png" or ".jpg" or ".jpeg")) return Results.BadRequest("Chi nhan .png, .jpg");
    if (!KiemTraDauTep.LaAnh(tep)) return Results.BadRequest("Noi dung tep khong phai anh");

    string thuMuc = Path.Combine(Path.GetTempPath(), "cshap_book_uploads");
    Directory.CreateDirectory(thuMuc);
    string tenLuu = $"{Guid.NewGuid():N}{duoi}";                  // ten MOI do server dat: chong path traversal / ghi de
    await using (var fs = File.Create(Path.Combine(thuMuc, tenLuu)))
        await tep.CopyToAsync(fs, ct);

    return Results.Ok(new { tenGoc = Path.GetFileName(tep.FileName), tenLuu, kichThuoc = tep.Length, ghiChu });
}).WithName("TaiLen");     // (Minimal API cung co the tat: .DisableAntiforgery() - chi khi dung xac thuc bang token, khong bang cookie)

app.Run();

public partial class Program;
