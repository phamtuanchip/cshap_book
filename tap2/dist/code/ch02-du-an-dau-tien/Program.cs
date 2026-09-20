// Chuong trinh ASP.NET Core nho nhat co the: ~10 dong.
var builder = WebApplication.CreateBuilder(args);   // 1. Cau hinh: DI, cau hinh, logging
var app = builder.Build();                            // 2. Dung ung dung

app.MapGet("/", () => "Xin chao ASP.NET Core!");                              // tra ve chuoi -> text/plain
app.MapGet("/gio", () => new { GioHienTai = DateTime.Now, May = Environment.MachineName });   // doi tuong -> JSON
app.MapGet("/chao/{ten}", (string ten) => $"Xin chao, {ten}!");               // tham so tu duong dan
app.MapGet("/cong", (int a, int b) => new { a, b, tong = a + b });            // tham so tu query: /cong?a=1&b=2

app.MapGet("/moi-truong", (IWebHostEnvironment env) => new
{
    env.EnvironmentName,
    env.ApplicationName,
    Cwd = Environment.CurrentDirectory,
});

app.Run();                                            // 3. Chay may chu Kestrel, chan cho toi khi tat
