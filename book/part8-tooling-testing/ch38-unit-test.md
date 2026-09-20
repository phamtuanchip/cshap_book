# Chương 38 — Unit test với xUnit

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **unit test** để làm gì và các nguyên tắc của một test tốt.
- Viết test bằng **xUnit**: `[Fact]`, `[Theory]`, `Assert`, mẫu **Arrange–Act–Assert**.
- Dùng **đối tượng giả (mock/stub)** với NSubstitute để cô lập code cần test.
- Chạy test bằng `dotnet test` / IDE và đọc kết quả.

Code mẫu: [`code/ch38-unit-test/`](../../code/ch38-unit-test/) (thư viện `ShopLib` + project `ShopLib.Tests`).

## Vì sao phải test?

Phần mềm thay đổi liên tục. Mỗi lần sửa, làm sao biết bạn không làm hỏng thứ đang chạy tốt? Bấm thử bằng tay thì chậm, dễ
sót, không lặp lại được. **Test tự động** là chương trình kiểm tra chương trình khác — chạy trong vài giây, mỗi lần commit.

- **Phát hiện lỗi sớm**, rẻ hơn nhiều so với lỗi lên production.
- **Tự tin sửa/tái cấu trúc code** (Chương 42): test xanh nghĩa là hành vi vẫn đúng.
- **Tài liệu sống**: tên test mô tả code phải làm gì.
- Ép bạn viết code **dễ kiểm thử** — thường cũng là code thiết kế tốt (tách phụ thuộc bằng interface, Chương 18).

Các loại test (kim tự tháp): **unit test** (nhỏ, nhanh, nhiều nhất — một lớp/hàm) → **integration test** (nhiều phần ghép
nhau, có CSDL/HTTP) → **end-to-end/UI test** (ít, chậm). Chương này tập trung unit test.

## Tạo project test

```
dotnet new classlib -n ShopLib
dotnet new xunit -n ShopLib.Tests
dotnet add ShopLib.Tests reference ShopLib
dotnet sln add ShopLib ShopLib.Tests
```

Các thư viện thường dùng: **xUnit** (khung test, phổ biến nhất trong .NET; NUnit và MSTest là lựa chọn khác với ý tưởng
tương tự), **NSubstitute** hoặc **Moq** (đối tượng giả), **FluentAssertions/Shouldly** (assert dễ đọc, tuỳ chọn). Quy ước:
project test đặt tên `<Project>.Tests`, class test `<Lớp>Tests`.

Chạy: `dotnet test` (hoặc **Test Explorer** trong IDE). Kết quả liệt kê test đạt/hỏng và thông báo lỗi chi tiết.

## Test đầu tiên: `[Fact]`

```csharp
public class GioHangTests
{
    [Fact]
    public void TongTien_GioHangMoi_BangKhong()
    {
        var gio = new GioHang();

        Assert.Equal(0m, gio.TongTien);
    }
}
```

- `[Fact]` đánh dấu một test luôn đúng. Test là một phương thức `public`, không tham số.
- Test **đạt** nếu không ném ngoại lệ; `Assert.X` ném ngoại lệ khi sai.

### Mẫu Arrange – Act – Assert (AAA)

```csharp
[Fact]
public void Them_HaiSanPhamKhacNhau_TinhDungTongTien()
{
    // Arrange: chuẩn bị dữ liệu và đối tượng
    var gio = new GioHang();

    // Act: thực hiện đúng MỘT hành động cần kiểm tra
    gio.Them("A", "Chuot", 150_000m, 2);
    gio.Them("B", "Ban phim", 500_000m);

    // Assert: kiểm tra kết quả
    Assert.Equal(800_000m, gio.TongTien);
    Assert.Equal(2, gio.Dong.Count);
}
```

