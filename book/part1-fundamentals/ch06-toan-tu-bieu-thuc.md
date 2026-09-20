# Chương 6 — Toán tử và biểu thức

## Mục tiêu học

Sau chương này, bạn sẽ:

- Dùng thành thạo toán tử số học, so sánh, logic, gán, tăng/giảm.
- Hiểu **độ ưu tiên** và **ngắn mạch** (short-circuit).
- Dùng các toán tử tiện lợi: `?:`, `??`, `??=`, `?.` và toán tử bit ở mức cơ bản.
- Tránh bẫy **chia nguyên** và `i++` vs `++i`.

Code mẫu: [`code/ch06-toan-tu-bieu-thuc/`](../../code/ch06-toan-tu-bieu-thuc/).

**Biểu thức** là thứ có giá trị (`2 + 3`, `tuoi >= 18`); **toán tử** là ký hiệu tạo biểu thức từ các
toán hạng.

## Toán tử số học

| Toán tử | Ý nghĩa | Ví dụ (`a=17, b=5`) |
|---------|---------|---------------------|
| `+ - *` | cộng, trừ, nhân | `a + b` → 22 |
| `/` | chia | `a / b` → **3** (chia nguyên) |
| `%` | chia lấy dư | `a % b` → 2 |

**Bẫy chia nguyên**: nếu cả hai toán hạng là số nguyên, kết quả là số nguyên (bỏ phần thập phân).
Muốn chia thực, ép ít nhất một vế: `a / (double)b` → `3.4`. Chia số nguyên cho `0` ném
`DivideByZeroException`; chia số thực cho `0` cho `Infinity`.

`%` rất hữu ích: `n % 2 == 0` kiểm tra số chẵn; `n % 10` lấy chữ số cuối.

## Tăng, giảm và gán kết hợp

```csharp
int i = 5;
int x = i++;   // x = 5, rồi i = 6  (hậu tố: dùng giá trị cũ trước)
int y = ++i;   // i = 7 rồi y = 7   (tiền tố: tăng trước rồi dùng)
```

Nên tránh nhồi `i++` vào biểu thức phức tạp — khó đọc. Gán kết hợp: `+=`, `-=`, `*=`, `/=`, `%=`.

## So sánh và logic

`==  !=  <  >  <=  >=` cho kết quả `bool`. Toán tử logic:

| Toán tử | Ý nghĩa |
|---------|---------|
| `&&` | và (AND) |
| `\|\|` | hoặc (OR) |
| `!` | phủ định (NOT) |

**Ngắn mạch**: `&&` dừng ngay nếu vế trái sai; `||` dừng ngay nếu vế trái đúng. Nhờ vậy viết được:

```csharp
bool ok = mang.Length > 0 && mang[0] > 0;   // không lỗi dù mảng rỗng
```

Đổi thứ tự hai vế sẽ gây lỗi khi mảng rỗng. Nhớ rằng `=` là **gán**, `==` là **so sánh**.

## Độ ưu tiên

Từ cao đến thấp (rút gọn): `! ++ --` → `* / %` → `+ -` → `< > <= >=` → `== !=` → `&&` → `||` → `?:` →
gán. Ví dụ `2 + 3 * 4` = 14. **Khi nghi ngờ, dùng ngoặc** `( )` — code rõ hơn là nhớ bảng ưu tiên.

## Toán tử điều kiện và null

```csharp
string kq = tuoi >= 18 ? "Người lớn" : "Trẻ em";   // ?: chọn một trong hai

string? ten = null;
string hienThi = ten ?? "(không tên)";   // ?? dùng vế phải nếu vế trái là null
ten ??= "Mặc định";                      // ??= gán nếu đang là null
int? len = ten?.Length;                  // ?. trả null thay vì ném lỗi nếu ten là null
```

`?.` và `??` là công cụ chính chống `NullReferenceException` — sẽ học sâu ở Chương 23.

## Toán tử bit (giới thiệu)

Làm việc trên từng bit của số nguyên: `&` (và), `|` (hoặc), `^` (xor), `~` (đảo), `<<`, `>>` (dịch bit).

```csharp
6 & 3     // 0b110 & 0b011 = 0b010 = 2
1 << 4    // 16 (nhân 2 bốn lần)
```

Thường gặp ở cờ (flags), quyền hạn, xử lý nhị phân (Chương 19 với `enum [Flags]`).

## Lỗi thường gặp

- `if (x = 5)` thay vì `x == 5` — C# chặn được ở hầu hết trường hợp (lỗi biên dịch), nhưng với `bool`
  thì `if (ok = true)` vẫn hợp lệ và gán luôn: hãy viết `if (ok)`.
- `1 / 2` ra 0; `7 / 2 * 2.0` ra `6`, không phải `7`. Chuyển sang thực **trước** khi chia.
- Quên ngoặc: `a + b / 2` khác `(a + b) / 2`.
- So sánh số thực bằng `==` (Chương 5).
- Quên rằng `%` với số âm cho dư âm: `-7 % 3` bằng `-1`.

## Bài tập

1. Đọc một số, in ra "chẵn" hay "lẻ" (dùng `%` và `?:`).
2. Tính điểm trung bình 3 môn (nhập từ bàn phím) với 2 chữ số thập phân — chú ý chia nguyên.
3. Cho `int a = 5; int b = a++ + ++a;` — dự đoán `a` và `b` rồi chạy kiểm tra.
4. Viết biểu thức kiểm tra năm nhuận: chia hết cho 4 nhưng không chia hết cho 100, hoặc chia hết cho 400.
