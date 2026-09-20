# Chương 2 — Cài đặt .NET SDK và IDE

## Mục tiêu học

Sau chương này, bạn sẽ:

- Cài được **.NET 10 SDK** và kiểm tra cài đặt thành công bằng `dotnet --version`.
- Chọn và cài một trình soạn thảo/IDE phù hợp: Visual Studio, VS Code hoặc JetBrains Rider.
- Biết vài lệnh `dotnet` nền tảng để dùng suốt cuốn sách.

## Bước 1 — Cài .NET SDK

Tải SDK (không phải chỉ Runtime) tại [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
— chọn **.NET 10 (LTS)**.

- **Windows**: chạy file cài đặt `.exe`, hoặc dùng winget:
  ```
  winget install Microsoft.DotNet.SDK.10
  ```
- **macOS**: dùng file `.pkg`, hoặc `brew install --cask dotnet-sdk`.
- **Linux**: cài theo hướng dẫn của bản phân phối (Ubuntu: `sudo apt install dotnet-sdk-10.0`, tên gói
  có thể khác nhau tuỳ phiên bản hệ điều hành).

Mở terminal **mới** (để nhận biến môi trường `PATH`) và kiểm tra:

```
dotnet --version
```

Kết quả sẽ là một số bắt đầu bằng `10.` (ví dụ `10.0.100`). Xem đầy đủ những gì đã cài:

```
dotnet --info
dotnet --list-sdks
```

Nếu báo `No .NET SDKs were found` hoặc không nhận lệnh `dotnet`: đóng hẳn terminal rồi mở lại; nếu
vẫn lỗi, cài lại SDK và kiểm tra `PATH` có chứa thư mục cài `dotnet`.

## Bước 2 — Chọn công cụ soạn thảo

Bạn có thể viết C# bằng bất kỳ editor nào, nhưng IDE giúp gợi ý code, gỡ lỗi, refactor. Ba lựa chọn
phổ biến:

| Công cụ | Nền tảng | Đặc điểm |
|---------|----------|----------|
| **Visual Studio Community** | Windows | IDE đầy đủ nhất, miễn phí cho cá nhân/học tập. Khi cài chọn workload **"ASP.NET and web development"** (cần cho Tập 2). |
| **VS Code + C# Dev Kit** | Windows / macOS / Linux | Nhẹ, đa nền tảng. Cài extension **C# Dev Kit** của Microsoft. |
| **JetBrains Rider** | Windows / macOS / Linux | IDE mạnh, miễn phí cho mục đích phi thương mại. |

**Gợi ý cho người mới**: Visual Studio nếu bạn dùng Windows; VS Code nếu dùng macOS/Linux hoặc muốn
nhẹ. Sách này luôn kèm **lệnh `dotnet` dòng lệnh** tương đương, nên dùng công cụ nào cũng theo được.

## Bước 3 — Vài lệnh `dotnet` cần biết

| Lệnh | Tác dụng |
|------|----------|
| `dotnet new list` | Liệt kê template project có sẵn |
| `dotnet new console -n TenProject` | Tạo project console mới |
| `dotnet run` | Biên dịch và chạy project trong thư mục hiện tại |
| `dotnet build` | Chỉ biên dịch |
| `dotnet test` | Chạy unit test |
| `dotnet add package <tên>` | Thêm thư viện NuGet |

Ta sẽ dùng chúng ngay ở chương sau.

## Thử nhanh cho chắc

```
dotnet new console -n Thu
cd Thu
dotnet run
```

Nếu thấy `Hello, World!` in ra, môi trường của bạn đã sẵn sàng.

## Lỗi thường gặp

- **Lệnh `dotnet` không nhận**: chưa mở terminal mới sau khi cài, hoặc `PATH` thiếu thư mục dotnet.
- **Cài nhiều SDK, chạy nhầm phiên bản**: kiểm tra bằng `dotnet --list-sdks`. Có thể ghim phiên bản
  cho một thư mục bằng file `global.json` (tạo bằng `dotnet new globaljson --sdk-version 10.0.100`).
- **VS Code không gợi ý code C#**: chưa cài **C# Dev Kit**, hoặc bạn mở *một file lẻ* thay vì mở
  *thư mục* project.
- **Visual Studio thiếu template web**: chạy lại Visual Studio Installer, thêm workload ASP.NET.

## Bài tập

1. Chạy `dotnet --info`, ghi lại phiên bản SDK và hệ điều hành mà nó báo.
2. Chạy `dotnet new list`, đếm xem có bao nhiêu template và tìm ra template `webapi`, `mvc`, `classlib`.
3. Tạo một project console tên `Thu2` và chạy nó bằng IDE bạn đã chọn (nút Run/Start).
