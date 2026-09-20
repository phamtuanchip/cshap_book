# Chương 15 — CI/CD với GitHub Actions

## Mục tiêu học

Sau chương này, bạn sẽ:

- Hiểu **CI (tích hợp liên tục)**, **CD (phân phối/triển khai liên tục)** và vì sao chúng giảm rủi ro thay vì tăng công sức.
- Viết workflow **GitHub Actions** build, test, quét và dựng image cho ứng dụng .NET.
- Thiết kế **pipeline** theo giai đoạn: build → test → đóng gói → triển khai staging → duyệt → production; biết **chiến lược triển khai** (rolling, blue-green, canary) và **rollback**.
- Quản lý **secret**, quyền tối thiểu, và tránh các lỗi phổ biến.

Code: workflow thật của repo này ở [`.github/workflows/kho-clean.yml`](../../../.github/workflows/kho-clean.yml).

> **Lưu ý trung thực:** workflow chỉ chạy được trên GitHub, nên tác giả **chưa chạy thật**. Đã kiểm chứng cục bộ đúng các lệnh mà pipeline gọi: `dotnet build -c Release -warnaserror` (0 cảnh báo) và `dotnet test` (45 test đều đạt). Cú pháp YAML tuân theo tài liệu GitHub Actions; lần chạy đầu bạn có thể phải chỉnh nhỏ (đường dẫn, phiên bản action).

## CI/CD là gì và để làm gì

- **CI (Continuous Integration)**: mỗi lần push/PR, máy chủ **tự động build và chạy test** trên môi trường sạch. Lỗi được phát hiện trong vài phút sau khi gây ra, khi còn nhớ mình vừa sửa gì — thay vì phát hiện lúc phát hành. "Trên máy tôi chạy được" chấm dứt vì CI là **nguồn sự thật**.
- **Continuous Delivery**: mọi thay đổi qua CI đều **sẵn sàng phát hành** (artifact/image đã tạo, đã kiểm thử); việc bấm "triển khai" có thể là quyết định của con người.
- **Continuous Deployment**: tự động triển khai lên production khi mọi kiểm tra đạt, không cần bước duyệt tay.

Nguyên tắc: **thay đổi nhỏ, thường xuyên, tự động** ít rủi ro hơn phát hành lớn, hiếm, thủ công. Pipeline tốt cho đội **niềm tin** để triển khai nhiều lần mỗi ngày.

## Các khái niệm của GitHub Actions

| Khái niệm | Ý nghĩa |
|-----------|---------|
| **Workflow** | file YAML trong `.github/workflows/` |
| **Event (`on`)** | điều kiện kích hoạt: `push`, `pull_request`, `schedule`, `workflow_dispatch` (chạy tay), `release` |
| **Job** | nhóm bước chạy trên **một runner**; các job chạy song song trừ khi có `needs` |
| **Step** | lệnh (`run`) hoặc **action** (`uses`) có sẵn |
| **Runner** | máy chạy job (`ubuntu-latest`, `windows-latest`, hoặc tự dựng — *self-hosted*) |
| **Artifact** | tệp giữ lại từ job (kết quả test, gói build) để job khác/người dùng tải |
| **Secret / Variable** | cấu hình mật / không mật, đặt ở repo/môi trường |
| **Environment** | "staging", "production" — có secret riêng và **quy tắc duyệt** |

## Workflow CI cho `kho-clean`

