# Chương 30 — LINQ

## Mục tiêu học

Sau chương này, bạn sẽ:

- Truy vấn dữ liệu bằng **LINQ** dạng method syntax và query syntax.
- Dùng thành thạo `Where`, `Select`, `OrderBy`, `GroupBy`, `Join`, `Any/All`, `Sum/Average/Max`, `Skip/Take`.
- Hiểu **thực thi trì hoãn** và khi nào phải `ToList()`/`ToArray()`.
- Tránh các bẫy hiệu năng và logic thường gặp.

Code mẫu: [`code/ch30-linq/`](../../code/ch30-linq/).

## LINQ là gì?

**LINQ** (Language Integrated Query) là bộ phương thức mở rộng (Chương 19) trên `IEnumerable<T>` để **truy vấn**
tập dữ liệu theo kiểu khai báo: nói *muốn gì* thay vì viết vòng lặp *làm thế nào*.

```csharp
// Kiểu vòng lặp
var ketQua = new List<string>();
foreach (var s in sanPham)
    if (s.Gia < 1_000_000m) ketQua.Add(s.Ten);

// Kiểu LINQ
var ketQua = sanPham.Where(s => s.Gia < 1_000_000m).Select(s => s.Ten);
```

Cùng cú pháp dùng cho mảng, `List`, `Dictionary`, file, XML, và — quan trọng nhất — **CSDL qua Entity Framework Core** (Tập 2):
truy vấn C# được dịch sang SQL. Cần `using System.Linq;` (đã có sẵn nhờ implicit usings).

## Các toán tử cốt lõi

Dữ liệu mẫu: `record SanPham(string Ten, string NhomHang, decimal Gia, int TonKho)`.

### Lọc và chiếu

```csharp
sanPham.Where(s => s.Gia < 1_000_000m)                 // lọc
sanPham.Select(s => s.Ten)                             // chiếu: lấy/biến đổi từng phần tử
sanPham.Select(s => new { s.Ten, ThanhTien = s.Gia * s.TonKho })   // kiểu ẩn danh
sanPham.SelectMany(dh => dh.DongHang)                  // "làm phẳng" danh sách lồng nhau
```

### Sắp xếp

```csharp
sanPham.OrderBy(s => s.Gia)
sanPham.OrderByDescending(s => s.Gia).ThenBy(s => s.Ten)
```

### Truy xuất phần tử và kiểm tra

| Phương thức | Ý nghĩa |
|-------------|---------|
| `First()` / `FirstOrDefault()` | phần tử đầu (ném lỗi / trả `default` nếu rỗng) |
| `Single()` / `SingleOrDefault()` | đúng **một** phần tử (lỗi nếu 0 hoặc nhiều hơn 1) |
| `Last()`, `ElementAt(i)` | cuối / theo chỉ số |
| `Any()` / `Any(đk)` | có phần tử (thoả điều kiện) nào không? |
| `All(đk)` | mọi phần tử thoả điều kiện? |
| `Contains(x)` | có chứa `x`? |

Dùng `Any()` thay cho `Count() > 0` — dừng ngay khi thấy phần tử đầu tiên.

### Tổng hợp

```csharp
sanPham.Count(s => s.NhomHang == "Phu kien")
sanPham.Sum(s => s.Gia * s.TonKho)
sanPham.Average(s => s.Gia)
sanPham.Max(s => s.Gia); sanPham.MinBy(s => s.Gia)   // MaxBy/MinBy trả về CẢ phần tử
new[] {"a","b","c"}.Aggregate((acc, x) => acc + "-" + x)   // gộp tuỳ ý
```

### Nhóm: `GroupBy`

```csharp
foreach (var nhom in sanPham.GroupBy(s => s.NhomHang))
    Console.WriteLine($"{nhom.Key}: {nhom.Count()} sp, tổng tồn {nhom.Sum(s => s.TonKho)}");
```

Mỗi `nhom` là một `IGrouping<TKey, T>`: có `.Key` và chính nó là dãy các phần tử thuộc nhóm.

### Ghép: `Join`

```csharp
sanPham.Join(nhomInfo, s => s.NhomHang, n => n.Ten, (s, n) => $"{s.Ten} ({n.MoTa})")
```

Ghép hai nguồn theo khoá chung (giống `INNER JOIN` của SQL). `Zip` ghép theo vị trí; `Concat`/`Union`/`Intersect`/`Except`
là phép toán tập hợp.

### Phân trang và loại trùng

