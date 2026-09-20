# Chương 31 — Delegate, event và lambda

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **delegate** là "kiểu của phương thức" và dùng `Func`, `Action`, `Predicate` có sẵn.
- Viết **lambda** và hiểu **closure**.
- Dùng **event** theo mẫu publisher/subscriber (`EventHandler<T>`).
- Truyền hành vi như một tham số — nền tảng của LINQ, async và ASP.NET Core.

Code mẫu: [`code/ch31-delegate-event-lambda/`](../../code/ch31-delegate-event-lambda/).

## Delegate — biến chứa phương thức

Với `int x = 5;` bạn lưu một giá trị. **Delegate** cho phép lưu **một phương thức** vào biến, truyền đi và gọi sau:

```csharp
delegate int PhepTinh(int a, int b);        // định nghĩa "hình dạng" phương thức

PhepTinh cong = (a, b) => a + b;
Console.WriteLine(cong(3, 4));              // 7

static int ApDung(int a, int b, PhepTinh f) => f(a, b);
ApDung(10, 5, (x, y) => x - y);             // 5 — truyền hành vi như tham số
```

Delegate là kiểu **an toàn**: chỉ nhận phương thức khớp chữ ký (số tham số, kiểu, kiểu trả về). Nó cho phép **tách "làm gì"
khỏi "khi nào/ở đâu làm"** — ví dụ `Sort` không biết bạn so sánh theo tiêu chí nào, bạn truyền cho nó.

## `Func`, `Action`, `Predicate` — delegate dựng sẵn

Hiếm khi cần tự khai báo `delegate`, vì .NET có sẵn dạng generic:

| Kiểu | Chữ ký | Ví dụ |
|------|--------|-------|
| `Action` / `Action<T1,...>` | không trả về (`void`) | `Action<string> inRa = s => Console.WriteLine(s);` |
| `Func<T1,...,TResult>` | trả về `TResult` (tham số **cuối** là kiểu trả về) | `Func<int,int,int> cong = (a, b) => a + b;` |
| `Predicate<T>` | nhận `T` trả `bool` | `Predicate<int> laChan = x => x % 2 == 0;` |

```csharp
Func<int, int> binh = x => x * x;
Func<int, int, string> ghep = (a, b) => $"{a}-{b}";
```

## Lambda expression

Lambda là cách viết **hàm vô danh** ngắn gọn: `(tham số) => biểu_thức` hoặc `(tham số) => { khối lệnh }`.

```csharp
x => x * x                       // một tham số, không cần ngoặc
(a, b) => a + b                  // nhiều tham số
() => Console.WriteLine("hi")    // không tham số
(int a, int b) => { var t = a + b; return t * 2; }   // khối lệnh
static x => x * 2                // static lambda: không cho phép capture, an toàn & nhanh hơn
```

Kiểu tham số thường được suy luận từ delegate đích. **Method group** cho phép truyền thẳng tên phương thức:
`so.Select(binh)`, `list.ForEach(Console.WriteLine)`.

### Truyền hành vi vào hàm

```csharp
static IEnumerable<int> Loc(IEnumerable<int> ds, Predicate<int> dieuKien)
{
    foreach (var x in ds)
        if (dieuKien(x)) yield return x;
}

Loc(so, x => x % 2 == 0);   // lọc chẵn
Loc(so, x => x > 3);        // lọc > 3
```

Đây chính xác là cách `Where` của LINQ được viết (Chương 30).

## Closure — lambda "nhớ" biến bên ngoài

```csharp
int heSo = 3;
Func<int, int> nhanHeSo = x => x * heSo;
nhanHeSo(5);    // 15
heSo = 10;
nhanHeSo(5);    // 50 — closure giữ THAM CHIẾU tới biến, không sao chép giá trị lúc tạo
```

Lambda **bắt (capture)** biến của phạm vi bao quanh và đọc/ghi giá trị *hiện tại lúc gọi*. Hàm trả về hàm:

```csharp
Func<int, int> TaoBoCong(int n) => x => x + n;
var cong5 = TaoBoCong(5);
cong5(1);   // 6 — n = 5 vẫn "sống" sau khi TaoBoCong kết thúc
```

Cẩn thận: bắt biến **vòng lặp** (`for`) trong lambda chạy muộn có thể cho kết quả bất ngờ (mọi lambda cùng thấy giá trị
cuối); `foreach` an toàn hơn vì mỗi vòng có biến riêng. Và closure giữ đối tượng "sống" lâu hơn dự kiến (rò rỉ bộ nhớ nếu
lưu lambda mãi).

