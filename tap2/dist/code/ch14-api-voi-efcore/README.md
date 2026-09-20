# Chương 14 — Code mẫu: Web API hoàn chỉnh với EF Core

```
cd CuaHangApi
dotnet run                     # tự migrate + nạp dữ liệu mẫu vào cuahang.db
dotnet test ../CuaHangApi.Tests   # 10 test tích hợp (SQLite in-memory)
```

Thử: `curl "http://localhost:5000/api/san-pham?sapXep=-gia&kichThuoc=3"` (cổng thật hiển thị trong terminal).

Tạo migration (không mở CSDL khi công cụ chạy Program): `dotnet ef migrations add Ten -- --MigrateOnStartup false`
