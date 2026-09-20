# Chương 8 — DTO và Validation

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **DTO** là gì và vì sao **không bao giờ** trả/nhận thẳng entity nội bộ.
- Kiểm tra dữ liệu bằng **DataAnnotations**, `IValidatableObject` và attribute tự viết.
- Đọc và tuỳ chỉnh lỗi validation theo chuẩn **ProblemDetails**.
- Validation cho Minimal API; biết tới FluentValidation; phân biệt kiểm tra **định dạng** và **nghiệp vụ**.

Code mẫu: [`code/ch08-dto-validation/`](../../code/ch08-dto-validation/).

## Vấn đề: đừng lộ entity

Ở Chương 7, controller nhận/trả thẳng class `Customer`. Với dữ liệu thật, đó là con đường dẫn tới lỗi và lộ bí mật:

```csharp
public class SanPham            // entity nội bộ
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public decimal Gia { get; set; }
    public decimal GiaVon { get; set; }      // bí mật kinh doanh!
    public string? EmailNhaCungCap { get; set; }
}

[HttpPost] public IActionResult Tao(SanPham sp) { ... }    // ✘ client tự gửi "id", "giaVon"
[HttpGet]  public List<SanPham> Lay() ...                   // ✘ trả cả GiaVon, email nội bộ
```

Hai lỗ hổng cùng một gốc:

- **Over-posting (mass assignment)**: client gửi thêm trường không được phép (`"id": 1`, `"isAdmin": true`, `"giaVon": 1`) và server gán luôn.
- **Data leak**: trả về mọi thuộc tính, gồm cả thứ nhạy cảm.

Ngoài ra, ràng buộc entity với API làm **mọi thay đổi bên trong** (đổi tên cột, tách bảng) vỡ hợp đồng với client.

## DTO — Data Transfer Object

**DTO** là kiểu chỉ dùng để **truyền dữ liệu qua ranh giới** (API), tách biệt với entity. Nguyên tắc: **DTO vào ≠ DTO ra ≠ entity.**

```csharp
// DTO vào: chỉ những trường client được phép gửi
public class TaoSanPhamRequest { public string? Ten { get; init; } public decimal Gia { get; init; } ... }

// DTO ra: chỉ những gì client cần biết
public record SanPhamResponse(int Id, string Ma, string Ten, decimal Gia, IReadOnlyList<string> The);
```

Anh xa (mapping) qua extension method:

```csharp
public static SanPhamResponse ToResponse(this SanPham s) => new(s.Id, s.Ma, s.Ten, s.Gia, s.The);
public static SanPham ToEntity(this TaoSanPhamRequest r) => new() { Ma = r.Ma!, Ten = r.Ten!.Trim(), Gia = r.Gia, GiaVon = r.Gia * 0.7m, ... };
```

Chạy code mẫu: `GET /api/san-pham/1` **không** có `giaVon`, dù entity có. Có thư viện mapping (**AutoMapper**, **Mapster**) nhưng nhiều đội chọn code tay: tường minh, kiểm tra được lúc biên dịch, không "phép thuật" — hợp với dự án vừa và nhỏ. Với `record`/`init` (Tập 1, Chương 32) DTO gọn và bất biến.

Quy ước đặt tên: `TaoSanPhamRequest`/`CapNhatSanPhamRequest` (vào), `SanPhamResponse`/`SanPhamDto` (ra); mỗi thao tác có DTO riêng vì trường bắt buộc khác nhau (tạo mới cần `Ma`; cập nhật thì không cho đổi `Ma`).

## DataAnnotations

```csharp
public class TaoSanPhamRequest
{
    [Required(ErrorMessage = "Ten san pham la bat buoc")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ten phai tu 2 den 100 ky tu")]
    public string? Ten { get; init; }

    [Required(ErrorMessage = "Ma san pham la bat buoc")]
    [RegularExpression("^[A-Z]{2}[0-9]{3}$", ErrorMessage = "Ma phai co dang 2 chu HOA + 3 so, vi du LT001")]
    public string? Ma { get; init; }

    [Range(1_000, 1_000_000_000, ErrorMessage = "Gia phai tu 1.000 den 1 ty")]
    public decimal Gia { get; init; }

    [EmailAddress] public string? EmailNhaCungCap { get; init; }
    [MaxLength(5)] public List<string> The { get; init; } = [];
}
```

