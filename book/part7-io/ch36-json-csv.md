# Chương 36 — JSON và CSV

## Mục tiêu học

Sau chương này, bạn sẽ:

- Chuyển object ↔ JSON bằng `System.Text.Json` (serialize/deserialize), tuỳ chỉnh bằng options và attribute.
- Đọc JSON không có sẵn class bằng `JsonDocument`.
- Đọc/ghi **CSV** đúng (dấu phẩy, ngoặc kép, xuống dòng trong ô) và biết khi nào nên dùng thư viện.
- Xử lý an toàn dữ liệu đầu vào không tin cậy.

Code mẫu: [`code/ch36-json-csv/`](../../code/ch36-json-csv/).

## JSON là gì?

**JSON** là định dạng văn bản trao đổi dữ liệu phổ biến nhất hiện nay (API web, file cấu hình):

```json
{ "ma": 1, "hoTen": "Nguyen Van An", "diem": 8.5, "monHoc": ["Toan", "Ly"] }
```

Gồm đối tượng `{ }`, mảng `[ ]`, chuỗi, số, `true/false`, `null`. Trong ASP.NET Core, request/response JSON được chuyển đổi tự động
bằng đúng cơ chế trong chương này (Tập 2). .NET có sẵn **`System.Text.Json`** (nhanh, mặc định); bạn cũng sẽ gặp `Newtonsoft.Json`
trong code cũ.

## Serialize và Deserialize

```csharp
using System.Text.Json;

var tuyChon = new JsonSerializerOptions
{
    WriteIndented = true,                                  // in đẹp, thụt lề
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,     // Ten -> "ten"
};

string json = JsonSerializer.Serialize(sv, tuyChon);                       // object -> JSON
SinhVien? sv2 = JsonSerializer.Deserialize<SinhVien>(json, tuyChon);        // JSON -> object
```

Quy tắc: chỉ **public property** (có `get`/`set`) được xử lý mặc định; field public cần `IncludeFields = true`. `record`, `init`,
`required` đều dùng được. Tên khớp **phân biệt hoa/thường** trừ khi bật `PropertyNameCaseInsensitive = true`
(hoặc dùng `JsonSerializerDefaults.Web` — camelCase + không phân biệt hoa/thường, chuẩn cho web).

Tái sử dụng `JsonSerializerOptions` (tạo một lần, để `static readonly`): tạo mới mỗi lần rất tốn hiệu năng.

## Tuỳ chỉnh bằng attribute

```csharp
class SinhVien
{
    public int Ma { get; set; }

    [JsonPropertyName("hoTen")]                              // đổi tên trong JSON
    public string Ten { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]         // enum ghi ra chữ ("Gioi") thay vì số (3)
    public XepLoai XepLoai { get; set; }

    [JsonIgnore]                                             // KHÔNG đưa vào JSON (dữ liệu nhạy cảm)
    public string MatKhau { get; set; } = "";
}
```

Chạy chương trình mẫu: `MatKhau` không xuất hiện trong JSON, và khi đọc lại nó là chuỗi rỗng. Các tuỳ chọn hữu ích
khác: `[JsonPropertyOrder]`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, `Converters`, `NumberHandling`.
`DateOnly`, `TimeOnly`, `DateTime`, `Guid`, `decimal` được hỗ trợ sẵn (ngày dùng chuẩn ISO 8601).

**Đừng bao giờ serialize thẳng entity CSDL chứa mật khẩu/băm/dữ liệu nội bộ ra ngoài** — dùng DTO/`record` riêng cho đầu ra (Tập 2).

## Đọc JSON không có class: `JsonDocument`

Khi cấu trúc thay đổi hoặc chỉ cần lấy vài giá trị:

```csharp
using var doc = JsonDocument.Parse(jsonChuoi);
var goc = doc.RootElement;
string ten = goc.GetProperty("cua_hang").GetProperty("ten").GetString()!;
decimal tong = goc.GetProperty("cua_hang").GetProperty("sach")
                  .EnumerateArray().Sum(x => x.GetProperty("gia").GetDecimal());
```

Dùng `TryGetProperty` nếu thuộc tính có thể vắng. `JsonDocument` chỉ đọc và nên `Dispose`. Cần vừa đọc vừa sửa: `JsonNode`
(`JsonNode.Parse(...)`, chỉ mục `node["a"]["b"]`).

## Lỗi và dữ liệu xấu

```csharp
try { JsonSerializer.Deserialize<SinhVien>("{ \"ma\": \"abc\" }", tuyChon); }
catch (JsonException e) { ... }        // sai cú pháp hoặc sai kiểu
```

