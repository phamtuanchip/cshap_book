using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using WebMvc.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();          // MVC: controller + view (Razor)

// Mac dinh Razor ma hoa ky tu ngoai Latin co ban thanh &#x1ED9; (trinh duyet van hien dung).
// Cho phep xuat thang tieng Viet de HTML de doc:
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddSingleton<SanPhamStore>();       // kho trong bo nho (Chuong 14 da noi cach dung EF Core)

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Loi");             // trang loi than thien cho nguoi dung
    app.UseHsts();
}

app.UseStaticFiles();                                 // phuc vu wwwroot/css, js, anh
app.UseRouting();

// Route quy uoc: /{controller}/{action}/{id?}   vd /SanPham/Details/3
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
