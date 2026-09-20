# Chương 37 — Reflection, Attribute và Serialization

## Mục tiêu học

Sau chương này, bạn sẽ:

- Dùng **reflection** để khảo sát kiểu, thành viên và gọi chúng lúc chạy.
- Viết và đọc **attribute** tuỳ chỉnh (metadata gắn vào code).
- Hiểu vì sao framework (ASP.NET Core, EF Core, JSON, test) dựa nhiều vào hai thứ này.
- Nắm khái niệm **serialization** và khi nào nên/không nên dùng.

Code mẫu: [`code/ch37-reflection-attribute/`](../../code/ch37-reflection-attribute/).

## Reflection — chương trình tự soi mình

**Reflection** cho phép khảo sát **thông tin về kiểu lúc chạy**: có những property/method nào, kiểu gì, có attribute gì, và cả
gọi/gán chúng mà không biết trước lúc biên dịch.

```csharp
Type kieu = typeof(SanPham);            // hoặc obj.GetType()
foreach (var pr in kieu.GetProperties())
    Console.WriteLine($"{pr.Name}: {pr.PropertyType.Name}");

var sp = (SanPham)Activator.CreateInstance(kieu)!;        // tạo đối tượng từ Type
kieu.GetProperty("Ten")!.SetValue(sp, "Chuot");           // gán bằng tên
kieu.GetMethod("MoTa")!.Invoke(sp, null);                  // gọi bằng tên
```

Các API chính (`System.Reflection`): `Type`, `PropertyInfo`, `MethodInfo`, `FieldInfo`, `ConstructorInfo`, `Assembly`.
`BindingFlags` lọc thành viên (`Public | NonPublic | Instance | Static | DeclaredOnly`).

### Ai dùng reflection?

Bạn hiếm khi viết reflection trực tiếp, nhưng **mọi framework lớn đều dựa vào nó**:

- ASP.NET Core: tìm controller, gắn giá trị request vào tham số (model binding).
- JSON serializer: liệt kê property để chuyển đổi.
- EF Core: ánh xạ class ↔ bảng.
- DI container, xUnit/NUnit (tìm `[Fact]`, `[Test]`), AutoMapper...

### Cái giá của reflection

- **Chậm hơn** gọi trực tiếp (hàng chục–trăm lần) — tránh trong vòng lặp nóng; nếu cần, cache `PropertyInfo`/`MethodInfo`,
  hoặc dùng `Expression`/delegate biên dịch sẵn, hoặc **source generator** (cách .NET hiện đại thay thế reflection).
- **Mất an toàn kiểu lúc biên dịch**: gõ sai tên chuỗi `"Ten"` chỉ lỗi lúc chạy → dùng `nameof(SanPham.Ten)`.
- Có thể truy cập cả thành viên `private` → phá vỡ đóng gói; và không tương thích tốt với **trimming/AOT** (biên dịch
  ahead-of-time) vì công cụ cắt code không biết bạn sẽ gọi gì.

Quy tắc: **có lựa chọn thì đừng dùng reflection**; dùng khi thật sự cần tính linh hoạt kiểu plugin/framework.

## Attribute — metadata gắn vào code

**Attribute** là "nhãn" thông tin đặt trong `[ ]` trước class, phương thức, property... Bản thân nó không làm gì; **có ai đó
(trình biên dịch, framework, code của bạn) đọc nó** rồi hành động.

Attribute có sẵn bạn đã gặp: `[Flags]` (enum), `[Obsolete("...")]` (cảnh báo dùng API cũ), `[JsonPropertyName]`, `[JsonIgnore]`,
`[Required]`, `[HttpGet]`, `[Fact]`, `[Serializable]`.

### Tự định nghĩa attribute

Là class kế thừa `Attribute`, tên thường kết thúc bằng `Attribute` (khi dùng có thể bỏ hậu tố):

```csharp
[AttributeUsage(AttributeTargets.Property)]      // chỉ đặt được trên property
class BatBuocAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
class KhoangAttribute(double min, double max) : Attribute
{
    public double Min { get; } = min;
    public double Max { get; } = max;
}

class SanPham
{
    [BatBuoc] public string Ten { get; set; } = "";
    [Khoang(0, 1000)] public int TonKho { get; set; }
}
```

### Đọc attribute bằng reflection — mini validator

