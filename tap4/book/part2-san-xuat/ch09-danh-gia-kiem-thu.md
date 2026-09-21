# Chương 9 — Đánh giá và kiểm thử ứng dụng LLM

## Mục tiêu học

Sau chương này, bạn sẽ:

- Phân biệt rõ **hai thứ khác nhau bị gộp chung là "kiểm thử LLM"**: kiểm thử **mã xung quanh** mô hình (tất định, `Assert.Equal` vẫn dùng được) và đánh giá **chất lượng đầu ra** của mô hình (không tất định, cần phương pháp khác).
- Xây một **bộ dữ liệu vàng (golden set)** và đo **tỉ lệ đạt theo ngưỡng**, không phải khớp chuỗi tuyệt đối.
- Viết **kiểm thử thống kê** cho hệ thống không xác định (lấy mẫu nhiều lần, xét trên tổng thể).
- Dùng **LLM làm giám khảo (LLM-as-judge)** khi so khớp từ vựng không đủ tinh — và thấy rõ **giới hạn** của cả hai cách.
- Coi **prompt là mã nguồn** cần kiểm thử hồi quy.

Code: [`code/ch09-danh-gia-kiem-thu/`](../../code/ch09-danh-gia-kiem-thu/) — 11 test, `dotnet test` chạy được ngay, không cần khoá API.

> **Trung thực về một phát hiện thật khi viết chương này:** bộ so khớp "tương tự theo từ vựng" viết ban đầu **đã thất bại thật** trong lúc soạn ví dụ — nó cho một câu trả lời **bịa sai số liệu** (nói đơn hàng tối đa "50 dòng" thay vì "20 dòng" đúng) điểm tương tự **đủ cao để coi là đạt**, chỉ vì hai câu chia sẻ nhiều từ chung ("đơn hàng", "tối đa"). Phần "Bài học thực tế" dưới đây thuật lại đúng phát hiện đó và cách sửa — đây không phải ví dụ dàn dựng, mà là điều thực sự xảy ra khi mã hoá chương này.

## Vì sao kiểm thử LLM khác kiểm thử thường

Kiểm thử truyền thống (Tập 2, Chương 14 và Tập 3, Chương 9) dựa trên tiền đề: **cùng đầu vào, cùng đầu ra**. `Assert.Equal(7, TinhTong(3, 4))` đúng mãi mãi. Với một LLM thật, **cùng một câu hỏi có thể ra hai câu trả lời khác nhau về câu chữ nhưng cùng đúng về nghĩa** — và đôi khi ra một câu **sai** dù trông rất tự tin. `Assert.Equal` cho văn bản LLM gần như luôn sai cách.

Điều quan trọng: **không phải mọi thứ trong một ứng dụng LLM đều không xác định**. Tách bạch:

