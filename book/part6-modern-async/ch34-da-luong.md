# Chương 34 — Đa luồng và đồng bộ hoá

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **thread**, **thread pool** và khi nào cần chạy song song.
- Nhận ra và sửa **race condition** bằng `lock`, `Interlocked`, collection an toàn đa luồng.
- Dùng `Parallel`, `SemaphoreSlim` và `Channel<T>` (mô hình producer/consumer).
- Hiểu **deadlock** và cách tránh; phân biệt với `async/await`.

Code mẫu: [`code/ch34-da-luong/`](../../code/ch34-da-luong/).

## Thread và thread pool

Một **thread** là một luồng thực thi. Chương trình luôn có một thread chính; bạn có thể tạo thêm để làm nhiều việc *cùng lúc*
(trên nhiều lõi CPU):

```csharp
var t = new Thread(() => Console.WriteLine("chay tren thread phu"));
t.Start();
t.Join();     // chờ thread kết thúc
```

Tạo thread thủ công đắt (mỗi thread tốn ~1 MB ngăn xếp) và khó quản lý. Thực tế dùng **thread pool** — một nhóm thread tái
sử dụng do .NET quản lý — thông qua `Task.Run`, `Parallel`, `async/await`. Hiếm khi bạn cần `new Thread` (chỉ khi cần thread
chạy lâu, ưu tiên riêng).

**Chọn công cụ:**

| Bài toán | Dùng |
|----------|------|
| Chờ I/O (mạng, đĩa, CSDL) | `async/await` (Chương 33) |
| Tính toán nặng, chia đều trên nhiều lõi | `Parallel.For/ForEach`, PLINQ (`.AsParallel()`) |
| Việc nền một lần | `Task.Run` |
| Luồng dữ liệu producer → consumer | `Channel<T>` |

## Race condition — vấn đề gốc

Khi **nhiều thread cùng đọc/ghi một dữ liệu chung** mà không phối hợp, kết quả phụ thuộc vào thứ tự "may rủi" giữa các thread:

```csharp
int dem = 0;
Parallel.For(0, 100_000, _ => dem++);
Console.WriteLine(dem);   // mong 100000, nhưng thường nhỏ hơn (vd 63.421) — và mỗi lần chạy khác nhau
```

Vì `dem++` không "nguyên tử": nó gồm ba bước **đọc – cộng – ghi**. Hai thread có thể cùng đọc `5`, cùng ghi `6` → mất một lần đếm.
Lỗi này **không nhất quán**, khó tái hiện và hay biến mất khi debug — nên phải phòng ngừa chứ không thể "thử xem có đúng không".

## Giải pháp 1: `lock`

`lock` chỉ cho **một thread tại một thời điểm** vào đoạn code găng (critical section):

```csharp
object khoa = new();
Parallel.For(0, 100_000, _ => { lock (khoa) { dem++; } });   // luôn đúng: 100000
```

Quy tắc dùng `lock`:

- Khoá trên một **đối tượng `private readonly`** riêng (hoặc `System.Threading.Lock` trong .NET 9+); **không** khoá trên
  `this`, `typeof(...)`, hay chuỗi — code khác có thể khoá cùng đối tượng và gây deadlock.
- Giữ khoá **càng ngắn càng tốt**; không gọi code lạ, không `await` bên trong `lock`
  (dùng `SemaphoreSlim` cho tình huống async).

## Giải pháp 2: `Interlocked`

Phép nguyên tử, không cần khoá, nhanh hơn cho thao tác đơn giản trên biến số:

```csharp
Parallel.For(0, 100_000, _ => Interlocked.Increment(ref dem));
Interlocked.Add(ref tong, 5);
Interlocked.CompareExchange(ref x, giaTriMoi, giaTriMongDoi);
```

## Giải pháp 3: collection an toàn đa luồng

`List<T>`, `Dictionary<K,V>` **không** an toàn khi nhiều thread cùng ghi. Dùng `System.Collections.Concurrent`:

```csharp
var dem = new ConcurrentDictionary<string, int>();
Parallel.ForEach(tu, t => dem.AddOrUpdate(t, 1, (_, cu) => cu + 1));
```

Có sẵn `ConcurrentQueue<T>`, `ConcurrentBag<T>`, `ConcurrentStack<T>`, `BlockingCollection<T>`. Lưu ý: từng thao tác an toàn,
nhưng **chuỗi hai thao tác** (kiểm tra rồi thêm) vẫn có thể bị xen — dùng phương thức gộp như `GetOrAdd`, `AddOrUpdate`.

Cách tốt nhất tránh race: **không chia sẻ trạng thái có thể thay đổi**. Dùng dữ liệu bất biến (`record`, Chương 20) và mỗi
thread giữ dữ liệu riêng rồi gộp kết quả cuối cùng.

## `Parallel` và PLINQ

