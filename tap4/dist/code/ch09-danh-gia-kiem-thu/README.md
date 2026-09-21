# Chương 9 — Code mẫu: Đánh giá và kiểm thử ứng dụng LLM

Yêu cầu: .NET SDK 10. Chạy trong thư mục này:

```
dotnet test
```

Không cần khoá API — mọi `IChatClient` trong bài là bản giả (`TroLyOnDinh`, `TroLyThucTe`, `TroLyKhongOnDinh`, `GiamKhaoGia`), được viết để minh hoạ **cách kiểm thử** hệ thống LLM, không phải chất lượng của một mô hình thật.

11 test chia 5 nhóm: logic tất định quanh mô hình, đánh giá theo ngưỡng tỉ lệ đạt, kiểm thử thống kê cho hệ thống không xác định, LLM làm giám khảo, và kiểm thử hồi quy prompt.
