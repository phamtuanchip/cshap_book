# Chương 40 — Git, NuGet và cấu hình

## Mục tiêu học

Sau chương này, bạn sẽ:

- Dùng **Git** cho quy trình làm việc hằng ngày: commit, branch, merge, `.gitignore`.
- Quản lý gói **NuGet** và hiểu `PackageReference`, phiên bản, `dotnet restore`.
- Tách **cấu hình** khỏi code: `appsettings.json`, biến môi trường, **user secrets**, **options pattern**.
- Không bao giờ để **bí mật** (mật khẩu, khoá API) lọt vào Git.

Code mẫu: [`code/ch40-git-nuget-cau-hinh/`](../../code/ch40-git-nuget-cau-hinh/).

## Git — quản lý phiên bản

**Git** ghi lại lịch sử thay đổi của dự án, cho phép quay lại, thử nghiệm song song (branch) và cộng tác. **GitHub/GitLab/
Azure DevOps** lưu repository trên mạng.

### Lệnh hằng ngày

```
git clone <url>                # lấy repo về
git status                     # xem thay đổi
git add <file>  |  git add .   # đưa vào "staging"
git commit -m "Mo ta ngan"     # chốt thành một commit
git log --oneline              # xem lịch sử
git diff                       # xem chi tiết chưa commit
git pull                       # lấy thay đổi mới từ server (fetch + merge)
git push                       # đẩy commit lên server
```

Ba "vùng": **working directory** (file đang sửa) → **staging** (`add`) → **repository** (`commit`). Commit là ảnh chụp toàn bộ
trạng thái tại một thời điểm.

### Branch (nhánh)

Làm tính năng mới trên nhánh riêng để không đụng `main`:

```
git switch -c tinh-nang-gio-hang     # tạo và chuyển sang nhánh mới
# ... sửa, commit ...
git switch main
git merge tinh-nang-gio-hang         # gộp vào main
```

Quy trình phổ biến (**GitHub Flow**): mọi công việc trên nhánh ngắn hạn → mở **Pull Request** → review → CI chạy test → merge vào
`main`. Xung đột khi merge (hai người sửa cùng dòng) phải giải quyết bằng tay: Git đánh dấu `<<<<<<<`/`=======`/`>>>>>>>`,
bạn chọn nội dung đúng, `git add`, rồi commit.

### Viết commit tốt

- Mỗi commit **một thay đổi logic**, build được, test đạt.
- Thông điệp: dòng đầu ngắn (≤ 72 ký tự), thể hiện *cái gì và vì sao*; ví dụ `Them kiem tra so luong am cho GioHang`.
- Commit **thường xuyên**, đừng gom cả tuần vào một commit.

### `.gitignore` — không đưa gì vào Git

File `.gitignore` liệt kê thứ **không** theo dõi. Kho này đã có sẵn mẫu Visual Studio. Chủ yếu loại: `bin/`, `obj/`,
`.vs/`, `*.user`, thư mục gói tải về, file log/tạm. Tạo mẫu cho dự án .NET: `dotnet new gitignore`.

**Bài học đắt giá:** thứ đã commit (dù xoá ở commit sau) **vẫn nằm trong lịch sử**. Bí mật lỡ commit phải coi là **đã lộ** — đổi
ngay (rotate) chứ không chỉ xoá file.

## NuGet — quản lý gói thư viện

Bạn đã dùng NuGet ở Chương 4. Nhắc lại và mở rộng:

```
dotnet add package Serilog                   # thêm bản mới nhất ổn định
dotnet add package Serilog --version 4.2.0   # chỉ định phiên bản
dotnet list package                          # đã dùng gói nào
dotnet list package --outdated               # gói nào có bản mới
dotnet list package --vulnerable             # gói có lỗ hổng bảo mật đã biết
dotnet remove package Serilog
dotnet restore                               # tải gói theo .csproj (build/run tự gọi)
```

Trong `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
</ItemGroup>
```

- **Phiên bản (SemVer)** `Major.Minor.Patch`: tăng *Major* = có thể phá vỡ tương thích; *Minor* = thêm tính năng tương thích;
  *Patch* = sửa lỗi. Đọc *release notes* trước khi nâng Major.
- Gói kéo theo **phụ thuộc bắc cầu** (transitive): thêm một gói có thể tải hàng chục gói khác. Kiểm tra gói lỗ hổng định kỳ.
- **Central Package Management** (`Directory.Packages.props`): ghi phiên bản một chỗ cho cả solution nhiều project.
- Chọn thư viện có uy tín: nhiều lượt tải, cập nhật gần đây, giấy phép rõ ràng, mã nguồn mở. Mỗi gói là **rủi ro chuỗi cung ứng**.
- Bạn cũng có thể đóng gói thư viện của mình (`dotnet pack`) và đẩy lên NuGet (công khai hoặc nguồn riêng của công ty).

## Cấu hình ứng dụng

Nguyên tắc (*Twelve-Factor App*): **cấu hình tách khỏi code**. Chuỗi kết nối CSDL, URL dịch vụ, cờ bật/tắt tính năng, mức log…
thay đổi giữa máy dev / test / production **mà không phải sửa code hay build lại**.

`Microsoft.Extensions.Configuration` hợp nhất nhiều **nguồn** cấu hình thành một cây khoá–giá trị:

