# Chương 33 — Lập trình bất đồng bộ `async`/`await`

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu vì sao cần **bất đồng bộ** và nó khác **đa luồng** thế nào.
- Viết và gọi phương thức `async Task` / `Task<T>` đúng cách.
- Chạy nhiều việc **song song** với `Task.WhenAll`/`WhenAny`, xử lý lỗi, **huỷ** bằng `CancellationToken`.
- Dùng `IAsyncEnumerable<T>` và biết cách tránh các lỗi kinh điển (`async void`, `.Result`, deadlock).

Code mẫu: [`code/ch33-async-await/`](../../code/ch33-async-await/).

## Vấn đề: chờ đợi mà không lãng phí

Nhiều thao tác phần lớn thời gian là **chờ**: đọc file, gọi API, truy vấn CSDL. Nếu thread đứng chờ (**block**), nó không làm
được việc gì khác:

- Ứng dụng giao diện: màn hình **đơ**.
- Web server: mỗi request chiếm một thread trong lúc chờ → hết thread, không phục vụ thêm được ai.

**Bất đồng bộ (asynchronous)** cho phép thread **rảnh tay** trong lúc chờ, và tiếp tục xử lý khi kết quả sẵn sàng.
Trong ASP.NET Core (Tập 2), `async` là mặc định cho mọi thao tác I/O, nên chương này rất quan trọng.

> **Async ≠ đa luồng.** Với I/O (chờ mạng, đĩa), **không có thread nào bị chiếm** trong lúc chờ. Đa luồng (Chương 34) là
> chạy *tính toán* song song trên nhiều thread/core. Hai công cụ cho hai bài toán khác nhau.

## `Task`, `async` và `await`

- **`Task`** đại diện một công việc sẽ hoàn thành trong tương lai (không kết quả); **`Task<T>`** có kết quả kiểu `T`.
- **`async`** đánh dấu phương thức được phép dùng `await`.
- **`await`** "chờ mà không chặn": nếu Task chưa xong, phương thức **trả quyền điều khiển** về nơi gọi; khi xong, phần còn
  lại của phương thức **chạy tiếp** từ chỗ dừng.

```csharp
static async Task<string> TaiDuLieuAsync(string ten, int ms)
{
    await Task.Delay(ms);     // giả lập chờ I/O; KHÔNG block thread
    return ten;
}

string kq = await TaiDuLieuAsync("A", 300);
```

Quy ước: tên phương thức bất đồng bộ kết thúc bằng **`Async`**; kiểu trả về là `Task`, `Task<T>` (hoặc `ValueTask<T>`).
`Main`/top-level statements có thể dùng `await` trực tiếp.

Bên trong, trình biên dịch biến phương thức `async` thành một **máy trạng thái**; bạn viết code tuần tự dễ đọc mà không
phải lồng callback.

## Chạy tuần tự hay song song?

```csharp
// Tuần tự: ~600 ms — đợi A xong mới bắt đầu B
await TaiDuLieuAsync("A", 300);
await TaiDuLieuAsync("B", 300);

// Song song: ~300 ms — bắt đầu cả hai TRƯỚC, rồi mới đợi
var tA = TaiDuLieuAsync("A", 300);
var tB = TaiDuLieuAsync("B", 300);
await Task.WhenAll(tA, tB);
```

Điểm mấu chốt: **gọi phương thức async bắt đầu Task ngay**; `await` mới là lúc chờ. Muốn song song, đừng `await`
ngay từng cái. `await Task.WhenAll(...)` chờ tất cả (và nếu Task trả `T`, kết quả là mảng `T[]`);
`await Task.WhenAny(...)` chờ **cái đầu tiên** xong (dùng cho timeout, "lấy nguồn nhanh nhất").

Với danh sách: `var kq = await Task.WhenAll(ds.Select(x => XuLyAsync(x)));`. Nhớ giới hạn số lượng đồng thời
(`SemaphoreSlim`, `Parallel.ForEachAsync`) nếu danh sách lớn.

## Xử lý lỗi

Ngoại lệ trong phương thức `async` được **lưu vào Task** và ném lại khi bạn `await`:

```csharp
try { await LoiAsync(); }
catch (InvalidOperationException e) { ... }   // bắt bình thường như code đồng bộ
```

Với `Task.WhenAll`, `await` ném ngoại lệ **đầu tiên**; các lỗi còn lại nằm trong `task.Exception` (`AggregateException`).
Nếu không bao giờ `await` một Task lỗi, lỗi bị "nuốt" lặng lẽ (fire-and-forget) — luôn `await` hoặc xử lý kết quả.

