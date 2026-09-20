# Chương 14 — Docker và docker-compose

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **container** là gì, khác máy ảo ra sao, và vì sao "chạy được trên máy tôi" không còn là vấn đề.
- Viết **Dockerfile nhiều giai đoạn (multi-stage)** cho ứng dụng ASP.NET Core: nhỏ, cache tốt, chạy không bằng root.
- Cấu hình ứng dụng qua **biến môi trường**, dùng **volume** cho dữ liệu, dùng **docker-compose** để chạy nhiều dịch vụ.
- Biết các thực hành an toàn và các lỗi thường gặp.

Code: [`code/kho-clean/Dockerfile`](../../code/kho-clean/Dockerfile), [`docker-compose.yml`](../../code/kho-clean/docker-compose.yml), [`.dockerignore`](../../code/kho-clean/.dockerignore).

> **Lưu ý trung thực về kiểm chứng:** khi viết sách, Docker daemon trên máy tác giả không chạy nên **chưa build được image thật**. Những gì đã kiểm chứng: lệnh `dotnet publish -c Release` dùng đúng trong Dockerfile chạy thành công và bản publish khởi động ở môi trường Production (tự migrate CSDL SQLite, `/health` trả 200, API trả đúng lỗi 400). Nội dung Dockerfile/compose tuân theo tài liệu chính thức của Microsoft/Docker; nếu build ở máy bạn gặp khác biệt (phiên bản image, tên biến), hãy xem `docker build` cho thông báo cụ thể.

## Container là gì?

**Container** đóng gói ứng dụng **cùng mọi thứ nó cần** (runtime, thư viện, cấu hình mặc định) thành một **image** bất biến; chạy image ra **container**. Khác **máy ảo (VM)**: VM ảo hoá cả phần cứng và chạy hệ điều hành đầy đủ (nặng, khởi động phút); container **dùng chung nhân (kernel)** của máy chủ, chỉ cô lập tiến trình/mạng/hệ tệp bằng namespace và cgroup (nhẹ, khởi động giây).

| Khái niệm | Ý nghĩa |
|-----------|---------|
| **Image** | khuôn bất biến, gồm nhiều **lớp (layer)** chỉ đọc |
| **Container** | một thể hiện đang chạy của image (thêm lớp ghi mỏng, mất khi xoá container) |
| **Registry** | kho lưu image (Docker Hub, GitHub Container Registry, Azure Container Registry, ECR) |
| **Volume** | thư mục lưu **bền** ngoài vòng đời container |
| **Tag** | nhãn phiên bản (`kho-web:1.4.2`); `latest` chỉ là một tag quy ước, không phải "mới nhất" thật |

Lợi ích cho .NET: môi trường **giống nhau** ở dev/CI/production, triển khai bằng **thay image** (không cài đặt trên máy chủ), **mở rộng** bằng cách chạy nhiều bản, **quay lui** bằng cách chạy lại tag cũ. Hầu hết nền tảng đám mây (Chương 16) đều chạy container.

## Dockerfile nhiều giai đoạn

```dockerfile
# ===== Giai đoạn 1: BUILD (có SDK, nặng ~1 GB) =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Chép RIÊNG các file csproj trước -> lớp "restore" được cache; chỉ khi đổi package mới restore lại
COPY Kho.Domain/Kho.Domain.csproj Kho.Domain/
COPY Kho.Application/Kho.Application.csproj Kho.Application/
COPY Kho.Infrastructure/Kho.Infrastructure.csproj Kho.Infrastructure/
COPY Kho.Web/Kho.Web.csproj Kho.Web/
RUN dotnet restore Kho.Web/Kho.Web.csproj

COPY . .
RUN dotnet publish Kho.Web/Kho.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ===== Giai đoạn 2: RUNTIME (chỉ runtime ASP.NET, nhỏ) =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /data && chown $APP_UID /data
ENV ConnectionStrings__Kho="Data Source=/data/kho.db" ASPNETCORE_HTTP_PORTS=8080
VOLUME /data

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kho.Web.dll"]
```

Giải thích từng quyết định:

