# Chương 16 — Razor Pages

## Mục tiêu học

Sau chương này, bạn sẽ:

- Dùng **Razor Pages** — mỗi trang là một cặp `.cshtml` + **`PageModel`** — để xây ứng dụng CRUD.
- Nắm **handler** (`OnGet`, `OnPost`, handler có tên), `[BindProperty]`, route của trang.
- Kết nối **EF Core**, dropdown (`SelectList`), partial dùng chung Create/Edit, Post–Redirect–Get.
- Phân biệt với MVC và biết khi nào chọn Razor Pages.

Code mẫu: [`code/ch16-razor-pages/`](../../code/ch16-razor-pages/) — CRUD sản phẩm có nhóm, tìm kiếm, phân trang, dùng EF Core + SQLite.

## Ý tưởng: một trang, một tệp lô-gic

Trong MVC (Chương 15) một chức năng "thêm sản phẩm" rải ở ba nơi: controller, view, ViewModel. **Razor Pages** gom về **một chỗ**: mỗi trang gồm

```
Pages/SanPhams/Create.cshtml        # giao diện (Razor)
Pages/SanPhams/Create.cshtml.cs     # PageModel: dữ liệu + các "handler" xử lý request của trang này
```

**URL suy ra từ đường dẫn file**: `Pages/SanPhams/Create.cshtml` → `/SanPhams/Create`; `Pages/Index.cshtml` → `/`. Không cần bảng route. Trang chỉ chạy khi có dòng `@page` ở đầu `.cshtml` (đó chính là thứ phân biệt Razor Page với view thường).

```csharp
builder.Services.AddRazorPages();
...
app.MapRazorPages();
```

Với chức năng xoay quanh **trang** (form, danh sách, chi tiết), cách tổ chức này gọn và dễ tìm hơn MVC.

## PageModel và handler

```csharp
public class CreateModel(CuaHangDb db) : PageModel            // DI qua constructor
{
    [BindProperty] public SanPhamInput Input { get; set; } = new();
    public SelectList DanhSachNhom { get; private set; } = null!;

    public async Task OnGetAsync() => await NapNhomAsync();     // GET /SanPhams/Create

    public async Task<IActionResult> OnPostAsync()             // POST /SanPhams/Create
    {
        if (await db.SanPhams.AnyAsync(s => s.Ma == Input.Ma))
            ModelState.AddModelError("Input.Ma", "Mã sản phẩm đã tồn tại");
        if (!ModelState.IsValid) { await NapNhomAsync(); return Page(); }     // hiện lại form + lỗi

        var sp = new SanPham { Ma = Input.Ma!, Ten = Input.Ten!.Trim(), Gia = Input.Gia, Ton = Input.Ton, NhomId = Input.NhomId };
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        TempData["ThongBao"] = $"Đã thêm {sp.Ten}";
        return RedirectToPage("Details", new { id = sp.Id });    // Post–Redirect–Get
    }
}
```

- **Handler** đặt tên `On<Verb>[Tên]Async`: `OnGet`, `OnPost`, `OnPostXoa`, `OnGetExport`… Framework chọn theo phương thức HTTP và tham số `?handler=Tên`.
- Kiểu trả về: `void`/`Task` (hiển thị trang mặc định), hoặc `IActionResult`: `Page()`, `RedirectToPage(...)`, `NotFound()`, `File(...)`, `StatusCode(...)`.
- `PageModel` có `ModelState`, `TempData`, `User`, `Request`, `HttpContext` sẵn.

### `[BindProperty]` — gắn dữ liệu form

`[BindProperty]` cho phép framework **gán dữ liệu form vào thuộc tính** trước khi chạy handler `POST` (nên handler chỉ cần đọc `Input`). Mặc định **chỉ áp dụng khi POST**. Với trang tìm kiếm cần lấy từ query khi `GET`: `[BindProperty(SupportsGet = true)] public string? TuKhoa { get; set; }`.

**Quan trọng về bảo mật:** chỉ `[BindProperty]` cho kiểu **nhập liệu riêng** (`SanPhamInput`), **không** cho entity (`SanPham`) — nếu không kẻ tấn công thêm trường ngoài ý muốn vào form (`Id`, `GiaVon`, `IsAdmin`) và bị gán (over-posting). Ở handler `Edit`, tải entity thật rồi **chỉ gán các trường cho phép**.

### Tham số từ route

```cshtml
@page "{id:int}"                    @* /SanPhams/Edit/5, id phải là số *@
```
```csharp
public async Task<IActionResult> OnGetAsync(int id) { ... }          // id lấy từ route (hoặc query)
```

### Handler có tên — nhiều nút trên cùng trang

```cshtml
<form method="post" asp-page-handler="Xoa" asp-route-id="@sp.Id">
    <button type="submit" class="btn nguy-hiem">Xoá</button>
</form>
```
```csharp
public async Task<IActionResult> OnPostXoaAsync(int id)
{
    await db.SanPhams.Where(s => s.Id == id).ExecuteDeleteAsync();
    TempData["ThongBao"] = "Đã xoá sản phẩm";
    return RedirectToPage(new { TuKhoa, Trang });          // giữ lại từ khoá/trang hiện tại
}
```

Mỗi hàng của bảng có form `POST` riêng (xoá phải là `POST` + có token chống CSRF).