```csharp
// Mỗi thread cộng dồn vào biến cục bộ, gộp một lần cuối => ít tranh chấp
long tongChan = 0;
Parallel.For(1, 1_000_001, () => 0L,
    (i, _, cucBo) => i % 2 == 0 ? cucBo + i : cucBo,
    cucBo => Interlocked.Add(ref tongChan, cucBo));

var kq = so.AsParallel().Where(LaNguyenTo).ToList();   // PLINQ
await Parallel.ForEachAsync(urls, opts, async (url, ct) => { ... });   // song song + async
```

Song song hoá chỉ đáng khi **mỗi đơn vị công việc đủ nặng** và độc lập; với việc nhỏ, chi phí phối hợp lớn hơn lợi ích
(thậm chí chậm hơn). Luôn **đo** trước và sau.

## `SemaphoreSlim` — giới hạn số việc đồng thời

Dùng khi muốn chỉ cho phép tối đa *N* việc chạy cùng lúc (giới hạn kết nối, gọi API có rate limit). Hỗ trợ `await`:

```csharp
var cho = new SemaphoreSlim(2);
await cho.WaitAsync();
try { await LamViecAsync(); }
finally { cho.Release(); }    // LUÔN Release trong finally
```

## `Channel<T>` — producer / consumer

Một bên **sản xuất** dữ liệu, bên kia **tiêu thụ**, ngăn cách bởi kênh có bộ đệm, an toàn đa luồng và hỗ trợ async:

```csharp
var kenh = Channel.CreateBounded<int>(3);    // đệm tối đa 3 phần tử: "áp suất ngược" (backpressure)

var sanXuat = Task.Run(async () =>
{
    for (int i = 1; i <= 5; i++) await kenh.Writer.WriteAsync(i);   // đầy thì chờ
    kenh.Writer.Complete();                                          // báo hết dữ liệu
});
var tieuThu = Task.Run(async () =>
{
    await foreach (var x in kenh.Reader.ReadAllAsync()) Console.WriteLine(x);
});
await Task.WhenAll(sanXuat, tieuThu);
```

Đây là mô hình nền cho hàng đợi công việc, xử lý sự kiện, background service trong ASP.NET Core.

## Deadlock

**Deadlock** = hai (hay nhiều) thread cùng chờ nhau mãi, không ai chạy được:

```csharp
// Thread 1: lock(A) { lock(B) { ... } }
// Thread 2: lock(B) { lock(A) { ... } }   // → mỗi bên giữ một khoá và chờ khoá kia
```

Cách tránh: (1) luôn khoá theo **cùng một thứ tự**; (2) giữ khoá ngắn, không gọi code ngoài khi đang giữ; (3) dùng
`Monitor.TryEnter` với timeout; (4) thiết kế ít khoá bằng cách tránh chia sẻ trạng thái. Deadlock kinh điển thứ hai:
`.Result`/`.Wait()` trên Task trong ngữ cảnh đồng bộ (Chương 33).

## Ghi nhớ

- Chạy song song là **tối ưu hiệu năng có giá**: thêm độ phức tạp và lỗi khó bắt. Chỉ dùng khi đã đo và thật sự cần.
- **`async` cho I/O, `Parallel` cho CPU.**
- Thứ tự ưu tiên khi phải chia sẻ dữ liệu: bất biến → không chia sẻ → collection concurrent → `Interlocked` → `lock`.
- Kiểm thử đa luồng rất khó; thiết kế để đoạn dùng chung nhỏ và dễ kiểm chứng bằng mắt.

## Lỗi thường gặp

- Nhiều thread ghi `List<T>`/`Dictionary` không khoá → dữ liệu hỏng, `IndexOutOfRange`, vòng lặp vô hạn.
- `lock(this)`/`lock("chuoi")`.
- `await` bên trong `lock` (lỗi biên dịch) — dùng `SemaphoreSlim`.
- Quên `Release()` semaphore trong `finally` → treo vĩnh viễn.
- Tưởng `volatile`/`static` là đủ an toàn.
- Dùng `Parallel.For` cho việc quá nhỏ — chậm hơn vòng `for`.
- Không gọi `Writer.Complete()` khiến consumer đợi mãi.

## Bài tập

1. Chạy đoạn race condition 10 lần và ghi lại kết quả; sửa bằng `lock`, rồi bằng `Interlocked`; so sánh tốc độ.
2. Dùng `Parallel.ForEach` + `ConcurrentDictionary` đếm tần suất từ trong nhiều chuỗi.
3. Viết chương trình hai thread gây **deadlock** có chủ đích, rồi sửa bằng thứ tự khoá cố định.
4. Dùng `Channel` mô phỏng nhà bếp: 1 đầu bếp (producer) và 3 người phục vụ (consumer) xử lý đơn.
5. Đo thời gian đếm số nguyên tố ≤ 5.000.000 bằng vòng `for`, `Parallel.For`, và PLINQ.
