# Chương 4 — Routing và Minimal API

## Mục tiêu học

Sau chương này, bạn sẽ:

- Định nghĩa route với `MapGet/MapPost/MapPut/MapDelete`, route constraint và `MapGroup`.
- Hiểu **parameter binding**: route, query, header, body, dịch vụ (DI).
- Trả kết quả bằng `TypedResults` và khai báo rõ kiểu trả về (`Results<...>`).
- Xây một API **CRUD** hoàn chỉnh cho `Todo`, có validation và tài liệu OpenAPI.

Code mẫu: [`code/ch04-minimal-api/`](../../code/ch04-minimal-api/).

## Routing là gì?

**Routing** khớp **phương thức + đường dẫn** của request với một **endpoint** (handler). Trong pipeline (Chương 3), middleware routing chọn endpoint, các middleware sau đọc metadata của nó (vd `[Authorize]`), rồi endpoint chạy.

```csharp
app.MapGet("/chao/{ten}", (string ten) => $"Xin chao, {ten}!");
app.MapPost("/todos", (TaoTodo req) => ...);
app.MapPut("/todos/{id}", ...);
app.MapDelete("/todos/{id}", ...);
app.MapMethods("/x", ["GET", "HEAD"], handler);
```

### Tham số route và ràng buộc (constraint)

```csharp
app.MapGet("/todos/{id:int}", (int id) => ...);          // chỉ khớp khi id là số nguyên
app.MapGet("/nguoi-dung/{ten:alpha:minlength(3)}", ...);
app.MapGet("/file/{**duongDan}", (string duongDan) => ...);   // "catch-all": bắt cả phần còn lại
app.MapGet("/bai-viet/{slug?}", (string? slug) => ...);       // tham số tuỳ chọn
```

Constraint thường dùng: `int`, `long`, `guid`, `bool`, `datetime`, `alpha`, `min(1)`, `max(100)`, `range(1,10)`, `length(5)`, `regex(...)`. Không khớp → `404` (không tới handler).

## Parameter binding

Minimal API tự suy ra nguồn dữ liệu của mỗi tham số handler:

| Nguồn | Khi nào | Ví dụ |
|-------|---------|-------|
| **Route** | tên khớp `{tham-so}` trong đường dẫn | `int id` |
| **Query string** | kiểu đơn giản, không có trong route | `bool? done`, `string? tim` |
| **Body (JSON)** | kiểu phức tạp (class/record) | `TaoTodoRequest req` |
| **Header** | `[FromHeader]` | `[FromHeader(Name="X-Khach")] string? khach` |
| **Dịch vụ (DI)** | kiểu đã đăng ký trong container | `TodoStore kho`, `ILogger<Program> log` |
| **Đặc biệt** | `HttpContext`, `HttpRequest`, `ClaimsPrincipal`, `CancellationToken`, `IFormFile` | |

Có thể chỉ định tường minh: `[FromRoute]`, `[FromQuery]`, `[FromBody]`, `[FromServices]`, `[FromForm]`. Gom nhiều tham số query vào một kiểu bằng `[AsParameters]`:

```csharp
record TimKiem(int Trang = 1, int KichThuoc = 10);
app.MapGet("/vi-du/{id:int}", (int id, [AsParameters] TimKiem tk) => new { id, tk.Trang, tk.KichThuoc });
// GET /vi-du/7?trang=2&kichThuoc=5
```

Kiểu **nullable** (`bool? done`, `string? tim`) nghĩa là **tuỳ chọn**; kiểu không nullable mà thiếu thì `400 Bad Request` tự động. Body chỉ được có **một** tham số kiểu phức tạp.

## Kết quả trả về

Trả `string`, object (→ JSON) cho nhanh; hoặc `Results/TypedResults` để kiểm soát mã trạng thái:

```csharp
TypedResults.Ok(todo)                                  // 200 + JSON
TypedResults.Created($"/api/todos/{id}", todo)         // 201 + header Location
TypedResults.NoContent()                               // 204
TypedResults.NotFound()                                // 404
TypedResults.BadRequest("Ly do")                       // 400
TypedResults.ValidationProblem(loi)                    // 400 + dạng ProblemDetails (RFC 9457)
TypedResults.Conflict(), .Unauthorized(), .Forbid(), .File(...), .Redirect(...), .Stream(...)
```

Nên dùng **`TypedResults`** (kiểu cụ thể) và khai báo kiểu trả về là **`Results<A, B, ...>`** — trình biên dịch biết mọi trường hợp có thể xảy ra, OpenAPI tự sinh đúng, và test dễ (kiểm tra kiểu kết quả):

```csharp
todos.MapGet("/{id:int}", Results<Ok<Todo>, NotFound> (int id, TodoStore kho)
    => kho.Tim(id) is { } t ? TypedResults.Ok(t) : TypedResults.NotFound())
    .WithName("LayTodo");
```

Đây là cách dùng pattern matching `is { } t` (Tập 1, Chương 32). Khi trả lỗi, luôn đi kèm **mã đúng**: `201` cho tạo mới, `204` cho cập nhật/xoá không body, `404` cho không tìm thấy, `400` cho dữ liệu sai.

## `MapGroup` — nhóm endpoint

```csharp
var todos = app.MapGroup("/api/todos").WithTags("Todos");
todos.MapGet("/", ...);          // GET /api/todos
todos.MapGet("/{id:int}", ...);  // GET /api/todos/5
todos.MapPost("/", ...);         // POST /api/todos
```

Nhóm dùng chung **tiền tố**, **metadata** (`WithTags`, `RequireAuthorization()`, `RequireRateLimiting`), **filter** — đỡ lặp lại. Có thể lồng nhóm (`api.MapGroup("/v1")`).

