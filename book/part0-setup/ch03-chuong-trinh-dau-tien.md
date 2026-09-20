# Chương 3 — Chương trình C# đầu tiên

## Mục tiêu học

Sau chương này, bạn sẽ:

- Tạo, biên dịch và chạy một chương trình C# chỉ bằng dòng lệnh `dotnet`, không cần IDE.
- Đọc hiểu file `Program.cs` dạng **top-level statements** và biết dạng `Main` truyền thống tương ứng.
- Đọc hiểu file dự án `.csproj` và các thư mục sinh ra (`bin/`, `obj/`).
- Nhận đối số dòng lệnh qua `args`.

Code mẫu đầy đủ: [`code/ch03-hello-world/`](../../code/ch03-hello-world/).

## Tạo project

```
dotnet new console -n HelloWorld
cd HelloWorld
```

Lệnh này sinh ra thư mục `HelloWorld` gồm:

```
HelloWorld/
├── HelloWorld.csproj    # file mô tả dự án
└── Program.cs           # mã nguồn
```

## Chương trình đầu tiên

Mở `Program.cs`, nội dung chỉ gồm một dòng:

```csharp
Console.WriteLine("Xin chao, C#!");
```

Chạy:

```
dotnet run
```

Kết quả:

```
Xin chao, C#!
```

Chỉ một dòng — vậy `class`, `Main`, `using System;` đâu rồi? Câu trả lời ở phần tiếp theo.

## Top-level statements và dạng `Main` truyền thống

Từ C# 9, chương trình nhỏ có thể viết **top-level statements**: lệnh đặt thẳng trong file, trình biên
dịch tự sinh ra class và phương thức `Main` cho bạn. Đây là dạng dùng trong sách để ví dụ ngắn gọn.

Dạng tương đương viết đầy đủ (cũng hợp lệ, và bạn sẽ gặp nhiều trong code cũ):

```csharp
namespace HelloWorld;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Xin chao, C#!");
    }
}
```

Giải phẫu:

- `class Program` — mọi phương thức trong C# đều thuộc một class (học kỹ ở Chương 13).
- `static void Main(string[] args)` — **điểm vào** (entry point): nơi chương trình bắt đầu chạy.
  `static` nghĩa là gọi được mà không cần tạo đối tượng; `void` nghĩa là không trả về giá trị;
  `args` là mảng đối số dòng lệnh.
- `Console.WriteLine(...)` — gọi phương thức `WriteLine` của class `Console` (thuộc namespace
  `System`) để in một dòng ra màn hình.

Dòng `using System;` không còn cần viết: mẫu project bật **`ImplicitUsings`**, tự thêm sẵn các
`using` phổ biến (`System`, `System.IO`, `System.Linq`, `System.Collections.Generic`...).

> Mỗi project chỉ được có **một** điểm vào. Top-level statements chỉ được dùng trong **một** file.

## File `.csproj` — hồ sơ của dự án

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

| Thuộc tính | Ý nghĩa |
|------------|---------|
| `Sdk="Microsoft.NET.Sdk"` | Dùng bộ build tiêu chuẩn của .NET |
| `OutputType` | `Exe` = chương trình chạy được; thư viện dùng `Library` (mặc định) |
| `TargetFramework` | Phiên bản .NET nhắm tới: `net10.0` |
| `ImplicitUsings` | Tự thêm các `using` phổ biến (đã nói ở trên) |
| `Nullable` | Bật kiểm tra tham chiếu null của trình biên dịch (Chương 23) |

Không cần liệt kê từng file `.cs` — SDK tự lấy mọi file `.cs` trong thư mục.

## `dotnet run` làm gì?

`dotnet run` gồm hai bước: **build** rồi **chạy**. Khi build, các thư mục mới xuất hiện:

- `obj/` — file trung gian của quá trình build.
- `bin/Debug/net10.0/` — kết quả build: `HelloWorld.dll` (IL) và file `HelloWorld` / `HelloWorld.exe`
  (bộ khởi chạy). Có thể chạy trực tiếp file này mà không cần lệnh `dotnet run`.

Hai thư mục này sinh ra tự động và **không đưa vào Git** (file `.gitignore` của repo đã loại trừ).
Xoá chúng đi bất kỳ lúc nào cũng an toàn — build lại sẽ tạo lại.

Để phát hành bản dùng thật: `dotnet publish -c Release`.

## Đối số dòng lệnh

Trong top-level statements, biến `args` có sẵn (kiểu `string[]`):

```csharp
string ten = args.Length > 0 ? args[0] : "hoc vien";

Console.WriteLine($"Xin chao, {ten}!");
Console.WriteLine($"Day la doi so dong lenh nhan duoc: {args.Length} doi so.");
```

- `args.Length` là số đối số. Biểu thức `điều_kiện ? a : b` chọn `a` nếu đúng, `b` nếu sai.
- `$"...{ten}..."` là **chuỗi nội suy**: chèn giá trị biến vào chuỗi (chi tiết ở Chương 11).

Chạy, lưu ý dấu `--` tách tham số của `dotnet run` khỏi đối số của chương trình:

```
dotnet run -- Tuan
```

```
Xin chao, Tuan!
Day la doi so dong lenh nhan duoc: 1 doi so.
```

## Lỗi thường gặp

- **`Couldn't find a project to run`**: bạn đang đứng sai thư mục. Chạy `dotnet run` trong thư mục có
  file `.csproj`, hoặc dùng `dotnet run --project HelloWorld`.
- **Thiếu dấu `;` cuối lệnh** → lỗi biên dịch `CS1002: ; expected`. Mỗi câu lệnh C# kết thúc bằng `;`.
- **Sai hoa/thường**: `console.writeline` là sai; đúng là `Console.WriteLine`. C# **phân biệt** hoa/thường.
- **Hai điểm vào** (`CS0017: Program has more than one entry point`): dùng cả top-level statements ở
  nhiều file, hoặc vừa có `Main` vừa có top-level statements.
- **Quên `--` khi truyền đối số**, khiến `dotnet run` hiểu nhầm đối số là tuỳ chọn của chính nó.

## Bài tập

1. Sửa chương trình in ra tên và tuổi của bạn trên hai dòng.
2. Viết lại `Greeter` bằng dạng `class Program` + `Main` truyền thống, kết quả phải giống hệt.
3. Cố tình đổi `WriteLine` thành `Writeline` và đọc thông báo lỗi. Trình biên dịch nói gì, chỉ vào dòng nào?
4. Sửa `Greeter` để ghép **mọi** đối số thành một câu chào (gợi ý: `string.Join(" ", args)`).
