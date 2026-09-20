var builder = WebApplication.CreateBuilder(args);

// ----- Dang ky dich vu voi 3 thoi gian song -----
builder.Services.AddTransient<IThoiGianTransient, ThoiGianDich>();   // moi lan yeu cau tu container: MOT doi tuong moi
builder.Services.AddScoped<IThoiGianScoped, ThoiGianDich>();         // mot doi tuong MOI cho MOI HTTP request
builder.Services.AddSingleton<IThoiGianSingleton, ThoiGianDich>();   // duy nhat mot doi tuong cho ca ung dung

// Dich vu nghiep vu dung cac phu thuoc tren (DI tu tiem qua constructor)
builder.Services.AddScoped<DichVuA>();
builder.Services.AddScoped<DichVuB>();

// Dang ky theo interface -> cai dat: doi cai dat khong can sua noi dung
builder.Services.AddScoped<IGuiThongBao, GuiEmail>();

// Nhieu cai dat cho cung interface: yeu cau IEnumerable<T> de nhan tat ca
builder.Services.AddSingleton<IQuyTacGiamGia, GiamCuoiTuan>();
builder.Services.AddSingleton<IQuyTacGiamGia, GiamDonLon>();

// Keyed services (.NET 8+): chon cai dat theo khoa
builder.Services.AddKeyedSingleton<IGuiThongBao, GuiEmail>("email");
builder.Services.AddKeyedSingleton<IGuiThongBao, GuiSms>("sms");

// Factory: tu quyet dinh cach tao
builder.Services.AddSingleton<Func<string, IGuiThongBao>>(sp => kenh => kenh switch
{
    "email" => sp.GetRequiredKeyedService<IGuiThongBao>("email"),
    "sms" => sp.GetRequiredKeyedService<IGuiThongBao>("sms"),
    _ => throw new ArgumentException($"Khong ho tro kenh {kenh}"),
});

var app = builder.Build();

// ----- Endpoint: dich vu duoc tiem thang vao tham so -----
app.MapGet("/thoi-gian", (
    IThoiGianTransient t1, IThoiGianTransient t2,
    IThoiGianScoped s1, IThoiGianScoped s2,
    IThoiGianSingleton g1, IThoiGianSingleton g2,
    DichVuA a, DichVuB b) => new
{
    Transient = new { lan1 = t1.Id, lan2 = t2.Id, giongNhau = t1.Id == t2.Id },
    Scoped = new { lan1 = s1.Id, lan2 = s2.Id, giongNhau = s1.Id == s2.Id, dungChungVoiDichVu = a.IdScoped == b.IdScoped && a.IdScoped == s1.Id },
    Singleton = new { lan1 = g1.Id, lan2 = g2.Id, giongNhau = g1.Id == g2.Id },
});

app.MapGet("/giam-gia", (decimal tien, DateTime? ngay, IEnumerable<IQuyTacGiamGia> quyTac) =>
{
    var ngayTinh = ngay ?? DateTime.Today;
    var ketQua = quyTac.Select(q => new { q.Ten, giam = q.Giam(tien, ngayTinh) }).ToList();
    return new { tien, cacQuyTac = ketQua, tongGiam = ketQua.Sum(x => x.giam) };
});

app.MapGet("/gui", (string kenh, string noiDung, Func<string, IGuiThongBao> chon) =>
{
    var dich = chon(kenh);
    return dich.Gui(noiDung);
});

app.MapGet("/gui-mac-dinh", (IGuiThongBao gui) => gui.Gui("xin chao"));

app.Run();

// -------- Dich vu --------
interface IThoiGianTransient { Guid Id { get; } }
interface IThoiGianScoped { Guid Id { get; } }
interface IThoiGianSingleton { Guid Id { get; } }

// Mot lop, ba interface: moi dang ky tao doi tuong rieng theo thoi gian song cua no
class ThoiGianDich : IThoiGianTransient, IThoiGianScoped, IThoiGianSingleton
{
    public Guid Id { get; } = Guid.NewGuid();
}

class DichVuA(IThoiGianScoped scoped) { public Guid IdScoped => scoped.Id; }
class DichVuB(IThoiGianScoped scoped) { public Guid IdScoped => scoped.Id; }

interface IGuiThongBao { string Gui(string noiDung); }
class GuiEmail : IGuiThongBao { public string Gui(string noiDung) => $"[EMAIL] {noiDung}"; }
class GuiSms : IGuiThongBao { public string Gui(string noiDung) => $"[SMS] {noiDung}"; }

interface IQuyTacGiamGia
{
    string Ten { get; }
    decimal Giam(decimal tien, DateTime ngay);
}

class GiamCuoiTuan : IQuyTacGiamGia
{
    public string Ten => "Cuoi tuan -5%";
    public decimal Giam(decimal tien, DateTime ngay) => ngay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? tien * 0.05m : 0;
}

class GiamDonLon : IQuyTacGiamGia
{
    public string Ten => "Don tren 1 trieu -10%";
    public decimal Giam(decimal tien, DateTime ngay) => tien >= 1_000_000 ? tien * 0.10m : 0;
}
