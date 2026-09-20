using System.Text;

// Lam viec trong thu muc tam de khong lam ban may
string thuMuc = Path.Combine(Path.GetTempPath(), "cshap_book_ch35");
if (Directory.Exists(thuMuc)) Directory.Delete(thuMuc, recursive: true);
Directory.CreateDirectory(thuMuc);
Console.WriteLine($"Thu muc lam viec: {thuMuc}");

// 1. Ghi / doc ca file (file nho)
string duongDan = Path.Combine(thuMuc, "ghichu.txt");
File.WriteAllText(duongDan, "Dong 1\nDong 2\nDong 3\n", Encoding.UTF8);
string noiDung = File.ReadAllText(duongDan);
Console.WriteLine($"Doc ca file:\n{noiDung}");

// 2. Ghi them, doc theo dong
File.AppendAllText(duongDan, "Dong 4 (them sau)\n");
string[] dong = File.ReadAllLines(duongDan);
Console.WriteLine($"So dong: {dong.Length}, dong cuoi: {dong[^1]}");

// 3. ReadLines: doc LAZY tung dong (file lon)
int dem = 0;
foreach (var d in File.ReadLines(duongDan))
    if (d.Contains("Dong")) dem++;
Console.WriteLine($"Dong chua chu 'Dong': {dem}");

// 4. StreamWriter / StreamReader: kiem soat chi tiet, using dong file
string log = Path.Combine(thuMuc, "log.txt");
using (var ghi = new StreamWriter(log, append: false, Encoding.UTF8))
{
    ghi.WriteLine("Bat dau");
    for (int i = 1; i <= 3; i++) ghi.WriteLine($"Buoc {i}");
    ghi.Write("Ket thuc");
}
using (var doc = new StreamReader(log))
{
    string? line;
    int so = 0;
    while ((line = doc.ReadLine()) is not null)
        Console.WriteLine($"  [{++so}] {line}");
}

// 5. Path: xu ly duong dan an toan
string p = @"C:\du-an\bao-cao\thang-3.xlsx";
Console.WriteLine($"{Path.GetFileName(p)} | {Path.GetFileNameWithoutExtension(p)} | {Path.GetExtension(p)}");
Console.WriteLine(Path.Combine("a", "b", "c.txt"));

// 6. File / Directory: thao tac he thong tap tin
string sao = Path.Combine(thuMuc, "sao.txt");
File.Copy(duongDan, sao, overwrite: true);
Directory.CreateDirectory(Path.Combine(thuMuc, "con"));
File.Move(sao, Path.Combine(thuMuc, "con", "sao-chuyen.txt"));
foreach (var f in Directory.EnumerateFiles(thuMuc, "*", SearchOption.AllDirectories))
    Console.WriteLine($"  {Path.GetRelativePath(thuMuc, f)}  ({new FileInfo(f).Length} bytes)");
Console.WriteLine($"Ton tai ghichu.txt? {File.Exists(duongDan)}");

// 7. Bat loi I/O
try
{
    File.ReadAllText(Path.Combine(thuMuc, "khong-co.txt"));
}
catch (FileNotFoundException e)
{
    Console.WriteLine($"Khong tim thay: {Path.GetFileName(e.FileName)}");
}

// 8. Bat dong bo
string asyncFile = Path.Combine(thuMuc, "async.txt");
await File.WriteAllTextAsync(asyncFile, "Noi dung bat dong bo");
Console.WriteLine(await File.ReadAllTextAsync(asyncFile));

// 9. FileStream: doc/ghi bytes
string nhiPhan = Path.Combine(thuMuc, "data.bin");
await File.WriteAllBytesAsync(nhiPhan, [1, 2, 3, 255]);
byte[] bytes = await File.ReadAllBytesAsync(nhiPhan);
Console.WriteLine($"Bytes: {string.Join(",", bytes)}");

// Don dep
Directory.Delete(thuMuc, recursive: true);
Console.WriteLine("Da don dep thu muc tam");
