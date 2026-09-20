# Chương 9 — Mảng

## Mục tiêu học

Sau chương này, bạn sẽ:

- Khai báo, khởi tạo, duyệt mảng một chiều; dùng chỉ số `^` và lát cắt `..`.
- Dùng các hàm tiện ích của `Array` (`Sort`, `Reverse`, `IndexOf`...).
- Làm việc với mảng nhiều chiều và **jagged array**.
- Hiểu mảng là **kiểu tham chiếu** và hệ quả khi gán/sao chép.

Code mẫu: [`code/ch09-mang/`](../../code/ch09-mang/).

## Mảng một chiều

**Mảng** lưu một dãy phần tử **cùng kiểu**, kích thước **cố định** sau khi tạo, truy cập bằng chỉ số
bắt đầu từ **0**.

```csharp
int[] diem = [8, 6, 9, 7, 10];        // collection expression (C# 12)
int[] rong = new int[3];               // 3 phần tử, mặc định 0
string?[] ten = new string?[2];        // mặc định null
```

Giá trị mặc định: số → `0`, `bool` → `false`, tham chiếu (`string`...) → `null`.

```csharp
Console.WriteLine(diem[0]);       // 8 — phần tử đầu
Console.WriteLine(diem[^1]);      // 10 — phần tử cuối (chỉ số ngược)
Console.WriteLine(diem.Length);   // 5
diem[1] = 7;                      // gán
```

Truy cập chỉ số ngoài phạm vi ném `IndexOutOfRangeException`.

### Duyệt mảng

```csharp
for (int i = 0; i < diem.Length; i++) Console.Write(diem[i] + " ");
foreach (int d in diem) Console.Write(d + " ");
```

### Lát cắt (range)

```csharp
int[] ba = diem[..3];        // 3 phần tử đầu
int[] giua = diem[1..^1];    // bỏ phần tử đầu và cuối
```

`a..b` lấy từ chỉ số `a` đến **trước** `b`. Kết quả là **mảng mới** (bản sao).

## Tiện ích của `Array`

```csharp
Array.Sort(diem);                  // sắp xếp tăng dần (thay đổi mảng gốc)
Array.Reverse(diem);               // đảo ngược
int vt = Array.IndexOf(diem, 9);   // vị trí đầu tiên của 9, hoặc -1
Array.Copy(nguon, dich, soLuong);  // sao chép
Array.Fill(rong, 7);               // điền giá trị
```

`string.Join(", ", diem)` ghép mảng thành chuỗi để in.

## Mảng hai chiều (hình chữ nhật)

```csharp
int[,] luoi = { { 1, 2, 3 }, { 4, 5, 6 } };   // 2 hàng x 3 cột
Console.WriteLine(luoi[1, 2]);                 // 6
luoi.GetLength(0);   // số hàng = 2
luoi.GetLength(1);   // số cột = 3
```

Mọi hàng có cùng độ dài. Duyệt bằng hai vòng `for` lồng nhau.

## Jagged array — mảng của các mảng

```csharp
int[][] tamGiac = new int[4][];
for (int i = 0; i < tamGiac.Length; i++)
{
    tamGiac[i] = new int[i + 1];              // mỗi hàng dài khác nhau
    for (int j = 0; j <= i; j++) tamGiac[i][j] = j + 1;
}
```

Linh hoạt hơn khi các hàng dài ngắn khác nhau; truy cập bằng `a[i][j]`. Thường nhanh hơn mảng `[,]`.

## Mảng là kiểu tham chiếu

```csharp
int[] a = [1, 2, 3];
int[] b = a;        // b và a trỏ CÙNG một mảng
b[0] = 99;
Console.WriteLine(a[0]);   // 99 !

int[] c = (int[])a.Clone();   // bản sao độc lập (nông)
c[0] = 1;                      // không ảnh hưởng a
```

Gán mảng **không** sao chép dữ liệu, chỉ sao chép tham chiếu. Tương tự khi truyền mảng vào phương thức:
phương thức sửa được nội dung mảng của người gọi (Chương 10). `Clone()` là sao chép **nông**: nếu phần
tử là kiểu tham chiếu thì các đối tượng bên trong vẫn dùng chung.

## Khi nào **không** dùng mảng?

Vì kích thước cố định, mảng bất tiện khi cần thêm/xoá phần tử. Khi đó dùng `List<T>` (Chương 24–25).
Mảng vẫn hợp cho dữ liệu kích thước biết trước, tham số `params`, và cần hiệu năng cao.

## Lỗi thường gặp

- `IndexOutOfRangeException`: dùng `<=` thay vì `<` khi duyệt, hoặc mảng rỗng.
- `NullReferenceException`: mảng chuỗi `new string[3]` chứa `null`, gọi `.Length` trên phần tử sẽ lỗi.
- Tưởng `b = a` là sao chép mảng.
- Thay đổi mảng gốc ngoài ý muốn khi `Array.Sort` (hãy `Clone()` trước nếu cần giữ bản gốc).
- Nhầm `int[,]` với `int[][]`.

## Bài tập

1. Tìm số lớn nhất, nhỏ nhất và vị trí của chúng trong một mảng (không dùng LINQ).
2. Đảo ngược mảng tại chỗ bằng vòng lặp (không dùng `Array.Reverse`).
3. Cộng hai ma trận `2×3` bằng `int[,]`.
4. In tam giác Pascal 6 dòng bằng jagged array.
5. Đếm tần suất từng chữ số 0–9 trong một chuỗi số nhập vào (mảng đếm 10 phần tử).
