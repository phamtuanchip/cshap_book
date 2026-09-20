# Chương 10 — Kiểm thử API

## Mục tiêu học

Sau chương này, bạn sẽ:

- Phân biệt **unit test**, **integration test** và khi nào dùng loại nào cho web.
- Viết integration test chạy **cả ứng dụng thật trong bộ nhớ** bằng `WebApplicationFactory<Program>`.
- **Thay thế dịch vụ** (cổng thanh toán, CSDL…) bằng bản giả trong test.
- Kiểm tra mã trạng thái, header, body JSON, xác thực, validation; giữ test độc lập và nhanh.

Code mẫu: [`code/ch10-kiem-thu-api/`](../../code/ch10-kiem-thu-api/) (API `DonHangApi` + project `DonHangApi.Tests`).

Nền tảng: xUnit, Arrange–Act–Assert, mock/stub — đã học ở Tập 1, Chương 38. Chương này áp dụng vào tầng HTTP.

## Hai tầng kiểm thử cho web

| | Unit test | Integration test (API) |
|---|-----------|------------------------|
| Đối tượng | một lớp/hàm (dịch vụ nghiệp vụ, validator) | **cả pipeline**: routing, binding, validation, filter, serialization |
| Phụ thuộc | mọi phụ thuộc bị thay bằng giả | dịch vụ **ngoại vi** bị thay; phần còn lại là thật |
| Tốc độ | mili-giây | vài chục mili-giây (không mạng thật) |
| Bắt được | lỗi logic | lỗi **kết nối các phần**: route sai, thiếu `Content-Type`, JSON sai tên, thứ tự middleware, DI thiếu đăng ký |

Unit test không thể phát hiện "quên `app.UseAuthentication()`" hay "đăng ký DI sai vòng đời" — integration test thì được. Cần cả hai: **nhiều unit test nhanh cho nghiệp vụ + vài chục integration test cho các luồng chính** (hình kim tự tháp).

## `WebApplicationFactory<Program>`

Package `Microsoft.AspNetCore.Mvc.Testing`. Nó khởi động **ứng dụng thật của bạn** trong tiến trình test bằng **TestServer** (không mở cổng mạng), và đưa cho bạn `HttpClient` gửi thẳng request vào pipeline.

Điều kiện: project test tham chiếu project web, và lớp `Program` phải nhìn thấy được. Với top-level statements `Program` là `internal` ẩn — thêm một dòng cuối `Program.cs`:

```csharp
public partial class Program;
```

Test đầu tiên:

```csharp
public class DonHangApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public DonHangApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task TrangChu_TraVeChuoiXacNhan()
    {
        var res = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("dang chay", await res.Content.ReadAsStringAsync());
    }
}
```

`IClassFixture<T>`: tạo factory **một lần cho cả class test** (khởi động app tốn chút thời gian) và chia sẻ. Vì thế trạng thái Singleton (kho dữ liệu trong bộ nhớ) **dùng chung giữa các test trong class** — viết test **không phụ thuộc thứ tự** (mỗi test tự tạo dữ liệu nó cần, không giả định "danh sách đang rỗng").

## Thay thế phụ thuộc bằng bản giả

API tạo đơn hàng gọi cổng thanh toán bên ngoài (`ICongThanhToan`). Test **không được** gọi cổng thật. Ghi đè đăng ký DI:

```csharp
public class ApiFactory : WebApplicationFactory<Program>
{
    public ICongThanhToan CongThanhToanGia { get; } = Substitute.For<ICongThanhToan>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddScoped(_ => CongThanhToanGia);        // đăng ký SAU → thắng đăng ký gốc
        });
    }
}
```

Đăng ký cuối cùng cho cùng một kiểu **thắng** — nên `AddScoped` trong `ConfigureServices` của test ghi đè bản thật. Trong test dàn kết quả:

```csharp
_factory.CongThanhToanGia
    .ThanhToanAsync(Arg.Any<decimal>(), Arg.Any<CancellationToken>())
    .Returns(new KetQuaThanhToan(true, "GD-TEST-01", null));
```

Đây là phần thưởng của việc **lập trình theo interface + DI** (Tập 1, Chương 42; Chương 5): thay phụ thuộc mà không sửa code sản phẩm. Tương tự thay `DbContext` bằng SQLite in-memory hoặc `IHttpClientFactory` bằng handler giả.

## Các loại kiểm tra thường gặp

```csharp
// 1. Tạo mới → 201 + Location + body
var res = await _client.PostAsJsonAsync("/api/don-hang", new { khachHang = "An", soTien = 250_000 });
Assert.Equal(HttpStatusCode.Created, res.StatusCode);
var don = await res.Content.ReadFromJsonAsync<DonHang>();
Assert.Equal($"/api/don-hang/{don!.Id}", res.Headers.Location?.OriginalString);

// 2. Tạo rồi đọc lại → đúng đối tượng (kiểm tra cả vòng khứ hồi)
var lay = await _client.GetFromJsonAsync<DonHang>($"/api/don-hang/{don.Id}");
Assert.Equal(don, lay);            // record: so sánh theo giá trị

// 3. Không tồn tại → 404
Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/don-hang/99999")).StatusCode);

// 4. Dữ liệu sai (Theory) → 400 + problem+json
[Theory]
[InlineData("", 1000)] [InlineData("An", 0)] [InlineData("An", -5)]
public async Task TaoDonHang_DuLieuSai_Tra400(string khachHang, decimal soTien) { ... }

// 5. Xác thực: client không có header → 401
var khongKhoa = _factory.CreateClient();                // client mới, không header mặc định
Assert.Equal(HttpStatusCode.Unauthorized, (await khongKhoa.GetAsync("/api/don-hang")).StatusCode);

// 6. Nhánh lỗi từ dịch vụ ngoài → 402 và KHÔNG lưu đơn
```

