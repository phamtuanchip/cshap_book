# Chương 39 — Debug và logging

## Mục tiêu học

Sau chương này, bạn sẽ:

- Dùng debugger hiệu quả: breakpoint có điều kiện, Watch, Call Stack, Step, Exception settings.
- Ghi **log** đúng cách với `ILogger`: cấp độ, structured logging, exception, scope.
- Biết log cái gì và **không** log cái gì.
- Có quy trình hệ thống để tìm và sửa lỗi.

Code mẫu: [`code/ch39-debug-logging/`](../../code/ch39-debug-logging/).

## Quy trình tìm lỗi

Đừng đoán mò. Làm theo trình tự:

1. **Tái hiện** lỗi ổn định (các bước cụ thể, dữ liệu cụ thể).
2. **Thu hẹp** phạm vi: lỗi nằm ở tầng nào, đầu vào nào?
3. **Đặt giả thuyết** rồi **kiểm chứng** bằng debugger/log/test — không sửa thử lung tung.
4. **Sửa nguyên nhân gốc**, không chỉ triệu chứng.
5. **Viết test** chứng minh lỗi đã sửa và không quay lại (Chương 38).

Đọc kỹ **thông báo lỗi và stack trace**: dòng trên cùng cho biết *chỗ nổ*, các dòng dưới cho biết *ai gọi đến đó*. Tìm dòng
đầu tiên thuộc **code của bạn** — thường là nguồn gốc.

## Debugger

Chạy ở chế độ **Debug** (`F5`, cấu hình Debug — khác Release: Release tối ưu code nên biến bị "mất", thứ tự chạy khó theo dõi).

### Breakpoint

- **Breakpoint** (`F9`): dừng trước khi chạy dòng đó.
- **Conditional breakpoint**: chuột phải vào chấm đỏ → *Conditions*, ví dụ `i == 500` hay `ten == "An"`; chỉ dừng khi đúng.
  Vô giá khi lỗi chỉ xảy ra ở vòng lặp thứ 500.
- **Hit count**: dừng ở lần thứ *n*. **Tracepoint / Logpoint**: in thông tin ra cửa sổ Output **mà không dừng** và không sửa code.
- **Exception breakpoint**: dừng ngay khi ném một loại exception (Debug → *Windows → Exception Settings*), kể cả khi nó được `catch`.

### Điều khiển chạy

| Thao tác | Phím (VS / VS Code) | Ý nghĩa |
|----------|--------------------|---------|
| Continue | `F5` | chạy tới breakpoint kế |
| Step Over | `F10` | chạy hết dòng, không vào hàm |
| Step Into | `F11` | đi vào hàm được gọi |
| Step Out | `Shift+F11` | chạy hết hàm hiện tại, quay ra nơi gọi |
| Run to Cursor | `Ctrl+F10` | chạy tới dòng đang trỏ |

### Xem trạng thái

