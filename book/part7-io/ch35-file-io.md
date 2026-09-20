# Chương 35 — File I/O

## Mục tiêu học

Sau chương này, bạn sẽ:

- Đọc/ghi file văn bản và nhị phân bằng `File`, `StreamReader/StreamWriter`, `FileStream`.
- Làm việc với đường dẫn và thư mục một cách an toàn, **đa nền tảng** (`Path`, `Directory`).
- Biết chọn giữa đọc **cả file** và đọc **từng dòng**; đọc/ghi **bất đồng bộ**.
- Bắt và xử lý lỗi I/O; đảm bảo file luôn được đóng.

Code mẫu: [`code/ch35-file-io/`](../../code/ch35-file-io/).

Chương trình mẫu làm việc trong thư mục tạm của hệ điều hành (`Path.GetTempPath()`) và **tự dọn dẹp** khi xong, nên chạy
được nhiều lần mà không làm bừa máy.

## Các lớp chính (namespace `System.IO`)

| Lớp | Vai trò |
|-----|---------|
| `File` | thao tác nhanh trên **một file** (static): đọc, ghi, sao chép, xoá... |
| `Directory` / `DirectoryInfo` | thư mục: tạo, liệt kê, xoá |
| `Path` | xử lý **chuỗi đường dẫn** (ghép, tách, đuôi file) |
| `StreamReader` / `StreamWriter` | đọc/ghi **văn bản** theo luồng |
| `FileStream` | đọc/ghi **byte** ở mức thấp |
| `FileInfo` | thông tin một file (kích thước, ngày sửa...) |

## Đọc và ghi toàn bộ file (file nhỏ)

```csharp
File.WriteAllText(path, "Dong 1\nDong 2\n", Encoding.UTF8);   // ghi đè
string noiDung = File.ReadAllText(path);                        // đọc cả file thành một chuỗi
File.AppendAllText(path, "them dong\n");                        // ghi nối vào cuối
string[] dong = File.ReadAllLines(path);                        // mảng các dòng
File.WriteAllLines(path, ["a", "b"]);
byte[] data = File.ReadAllBytes(path);
```

Tiện, nhưng **nạp cả file vào bộ nhớ** → chỉ nên dùng cho file nhỏ (vài MB). **Luôn chỉ rõ mã hoá** (`Encoding.UTF8`) khi
ghi văn bản tiếng Việt để tránh vỡ dấu ở máy khác (mặc định .NET Core dùng UTF-8, nhưng nên tường minh khi trao đổi dữ liệu).

## Đọc từng dòng, xử lý file lớn

```csharp
foreach (var dong in File.ReadLines(path))     // LƯỜI: đọc từng dòng theo yêu cầu
    if (dong.Contains("ERROR")) dem++;
```

`File.ReadLines` trả `IEnumerable<string>` đọc dần (Chương 27) — dùng được cho file nhiều GB. (`ReadAllLines` thì đọc hết vào mảng.)
Vì lười, file **được mở suốt lúc duyệt**; và duyệt hai lần là đọc file hai lần.

## `StreamReader` / `StreamWriter` và `using`

Khi cần kiểm soát chi tiết (ghi dần, đọc theo điều kiện):

```csharp
using (var ghi = new StreamWriter(path, append: false, Encoding.UTF8))
{
    ghi.WriteLine("Bat dau");
    ghi.Write("Ket thuc");
}   // kết thúc khối using: tự Flush + đóng file

using var doc = new StreamReader(path);
string? line;
while ((line = doc.ReadLine()) is not null)
    Console.WriteLine(line);
```

**Luôn bọc trong `using`** (Chương 21): file là tài nguyên hệ điều hành; nếu không đóng, nó bị khoá (process khác không mở
được), dữ liệu có thể chưa ghi hết (chưa `Flush`), và rò rỉ handle. `ReadLine()` trả `null` khi hết file.

## Đường dẫn: `Path`

```csharp
Path.Combine("a", "b", "c.txt");                 // "a\b\c.txt" (Windows) hay "a/b/c.txt" (Linux/macOS)
Path.GetFileName(p);                              // "thang-3.xlsx"
Path.GetFileNameWithoutExtension(p);              // "thang-3"
Path.GetExtension(p);                             // ".xlsx"
Path.GetDirectoryName(p);
Path.GetTempPath(); Path.GetRelativePath(goc, f); Path.GetFullPath("x");
```

