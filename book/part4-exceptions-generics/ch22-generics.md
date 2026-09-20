# Chương 22 — Generics

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu vấn đề mà **generics** giải quyết: tái sử dụng code mà vẫn **an toàn kiểu**.
- Viết **generic class**, **generic method**, **generic interface/record**.
- Dùng **constraint** (`where`) để giới hạn kiểu.
- Hiểu ở mức khái niệm **covariance** / **contravariance** (`out` / `in`).

Code mẫu: [`code/ch22-generics/`](../../code/ch22-generics/).

## Vấn đề: một ngăn xếp cho nhiều kiểu

Muốn viết ngăn xếp (stack) lưu số nguyên, rồi lại cần ngăn xếp lưu chuỗi… Có hai cách "cũ" đều tệ:

1. Viết `NganXepInt`, `NganXepString`, ... — lặp code.
2. Dùng `object` cho mọi thứ — mất an toàn kiểu:

```csharp
var stack = new NganXepObject();
stack.Day(1);
stack.Day("chao");            // vẫn chạy!
int x = (int)stack.Lay();     // InvalidCastException lúc chạy
```

Ngoài ra số nguyên bị **boxing** (bọc vào heap) khi chuyển thành `object` → chậm.

**Generics** giải quyết cả hai: viết **một lần** với tham số kiểu `T`, trình biên dịch kiểm tra kiểu **lúc biên dịch**,
không boxing.

## Generic class

```csharp
class NganXep<T>
{
    private readonly List<T> _ds = [];
    public int SoLuong => _ds.Count;
    public void Day(T item) => _ds.Add(item);

    public T Lay()
    {
        if (_ds.Count == 0) throw new InvalidOperationException("Ngan xep rong");
        T item = _ds[^1];
        _ds.RemoveAt(_ds.Count - 1);
        return item;
    }
}

var soNguyen = new NganXep<int>();
soNguyen.Day(1);
// soNguyen.Day("a");   // LỖI BIÊN DỊCH — an toàn kiểu
```

`T` là **tham số kiểu** (type parameter), được thay bằng kiểu cụ thể khi dùng (`NganXep<int>`, `NganXep<string>`). Quy ước
tên: `T`, hoặc `TKey`, `TValue`, `TResult`. Bạn đã dùng generics từ lâu: `List<T>`, `Dictionary<TKey, TValue>`.

## Generic method

Phương thức có tham số kiểu riêng; trình biên dịch thường **suy luận** được `T`:

```csharp
public static void HoanDoi<T>(ref T a, ref T b) => (a, b) = (b, a);

int x = 1, y = 2;
Tien.HoanDoi(ref x, ref y);      // T = int (tự suy ra)
Tien.HoanDoi<string>(ref s1, ref s2);   // chỉ rõ khi cần
```

## Constraint — giới hạn kiểu với `where`

Với `T` bất kỳ, bạn chỉ dùng được những gì `object` có. Muốn so sánh hai `T` thì phải cho biết `T` **so sánh được**:

```csharp
public static T LonNhat<T>(params T[] ds) where T : IComparable<T>
{
    T max = ds[0];
    foreach (var x in ds)
        if (x.CompareTo(max) > 0) max = x;
    return max;
}

Tien.LonNhat(3, 9, 4);               // 9
Tien.LonNhat("cam", "tao", "xoai");   // "xoai"
// Tien.LonNhat(new object(), ...);   // LỖI: object không IComparable<object>
```

Các constraint thường gặp:

| Constraint | Ý nghĩa |
|------------|---------|
| `where T : class` | `T` là kiểu tham chiếu |
| `where T : struct` | `T` là kiểu giá trị |
| `where T : new()` | có constructor không tham số (cho phép `new T()`) |
| `where T : BaseClass` | `T` kế thừa `BaseClass` |
| `where T : IInterface` | `T` cài đặt interface |
| `where T : notnull` | `T` không nullable |
| Kết hợp | `where T : class, ICoMa, new()` (thứ tự: class/struct trước, `new()` cuối) |

