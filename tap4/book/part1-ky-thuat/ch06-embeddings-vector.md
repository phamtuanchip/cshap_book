# Chương 6 — Embeddings và tìm kiếm vector

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **embedding** là gì: một đoạn văn bản → một **vector số** sao cho văn bản *giống nghĩa* thì vector *gần nhau*.
- Đo độ giống bằng **cosine similarity**; hiểu vì sao cần **ngưỡng điểm**, không chỉ lấy top-k.
- Xây một **kho vector trong bộ nhớ** và tìm kiếm bằng `System.Numerics.Tensors`.
- Biết khi nào cần **CSDL vector thật** (pgvector, Azure AI Search, Qdrant...) và lọc theo **siêu dữ liệu** (metadata) cho quyền truy cập.

Code: [`code/ch06-embeddings/`](../../code/ch06-embeddings/) — bộ sinh embedding **giả** (băm từ, không cần khoá API) để chạy và hiểu cơ chế.

> **Trung thực về kiểm chứng — đọc kỹ mục này.** Bộ sinh embedding trong code mẫu là **tự chế**: nó băm từng từ (sau khi bỏ dấu, bỏ từ dừng) vào một vector 512 chiều kiểu "túi từ" (bag-of-words). Nó **giống văn bản thật ở việc từ trùng nhau → vector gần nhau**, nhưng **không hiểu ngữ nghĩa**: "laptop" và "máy tính xách tay" (đồng nghĩa) sẽ **không** được nhận là gần nhau nếu không chia sẻ từ. Kết quả chạy dưới đây minh hoạ đúng hạn chế này — đó là mục đích của ví dụ. Embedding thật (`text-embedding-3-*` của OpenAI, embedding của Voyage/Cohere, mô hình mở như `all-MiniLM`...) được huấn luyện để nắm ngữ nghĩa và **chưa được đo trong sách này** vì cần khoá API/tải mô hình.

## Embedding là gì

Một **mô hình embedding** ánh xạ văn bản → vector số thực có số chiều cố định (ví dụ 1536). Mô hình được huấn luyện sao cho **khoảng cách hình học** giữa hai vector phản ánh **độ giống nghĩa** giữa hai văn bản gốc. Khác với LLM sinh văn bản (Chương 1–5), embedding **không sinh gì cả** — nó chỉ "định vị" văn bản trong một không gian số.

```csharp
IEmbeddingGenerator<string, Embedding<float>> gen = ...;                 // giao diện chuẩn của Microsoft.Extensions.AI
var kq = await gen.GenerateAsync(["Laptop Dell XPS 13, man hinh 13 inch"]);
float[] vector = kq[0].Vector.ToArray();                                  // ví dụ chạy: 512 chiều, độ dài chuẩn hoá = 1.00
```

Ứng dụng chính: **tìm kiếm ngữ nghĩa** (tìm tài liệu *liên quan*, không chỉ *trùng từ khoá*), **phân cụm**, **phát hiện trùng lặp**, và làm nền cho **RAG** (Chương 7).

## Đo độ giống: cosine similarity

Với vector đã **chuẩn hoá độ dài về 1**, độ giống cosine chính là **tích vô hướng**, nằm trong khoảng **[-1, 1]** (thường [0, 1] với embedding văn bản): 1 là giống hệt hướng, 0 là không liên quan, âm là ngược nghĩa (hiếm với embedding văn bản thông thường).

```csharp
float diem = TensorPrimitives.CosineSimilarity(vectorA, vectorB);        // System.Numerics.Tensors
```

Chạy với bộ sinh giả, so câu `"Laptop Dell XPS 13 man hinh 13 inch"` với bốn câu khác:

```
0.671  Laptop Dell XPS 13 nhe va mong
0.183  Chuot khong day Logitech
0.000  Ban phim co Keychron K2
0.258  May tinh xach tay Dell XPS
```

Câu cuối **đồng nghĩa** ("máy tính xách tay" = "laptop") nhưng chỉ đạt 0,258 — thấp hơn nhiều so với câu chỉ trùng từ ("Dell XPS 13"). Đây chính là giới hạn của embedding "túi từ": nó đo **trùng từ**, không đo **nghĩa**. Embedding thật (dựa trên mạng nơ-ron học từ dữ liệu khổng lồ) nắm được "laptop" ≈ "máy tính xách tay" tốt hơn nhiều — nhưng con số cụ thể phải đo trên mô hình thật, không suy ra từ ví dụ này.