- **Locals / Autos**: biến cục bộ hiện tại. **Watch**: gõ biểu thức bạn quan tâm (`danhSach.Count`, `x * 2`).
- **Rê chuột** lên biến; ghim (pin) để theo dõi.
- **Call Stack**: chuỗi hàm dẫn tới đây — bấm vào từng khung để xem biến ở tầng đó.
- **Immediate/Debug Console**: chạy thử biểu thức, gọi hàm, **đổi giá trị biến** ngay lúc dừng.
- **Edit and Continue** (Visual Studio): sửa code khi đang dừng rồi chạy tiếp mà không khởi động lại.
- **Debug.WriteLine**/**`Debug.Assert`**: chỉ hoạt động ở bản Debug; bị loại khi build Release.

### Gỡ lỗi ứng dụng web/đang chạy

*Attach to Process* gắn debugger vào tiến trình đang chạy. Với ASP.NET Core, chạy dự án bằng `F5` là debug luôn; lỗi trên server
thật thì **log** là công cụ chính (bạn không thể đặt breakpoint trên production).

## Vì sao cần logging?

Debugger chỉ dùng được khi *bạn ngồi trước máy đang chạy*. Khi ứng dụng chạy trên server lúc 3 giờ sáng, **log** là "hộp đen"
duy nhất ghi lại chuyện gì đã xảy ra.

## `ILogger` — logging chuẩn của .NET

Thư viện: `Microsoft.Extensions.Logging` (là chuẩn; ASP.NET Core dùng sẵn, nhận `ILogger<T>` qua dependency injection).

```csharp
using var factory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole());
ILogger<Program> log = factory.CreateLogger<Program>();

log.LogInformation("Nguoi dung {NguoiDung} them {SoLuong} san pham", nguoiDung, soLuong);
```

Trong lớp nghiệp vụ, nhận `ILogger<T>` qua constructor (Tập 2 sẽ dùng DI tự cấp):

```csharp
class DichVuTinhToan(ILogger<DichVuTinhToan> log) { ... }
```

### Các cấp độ (LogLevel)

| Cấp | Dùng cho |
|-----|---------|
| `Trace` | cực kỳ chi tiết, có thể chứa dữ liệu nhạy cảm (chỉ dev) |
| `Debug` | thông tin cho lập trình viên khi điều tra |
| `Information` | luồng bình thường của ứng dụng (đã đăng nhập, đã đặt hàng) |
| `Warning` | bất thường nhưng vẫn chạy được (dữ liệu thiếu, dùng giá trị mặc định, sắp hết dung lượng) |
| `Error` | một thao tác **thất bại** (exception đã bắt) |
| `Critical` | lỗi nghiêm trọng, ứng dụng/hệ thống có nguy cơ sập |

Mức tối thiểu cấu hình theo môi trường (`Debug` khi dev, `Information`/`Warning` khi production) — **không đổi code** mà đổi cấu hình
(`appsettings.json`, Chương 40; chương trình mẫu đọc biến môi trường `LOG_LEVEL`).

### Structured logging — dùng tham số đặt tên, không nội suy chuỗi

```csharp
log.LogInformation("Nguoi dung {NguoiDung} them {SoLuong} san pham", nguoiDung, soLuong);   // ĐÚNG
log.LogInformation($"Nguoi dung {nguoiDung} them {soLuong} san pham");                       // SAI
```

Bản đúng giữ lại **mẫu thông điệp** và **các tham số riêng biệt** → công cụ (Seq, Elasticsearch, Application Insights) lọc/tìm theo
`NguoiDung = "an"` được, và tiết kiệm bộ nhớ khi mức log bị tắt (không phải dựng chuỗi). Bản nội suy chỉ ra một chuỗi trơn.

### Ghi exception

```csharp
catch (DivideByZeroException e)
{
    log.LogError(e, "Khong the chia {A} cho {B}", a, b);    // truyền exception làm tham số ĐẦU
    throw;
}
```

Truyền cả `Exception` để log giữ đầy đủ stack trace. **Đừng vừa log vừa `throw` ở mọi tầng** — cùng một lỗi bị log nhiều lần
gây nhiễu. Quy tắc: log **một lần** ở nơi cuối cùng xử lý lỗi (ví dụ middleware toàn cục của web), hoặc log ở nơi có thêm ngữ
cảnh hữu ích rồi ném lại.

### Scope — gắn ngữ cảnh chung

```csharp
using (log.BeginScope("RequestId={RequestId}", requestId))
{
    log.LogInformation("Bat dau xu ly");   // mọi dòng log trong khối đều kèm RequestId
    ...
}
```

Rất hữu ích để nối các dòng log của cùng một request (correlation id).

### Đích ghi log (provider) và Serilog

Mặc định .NET có provider Console, Debug, EventLog. Cho ứng dụng thật, nhiều người dùng **Serilog** (hoặc NLog) — cắm vào
`ILogger` nên code của bạn không đổi — để ghi ra file (có xoay vòng), Seq, Elasticsearch, Application Insights:

```
dotnet add package Serilog.AspNetCore
```

Ở Tập 2 (ASP.NET Core) ta sẽ cấu hình Serilog và OpenTelemetry (Tập 3).

## Log cái gì, không log cái gì?

**Nên log:** khởi động/tắt ứng dụng, các sự kiện nghiệp vụ quan trọng, lỗi và cảnh báo (kèm ID/ngữ cảnh: mã đơn hàng, user id),
thời gian xử lý các thao tác chậm, lời gọi dịch vụ ngoài và kết quả.

**TUYỆT ĐỐI KHÔNG log:** mật khẩu, token, khoá API, số thẻ, dữ liệu cá nhân nhạy cảm (số CMND, y tế...), toàn bộ body request chứa dữ
liệu như vậy. Log thường được lưu lâu và nhiều người truy cập được → rò rỉ. Che/băm dữ liệu nhạy cảm (`sdt: 09****111`).

Log **đúng lượng**: quá ít không đủ điều tra, quá nhiều (log trong vòng lặp nóng) làm chậm và tốn dung lượng. Mỗi dòng log
nên giúp trả lời được: *cái gì xảy ra, với đối tượng nào, kết quả ra sao*.

## Lỗi thường gặp

- Debug ở cấu hình Release rồi thắc mắc vì sao breakpoint không dừng/biến "optimized away".
- Nội suy chuỗi trong `Log...` thay vì tham số đặt tên.
- Log mật khẩu/token.
- `catch (Exception e) { log.LogError(e.Message); }` — mất stack trace; truyền `e` làm tham số đầu.
- Log lỗi ở mọi tầng → một sự cố ra hàng chục dòng.
- Đặt tất cả ở `Information`, không phân cấp → không lọc được; hoặc để `Debug/Trace` trên production.
- Dùng `Console.WriteLine` thay `ILogger` trong ứng dụng thật (không có cấp độ, không cấu hình được).
- Sửa lỗi bằng cách "thử đại" thay vì tái hiện và xác định nguyên nhân.

## Bài tập

1. Chạy chương trình mẫu với `LOG_LEVEL=Debug` rồi `Warning`; quan sát dòng nào biến mất.
2. Đặt breakpoint có điều kiện trong `TinhTong` chỉ dừng khi `x == 2`; dùng Watch theo dõi `tong`.
3. Thêm log vào `TaiKhoan.RutTien` (Chương 14): `Information` khi thành công, `Warning` khi không đủ tiền — có nên log số dư?
4. Cố tình gây `NullReferenceException`, đọc stack trace và tìm dòng gốc bằng Call Stack.
5. Dùng Logpoint (không sửa code) để in giá trị một biến trong vòng lặp.