## CRUD hoàn chỉnh

```csharp
// LIST + lọc: GET /api/todos?done=true&tim=hoc
todos.MapGet("/", (TodoStore kho, bool? done, string? tim) =>
{
    var ds = kho.TatCa().AsEnumerable();
    if (done is not null) ds = ds.Where(t => t.Done == done);
    if (!string.IsNullOrWhiteSpace(tim)) ds = ds.Where(t => t.Title.Contains(tim, StringComparison.OrdinalIgnoreCase));
    return TypedResults.Ok(ds.ToList());
});

// CREATE: 201 + Location, hoặc 400 khi dữ liệu sai
todos.MapPost("/", Results<Created<Todo>, ValidationProblem> (TaoTodoRequest req, TodoStore kho) =>
{
    var loi = req.KiemTra();
    if (loi.Count > 0) return TypedResults.ValidationProblem(loi);
    var moi = kho.Them(req.Title!.Trim());
    return TypedResults.Created($"/api/todos/{moi.Id}", moi);
});

// UPDATE / DELETE: 204 hoặc 404
todos.MapPut("/{id:int}", ...);
todos.MapDelete("/{id:int}", ...);
```

Thử bằng `curl`:

```
curl -i -X POST http://localhost:5204/api/todos -H "Content-Type: application/json" -d '{"title":"Viet sach"}'
# HTTP/1.1 201 Created   Location: /api/todos/3

curl -X POST http://localhost:5204/api/todos -H "Content-Type: application/json" -d '{"title":""}'
# {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"title":["Tieu de khong duoc rong"]}}
```

Body lỗi theo chuẩn **`ProblemDetails`** (RFC 9457) — cách thống nhất mô tả lỗi cho client (Chương 8, 9).

## Dữ liệu dùng chung an toàn đa luồng

`TodoStore` đăng ký `AddSingleton` và được **nhiều request chạy song song** gọi cùng lúc → mọi thao tác trên `List` bên trong phải **khoá** (`lock`, Tập 1, Chương 34). Ở Chương 12 ta thay bằng cơ sở dữ liệu (EF Core) — lúc đó CSDL lo tính đúng đắn. Quy tắc nhớ: **singleton phải thread-safe**.

## Endpoint filter

Filter cho chạy code **trước/sau handler**, đã biết tham số:

```csharp
todos.MapPost("/", handler).AddEndpointFilter(async (ctx, next) =>
{
    var req = ctx.GetArgument<TaoTodoRequest>(0);
    if (string.IsNullOrWhiteSpace(req.Title))
        return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["Bat buoc"] });
    return await next(ctx);       // hợp lệ → chạy handler
});
```

Dùng cho validation dùng chung, log theo endpoint, chuẩn hoá kết quả. Áp cho cả nhóm: `todos.AddEndpointFilter(...)`.

## OpenAPI — tài liệu API tự sinh

```csharp
builder.Services.AddOpenApi();                                    // package Microsoft.AspNetCore.OpenApi
if (app.Environment.IsDevelopment()) app.MapOpenApi();            // GET /openapi/v1.json
```

Tài liệu mô tả mọi endpoint, tham số, kiểu request/response, mã trạng thái (lấy từ `Results<...>`). Các công cụ như **Swagger UI**, **Scalar**, Postman đọc file này để dựng giao diện thử API và sinh code client. Chi tiết ở Chương 9.

## Khi nào Minimal API, khi nào Controller?

| | Minimal API | Controller (Chương 7) |
|---|---|---|
| Độ dài code | rất ngắn | nhiều "nghi thức" hơn |
| Tổ chức | tự do (nhóm, file riêng, class static) | quy ước rõ ràng, class theo tài nguyên |
| Tính năng nâng cao (filters, model binding tuỳ biến, versioning sẵn) | có, đang hoàn thiện | đầy đủ, lâu đời |
| Hợp với | microservice, API nhỏ–vừa, dự án mới | API lớn, đội quen MVC, cần nhiều convention |

Hai phong cách cùng nền tảng, có thể **dùng chung trong một dự án**. Khi dự án lớn, đừng nhét mọi thứ vào `Program.cs`: tách mỗi tài nguyên thành một **extension method** `app.MapTodoEndpoints()` trong file riêng.

## Lỗi thường gặp

- `400` do quên `Content-Type: application/json` hoặc JSON sai → kiểm tra body/header.
- Handler có hai tham số phức tạp → lỗi khi khởi động ("cannot infer body"); gộp thành một kiểu hoặc chỉ định `[FromServices]`.
- Route bị xung đột (`AmbiguousMatchException`) — hai endpoint khớp cùng URL; thu hẹp bằng constraint/đổi URL.
- Quên constraint `:int` → `/todos/abc` rơi vào handler và lỗi bind.
- Trả `200` kèm `null` cho "không tìm thấy" thay vì `404`.
- Singleton giữ dữ liệu mà không khoá / không thread-safe.
- Đặt logic nghiệp vụ dài dằng dặc ngay trong lambda — hãy tách vào dịch vụ (Chương 5).

## Bài tập

1. Thêm `PATCH /api/todos/{id}/hoan-thanh` đánh dấu xong; trả `204` hoặc `404`.
2. Thêm phân trang `GET /api/todos?trang=1&kichThuoc=10` dùng `[AsParameters]` và trả header `X-Tong-So`.
3. Thêm route constraint để `GET /api/todos/{id:int:min(1)}` từ chối `0`, `-5`.
4. Viết endpoint filter chung cho nhóm `/api/todos` bắt buộc header `X-Api-Key`, trả `401` nếu thiếu.
5. Tách các endpoint Todo thành extension method `MapTodoEndpoints` trong file `TodoEndpoints.cs`.
