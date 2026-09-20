# Chương 10 — Code mẫu: Kiểm thử API

Solution gồm API `DonHangApi` và project test `DonHangApi.Tests` (xUnit + `WebApplicationFactory` + NSubstitute).

Chạy test:

```
cd DonHangApi.Tests
dotnet test
```

Chạy API để thử thủ công (cần header `X-Api-Key: khoa-thu-nghiem`):

```
dotnet run --project DonHangApi
curl -H "X-Api-Key: khoa-thu-nghiem" http://localhost:5000/api/don-hang
```
