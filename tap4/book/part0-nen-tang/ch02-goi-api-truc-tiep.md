# Chương 2 — Gọi API LLM bằng HttpClient

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **cấu trúc request/response** của một API chat LLM (lấy Messages API của Anthropic làm ví dụ) và tự gọi được bằng `HttpClient`.
- Xử lý **streaming** bằng **Server-Sent Events (SSE)** với `IAsyncEnumerable<string>`.
- Cài **thử lại có lùi (backoff + jitter)**, tôn trọng `Retry-After`, và phân biệt lỗi nên/không nên thử lại.
- Biết vì sao thực tế nên dùng **SDK/abstraction** (Chương 3) — nhưng hiểu bản chất giúp bạn gỡ lỗi.

Code: [`code/ch02-goi-api-truc-tiep/`](../../code/ch02-goi-api-truc-tiep/) — chạy mặc định với **máy chủ giả** (không khoá API, không tốn tiền); đặt `ANTHROPIC_API_KEY` để gọi thật.

> **Trung thực về kiểm chứng:** mọi kết quả dưới đây là từ **máy chủ giả** do chính code mẫu dựng theo định dạng tài liệu Messages API. Việc gọi `api.anthropic.com` thật **chưa được chạy** khi viết sách (không có khoá API). Định dạng có thể thay đổi; luôn đối chiếu tài liệu hiện hành (đặc biệt tên mô hình, phiên bản API, trường mới).

## Request

Mọi dịch vụ LLM hiện nay đều là **HTTP + JSON**. Với Messages API:

```http
POST https://api.anthropic.com/v1/messages
x-api-key: <khoá>
anthropic-version: 2023-06-01
content-type: application/json

{
  "model": "claude-sonnet-5",
  "max_tokens": 512,
  "system": "Ban la tro ly kho hang. Tra loi ngan gon bang tieng Viet.",
  "messages": [
    { "role": "user", "content": "San pham LT001 con bao nhieu?" }
  ]
}
```

Điểm cần nhớ:

- **Xác thực bằng header** (`x-api-key`) — khoá là **bí mật**: đọc từ biến môi trường/Key Vault (Tập 3, Chương 13), **không** commit, không log, không gửi xuống trình duyệt. Ứng dụng web/mobile gọi **qua backend của bạn**, không gọi thẳng từ client.
- **`max_tokens` bắt buộc**: trần số token sinh ra.
- **`system`** là tham số riêng; `messages` xen kẽ `user`/`assistant`.
- **`model`** là chuỗi định danh mô hình; đặt trong cấu hình, không hard-code rải rác (mô hình ra mới/ngừng hỗ trợ theo thời gian).
- Không có "phiên": mỗi request phải mang **toàn bộ hội thoại** (Chương 1).

## Response

```json
{
  "id": "msg_01...",
  "type": "message",
  "role": "assistant",
  "content": [ { "type": "text", "text": "LT001 con 7 chiec." } ],
  "stop_reason": "end_turn",
  "usage": { "input_tokens": 32, "output_tokens": 14 }
}
```

- **`content` là mảng các khối** (`text`, `tool_use`, ...) — không phải một chuỗi. Lấy chữ bằng cách ghép các khối `text`. (Khối `tool_use` là nền tảng của Chương 5.)
- **`stop_reason`**: `end_turn` (xong), `max_tokens` (bị cắt do trần — câu trả lời **dở dang**, phải xử lý!), `tool_use` (mô hình muốn gọi công cụ), `stop_sequence`...
- **`usage`**: số token thực tế — nguồn duy nhất để tính chi phí chính xác (Chương 10).

## Client tối thiểu

```csharp
public sealed class ClaudeClient(HttpClient http, string model, int toiDaThuLai = 3)
{
    public async Task<KetQua> HoiAsync(string heThong, string nguoiDung, int toiDaToken = 512, CancellationToken ct = default)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = toiDaToken,
            ["system"] = heThong,
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = nguoiDung }),
        };

        using var res = await GuiCoThuLaiAsync(body, ct);
        var json = await res.Content.ReadFromJsonAsync<JsonNode>(ct) ?? throw new InvalidOperationException("Phan hoi rong");
        string van = string.Concat(json["content"]!.AsArray()
            .Where(k => k!["type"]!.GetValue<string>() == "text").Select(k => k!["text"]!.GetValue<string>()));
        return new(van, json["stop_reason"]!.GetValue<string>(), json["usage"]!["input_tokens"]!.GetValue<int>(), json["usage"]!["output_tokens"]!.GetValue<int>());
    }
}
```