Mẹo:

- `PostAsJsonAsync`, `GetFromJsonAsync`, `ReadFromJsonAsync<T>` (`System.Net.Http.Json`) gọn hơn tự dựng chuỗi JSON.
- Dùng **DTO/record của chính dự án** để đọc response — nếu đổi tên trường, test biên dịch lỗi, báo ngay.
- Kiểm tra `Content-Type` cho lỗi (`application/problem+json`).
- Client mặc định **tự theo redirect**; cần kiểm tra redirect thì `CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })`.
- `builder.UseSetting("ApiKey", "khac")` hoặc `ConfigureAppConfiguration` để đổi cấu hình trong test.

## Kiểm thử với cơ sở dữ liệu

Từ Chương 12 có EF Core. Ba chiến lược cho test:

| Chiến lược | Ưu | Nhược |
|-----------|-----|-------|
| **SQLite in-memory** (`DataSource=:memory:`, giữ kết nối mở) | nhanh, gần CSDL thật, chạy khắp nơi | không giống 100% SQL Server/PostgreSQL |
| **EF InMemory provider** | rất nhanh | **không** mô phỏng ràng buộc, transaction, SQL → dễ "xanh giả"; **không khuyến khích** |
| **CSDL thật trong container** (Testcontainers) | chính xác nhất | chậm hơn, cần Docker (Tập 3) |

Nguyên tắc: test khớp môi trường thật nhất có thể chấp nhận về chi phí; luôn chạy migration/khởi tạo schema trong fixture; dọn dữ liệu (hoặc mỗi test một CSDL/transaction rollback).

## Tổ chức và chất lượng

- Đặt tên test mô tả tình huống: `TaoDonHang_ThanhToanThatBai_Tra402VaKhongLuuDon`.
- Test **hợp đồng** (contract): status code, header, hình dạng JSON — đây là thứ client phụ thuộc.
- Test **hành vi**, không test chi tiết cài đặt (đừng verify từng lời gọi nội bộ trừ khi đó là tác dụng phụ quan trọng, như gửi email).
- Test độc lập: không phụ thuộc thứ tự; dùng dữ liệu ngẫu nhiên/riêng (`Guid.NewGuid()` cho tên).
- Chạy trong **CI** mỗi commit (Tập 3: GitHub Actions `dotnet test`).
- Một số ít test **smoke** đầu–cuối trên môi trường thật sau deploy (gọi `/health`).

## File `.http` và kiểm thử thủ công

Song song test tự động, giữ file `.http` (như `customers.http` ở Chương 7) làm **tài liệu sống + thử nhanh** khi phát triển — khác test: chạy bằng tay, cho người đọc.

## Lỗi thường gặp

- `The type 'Program' is inaccessible` — quên `public partial class Program;`.
- Test chạy được riêng lẻ nhưng lỗi khi chạy cả bộ — **phụ thuộc thứ tự / trạng thái chung** trong Singleton dùng chung qua `IClassFixture`.
- Gọi dịch vụ ngoài thật trong test (chậm, không ổn định, tác dụng phụ).
- Không cấu hình môi trường/thiết lập → ứng dụng đọc `appsettings.Development.json` ngoài ý muốn (dùng `UseEnvironment("Testing")`).
- Dùng EF InMemory rồi tin rằng "test qua = CSDL chạy đúng".
- Bỏ qua kiểm tra nhánh lỗi (400/401/404/409/5xx) — chính là nơi lỗi hay ẩn.
- Test quá chi tiết vào JSON (so chuỗi thô) → giòn; nên đọc thành DTO.
- Quên `await` với `PostAsJsonAsync`… → test "xanh" nhưng không kiểm tra gì.

## Bài tập

1. Thêm `PUT /api/don-hang/{id}` (đổi tên khách) và viết bốn test: thành công (204), không tồn tại (404), dữ liệu sai (400), không có API key (401).
2. Viết test cho tình huống `ICongThanhToan` **ném exception** — API nên trả gì? Chỉnh code cho hợp lý (`502`/`503`) rồi viết test.
3. Đổi API key bằng cấu hình trong test (`builder.UseSetting("ApiKey", "khoa-khac")`) và kiểm tra khoá cũ bị từ chối.
4. Viết test kiểm tra header `Location` của `POST` dùng được để `GET` (theo `Location`, không tự ghép URL).
5. Chạy `dotnet test --collect:"XPlat Code Coverage"` và tìm nhánh chưa được test trong `Program.cs`.
