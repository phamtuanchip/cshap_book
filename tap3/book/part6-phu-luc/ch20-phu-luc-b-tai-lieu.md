# Phụ lục B — Tài liệu tham khảo

Danh sách nguồn nên đọc tiếp. Địa chỉ và phiên bản có thể thay đổi; hãy đối chiếu bản mới nhất.

## Tài liệu chính thức

- **.NET / ASP.NET Core / EF Core**: `learn.microsoft.com/dotnet`, `learn.microsoft.com/aspnet/core`, `learn.microsoft.com/ef/core`.
- **Kiến trúc**: *.NET Microservices: Architecture for Containerized .NET Applications* và *Architecting Modern Web Applications with ASP.NET Core and Azure* (eBook miễn phí của Microsoft).
- **OpenTelemetry**: `opentelemetry.io` (đặc tả, tài liệu .NET, Collector).
- **OAuth 2.0 / OIDC**: RFC 6749 (OAuth 2.0), RFC 7636 (PKCE), RFC 9700 (best current practice), OpenID Connect Core 1.0, RFC 7519 (JWT), RFC 9110 (ngữ nghĩa HTTP).
- **W3C Trace Context**: `w3.org/TR/trace-context`.
- **Docker**: `docs.docker.com` (Dockerfile best practices, Compose). **Kubernetes**: `kubernetes.io/docs`.
- **GitHub Actions**: `docs.github.com/actions`.
- **YARP**: `microsoft.github.io/reverse-proxy`. **.NET Aspire**: tài liệu trên learn.microsoft.com.
- **Nghị định 13/2023/NĐ-CP** về bảo vệ dữ liệu cá nhân (Việt Nam); GDPR nếu phục vụ EU.

## Sách

| Sách | Tác giả | Chủ đề |
|------|---------|--------|
| *Clean Architecture* | Robert C. Martin | nguyên tắc phụ thuộc, ranh giới |
| *Domain-Driven Design* | Eric Evans | miền, bounded context, aggregate |
| *Implementing Domain-Driven Design* | Vaughn Vernon | DDD thực hành |
| *Patterns of Enterprise Application Architecture* | Martin Fowler | Repository, Unit of Work... |
| *Designing Data-Intensive Applications* | Martin Kleppmann | nhất quán, giao dịch, hệ phân tán |
| *Building Microservices* | Sam Newman | microservices, ranh giới, dữ liệu |
| *Release It!* | Michael Nygard | ổn định, circuit breaker, bulkhead |
| *Site Reliability Engineering* | Google | SLO, cảnh báo, vận hành |
| *Accelerate* | Forsgren, Humble, Kim | CI/CD, hiệu quả phát hành |
| *Unit Testing: Principles, Practices, and Patterns* | Vladimir Khorikov | kiểm thử có giá trị |
| *Dependency Injection Principles, Practices, and Patterns* | Mark Seemann, Steven van Deursen | DI |

## Thư viện và công cụ nhắc tới trong Tập 3

| Nhu cầu | Công cụ |
|---------|---------|
| Validation | FluentValidation |
| Mediator | MediatR (thương mại từ 2025 — xem giấy phép), Wolverine, hoặc tự viết như Chương 5 |
| Kiểm thử | xUnit, NSubstitute, `WebApplicationFactory`, Testcontainers, Bogus, Verify |
| Kiểm tra kiến trúc | NetArchTest, ArchUnitNET |
| Cache | `Microsoft.Extensions.Caching.Hybrid`, Redis (StackExchange.Redis) |
| Quan sát | OpenTelemetry, Grafana (Tempo/Loki/Prometheus), Jaeger, Application Insights |
| Định danh | Keycloak, Microsoft Entra ID, Auth0, Duende IdentityServer, OpenIddict |
| Messaging | MassTransit, Wolverine, NServiceBus, RabbitMQ, Azure Service Bus, Kafka |
| Gateway | YARP |
| Hạ tầng dưới dạng mã | Bicep, Terraform/OpenTofu, Pulumi |
| Tải | k6, NBomber; vi mô: BenchmarkDotNet |
| Chẩn đoán | `dotnet-counters`, `dotnet-trace`, `dotnet-dump` |

## Blog và cộng đồng

- Blog .NET của Microsoft (`devblogs.microsoft.com/dotnet`), bản ghi chú phát hành .NET và ASP.NET Core.
- Blog của Milan Jovanović, Andrew Lock, Steve Smith (Ardalis), Nick Chapsas, Jimmy Bogard (tác giả AutoMapper/MediatR).
- Martin Fowler (`martinfowler.com`): mô tả các pattern kinh điển.
- Cộng đồng: `r/dotnet`, Stack Overflow, GitHub Discussions của dotnet, các nhóm .NET Việt Nam.

## Ba tập của bộ sách

- **Tập 1** — C# và .NET nền tảng: ngôn ngữ, thư viện, bất đồng bộ, kiểm thử, ứng dụng dòng lệnh.
- **Tập 2** — ASP.NET Core: HTTP, Minimal API, MVC/Razor Pages/Blazor, EF Core, xác thực, bảo mật, dự án tổng hợp.
- **Tập 3** — Kiến trúc và vận hành: Clean Architecture, DDD, CQRS, outbox, kiểm thử, cache, quan sát, OIDC, Docker, CI/CD, cloud, microservices.