Dùng `JsonNode` cho mã minh hoạ để thấy rõ cấu trúc; sản phẩm thật dùng **record + `System.Text.Json`** (Tập 2) hoặc SDK. `HttpClient` **đăng ký bằng `IHttpClientFactory`** (Tập 2, Chương 19), đặt `BaseAddress`, header và timeout — đừng `new HttpClient()` cho mỗi lời gọi.

Chạy (máy chủ giả):

```
[1] Hoi mot lan
   LT001 con 7 chiec (gia lap).
   dung: end_turn, token vao/ra: 32/14
```

## Streaming (SSE)

Câu trả lời dài có thể mất hàng chục giây. Với `"stream": true`, server trả **Server-Sent Events**: dòng `event:` và `data:` cách nhau bằng dòng trống, mỗi sự kiện một mẩu:

```text
event: content_block_delta
data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Xin "}}

event: message_stop
data: {"type":"message_stop"}
```

```csharp
using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);   // KHÔNG đợi hết body
using var reader = new StreamReader(await res.Content.ReadAsStreamAsync(ct));
while ((dong = await reader.ReadLineAsync(ct)) is not null)
{
    if (!dong.StartsWith("data: ")) continue;
    var sk = JsonNode.Parse(dong[6..])!;
    ...
    case "content_block_delta" when loaiDelta == "text_delta": yield return sk["delta"]!["text"]!.GetValue<string>(); break;
    case "message_stop": yield break;
}
```

Ba điểm mấu chốt:

1. **`HttpCompletionOption.ResponseHeadersRead`** — mặc định `HttpClient` chờ đọc **toàn bộ** body rồi mới trả; với streaming bạn cần nhận ngay khi có header.
2. Phương thức là **`IAsyncEnumerable<string>`** (Tập 1, bất đồng bộ) — người gọi dùng `await foreach`, in/đẩy tới trình duyệt ngay từng mảnh.
3. **Lỗi có thể đến giữa luồng** (sự kiện `error`), khi đã trả 200. Phải xử lý, không chỉ kiểm tra mã trạng thái ban đầu. Truyền `CancellationToken` để người dùng đóng tab thì **huỷ** lời gọi (khỏi tốn token).

Trong ASP.NET Core, có thể chuyển tiếp tới trình duyệt bằng `Results.ServerSentEvents(stream)` (.NET 10) hoặc `IAsyncEnumerable` trả trực tiếp từ endpoint (Tập 2).

Kết quả (máy chủ giả): `Xin chao! Toi co the giup gi?` in dần từng mảnh.

## Lỗi và thử lại

| Mã | Nghĩa | Thử lại? |
|----|-------|----------|
| 400 | request sai (thiếu `max_tokens`, JSON hỏng, vượt ngữ cảnh) | **Không** — sửa mã |
| 401/403 | sai khoá / không có quyền | **Không** |
| 404 | sai mô hình/đường dẫn | **Không** |
| 413/422 | quá lớn / không xử lý được | **Không** |
| **429** | vượt giới hạn tốc độ | **Có**, theo `Retry-After` |
| **5xx**, 529 (quá tải) | lỗi tạm thời phía dịch vụ | **Có** |
| timeout / mất mạng | không rõ đã xử lý chưa | **Có**, cẩn trọng (LLM chỉ sinh văn bản nên thử lại an toàn; nhưng với tool có tác dụng phụ xem Chương 5) |

```csharp
for (int lan = 0; ; lan++)
{
    var res = await http.SendAsync(TaoRequest(body), ct);
    if (res.IsSuccessStatusCode) return res;

    bool thuLaiDuoc = res.StatusCode == HttpStatusCode.TooManyRequests || (int)res.StatusCode is 500 or 502 or 503 or 529;
    if (!thuLaiDuoc || lan >= toiDaThuLai) { /* nem loi kem noi dung phan hoi */ }

    var cho = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(200 * Math.Pow(2, lan) + Random.Shared.Next(0, 100));   // lũy thừa + jitter
    await Task.Delay(cho, ct);
}
```