## Giao diện: tag helper, partial, layout

Cùng Razor và tag helper như MVC (Chương 15), nhưng dùng `asp-page` thay `asp-controller/asp-action`:

```cshtml
<a asp-page="Details" asp-route-id="@sp.Id">@sp.Ten</a>              @* /SanPhams/Details/3 *@
<a asp-page="/SanPhams/Create">Thêm mới</a>                           @* đường dẫn tuyệt đối từ gốc Pages *@
<input asp-for="Input.Ma" />                                         @* name="Input.Ma" *@
<select asp-for="Input.NhomId" asp-items="Model.DanhSachNhom"></select>
```

**Form dùng chung Create/Edit** bằng partial `_Form.cshtml`:

```cshtml
<form method="post">
    <partial name="_Form" for="Input" view-data="ViewData" />       @* for="Input": tiền tố tên trường "Input." tự thêm *@
    <button type="submit" class="btn">Lưu</button>
</form>
```

Danh sách chọn: dựng `SelectList` từ CSDL trong `PageModel` (`new SelectList(nhoms, nameof(Nhom.Id), nameof(Nhom.Ten), Input.NhomId)`), truyền vào `asp-items`. Nhớ **nạp lại** danh sách khi trả `Page()` sau lỗi validation, vì request POST không còn giữ nó.

`Pages/Shared/_Layout.cshtml`, `_ViewStart.cshtml`, `_ViewImports.cshtml` hoạt động như MVC (đặt trong `Pages/`). Với Razor Pages, `_ViewImports` thường có `@namespace WebRazor.Pages` và `@addTagHelper`.

> **Cẩn thận namespace:** namespace của PageModel mặc định theo thư mục (`WebRazor.Pages.SanPhams`). Đặt tên thư mục trùng tên kiểu (`SanPham`) sẽ gây nhập nhằng `WebRazor.Pages.SanPham` với entity `SanPham` — lý do mẫu dùng thư mục `SanPhams`.

## Luồng CRUD hoàn chỉnh

| Chức năng | Trang / handler | Kết quả |
|-----------|-----------------|---------|
| Danh sách + tìm + phân trang | `Index` · `OnGetAsync` | `GET /SanPhams?TuKhoa=laptop&Trang=2` |
| Xem chi tiết | `Details` · `OnGetAsync(id)` | `404` nếu không có |
| Thêm | `Create` · `OnGet`/`OnPost` | lỗi → hiện lại form; thành công → `302` tới `Details` |
| Sửa | `Edit` · `OnGet(id)`/`OnPost(id)` | nạp Input từ entity; POST cập nhật |
| Xoá | `Index` · `OnPostXoa(id)` | `302` về danh sách kèm thông báo |

Thử code mẫu: tạo sản phẩm sai → thấy lỗi từng ô bằng tiếng Việt; trùng mã → "Mã sản phẩm đã tồn tại"; hợp lệ → `302 Location: /SanPhams/Details/5` và thông báo "Đã thêm …" hiện đúng một lần (TempData).

## Razor Pages hay MVC?

- Không có "đúng/sai". Hai cái **cùng chạy trong một ứng dụng**.
- Razor Pages hợp khi bạn nghĩ theo **trang** (mỗi URL một giao diện + xử lý riêng): CRUD, form, báo cáo, trang đăng nhập.
- MVC hợp khi controller phục vụ **nhiều view** hoặc bạn cần tách bạch mạnh/tái sử dụng action; hoặc kết hợp trả HTML và JSON cùng controller.
- Kiến thức bên dưới (routing, DI, model binding, validation, tag helper, filter, xác thực) là **một**.

## Lỗi thường gặp

- Quên dòng `@page` → trang trả `404` (được coi là view thường).
- `[BindProperty]` không nhận giá trị ở `GET` — cần `SupportsGet = true`.
- Quên nạp lại `SelectList` khi trả `Page()` sau lỗi → dropdown trống hoặc `NullReferenceException`.
- Tên trường lỗi sai tiền tố: `ModelState.AddModelError("Ma", ...)` không hiện cạnh `Input.Ma` — phải `"Input.Ma"`.
- `asp-page` sai đường dẫn tương đối/tuyệt đối.
- Bind entity thay vì kiểu `Input` (over-posting).
- Thiếu `TempData` giữ qua redirect (thông báo mất) hoặc đọc `TempData` nhiều lần (chỉ đọc được một lần).
- Handler trả `Page()` sau `POST` thành công (F5 gửi lại form) — phải redirect.

## Bài tập

1. Thêm cột "Xoá mềm" (`DaXoa`) và query filter (Chương 13); trang Index chỉ hiện sản phẩm chưa xoá.
2. Thêm handler `OnPostTangTonAsync(int id)` để nút "+10 tồn" trên mỗi hàng (một câu `ExecuteUpdate`).
3. Thêm trang `Nhoms/Index` quản lý nhóm sản phẩm (CRUD) dùng lại mẫu của `SanPhams`.
4. Thêm sắp xếp qua `[BindProperty(SupportsGet = true)] string? SapXep` với whitelist (Chương 14).
5. So sánh: viết lại trang `Create` bằng MVC (Chương 15) và liệt kê số file/tầng khác nhau.
