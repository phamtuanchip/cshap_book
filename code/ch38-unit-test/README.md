# Chương 38 — Code mẫu: Unit test với xUnit

Solution gồm thư viện `ShopLib` và project test `ShopLib.Tests` (xUnit + NSubstitute).

Chạy toàn bộ test:

```
cd ShopLib.Tests
dotnet test
```

Chạy test theo tên hoặc lọc:

```
dotnet test --filter "FullyQualifiedName~GioHangTests"
dotnet test --logger "console;verbosity=detailed"
```

Các phiên bản gói trong `.csproj` có thể cũ hơn bản mới nhất; cập nhật bằng `dotnet add package <tên>`
hoặc dùng `dotnet new xunit` để lấy bộ phiên bản mới nhất.