1. **Multi-stage**: SDK (compiler, công cụ) chỉ ở giai đoạn build; image cuối chỉ có **runtime** (`aspnet`), nhỏ hơn nhiều và **ít bề mặt tấn công** (không có compiler cho kẻ xâm nhập).
2. **Cache theo lớp**: Docker cache từng lệnh; nếu đầu vào không đổi, dùng lại. Chép `*.csproj` và `restore` **trước** khi chép mã nguồn: sửa mã (thường xuyên) không làm mất lớp restore (chậm, tải package). Đây là mẹo quan trọng nhất về tốc độ build.
3. **`--no-restore`, `UseAppHost=false`**: publish nhanh hơn, không cần file thực thi native vì dùng `dotnet Kho.Web.dll`.
4. **Không chạy bằng root**: image .NET 8+ có sẵn người dùng `app` (UID trong biến `$APP_UID`); `USER $APP_UID` để nếu ứng dụng bị chiếm quyền, kẻ tấn công không có root trong container. Cấp quyền ghi thư mục dữ liệu cho người dùng đó.
5. **Cổng 8080**: image .NET 8+ mặc định lắng nghe **8080** (không phải 80) vì cổng dưới 1024 cần đặc quyền. `ASPNETCORE_HTTP_PORTS` cấu hình cổng. TLS thường **kết thúc ở reverse proxy/ingress** phía trước, không phải trong container.
6. **`ENTRYPOINT` dạng mảng (exec form)**: tiến trình `dotnet` là PID 1 nhận tín hiệu `SIGTERM` trực tiếp → tắt êm (graceful shutdown, hoàn tất request đang chạy, dừng `BackgroundService` như outbox).

### `.dockerignore`

```
**/bin/
**/obj/
**/*.db
.git
```

Không đưa `bin/obj` (ghi đè kết quả build trong container), cơ sở dữ liệu cục bộ, `.git` vào **ngữ cảnh build** — nhỏ hơn, nhanh hơn, và không rò rỉ dữ liệu/bí mật vào image.

### Build và chạy

```bash
docker build -t kho-web:dev tap3/code/kho-clean
docker run --rm -p 8080:8080 -v kho-data:/data kho-web:dev
curl http://localhost:8080/health
```

Ngoài Dockerfile, .NET SDK còn có thể tạo image **không cần Dockerfile**: `dotnet publish -t:PublishContainer` (gói `Microsoft.NET.Build.Containers`, cấu hình trong csproj). Tiện cho CI; Dockerfile linh hoạt hơn khi cần tuỳ biến.

## Cấu hình bằng biến môi trường

Nguyên tắc **12-factor**: cấu hình theo môi trường **nằm ngoài image**. ASP.NET Core map biến môi trường tới cấu hình: dấu `:` thành `__` (hai gạch dưới):

| Biến | Tương ứng `appsettings` |
|------|-------------------------|
| `ConnectionStrings__Kho` | `ConnectionStrings:Kho` |
| `Outbox__Bat` | `Outbox:Bat` |
| `Logging__LogLevel__Default` | `Logging:LogLevel:Default` |
| `ASPNETCORE_ENVIRONMENT` | môi trường (Development/Production) |

**Một image cho mọi môi trường**: build một lần, khác biệt chỉ nằm ở biến môi trường/secret khi chạy. **Không nhúng bí mật vào image** (`ENV PASSWORD=...` nằm mãi trong lịch sử layer); truyền lúc chạy qua secret của nền tảng (Chương 16).

## Dữ liệu: container không lưu trạng thái

Container là **tạm thời**: xoá đi là mất lớp ghi. Dữ liệu phải ở **volume** (như `kho-data:/data` cho SQLite) hoặc — chuẩn hơn ở production — **dịch vụ CSDL ngoài** (PostgreSQL/SQL Server, thường là dịch vụ quản lý). SQLite trong volume phù hợp demo và ứng dụng nhỏ **một instance**; khi chạy nhiều bản (mở rộng ngang) bắt buộc chuyển sang CSDL máy chủ.

## docker-compose

**Compose** khai báo cả hệ thống nhiều container trong một file YAML:

```yaml
services:
  kho-web:
    build: .
    ports: ["8080:8080"]
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Kho: "Data Source=/data/kho.db"
    volumes: [kho-data:/data]
    restart: unless-stopped
volumes:
  kho-data:
```

`docker compose up --build` dựng và chạy; `docker compose logs -f kho-web`; `docker compose down` (thêm `-v` để xoá cả volume — cẩn thận, mất dữ liệu).

### Thêm PostgreSQL (mẫu tham khảo)

Khi chuyển sang PostgreSQL: đổi provider EF (`Npgsql.EntityFrameworkCore.PostgreSQL`), tạo lại migration, và compose có thêm dịch vụ CSDL:

```yaml
services:
  kho-web:
    build: .
    ports: ["8080:8080"]
    environment:
      ConnectionStrings__Kho: "Host=csdl;Database=kho;Username=kho;Password=${KHO_DB_PASSWORD}"
    depends_on:
      csdl:
        condition: service_healthy        # chờ CSDL SẴN SÀNG, không chỉ "đã khởi động"
  csdl:
    image: postgres:17
    environment:
      POSTGRES_DB: kho
      POSTGRES_USER: kho
      POSTGRES_PASSWORD: ${KHO_DB_PASSWORD}
    volumes: [pg-data:/var/lib/postgresql/data]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U kho -d kho"]
      interval: 5s
      retries: 10
volumes:
  pg-data:
```

