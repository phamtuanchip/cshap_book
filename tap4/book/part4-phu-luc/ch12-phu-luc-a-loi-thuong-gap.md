# Phụ lục A — Lỗi và bẫy thường gặp

Bảng tra nhanh cho Tập 4. Số trong ngoặc là chương liên quan.

## Nền tảng và gọi API (1–3)

| Bẫy | Hậu quả | Cách xử lý |
|-----|---------|-----------|
| Coi LLM là hàm tất định; `Assert.Equal` trên văn bản | test chập chờn | đánh giá theo ngưỡng (9) |
| Tin đầu ra cho quyết định quan trọng | hallucination gây hại | nguồn thật (RAG), kiểm chứng, người duyệt |
| Không gửi lại lịch sử hội thoại | mô hình "quên" | giữ `List<ChatMessage>`, nhớ thêm câu trả lời (1) |
| Gửi lịch sử vô hạn | tràn ngữ cảnh, tốn tiền | cắt/tóm tắt (1) |
| Không đặt `max_tokens` | API từ chối hoặc chi phí không kiểm soát | luôn đặt trần (2) |
| Bỏ qua `stop_reason = max_tokens` | trả câu cụt lủn | phát hiện và xử lý (2) |
| Coi `content` là chuỗi | mất khối `tool_use` | duyệt mảng khối (2) |
| Streaming không dùng `ResponseHeadersRead` | chờ hết câu mới thấy chữ | (2) |
| Thử lại cả 4xx, không backoff/jitter | lãng phí, "bão thử lại" | chỉ 429/5xx, có `Retry-After` (2) |
| Quên `CancellationToken` | tiếp tục sinh (và trả tiền) khi người dùng đã đi | truyền xuyên suốt |
| `new HttpClient()` mỗi lần; khoá API trong mã | cạn socket; lộ khoá | `IHttpClientFactory`; secret/Key Vault |
| Gọi API từ client (trình duyệt/mobile) | lộ khoá | luôn qua backend |
| Sai thứ tự middleware (đếm token ngoài cache) | số liệu chi phí sai (đã gặp: 60/24 thay vì 30/12) | hỏi "lớp này cần thấy mọi lời gọi hay chỉ lời gọi thật?" (3) |
| Chỉ override `GetResponseAsync` | streaming bỏ qua middleware | override cả hai (3) |
| Cache khi `Temperature > 0` hoặc khoá thiếu tham số | trả nhầm câu trả lời | cache có điều kiện, khoá đầy đủ (3) |
| Log ở mức Trace ở production | rò rỉ nội dung hội thoại | Debug trở xuống (3, 10) |

## Kỹ thuật cốt lõi (4–7)

| Bẫy | Cách xử lý |
|-----|-----------|
| Tin JSON "hợp lệ" mà không kiểm quy tắc nghiệp vụ | kiểm chứng bằng chính quy tắc miền (4) |
| Trường bắt buộc không có lối "không biết" → mô hình bịa | cho phép `null`/enum `KhongRo` (4) |
| Vòng lặp sửa lỗi không giới hạn | tối đa 1–2 lần; thất bại là kết quả hợp lệ (4) |
| "Sửa ngầm" dữ liệu sai bằng đoán | báo lỗi, đừng đổi nghĩa (4) |
| Mô tả công cụ mơ hồ; quá nhiều công cụ chồng lấn | 5–15 công cụ, mô tả nói rõ khi nào dùng/không dùng (5) |
| Công cụ ghi chạy thẳng theo ý mô hình | đề xuất → người duyệt (5, 8) |
| Không kiểm chứng đầu vào **trong** công cụ | tham số của mô hình = đầu vào không tin cậy (5) |
| Chạy công cụ bằng quyền dịch vụ | quyền của người dùng đang hỏi (5) |
| Quên `MaximumIterationsPerRequest` | vòng lặp công cụ vô hạn (5) |
| So embedding của hai mô hình khác nhau | cùng một mô hình cho cả tài liệu và truy vấn (6) |
| Không đặt ngưỡng điểm | trả kết quả "gần nhất" không liên quan (6) |
| Không lọc quyền truy cập trong truy xuất | rò rỉ dữ liệu giữa người dùng/tenant (6, 7) |
| Chia đoạn quá lớn/nhỏ; không cập nhật khi nguồn đổi | dữ liệu lỗi thời (6) |
| Nhét cả tài liệu vào prompt "cho chắc" | tốn tiền, mô hình lạc hướng (7) |
| Không yêu cầu trích nguồn | không kiểm chứng được (7) |
| Tin "có RAG là không sai" | vẫn cần đánh giá truy xuất và câu trả lời (7) |

## Sản xuất (8–10)

- **Agent**: không giới hạn bước/thời gian/chi phí; cho hành động ghi thật từ đầu; không lưu vết từng bước; dùng agent cho quy trình vốn tất định (8).
- **Đánh giá**: golden set nhỏ/toàn ca dễ; dùng độ tương tự văn bản để bắt lỗi số liệu (đã gặp thật: "50 dòng" vs "20 dòng" lọt qua); tin LLM-as-judge mà không kiểm định bằng người; sửa prompt không chạy lại đánh giá (9).
- **An toàn**: nghĩ "dặn trong system prompt" là đủ chống injection; danh sách từ khoá là lớp phòng thủ duy nhất; gửi PII không cần thiết; không có ngân sách/giới hạn tốc độ; không có cách tắt nhanh (10).
- **Chi phí**: tính bằng ước lượng thay vì `Usage`; chỉ theo dõi CPU/RAM mà quên chi phí, tỉ lệ từ chối, độ trễ mô hình (10).

## Nguyên tắc gỡ lỗi ứng dụng LLM

1. Tách **phần tất định** (dựng prompt, parse, công cụ) khỏi **phần mô hình**; kiểm thử phần đầu bằng `IChatClient` giả.
2. Ghi lại **toàn bộ vết**: prompt gửi đi, phản hồi, công cụ, kết quả, token, mô hình/phiên bản prompt.
3. Tái hiện bằng `Temperature = 0` và cùng đầu vào; vẫn không đảm bảo giống hệt.
4. Đổi **một** thứ mỗi lần (prompt, mô hình, tham số) và **đo lại** bằng golden set.
5. Nghi ngờ **dữ liệu đưa vào ngữ cảnh** (truy xuất kém, kết quả công cụ lạ) trước khi đổ lỗi cho mô hình.
6. Nếu môi trường (chính sách hệ điều hành, hạn mức API) chặn, báo người có quyền — đừng lách.
