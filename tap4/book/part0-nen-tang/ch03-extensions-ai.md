# Chương 3 — Microsoft.Extensions.AI: IChatClient và middleware

## Mục tiêu học

Sau chương này, bạn sẽ:

- Dùng **`IChatClient`** — abstraction chuẩn của .NET cho LLM — để viết mã **không phụ thuộc nhà cung cấp**.
- Xây **pipeline middleware** (`ChatClientBuilder`, `DelegatingChatClient`): logging, cache, đếm token... giống middleware ASP.NET Core.
- Tạo **nhà cung cấp giả** để **kiểm thử** mà không gọi mạng, không tốn tiền.
- Đăng ký vào **DI** và chuyển đổi nhà cung cấp bằng cấu hình.

Code: [`code/ch03-extensions-ai/`](../../code/ch03-extensions-ai/) — gói `Microsoft.Extensions.AI` 10.10.0.

## Vấn đề: mỗi nhà cung cấp một SDK

OpenAI, Anthropic, Azure OpenAI, Google, Ollama (chạy cục bộ)... mỗi nơi có SDK và kiểu dữ liệu riêng. Mã ứng dụng gắn chặt với một SDK sẽ khó đổi nhà cung cấp, khó kiểm thử, và khó gắn các mối quan tâm xuyên suốt (log, cache, giới hạn, telemetry).

**`Microsoft.Extensions.AI`** giải quyết giống như `ILogger`/`IDistributedCache` đã làm: một **giao diện chung** (`Microsoft.Extensions.AI.Abstractions`) + **middleware dùng chung** (`Microsoft.Extensions.AI`); mỗi nhà cung cấp cung cấp một **cài đặt** `IChatClient`. Mã của bạn phụ thuộc vào giao diện; nhà cung cấp được chọn ở **composition root** (Tập 3, Chương 2).

## `IChatClient`

```csharp
public interface IChatClient : IDisposable
{
    Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default);
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default);
    object? GetService(Type serviceType, object? serviceKey = null);
}
```

Kiểu dữ liệu chính:

| Kiểu | Ý nghĩa |
|------|---------|
| `ChatMessage(ChatRole, text)` | một tin nhắn; `ChatRole.System/User/Assistant/Tool` |
| `ChatOptions` | `Temperature`, `MaxOutputTokens`, `ModelId`, `Tools`, `ResponseFormat`... (chung cho mọi nhà cung cấp) |
| `ChatResponse` | phản hồi: `Messages`, `.Text` (ghép văn bản), `Usage` (token vào/ra) |
| `ChatResponseUpdate` | một mảnh của luồng streaming (`.Text`) |

Dùng:

```csharp
var lichSu = new List<ChatMessage>
{
    new(ChatRole.System, "Ban la tro ly kho hang, tra loi ngan."),
    new(ChatRole.User, "LT001 con bao nhieu?"),
};
var r = await client.GetResponseAsync(lichSu, new ChatOptions { Temperature = 0, MaxOutputTokens = 200 });
Console.WriteLine(r.Text);

await foreach (var manh in client.GetStreamingResponseAsync(lichSu)) Console.Write(manh.Text);
```

Đổi nhà cung cấp chỉ là đổi cách tạo `IChatClient`, ví dụ (theo tài liệu từng gói; **chưa chạy trong sách này**):

```csharp
// OpenAI / Azure OpenAI:  new OpenAI.Chat.ChatClient(model, key).AsIChatClient()
// Ollama (cục bộ):        new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.2")
// Anthropic:              dùng SDK chính thức của Anthropic cho .NET (có bộ chuyển thành IChatClient) — xem tài liệu SDK hiện hành
```

Tên gói/phương thức chuyển đổi thay đổi theo phiên bản SDK; hãy tra tài liệu của nhà cung cấp bạn dùng.

## Pipeline middleware

`ChatClientBuilder` xếp các **`DelegatingChatClient`** thành chuỗi, y hệt middleware của ASP.NET Core: mỗi lớp bọc `inner`, có thể xử lý trước/sau hoặc chặn hẳn.