- Các dịch vụ trong cùng compose gọi nhau **bằng tên dịch vụ** (`Host=csdl`) qua mạng riêng.
- Mật khẩu lấy từ biến `${KHO_DB_PASSWORD}` (file `.env` **không commit**, hoặc `docker compose --env-file`).
- Ứng dụng nên **chịu được CSDL chưa sẵn sàng** lúc khởi động (retry), đừng dựa hoàn toàn vào `depends_on`. Tự `MigrateAsync` khi khởi động chỉ hợp với một instance; khi nhiều bản, chạy migration bằng **job riêng** (hoặc `dotnet ef migrations bundle`) trước khi triển khai.

(Đây là mẫu theo tài liệu, **chưa chạy ở máy tác giả**; provider PostgreSQL cũng cần đổi vài chỗ SQL đặc thù SQLite như `INSERT OR IGNORE` ở Chương 13 → `ON CONFLICT DO NOTHING`.)

### Health check trong container

`/health` của ứng dụng (Chương 11) là nền tảng để orchestrator biết sống/sẵn sàng. Image `aspnet` mặc định **không có `curl`** (và bản *chiseled* không có cả shell), nên khai báo `HEALTHCHECK` trong Dockerfile khó; trên Kubernetes/Container Apps dùng **probe HTTP** do nền tảng gọi (không cần công cụ trong container), và trên compose có thể kiểm tra từ dịch vụ khác hoặc dùng image có `curl`.

## Thực hành tốt và an toàn

- **Ghim phiên bản**: `dotnet/aspnet:10.0` (hoặc ghim tới patch/digest cho tái lập); tránh `latest`.
- **Image nhỏ/an toàn hơn**: biến thể `-noble-chiseled` (Ubuntu chiseled: không shell, không package manager, chạy sẵn non-root) và `-alpine`; đánh đổi: khó debug (không có shell), musl libc.
- **Quét lỗ hổng** image (Trivy, Docker Scout, Defender) trong CI; cập nhật base image thường xuyên (vá bảo mật).
- **Không chạy root**, không mount `docker.sock`, không dùng `--privileged`.
- **Hệ tệp chỉ đọc** (`read_only: true` + volume ghi cho thư mục cần thiết) nếu có thể.
- **Giới hạn tài nguyên** (`mem_limit`, `cpus`); .NET tự nhận giới hạn bộ nhớ container để đặt heap GC.
- **Log ra stdout/stderr** (mặc định của ASP.NET Core console logger); nền tảng thu gom — đừng ghi file log trong container.
- **Một tiến trình chính mỗi container**; ứng dụng phải **tắt êm** khi nhận SIGTERM.
- Đừng nhét **secret** hay `appsettings.Production.json` chứa bí mật vào image.

## Lỗi thường gặp

- Chép toàn bộ mã trước `restore` → mỗi lần sửa một dòng lại tải lại toàn bộ package.
- Thiếu `.dockerignore` → ngữ cảnh build lớn, `bin/obj` cũ làm hỏng build.
- Ứng dụng nghe `localhost` trong container → không truy cập được từ ngoài (dùng `ASPNETCORE_HTTP_PORTS`/`URLS=http://+:8080`).
- Quên volume → **mất dữ liệu SQLite** khi tạo lại container; hoặc lỗi quyền ghi vì chạy non-root mà thư mục thuộc root.
- Dùng `latest` → build không tái lập được.
- Nhúng secret vào `ENV`/`ARG`/image; commit `.env`.
- Giả định `depends_on` đảm bảo CSDL sẵn sàng.
- Cổng 80 trong container non-root → lỗi quyền (dùng 8080).
- Chạy migration tự động ở mỗi bản trong cụm → đua nhau migrate.

## Bài tập

1. Build image của `kho-clean`, chạy, tạo vài sản phẩm bằng `curl`, **xoá container**, chạy lại cùng volume và xác nhận dữ liệu còn. Chạy lại **không** volume và xác nhận mất.
2. So sánh kích thước image khi dùng `sdk` làm image cuối với multi-stage; ghi lại số liệu bằng `docker images`.
3. Đo tác dụng cache: sửa một dòng mã rồi `docker build` lại; xem lớp `dotnet restore` có `CACHED` không. Sau đó đổi thứ tự (chép hết trước restore) và so sánh thời gian.
4. Tạo image không cần Dockerfile bằng `dotnet publish -t:PublishContainer` (kèm `ContainerRepository`, `ContainerFamily` trong csproj).
5. Chuyển `kho-clean` sang PostgreSQL theo mẫu compose ở trên, sửa `INSERT OR IGNORE`, tạo lại migration và chạy `docker compose up`.
6. Thêm dịch vụ **OpenTelemetry Collector** hoặc **Jaeger** vào compose và cấu hình `OTEL_EXPORTER_OTLP_ENDPOINT` cho ứng dụng (kết nối Chương 11).
