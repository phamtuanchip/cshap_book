# Chương 7 — Web API với Controller

## Mục tiêu học

Sau chương này, bạn sẽ:

- Viết Web API theo kiểu **controller**: `ControllerBase`, `[ApiController]`, `[Route]`, `[HttpGet]`...
- Hiểu vòng đời request qua **MVC pipeline**: routing → model binding → validation → action → result.
- Trả kết quả đúng chuẩn: `ActionResult<T>`, `CreatedAtAction`, `NoContent`, `NotFound`.
- Tách nghiệp vụ khỏi controller (controller **mỏng**), nhận dịch vụ qua **DI**.

Code mẫu: [`code/ch07-controller-api/`](../../code/ch07-controller-api/) — bản hiện đại hoá của `CustomersController` và `UserController` mà repo này bắt đầu từ đó.

## Controller hay Minimal API?

Chương 4 đã viết API bằng Minimal API. **Controller** là kiểu truyền thống (từ ASP.NET MVC), vẫn được dùng rất rộng rãi — bạn sẽ gặp nó trong hầu hết code có sẵn. Cùng nền tảng (routing, DI, middleware, JSON), khác cách tổ chức:

- Mỗi **controller** là một class gom các **action** (phương thức) của một tài nguyên.
- Nhiều **quy ước** có sẵn: lấy tên route từ tên class, tự validation model, filter theo nhóm, `[Authorize]` trên class...

Chọn cái nào ở cuối Chương 4; điều quan trọng là **hiểu cả hai**.

## Cài đặt

```csharp
builder.Services.AddControllers();                  // đăng ký MVC controllers
builder.Services.AddRouting(o => o.LowercaseUrls = true);   // URL sinh ra viết thường
...
app.MapControllers();                                // gắn [Route] của controller vào pipeline
```

## Controller đầu tiên

Ta viết lại `CustomersController` gốc của repo:

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController(ICustomerService customers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers([FromQuery] string? search, CancellationToken ct)
        => Ok(await customers.GetAllAsync(search, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetCustomerById(int id, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer, CancellationToken ct)
    {
        var created = await customers.CreateAsync(customer, ct);
        return CreatedAtAction(nameof(GetCustomerById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, Customer customer, CancellationToken ct)
        => await customers.UpdateAsync(id, customer, ct) ? NoContent() : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken ct)
        => await customers.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
```

So với bản gốc: ba chỗ đã được **thay đổi có chủ đích**:

1. Dữ liệu không còn "giả cứng" trong action mà nằm trong **dịch vụ** `ICustomerService` được tiêm qua constructor (**primary constructor** C# 12) — controller chỉ lo HTTP.
2. Action **bất đồng bộ** (`async Task`) và nhận `CancellationToken` — khi client ngắt kết nối, việc dở dang được huỷ (Tập 1, Chương 33).
3. Route `{id:int}` có **constraint**; `PUT`/`DELETE` trả `404` khi không có, thay vì luôn `204`.

## Các thành phần cần hiểu

### `ControllerBase` và `Controller`

`ControllerBase` cho API (không có view). `Controller` (kế thừa `ControllerBase`) thêm hỗ trợ **View** cho MVC trả HTML (Chương 15). Web API dùng `ControllerBase`.

### `[ApiController]`

Bật cả bộ hành vi tiện cho API:

- **Tự trả `400`** với `ValidationProblemDetails` khi model không hợp lệ (không cần tự viết `if (!ModelState.IsValid)`).
- **Suy ra nguồn binding**: kiểu phức tạp → `[FromBody]`; tham số trùng tên route → `[FromRoute]`; kiểu đơn giản còn lại → `[FromQuery]`.
- Bắt buộc dùng **attribute routing**; trả lỗi `4xx` dạng ProblemDetails.
- Body sai JSON → `400`.

### Route

```csharp
[Route("api/[controller]")]        // [controller] = tên class bỏ hậu tố "Controller" → "api/customers"
[HttpGet("{id:int}")]               // ghép: GET /api/customers/{id}
[HttpPost]                          // POST /api/customers
[HttpGet("~/thong-ke")]            // "~/" bỏ tiền tố controller: GET /thong-ke
```

Các token: `[controller]`, `[action]`, `[area]`. Nhiều dự án đặt tên tường minh (`[Route("api/khach-hang")]`) để URL không phụ thuộc tên class (đổi tên class → vỡ URL).

### Model binding — dữ liệu request → tham số

| Attribute | Nguồn |
|-----------|-------|
| `[FromRoute]` | phần `{id}` trong URL |
| `[FromQuery]` | query string |
| `[FromBody]` | body (JSON) — chỉ **một** tham số |
| `[FromHeader]` | header |
| `[FromForm]` | form/multipart (upload file: `IFormFile`) |
| `[FromServices]` | DI |

Binding **lỗi kiểu** (`/api/customers/abc`, hay số truyền chuỗi) → `400` hoặc `404` (nếu vướng constraint). Trong bản mẫu, `GET /api/customers/abc` trả `404` vì constraint `:int` không khớp.

### Kiểu trả về

| Kiểu | Khi nào |
|------|---------|
| `ActionResult<T>` | **khuyến nghị**: trả `T` (200) hoặc kết quả lỗi; OpenAPI biết kiểu `T` |
| `IActionResult` | khi không có body/nhiều dạng khác nhau |
| `T` hoặc `Task<T>` | luôn 200 (không nên nếu có thể lỗi) |
| `IAsyncEnumerable<T>` | trả luồng lớn |

Helper (kế thừa từ `ControllerBase`): `Ok()`, `Created()`, **`CreatedAtAction(...)`**, `NoContent()`, `BadRequest()`, `NotFound()`, `Conflict()`, `Unauthorized()`, `Forbid()`, `ValidationProblem()`, `Problem()`, `File()`, `Redirect()`.

`CreatedAtAction(nameof(GetCustomerById), new { id = created.Id }, created)` trả `201 Created` **kèm header `Location`** trỏ tới action lấy đối tượng mới:

```
HTTP/1.1 201 Created
Location: http://localhost:5207/api/customers/4
{"id":4,"name":"Alice","email":"a@x.com"}
```

Khai báo mã trả về cho tài liệu API: `[ProducesResponseType<Customer>(StatusCodes.Status201Created)]`, `[ProducesResponseType(StatusCodes.Status404NotFound)]`.

## Controller mỏng, nghiệp vụ trong dịch vụ

Một quy tắc quan trọng: **controller chỉ làm việc của tầng HTTP**:

1. Nhận dữ liệu (đã bind/validate),
2. Gọi **một** dịch vụ nghiệp vụ,
3. Chuyển kết quả thành **mã HTTP + body**.

Quy tắc kinh doanh, truy cập CSDL, gửi email… thuộc về dịch vụ/domain. Lợi ích: nghiệp vụ test được **không cần HTTP** (unit test Tập 1, Chương 38); dùng lại được (cho worker, gRPC, console); đổi CSDL không đụng controller.

```csharp
public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(string? search, CancellationToken ct);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct);
    ...
}
builder.Services.AddSingleton<ICustomerService, InMemoryCustomerService>();   // Chương 12: thay bằng EF Core
```

`UserController` gốc dùng `IUserRepository` đúng theo hướng này; bản mới chỉ đổi thành primary constructor và trả `201`.

## Thử API

File `customers.http` đi kèm (mở bằng VS/VS Code hoặc dùng `curl`):

```
curl -s http://localhost:5207/api/customers?search=jo
curl -i -X POST http://localhost:5207/api/customers -H "Content-Type: application/json" -d '{"name":"Alice","email":"a@x.com"}'
curl -i -X PUT  http://localhost:5207/api/customers/2 -H "Content-Type: application/json" -d '{"name":"J2","email":"j@x.com"}'
curl -i -X DELETE http://localhost:5207/api/customers/3
```

## Filters — chèn xử lý quanh action

Tương tự middleware nhưng **biết action, tham số, kết quả**:

```
Authorization → Resource → [Model binding] → Action filter (trước) → ACTION → Action filter (sau) → Result filter → Exception filter (khi lỗi)
```

```csharp
public class GhiThoiGianAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext ctx) { /* trước action */ }
    public override void OnActionExecuted(ActionExecutedContext ctx) { /* sau action */ }
}