| Phần | Xác định? | Cách kiểm thử |
|------|-----------|---------------|
| Dựng prompt, gọi API, xử lý lỗi/retry (Chương 2) | **Có** | `Assert.Equal` bình thường, với `IChatClient` giả (Chương 3) |
| Parse/kiểm chứng đầu ra có cấu trúc (Chương 4) | **Có** (với đầu vào giả cố định) | `Assert.Equal`/`Assert.True` trên logic kiểm chứng |
| Công cụ (hàm C# được gọi) (Chương 5) | **Có** | test như hàm bình thường |
| **Nội dung câu trả lời của chính mô hình** | **Không** | đánh giá theo ngưỡng, thống kê, giám khảo (chương này) |

Phần lớn lỗi trong ứng dụng LLM thực tế nằm ở **phần tất định** (dựng prompt sai, parse sai, quên xử lý lỗi) — và phần đó **kiểm thử được đầy đủ, nhanh, miễn phí** như mọi test khác, minh chứng bởi `LogicXungQuanhMoHinh_LaTatDinh` trong code mẫu: dùng `TroLyOnDinh` (một `IChatClient` giả trả lời cố định), `Assert.Equal` hoàn toàn hợp lệ vì **chính bản giả đó là tất định theo thiết kế** — ta đang kiểm thử cách ứng dụng gọi và xử lý, không kiểm thử "trí tuệ" của mô hình.

## Bộ dữ liệu vàng và ngưỡng tỉ lệ đạt

Với **chất lượng câu trả lời**, đơn vị kiểm thử không còn là "một ca" mà là **một tập ca** (golden set) và **tỉ lệ đạt**:

```csharp
public sealed record CaVang(string Id, string CauHoi, string DapAnMongDoi, float NguongDiem = 0.5f);

public static async Task<BaoCaoDanhGia> ChayAsync(IChatClient client, IEnumerable<CaVang> caVang, CancellationToken ct = default)
{
    var ketQua = new List<KetQuaCa>();
    foreach (var ca in caVang)
    {
        var res = await client.GetResponseAsync(ca.CauHoi, cancellationToken: ct);
        float diem = DoTuongTu.Diem(res.Text, ca.DapAnMongDoi);
        ketQua.Add(new(ca.Id, diem >= ca.NguongDiem, diem, res.Text));
    }
    return new(ketQua, tiLeDat: ..., diemTrungBinh: ...);
}
```

```csharp
[Fact]
public async Task TroLyThucTe_PhaiDatItNhat80PhanTram_KhongBatBuoc100Phantram()
{
    var baoCao = await BoDanhGia.ChayAsync(new TroLyThucTe(), BoCauHoiVang.Ca);
    Assert.True(baoCao.TiLeDat >= 0.8f, $"Ti le dat {baoCao.TiLeDat:P0} duoi nguong. Chi tiet: {baoCao}");
}
```

Đây là khác biệt cốt lõi so với test thường: **assert trên tổng thể** (tỉ lệ, trung bình), **không** đòi hỏi từng ca đều hoàn hảo. Ngưỡng "80%" (hay bất kỳ số nào) là **quyết định sản phẩm**, không phải hằng số kỹ thuật — chọn dựa trên mức rủi ro chấp nhận được của tính năng, và **phải được người có trách nhiệm nghiệp vụ đồng ý**, giống hạn mức SLO (Tập 3, Chương 11).

## Bài học thực tế: đo "tương tự" không đơn giản

Bộ dữ liệu vàng ở trên có ca `Q4`: hỏi "Đơn hàng tối đa bao nhiêu dòng?", đáp án đúng "Đơn hàng tối đa 20 dòng." Bản mô hình `TroLyThucTe` (đại diện một hệ thống thật, thỉnh thoảng bịa) trả lời sai: **"Đơn hàng có thể có tối đa 50 dòng tuỳ cấu hình hệ thống."**

Bộ so khớp **tương tự theo từ vựng** (đếm từ chung, cosine trên vector tần suất từ — kỹ thuật ở Chương 6) ban đầu cho cặp này **điểm đủ cao để "đạt"**: hai câu chia sẻ nhiều từ ("đơn hàng", "tối đa", "hệ thống"...), nên cosine tính theo từ vẫn cao dù **con số cốt lõi sai hoàn toàn** (50 so với 20). Chạy `dotnet test` với logic ban đầu, test khẳng định "câu bịa phải bị phát hiện" **thất bại thật**.

Sửa bằng cách thêm **kiểm tra số liệu riêng biệt**:

```csharp
public static float Diem(string a, string b)
{
    double diemTu = DiemTheoTu(a, b);
    var soTrongB = SoTrongCau(b);                              // "b" la dap an mau: so trong do la SU KIEN can khop dung
    if (soTrongB.Count > 0 && !soTrongB.All(SoTrongCau(a).Contains))
        diemTu *= 0.3;                                         // phat nang: sai so lieu la loi nghiem trong hon la khac tu vung
    return (float)diemTu;
}
```

Bài học tổng quát, không chỉ riêng ví dụ này: **độ tương tự văn bản chung chung (từ vựng hay cả embedding ngữ nghĩa) không đủ để bắt lỗi sai sự kiện/số liệu cụ thể.** Với câu trả lời chứa **sự kiện kiểm chứng được** (số lượng, ngày tháng, tên riêng, mã sản phẩm), hãy **trích xuất và so khớp riêng** các yếu tố đó, tách khỏi điểm "giống nhau nói chung". Đây chính xác là lý do các bộ đánh giá LLM nghiêm túc trong thực tế dùng **nhiều chỉ số kết hợp** (tương tự ngữ nghĩa + kiểm tra sự kiện + giám khảo) thay vì một con số duy nhất.

## Kiểm thử thống kê cho hệ thống không xác định

Khi một thành phần **thật sự** không xác định (mô hình thật với `Temperature > 0`, hoặc mô phỏng bằng `TroLyKhongOnDinh` trong code mẫu), kiểm thử **một lần chạy** vô nghĩa — có thể trúng hoặc trượt ngẫu nhiên. Cách đúng: **lấy mẫu nhiều lần, assert trên phân phối**:

```csharp
[Fact]
public async Task ChayNhieuLan_TiLeDungXapXiThamSoDaCauHinh_TrongSaiSoChapNhanDuoc()
{
    const int soLan = 200;
    const int tiLeKyVong = 75;
    var client = new TroLyKhongOnDinh(tiLeKyVong, seed: 42);      // seed CỐ ĐỊNH -> bài test này TẤT ĐỊNH khi chạy lại

    int soDung = 0;
    for (int i = 0; i < soLan; i++) if ((await client.GetResponseAsync("...")).Text.Contains("7 chiec")) soDung++;

    Assert.InRange(100.0 * soDung / soLan, tiLeKyVong - 10, tiLeKyVong + 10);
}
```

Hai điểm dễ nhầm:

- **Bản thân bài test phải tất định** dù hệ thống được kiểm thử không tất định — dùng `Random` có **seed cố định** trong bản giả để `dotnet test` luôn ra cùng kết quả. Với mô hình LLM **thật**, bạn không kiểm soát được seed nội bộ của nó — nên loại test "chạy N lần, xét phân phối" với mô hình thật thường chạy **thủ công/định kỳ** (không phải mỗi lần CI) vì tốn tiền và thời gian; CI hằng ngày dựa vào bộ giả + số liệu tỉ lệ đạt được ghi nhận định kỳ từ mô hình thật.
- **Biên sai số** (`± 10` ở trên) phải đủ rộng để không báo động giả do nhiễu lấy mẫu, nhưng đủ hẹp để bắt được suy giảm chất lượng thật. Đây là đánh đổi thống kê, không có công thức vạn năng — với ứng dụng quan trọng, dùng khoảng tin cậy chặt chẽ hơn (nhị thức Wilson...) thay vì biên cố định.

## LLM làm giám khảo (LLM-as-judge)

Khi tiêu chí "đúng" phức tạp hơn khớp từ hay khớp số (văn phong, tính đầy đủ, có bám sát ngữ cảnh được cung cấp hay không — độ **có căn cứ**/*faithfulness* nhắc ở Chương 7), một cách phổ biến là dùng **một mô hình thứ hai** để chấm điểm câu trả lời của mô hình thứ nhất theo tiêu chí (rubric) rõ ràng, trả **JSON có cấu trúc** (Chương 4):

```csharp
public static async Task<PhanXu> ChamAsync(IChatClient giamKhao, string cauHoi, string ngoCanh, string traLoi, CancellationToken ct = default)
{
    string heThong = """
        Ban la giam khao cham cau tra loi cua mot tro ly AI. Cham theo thang 1-5:
        5 = dung hoan toan, dua tren ngu canh, day du. 3 = dung mot phan. 1 = sai hoac bia dat.
        Tra loi CHI bang JSON: {"diem":so,"lyDo":"...","coCanCu":true/false}
        """;
    ...
}
```

Chạy mẫu (giám khảo giả theo quy tắc từ khoá, đại diện một giám khảo thật): câu trả lời **có căn cứ** (khớp đúng "30 ngày" có trong ngữ cảnh) được chấm ≥ 4 điểm; câu trả lời **bịa số** ("50 dòng" khi ngữ cảnh nói "20 dòng") được chấm ≤ 2 điểm và `coCanCu = false`. Ở đây, giám khảo **bắt được đúng lỗi mà bộ so khớp từ vựng ban đầu bỏ lọt** — minh hoạ tại sao kết hợp nhiều phương pháp tốt hơn một.

Giới hạn của LLM-as-judge (thành thật, không tô hồng):

- **Giám khảo cũng là một LLM**: có thể thiên vị (ưa câu trả lời dài, ưa văn phong giống chính nó), có thể bị "qua mặt" bởi câu trả lời tự tin nhưng sai, và **bản thân giám khảo cần được kiểm định** (so với đánh giá của con người trên một mẫu, định kỳ).
- Tốn thêm **một lượt gọi mô hình** cho mỗi ca — nhân đôi chi phí đánh giá.
- Không thay thế được **đánh giá của con người** cho quyết định rủi ro cao; dùng giám khảo để **sàng lọc quy mô lớn**, con người xem lại mẫu và các trường hợp biên.

## Prompt là mã nguồn: kiểm thử hồi quy

Một câu đổi trong system prompt có thể đổi hành vi hàng loạt (Chương 1). Đối xử với prompt như **mã**: lưu trong kho, và có **kiểm thử phát hiện thay đổi ngoài ý muốn**:

```csharp
private const string PromptDaChapNhan = """
    Ban la tro ly tra loi dua CHI TREN tai lieu duoc cung cap trong <tai_lieu>.
    ...
    """;

[Fact]
public void SystemPrompt_KhongDoiNgoaiYMuon()
{
    Assert.Equal(PromptDaChapNhan, PromptHienTaiTrongMa);
    // Neu do: (a) vo tinh sua -> sua lai; (b) co y -> cap nhat hang so nay VA chay lai toan bo bo danh gia truoc khi merge
}
```

Đây là kiểm thử **snapshot** đơn giản nhất: nó không đảm bảo prompt mới *tốt hơn*, chỉ đảm bảo **không ai vô tình sửa nó** mà không nhận ra. Mọi thay đổi prompt **có chủ đích** phải kèm theo: chạy lại bộ đánh giá (golden set), so sánh tỉ lệ đạt trước/sau, và người duyệt xác nhận thay đổi tỉ lệ (tăng hoặc giảm) là chấp nhận được — quy trình gọi là **eval-driven development**, tương tự test-driven development nhưng đơn vị là "tỉ lệ đạt" thay vì "pass/fail".

## Đưa đánh giá vào quy trình

| Khi nào | Chạy gì |
|---------|---------|
| Mỗi commit (CI) | test tất định (mã xung quanh mô hình) + bộ giả — nhanh, miễn phí, luôn chạy |
| Mỗi thay đổi prompt/mô hình | toàn bộ golden set trên **mô hình thật** (hoặc mô hình rẻ đại diện) — chậm hơn, có phí, chạy khi cần |
| Định kỳ (tuần/tháng) trên sản xuất | lấy mẫu tương tác thật, đánh giá bằng giám khảo + con người — phát hiện **suy giảm chất lượng theo thời gian** (model drift khi nhà cung cấp cập nhật mô hình, dữ liệu thay đổi) |
| Trước khi tăng quyền tự chủ agent (Chương 8) | đánh giá tỉ lệ đề xuất đúng trên dữ liệu thật, không chỉ trên bộ giả |

Ghi lại **mọi lần chạy đánh giá** (phiên bản prompt, mô hình, tỉ lệ đạt, ngày) để thấy xu hướng — một bảng đơn giản còn hơn không có gì.

## Lỗi thường gặp

- `Assert.Equal` trên văn bản do LLM sinh ra.
- Coi độ tương tự văn bản là đủ để bắt sai lệch về **sự kiện/số liệu** — như phát hiện thật ở trên.
- Golden set quá nhỏ hoặc không đại diện (chỉ toàn ca dễ) → tỉ lệ đạt cao giả tạo.
- Không đặt ngưỡng rõ ràng, hoặc đặt ngưỡng mà không ai chịu trách nhiệm quyết định.
- Test hệ thống không xác định bằng một lần chạy duy nhất.
- Tin tuyệt đối vào LLM-as-judge mà không kiểm định giám khảo bằng đánh giá của con người.
- Sửa prompt mà không chạy lại bộ đánh giá — coi prompt như "chỉ là văn bản", không phải mã ảnh hưởng hành vi.
- Không log/lưu vết kết quả đánh giá theo thời gian → không phát hiện được suy giảm chất lượng dần dần.

## Bài tập

1. Thêm 5 ca vào `BoCauHoiVang` (kể cả một ca có số liệu) và chạy lại `DanhGiaTheoNguongTiLe` với `TroLyThucTe`; điều chỉnh ngưỡng tổng thể hợp lý.
2. Viết thêm một chỉ số so khớp: kiểm tra **mã sản phẩm** (`[A-Z]{2}[0-9]{3}`) xuất hiện trong đáp án mẫu có xuất hiện đúng trong câu trả lời không, theo cùng nguyên tắc với kiểm tra số liệu.
3. Thử nghiệm: đổi `PromptDaChapNhan` trong `KiemThuHoiQuyPrompt` (cố tình bỏ câu "Nếu tài liệu không đủ...") mà **không** cập nhật prompt thật; xác nhận test đỏ và giải thích quy trình xử lý đúng khi gặp việc này.
4. Với `TroLyKhongOnDinh`, thử hai `seed` khác nhau và giải thích vì sao bài test vẫn tất định dù mô phỏng hệ thống ngẫu nhiên; sau đó thử **không** cố định seed và quan sát test trở nên "flaky" — nêu lý do đây là điều cần tránh.
5. Thiết kế (trên giấy) quy trình eval-driven development cho việc sửa prompt trợ lý kho: các bước, ai duyệt, tiêu chí chấp nhận khi tỉ lệ đạt giảm ở một golden set nhưng tăng ở golden set khác.