```csharp
IChatClient client = new ChatClientBuilder(nhaCungCap)
    .UseLogging(loggerFactory)                  // có sẵn: log request/response
    .Use(inner => new CacheClient(inner))       // tự viết
    .Use(inner => new DemTokenClient(inner))    // tự viết
    .Build();
```

Có sẵn trong gói: `UseLogging`, `UseOpenTelemetry` (span/metric theo chuẩn GenAI), `UseDistributedCache`, `UseFunctionInvocation` (tự chạy tool — Chương 5), `ConfigureOptions` (đặt mặc định cho `ChatOptions`), `UseRateLimiting`... (tuỳ phiên bản).

**Thứ tự quan trọng: cái thêm đầu tiên là ngoài cùng.** Ví dụ chạy được (nhà cung cấp giả, hai lần hỏi cùng câu):

```
--- Lan 1 ---  → log, cache trượt, đếm token, gọi (giả)
--- Lan 2 ---  [cache] trung   → dừng ở lớp cache, KHÔNG xuống đếm token/nhà cung cấp
Tong token da dung: 30 vao / 12 ra; so lan goi that: 1
```

Nếu đặt `DemTokenClient` **ngoài** `CacheClient`, lần cache trúng vẫn bị cộng token (60/24 với chỉ 1 lần gọi thật) — sai số liệu chi phí. Đây là một lỗi thật đã xảy ra khi viết mã mẫu: **luôn hỏi "lớp này muốn thấy mọi lời gọi hay chỉ lời gọi thật?"** rồi đặt đúng chỗ.

### Viết middleware

```csharp
sealed class DemTokenClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public static int TongVao, TongRa;

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        var res = await base.GetResponseAsync(messages, options, ct);          // gọi lớp bên trong
        TongVao += (int)(res.Usage?.InputTokenCount ?? 0);
        TongRa  += (int)(res.Usage?.OutputTokenCount ?? 0);
        return res;
    }
}
```

`DelegatingChatClient` mặc định chuyển tiếp mọi thứ; bạn chỉ override cái cần (`GetResponseAsync`, và **`GetStreamingResponseAsync` nếu muốn áp dụng cho streaming** — ví dụ trên chỉ đếm lời gọi không-streaming; với streaming, token nằm ở cập nhật cuối, cần xử lý riêng).

Bài học cache (đã cài trong `CacheClient`): **chỉ cache khi kết quả ổn định** (`Temperature = 0`), khoá phải gồm **toàn bộ hội thoại + mọi tham số ảnh hưởng** (model, max tokens, công cụ...), và **cẩn trọng dữ liệu theo người dùng** (không chia sẻ cache giữa người dùng nếu prompt chứa dữ liệu riêng — Tập 3, Chương 10). Trong sản phẩm dùng `UseDistributedCache` có sẵn thay vì tự viết.

## Nhà cung cấp giả để kiểm thử

`IChatClient` là giao diện nhỏ ⇒ tự cài một bản giả rất dễ:

```csharp
sealed class TroLyGia : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
    {
        string hoi = messages.Last(m => m.Role == ChatRole.User).Text;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"(gia lap) Ban hoi: \"{hoi}\". LT001 con 7 chiec."))
        {
            Usage = new UsageDetails { InputTokenCount = 30, OutputTokenCount = 12, TotalTokenCount = 42 },
        });
    }
    // GetStreamingResponseAsync: yield từng mảnh; GetService: trả this nếu đúng kiểu; Dispose: rỗng
}
```

Ứng dụng nhận `IChatClient` qua DI ⇒ trong test, bạn truyền bản giả có **kịch bản** (trả đúng câu bạn muốn, hoặc ném lỗi 429 để kiểm thử xử lý lỗi, hoặc trả yêu cầu gọi tool). Nhờ vậy **toàn bộ logic quanh LLM** (dựng prompt, phân tích đầu ra, xử lý lỗi, giới hạn) được **kiểm thử tất định, nhanh, miễn phí**. Việc kiểm tra *chất lượng* câu trả lời của mô hình thật là chuyện khác (Chương 9).

