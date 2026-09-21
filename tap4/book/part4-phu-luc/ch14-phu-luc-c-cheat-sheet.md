# Phụ lục C — Cheat sheet

## Bản đồ nhanh: cần gì thì dùng gì

| Bạn cần... | Dùng | Chương |
|-----------|------|--------|
| Gọi LLM, không phụ thuộc nhà cung cấp | `IChatClient` | 3 |
| Log/cache/đếm token/ngân sách xuyên suốt | `ChatClientBuilder` + `DelegatingChatClient` | 3, 10 |
| Test không gọi mạng | `IChatClient` giả có kịch bản | 3, 9 |
| Đối tượng C# từ văn bản | `GetResponseAsync<T>` + kiểm chứng + sửa lỗi có giới hạn | 4 |
| Cho mô hình gọi hàm | `AIFunctionFactory` + `UseFunctionInvocation` | 5 |
| Tìm theo nghĩa | embeddings + cosine + ngưỡng + lọc siêu dữ liệu | 6 |
| Trả lời trên dữ liệu riêng | RAG: truy xuất → prompt → sinh + trích nguồn | 7 |
| Nhiều bước, chưa biết trước | vòng lặp agent + giới hạn bước | 8 |
| Đo chất lượng | golden set + ngưỡng tỉ lệ đạt + giám khảo + người | 9 |
| Chống lạm dụng/chi phí | lọc, che PII, ngân sách, giới hạn tốc độ | 10 |

## Khung gọi API thô (Messages API)

```http
POST /v1/messages
x-api-key: <khoá từ biến môi trường>
anthropic-version: 2023-06-01

{ "model": "...", "max_tokens": 512, "system": "...",
  "messages": [ { "role": "user", "content": "..." } ] }
```

- Phản hồi: `content[]` (khối `text`/`tool_use`), `stop_reason` (`end_turn`, `max_tokens`, `tool_use`), `usage`.
- Streaming: `"stream": true`, đọc SSE, `HttpCompletionOption.ResponseHeadersRead`.
- Lỗi: thử lại **429/5xx** (backoff + jitter + `Retry-After`); **4xx còn lại không thử lại**.

## Mẫu mã

```csharp
// Pipeline (đầu tiên = ngoài cùng)
IChatClient client = new ChatClientBuilder(nhaCungCap)
    .UseLogging(loggerFactory)
    .Use(inner => new CacheClient(inner))          // trước đếm token: cache trúng thì không đếm
    .Use(inner => new DemTokenClient(inner))
    .UseFunctionInvocation(c => c.MaximumIterationsPerRequest = 5)
    .Build();

// Công cụ
AIFunctionFactory.Create(kho.TraTon, "tra_ton_kho");        // [Description] trên phương thức và tham số

// Có cấu trúc
var r = await client.GetResponseAsync<PhieuNhap>(tinNhan);   // r.Result

// Embedding
var v = (await gen.GenerateAsync([van]))[0].Vector.ToArray();
float diem = TensorPrimitives.CosineSimilarity(a, b);
```

## Quy tắc thiết kế

1. **LLM đề xuất, ứng dụng quyết định.** Mọi hành động đi qua mã của bạn với quyền của người dùng.
2. **Công cụ ghi = đề xuất + người duyệt**; công cụ đọc mới được tự chạy.
3. **Tham số của mô hình = đầu vào không tin cậy**; kiểm chứng trong công cụ bằng quy tắc miền.
4. **Dữ liệu ngoài (tài liệu, kết quả công cụ, đánh giá người dùng) không phải chỉ dẫn**; bọc, dò, giới hạn quyền.
5. **Giới hạn mọi thứ**: `max_tokens`, số vòng công cụ, số bước agent, thời gian, ngân sách, tốc độ.
6. **Không tin đầu ra**: parse → kiểm chứng nghiệp vụ → thất bại có kiểm soát.
7. **Ngưỡng liên quan** trong truy xuất; "không biết" là câu trả lời hợp lệ.
8. **Lọc quyền truy cập ở tầng truy vấn**, không dựa vào điểm số.
9. **Test phần tất định 100%; phần mô hình đo bằng tỉ lệ đạt**; kiểm số liệu/sự kiện riêng, đừng chỉ dựa độ tương tự văn bản.
10. **Prompt là mã**: lưu kho, đặt phiên bản, kiểm thử hồi quy, đổi thì chạy lại đánh giá.
11. **Đo chi phí từ `Usage` thực tế**; ngân sách theo người dùng; log có cấu trúc, che PII.
12. **Có cách tắt nhanh** (feature flag) và vết đầy đủ để điều tra.

## Danh sách kiểm tra trước khi phát hành tính năng AI

- [ ] Khoá API trong secret; gọi qua backend; hạn mức chi tiêu ở nhà cung cấp.
- [ ] `max_tokens`, timeout, `CancellationToken`, retry đúng loại lỗi.
- [ ] Công cụ: phân loại rủi ro; ghi = đề xuất; kiểm chứng đầu vào; quyền người dùng; giới hạn vòng lặp.
- [ ] Đầu ra có cấu trúc được kiểm chứng; thất bại được xử lý.
- [ ] RAG: ngưỡng, trích nguồn, lọc quyền, cập nhật khi nguồn đổi.
- [ ] Golden set + ngưỡng tỉ lệ đạt; đã chạy trên **mô hình thật**.
- [ ] Prompt injection: dữ liệu ngoài bị bọc/dò; quyền tối thiểu.
- [ ] PII được che ở log; dữ liệu gửi đi được phép.
- [ ] Ngân sách/giới hạn tốc độ; cảnh báo chi phí, tỉ lệ từ chối, độ trễ.
- [ ] Trace/log/metric; feature flag; runbook khi mô hình lỗi hoặc chất lượng giảm.

## Lệnh thường dùng

```bash
dotnet add package Microsoft.Extensions.AI
dotnet add package System.Numerics.Tensors
dotnet test                                # phần tất định + đánh giá bằng bộ giả
ANTHROPIC_API_KEY=... dotnet run           # chế độ thật (có phí)
```