Constraint cho phép **dùng** thành viên của kiểu ràng buộc bên trong code generic (ví dụ `item.Ma` khi `T : ICoMa`).

```csharp
class KhoTheoMa<T> where T : class, ICoMa
{
    private readonly Dictionary<string, T> _ds = [];
    public void Them(T item) => _ds[item.Ma] = item;
    public T? Tim(string ma) => _ds.GetValueOrDefault(ma);
}
```

## Generic interface và record

```csharp
interface IKetQua<out T> { bool ThanhCong { get; } T? GiaTri { get; } string? Loi { get; } }

record KetQua<T>(bool ThanhCong, T? GiaTri, string? Loi) : IKetQua<T>
{
    public static KetQua<T> ThatBai(string loi) => new(false, default, loi);
}
```

`default` cho giá trị mặc định của `T` (0 với số, `null` với tham chiếu). Kiểu **Result** như trên là mẫu quen thuộc để
trả về "thành công hoặc lỗi" mà không ném exception (sẽ gặp lại ở Tập 3).

## Covariance và contravariance (cơ bản)

Câu hỏi: `string` là `object`, vậy `IEnumerable<string>` có là `IEnumerable<object>` không?

- **Covariance (`out T`)**: giữ nguyên chiều kế thừa; an toàn khi `T` chỉ xuất hiện ở vị trí **đầu ra** (trả về).

```csharp
IEnumerable<string> tenChuoi = ["a", "b"];
IEnumerable<object> tenObj = tenChuoi;   // OK vì IEnumerable<out T>
```

- **Contravariance (`in T`)**: đảo chiều; an toàn khi `T` chỉ ở vị trí **đầu vào** (tham số).

```csharp
Action<object> inRa = o => Console.WriteLine(o);
Action<string> inChuoi = inRa;   // OK vì Action<in T>: nơi nhận string có thể dùng hàm nhận object
```

Còn `List<T>` vừa đọc vừa ghi nên **không** biến thiên: `List<object> ds = new List<string>();` là lỗi — nếu cho phép,
bạn sẽ `Add(5)` được vào một danh sách chuỗi. Bạn hiếm khi tự khai báo `in`/`out`, nhưng hiểu để đọc hiểu lỗi và API.

## Khi nào nên viết generic?

- Có cấu trúc dữ liệu/thuật toán dùng lại cho nhiều kiểu (kho lưu trữ, cache, kết quả, phân trang).
- Đừng generic hoá quá sớm: nếu chỉ có một kiểu dùng, cứ viết cụ thể; khi thứ hai xuất hiện hãy tổng quát hoá.
- Nếu constraint dài dằng dặc và lộn xộn, có thể thiết kế đang phức tạp hơn cần thiết.

## Lỗi thường gặp

- `CS0314/CS0311`: kiểu truyền vào không thoả constraint.
- `CS0304: Cannot create an instance of the variable type 'T'` — thiếu `where T : new()`.
- `CS0019: Operator '>' cannot be applied to operands of type 'T'` — dùng `IComparable<T>` thay vì toán tử.
- Nhầm `default(T)`: với kiểu tham chiếu là `null` — cân nhắc `T?`/nullable.
- Kỳ vọng `List<Con>` gán được cho `List<Cha>` (không được, xem covariance).
- Khai báo tham số kiểu trùng tên kiểu có sẵn (`class Kho<string>` là sai — `string` không phải tên tham số).

## Bài tập

1. Viết `HangDoi<T>` (queue) với `Them`, `Lay`, `SoLuong` bằng `LinkedList<T>` bên trong.
2. Viết `static T NhoNhat<T>(IEnumerable<T> ds) where T : IComparable<T>`, thử với `int`, `string`, `record SanPham`.
3. Viết `Cap<TA, TB>` (record generic hai tham số) và phương thức `DaoNguoc()` trả `Cap<TB, TA>`.
4. Viết `Kho<T> where T : class, ICoMa` hỗ trợ `Xoa(string ma)`; thử với `Sach` và với `int` — quan sát lỗi.
5. Giải thích vì sao `IReadOnlyList<T>` là covariant còn `IList<T>` thì không.
