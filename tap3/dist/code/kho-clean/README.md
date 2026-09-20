# Kho Clean — solution dùng cho Tập 3 (chương 2–8, 9–11, 13, 18)

Hệ thống quản lý kho theo **Clean Architecture + CQRS + Result pattern + Outbox**.

```
cd Kho.Web
dotnet run                          # tự migrate SQLite; POST/GET /api/san-pham
cd ../Kho.Tests
dotnet test                         # Domain + Application + Architecture + Integration
```

Tạo migration mới (chạy trong `Kho.Web`):

```
dotnet ef migrations add Ten -p ../Kho.Infrastructure -s . -o Persistence/Migrations -- --MigrateOnStartup false
```