| Attribute | Kiểm tra |
|-----------|---------|
| `[Required]` | không null (và với `string`: không rỗng) |
| `[StringLength]`, `[MinLength]`, `[MaxLength]` | độ dài chuỗi/số phần tử |
| `[Range]` | khoảng số/ngày |
| `[RegularExpression]` | khớp mẫu regex |
| `[EmailAddress]`, `[Url]`, `[Phone]`, `[CreditCard]` | định dạng thường gặp |
| `[Compare("Field")]` | bằng trường khác (xác nhận mật khẩu) |

**Lưu ý nullable:** thuộc tính `string Ten` (không nullable) bị coi là `[Required]` ngầm định dưới nullable reference types; nhưng để **thông báo lỗi** như mong muốn và phân biệt "thiếu" với "rỗng", khai báo `string?` kèm `[Required]` như trên rất phổ biến. Với kiểu giá trị (`decimal Gia`), thiếu trong JSON sẽ mang giá trị `0` (không phải "thiếu") — nếu cần bắt buộc thật sự hãy dùng `decimal?` + `[Required]` hoặc `required` (C# 11).

### Kết quả: 400 với ValidationProblemDetails

Với `[ApiController]`, request sai **không chạm tới action**; framework trả ngay:

```json
{
  "title": "Du lieu gui len khong hop le",
  "status": 400,
  "instance": "/api/san-pham",
  "errors": {
    "Ma":  ["Ma phai co dang 2 chu HOA + 3 so, vi du LT001"],
    "Gia": ["Gia phai tu 1.000 den 1 ty"],
    "Ten": ["Ten khong duoc chua tu 're rach'"],
    "The": ["Toi da 5 the"],
    "EmailNhaCungCap": ["Email lien he khong dung dinh dang"]
  },
  "traceId": "0HN..."
}
```

Mọi lỗi được trả **một lần** (không dừng ở lỗi đầu tiên) để client hiển thị tất cả bên cạnh các ô nhập. Dạng này chuẩn **RFC 9457 Problem Details** (`application/problem+json`). Tuỳ chỉnh (đổi tiêu đề, thêm `traceId`) qua `ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ...)` như trong `Program.cs` mẫu.

## Attribute tự viết

Dùng lại một quy tắc ở nhiều DTO (nhờ Tập 1, Chương 37 — attribute):

```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class KhongChuaTuCamAttribute(params string[] tuCam) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is string s)
            foreach (var tu in tuCam)
                if (s.Contains(tu, StringComparison.OrdinalIgnoreCase))
                    return new ValidationResult($"{ctx.DisplayName} khong duoc chua tu '{tu}'", [ctx.MemberName!]);
        return ValidationResult.Success;
    }
}

[KhongChuaTuCam("re rach", "hang gia")] public string? Ten { get; init; }
```

## Kiểm tra chéo nhiều trường: `IValidatableObject`

```csharp
public class TaoSanPhamRequest : IValidatableObject
{
    public DateOnly? KhuyenMaiTu { get; init; }
    public DateOnly? KhuyenMaiDen { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (KhuyenMaiTu is { } tu && KhuyenMaiDen is { } den && den < tu)
            yield return new ValidationResult("Ngay ket thuc khuyen mai phai sau ngay bat dau",
                [nameof(KhuyenMaiTu), nameof(KhuyenMaiDen)]);
    }
}
```

> **Bẫy:** `Validate` của `IValidatableObject` **chỉ chạy khi mọi validation cấp thuộc tính đã đạt.** Trong ví dụ "nhiều lỗi" ở trên, lỗi ngày khuyến mãi không xuất hiện vì các lỗi khác đã chặn; gửi riêng một request chỉ sai ngày sẽ thấy nó. Đây là hành vi có chủ đích, đừng ngạc nhiên.

## Định dạng ≠ nghiệp vụ

Hai tầng kiểm tra khác nhau:

| | Validation **đầu vào** (định dạng) | Kiểm tra **nghiệp vụ** |
|---|---|---|
| Ví dụ | tên không rỗng, email đúng dạng, giá trong khoảng | **mã sản phẩm chưa tồn tại**, đủ tồn kho, khách chưa bị khoá |
| Cần dữ liệu ngoài request? | Không | **Có** (CSDL, dịch vụ khác) |
| Nơi thực hiện | DataAnnotations/filter | **dịch vụ nghiệp vụ** (ném exception nghiệp vụ hoặc trả Result) |
| Lỗi | `400 Bad Request` | thường `409 Conflict`, `422`, `404`... tuỳ trường hợp |

Trong controller mẫu: kiểm tra trùng mã là nghiệp vụ (cần kho dữ liệu), nên viết trong action/dịch vụ, và vẫn dùng `ModelState.AddModelError` + `ValidationProblem(ModelState)` để định dạng lỗi cùng dạng với các lỗi khác:

```csharp
if (kho.TonTaiMa(req.Ma!))
{
    ModelState.AddModelError(nameof(req.Ma), $"Ma {req.Ma} da ton tai");
    return ValidationProblem(ModelState);
}
```

Đừng tin chỉ validation phía client (JavaScript) — dễ bị bỏ qua. **Server luôn validate lại.**

## Validation cho Minimal API

Minimal API **không** tự validate DataAnnotations (trước .NET 10; .NET 10 đã thêm `AddValidation()` cho Minimal API — kiểm tra tài liệu phiên bản bạn dùng). Cách chung, chạy trên mọi phiên bản: **endpoint filter** dùng `Validator.TryValidateObject`:

```csharp
app.MapPost("/minimal/san-pham", (TaoSanPhamRequest req) => ...)
   .AddEndpointFilter(async (ctx, next) =>
   {
       foreach (var arg in ctx.Arguments.Where(a => a is not null))
       {
           var ketQua = new List<ValidationResult>();
           if (!Validator.TryValidateObject(arg!, new ValidationContext(arg!), ketQua, validateAllProperties: true))
               return Results.ValidationProblem(GomLoi(ketQua));
       }
       return await next(ctx);
   });
```

Mã đầy đủ trong `Program.cs` mẫu; nên rút thành filter dùng lại cho cả nhóm route.

## FluentValidation

Khi quy tắc phức tạp, phụ thuộc lẫn nhau hay cần test riêng, nhiều dự án dùng thư viện **FluentValidation** — viết quy tắc bằng code thay vì attribute:

```csharp
public class TaoSanPhamValidator : AbstractValidator<TaoSanPhamRequest>
{
    public TaoSanPhamValidator()
    {
        RuleFor(x => x.Ten).NotEmpty().Length(2, 100);
        RuleFor(x => x.Gia).InclusiveBetween(1_000, 1_000_000_000);
        RuleFor(x => x.KhuyenMaiDen).GreaterThan(x => x.KhuyenMaiTu).When(x => x.KhuyenMaiTu is not null);
    }
}
```

Ưu: quy tắc **tách khỏi DTO**, dễ đọc, dễ unit test, hỗ trợ điều kiện/quy tắc bất đồng bộ (gọi CSDL). Nhược: thêm phụ thuộc. Sẽ dùng ở Tập 3 (pipeline validation).

## Lỗi thường gặp

- Trả thẳng entity ra API → lộ trường nhạy cảm; nhận thẳng entity → over-posting.
- Dùng cùng một DTO cho tạo và cập nhật nên phải nới lỏng `[Required]`.
- Tưởng `[Required]` trên `int/decimal` bắt được trường vắng (giá trị mặc định `0` vẫn "có giá trị").
- Chỉ validate ở client.
- Nhồi kiểm tra nghiệp vụ (cần CSDL) vào attribute → khó test, phụ thuộc DI khó chịu.
- Trả lỗi `500` thay vì `400/409` khi dữ liệu sai; hoặc trả `200` kèm `"error": ...`.
- Thông báo lỗi lộ chi tiết nội bộ (tên bảng, câu SQL).
- Quên `IValidatableObject` chỉ chạy sau khi các lỗi thuộc tính đã sạch.

## Bài tập

1. Thêm `CapNhatSanPhamRequest` (không cho đổi `Ma`) và action `PUT /api/san-pham/{id}`; ánh xạ vào entity.
2. Viết attribute `[NgayTuongLai]` chỉ chấp nhận `DateOnly` sau hôm nay.
3. Thêm `SanPhamSummaryResponse` (id, tên, giá) cho danh sách và `SanPhamDetailResponse` cho chi tiết.
4. Viết một endpoint filter dùng lại (generic) `ValidationFilter<T>` cho Minimal API và áp dụng cho `MapGroup`.
5. Thử gửi `{"id":999,"giaVon":1,"ten":"A"}` tới action nhận DTO và tới action nhận entity; so sánh hậu quả.
