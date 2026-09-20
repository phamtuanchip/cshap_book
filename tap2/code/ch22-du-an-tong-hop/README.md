# Chương 22 — Dự án tổng hợp Tập 2: Web quản lý kho

```
cd QuanLyKhoWeb
dotnet run                          # tự migrate + dữ liệu mẫu; mở trang chủ và /scalar/v1
dotnet test ../QuanLyKhoWeb.Tests   # 14 test tích hợp (SQLite file tạm)
```

Tài khoản mẫu: `admin / Admin@123`, `nhanvien / NhanVien@123`. Khoá JWT phát triển tự điền ở môi trường Development;
production đặt biến môi trường `Jwt__Khoa` (≥ 32 ký tự).
