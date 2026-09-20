# Chương 40 — Code mẫu: Cấu hình ứng dụng

```
dotnet run
```

Ghi đè cấu hình bằng biến môi trường (dấu `__` thay cho `:`):

```
# Bash
CuaHang__Ten="Sach Do" DOTNET_ENVIRONMENT=Production dotnet run
# PowerShell
$env:CuaHang__Ten="Sach Do"; dotnet run
```

Dùng user secrets (không bao giờ commit lên Git):

```
dotnet user-secrets set "KetNoi:MatKhau" "bi-mat-cua-toi"
dotnet run
```
