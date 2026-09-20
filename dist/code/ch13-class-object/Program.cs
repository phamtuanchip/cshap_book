// Tao doi tuong tu class
var h1 = new HinhChuNhat(3, 4);
var h2 = new HinhChuNhat();
Console.WriteLine($"{h1}: dien tich {h1.DienTich()}, chu vi {h1.ChuVi()}");
Console.WriteLine($"{h2}: dien tich {h2.DienTich()}");

h1.PhongTo(2);
Console.WriteLine($"Sau khi phong to: {h1}");

// Object initializer
var h3 = new HinhChuNhat { Rong = 5, Cao = 2 };
Console.WriteLine(h3);

// Class la kieu THAM CHIEU: gan la sao chep dia chi
var a = new SinhVien("An", 8);
var b = a;
b.DoiTen("Binh");
Console.WriteLine($"a = {a}, b = {b}   (cung mot doi tuong)");

var c = new SinhVien("Chi", 9);
Console.WriteLine($"c = {c}");
Console.WriteLine($"Tong so sinh vien: {SinhVien.TongSoSinhVien}");

// null va tham chieu
SinhVien? khongCo = null;
Console.WriteLine(khongCo?.Ten ?? "(chua co sinh vien)");

// So sanh tham chieu
var d1 = new SinhVien("Dung", 7);
var d2 = new SinhVien("Dung", 7);
Console.WriteLine($"d1 == d2 ? {d1 == d2}");        // false: hai doi tuong khac nhau
Console.WriteLine($"d1 == d1 ? {d1 == d1}");        // true
