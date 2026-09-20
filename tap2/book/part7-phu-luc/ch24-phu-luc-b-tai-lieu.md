# Phụ lục B — Tài liệu tham khảo (Tập 2)

Ưu tiên nguồn chính thức, miễn phí, cập nhật thường xuyên. Địa chỉ có thể đổi — nếu không mở được, tìm theo tên.

## B.1 Tài liệu chính thức

- **ASP.NET Core**: <https://learn.microsoft.com/aspnet/core/> — Fundamentals, Minimal APIs, MVC, Razor Pages, Blazor, Security, Performance.
- **Entity Framework Core**: <https://learn.microsoft.com/ef/core/> — quan hệ, migration, hiệu năng, provider.
- **Bảo mật ASP.NET Core**: <https://learn.microsoft.com/aspnet/core/security/> — Identity, JWT, Data Protection, CORS, rate limiting.
- **HttpClient & resilience**: <https://learn.microsoft.com/dotnet/core/resilience/> — `Microsoft.Extensions.Http.Resilience`, Polly.
- **OpenAPI/Scalar**: <https://learn.microsoft.com/aspnet/core/fundamentals/openapi/>, <https://github.com/scalar/scalar>.
- **Blazor**: <https://learn.microsoft.com/aspnet/core/blazor/>.
- **RFC 9110** (HTTP semantics), **RFC 9457** (Problem Details), **RFC 7519** (JWT): tra cứu chuẩn.
- **OWASP**: <https://owasp.org/www-project-top-ten/>, **Cheat Sheet Series** <https://cheatsheetseries.owasp.org/>.
- **Mã nguồn ASP.NET Core**: <https://github.com/dotnet/aspnetcore>; **EF Core**: <https://github.com/dotnet/efcore>.

## B.2 Sách

| Sách | Tác giả | Nội dung |
|------|---------|----------|
| *ASP.NET Core in Action* (bản mới) | Andrew Lock | toàn diện, giải thích "vì sao" |
| *Pro ASP.NET Core* | Adam Freeman | tham khảo rộng |
| *Entity Framework Core in Action* | Jon P Smith | EF Core chuyên sâu |
| *Designing Web APIs* | Brenda Jin et al. | thiết kế API |
| *RESTful Web API Patterns and Practices Cookbook* | Mike Amundsen | mẫu thiết kế API |
| *Web Application Security* | Andrew Hoffman | bảo mật ứng dụng web |
| *Release It!* | Michael Nygard | chịu lỗi, retry/circuit breaker |
| *Designing Data-Intensive Applications* | Martin Kleppmann | nền tảng hệ thống dữ liệu |
| *SQL Antipatterns* | Bill Karwin | tránh lỗi thiết kế CSDL |

## B.3 Blog, video, cộng đồng

- **Andrew Lock** (andrewlock.net), **Khalid Abuhakmeh**, **Steve Gordon** — ASP.NET Core chuyên sâu.
- **Milan Jovanović**, **Nick Chapsas**, **Tim Corey**, **Raw Coding** — video thực hành.
- **.NET Blog**, **ASP.NET Community Standup**, **.NET Conf**.
- **Stack Overflow** (`asp.net-core`, `entity-framework-core`), **r/dotnet**, **Discord .NET**.

## B.4 Công cụ

| Nhu cầu | Công cụ |
|---------|---------|
| Thử API | file `.http` (VS/VS Code), Postman, Insomnia, **Bruno**, `curl`, **Scalar/Swagger UI** |
| CSDL | DB Browser for SQLite, **Azure Data Studio**/SSMS, **pgAdmin**/DBeaver |
| Xem SQL EF | `ToQueryString()`, log `Database.Command`, MiniProfiler |
| Kiểm thử | xUnit, `WebApplicationFactory`, **Testcontainers** (CSDL thật trong Docker), Respawn |
| Tải/hiệu năng | **k6**, **NBomber**, `dotnet-counters`, BenchmarkDotNet |
| Bảo mật | `dotnet list package --vulnerable`, Dependabot, OWASP ZAP, securityheaders.com |
| Gỡ lỗi mạng | DevTools, Fiddler, mitmproxy, Wireshark |

## B.5 Luyện tập

1. Mở rộng dự án Chương 22 theo danh sách bài tập lớn.
2. Viết lại một API bạn từng dùng (thư viện sách, blog, đặt lịch) từ đầu đến khi có test, JWT, Docker.
3. Đọc mã nguồn mẫu chính thức: **eShop** (`dotnet/eShop`), **Contoso University**.
4. Thử thách bảo mật: **OWASP Juice Shop**, **PortSwigger Web Security Academy** (miễn phí).
5. Đóng góp/đọc issue của một dự án mã nguồn mở .NET web.

## B.6 Lộ trình tiếp theo

**Tập 3** của bộ sách: kiến trúc sạch, CQRS, mẫu thiết kế nâng cao, quan sát, bảo mật nâng cao (OIDC), Docker, CI/CD, triển khai đám mây, microservices tổng quan.