## Đăng ký vào DI (ASP.NET Core)

```csharp
builder.Services.AddChatClient(sp =>
        new ChatClientBuilder(TaoNhaCungCap(sp.GetRequiredService<IConfiguration>()))
            .UseLogging(sp.GetRequiredService<ILoggerFactory>())
            .UseOpenTelemetry()
            .Build(sp))
    .UseFunctionInvocation();

// dùng ở bất kỳ đâu:
app.MapPost("/hoi", async (HoiRequest r, IChatClient chat, CancellationToken ct)
    => (await chat.GetResponseAsync(r.CauHoi, cancellationToken: ct)).Text);
```

`AddChatClient` đăng ký `IChatClient` (mặc định singleton) và trả một builder để nối thêm middleware. Chọn nhà cung cấp theo **cấu hình** (`AI:Provider = fake|openai|ollama`), giữ khoá trong secret (Tập 3, Chương 13). Có `AddKeyedChatClient` nếu cần nhiều mô hình cùng lúc (mô hình rẻ cho tác vụ dễ, mô hình mạnh cho tác vụ khó).

> Chữ ký chính xác của `AddChatClient`/`Build(sp)` đã thay đổi giữa các bản xem trước; đoạn mã trên theo tài liệu phiên bản 9–10 và **chưa được biên dịch trong sách này** (mã chạy được của chương là ví dụ pipeline ở trên). Nếu trình biên dịch báo khác, hãy theo IntelliSense và ghi chú phát hành gói.

## Các lợi ích khác của abstraction

- **Telemetry chuẩn**: `UseOpenTelemetry()` phát span/metric theo **GenAI semantic conventions** (số token, mô hình, độ trễ) — nối thẳng vào Tập 3, Chương 11.
- **Nhúng (embeddings)**: `IEmbeddingGenerator<string, Embedding<float>>` cùng họ abstraction (Chương 6).
- **Tool calling** bằng `AIFunction`/`AIFunctionFactory` (Chương 5) — dùng chung cho mọi nhà cung cấp hỗ trợ.
- Cộng đồng/hệ sinh thái: **Semantic Kernel**, **Microsoft Agent Framework** xây trên các abstraction này (Chương 8).

## Lỗi thường gặp

- Dùng thẳng SDK của một nhà cung cấp rải khắp mã → khó đổi và khó test.
- Sai thứ tự middleware (cache trong/ngoài đếm token, log trong/ngoài retry).
- Cache kết quả có `Temperature > 0` hoặc khoá thiếu tham số → trả nhầm câu trả lời.
- Chỉ override `GetResponseAsync`, quên `GetStreamingResponseAsync` → streaming bỏ qua middleware.
- Đăng ký `IChatClient` scoped/transient tạo lại kết nối liên tục (thường dùng singleton).
- Log toàn bộ nội dung (mức Trace) ở production → rò rỉ dữ liệu.

## Bài tập

1. Viết `GioiHanDoDaiClient` từ chối (ném `InvalidOperationException`) khi tổng ký tự hội thoại vượt ngưỡng; test bằng nhà cung cấp giả.
2. Viết `TuThuLaiClient` thử lại khi client bên trong ném lỗi tạm thời (giả 2 lần lỗi rồi thành công); test số lần gọi.
3. Bổ sung `GetStreamingResponseAsync` cho `DemTokenClient` (cộng token ở cập nhật cuối có `UsageContent`).
4. Viết `TroLyGiaCoKichBan` nhận danh sách phản hồi định sẵn (theo thứ tự) và dùng nó để test một lớp `TroLyKho` (dựng prompt + gọi + xử lý `stop_reason` bị cắt).
5. Thay `CacheClient` bằng `UseDistributedCache` với `MemoryDistributedCache` và so sánh hành vi (khoá cache, hết hạn).
