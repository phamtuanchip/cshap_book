# Chương 11 — Code mẫu: Dự án tổng hợp: Trợ lý kho hàng

Yêu cầu: .NET SDK 10. Chạy trong thư mục này:

```
dotnet run
```

Mở địa chỉ hiển thị trong terminal (thường `http://localhost:5xxx`) bằng trình duyệt hoặc `curl`. Console app này tham chiếu TRỰC TIẾP các dự án Kho.Domain/Kho.Application/Kho.Infrastructure của `tap3/code/kho-clean` — không viết lại nghiệp vụ, chỉ thêm lớp AI (mô hình giả, không cần khoá API) gọi qua ISender thật trên một SQLite tạm. Test ở `../ch11-tro-ly-kho.Tests` (`dotnet test`, 6 test, không cần mô hình).
