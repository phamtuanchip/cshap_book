# Chương 14 — Đóng gói và Property

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **đóng gói** và vì sao class phải tự bảo vệ dữ liệu của nó.
- Dùng đúng các **access modifier**: `public`, `private`, `protected`, `internal`.
- Viết **property** (auto-property, `get`/`set` có kiểm tra, `private set`, `init`, `required`).
- Thiết kế **đối tượng bất biến** và không để lộ collection nội bộ.

Code mẫu: [`code/ch14-encapsulation-property/`](../../code/ch14-encapsulation-property/).

## Đóng gói là gì?

**Đóng gói (encapsulation)** = giấu dữ liệu bên trong class và chỉ cho bên ngoài tác động qua các "cửa" do
class kiểm soát. Lý do:

1. **Bảo vệ tính hợp lệ**: số dư không được âm, tuổi không được 500.
2. **Tự do thay đổi bên trong**: bên ngoài chỉ biết "cửa", nên đổi cách lưu trữ không làm vỡ code khác.
3. **Giảm phụ thuộc**: ít thứ lộ ra thì ít chỗ có thể dùng sai.

Ví dụ tệ: `public decimal SoDu;` — ai cũng gán được `tk.SoDu = 1_000_000_000`, hoặc `-5`.

## Access modifier

| Modifier | Ai truy cập được |
|----------|------------------|
| `public` | mọi nơi |
| `private` | chỉ bên trong chính class đó (**mặc định** của thành viên) |
| `protected` | class đó và class con (Chương 16) |
| `internal` | mọi code trong cùng assembly (project) — mặc định của class cấp cao nhất |
| `protected internal` / `private protected` | kết hợp hai điều kiện |

**Nguyên tắc:** để mọi thứ `private`, chỉ mở (`public`) khi thật cần. Dễ mở thêm về sau, rất khó thu hẹp lại.

## Property — cửa vào có kiểm soát

Property trông như field khi dùng, nhưng thực chất là cặp phương thức `get`/`set`:

```csharp
public class TaiKhoan
{
    private decimal _soDu;   // field ẩn (backing field)

    public decimal SoDu
    {
        get => _soDu;
        private set
        {
            if (value < 0) throw new InvalidOperationException("So du khong duoc am");
            _soDu = value;
        }
    }
}
```

- `get` trả giá trị; `set` nhận giá trị mới qua từ khoá `value`.
- `private set`: bên ngoài **đọc** được nhưng chỉ class mới **ghi** được.

### Auto-property

Khi không cần logic, dùng dạng rút gọn (trình biên dịch tự sinh backing field):

```csharp
public string ChuTaiKhoan { get; set; }
public string SoTaiKhoan { get; }          // chỉ gán được trong constructor
public bool ConTien => _soDu > 0;          // property tính toán, không lưu trữ
```

**Vì sao dùng property thay vì field `public`?** Hôm nay `Ten` chỉ là auto-property; ngày mai cần kiểm tra hoặc
ghi log thì thêm logic vào `get/set` mà **không đổi cú pháp** ở nơi gọi. Với field public không làm được.
Ngoài ra nhiều framework (JSON, ORM, data binding) chỉ làm việc với property.

### `init` và `required`

```csharp
public class Sach
{
    public required string TieuDe { get; init; }   // bắt buộc gán, và chỉ gán lúc khởi tạo
    public int NamXuatBan { get; init; }
}

var s = new Sach { TieuDe = "C#", NamXuatBan = 2025 };
// s.NamXuatBan = 2026;   // LỖI: init-only
```

- `init`: gán được trong constructor hoặc object initializer, sau đó **chỉ đọc**.
- `required` (C# 11): trình biên dịch buộc phải gán khi tạo — không quên được.

## Phương thức thay vì setter cho nghiệp vụ

Không phải mọi thay đổi đều nên là "gán thuộc tính". Với tài khoản, hành vi có ý nghĩa nghiệp vụ, nên cung cấp
phương thức:

```csharp
public void NapTien(decimal soTien)
{
    if (soTien <= 0) throw new ArgumentOutOfRangeException(nameof(soTien));
    SoDu += soTien;
    _lichSu.Add($"+{soTien}");
}

public bool RutTien(decimal soTien)
{
    if (soTien <= 0 || soTien > SoDu) return false;
    SoDu -= soTien;
    _lichSu.Add($"-{soTien}");
    return true;
}
```

Hai phương thức này bảo đảm mọi thay đổi đều được kiểm tra **và** ghi lịch sử. Không có đường nào khác để thay
đổi số dư → đối tượng luôn hợp lệ (đây gọi là giữ **bất biến của lớp** — class invariant).

## Đừng để lộ collection nội bộ

```csharp
public List<string> LichSu { get; } = [];                     // TỆ: bên ngoài tự Add/Clear được
private readonly List<string> _lichSu = [];
public IReadOnlyList<string> LichSu => _lichSu;               // TỐT: chỉ đọc
```

Trả về `IReadOnlyList<T>` (hoặc `IEnumerable<T>`) để bên ngoài chỉ xem mà không phá được dữ liệu.

## Đối tượng bất biến (immutable)

Đối tượng không đổi sau khi tạo: mọi thuộc tính `get`/`init`. Muốn "sửa" thì tạo bản mới:

```csharp
public DiemDat DichChuyen(double dx, double dy) => new() { X = X + dx, Y = Y + dy };
```

Ưu điểm: an toàn khi chia sẻ giữa nhiều nơi/luồng, dễ suy luận, không có trạng thái "nửa vời". Dữ liệu giá trị
(toạ độ, tiền, ngày) rất hợp phong cách này; `record` (Chương 20) làm việc này gọn hơn nữa.

## Quy ước đặt tên

- Property, phương thức, class: `PascalCase` (`SoDu`, `NapTien`).
- Field private: `_camelCase` (`_soDu`).
- Tham số, biến cục bộ: `camelCase`.

## Lỗi thường gặp

- `CS0122: 'X' is inaccessible due to its protection level` — truy cập thành viên `private`.
- Stack overflow trong property tự tham chiếu: `public int X { get => X; }` (gọi lại chính nó) — phải dùng backing field.
- Để `public List<T>` cho phép bên ngoài phá dữ liệu.
- Kiểm tra hợp lệ trong `set` nhưng constructor lại gán thẳng vào field, bỏ qua kiểm tra.
- Lạm dụng getter/setter cho mọi field: class thành "túi dữ liệu", logic nằm rải rác nơi khác (giống lập trình thủ tục).

## Bài tập

1. Viết class `NhanVien` có `Ten` (không rỗng) và `Luong` (không âm) với kiểm tra ném `ArgumentException`.
2. Thêm vào `TaiKhoan` phương thức `ChuyenKhoan(TaiKhoan den, decimal soTien)`, đảm bảo trừ và cộng cùng thành công hoặc cùng thất bại.
3. Viết class `NhietDo` bất biến với `Celsius` và property tính toán `Fahrenheit`.
4. Cố tình thử các dòng bị comment trong `Program.cs` và đọc thông báo lỗi.
5. Giải thích vì sao `LichSu` trả `IReadOnlyList<string>` nhưng vẫn có thể bị "lách" bằng ép kiểu — và cách phòng (`AsReadOnly()`).
