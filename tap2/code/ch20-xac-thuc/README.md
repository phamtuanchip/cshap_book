# Chương 20 — Code mẫu: Xác thực và phân quyền

Yêu cầu: .NET SDK 10. Chạy trong thư mục này:

```
dotnet run
```

Mở địa chỉ hiển thị trong terminal (thường `http://localhost:5xxx`) bằng trình duyệt hoặc `curl`. Đăng nhập thử: `curl -X POST /api/token -H 'Content-Type: application/json' -d '{"tenDangNhap":"an","matKhau":"An@12345"}'` (tài khoản: admin/Admin@123, an/An@12345, be/Be@12345).
