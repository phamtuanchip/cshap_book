var tk = new TaiKhoan("001", "Tuan", 100m);
tk.NapTien(50m);
Console.WriteLine($"So du: {tk.SoDu}");

Console.WriteLine($"Rut 500: {tk.RutTien(500m)}");   // false: khong du tien
Console.WriteLine($"Rut 30:  {tk.RutTien(30m)}");    // true
Console.WriteLine($"So du: {tk.SoDu}, con tien: {tk.ConTien}");
Console.WriteLine($"Lich su: {string.Join(", ", tk.LichSu)}");

// tk.SoDu = 1_000_000;   // LOI BIEN DICH: setter la private
// tk._soDu = 1_000_000;  // LOI BIEN DICH: field la private

try
{
    tk.NapTien(-5m);
}
catch (ArgumentOutOfRangeException e)
{
    Console.WriteLine($"Loi: {e.Message}");
}

// Doi tuong bat bien
var p1 = new DiemDat { X = 1, Y = 2 };
var p2 = p1.DichChuyen(3, 3);
// p1.X = 9;   // LOI BIEN DICH: init-only
Console.WriteLine($"p1 = {p1}, p2 = {p2}");
