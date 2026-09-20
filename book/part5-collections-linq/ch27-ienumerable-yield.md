# Chương 27 — `IEnumerable`, `yield` và sắp xếp

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu `foreach` hoạt động dựa trên `IEnumerable<T>`/`IEnumerator<T>`.
- Viết bộ sinh dữ liệu bằng **`yield return`** và hiểu **thực thi trì hoãn** (deferred execution).
- Tự cài đặt `IEnumerable<T>` cho lớp của mình.
- Sắp xếp bằng `Sort`, `OrderBy/ThenBy`, `IComparer`/`Comparer.Create`, biết sự khác nhau.

Code mẫu: [`code/ch27-ienumerable-yield/`](../../code/ch27-ienumerable-yield/).

## `foreach` thực sự làm gì?

```csharp
foreach (var x in ds) { ... }
```

được trình biên dịch biến thành:

```csharp
using var it = ds.GetEnumerator();
while (it.MoveNext())
{
    var x = it.Current;
    ...
}
```

Tức là **bất kỳ kiểu nào có `GetEnumerator()`** (thường là cài `IEnumerable<T>`) đều dùng được với `foreach`.

```csharp
interface IEnumerable<T> { IEnumerator<T> GetEnumerator(); }
interface IEnumerator<T> { bool MoveNext(); T Current { get; } void Reset(); }
```

`IEnumerable<T>` là interface "gốc" của mọi collection và là kiểu nền của **LINQ** (Chương 30): nó chỉ nói "tôi có thể
đưa bạn từng phần tử một", **không** nói có bao nhiêu hay cho truy cập theo chỉ số.

## `yield return` — sinh dữ liệu theo yêu cầu

Viết bộ liệt kê bằng tay khá dài. `yield return` để trình biên dịch tự sinh:

```csharp
static IEnumerable<long> Fibonacci()
{
    long a = 0, b = 1;
    while (true)              // vô hạn, nhưng an toàn!
    {
        yield return a;       // "trả" một giá trị và tạm dừng tại đây
        (a, b) = (b, a + b);
    }
}

foreach (var f in Fibonacci().Take(10)) Console.Write($"{f} ");
```

Mỗi lần `MoveNext()`, phương thức chạy tiếp từ chỗ dừng cho tới `yield return` kế tiếp. Nhờ vậy:

- **Tiết kiệm bộ nhớ**: không dựng toàn bộ danh sách, kể cả dãy vô hạn hay rất lớn (đọc từng dòng file khổng lồ).
- **Lười (lazy)**: chỉ tính khi được yêu cầu. `Take(10)` chỉ chạy 10 vòng.
- `yield break;` kết thúc dãy sớm.

## Thực thi trì hoãn (deferred execution)

```csharp
static IEnumerable<int> DemSo(int n)
{
    for (int i = 1; i <= n; i++) { Console.WriteLine($"  sinh {i}"); yield return i; }
}

var seq = DemSo(3);                    // CHƯA chạy dòng nào của DemSo
Console.WriteLine("Da tao seq");
foreach (var x in seq) Console.WriteLine($"nhan {x}");   // giờ mới chạy: sinh 1, nhan 1, sinh 2, ...
```

Kết quả xen kẽ "sinh" và "nhận" — chứng tỏ giá trị được sinh **từng cái một** theo nhịp `foreach`. Hệ quả cần nhớ:

1. Phương thức `yield` **không chạy** cho tới khi bắt đầu duyệt — cả kiểm tra tham số cũng bị hoãn (nếu cần kiểm tra ngay,
   tách thành phương thức bọc bên ngoài rồi gọi phương thức `yield` bên trong).
2. Duyệt **hai lần** thì chạy lại từ đầu hai lần; nếu tốn kém, hãy `.ToList()` để "chốt" kết quả.
3. Dữ liệu nguồn thay đổi giữa lúc tạo và lúc duyệt sẽ ảnh hưởng kết quả (Chương 30).

## Tự cài `IEnumerable<T>`