JSON từ bên ngoài **không đáng tin**: kiểm tra sau khi đọc (giá trị null, khoảng hợp lệ — `required`, validation của Tập 2),
đặt giới hạn kích thước/độ sâu (`MaxDepth`) nếu nhận từ người lạ.

## File và luồng

```csharp
await File.WriteAllTextAsync(path, json);
await using var fs = File.OpenRead(path);
var t = await JsonSerializer.DeserializeAsync<SinhVien>(fs, tuyChon);   // đọc thẳng từ stream, tiết kiệm bộ nhớ
```

`DeserializeAsyncEnumerable<T>` đọc mảng JSON rất lớn theo từng phần tử.

## CSV

**CSV** (giá trị phân tách bằng dấu phẩy) là định dạng bảng đơn giản, mở được bằng Excel:

```
ma,ten,diem
1,An,8.5
2,"Binh, Nguyen",7.0
3,"Chi ""Cool""",9.0
```

Nghe đơn giản nhưng có bẫy: **`Split(',')` sai** vì ô có thể chứa dấu phẩy (`"Binh, Nguyen"`), dấu ngoặc kép
(`""` biểu diễn một `"`) và cả xuống dòng. Quy tắc chuẩn (RFC 4180): ô chứa `,`, `"` hoặc xuống dòng phải bọc trong `"..."`, và
mỗi `"` bên trong nhân đôi.

Code mẫu có bộ tách dòng đơn giản dạng **máy trạng thái** (đang trong hay ngoài ngoặc kép) để bạn hiểu nguyên lý, và hàm ghi
`DongGoiCsv` escape đúng. Với dữ liệu thật (xuống dòng trong ô, mã hoá lạ, dấu phân cách `;`, file lớn), hãy dùng thư viện
đã kiểm chứng như **CsvHelper**:

```
dotnet add package CsvHelper
```

```csharp
using var reader = new StreamReader("data.csv");
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
var dong = csv.GetRecords<SinhVien>().ToList();
```

Lưu ý **culture**: số thực `8.5` hay `8,5`? Excel tiếng Việt thường dùng dấu phẩy thập phân và `;` làm dấu phân cách. Khi trao
đổi dữ liệu máy–máy, dùng `CultureInfo.InvariantCulture` (Chương 11) và ghi rõ định dạng.

Với mọi dữ liệu người dùng tải lên: cẩn thận **CSV injection** — ô bắt đầu bằng `=`, `+`, `-`, `@` có thể bị Excel hiểu là công
thức; khi xuất ra cho người khác mở bằng Excel, thêm ký tự `'` phía trước các ô đó.

## JSON hay CSV hay XML?

| | JSON | CSV | XML |
|---|------|-----|-----|
| Cấu trúc | lồng nhau, linh hoạt | bảng phẳng | lồng nhau, nặng nề |
| Dùng cho | API, cấu hình, lưu object | trao đổi bảng tính, báo cáo | hệ thống cũ, tài liệu |
| Kiểu dữ liệu | có (số, bool, null) | chỉ chuỗi | chỉ chuỗi (+ schema) |

## Lỗi thường gặp

- Quên `[JsonIgnore]` → lộ mật khẩu/dữ liệu nhạy cảm.
- Tên thuộc tính lệch hoa/thường → giá trị mặc định (không lỗi!) — kiểm tra kết quả; bật case-insensitive hoặc dùng `JsonSerializerDefaults.Web`.
- `JsonException` do sai kiểu (`"abc"` vào `int`).
- Deserialize ra `null` (đầu vào `"null"`) → `NullReferenceException`.
- Dùng `Split(',')` cho CSV.
- Đọc `double` bằng `Parse` mà không chỉ culture → sai trên máy khác.
- Tạo `JsonSerializerOptions` mới mỗi lần gọi.
- Vòng tham chiếu (A trỏ B, B trỏ A) → `JsonException`; đặt `ReferenceHandler.IgnoreCycles` hoặc tách DTO.

## Bài tập

1. Serialize một `record` có `List<record>` lồng nhau ra JSON đẹp; đọc lại và so sánh (`==`) hai object.
2. Đọc JSON từ chuỗi gồm mảng người dùng, dùng `JsonDocument` in tên những người trên 18 tuổi.
3. Ghi danh sách sinh viên ra file CSV và đọc lại đúng, kể cả tên có dấu phẩy và ngoặc kép.
4. Thêm thuộc tính `NgayTao` và cấu hình để bỏ qua giá trị `null` khi ghi.
5. Viết converter tuỳ chỉnh `JsonConverter<T>` đọc/ghi ngày theo định dạng `dd/MM/yyyy`.