- **Lùi lũy thừa + jitter**: nhiều client cùng bị 429 mà cùng thử lại một lúc sẽ lại đụng nhau; jitter ngẫu nhiên phân tán.
- **Tôn trọng `Retry-After`** khi server chỉ định.
- **Giới hạn số lần** và **tổng thời gian**; ném lỗi rõ ràng kèm thân phản hồi (chứa lý do).
- Trong sản phẩm, dùng **`Microsoft.Extensions.Http.Resilience`** (Tập 2, Chương 19: retry, circuit breaker, timeout) thay vì tự viết vòng lặp như trên.

Kết quả (máy chủ giả): hai lần 429 → hai lần chờ 100 ms → thành công; còn 401 → **ném lỗi ngay, không thử lại**.

## Giới hạn tốc độ và đồng thời

Dịch vụ giới hạn theo **yêu cầu/phút** và **token/phút**. Nếu ứng dụng gọi song song nhiều lời gọi (ví dụ xử lý hàng loạt tài liệu), hãy **giới hạn độ song song** (`SemaphoreSlim`, `Parallel.ForEachAsync(..., new ParallelOptions { MaxDegreeOfParallelism = 4 })`) hoặc dùng `System.Threading.RateLimiting` (Tập 2). Với xử lý hàng loạt không cần kết quả tức thì, nhiều nhà cung cấp có **Batch API** rẻ hơn.

## Bảo mật khi gọi API

- Khoá ở **backend**, đọc từ cấu hình bí mật; xoay vòng; mỗi môi trường một khoá; đặt hạn mức chi tiêu tại nhà cung cấp.
- **Đừng log** khoá hay toàn bộ prompt nếu chứa dữ liệu nhạy cảm (Chương 10).
- Kiểm soát dữ liệu gửi đi: **tối thiểu hoá** và che thông tin cá nhân khi không cần.
- Đặt **timeout** rõ ràng (LLM chậm hơn API thường: 30–120 giây tuỳ tác vụ; streaming dùng timeout theo khoảng lặng).

## Lỗi thường gặp

- Không đặt `max_tokens` (API từ chối) hoặc đặt quá thấp → `stop_reason = "max_tokens"` bị bỏ qua, trả câu cụt lủn cho người dùng.
- Coi `content` là chuỗi; nhận `tool_use` mà không xử lý.
- Streaming mà không `ResponseHeadersRead` → người dùng chờ hết cả câu.
- Thử lại cả lỗi 4xx (lãng phí và có thể tốn tiền), hoặc thử lại không lùi/jitter → "bão thử lại".
- Quên `CancellationToken` → tiếp tục sinh (và trả tiền) khi người dùng đã bỏ đi.
- Tạo `new HttpClient()` mỗi lần (cạn socket); hard-code tên mô hình và khoá.
- Đưa khoá API vào ứng dụng phía client.

## Bài tập

1. Thêm phương thức nhận **danh sách tin nhắn** (`user`/`assistant` xen kẽ) và test với máy chủ giả rằng toàn bộ lịch sử được gửi đi.
2. Xử lý `stop_reason = "max_tokens"`: ném ngoại lệ hoặc trả cờ `BiCat` cho người gọi; viết test bằng máy chủ giả.
3. Đăng ký `ClaudeClient` bằng `IHttpClientFactory` + `AddStandardResilienceHandler()` và bỏ vòng thử lại tự viết; so sánh hành vi với 429.
4. Viết endpoint ASP.NET Core `GET /hoi?q=...` phát streaming về trình duyệt bằng SSE và thử bằng `curl -N`; khi client ngắt, xác nhận `CancellationToken` được kích hoạt.
5. (Cần khoá API) Đặt `ANTHROPIC_API_KEY`, chạy chế độ thật, và ghi lại số token vào/ra cho một câu hỏi tiếng Việt và bản dịch tiếng Anh của nó; nhận xét chênh lệch.