**Đặt tên test** nói rõ *cái gì – trong tình huống nào – kết quả mong đợi*:
`TenPhuongThuc_TinhHuong_KetQuaMongDoi`. Khi test đỏ, chỉ đọc tên đã biết hỏng ở đâu.

### Các `Assert` thường dùng

```csharp
Assert.Equal(mongDoi, thucTe);      Assert.NotEqual(...)
Assert.True(dk);                    Assert.False(dk);
Assert.Null(x);                     Assert.NotNull(x);
Assert.Empty(ds);                   Assert.Single(ds);       Assert.Contains("abc", chuoi);
Assert.Throws<ArgumentException>(() => gio.Them("", "x", 1m));   // kiểm tra NÉM lỗi đúng kiểu
Assert.Equal(1.23, ketQua, precision: 2);                          // so sánh số thực có sai số
```

Thứ tự tham số: **`Assert.Equal(mongDoi, thucTe)`** (mong đợi trước). Đảo ngược làm thông báo lỗi gây hiểu nhầm.

## `[Theory]` — một test, nhiều bộ dữ liệu

```csharp
[Theory]
[InlineData(0, 1_000_000, 1_000_000)]
[InlineData(10, 1_000_000, 900_000)]
[InlineData(100, 1_000_000, 0)]
public void TinhTienSauGiamGia_PhanTramHopLe_TraVeDung(int phanTram, int gia, int mongDoi)
{
    var gio = new GioHang();
    gio.Them("A", "Laptop", gia);

    Assert.Equal((decimal)mongDoi, gio.TinhTienSauGiamGia(phanTram));
}
```

Mỗi `[InlineData]` sinh một test riêng. Với dữ liệu phức tạp dùng `[MemberData]`/`[ClassData]`. Nhớ kiểm tra cả **giá trị biên**
(0, 100, âm, 101) — nơi hay có lỗi nhất.

## Vòng đời và độc lập giữa các test

xUnit tạo **một đối tượng class test mới cho mỗi test**. Constructor là "setup", `IDisposable.Dispose` là "teardown":

```csharp
public class DonHangServiceTests : IDisposable
{
    public DonHangServiceTests() { /* chạy trước MỖI test */ }
    public void Dispose() { /* chạy sau MỖI test */ }
}
```

Test phải **độc lập**: không phụ thuộc thứ tự chạy, không dùng chung trạng thái thay đổi được (biến `static`, file, CSDL thật).
Test có thể chạy song song.

## Cô lập phụ thuộc: mock và stub

Lớp `DonHangService` cần kho hàng (CSDL) và gửi email (mạng). Unit test không nên chạm vào chúng: chậm, không ổn định, có
tác dụng phụ (gửi email thật!). Nhờ **lập trình theo interface** (Chương 18), ta thay bằng **đối tượng giả**:

```csharp
public class DonHangService(IKhoHang kho, IGuiEmail email) { ... }   // phụ thuộc vào interface

// Trong test:
private readonly IKhoHang _kho = Substitute.For<IKhoHang>();
private readonly IGuiEmail _email = Substitute.For<IGuiEmail>();

[Fact]
public void DatHang_DuHang_GiamTonVaGuiEmail()
{
    var gio = new GioHang();
    gio.Them("A", "Chuot", 100_000m, 2);
    _kho.SoLuongTon("A").Returns(10);           // STUB: dàn kết quả trả về

    var kq = _service.DatHang(gio, "khach@mail.com");

    Assert.True(kq.OK);
    _kho.Received(1).GiamTon("A", 2);           // MOCK: kiểm tra hàm ĐÃ được gọi đúng cách
    _email.Received(1).Gui("khach@mail.com", Arg.Any<string>(), Arg.Any<string>());
}

[Fact]
public void DatHang_KhongDuHang_KhongGiamTonKhongGuiEmail()
{
    ...
    _kho.DidNotReceive().GiamTon(Arg.Any<string>(), Arg.Any<int>());
}
```