## Kho vector và tìm kiếm

```csharp
public sealed record Muc(string Id, string VanBan, float[] Vector, IReadOnlyDictionary<string, string> SieuDuLieu);

public sealed class KhoVector
{
    private readonly List<Muc> _muc = [];
    public void Them(Muc muc) => _muc.Add(muc);

    public IReadOnlyList<KetQuaTim> Tim(float[] truyVan, int k, float diemToiThieu = 0f, Func<Muc, bool>? loc = null)
        => _muc.Where(m => loc?.Invoke(m) ?? true)                        // LỌC THEO SIÊU DỮ LIỆU trước
               .Select(m => new KetQuaTim(m, TensorPrimitives.CosineSimilarity(truyVan, m.Vector)))
               .Where(r => r.Diem >= diemToiThieu)                        // NGƯỠNG: không trả kết quả không liên quan
               .OrderByDescending(r => r.Diem).Take(k).ToList();
}
```

Đây là **brute force** (`O(n·d)`, so với mọi mục) — chính xác tuyệt đối, phù hợp tới hàng chục nghìn mục. Kết quả chạy mẫu trên 5 sản phẩm:

```
Cau hoi: laptop man hinh 13 inch pin lau
   0.683  LT001  Laptop Dell XPS 13 - man hinh 13 inch, RAM 16GB, pin 12 gio
   0.390  LT002  Laptop Asus Vivobook 15 - man hinh 15.6 inch, RAM 8GB, gia re
```

### Ngưỡng điểm: đừng luôn trả top-k

```
Cau hoi: cong thuc nau pho bo   [nguong=0]
   0.141  MH001  Man hinh Dell U2720Q 27 inch 4K - cong USB-C
Cau hoi: cong thuc nau pho bo   [nguong=0.2]
   (khong co ket qua vuot nguong)
```

Nếu chỉ lấy "top-k gần nhất", hệ thống **luôn trả về gì đó** — kể cả khi câu hỏi hoàn toàn không liên quan đến kho dữ liệu ("công thức nấu phở bò" so với kho sản phẩm). Với RAG (Chương 7), đưa những đoạn "gần nhất nhưng không liên quan" này vào ngữ cảnh sẽ khiến mô hình **bịa hoặc trả lời lạc đề với vẻ tự tin**. Luôn đặt **ngưỡng điểm tối thiểu** và xử lý rõ ràng trường hợp "không tìm thấy gì liên quan" — ngưỡng cụ thể phải hiệu chỉnh theo mô hình embedding và dữ liệu thật của bạn, không có con số vạn năng.

### Lọc theo siêu dữ liệu

```
Cau hoi: Dell 13 inch   [k=3]
   0.596  LT001 ...
   0.365  MH001 ...
   0.204  CH001 ...
Cau hoi: Dell 13 inch   [k=3, nhom=man-hinh]
   0.365  MH001 ...
```

Tìm kiếm vector **không thay thế** việc lọc theo điều kiện chính xác (nhóm, ngày, **quyền truy cập**). Luôn lọc bằng siêu dữ liệu (giống `WHERE` trong SQL) **trước hoặc cùng lúc** với so khớp vector. Đây là chỗ dễ bị bỏ sót nhất về bảo mật: nếu tài liệu của người dùng A và B nằm chung một kho vector, **phải lọc theo quyền sở hữu/tenant ở tầng truy vấn**, không dựa vào "may mắn" điểm số thấp để loại tài liệu không được phép xem (Chương 10).

## Khi nào cần CSDL vector thật

| Quy mô / nhu cầu | Giải pháp |
|-------------------|-----------|
| Vài nghìn–vài chục nghìn mục, một tiến trình | kho trong bộ nhớ (như trên) hoặc SQLite + tính tay |
| CSDL quan hệ đã có, muốn thêm tìm kiếm ngữ nghĩa | **pgvector** (PostgreSQL), SQL Server có vector search |
| Quy mô lớn, cần chỉ mục gần đúng (ANN) tốc độ cao | **Qdrant, Weaviate, Milvus, Pinecone, Azure AI Search** |
| Đã dùng Elasticsearch/OpenSearch | tính năng vector tích hợp sẵn |

