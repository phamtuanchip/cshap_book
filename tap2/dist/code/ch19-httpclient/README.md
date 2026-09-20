# Chương 19 — Code mẫu: HttpClient và resilience

Yêu cầu: .NET SDK 10. Chạy trong thư mục này:

```
dotnet run
```

Mở địa chỉ hiển thị trong terminal (thường `http://localhost:5xxx`) bằng trình duyệt hoặc `curl`. Ứng dụng tự gọi chính nó như 'dịch vụ ngoài'; chạy với `--urls http://localhost:5219` hoặc đặt `ThoiTiet__BaseUrl` trùng cổng thật. Thử `/thoi-tiet/hanoi`, `/thoi-tiet-tran/hanoi`, `/cham`.