```csharp
class DanhSachTen(string[] ten) : IEnumerable<string>
{
    public IEnumerator<string> GetEnumerator()
    {
        foreach (var t in ten) yield return t;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

foreach (var t in new DanhSachTen(["An", "Binh"])) ...
```

Cài đặt interface không generic `IEnumerable` (dòng cuối) chỉ là thủ tục kế thừa; chuyển thẳng tới bản generic.
Sau khi cài đặt, lớp của bạn dùng được với `foreach` **và mọi phương thức LINQ** (`Where`, `Select`...).

## Sắp xếp

### `List<T>.Sort` — tại chỗ

```csharp
sv.Sort((x, y) => y.Diem.CompareTo(x.Diem));   // giảm dần theo điểm
```

- Thay đổi **danh sách gốc**, trả về `void`.
- **Không ổn định**: hai phần tử "bằng nhau" có thể đổi thứ tự tương đối. Dùng `IComparable<T>`/`IComparer<T>`/lambda (Chương 20).

### `OrderBy` / `ThenBy` (LINQ) — tạo dãy mới

```csharp
var xep = sv.OrderByDescending(s => s.Diem).ThenBy(s => s.Ten);
```

- **Ổn định** (giữ thứ tự gốc của các phần tử bằng nhau), **không** sửa nguồn, trả về `IOrderedEnumerable<T>`, thực thi trì hoãn.
- `ThenBy`/`ThenByDescending` thêm khoá phụ. Rất dễ đọc — chọn cách này cho hầu hết trường hợp.

### `Comparer<T>.Create` và `IComparer<T>`

```csharp
var theoTen = Comparer<SinhVien>.Create((x, y) => string.Compare(x.Ten, y.Ten, StringComparison.Ordinal));
sv.Sort(theoTen);
```

Dùng khi cần truyền một bộ so sánh có thể tái sử dụng (cho `Sort`, `SortedSet`, `SortedDictionary`, `Array.Sort`, `OrderBy(x => x, comparer)`).

### Sắp xếp chuỗi

Luôn chỉ rõ `StringComparison`/`StringComparer` — sắp xếp theo culture khác nhau cho kết quả khác nhau và chậm hơn
`Ordinal`.

## Khi nào trả `IEnumerable<T>`?

- Bạn muốn trả một dãy chỉ-đọc, có thể lười, và người dùng chỉ cần duyệt.
- Nhưng đừng để lộ nguồn thay đổi được, và nếu người gọi sẽ duyệt nhiều lần hoặc cần `Count`, hãy trả `IReadOnlyList<T>`.
- Cảnh giác **duyệt nhiều lần**: mỗi lần lại chạy lại toàn bộ truy vấn/truy cập CSDL phía sau.

## Lỗi thường gặp

- Tưởng phương thức `yield` chạy ngay khi gọi.
- Duyệt cùng `IEnumerable` nhiều lần ngầm gây tính lại tốn kém (hoặc gọi CSDL nhiều lần).
- `yield return` trong `try/catch` (không được phép với `catch`) hoặc trong phương thức có tham số `ref`/`out`.
- Dùng `Sort` khi cần sắp xếp **ổn định**.
- `NullReferenceException` do so sánh giá trị `null` trong bộ so sánh tự viết.
- Dùng `Count()` (LINQ) trên `IEnumerable` khổng lồ chỉ để hỏi "có phần tử nào không?" — dùng `Any()`.

## Bài tập

1. Viết `IEnumerable<int> SoChan(int toiDa)` bằng `yield return`; in ra và dùng LINQ `Take(5)`.
2. Viết `IEnumerable<string> DocDong(string path)` đọc file theo từng dòng bằng `yield` (hoặc gọi `File.ReadLines`).
3. Tự cài `PhamVi` (`IEnumerable<int>`) để `foreach (var i in new PhamVi(1, 5))` chạy được.
4. So sánh `Sort` và `OrderBy` trên danh sách có nhiều phần tử điểm bằng nhau — thứ tự có khác không? Vì sao?
5. Sắp xếp danh sách tên tiếng Việt theo `StringComparer.Create(new CultureInfo("vi-VN"), true)` và so với `Ordinal`.
