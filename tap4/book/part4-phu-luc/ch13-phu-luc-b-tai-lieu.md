# Phụ lục B — Tài liệu tham khảo

Lĩnh vực này thay đổi nhanh: tên mô hình, giá, gói NuGet và API đều đổi theo tháng. Hãy luôn đối chiếu **tài liệu hiện hành**; phần dưới chỉ là điểm bắt đầu.

## Tài liệu chính thức

- **.NET và AI**: mục "AI" trên `learn.microsoft.com/dotnet/ai` — `Microsoft.Extensions.AI`, `Microsoft.Extensions.VectorData`, hướng dẫn RAG/agent cho .NET.
- **Microsoft.Extensions.AI**: kho `github.com/dotnet/extensions` (mã nguồn, ghi chú phát hành, ví dụ).
- **Anthropic**: `docs.anthropic.com` — Messages API, tool use, streaming, prompt caching, batch, bảng giá và danh sách mô hình hiện hành.
- **OpenAI / Azure OpenAI, Google Gemini, Ollama**: tài liệu của từng nhà cung cấp (định dạng request, giới hạn, giá).
- **Model Context Protocol (MCP)**: `modelcontextprotocol.io` và SDK C# chính thức.
- **OpenTelemetry GenAI semantic conventions**: `opentelemetry.io` (mục semantic conventions).
- **OWASP Top 10 for LLM Applications**: danh mục rủi ro (prompt injection, rò rỉ dữ liệu, xử lý đầu ra không an toàn, "excessive agency"...).
- **Bảo vệ dữ liệu**: Nghị định 13/2023/NĐ-CP (Việt Nam); GDPR nếu phục vụ EU; điều khoản xử lý dữ liệu của nhà cung cấp LLM bạn dùng.

## Sách và tài liệu nền

| Nguồn | Chủ đề |
|-------|--------|
| *Designing Machine Learning Systems* — Chip Huyen | hệ thống ML/AI trong sản xuất, đánh giá, giám sát |
| *AI Engineering* — Chip Huyen | xây ứng dụng trên mô hình nền: prompt, RAG, agent, đánh giá |
| *Building LLMs for Production* | kỹ thuật ứng dụng LLM |
| *Speech and Language Processing* — Jurafsky & Martin | nền tảng NLP (miễn phí trực tuyến) |
| *Release It!* — Michael Nygard | ổn định hệ thống (áp dụng cho phụ thuộc LLM chậm/lỗi) |

## Thư viện và công cụ nhắc trong Tập 4

| Nhu cầu | Công cụ |
|---------|---------|
| Abstraction LLM | `Microsoft.Extensions.AI` (`IChatClient`, `IEmbeddingGenerator`) |
| Framework agent/điều phối | Semantic Kernel, Microsoft Agent Framework |
| Kho vector | `Microsoft.Extensions.VectorData`; pgvector, Qdrant, Azure AI Search, Weaviate, Milvus |
| Tính toán vector | `System.Numerics.Tensors` (`TensorPrimitives`) |
| Mô hình cục bộ | Ollama, LM Studio, ONNX Runtime |
| Đánh giá | tự xây golden set + xUnit (Chương 9); các bộ đánh giá LLM mã nguồn mở |
| Quan sát | OpenTelemetry, Grafana, Application Insights |
| Kiểm duyệt nội dung | moderation API của nhà cung cấp; dịch vụ an toàn nội dung của đám mây |
| Đọc tài liệu (PDF/Office) cho RAG | thư viện trích văn bản; dịch vụ document intelligence |

## Ba tập trước

- **Tập 1** — C# và .NET nền tảng (bất đồng bộ, JSON, kiểm thử).
- **Tập 2** — ASP.NET Core, EF Core, xác thực, dự án tổng hợp.
- **Tập 3** — Clean Architecture, CQRS, outbox, idempotency, quan sát, triển khai — nền của `kho-clean`, hệ thống mà Chương 11 gắn trợ lý AI lên.

## Lời nhắc về độ tin cậy của thông tin

Mọi số liệu về **giá, giới hạn, tên mô hình, khả năng** trong tài liệu (kể cả sách này) đều có thể lỗi thời. Sách này cố ý dùng **mô hình giả** ở mọi ví dụ chạy được: chúng kiểm chứng *mã của bạn*, không kiểm chứng *chất lượng mô hình*. Trước khi quyết định kiến trúc hay chi phí, hãy **tự đo** trên mô hình và dữ liệu thật của bạn.
