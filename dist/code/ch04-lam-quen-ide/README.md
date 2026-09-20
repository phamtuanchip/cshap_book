# Chương 4 — Code mẫu: Solution nhiều project và NuGet

Dựng lại solution mẫu của chương bằng dòng lệnh (hoặc IDE):

```
dotnet new sln -n LamQuenIde
dotnet new console -n App
dotnet new classlib -n MayTinh
dotnet sln add App MayTinh
dotnet add App reference MayTinh
dotnet add App package Humanizer.Core
```

Sau đó chép `MayTinh/PhepTinh.cs` và `App/Program.cs` trong thư mục này vào đúng project, rồi:

```
dotnet run --project App
```

Kết quả mong đợi:

```
7 + 5 = 12
Khoang thoi gian: 3 days
```