## Huỷ việc: `CancellationToken`

Việc chạy lâu phải cho phép **huỷ** (người dùng bấm Cancel, request bị ngắt, hết timeout):

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));   // tự huỷ sau 250 ms
try { await ViecDaiAsync(cts.Token); }
catch (OperationCanceledException) { Console.WriteLine("bi huy"); }

static async Task ViecDaiAsync(CancellationToken ct)
{
    for (int i = 0; i < 10; i++)
    {
        ct.ThrowIfCancellationRequested();     // tự kiểm tra
        await Task.Delay(100, ct);             // hầu hết API async nhận token
    }
}
```

Quy ước: phương thức async có thể mất thời gian nên nhận `CancellationToken ct = default` là **tham số cuối** và truyền nó
xuống mọi lời gọi async bên trong. Trong ASP.NET Core, framework cung cấp sẵn token gắn với request.

## `IAsyncEnumerable<T>` — luồng bất đồng bộ

Sinh dãy giá trị mà mỗi phần tử cần chờ:

```csharp
static async IAsyncEnumerable<int> SinhSoAsync(int n)
{
    for (int i = 1; i <= n; i++)
    {
        await Task.Delay(50);
        yield return i;
    }
}

await foreach (var so in SinhSoAsync(3)) Console.WriteLine(so);
```

Hợp cho đọc dữ liệu từng phần (dòng file lớn, kết quả CSDL, dữ liệu phân trang).
`await using` giải phóng tài nguyên `IAsyncDisposable` (Chương 21).

## `Task.Run` — việc CPU nặng

`await` giúp với I/O. Với **tính toán nặng** trên luồng giao diện, dùng `Task.Run` để đẩy sang thread pool:

```csharp
long tong = await Task.Run(() => TinhToanNang());
```

Trong ASP.NET Core **không** bọc code đồng bộ bằng `Task.Run` chỉ để "trông có vẻ async" — nó chỉ chiếm thread khác thay vì
thread hiện tại, không đem lại lợi ích.

## Quy tắc vàng

1. **"Async all the way"**: đã async thì async xuyên suốt, từ trên xuống dưới; đừng trộn.
2. **Không chặn** Task bằng `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` — có thể gây **deadlock** (nhất là ở UI/
   ngữ cảnh cũ) và làm mất lợi ích. Hãy `await`.
3. **Tránh `async void`** — chỉ dành cho event handler; lỗi trong `async void` không bắt được và làm sập tiến trình.
   Dùng `async Task`.
4. Phương thức chỉ chuyển tiếp Task mà không cần `await` có thể trả thẳng Task (`return repo.GetAsync(id);`) — nhưng
   nếu có `using`/`try/catch` bao quanh thì phải `await`.
5. Trong thư viện dùng chung, cân nhắc `ConfigureAwait(false)`; trong ASP.NET Core không cần (không có
   SynchronizationContext).
6. **Luôn truyền `CancellationToken`.**

## Lỗi thường gặp

- Quên `await` → Task chạy nhưng code phía sau chạy tiếp mà không chờ; lỗi bị bỏ sót (cảnh báo `CS4014`).
- `.Result`/`.Wait()` → deadlock hoặc chặn thread.
- `async void` ngoài event handler.
- Hai `await` liên tiếp khi lẽ ra chạy song song (mất hiệu năng).
- Bắt `Exception` chung chung nuốt cả `OperationCanceledException` — bắt riêng và bỏ qua khi huỷ hợp lệ.
- Lạm dụng `Task.Run` trong web.
- Quên `Dispose` `CancellationTokenSource` (`using var cts`).

## Bài tập

1. Viết `Task<string> DocFileAsync(string path)` dùng `File.ReadAllTextAsync`; gọi 3 file **song song**.
2. Đo thời gian của 5 lời gọi `Task.Delay(200)` chạy tuần tự vs `Task.WhenAll`.
3. Thêm timeout 1 giây cho một việc chạy lâu bằng `CancellationTokenSource` hoặc `Task.WaitAsync(TimeSpan)`.
4. Dùng `Task.WhenAny` để "chạy đua" hai nguồn dữ liệu, lấy kết quả của nguồn nhanh hơn và huỷ nguồn còn lại.
5. Cố tình gọi `.Result` trên một Task trong ứng dụng console và giải thích vì sao ở đó không deadlock nhưng vẫn là thói quen xấu.
