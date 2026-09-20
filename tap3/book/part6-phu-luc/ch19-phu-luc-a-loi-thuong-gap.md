# Phụ lục A — Lỗi và bẫy kiến trúc thường gặp

Bảng tra nhanh các sai lầm hay gặp khi áp dụng nội dung Tập 3, kèm dấu hiệu nhận biết và hướng xử lý. Số trong ngoặc là chương liên quan.

## Kiến trúc và phân lớp (1–2)

| Bẫy | Dấu hiệu | Cách xử lý |
|-----|----------|------------|
| Domain phụ thuộc EF Core/ASP.NET | `using Microsoft.EntityFrameworkCore` trong Domain | Chiều phụ thuộc hướng vào trong; ánh xạ ở Infrastructure; thêm architecture test |
| Controller/endpoint chứa nghiệp vụ | if/else quy tắc trong endpoint | Đẩy vào aggregate/handler; endpoint chỉ dịch HTTP |
| Application trả `IQueryable` | truy vấn EF rò lên trên | Cổng dạng phương thức async trả DTO |
| Chia lớp theo kỹ thuật cho dự án nhỏ | ba tầng chỉ chuyển tiếp lời gọi | Đơn giản hoá; kiến trúc phải tương xứng độ phức tạp |
| Interface cho mọi thứ | mỗi lớp có `IFoo` một cài đặt | Chỉ tạo cổng ở ranh giới thật (I/O, ngoài hệ thống) |

## Miền và dữ liệu (3–4)

| Bẫy | Dấu hiệu | Xử lý |
|-----|----------|-------|
| Anemic model | entity chỉ có getter/setter, logic ở service | Chuyển hành vi vào entity; setter private |
| Aggregate quá lớn | nạp cả cây, khoá xung đột liên tục | Aggregate nhỏ; tham chiếu bằng id |
| Giữ đối tượng giữa aggregate | `don.SanPham.TonKho--` | Tham chiếu bằng mã; điều phối ở use case |
| Value object có setter | "Tien" bị sửa giữa chừng | Bất biến, so sánh theo giá trị |
| Repository lộ mọi truy vấn | 40 phương thức `LayTheo...` | Repository theo aggregate; đọc qua read model |
| Specification thay cho việc đơn giản | pattern cho truy vấn dùng một lần | Chỉ dùng khi tái sử dụng/kết hợp điều kiện |
| SQLite: `decimal`, `DateTimeOffset`, `Contains` | lỗi dịch LINQ / sai thứ tự | Đổi sang `double`/binary converter, `EF.Functions.Like` — hoặc dùng CSDL máy chủ |

## CQRS, pipeline, Result (5–7)

| Bẫy | Xử lý |
|-----|-------|
| Bọc CQRS + mediator cho CRUD đơn giản | Chỉ dùng khi lợi ích rõ (pipeline dùng chung, tách đọc/ghi) |
| Truy vấn đi qua UnitOfWork behavior | Chỉ ràng buộc behavior này vào `ICommand` |
| Sai thứ tự behavior | Logging > Validation > UnitOfWork; có test kiểm thứ tự |
| Ném exception cho lỗi nghiệp vụ dự kiến | Dùng `Result`; exception cho điều bất thường |
| Lẫn lỗi nghiệp vụ với lỗi hạ tầng | Hạ tầng nem exception cụ thể, Application dịch |
| Validate lặp ở nhiều tầng | Định dạng ở validator; bất biến ở domain — mỗi cái một vai |
| Bỏ qua `Result` (không kiểm `ThanhCong`) | Analyzer/test; API `Match`; không truy cập `.GiaTri` bừa |
| Ánh xạ lỗi → HTTP rải rác | Một điểm `ToHttp()` duy nhất |

## Sự kiện và outbox (8)