## Multicast delegate

Một delegate có thể chứa **nhiều** phương thức; gọi một lần chạy tất cả theo thứ tự đăng ký:

```csharp
Action thongBao = () => Console.WriteLine("ghi log");
thongBao += () => Console.WriteLine("gui email");
thongBao();   // ghi log, gui email
```

`+=` thêm, `-=` bỏ. Với delegate có kiểu trả về, chỉ giá trị của phương thức **cuối** được trả về. Đây là nền của **event**.

## Event — mẫu Publisher / Subscriber

**Event** là delegate được "bọc" để: bên ngoài chỉ được **đăng ký (`+=`) / huỷ (`-=`)**; chỉ bản thân class phát mới
được **kích hoạt** (`Invoke`). Nhờ đó bên phát không phụ thuộc vào bên nghe (giảm ràng buộc — Observer pattern, Chương 41).

```csharp
class DoUongEventArgs(string ten) : EventArgs
{
    public string TenDoUong { get; } = ten;
}

class MayPha
{
    public event EventHandler<DoUongEventArgs>? SanSang;   // publisher

    public void Pha(string ten)
    {
        Console.WriteLine($"Dang pha {ten}...");
        SanSang?.Invoke(this, new DoUongEventArgs(ten));   // ?. : an toàn khi chưa ai đăng ký
    }
}

var may = new MayPha();
may.SanSang += (sender, e) => Console.WriteLine($"[Khach A] nhan: {e.TenDoUong}");   // subscriber
may.SanSang += DocThongBao;
may.Pha("ca phe sua");
may.SanSang -= DocThongBao;      // huỷ đăng ký
```

Quy ước chuẩn: `EventHandler<TEventArgs>` (`object? sender, TEventArgs e`); tên sự kiện là động từ (`SanSang`, `Clicked`);
phương thức kích hoạt `OnXxx`.

Điểm cần nhớ: **hãy huỷ đăng ký (`-=`)** khi subscriber không còn dùng — nếu không publisher vẫn giữ tham chiếu, ngăn GC
dọn subscriber ("memory leak do event" rất phổ biến). Trong ASP.NET Core bạn ít tự viết event, nhưng sẽ gặp mẫu tương tự
trong thông báo, `IObservable`, MediatR (Tập 3).

## Khi nào dùng cái nào?

| Tình huống | Chọn |
|-----------|------|
| Truyền một hành vi vào hàm (lọc, sắp xếp, callback) | `Func`/`Action`/`Predicate` + lambda |
| Nhiều bên cần được báo khi có chuyện xảy ra | `event` |
| Hợp đồng nhiều phương thức có liên quan | `interface` (Chương 18) |
| Một hành vi đơn lẻ, cần tên rõ nghĩa | delegate đặt tên (`PhepTinh`) hoặc interface nhỏ |

## Lỗi thường gặp

- `NullReferenceException` khi gọi `SanSang(this, e)` mà chưa ai đăng ký — dùng `SanSang?.Invoke(...)`.
- Quên huỷ đăng ký event → rò rỉ bộ nhớ, handler chạy cả khi đối tượng đã "chết" logic.
- Mong đợi closure sao chép giá trị lúc tạo (thực ra đọc lúc gọi).
- Nhầm `Func<int, string>` (nhận int, trả string) với `Func<string, int>`.
- Lambda quá dài, phức tạp — tách thành phương thức có tên.
- Dùng `async void` cho event handler mà không bắt lỗi (Chương 33).

## Bài tập

1. Viết `ApDung<T>(IEnumerable<T> ds, Action<T> hanhDong)` tự cài đặt `ForEach`.
2. Viết `Func<double, double> TaoHamNhan(double heSo)` trả về hàm nhân với hệ số; kiểm chứng closure.
3. Tạo class `NutBam` có event `DuocBam` và hai subscriber; huỷ một subscriber rồi bấm lại.
4. Cài `SapXep<T>(List<T> ds, Func<T, T, int> soSanh)` bằng thuật toán sắp xếp chèn đơn giản.
5. Giải thích vì sao đoạn sau in `3 3 3` rồi sửa cho in `0 1 2`: `var fs = new List<Action>(); for (int i = 0; i < 3; i++) fs.Add(() => Console.Write(i + " "));`.