```csharp
sanPham.OrderBy(s => s.Ten).Skip((trang - 1) * kichThuoc).Take(kichThuoc)
sanPham.Select(s => s.NhomHang).Distinct()
sanPham.Chunk(3)     // chia thành các mảng 3 phần tử
```

### Chuyển thành cấu trúc cụ thể

```csharp
.ToList()   .ToArray()   .ToDictionary(s => s.Ten, s => s.Gia)   .ToHashSet()   .ToLookup(...)
```

## Query syntax

Cú pháp giống SQL, biên dịch ra đúng các lời gọi method ở trên:

```csharp
var re = from s in sanPham
         where s.Gia < 1_000_000m
         orderby s.Gia descending
         select s.Ten;
```

Method syntax phổ biến hơn (chuỗi `.` linh hoạt, đủ toán tử); query syntax nổi bật khi có nhiều `join`/`let`. Bạn có thể trộn cả hai.

## Thực thi trì hoãn — nhắc lại và mở rộng

Hầu hết toán tử LINQ (`Where`, `Select`, `OrderBy`...) **không chạy ngay**; chúng chỉ tạo "công thức" và chạy khi
bạn **duyệt** (`foreach`, `ToList`, `Count`, `First`...):

```csharp
var danhSach = new List<int> { 1, 2, 3 };
var chan = danhSach.Where(x => x % 2 == 0);   // chưa chạy
danhSach.Add(4);
Console.WriteLine(string.Join(",", chan));    // 2,4 — thấy cả số 4 thêm sau!

var daChot = danhSach.Where(x => x % 2 == 0).ToList();   // chạy NGAY, lưu kết quả
danhSach.Add(6);
Console.WriteLine(string.Join(",", daChot));               // 2,4 — không có 6
```

Hệ quả:

- Truy vấn duyệt **nhiều lần** thì **chạy lại nhiều lần** (và với EF Core là **gọi CSDL nhiều lần**). Cần dùng nhiều
  lần hoặc cần "chụp ảnh" dữ liệu → `.ToList()` một lần.
- Biến bị "đóng" (closure) trong lambda được đọc **lúc chạy**, không phải lúc tạo truy vấn.
- Lỗi trong lambda chỉ hiện khi duyệt, không phải lúc gọi `Where`.

## Bẫy và mẹo hiệu năng

- **Đừng `ToList()` quá sớm** khi còn muốn lọc thêm (đặc biệt với EF Core: lọc phải diễn ra ở CSDL, không phải trong bộ nhớ).
- `Count()` duyệt hết nếu nguồn là `IEnumerable` thuần — nếu chỉ hỏi "có không?" dùng `Any()`.
- `OrderBy` rồi `First()` là O(n log n); dùng `MinBy`/`MaxBy` là O(n).
- `list.Where(...).Count()` → `list.Count(...)`.
- Với danh sách lớn, chuyển `Contains` trên `List` thành `HashSet` (Chương 26) khi lọc lặp.
- LINQ trên tập nhỏ rất tiện; đường ống nóng (hot path) hàng triệu phần tử hãy đo (`Stopwatch`/BenchmarkDotNet) trước khi tối ưu.
- Tránh tác dụng phụ (sửa biến ngoài, ghi log) bên trong `Where`/`Select`: vì chạy trì hoãn và có thể chạy nhiều lần.

## Lỗi thường gặp

- `InvalidOperationException: Sequence contains no elements` — `First()`/`Max()` trên dãy rỗng (dùng `FirstOrDefault`, `DefaultIfEmpty`).
- `Single()` khi có nhiều hơn một kết quả.
- Duyệt cùng truy vấn nhiều lần rồi ngạc nhiên vì chạy lại.
- Sửa collection nguồn khi đang duyệt truy vấn lười.
- Nhầm `Select` (biến đổi) với `Where` (lọc).
- `NullReferenceException` với `FirstOrDefault()` không kiểm tra `null` (`?.`).

## Bài tập

Với `List<SanPham>` của chương:

1. Liệt kê tên các sản phẩm còn hàng, giá giảm dần.
2. Tính tổng giá trị tồn kho từng nhóm hàng; tìm nhóm có giá trị cao nhất.
3. Tìm sản phẩm đắt thứ hai (`OrderByDescending` + `Skip(1)` + `First`).
4. Viết một truy vấn thống kê: mỗi nhóm có bao nhiêu sản phẩm hết hàng.
5. Viết lại đoạn code dùng vòng lặp `foreach` + `if` của bạn (từ chương trước) thành LINQ; so sánh độ dễ đọc.
6. Chứng minh thực thi trì hoãn bằng một truy vấn có `Console.WriteLine` bên trong `Select` — in ra khi nào?
