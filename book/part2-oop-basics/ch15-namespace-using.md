# Chương 15 — Namespace, `using` và tổ chức project

## Mục tiêu học

Sau chương này, bạn sẽ:

- Dùng **namespace** để nhóm class và tránh trùng tên.
- Hiểu `using` (directive, alias, `using static`, global using, implicit usings).
- Tổ chức project nhiều file/thư mục và hiểu **assembly** với `internal`.
- Đặt tên và cấu trúc thư mục theo quy ước chung của .NET.

Code mẫu: [`code/ch15-namespace-using/`](../../code/ch15-namespace-using/).

## Vấn đề: trùng tên

Dự án lớn có hàng trăm class, và thư viện bên ngoài cũng có class của chúng. Hai class cùng tên `Sach` hay
`Logger` sẽ đụng nhau. **Namespace** là "họ" của class: `ThuVien.Models.Sach` khác `CuaHang.Sach`.

## Khai báo namespace

Dạng **file-scoped** (C# 10) — khuyến nghị:

```csharp
namespace ThuVien.Models;

public class Sach { ... }
```

Một dòng áp dụng cho cả file, đỡ thụt lề. Dạng khối cũ:

```csharp
namespace ThuVien.Models
{
    public class Sach { ... }
}
```

Dấu `.` thể hiện cấp bậc: `ThuVien` → `Models` → `Sach`. Quy ước: namespace khớp với **thư mục** chứa file
(`Models/Sach.cs` ↔ `namespace ThuVien.Models`), bắt đầu bằng tên dự án/công ty.

## `using` — dùng tên ngắn

Không có `using`, phải viết tên đầy đủ:

```csharp
var s = new ThuVien.Models.Sach { ... };
```

Có `using`, viết tên ngắn:

```csharp
using ThuVien.Models;
var s = new Sach { ... };
```

Các dạng khác:

```csharp
using Console = System.Console;          // alias: đặt bí danh (dùng khi trùng tên)
using static System.Math;                // dùng thẳng Sqrt(x) thay vì Math.Sqrt(x)
global using System.Text;                // áp dụng cho MỌI file trong project
```

**Implicit usings**: với `<ImplicitUsings>enable</ImplicitUsings>` trong `.csproj`, SDK tự thêm
`global using` cho các namespace phổ biến (`System`, `System.Collections.Generic`, `System.IO`, `System.Linq`,
`System.Threading.Tasks`...). Đó là lý do ta dùng `List<T>`, `Console` mà không cần `using`.

Khi hai namespace có class cùng tên và cả hai đều `using`, trình biên dịch báo `CS0104: ambiguous reference` — dùng
alias hoặc tên đầy đủ để giải quyết.

## Assembly và `internal`

- **Namespace** là cách nhóm về mặt *logic/tên gọi*.
- **Assembly** (`.dll`/`.exe`) là đơn vị *biên dịch/triển khai vật lý* = một project.

Một assembly có thể chứa nhiều namespace; một namespace có thể trải qua nhiều assembly. Hai khái niệm độc lập.

`internal` chỉ cho phép truy cập trong cùng **assembly**:

```csharp
internal void XoaHet() => _sach.Clear();   // code trong project khác không thấy
```

Vì vậy khi tách một thư viện (`classlib`), chỉ đánh `public` cho phần muốn cho người khác dùng, phần còn lại để
`internal` — bạn tự do thay đổi phần đó mà không lo làm vỡ người dùng thư viện.

## Tổ chức project

Cấu trúc project trong code mẫu:

```
ch15-namespace-using/
├── ch15-namespace-using.csproj
├── Program.cs
├── Models/
│   └── Sach.cs            → namespace ThuVien.Models
└── Services/
    └── KhoSach.cs         → namespace ThuVien.Services
```

Quy ước phổ biến:

- **Một file một kiểu** (class/record/interface/enum), tên file = tên kiểu.
- Nhóm theo **vai trò**: `Models` (dữ liệu), `Services` (nghiệp vụ), `Repositories`, `Utils`...
- Hoặc nhóm theo **tính năng** (`Orders/`, `Customers/`) — thường tốt hơn ở dự án lớn.
- Lớp phụ thuộc theo một chiều: `Services` dùng `Models`; `Models` **không** dùng `Services`.

Khi dự án lớn dần, tách thành nhiều project trong một solution (như Chương 4): ví dụ `ThuVien.Core` (model,
nghiệp vụ) và `ThuVien.App` (console). Ranh giới giữa các project cưỡng chế sự tách bạch mà namespace đơn thuần
không làm được.

## Quy ước đặt tên tổng hợp

| Thành phần | Quy ước | Ví dụ |
|-----------|---------|-------|
| Namespace, class, method, property | `PascalCase` | `ThuVien.Models`, `NapTien` |
| Interface | `I` + `PascalCase` | `IKhoSach` |
| Field private | `_camelCase` | `_sach` |
| Tham số, biến cục bộ | `camelCase` | `soTien` |
| Hằng | `PascalCase` | `MaxRetry` |

## Lỗi thường gặp

- `CS0246: The type or namespace name 'X' could not be found` — thiếu `using`, thiếu tham chiếu project/package,
  hoặc gõ sai tên.
- `CS0104: 'X' is an ambiguous reference` — hai `using` cùng có kiểu `X`.
- Quên `public`: class mặc định là `internal`, project khác không thấy được.
- Namespace không khớp thư mục → khó tìm file; IDE cũng cảnh báo.
- Vòng phụ thuộc giữa các namespace (A dùng B, B dùng A) — dấu hiệu thiết kế cần xem lại; giữa các project thì
  không thể xảy ra (lỗi biên dịch).

## Bài tập

1. Thêm class `NguoiMuon` vào namespace `ThuVien.Models` và service `MuonSach` trong `ThuVien.Services`.
2. Thêm `global using ThuVien.Models;` vào một file `GlobalUsings.cs` rồi bỏ `using` khỏi các file khác.
3. Tách `Models` và `Services` thành hai project (`classlib`) và một `console`, thiết lập tham chiếu. Điều gì
   xảy ra với `internal` nay?
4. Tạo hai class cùng tên `Logger` ở hai namespace, dùng cả hai trong một file bằng alias.
5. Dùng `using static System.Math;` viết hàm tính khoảng cách hai điểm mà không gõ chữ `Math.`.
