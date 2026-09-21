using System.Net;
using System.Text;

// Mac dinh chay voi "may chu gia" (khong can khoa API, khong ton tien). Dat ANTHROPIC_API_KEY de goi that.
string? khoa = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
string model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-sonnet-5";

HttpClient http;
if (!string.IsNullOrEmpty(khoa))
{
    Console.WriteLine("== Che do THAT: goi api.anthropic.com ==");
    http = new HttpClient { BaseAddress = new Uri("https://api.anthropic.com/"), Timeout = TimeSpan.FromSeconds(60) };
    http.DefaultRequestHeaders.Add("x-api-key", khoa);                           // khoa lay tu bien moi truong, KHONG viet vao ma
}
else
{
    Console.WriteLine("== Che do GIA (khong co ANTHROPIC_API_KEY) ==");
    http = new HttpClient(new MayChuGia()) { BaseAddress = new Uri("https://api.anthropic.com/") };
}

var claude = new ClaudeClient(http, model);

Console.WriteLine("\n[1] Hoi mot lan");
var kq = await claude.HoiAsync("Ban la tro ly kho hang. Tra loi ngan gon bang tieng Viet.", "San pham LT001 con bao nhieu?");
Console.WriteLine($"   {kq.VanBan}\n   dung: {kq.LyDoDung}, token vao/ra: {kq.TokenVao}/{kq.TokenRa}");

Console.WriteLine("\n[2] Streaming");
Console.Write("   ");
await foreach (var manh in claude.HoiStreamAsync("Tra loi ngan.", "Chao ban"))
    Console.Write(manh);
Console.WriteLine();

if (string.IsNullOrEmpty(khoa))
{
    Console.WriteLine("\n[3] May chu gia tra 429 hai lan roi thanh cong -> thu lai co lui");
    MayChuGia.SoLan429 = 2;
    var kq3 = await claude.HoiAsync("s", "u");
    Console.WriteLine($"   {kq3.VanBan}");

    Console.WriteLine("\n[4] Loi 401 (sai khoa) -> KHONG thu lai, nem loi ro rang");
    MayChuGia.Tra401 = true;
    try { await claude.HoiAsync("s", "u"); }
    catch (HttpRequestException e) { Console.WriteLine($"   {e.StatusCode}: {e.Message}"); }
}

// ---------------- May chu gia: bat chuoc dinh dang phan hoi cua Messages API ----------------
sealed class MayChuGia : HttpMessageHandler
{
    public static int SoLan429;
    public static bool Tra401;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (Tra401) { Tra401 = false; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{\"type\":\"error\",\"error\":{\"type\":\"authentication_error\",\"message\":\"invalid x-api-key\"}}") }); }
        if (SoLan429 > 0)
        {
            SoLan429--;
            var r = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{\"type\":\"error\"}") };
            r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(100));
            return Task.FromResult(r);
        }

        string body = req.Content!.ReadAsStringAsync(ct).Result;
        bool stream = body.Contains("\"stream\":true");
        if (stream)
        {
            var sse = new StringBuilder();
            sse.Append("event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"id\":\"msg_1\",\"role\":\"assistant\"}}\n\n");
            sse.Append("event: content_block_start\ndata: {\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"text\",\"text\":\"\"}}\n\n");
            foreach (var manh in new[] { "Xin ", "chao! ", "Toi co the ", "giup gi?" })
                sse.Append($"event: content_block_delta\ndata: {{\"type\":\"content_block_delta\",\"index\":0,\"delta\":{{\"type\":\"text_delta\",\"text\":\"{manh}\"}}}}\n\n");
            sse.Append("event: content_block_stop\ndata: {\"type\":\"content_block_stop\",\"index\":0}\n\n");
            sse.Append("event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(sse.ToString(), Encoding.UTF8, "text/event-stream") });
        }

        const string json = "{\"id\":\"msg_1\",\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"LT001 con 7 chiec (gia lap).\"}],\"stop_reason\":\"end_turn\",\"usage\":{\"input_tokens\":32,\"output_tokens\":14}}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
