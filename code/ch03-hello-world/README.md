# Chương 3 — Code mẫu: Hello World

Yêu cầu: .NET SDK 10 (kiểm tra bằng `dotnet --version`).

## Chạy `HelloWorld`

```
cd HelloWorld
dotnet run
```

Kết quả mong đợi:

```
Xin chao, C#!
```

## Chạy `Greeter` (có đối số dòng lệnh)

```
cd Greeter
dotnet run -- Tuan
```

Kết quả mong đợi:

```
Xin chao, Tuan!
Day la doi so dong lenh nhan duoc: 1 doi so.
```

Dấu `--` tách tham số của `dotnet run` khỏi đối số truyền vào chương trình. Thử `dotnet run` không
kèm đối số để thấy giá trị mặc định "hoc vien".