```csharp
IConfiguration cauHinh = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{moiTruong}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();
```

**Nguồn thêm sau ghi đè nguồn thêm trước** — thứ tự ở trên nghĩa là biến môi trường có ưu tiên cao nhất, rồi user secrets, rồi file
theo môi trường, cuối cùng `appsettings.json` (giá trị mặc định). (Với `WebApplication.CreateBuilder` của ASP.NET Core, việc này đã
được cấu hình sẵn — Tập 2.)

### `appsettings.json` và theo môi trường

```json
{
  "CuaHang": { "Ten": "Sach Xanh", "ThueVat": 0.08, "KenhHoTro": [ "email", "dien thoai" ] },
  "KetNoi": { "MayChu": "localhost", "MatKhau": "" }
}
```

`appsettings.Development.json` chỉ chứa phần **khác biệt** ở môi trường dev. Môi trường lấy từ biến `DOTNET_ENVIRONMENT`
(hoặc `ASPNETCORE_ENVIRONMENT` với web): `Development`, `Staging`, `Production`. (Trong csproj cần đặt file `CopyToOutputDirectory`
để file JSON nằm cạnh file chạy.)

### Đọc cấu hình

```csharp
cauHinh["CuaHang:Ten"]                          // khoá lồng nhau dùng dấu ':'
cauHinh.GetValue<int>("CuaHang:SoNgayDoiTra", 7) // có kiểu, kèm giá trị mặc định
var cuaHang = cauHinh.GetSection("CuaHang").Get<CuaHangOptions>();   // BIND vào class có kiểu (khuyến nghị)
```

**Options pattern**: gom cấu hình liên quan vào một class đơn giản (`CuaHangOptions`) và truyền vào nơi cần qua
`IOptions<CuaHangOptions>`. Code nghiệp vụ không đọc chuỗi khoá rải rác — an toàn kiểu, dễ test, dễ kiểm tra tính hợp lệ
(`ValidateDataAnnotations`, `ValidateOnStart` — Tập 2).

### Biến môi trường

Ghi đè bất kỳ khoá nào bằng biến môi trường, dùng `__` (hai gạch dưới) thay cho `:`:

```
CuaHang__Ten="Sach Do"            # tương ứng CuaHang:Ten
```

Đây là cách chuẩn để cấu hình khi chạy trên container/Docker/cloud (Tập 3) mà không đụng file.

### Bí mật: `user-secrets` và kho bí mật

**Không bao giờ** đặt mật khẩu, khoá API, chuỗi kết nối chứa mật khẩu trong `appsettings.json` rồi commit.

- **Lúc phát triển**: **User Secrets** — lưu ngoài thư mục dự án (trong thư mục hồ sơ người dùng), không bị đưa vào Git:

  ```
  dotnet user-secrets init
  dotnet user-secrets set "KetNoi:MatKhau" "bi-mat-cua-toi"
  dotnet user-secrets list
  ```

- **Production**: biến môi trường do nền tảng cấp, hoặc kho bí mật chuyên dụng (**Azure Key Vault**, AWS Secrets Manager,
  HashiCorp Vault) qua provider cấu hình tương ứng.
- **User Secrets không mã hoá**; chỉ để tiện dev, không phải giải pháp production.

## Checklist trước khi push

- [ ] Không có mật khẩu/khoá API/token trong code hay file cấu hình được commit (tìm `password`, `secret`, `apikey`).
- [ ] `.gitignore` đã loại `bin/`, `obj/`, `.vs/`, file bí mật cục bộ.
- [ ] Build và test đạt (`dotnet build`, `dotnet test`).
- [ ] Commit nhỏ, thông điệp rõ ràng.

## Lỗi thường gặp

- Commit `appsettings.json` chứa mật khẩu thật; commit `bin/`, `obj/`.
- `FileNotFoundException: appsettings.json` — quên `CopyToOutputDirectory` hoặc đặt sai `SetBasePath`.
- Cấu hình đọc ra `null`/mặc định vì gõ sai khoá hoặc sai cấp lồng nhau (không có lỗi báo!). Bind vào class và kiểm tra bắt buộc.
- Hiểu sai thứ tự ưu tiên nguồn cấu hình (nguồn sau thắng).
- Sai dấu phân cách biến môi trường: dùng `__`, không phải `:` (một số shell không cho `:`).
- Không `git pull` trước khi `git push` → bị từ chối (non-fast-forward).
- Dùng `git add .` mà không xem `git status` → đưa cả file rác/bí mật vào commit.
- Dùng `git push --force` lên nhánh chung (ghi đè lịch sử của người khác).

## Bài tập

1. Tạo repo mới, commit `.gitignore`, một dự án console, rồi tạo nhánh `feature/x`, sửa, merge về `main`.
2. Tạo cố ý một **xung đột merge** (hai nhánh sửa cùng một dòng) và giải quyết nó.
3. Thêm `Serilog` vào project bằng CLI; dùng `dotnet list package --outdated` và `--vulnerable`.
4. Ghi đè `CuaHang:Ten` bằng biến môi trường và bằng `user-secrets`; quan sát ai thắng khi cả hai cùng có.
5. Thêm class `KetNoiOptions` và bind mục `KetNoi`; ném lỗi khi `MayChu` rỗng ngay lúc khởi động.
