# Chương 18 — Blazor

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **Blazor** là gì và các **chế độ render** (Static SSR, Interactive Server, WebAssembly, Auto).
- Viết **component Razor**: `@code`, sự kiện `@onclick`, **data binding** `@bind`, tham số `[Parameter]`, `EventCallback`.
- Dùng **vòng đời** component, `@inject` dịch vụ, tải dữ liệu **bất đồng bộ**.
- Xây form với `EditForm` + validation (DataAnnotations), component con và danh sách với `@key`.

Code mẫu: [`code/ch18-blazor/`](../../code/ch18-blazor/) — bộ đếm (sự kiện, binding, vòng đời) và trang quản lý sản phẩm.

## Blazor là gì?

Với MVC/Razor Pages, mỗi thao tác (bấm nút, lọc) thường là **một request** tải lại trang; muốn giao diện mượt phải viết **JavaScript**. **Blazor** cho phép viết giao diện **tương tác** bằng **C# + Razor**: cùng ngôn ngữ, cùng kiểu dữ liệu, cùng thư viện (validation, DTO, dịch vụ) cho cả server và giao diện — không cần framework JavaScript riêng cho hầu hết nhu cầu.

Đơn vị xây dựng là **component** — một file `.razor` gồm HTML/Razor và khối `@code` (C#). Component có thể lồng nhau, nhận tham số, phát sự kiện, dùng lại. Cả trang cũng là component (có `@page`).

## Chế độ render

Blazor Web App (từ .NET 8) cho **chọn từng trang/component** chạy theo cách nào:

| Chế độ | Chạy ở đâu | Tương tác? | Đặc điểm |
|--------|-----------|-----------|----------|
| **Static SSR** (mặc định) | server, sinh HTML một lần | Không (như Razor Pages) | nhanh, nhẹ, SEO tốt; form gửi bằng POST |
| **Interactive Server** | **server**; sự kiện qua kết nối **SignalR (WebSocket)** | Có | khởi động nhanh, code chạy trên server (truy cập CSDL trực tiếp); cần kết nối liên tục, mỗi người dùng tốn bộ nhớ server |
| **Interactive WebAssembly** | **trình duyệt** (.NET chạy bằng WebAssembly) | Có | chạy offline, giảm tải server; tải lần đầu nặng hơn; gọi server qua API |
| **Interactive Auto** | server lúc đầu, chuyển sang WebAssembly khi đã tải xong | Có | cân bằng cả hai |

Chọn bằng attribute: `@rendermode InteractiveServer` (trên component/trang) hoặc `AddInteractiveServerRenderMode()` khi map. Sách dùng **Interactive Server** vì đơn giản nhất (không cần dự án API riêng, truy cập dịch vụ thẳng).

```csharp
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
...
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

Cấu trúc: `App.razor` (khung HTML, nạp `blazor.web.js`) → `Routes.razor` (bộ định tuyến) → `MainLayout.razor` (bố cục) → các trang trong `Components/Pages`. Trang tương tác được **prerender** (HTML dựng sẵn từ server, hiện ngay) rồi "gắn" kết nối để trở nên tương tác.

> Kết quả prerender kiểm chứng bằng `curl`: `GET /dem` trả HTML đã có `<h1>Bộ đếm</h1>` và `Giá trị hiện tại: 0`. Tính tương tác cần trình duyệt chạy `blazor.web.js`.

## Component đầu tiên: bộ đếm

```razor
@page "/dem"
@rendermode InteractiveServer

<p>Giá trị hiện tại: <strong>@soDem</strong></p>

<button class="btn" @onclick="Tang">Tăng</button>
<button class="btn" @onclick="() => soDem -= buoc">Giảm</button>
<button class="btn" @onclick="Dat0" disabled="@(soDem == 0)">Về 0</button>

@code {
    private int soDem;
    private int buoc = 1;

    private void Tang() => soDem += buoc;
    private void Dat0() => soDem = 0;
}
```

- `@page "/dem"` biến component thành trang có URL.
- `@soDem` in giá trị biến/biểu thức; `@code { ... }` chứa trường, thuộc tính, phương thức C#.
- **`@onclick="Tang"`** gắn xử lý sự kiện (phương thức hoặc lambda). Sau khi xử lý, Blazor **tự render lại** component và chỉ gửi **phần khác biệt** của giao diện (diff) tới trình duyệt.
- Thuộc tính HTML điều kiện: `disabled="@(soDem == 0)"` — `true` thì bật, `false` thì bỏ thuộc tính.
- Điều kiện/lặp trong Razor: `@if (...) { }`, `@foreach (...) { }`, `@switch`.

Nếu thay đổi dữ liệu **ngoài** luồng sự kiện của Blazor (timer, callback từ dịch vụ), gọi `StateHasChanged()` (và `await InvokeAsync(StateHasChanged)` từ luồng khác) để yêu cầu vẽ lại.

## Data binding: `@bind`

Liên kết **hai chiều** giữa ô nhập và biến:

```razor
<input type="number" @bind="buoc" min="1" max="100" />                          @* cập nhật khi rời ô (onchange) *@
<input @bind="chu" @bind:event="oninput" />                                     @* cập nhật NGAY khi gõ *@
<p>Bạn đã gõ <strong>@chu.Length</strong> ký tự: <em>@chu.ToUpperInvariant()</em></p>

<input @bind="tuKhoa" @bind:event="oninput" @bind:after="TaiAsync" />           @* chạy TaiAsync SAU khi giá trị đã gán *@
```

Kiểu dữ liệu tự chuyển đổi (`int`, `decimal`, `DateTime`, `bool`...). `@bind:after` (mới) gọn hơn viết `@bind-Value:set`; dùng cho "gõ đến đâu tìm đến đó". Với tìm kiếm gọi CSDL nên thêm **debounce** (đợi người dùng ngừng gõ ~300 ms) để tránh gọi liên tục.

## Vòng đời component

Chạy theo thứ tự (chỉ những hàm bạn `override`):

| Phương thức | Khi nào | Dùng để |
|-------------|---------|---------|
| `SetParametersAsync` | nhận tham số | hiếm khi ghi đè |
| **`OnInitialized[Async]`** | **một lần** lúc tạo | khởi tạo, **tải dữ liệu ban đầu** |
| **`OnParametersSet[Async]`** | mỗi khi tham số từ cha đổi | phản ứng theo tham số mới |
| `OnAfterRender[Async](bool firstRender)` | sau mỗi lần vẽ | gọi JavaScript, thao tác DOM (chỉ ở đây DOM đã tồn tại) |
| `Dispose` / `IAsyncDisposable` | khi bị gỡ | huỷ đăng ký sự kiện, timer |

```csharp
protected override async Task OnInitializedAsync() => await TaiAsync();     // tải dữ liệu bất đồng bộ
```

Lưu ý với **prerender**: `OnInitialized` chạy **hai lần** (một lần lúc prerender trên server, một lần khi kết nối tương tác) — tránh tác dụng phụ không lặp được (gửi email, trừ kho) trong đó.

## Tải dữ liệu bất đồng bộ và trạng thái "đang tải"

```razor
@inject SanPhamService DichVu

@if (dangTai) { <p><em>Đang tải…</em></p> }
else if (danhSach.Count == 0) { <p><em>Không có sản phẩm nào.</em></p> }
else { ...bảng... }

@code {
    private bool dangTai = true;
    private List<SanPham> danhSach = [];

    private async Task TaiAsync()
    {
        dangTai = true;
        danhSach = await DichVu.TimAsync(tuKhoa);       // trong lúc chờ, giao diện hiện "Đang tải…"
        dangTai = false;
    }
}
```

**`@inject Kiểu Tên`** lấy dịch vụ từ DI (Chương 5) — giống tiêm constructor. Dịch vụ Scoped trong Blazor Server có vòng đời là **một mạch kết nối (circuit)** = một tab trình duyệt, không phải một HTTP request. `DbContext` nên lấy qua `IDbContextFactory<T>` (tạo ngắn hạn cho mỗi thao tác) thay vì Scoped dài hạn.

## Component con: `[Parameter]` và `EventCallback`

Dữ liệu chảy **xuống** qua tham số, sự kiện chảy **lên** qua callback:

```razor
@* SanPhamRow.razor *@
<tr>
    <td>@SanPham.Ma</td><td>@SanPham.Ten</td>
    <td><button class="btn nguy-hiem" @onclick="() => OnXoa.InvokeAsync(SanPham.Id)">Xoá</button></td>
</tr>

@code {
    [Parameter, EditorRequired] public SanPham SanPham { get; set; } = default!;
    [Parameter] public EventCallback<int> OnXoa { get; set; }
}
```
```razor
@* trang cha *@
@foreach (var sp in danhSach)
{
    <SanPhamRow @key="sp.Id" SanPham="sp" OnXoa="XoaAsync" />
}
```

- `[Parameter]` phải là property `public`; **đừng gán** vào tham số bên trong component con (cha sẽ ghi đè khi vẽ lại).
- **`EventCallback<T>`** là kiểu callback chuẩn: tự vẽ lại component cha sau khi gọi (khác `Action<T>`).
- **`@key`** giúp Blazor nhận đúng dòng nào là dòng nào khi danh sách đổi thứ tự/xoá — tránh giữ nhầm trạng thái của dòng khác.
- Ngoài ra: `RenderFragment` (`ChildContent`) để nhận nội dung bên trong thẻ; `CascadingParameter` truyền dữ liệu xuống mọi con cháu (theme, người dùng); component **generic** (`@typeparam T`).

## Form và validation: `EditForm`

```razor
<EditForm Model="form" OnValidSubmit="ThemAsync" FormName="them-san-pham">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <label>Mã <InputText @bind-Value="form.Ma" /></label>
    <ValidationMessage For="() => form.Ma" />

    <label>Giá <InputNumber @bind-Value="form.Gia" /></label>
    <ValidationMessage For="() => form.Gia" />

    <button type="submit">Thêm</button>
</EditForm>
```

- `Model` là lớp có **DataAnnotations** — **chính các attribute** đã dùng ở Chương 8/17 (`[Required]`, `[Range]`, `[RegularExpression]`). Cùng một lớp validation dùng lại cho API, Razor Pages, Blazor.
- `<DataAnnotationsValidator />` chạy validation; `OnValidSubmit` chỉ gọi khi hợp lệ, `OnInvalidSubmit` khi không.
- Component `Input*`: `InputText`, `InputNumber`, `InputDate`, `InputCheckbox`, `InputSelect`, `InputTextArea`, `InputFile` (tải file). Chúng tự thêm CSS `valid`/`invalid`.
- Lỗi nghiệp vụ từ dịch vụ (mã trùng): bắt exception, hiện cạnh form (như `loi` trong mẫu) hoặc dùng `ValidationMessageStore` để gắn vào một trường.
- Với Static SSR, form cần `FormName` và `[SupplyParameterFromForm]`; với chế độ tương tác thì không.

## Điều hướng và tham số route

```razor
@page "/san-pham/{Id:int}"
@inject NavigationManager Nav

@code {
    [Parameter] public int Id { get; set; }
    void VeDanhSach() => Nav.NavigateTo("/san-pham");
}
```

Dùng `<NavLink href="...">` cho menu (tự thêm class `active`), `Nav.NavigateTo(...)` để chuyển trang bằng code, `[SupplyParameterFromQuery]` để lấy tham số query.

## Gọi JavaScript khi cần (JS interop)

Blazor không loại bỏ JavaScript hoàn toàn: cần API trình duyệt (clipboard, localStorage, thư viện biểu đồ) thì dùng `IJSRuntime`:

```csharp
@inject IJSRuntime JS
await JS.InvokeVoidAsync("localStorage.setItem", "khoa", "gia_tri");
var w = await JS.InvokeAsync<int>("eval", "window.innerWidth");     // ví dụ minh hoạ (tránh eval thật)
```

Chỉ gọi JS sau khi component đã vẽ (`OnAfterRenderAsync`). Với Blazor Server, mỗi lệnh gọi là một vòng qua mạng.

## Blazor Server hay WebAssembly? Blazor hay React/Angular?

- **Server**: ứng dụng nội bộ/quản trị, cần truy cập dữ liệu thẳng, muốn khởi động nhanh, ít cần scale hàng chục nghìn kết nối đồng thời (mỗi kết nối giữ trạng thái trên server).
- **WebAssembly**: ứng dụng cần chạy offline, giảm tải server, đã có API; chấp nhận bản tải xuống lớn hơn ban đầu. Code chạy **trên máy người dùng** — không được nhét bí mật vào đó.
- **Auto**: kết hợp.
- So với SPA JavaScript (React/Angular/Vue): Blazor hợp khi đội mạnh C#, muốn chia sẻ code, không muốn duy trì hai hệ sinh thái; hệ sinh thái thư viện giao diện của JS lớn hơn nhiều, vì vậy nhiều đội vẫn chọn JS cho giao diện phức tạp, dùng ASP.NET Core làm API (Phần 2–3).

## Lỗi thường gặp

- Component không tương tác: quên `@rendermode` (mặc định Static SSR) hoặc quên `AddInteractiveServerComponents/RenderMode`.
- Giao diện không cập nhật vì đổi dữ liệu ngoài luồng sự kiện mà không gọi `StateHasChanged`/`InvokeAsync`.
- Gọi JS trong `OnInitialized` (chưa có DOM) — dùng `OnAfterRenderAsync`.
- Tác dụng phụ trong `OnInitialized` bị chạy 2 lần do prerender.
- Gán vào `[Parameter]` bên trong component con.
- Quên `@key` cho danh sách động → trạng thái nhập lẫn dòng.
- Giữ dữ liệu người dùng trong dịch vụ **Singleton** (lẫn giữa mọi người dùng) — dùng Scoped (theo circuit).
- `DbContext` Scoped sống suốt circuit → lỗi đồng thời/dữ liệu cũ; dùng `IDbContextFactory`.
- Mất kết nối: người dùng thấy giao diện "đơ" — cấu hình UI kết nối lại và chấp nhận nó là đặc tính của Blazor Server.
- Bỏ qua bảo mật: code chạy trên server vẫn phải kiểm tra quyền cho mọi thao tác (Chương 20).

## Bài tập

1. Thêm nút "Sao chép mã" dùng JS interop (`navigator.clipboard.writeText`) trong `SanPhamRow`.
2. Thêm debounce 300 ms cho ô tìm kiếm bằng `System.Timers.Timer` hoặc `CancellationTokenSource`.
3. Thêm trang `/san-pham/{id:int}` hiển thị chi tiết và nút quay lại bằng `NavigationManager`.
4. Viết component `TheThongBao` nhận `ChildContent` và tham số `Loai` ("thanh-cong"/"loi") để hiển thị thông báo.
5. Thay `SanPhamService` bằng dịch vụ dùng EF Core + `IDbContextFactory<T>`; giải thích vì sao không dùng `DbContext` Scoped ở đây.
