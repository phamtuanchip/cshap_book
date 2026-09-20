# Chương 10 — Phương thức

## Mục tiêu học

Sau chương này, bạn sẽ:

- Viết phương thức có tham số và giá trị trả về; hiểu vì sao cần chia code thành phương thức.
- Dùng **overloading**, tham số tuỳ chọn, tham số đặt tên, `params`.
- Phân biệt truyền theo **giá trị** và theo **tham chiếu** (`ref`, `out`, `in`).
- Trả về nhiều giá trị bằng `out` và **tuple**; viết đệ quy; hiểu phạm vi (scope) biến.

Code mẫu: [`code/ch10-phuong-thuc/`](../../code/ch10-phuong-thuc/).

## Vì sao cần phương thức?

**Phương thức** (method) là một khối lệnh có tên, có thể gọi lại nhiều lần. Chia code thành phương thức
giúp **tái sử dụng**, **dễ đọc** (mỗi phương thức làm một việc, tên nói lên việc đó) và **dễ kiểm thử**.

```csharp
static int Binh(int x) => x * x;   // kiểu_trả_về Tên(tham_số) => biểu_thức;
```

Dạng đầy đủ:

```csharp
public static int Binh(int x)
{
    return x * x;
}
```

- `int` (đứng trước tên): kiểu giá trị trả về. `void` nếu không trả về gì.
- `x`: **tham số** (parameter) trong định nghĩa; giá trị bạn truyền lúc gọi là **đối số** (argument).
- `return` trả giá trị và kết thúc phương thức.
- `=>` là cú pháp **expression-bodied** cho phương thức chỉ có một biểu thức.

Trong sách này, phương thức dùng chung đặt trong một `static class` (`Ham`): ở top-level statements, hàm
cục bộ không overload được. Từ Chương 13 ta sẽ đặt phương thức trong class đúng nghĩa OOP.

## Overloading — cùng tên, khác tham số

```csharp
public static int Cong(int a, int b) => a + b;
public static double Cong(double a, double b) => a + b;
public static int Cong(int a, int b, int c) => a + b + c;
```

Trình biên dịch chọn phiên bản theo **số lượng và kiểu** đối số (không tính kiểu trả về). Hai phương thức
chỉ khác nhau kiểu trả về thì không hợp lệ.

## Tham số tuỳ chọn, tham số đặt tên, `params`

```csharp
public static string ChaoHoi(string ten, string loiChao = "Xin chao", bool viet = false) { ... }

ChaoHoi("Tuan");                          // dùng mặc định
ChaoHoi("Tuan", viet: true);              // bỏ qua tham số giữa, đặt tên tham số cuối
ChaoHoi(loiChao: "Hi", ten: "An");        // đặt tên: thứ tự tuỳ ý
```

Tham số tuỳ chọn phải đứng **sau** các tham số bắt buộc. Đặt tên giúp lời gọi tự giải thích, đặc biệt với
tham số `bool`.

```csharp
public static int Tong(params int[] so) { ... }
Tong(1, 2, 3, 4);   // số lượng đối số tuỳ ý
```

## Giá trị hay tham chiếu?

Mặc định C# **truyền theo giá trị**: phương thức nhận một **bản sao**.

```csharp
static void TangSai(int x) => x++;    // chỉ tăng bản sao
int v = 10;
TangSai(v);                            // v vẫn là 10
```

Muốn phương thức sửa trực tiếp biến của người gọi, dùng `ref`:

```csharp
static void TangDung(ref int x) => x++;
TangDung(ref v);                       // v = 11
static void HoanDoi(ref int a, ref int b) => (a, b) = (b, a);
```

| Từ khoá | Người gọi phải gán trước? | Phương thức phải gán? | Mục đích |
|---------|--------------------------|----------------------|----------|
| (không) | có | không | truyền bản sao |
| `ref` | **có** | không | đọc và ghi biến gốc |
| `out` | không | **có** | trả về thêm giá trị |
| `in` | có | không (chỉ đọc) | tránh sao chép struct lớn |

> Với **kiểu tham chiếu** (mảng, class), giá trị được truyền là *bản sao của tham chiếu*: phương thức sửa
> được **nội dung** đối tượng, nhưng gán tham số sang đối tượng khác không ảnh hưởng người gọi (trừ khi `ref`).

### `out` — mẫu `TryXxx`

```csharp
public static bool ChiaCoDu(int a, int b, out int thuong, out int du)
{
    if (b == 0) { thuong = 0; du = 0; return false; }
    thuong = a / b;
    du = a % b;
    return true;
}

if (ChiaCoDu(17, 5, out int th, out int du)) Console.WriteLine($"{th} dư {du}");
```

Đây chính là mẫu của `int.TryParse`. Biến `out` có thể khai báo ngay tại chỗ gọi (`out int th`).

### Tuple — trả về nhiều giá trị gọn hơn

```csharp
public static (int Min, int Max) MinMax(int[] mang) => (mang.Min(), mang.Max());

var (min, max) = MinMax([4, 9, 1, 7]);
```

## Đệ quy

Phương thức tự gọi lại chính nó, cần **điều kiện dừng**:

```csharp
public static long GiaiThua(int n) => n <= 1 ? 1 : n * GiaiThua(n - 1);
```

Đệ quy rất hợp cho cấu trúc lồng nhau (cây, thư mục) nhưng mỗi lần gọi tốn bộ nhớ ngăn xếp: đệ quy quá
sâu ném `StackOverflowException` (không bắt được, chương trình chết).

## Phạm vi biến (scope)

Biến chỉ tồn tại trong khối `{ }` khai báo nó. Hàm cục bộ (local function) nhìn được biến của nơi bao quanh:

```csharp
int dem = 0;
void Dem() => dem++;
Dem(); Dem();      // dem = 2
```

Không được khai báo biến cùng tên với biến ở khối bao ngoài (`CS0136`). Ưu tiên biến có phạm vi hẹp nhất có thể.

## Đặt tên & thiết kế

- Tên phương thức là **động từ**, `PascalCase`: `TinhTong`, `LayDanhSach`.
- Mỗi phương thức làm **một việc**; nếu phải dùng chữ "và" để mô tả, hãy tách ra.
- Ít tham số (≤ 3–4); nhiều hơn thì gom vào một đối tượng (Chương 13).

## Lỗi thường gặp

- `CS0165`/`CS0177`: dùng biến chưa gán, hoặc `out` chưa được gán trước khi return.
- `CS0161: not all code paths return a value` — có nhánh không `return`.
- Quên `ref`/`out` ở **lời gọi** (`TangDung(v)` thay vì `TangDung(ref v)`).
- Đệ quy thiếu điều kiện dừng → `StackOverflowException`.
- Nhầm truyền theo giá trị với tham chiếu: tưởng sửa `x` trong phương thức là sửa biến gốc.

## Bài tập

1. Viết `LaNguyenTo(int n)` trả `bool`, rồi in các số nguyên tố ≤ 100 bằng nó.
2. Viết `TinhTB(params double[] so)`; thử gọi với 0 đối số và xử lý cho hợp lý.
3. Viết `TryDocSo(string s, out int kq)` giống `int.TryParse` (không dùng `TryParse`).
4. Viết `UCLN(a, b)` bằng đệ quy (thuật toán Euclid) và bằng vòng lặp.
5. Viết phương thức trả về `(tong, trungBinh, lonNhat)` của một mảng bằng tuple.