```yaml
name: kho-clean

on:
  push:
    branches: [main]
    paths: ["tap3/code/kho-clean/**", ".github/workflows/kho-clean.yml"]
  pull_request:
    paths: ["tap3/code/kho-clean/**", ".github/workflows/kho-clean.yml"]

permissions:
  contents: read                     # quyền TỐI THIỂU cho GITHUB_TOKEN

concurrency:
  group: kho-clean-${{ github.ref }}
  cancel-in-progress: true           # commit mới → huỷ lần chạy cũ cùng nhánh

defaults:
  run:
    working-directory: tap3/code/kho-clean

jobs:
  build-test:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore Kho.slnx
      - run: dotnet build Kho.slnx -c Release --no-restore -warnaserror
      - run: dotnet test Kho.slnx -c Release --no-build --logger "trx;LogFileName=ket-qua.trx" --collect:"XPlat Code Coverage"
      - if: always()
        uses: actions/upload-artifact@v4
        with:
          name: ket-qua-test
          path: tap3/code/kho-clean/**/TestResults/**

  docker:
    needs: build-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/build-push-action@v6
        with:
          context: tap3/code/kho-clean
          push: false
          tags: kho-web:ci
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

Từng chỗ đáng chú ý:

- **`paths`**: chỉ chạy khi thư mục liên quan đổi — monorepo (repo này chứa nhiều dự án) khỏi tốn phút CI vô ích.
- **`permissions: contents: read`**: mặc định token có thể có quyền ghi rộng; **luôn thu hẹp** rồi chỉ cấp thêm cho job cần (ví dụ `packages: write` để đẩy image). Nguyên tắc **đặc quyền tối thiểu**.
- **`concurrency` + `cancel-in-progress`**: không lãng phí runner cho commit đã bị đè.
- **`timeout-minutes`**: job treo không ăn hết phút miễn phí.
- **`-warnaserror`**: cảnh báo (nullable, deprecated) là **nợ kỹ thuật ngầm**; cho nó làm hỏng build khi dự án còn sạch. Đã xác nhận solution hiện tại build với 0 cảnh báo.
- **`--no-build` / `--no-restore`**: không làm lại việc đã làm ở bước trước.
- **`if: always()`** khi tải artifact: kết quả test cần có **cả khi test đỏ**.
- **`needs: build-test`**: chỉ dựng image khi test đạt.
- **Ghim phiên bản action** (`@v4`); với an toàn cao hơn ghim theo **commit SHA** (tránh tác giả action đẩy mã độc vào tag).

### Cache NuGet

Restore mỗi lần tải lại package. `actions/setup-dotnet` hỗ trợ `cache: true` nhưng yêu cầu **file khoá** `packages.lock.json` (bật `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` trong csproj/`Directory.Build.props`). Khoá phụ thuộc còn giúp **build tái lập được** và phát hiện thay đổi package ngoài ý muốn. Cách khác: `actions/cache` thư mục `~/.nuget/packages` với khoá là hash của các `*.csproj`.

### Ma trận (matrix)

Kiểm thử thư viện trên nhiều hệ điều hành/phiên bản .NET:

```yaml
strategy:
  fail-fast: false
  matrix:
    os: [ubuntu-latest, windows-latest]
    dotnet: [10.0.x]
runs-on: ${{ matrix.os }}
```

Ứng dụng chạy trong container Linux thì thường chỉ cần `ubuntu-latest`.

## Từ CI tới CD: đẩy image và triển khai

Ví dụ phát hành: khi có tag `v*`, dựng và đẩy image lên **GitHub Container Registry (GHCR)**, rồi triển khai.

```yaml
name: phat-hanh
on:
  push:
    tags: ["v*"]

permissions:
  contents: read
  packages: write               # cần để đẩy vào GHCR

jobs:
  image:
    runs-on: ubuntu-latest
    outputs:
      digest: ${{ steps.build.outputs.digest }}
    steps:
      - uses: actions/checkout@v4
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}            # token tự cấp, KHÔNG phải mật khẩu cá nhân
      - id: build
        uses: docker/build-push-action@v6
        with:
          context: tap3/code/kho-clean
          push: true
          tags: ghcr.io/${{ github.repository }}/kho-web:${{ github.ref_name }}

  staging:
    needs: image
    runs-on: ubuntu-latest
    environment: staging                                   # secret + quy tắc riêng của môi trường
    steps:
      - run: echo "Triển khai ${{ github.ref_name }} lên staging (lệnh cụ thể tuỳ nền tảng — Chương 16)"

  production:
    needs: staging
    runs-on: ubuntu-latest
    environment: production                                # cấu hình "Required reviewers": CHỜ NGƯỜI DUYỆT trước khi chạy
    steps:
      - run: echo "Triển khai lên production"
