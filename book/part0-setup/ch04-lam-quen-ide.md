# Chương 4 — Làm quen IDE, solution và NuGet

## Mục tiêu học

Sau chương này, bạn sẽ:

- Tạo được **solution** chứa nhiều project (một ứng dụng + một thư viện) bằng cả IDE lẫn `dotnet` CLI.
- Chạy và **gỡ lỗi** (debug) cơ bản: breakpoint, step over/into, xem biến.
- Thêm thư viện từ **NuGet** và hiểu nó được ghi vào đâu trong `.csproj`.

Code mẫu: [`code/ch04-lam-quen-ide/`](../../code/ch04-lam-quen-ide/).

## Solution và project

- **Project** (`.csproj`): một đơn vị biên dịch, cho ra một `.dll` hoặc `.exe`.
- **Solution** (`.sln` hoặc `.slnx`): "hộp" gom nhiều project lại để build/mở cùng nhau.

Dự án thực tế hiếm khi chỉ có một project. Ta sẽ dựng solution gồm ứng dụng `App` dùng thư viện
`MayTinh`:

```
dotnet new sln -n LamQuenIde
dotnet new console -n App
dotnet new classlib -n MayTinh
dotnet sln add App MayTinh
dotnet add App reference MayTinh
```

- `classlib` tạo **thư viện lớp** (không chạy độc lập, để project khác dùng).
- `dotnet add App reference MayTinh` khai báo `App` phụ thuộc `MayTinh` (thêm dòng `ProjectReference`
  vào `App.csproj`).

Trong Visual Studio: **File → New → Project**, làm tương tự qua giao diện. Trong VS Code: mở thư mục
solution, dùng **Solution Explorer** của C# Dev Kit.

Thư viện `MayTinh/PhepTinh.cs`:

```csharp
namespace MayTinh;

public static class PhepTinh
{
    public static int Cong(int a, int b) => a + b;
}
```

`App/Program.cs`:

```csharp
using MayTinh;

Console.WriteLine($"7 + 5 = {PhepTinh.Cong(7, 5)}");
```

Từ khoá `public` cho phép project khác truy cập class — nếu thiếu, `App` sẽ báo không thấy `PhepTinh`
(học kỹ ở Chương 14). Chạy: `dotnet run --project App`.

## Chạy và debug trong IDE

Debug là kỹ năng quan trọng nhất sau khi viết được code. Quy trình cơ bản:

1. **Đặt breakpoint**: bấm vào lề trái cạnh số dòng (hoặc phím `F9`). Chương trình sẽ dừng *trước
   khi* chạy dòng đó.
2. **Chạy chế độ debug**: `F5` (Visual Studio / VS Code), không phải `Ctrl+F5` (chạy không debug).
3. Khi dừng ở breakpoint, dùng:

| Thao tác | Phím (VS) | Ý nghĩa |
|----------|-----------|---------|
| Step Over | `F10` | Chạy hết dòng hiện tại, không đi vào phương thức được gọi |
| Step Into | `F11` | Đi vào bên trong phương thức đang gọi |
| Step Out | `Shift+F11` | Chạy nốt phương thức hiện tại, quay về nơi gọi |
| Continue | `F5` | Chạy tiếp đến breakpoint kế |

4. **Xem giá trị biến**: rê chuột lên biến, hoặc mở cửa sổ **Locals / Watch**. Thử đặt breakpoint ở
   dòng gọi `PhepTinh.Cong`, nhấn `F11` để đi vào phương thức và xem `a`, `b`.

Debug chi tiết hơn (điều kiện breakpoint, logging) sẽ học ở Chương 39.

## NuGet: dùng thư viện của người khác

**NuGet** là kho thư viện .NET (tại [nuget.org](https://www.nuget.org)). Ví dụ thêm thư viện
**Humanizer** (chuyển đổi thời gian/số thành văn bản dễ đọc) vào `App`:

```
dotnet add App package Humanizer.Core
```

Lệnh này (1) tải thư viện về bộ nhớ đệm cục bộ, (2) ghi một dòng vào `App.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Humanizer.Core" Version="..." />
</ItemGroup>
```

Dùng thử:

```csharp
using Humanizer;
using MayTinh;

Console.WriteLine($"7 + 5 = {PhepTinh.Cong(7, 5)}");
Console.WriteLine($"Khoang thoi gian: {TimeSpan.FromDays(3).Humanize()}");
```

```
7 + 5 = 12
Khoang thoi gian: 3 days
```

Các lệnh liên quan: `dotnet list package` (xem đã dùng gì), `dotnet remove package <tên>` (gỡ),
`dotnet restore` (tải lại các gói theo `.csproj` — `dotnet build`/`run` tự gọi khi cần). Trong IDE có
giao diện **Manage NuGet Packages**.

> **Quy tắc chọn thư viện**: xem số lượt tải, ngày cập nhật gần nhất, giấy phép và mức độ được cộng
> đồng tin dùng trước khi thêm vào dự án. Mỗi gói thêm vào là một phụ thuộc bạn phải gánh lâu dài.

## Cấu trúc thư mục nên có

```
LamQuenIde/
├── LamQuenIde.sln
├── App/          (console, tham chiếu MayTinh)
└── MayTinh/      (class library)
```

Đặt code dùng chung vào thư viện, code khởi chạy vào ứng dụng — kiểu tách này là nền của kiến trúc
phân lớp ở Tập 3.

## Lỗi thường gặp

- **`The type or namespace name 'MayTinh' could not be found`**: quên `dotnet add App reference MayTinh`,
  hoặc quên `using MayTinh;`.
- **`'PhepTinh' is inaccessible due to its protection level`**: class/phương thức thiếu `public`.
- **Breakpoint có hình tròn rỗng, không dừng**: đang chạy bản Release, hoặc file nguồn không khớp
  bản build. Chạy lại ở cấu hình **Debug**.
- **Restore package thất bại**: kiểm tra mạng, hoặc chạy `dotnet nuget locals all --clear` rồi thử lại.
- **Lỡ `dotnet run` ở thư mục solution có nhiều project**: chỉ định `--project <tên>`.

## Bài tập

1. Thêm phương thức `Nhan(int a, int b)` vào `PhepTinh`, gọi từ `App`, và debug bước vào nó bằng `F11`.
2. Dùng Humanizer để in `1234567` dưới dạng chữ (tìm trong tài liệu phương thức `ToWords()`).
3. Thêm project thứ ba `dotnet new console -n App2` vào solution, cho nó cũng tham chiếu `MayTinh`.
4. Mở `App.csproj` bằng editor và chỉ ra `ProjectReference` và `PackageReference`.