```csharp
static List<string> KiemTra(object o)
{
    var loi = new List<string>();
    foreach (var pr in o.GetType().GetProperties())
    {
        var giaTri = pr.GetValue(o);
        if (pr.GetCustomAttribute<BatBuocAttribute>() is not null && (giaTri is null || giaTri is string { Length: 0 }))
            loi.Add($"{pr.Name} la bat buoc");
        if (pr.GetCustomAttribute<KhoangAttribute>() is { } k && giaTri is IConvertible c)
        {
            double v = c.ToDouble(null);
            if (v < k.Min || v > k.Max) loi.Add($"{pr.Name} phai trong [{k.Min}, {k.Max}]");
        }
    }
    return loi;
}
```

Đây chính xác là cách `[Required]`, `[Range]` của **Data Annotations** hoạt động trong ASP.NET Core: framework quét property,
đọc attribute, kiểm tra giá trị. Bạn được dùng validator có sẵn thay vì tự viết (Tập 2), nhưng nay bạn hiểu bên trong.

## Mẫu plugin: tìm kiểu cài đặt một interface

```csharp
var cacPlugin = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
    .Select(t => (IPlugin)Activator.CreateInstance(t)!);
```

Thêm class `PluginC : IPlugin` là tự động được nhận ra, không sửa dòng nào — cơ chế "quét và đăng ký" mà các DI container
hay dùng. (Với thư viện bên ngoài: `Assembly.LoadFrom(path)`.)

## Serialization — tổng quan

**Serialization** = chuyển **đối tượng trong bộ nhớ** thành dạng có thể **lưu hoặc gửi đi** (chuỗi, byte), và **deserialization** là
chiều ngược lại. Bạn đã làm với JSON (Chương 36).

| Định dạng | Ưu | Nhược |
|-----------|-----|-------|
| **JSON** (`System.Text.Json`) | đọc được bằng mắt, chuẩn web, đa ngôn ngữ | kích thước lớn hơn nhị phân |
| **XML** (`XmlSerializer`) | hệ thống cũ, có schema | dài dòng |
| **Protobuf, MessagePack** | nhỏ, nhanh, có schema | không đọc được bằng mắt, cần thư viện |
| **`BinaryFormatter`** | — | **ĐÃ BỊ LOẠI BỎ — nguy hiểm, tuyệt đối không dùng** |

**Cảnh báo bảo mật:** `BinaryFormatter` (và vài serializer nhị phân "ma thuật" tương tự) cho phép kẻ tấn công **thực thi mã tuỳ ý**
khi deserialize dữ liệu độc hại; nó đã bị loại khỏi .NET hiện đại. Nguyên tắc chung: **không bao giờ deserialize dữ liệu không tin cậy** bằng cơ chế
có thể tạo kiểu tuỳ ý. Dùng JSON với kiểu đích cố định (`Deserialize<SinhVien>`), tránh `object`/kiểu động và cấu hình cho phép
kiểu đa hình tự do.

Khi thiết kế dữ liệu để serialize: dùng DTO/`record` đơn giản, tránh tham chiếu vòng, không đưa thông tin nhạy cảm, và nghĩ đến
**phiên bản hoá** (thêm trường mới không được làm hỏng người đọc cũ).

## Lỗi thường gặp

- Gõ sai tên thuộc tính trong `GetProperty("Tên")` → trả về `null` → `NullReferenceException` (dùng `nameof`).
- `GetProperties()` không trả thành viên `private`/`static` (cần `BindingFlags`).
- Dùng reflection trong vòng lặp lớn mà không cache.
- Quên `[AttributeUsage]` khiến attribute đặt sai chỗ mà không báo lỗi.
- Nghĩ rằng attribute tự chạy: nó chỉ là dữ liệu, cần có code đọc nó.
- Dùng `BinaryFormatter`, hoặc deserialize dữ liệu người lạ sang kiểu tự do.
- Quên rằng trimming/AOT có thể loại bỏ thành viên chỉ dùng qua reflection.

## Bài tập

1. Viết hàm `InThongTin(object o)` in tên và giá trị của mọi property public bằng reflection.
2. Thêm attribute `[DoDaiToiDa(50)]` vào validator và kiểm tra chuỗi.
3. Viết `[Ten("...")]` attribute cho enum và hàm đọc lấy tên hiển thị bằng reflection.
4. Đo thời gian gán 1 triệu lần một property bằng cách gọi trực tiếp và bằng `PropertyInfo.SetValue`; sau đó cache `PropertyInfo` và đo lại.
5. Tạo thêm `PluginC : IPlugin` và chứng minh nó tự xuất hiện trong danh sách mà không sửa chỗ quét.