CSDL vector thật dùng **chỉ mục gần đúng** (HNSW, IVF...) đánh đổi một chút độ chính xác lấy tốc độ ở quy mô triệu mục, hỗ trợ **lọc kết hợp** (vector + điều kiện) hiệu quả, và **cập nhật/xoá** mục mà không phải xây lại toàn bộ. `Microsoft.Extensions.VectorData` cung cấp abstraction (`VectorStore`, `VectorStoreCollection`) tương tự `IChatClient`, để đổi CSDL vector mà không viết lại logic ứng dụng.

## Chuẩn bị dữ liệu: chia nhỏ (chunking)

Tài liệu dài phải **chia thành đoạn (chunk)** trước khi tạo embedding — một embedding cho *cả một cuốn sách* sẽ quá loãng để tìm chính xác. Nguyên tắc:

- Chia theo **ranh giới có nghĩa** (đoạn văn, mục, cặp hỏi-đáp) hơn là cắt cứng theo số ký tự.
- Kích thước đoạn: đủ để **có ngữ cảnh** (không cụt câu) nhưng đủ nhỏ để **liên quan chính xác**; cần thử nghiệm theo dữ liệu (thường vài trăm token).
- **Chồng lấn (overlap)** nhẹ giữa các đoạn để không cắt đứt ý ở ranh giới.
- Giữ **siêu dữ liệu nguồn** (tài liệu gốc, mục, ngày cập nhật, quyền truy cập) theo từng đoạn để trích dẫn và lọc.
- Khi tài liệu nguồn **đổi**, phải **cập nhật lại embedding** của đoạn liên quan — thiết kế cơ chế đồng bộ (tương tự cache invalidation, Tập 3 Chương 10).

## Lỗi thường gặp

- So sánh embedding từ **hai mô hình khác nhau** — không gian vector khác nhau, kết quả vô nghĩa.
- Quên **chuẩn hoá** trước khi dùng tích vô hướng làm cosine (một số API trả vector chưa chuẩn hoá).
- Không đặt **ngưỡng điểm** → trả kết quả không liên quan với vẻ tự tin.
- Không lọc **quyền truy cập** theo siêu dữ liệu → rò rỉ dữ liệu giữa người dùng/tenant.
- Chia đoạn quá lớn (loãng nghĩa) hoặc quá nhỏ (mất ngữ cảnh).
- Không cập nhật embedding khi tài liệu nguồn thay đổi → tìm kiếm dựa trên dữ liệu cũ.
- Dùng brute force ở quy mô triệu mục (chậm) hoặc dùng CSDL vector phức tạp cho vài trăm mục (thừa).

## Bài tập

1. Thêm phương thức `KhoVector.Xoa(id)` và `CapNhat(muc)`; viết test rằng tìm kiếm không còn trả về mục đã xoá.
2. Đo cosine similarity giữa `"laptop"`, `"labtop"` (sai chính tả) và `"điện thoại"` bằng bộ sinh giả; giải thích tại sao kết quả không phản ánh đúng "sai chính tả nên vẫn gần nghĩa" — đây là hạn chế của embedding từ-vựng so với embedding học sâu.
3. Viết một chỉ mục đảo (inverted index) đơn giản và so sánh tốc độ tìm kiếm từ khoá thuần tuý với tìm kiếm vector trên cùng bộ dữ liệu; nêu trường hợp mỗi cách phù hợp hơn.
4. Thiết kế (trên giấy) lược đồ siêu dữ liệu cho một kho tài liệu nội bộ nhiều phòng ban, đảm bảo nhân viên phòng A không tìm thấy tài liệu chỉ dành cho phòng B dù embedding có thể "gần" về mặt nội dung.
5. (Cần khoá API hoặc mô hình cục bộ) Thay `HashEmbeddingGenerator` bằng một bộ sinh embedding thật (`Microsoft.Extensions.AI` có cài đặt cho OpenAI/Ollama), lặp lại thí nghiệm mục 2 và so sánh kết quả.
