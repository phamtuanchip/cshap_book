# Phụ lục B — Tài liệu tham khảo và nguồn học thêm

Danh sách dưới đây ưu tiên **nguồn chính thức, miễn phí và được duy trì thường xuyên**. Địa chỉ web có thể thay đổi; nếu link không
mở được, hãy tìm theo tên.

## B.1 Tài liệu chính thức

- **Microsoft Learn — C#**: <https://learn.microsoft.com/dotnet/csharp/> — hướng dẫn, tham chiếu ngôn ngữ, "What's new in C#".
- **Microsoft Learn — .NET**: <https://learn.microsoft.com/dotnet/> — kiến trúc, CLI, thư viện lớp cơ sở.
- **.NET API Browser**: <https://learn.microsoft.com/dotnet/api/> — tra từng lớp/phương thức (ví dụ `List<T>`, `File`).
- **Tải .NET SDK**: <https://dotnet.microsoft.com/download> — kèm lịch hỗ trợ (LTS/STS).
- **C# language specification** và đề xuất tính năng: <https://github.com/dotnet/csharplang>.
- **Mã nguồn .NET Runtime**: <https://github.com/dotnet/runtime> — đọc cách `List<T>`, `Dictionary` được cài đặt.
- **NuGet Gallery**: <https://www.nuget.org> — tìm thư viện.
- **ASP.NET Core** (cho Tập 2): <https://learn.microsoft.com/aspnet/core/>.
- **Entity Framework Core** (Tập 2): <https://learn.microsoft.com/ef/core/>.

## B.2 Sách nên đọc thêm

| Sách | Tác giả | Đọc khi |
|------|---------|---------|
| *C# in Depth* (bản mới nhất) | Jon Skeet | muốn hiểu sâu ngôn ngữ, các phiên bản mới |
| *Pro C# and .NET* | Andrew Troelsen, Philip Japikse | tham khảo toàn diện |
| *Clean Code* | Robert C. Martin | viết code dễ đọc, dễ bảo trì |
| *Refactoring* | Martin Fowler | tái cấu trúc code (kèm Chương 42) |
| *Design Patterns* (GoF) | Gamma, Helm, Johnson, Vlissides | gốc của các pattern (Chương 41) |
| *Head First Design Patterns* | Freeman & Robson | tiếp cận design pattern dễ hiểu |
| *The Pragmatic Programmer* | Hunt, Thomas | tư duy và thói quen nghề nghiệp |
| *Concurrency in C# Cookbook* | Stephen Cleary | async/đa luồng thực dụng (Chương 33–34) |
| *Unit Testing Principles, Practices, and Patterns* | Vladimir Khorikov | test tốt (Chương 38) |
| *Dependency Injection Principles, Practices, and Patterns* | Steven van Deursen, Mark Seemann | DI đúng cách (Chương 42) |
| *Algorithms* | Sedgewick & Wayne | cấu trúc dữ liệu và thuật toán (Chương 24–29) |
| *Introduction to Algorithms* (CLRS) | Cormen và cộng sự | tham khảo thuật toán nâng cao |

## B.3 Blog, video, cộng đồng

- **.NET Blog**: <https://devblogs.microsoft.com/dotnet/> — thông báo tính năng, bài viết hiệu năng (Stephen Toub).
- **Nick Chapsas**, **Milan Jovanović**, **Tim Corey** (YouTube) — video thực hành C#/.NET.
- **Stephen Cleary's blog** — async chuyên sâu; **Andrew Lock** — ASP.NET Core; **Jon Skeet** — ngôn ngữ C#.
- **Stack Overflow** (thẻ `c#`, `.net`) — hỏi đáp; **Reddit r/dotnet**, **r/csharp**; **Discord C# / .NET**.
- **.NET Conf** (hằng năm, video miễn phí) và **.NET Community Standup**.

## B.4 Công cụ

| Nhu cầu | Công cụ |
|---------|---------|
| IDE | Visual Studio, VS Code + C# Dev Kit, JetBrains Rider |
| Thử code nhanh | **LINQPad**, `dotnet-script`, [SharpLab.io](https://sharplab.io) (xem code sau khi biên dịch) |
| Kiểm thử | xUnit, NUnit, MSTest; NSubstitute, Moq; FluentAssertions |
| Độ phủ / chất lượng | Coverlet, ReportGenerator; SonarQube; `dotnet format`; Roslyn analyzers |
| Hiệu năng | **BenchmarkDotNet**, `dotnet-counters`, `dotnet-trace`, PerfView |
| Xem/giải mã assembly | ILSpy, dnSpy (mã nguồn mở) |
| Logging | Serilog, NLog; Seq (xem log) |
| Quản lý mã nguồn | Git, GitHub Desktop, GitKraken |

## B.5 Luyện tập

- **Exercism** (C# track): bài tập có mentor. **LeetCode**, **HackerRank**, **Codewars**, **Advent of Code**: thuật toán và tư duy.
- **Microsoft Learn Modules**: lộ trình có chấm bài. **Roslyn / dotnet/samples**: mẫu code chính thức.
- Tự làm dự án: mở rộng dự án Chương 43; đóng góp cho dự án mã nguồn mở .NET (tìm issue `good first issue`).

## B.6 Lộ trình sau Tập 1

1. **Tập 2 của bộ sách này**: ASP.NET Core, EF Core, MVC/Razor Pages/Blazor.
2. **Tập 3**: kiến trúc sạch, DDD, CQRS, bảo mật, hiệu năng, Docker, CI/CD, triển khai cloud.
3. Học nền tảng bổ trợ: **SQL**, **HTTP/REST**, **Git**, **Docker**, kiến thức cơ bản về **cloud** (Azure).
4. Chuyên sâu tuỳ hướng: API/backend, dịch vụ nền, **Blazor** (frontend C#), game (**Unity**), ứng dụng đa nền tảng (**.NET MAUI**), AI (thư viện .NET cho ML/LLM).
