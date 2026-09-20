# Chương 11 — Code mẫu: OpenTelemetry và health checks

Yêu cầu: .NET SDK 10. Chạy trong thư mục này:

```
dotnet run
```

Mở địa chỉ hiển thị trong terminal (thường `http://localhost:5xxx`) bằng trình duyệt hoặc `curl`. Chạy với `dotnet run --urls http://localhost:5250` (ứng dụng tự gọi chính nó để minh hoạ traceparent). Thử `POST /dat-hang/LT001?soLuong=2`, `/health/live`, `/health/ready`.