**Đừng tự nối chuỗi đường dẫn** bằng `+ "\\" +` hay `"/"` — dùng `Path.Combine` để chạy đúng trên mọi hệ điều hành. Chuỗi
đường dẫn kiểu Windows (`C:\...`) chỉ phân tích đúng trên Windows; trên Linux `\` không phải dấu phân cách.

**Bảo mật:** không ghép trực tiếp tên file do người dùng cung cấp vào đường dẫn (`../../etc/passwd` — *path traversal*).
Kiểm tra bằng `Path.GetFileName(input)` hoặc so sánh `Path.GetFullPath` với thư mục gốc cho phép.

## Thư mục và thao tác hệ thống tập tin

```csharp
Directory.CreateDirectory(dir);                    // tạo (cả các cấp cha; không lỗi nếu đã có)
File.Copy(nguon, dich, overwrite: true);
File.Move(nguon, dich);
File.Delete(path);                                 // không lỗi nếu file không tồn tại
Directory.Delete(dir, recursive: true);            // xoá cả nội dung
File.Exists(path); Directory.Exists(dir);

foreach (var f in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))   // lười
    Console.WriteLine(new FileInfo(f).Length);
```

Ưu tiên `Enumerate...` (lười) hơn `Get...` (dựng cả mảng) với thư mục lớn.

## Bất đồng bộ

```csharp
await File.WriteAllTextAsync(path, "Noi dung");
string s = await File.ReadAllTextAsync(path);
await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
```

Trong ứng dụng web/giao diện, dùng phiên bản `Async` để không chặn thread (Chương 33).

## `FileStream` — byte và truy cập ngẫu nhiên

```csharp
await File.WriteAllBytesAsync(path, [1, 2, 3, 255]);

using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
fs.Seek(2, SeekOrigin.Begin);     // nhảy tới vị trí
int b = fs.ReadByte();
```

`FileMode` (Create, Open, OpenOrCreate, Append, Truncate...), `FileAccess`, `FileShare` điều khiển cách mở. Ảnh, video, file
nén… đều là byte; xử lý qua `Stream` (kể cả `MemoryStream`, `GZipStream`, `NetworkStream` có cùng giao diện `Stream`).

## Xử lý lỗi I/O

Các ngoại lệ thường gặp:

| Ngoại lệ | Nguyên nhân |
|----------|-------------|
| `FileNotFoundException` / `DirectoryNotFoundException` | không tồn tại |
| `UnauthorizedAccessException` | không đủ quyền / đường dẫn là thư mục |
| `IOException` | file đang bị process khác khoá, đĩa đầy, đường dẫn sai |
| `PathTooLongException`, `ArgumentException` | đường dẫn không hợp lệ |

```csharp
try { File.ReadAllText(path); }
catch (FileNotFoundException e) { Console.WriteLine($"Khong tim thay: {e.FileName}"); }
catch (IOException e) { Console.WriteLine($"Loi I/O: {e.Message}"); }
```

Đừng kiểm tra `File.Exists` rồi mới mở và tin là an toàn — giữa hai lệnh, file có thể bị xoá (**race condition** TOCTOU);
vẫn phải `try/catch` khi mở.

## Ghi file an toàn

Ghi trực tiếp đè lên file quan trọng mà chương trình sập giữa chừng → file hỏng. Mẫu an toàn: ghi vào **file tạm**, xong
thì `File.Move(tam, dich, overwrite: true)` (thao tác đổi tên gần như nguyên tử).

## Lỗi thường gặp

- Quên `using` → file bị khoá / dữ liệu chưa ghi.
- Dùng `ReadAllText` cho file khổng lồ → hết bộ nhớ (`OutOfMemoryException`).
- Ghép đường dẫn bằng `+` và dấu `\`/`/` cứng.
- Đường dẫn tương đối phụ thuộc "thư mục làm việc hiện tại" (thay đổi tuỳ cách chạy). Dùng `AppContext.BaseDirectory` hoặc
  đường dẫn tuyệt đối/cấu hình.
- Sai mã hoá → tiếng Việt vỡ dấu.
- Ghi vào thư mục cài đặt (`Program Files`) → `UnauthorizedAccessException`; dùng thư mục dữ liệu người dùng
  (`Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)`).
- Đọc file đang được chương trình khác ghi (cần `FileShare.ReadWrite`).

## Bài tập

1. Viết chương trình đếm số dòng, số từ, số ký tự của một file văn bản (dùng `ReadLines`).
2. Sao chép toàn bộ file `.txt` từ thư mục A sang B, giữ nguyên cấu trúc thư mục con.
3. Viết nhật ký (logger) ghi thêm dòng có timestamp vào file `app.log`, mỗi lần chạy nối tiếp.
4. Tìm 5 file lớn nhất trong một thư mục (kèm thư mục con), in tên và kích thước.
5. Viết hàm `GhiAnToan(path, noiDung)` ghi qua file tạm rồi đổi tên.
