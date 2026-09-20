# Chương 21 — Code mẫu: Bảo mật ứng dụng web

Yêu cầu: .NET SDK 10. Chạy trong thư mục này:

```
dotnet run
```

Mở địa chỉ hiển thị trong terminal (thường `http://localhost:5xxx`) bằng trình duyệt hoặc `curl`. Thử `/xss-sai?ten=<script>…`, `/xss-dung`, `/don-hang/1` (header `X-Demo-User: an`), `/chuyen-huong?url=…`; gọi `POST /dang-nhap` 5 lần để thấy 429.
