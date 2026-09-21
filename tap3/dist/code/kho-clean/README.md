# Kho Clean — solution dùng cho Tập 3 (chương 2–8, 9–11, 13, 18)

Hệ thống quản lý kho theo **Clean Architecture + CQRS + Result pattern + Outbox**.

```
cd Kho.Web
dotnet run                          # tự migrate SQLite; POST/GET /api/san-pham
cd ../Kho.Tests
dotnet test                         # Domain + Application + Architecture + Integration + Idempotency/Audit (45 test)
```

Tạo migration mới (chạy trong `Kho.Web`):

```
dotnet ef migrations add Ten -p ../Kho.Infrastructure -s . -o Persistence/Migrations -- --MigrateOnStartup false
```

## Trạng thái kiểm chứng

- 45 test (domain, application, architecture, tích hợp, idempotency/audit) đã đạt **trước khi** thêm mô-đun Đơn hàng (Chương 18).
- Mô-đun Đơn hàng: build được; 3 test domain đạt; các test tích hợp trong `DonHangTests.cs` **chưa chạy được ở máy tác giả** (Windows Application Control chặn nạp `Kho.Infrastructure.dll`). Hãy chạy `dotnet test` ở máy bạn.
- `Dockerfile`, `docker-compose.yml` và `.github/workflows/kho-clean.yml` chưa được chạy thật (không có Docker daemon/GitHub Actions ở máy tác giả); lệnh `dotnet publish` và bản publish khởi động đã kiểm chứng.
