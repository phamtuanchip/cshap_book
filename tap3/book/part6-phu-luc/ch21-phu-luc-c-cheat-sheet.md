# Phụ lục C — Cheat sheet kiến trúc và triển khai

## Quy tắc phụ thuộc (Clean Architecture)

```
Web ──► Application ──► Domain
 └────► Infrastructure ──► Application (cài đặt các "cổng")
Domain: không phụ thuộc gì. Application: chỉ Domain. Infrastructure/Web: nối mọi thứ ở composition root.
```

## Chọn mẫu thiết kế

| Bạn cần... | Dùng |
|-----------|------|
| Bảo vệ quy tắc nghiệp vụ | aggregate + phương thức nghiệp vụ, setter private |
| Giá trị không có danh tính | value object bất biến (`record`) |
| Lỗi nghiệp vụ dự kiến | `Result<T>` (không exception) |
| Việc dùng chung cho mọi use case | pipeline behavior |
| Tách đọc/ghi | CQRS: lệnh qua aggregate, truy vấn qua read model/DTO |
| Phát sự kiện đáng tin | outbox trong cùng giao dịch |
| Gửi lại an toàn | `Idempotency-Key` + khoá duy nhất (INSERT nguyên tử) |
| Ai làm gì | audit log cùng giao dịch, chỉ thêm |
| Nghiệp vụ nhiều dịch vụ | saga (bù trừ), không 2PC |

## Pipeline mặc định

`Logging > Validation > UnitOfWork > Handler` — lệnh thành công thì `LuuAsync` một lần; thất bại thì không lưu gì.

## Mã trạng thái theo loại lỗi

| `LoaiLoi` | HTTP |
|-----------|------|
| DuLieuKhongHopLe | 400 |
| KhongTimThay | 404 |
| XungDot (trùng, đồng thời) | 409 |
| NghiepVu | 422 |
| (không lường trước) | 500 ProblemDetails, không lộ chi tiết |
| Chưa xác thực / không đủ quyền | 401 / 403 |

## Cache

| Công cụ | Dùng cho |
|---------|----------|
| `IMemoryCache` | cache cục bộ đơn giản, một instance |
| `HybridCache` | L1+L2, **chống stampede**, tag để vô hiệu hoá |
| Output cache | cả response HTTP, chính sách + tag |
| Redis | cache phân tán/nhiều instance |

Luôn có: TTL, chiến lược vô hiệu hoá, khoá gồm mọi tham số ảnh hưởng kết quả, không cache dữ liệu theo người dùng ở nơi dùng chung.

## Quan sát

- **Logs** (chuyện gì), **Metrics** (xu hướng), **Traces** (đi đâu) — nối bằng `TraceId`.
- Metric nhãn **cardinality thấp**; không `UserId`/`OrderId`.
- Bốn tín hiệu vàng: latency, traffic, errors, saturation.
- **Liveness** (còn sống? — không kiểm tra phụ thuộc) ≠ **Readiness** (nhận tải được? — kiểm tra phụ thuộc).

## OAuth 2.0 / OIDC

- Web/SPA/mobile: **Authorization Code + PKCE**. Không Implicit/Password.
- **ID token** = ai là người dùng (cho client); **access token** = quyền gọi API (cho resource server).
- API kiểm: chữ ký, `iss`, `aud`, `exp`, `scope`/role. `MapInboundClaims = false`.
- Máy-với-máy: **Client Credentials** (ưu tiên managed identity).

## Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build      # copy *.csproj → restore → copy code → publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final   # copy publish; USER $APP_UID; cổng 8080
ENTRYPOINT ["dotnet", "App.dll"]
```

Biến môi trường: `A:B` → `A__B`. Dữ liệu ở volume/CSDL ngoài. Không secret trong image. Không `latest`.

## Lệnh hay dùng

```bash
dotnet new webapi -n Ten && dotnet run
dotnet build -c Release -warnaserror
dotnet test --logger trx
dotnet publish -c Release -o out -p:UseAppHost=false

dotnet ef migrations add Ten -p Infrastructure -s Web -o Persistence/Migrations
dotnet ef database update -p Infrastructure -s Web
dotnet ef migrations bundle -p Infrastructure -s Web        # tệp chạy migration cho CI/CD

dotnet user-secrets init && dotnet user-secrets set "Khoa" "giatri"
dotnet list package --vulnerable --include-transitive
dotnet-counters monitor -p <PID>

docker build -t app:dev . && docker run --rm -p 8080:8080 -v data:/data app:dev
docker compose up --build / logs -f / down
```

## Triển khai và phát hành

- **CI**: restore → build (`-warnaserror`) → test → quét → dựng artifact **một lần**.
- **CD**: thăng cấp cùng artifact; staging → smoke test → duyệt → production.
- Chiến lược: recreate, **rolling**, **blue-green**, **canary**; DB theo **expand/contract**.
- Migration chạy như bước riêng (bundle), không để mọi instance tự migrate.
- Nhiều instance: Data Protection keys chia sẻ; không trạng thái cục bộ; forwarded headers **chỉ tin proxy của bạn**.

## Danh sách kiểm tra ngắn cho một thay đổi

- [ ] Quy tắc nghiệp vụ nằm ở Domain, có test domain?
- [ ] Use case là lệnh/truy vấn rõ ràng; lỗi dự kiến trả `Result`?
- [ ] Ghi dữ liệu có thể bị gửi lặp → idempotent?
- [ ] Thay đổi trạng thái quan trọng có audit; sự kiện đi qua outbox?
- [ ] Có log/metric/trace đủ để điều tra khi lỗi?
- [ ] Migration tương thích ngược; có kế hoạch rollback?
- [ ] Không secret/PII trong mã, log, image?
- [ ] CI xanh; đã chạy thật (không chỉ "biên dịch được")?
