// 1. try / catch / finally
try
{
    int so = int.Parse("abc");
    Console.WriteLine(so);
}
catch (FormatException e)
{
    Console.WriteLine($"[FormatException] {e.Message}");
}
finally
{
    Console.WriteLine("finally luon chay");
}

// 2. Nhieu catch: cu the truoc, tong quat sau
foreach (var input in new[] { "10", "0", "xyz" })
{
    try
    {
        Console.WriteLine($"100 / {input} = {100 / int.Parse(input)}");
    }
    catch (DivideByZeroException)
    {
        Console.WriteLine("Khong chia duoc cho 0");
    }
    catch (FormatException)
    {
        Console.WriteLine($"'{input}' khong phai so");
    }
    catch (Exception e)   // tong quat nhat: dat cuoi cung
    {
        Console.WriteLine($"Loi khong ro: {e.Message}");
    }
}

// 3. Custom exception + throw
try
{
    var kho = new Kho(5);
    kho.Xuat(3);
    kho.Xuat(10);
}
catch (KhongDuHangException e)
{
    Console.WriteLine($"[KhongDuHang] can {e.SoCan}, chi con {e.SoCon}");
}

// 4. Exception filter: chi bat khi dieu kien dung
try
{
    throw new HttpLoi(404, "Khong tim thay");
}
catch (HttpLoi e) when (e.MaLoi == 500)
{
    Console.WriteLine("Loi server");
}
catch (HttpLoi e) when (e.MaLoi is >= 400 and < 500)
{
    Console.WriteLine($"Loi client {e.MaLoi}: {e.Message}");
}

// 5. Nem lai: 'throw;' giu nguyen stack trace
try
{
    try
    {
        DocDuLieu();
    }
    catch (Exception)
    {
        Console.WriteLine("Ghi log roi nem lai");
        throw;   // KHONG viet 'throw e;' vi mat stack trace
    }
}
catch (InvalidOperationException e)
{
    Console.WriteLine($"Bat o ngoai: {e.Message}");
}

// 6. Boc exception ben trong (InnerException)
try
{
    try { _ = int.Parse("x"); }
    catch (FormatException ex) { throw new ApplicationException("Doc cau hinh that bai", ex); }
}
catch (ApplicationException e)
{
    Console.WriteLine($"{e.Message} <- {e.InnerException?.GetType().Name}");
}

// 7. using: tu dong giai phong tai nguyen (goi Dispose ke ca khi co loi)
try
{
    using var tn = new TaiNguyen("ket noi DB");
    Console.WriteLine("Dang dung tai nguyen");
    throw new Exception("loi giua chung");
}
catch (Exception e)
{
    Console.WriteLine($"Da xu ly: {e.Message}");
}

// 8. Kiem tra tham so dau vao
try
{
    ChaoHoi(null!);
}
catch (ArgumentNullException e)
{
    Console.WriteLine($"Tham so loi: {e.ParamName}");
}

static void DocDuLieu() => throw new InvalidOperationException("Du lieu hong");

static void ChaoHoi(string ten)
{
    ArgumentNullException.ThrowIfNull(ten);
    Console.WriteLine($"Chao {ten}");
}

class KhongDuHangException(int soCan, int soCon)
    : Exception($"Khong du hang: can {soCan}, con {soCon}")
{
    public int SoCan { get; } = soCan;
    public int SoCon { get; } = soCon;
}

class HttpLoi(int maLoi, string thongBao) : Exception(thongBao)
{
    public int MaLoi { get; } = maLoi;
}

class Kho(int tonKho)
{
    public void Xuat(int soLuong)
    {
        if (soLuong > tonKho) throw new KhongDuHangException(soLuong, tonKho);
        tonKho -= soLuong;
        Console.WriteLine($"Xuat {soLuong}, con {tonKho}");
    }
}

class TaiNguyen(string ten) : IDisposable
{
    public void Dispose() => Console.WriteLine($"Da giai phong: {ten}");
}
