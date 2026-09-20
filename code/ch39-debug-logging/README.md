# Chương 39 — Code mẫu: Debug và logging

Yêu cầu: .NET SDK 10. Chạy:

```
dotnet run
```

Đặt cấp độ log bằng biến môi trường (Windows PowerShell: `$env:LOG_LEVEL="Debug"`; Bash: `LOG_LEVEL=Debug dotnet run`).
Mặc định là `Information`.

Để thực hành debug: đặt breakpoint ở `TinhTong`, chạy `F5` trong IDE và dùng Step Into/Over, Watch, Call Stack.
