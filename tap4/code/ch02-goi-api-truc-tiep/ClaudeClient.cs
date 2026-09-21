using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

// Client toi thieu cho Messages API cua Anthropic, viet bang HttpClient de THAY tan cung "co gi tren day day".
// Thuc te nen dung SDK chinh thuc hoac Microsoft.Extensions.AI (Chuong 3); phan nay de hieu ban chat.
public sealed class ClaudeClient(HttpClient http, string model, int toiDaThuLai = 3)
{
    public record KetQua(string VanBan, string LyDoDung, int TokenVao, int TokenRa);

    public async Task<KetQua> HoiAsync(string heThong, string nguoiDung, int toiDaToken = 512, CancellationToken ct = default)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = toiDaToken,                     // BAT BUOC: tran so token sinh ra
            ["system"] = heThong,                            // "system" la tham so rieng, KHONG nam trong messages
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = nguoiDung }),
        };

        using var res = await GuiCoThuLaiAsync(body, ct);
        var json = await res.Content.ReadFromJsonAsync<JsonNode>(ct) ?? throw new InvalidOperationException("Phan hoi rong");

        // content la MANG cac khoi (text, tool_use...). Chi ghep khoi "text".
        string van = string.Concat(json["content"]!.AsArray().Where(k => k!["type"]!.GetValue<string>() == "text").Select(k => k!["text"]!.GetValue<string>()));
        return new(van, json["stop_reason"]!.GetValue<string>(), json["usage"]!["input_tokens"]!.GetValue<int>(), json["usage"]!["output_tokens"]!.GetValue<int>());
    }

    // Streaming: server tra Server-Sent Events; moi su kien "content_block_delta" mang mot manh van ban
    public async IAsyncEnumerable<string> HoiStreamAsync(string heThong, string nguoiDung, int toiDaToken = 512, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = toiDaToken,
            ["stream"] = true,
            ["system"] = heThong,
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = nguoiDung }),
        };

        using var req = TaoRequest(body);
        using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);      // KHONG doi het body
        await KiemTraAsync(res, ct);

        using var reader = new StreamReader(await res.Content.ReadAsStreamAsync(ct));
        string? dong;
        while ((dong = await reader.ReadLineAsync(ct)) is not null)
        {
            if (!dong.StartsWith("data: ")) continue;                       // bo dong "event: ..." va dong trong
            var sk = JsonNode.Parse(dong[6..]) ?? throw new InvalidOperationException("Su kien SSE rong");
            string? loai = sk["type"]?.GetValue<string>();
            string? loaiDelta = sk["delta"]?["type"]?.GetValue<string>();
            switch (loai)
            {
                case "content_block_delta" when loaiDelta == "text_delta":
                    yield return sk["delta"]!["text"]!.GetValue<string>();
                    break;
                case "error":
                    throw new InvalidOperationException("Loi giua luong: " + sk["error"]?["message"]);
                case "message_stop":
                    yield break;
            }
        }
    }

    private HttpRequestMessage TaoRequest(JsonObject body) => new(HttpMethod.Post, "v1/messages")
    {
        Content = JsonContent.Create(body),
        Headers = { { "anthropic-version", "2023-06-01" } },
    };

    // Thu lai co lui (backoff) voi 429 (qua tai) / 5xx tam thoi; ton trong header Retry-After. Loi 4xx khac KHONG thu lai.
    private async Task<HttpResponseMessage> GuiCoThuLaiAsync(JsonObject body, CancellationToken ct)
    {
        for (int lan = 0; ; lan++)
        {
            using var req = TaoRequest(body);
            var res = await http.SendAsync(req, ct);
            if (res.IsSuccessStatusCode) return res;

            bool thuLaiDuoc = res.StatusCode == HttpStatusCode.TooManyRequests || (int)res.StatusCode is 500 or 502 or 503 or 529;
            if (!thuLaiDuoc || lan >= toiDaThuLai)
            {
                try { await KiemTraAsync(res, ct); } finally { res.Dispose(); }
            }

            var cho = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(200 * Math.Pow(2, lan) + Random.Shared.Next(0, 100));    // luy thua + jitter
            Console.WriteLine($"   (thu lai lan {lan + 1} sau {cho.TotalMilliseconds:F0} ms vi {(int)res.StatusCode})");
            res.Dispose();
            await Task.Delay(cho, ct);
        }
    }

    private static async Task KiemTraAsync(HttpResponseMessage res, CancellationToken ct)
    {
        if (res.IsSuccessStatusCode) return;
        string noi = await res.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"API tra {(int)res.StatusCode}: {noi}", null, res.StatusCode);
    }
}
