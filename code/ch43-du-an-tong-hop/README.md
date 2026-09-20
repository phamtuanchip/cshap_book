# Chương 43 — Dự án tổng hợp: Quản lý kho hàng (console)

Ứng dụng console hoàn chỉnh áp dụng kiến thức Tập 1: OOP, record, collection, LINQ, async, JSON, exception, interface/DIP,
unit test.

## Cấu trúc

```
ch43-du-an-tong-hop/
├── QuanLyKho.slnx
├── QuanLyKho.Core/            # thư viện nghiệp vụ (không phụ thuộc console)
│   ├── Models/                # SanPham, GiaoDich, DuLieuKho, KhoException...
│   ├── Abstractions/          # IKhoRepository
│   ├── Services/              # KhoService (nghiệp vụ)
│   └── Storage/               # JsonKhoRepository (lưu file JSON)
├── QuanLyKho.App/             # ứng dụng console (Program.cs = composition root)
└── QuanLyKho.Tests/           # unit test xUnit với repository giả
```

## Chạy

Chạy kịch bản mẫu (dữ liệu tạm, tự xoá):

```
dotnet run --project QuanLyKho.App -- --demo
```

Chạy menu tương tác (dữ liệu lưu ở `%APPDATA%/QuanLyKho/kho.json`, hoặc chọn file bằng `--file đường-dẫn.json`):

```
dotnet run --project QuanLyKho.App
```

Chạy test:

```
dotnet test
```