[GhiThoiGian] public class CustomersController { ... }     // áp cho cả controller (hoặc từng action)
```

Loại filter: **Authorization** (`[Authorize]`), **Resource**, **Action**, **Exception**, **Result**. Dùng cho vấn đề xuyên suốt gắn với MVC (audit, chuẩn hoá kết quả). Xử lý lỗi toàn cục tốt hơn nên ở middleware/`IExceptionHandler` (Chương 9).

## Lỗi thường gặp

- `404` cho endpoint tồn tại — quên `app.MapControllers()`, hoặc route/constraint không khớp (`abc` cho `{id:int}`).
- `415 Unsupported Media Type` — body JSON nhưng thiếu `Content-Type: application/json`.
- `AmbiguousMatchException: The request matched multiple endpoints` — hai action cùng route + phương thức.
- Action trả `200` cho mọi tình huống thay vì `404/201/204` đúng nghĩa.
- Nhét nghiệp vụ và truy cập CSDL vào controller ("fat controller").
- Trả thẳng entity CSDL (lộ trường nhạy cảm) — dùng DTO (Chương 8).
- Quên `CancellationToken`, hoặc chặn luồng bằng `.Result` thay vì `await`.
- Trả `List<T>` lớn không phân trang.

## Bài tập

1. Thêm `GET /api/customers/{id}/don-hang` (route lồng) — trả danh sách rỗng, chú ý đặt tên và mã trạng thái.
2. Thêm `PATCH /api/customers/{id}/email` nhận body `{ "email": "..." }`.
3. Viết `ActionFilter` `[GhiLog]` in tên action và thời gian; gắn vào controller.
4. Đổi tên class `CustomersController` thành `KhachHangController` và quan sát URL; sau đó cố định URL bằng `[Route("api/customers")]`.
5. Viết unit test (không HTTP) cho `InMemoryCustomerService` và một test kiểm tra `CustomersController` bằng dịch vụ giả.