```

**Environment `production` với "Required reviewers"** chèn cổng duyệt tay giữa staging và production — Continuous *Delivery* có kiểm soát. Bỏ cổng đó là Continuous *Deployment*.

### Chiến lược triển khai

| Chiến lược | Cách làm | Ưu | Nhược |
|-----------|----------|----|-------|
| **Recreate** | tắt bản cũ, bật bản mới | đơn giản | có thời gian ngừng |
| **Rolling** | thay dần từng instance | không ngừng, ít tài nguyên thêm | hai phiên bản chạy song song (phải **tương thích ngược**) |
| **Blue-Green** | dựng môi trường mới (green) đầy đủ, chuyển lưu lượng một lần | rollback tức thì (chuyển lại) | tốn gấp đôi tài nguyên |
| **Canary** | chuyển 1–5% lưu lượng sang bản mới, quan sát metric, tăng dần | phát hiện lỗi sớm, ảnh hưởng nhỏ | cần quan sát tốt (Chương 11) và định tuyến lưu lượng |

Mọi chiến lược "không ngừng" đòi hỏi **thay đổi CSDL tương thích ngược** — mẫu **expand/contract** (mở rộng → triển khai → thu hẹp):

1. *Expand*: thêm cột mới (cho phép null), mã mới ghi cả hai cột, mã cũ vẫn chạy được.
2. Di chuyển dữ liệu; chuyển hẳn sang cột mới.
3. *Contract*: ở lần phát hành sau mới xoá cột cũ.

Đổi tên/xoá cột trong **một** bước là cách chắc chắn nhất để làm hỏng rolling deployment.

**Rollback**: giữ image tag cũ, triển khai lại là quay lui code; **migration** thì khó lùi — thiết kế migration tương thích (expand/contract), sao lưu trước thay đổi lớn, và kiểm tra khôi phục.

**Feature flag** tách "triển khai mã" khỏi "bật tính năng": mã lên production ở trạng thái tắt, bật dần cho nhóm người dùng, tắt ngay nếu có sự cố (`Microsoft.FeatureManagement`).

## Một pipeline tốt gồm những gì

1. **Build + phân tích tĩnh** (cảnh báo là lỗi, `dotnet format --verify-no-changes`, analyzer).
2. **Test nhanh trước** (đơn vị) rồi test chậm (tích hợp); báo cáo độ phủ (đừng coi độ phủ là mục tiêu tự thân).
3. **Quét bảo mật**: `dotnet list package --vulnerable --include-transitive`, quét secret (`gitleaks`), quét image (Trivy), CodeQL, Dependabot cập nhật phụ thuộc.
4. **Dựng artifact một lần** rồi **thăng cấp cùng artifact** qua các môi trường (build once, deploy many) — không build lại cho production.
5. **Triển khai tự động lên staging**, chạy **smoke test** (gọi `/health/ready`, vài endpoint chính) sau triển khai.
6. **Cổng duyệt** trước production; theo dõi metric sau triển khai (Chương 11) và **tự rollback** nếu tỉ lệ lỗi tăng.
7. **Nhanh**: mục tiêu CI dưới ~10 phút; chậm sẽ bị né tránh.

### Migration trong pipeline

Chạy `dotnet ef migrations bundle` (tạo tệp thực thi tự chứa) như **bước riêng, một lần**, trước khi khởi động các instance mới; **không** để mọi instance cùng tự `MigrateAsync` khi cụm chạy nhiều bản.

## Secret và bảo mật pipeline

- Đặt secret ở **Settings → Secrets** (theo repo/environment), truy cập bằng `${{ secrets.TEN }}`; log tự che giá trị nhưng **đừng in/`echo`** chúng và đừng truyền qua tham số dòng lệnh dễ lộ.
- **PR từ fork** không nhận secret (mặc định) — đúng vì mã lạ không được đọc secret; cẩn trọng với `pull_request_target`.
- Ưu tiên **OIDC liên kết (workload identity federation)** với đám mây (Azure/AWS/GCP): job xin token ngắn hạn từ GitHub, **không cần lưu khoá dài hạn** trong secret (`permissions: id-token: write`).
- **Tin cậy có chọn lọc** vào action bên thứ ba; ghim SHA; bật Dependabot cho chính các action.
- Runner tự dựng (self-hosted) cho repo **công khai** rất nguy hiểm.

## Các nền tảng khác

Khái niệm y hệt ở **Azure DevOps Pipelines**, **GitLab CI**, **Jenkins**, **TeamCity**. Học một nền tảng, chuyển sang nền khác chủ yếu là học cú pháp.

## Lỗi thường gặp

- Chỉ chạy CI trên nhánh chính — lỗi tới quá muộn; nên chạy trên **pull request** và chặn merge khi đỏ (branch protection).
- Cấp `permissions` rộng; dùng PAT cá nhân thay vì `GITHUB_TOKEN`.
- Build lại cho từng môi trường thay vì thăng cấp cùng artifact.
- Test chập chờn (flaky) bị "bấm chạy lại" cho qua — làm mất niềm tin; sửa hoặc cách ly.
- Tag `latest` cho image triển khai; không có cách xác định "đang chạy phiên bản nào".
- Migration phá vỡ tương thích ngược trong rolling deployment.
- Không có smoke test/giám sát sau triển khai; không diễn tập rollback.
- Secret in ra log, lưu trong image, hoặc commit vào `.github`.
- Pipeline quá chậm, không cache.

## Bài tập

1. Đẩy repo lên GitHub, bật workflow `kho-clean.yml`, tạo PR có test đỏ và xem PR bị chặn khi bật *branch protection*.
2. Bật **khoá phụ thuộc** (`packages.lock.json`) và `cache: true` cho `setup-dotnet`; so sánh thời gian restore.
3. Viết workflow `phat-hanh` đẩy image lên GHCR khi gắn tag `v1.0.0`; thêm environment `production` với người duyệt.
4. Thêm bước quét `dotnet list package --vulnerable --include-transitive` và làm hỏng pipeline nếu có lỗ hổng.
5. Thêm **Dependabot** (`.github/dependabot.yml`) cho NuGet, GitHub Actions và Docker, lịch hàng tuần.
6. Vẽ kế hoạch **expand/contract** để đổi tên cột `Nhom` → `DanhMuc` mà không ngừng dịch vụ.