- **Stub**: trả dữ liệu định sẵn để dẫn code vào nhánh cần test.
- **Mock**: ghi lại lời gọi để bạn kiểm tra "có gọi/không gọi" — dùng khi hành vi quan sát được là *tác dụng phụ* (gửi email).
- Đừng lạm dụng mock cho mọi thứ: test bám sát chi tiết cài đặt sẽ hỏng mỗi lần tái cấu trúc. Ưu tiên kiểm tra **kết quả** trước,
  chỉ verify lời gọi khi đó chính là hành vi cần bảo đảm.
- Đây là lý do **Dependency Injection** (nhận phụ thuộc qua constructor) quan trọng: nó cho phép thay thế phụ thuộc khi test.

## Nguyên tắc test tốt (FIRST)

- **F**ast — nhanh (mili-giây), để chạy liên tục.
- **I**ndependent — độc lập nhau.
- **R**epeatable — chạy lúc nào, ở đâu cũng cùng kết quả (không phụ thuộc giờ hiện tại, mạng, dữ liệu thật: bọc `DateTime.Now` sau
  interface `TimeProvider`).
- **S**elf-validating — tự báo đạt/hỏng, không cần người đọc kết quả.
- **T**imely — viết cùng lúc/trước code.

Một test nên kiểm tra **một hành vi** (nhiều `Assert` cho cùng một hành vi là được), không có `if`/vòng lặp/logic phức tạp,
và **đọc như tài liệu**. Test cũng là code: giữ sạch, tránh lặp.

**TDD** (Test-Driven Development): viết test *thất bại* trước → viết code tối thiểu cho test đạt → tái cấu trúc → lặp lại
(Red – Green – Refactor). Không bắt buộc, nhưng luyện tập vài lần giúp thiết kế tốt hơn.

## Độ phủ (code coverage)

`dotnet test --collect:"XPlat Code Coverage"` đo bao nhiêu dòng code được test chạy qua. Độ phủ **cao không có nghĩa là test tốt**
(test không có `Assert` vẫn tăng độ phủ), nhưng **độ phủ thấp chắc chắn là vùng rủi ro**. Đừng đuổi theo con số 100%; hãy phủ kỹ
nghiệp vụ quan trọng và các nhánh lỗi.

## Lỗi thường gặp

- Test phụ thuộc thứ tự chạy hoặc trạng thái chung → lúc đạt lúc hỏng ("flaky test").
- Test gọi CSDL/mạng/`DateTime.Now` thật.
- `Assert.Equal(thucTe, mongDoi)` đảo tham số.
- Một test làm quá nhiều việc; tên test mơ hồ (`Test1`).
- Bắt ngoại lệ bằng `try/catch` thay vì `Assert.Throws` (test luôn đạt nếu không ném lỗi).
- Mock quá nhiều → test gắn chặt vào cài đặt.
- Quên đặt `[Fact]`/`[Theory]` → test không bao giờ chạy (kiểm tra số test được phát hiện).
- So sánh số thực bằng `Assert.Equal(0.3, 0.1 + 0.2)` (dùng tham số `precision`).

## Bài tập

1. Viết test cho `GioHang.Xoa` khi sản phẩm không tồn tại, và test gộp số lượng khi thêm cùng sản phẩm 3 lần.
2. Dùng `[Theory]` với `[MemberData]` kiểm tra hàm kiểm tra năm nhuận cho 10 năm khác nhau.
3. Viết `DonHangService` test cho trường hợp `IKhoHang` ném ngoại lệ (`_kho.When(...).Do(_ => throw ...)`); kiểm tra không gửi email.
4. Làm theo TDD: viết test cho `TinhPhiVanChuyen(khoiLuong)` (miễn phí dưới 1kg, 20k đến 5kg, 50k trên đó) trước rồi mới viết hàm.
5. Đo độ phủ và tìm các dòng chưa được test trong `GioHang`; viết thêm test cho chúng.