- Phát sự kiện **trước** khi commit → phát ma khi giao dịch thất bại. Dùng outbox cùng giao dịch.
- Consumer không idempotent; xử lý trùng gây tác dụng phụ kép.
- Outbox không dọn dẹp → bảng phình; không giới hạn số lần thử/không dead-letter.
- Nhiều instance cùng lấy một dòng outbox → cần khoá hàng/`SKIP LOCKED` hoặc cập nhật có điều kiện.
- Sự kiện chứa cả entity thay vì dữ liệu tối thiểu, phiên bản hoá kém.

## Kiểm thử (9)

- Mock quá đà → test chỉ kiểm cách gọi, không kiểm hành vi. Ưu tiên fake/CSDL thật cho tích hợp.
- `:memory:` chia sẻ kết nối làm hỏng test đồng thời → dùng tệp tạm.
- Test phụ thuộc thứ tự/dữ liệu chung; dùng dữ liệu riêng mỗi test (mã duy nhất).
- Sleep để chờ thay vì điều khiển thời gian (`TimeProvider`/`FakeTimeProvider`).
- Đuổi theo % độ phủ, bỏ qua test hành vi quan trọng; test chập chờn bị bỏ qua.

## Caching (10)

- Cache không có chính sách vô hiệu hoá; TTL vô hạn.
- **Stampede**: nhiều request cùng nạp một khoá hết hạn → dùng `HybridCache`/khoá.
- Cache dữ liệu theo **người dùng** trong cache dùng chung (rò rỉ dữ liệu); output cache cho response có cookie/`Authorization`.
- Cache trong bộ nhớ khi chạy nhiều instance mà cần nhất quán.
- Cache kết quả rỗng/lỗi; khoá không phân biệt tham số.

## Quan sát (11)

- Quên `AddSource`/`AddMeter` cho nguồn tự viết.
- Nhãn metric có cardinality cao (`UserId`).
- Log dữ liệu nhạy cảm; log ở mức `Debug` trên production.
- Kiểm tra phụ thuộc trong **liveness** → restart dây chuyền.
- Tối ưu không đo; cảnh báo không có runbook.

## Bảo mật và độ tin cậy (12–13)

- Implicit/Password flow; token trong `localStorage`; không PKCE; `redirect_uri` ký tự đại diện.
- Không kiểm tra `aud`/`iss`; nhầm 401 với 403; chỉ dùng role, bỏ kiểm tra quyền sở hữu.
- Idempotency kiểu "kiểm tra rồi ghi" (race); lưu kết quả 5xx; không đối chiếu nội dung với khoá.
- Audit ngoài giao dịch; cho phép sửa/xoá audit; ghi bí mật vào log/audit.
- Secret trong Git/image; khoá mã hoá cạnh dữ liệu.

## Đóng gói và triển khai (14–17)

- Docker: chép mã trước `restore`; thiếu `.dockerignore`; chạy root; dùng `latest`; quên volume; nghe `localhost` trong container.
- CI/CD: `permissions` rộng; build lại cho từng môi trường; migration phá tương thích ngược; thiếu smoke test/rollback.
- Cloud: quên forwarded headers; tin mọi `X-Forwarded-*`; trạng thái cục bộ khi nhân bản; Data Protection keys không chia sẻ; chưa từng thử khôi phục sao lưu.
- Microservices: **distributed monolith**, chung CSDL, chia theo tầng kỹ thuật, không có trace phân tán, cố dùng giao dịch phân tán.

## Nguyên tắc chung khi gặp lỗi lạ

1. Tái hiện tối thiểu (một test đỏ).
2. Đọc **thông báo lỗi và inner exception** đầy đủ; đọc SQL EF sinh ra (`LogTo`/log mức `Information` của `Microsoft.EntityFrameworkCore.Database.Command`).
3. Dùng `traceId` xuyên log/trace (Chương 11).
4. Thay đổi **một** thứ mỗi lần; viết test khoá lỗi trước khi sửa.
5. Nghi ngờ môi trường (chính sách hệ điều hành, phiên bản SDK, cổng bận) trước khi nghi ngờ trình biên dịch — và nếu môi trường chặn thì báo cho người có quyền, đừng lách.
