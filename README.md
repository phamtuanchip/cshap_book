# C# Book — Từ Cơ bản đến Nâng cao

Sách lập trình C# bằng tiếng Việt, dành cho người **chưa biết gì về lập trình lẫn OOP**: đi từ cài đặt
.NET SDK/IDE, cú pháp cơ bản, OOP từ số 0, đến lập trình web hiện đại với **ASP.NET Core**
và các ứng dụng web nâng cao áp dụng design pattern / kiến trúc best practice của .NET — kèm code mẫu
đầy đủ, chạy được cho từng chương.

> **Trạng thái: đang viết Tập 1 — đã xong Phần 0–6 (chương 1–34).** Đã dựng khung, build được HTML (`npm run build`). Cấu trúc và pipeline tham khảo repo
> `java_book` (đã hoàn thành 47 chương, có HTML + PDF). Định dạng phát hành: **HTML** trước, sau đó
> **PDF** và **EPUB**.

## 1. Mục tiêu

- Dạy C# từ **con số 0** — không giả định người đọc từng lập trình hay biết OOP.
- Giảng OOP (class, encapsulation, kế thừa, đa hình, abstract/interface...) chi tiết, đúng bản chất.
- Đi trọn lộ trình: ngôn ngữ → ASP.NET Core → kiến trúc nâng cao. Chỉ dạy công nghệ hiện đại (.NET LTS mới nhất), không đề cập nền tảng legacy.
- Mỗi chương có ví dụ code mẫu lưu trong repo, chạy được (không phải snippet rời rạc).
- Chất lượng "release": đủ để công bố công khai kèm code.

## 2. Đối tượng độc giả

- Người mới hoàn toàn, hoặc biết chút ít nhưng chưa hiểu OOP.
- Chưa biết .NET, SDK, CLR là gì — cần hướng dẫn cài đặt từ đầu.
- Tập 2, 3 giả định đã nắm nội dung tập trước.

## 3. Chia tập

| Tập | Nội dung | Kết quả sau khi đọc |
|-----|----------|---------------------|
| **Tập 1** — C# cơ bản đến nâng cao | Môi trường, cú pháp, OOP, generics, collection, LINQ, async, I/O, test | Viết được ứng dụng console hoàn chỉnh |
| **Tập 2** — ASP.NET Core | Dữ liệu (EF Core), Minimal API/Web API, MVC, Razor Pages, Blazor | Viết được web app/API CRUD có CSDL |
| **Tập 3** — Web nâng cao & Design Pattern | Kiến trúc phân lớp, DI, pattern, CQRS, bảo mật, hiệu năng, deploy | Xây dựng web app theo kiến trúc chuẩn .NET |

## 4. Cấu trúc thư mục 

```
cshap_book/
├── book/                        # Nội dung sách (Markdown), 1 file/chương + manifest.json (mục lục)
│   ├── part0-setup/ ... part14-...  # theo mục lục bên dưới
├── code/                        # Code mẫu — mỗi chương 1 project riêng, chạy độc lập
│   ├── legacy-unused/           # Code cũ chưa gắn chương (nếu có file không khớp chương nào)
│   └── ch03-hello-world/, ch13-class-object/, ...
├── tools/                       # build.js (HTML), build-pdf.js (PDF), style.css
├── dist/                        # HTML + PDF đã build (commit sẵn)
├── package.json
└── README.md                    # File kế hoạch này
```

Quy ước: chương `chXX-slug` gồm `book/<part>/chXX-slug.md` + `code/chXX-slug/` (có README riêng, chạy
bằng `dotnet run`). Các file `CustomersController.cs`, `UserController.cs` ở thư mục gốc hiện tại là
ví dụ đầu tiên (ASP.NET Core Web API) — sẽ chuyển vào `code/` đúng chương Web API (hoặc
`legacy-unused/` nếu không khớp).

## 5. Quy ước cho mỗi chương

1. Mục tiêu học (đọc xong làm được gì).
2. Lý thuyết/khái niệm nền, có sơ đồ (Mermaid) khi cần — đặc biệt OOP, kiến trúc.
3. Ví dụ code từng bước, giải thích **vì sao** chứ không chỉ cú pháp.
4. Project mẫu hoàn chỉnh trong `code/`, chạy được bằng `dotnet run`.
5. Bài tập / gợi ý mở rộng cuối chương.
6. Lỗi thường gặp + cách khắc phục (`NullReferenceException`, `InvalidCastException`, deadlock async...).

## 6. Mục lục

### TẬP 1 — C# cơ bản đến nâng cao

**Phần 0 — Chuẩn bị môi trường**
1. Giới thiệu C# và .NET: CLR, SDK, runtime, BCL, các phiên bản LTS
2. Cài đặt .NET SDK và IDE (Visual Studio Community / VS Code + C# Dev Kit / Rider)
3. Chương trình C# đầu tiên: `dotnet new console`, `dotnet run`, top-level statements, cấu trúc project `.csproj`
4. Làm quen IDE: tạo solution, chạy/debug cơ bản, NuGet

**Phần 1 — Nền tảng lập trình (chưa cần OOP)**
5. Biến, kiểu dữ liệu (value type), hằng số, ép kiểu, `var`
6. Toán tử, biểu thức, độ ưu tiên
7. Cấu trúc điều khiển: `if/else`, `switch` statement & switch expression
8. Vòng lặp: `for`, `while`, `do-while`, `foreach`, `break/continue`
9. Mảng một chiều, nhiều chiều, jagged array
10. Phương thức: tham số, `ref/out/in`, giá trị trả về, overloading, tham số tuỳ chọn/đặt tên, scope
11. Chuỗi (`string`): bất biến, nội suy, `StringBuilder`, định dạng

**Phần 2 — Nhập môn OOP**
12. Vì sao cần OOP: thủ tục vs hướng đối tượng, 4 trụ cột (tổng quan)
13. Class & Object: field, constructor, `this`, tạo và dùng object
14. Encapsulation: access modifier, **property** (get/set/init), auto-property, immutable object
15. Namespace, `using`, tổ chức project nhiều class/assembly

**Phần 3 — OOP nâng cao**
16. Kế thừa: `base`, constructor cha, `virtual/override`, `new` hiding, `sealed`
17. Đa hình: upcasting/downcasting, `is/as`, pattern matching, dynamic dispatch
18. Abstract class & Interface: khi nào dùng cái nào, default interface method, explicit implementation
19. Lớp lồng, `static` class, `partial`, extension method, enum (và `[Flags]`)
20. Lớp `object`: `Equals/GetHashCode/ToString`, so sánh đối tượng, `IComparable` vs `IComparer`, `struct` vs `class`, `record`

**Phần 4 — Xử lý lỗi & Generics**
21. Exception: `try/catch/finally`, `using`, exception filter, custom exception
22. Generics: generic class/method, constraint (`where`), covariance/contravariance
23. Nullable value type & nullable reference type (`?`, `??`, `?.`)

**Phần 5 — Collection & LINQ**
24. Tổng quan collection: `List<T>`, `Dictionary<,>`, `HashSet<T>`, `Queue/Stack`, khi nào chọn gì
25. `List<T>`/`LinkedList<T>`: cơ chế bên trong, độ phức tạp
26. `Dictionary`, `SortedDictionary`, `HashSet`, `SortedSet` — vai trò của `Equals/GetHashCode`
27. Duyệt & sắp xếp: `IEnumerable`, `yield`, `IComparer`
28. Thuật toán tìm kiếm: Linear, Binary, Jump, Interpolation
29. Duyệt đồ thị/cây: BFS & DFS
30. **LINQ**: query/method syntax, deferred execution, `GroupBy`, `Join`, `Aggregate`

**Phần 6 — C# hiện đại & bất đồng bộ**
31. Delegate, `event`, lambda, `Func/Action/Predicate`
32. Record, init-only, pattern matching nâng cao, `required`, collection expression, primary constructor
33. Lập trình bất đồng bộ: `async/await`, `Task`, `CancellationToken`, các lỗi thường gặp (deadlock, `async void`)
34. Đa luồng: `Thread`, `lock`, `Interlocked`, `ConcurrentDictionary`, `Parallel`, `Channel`

**Phần 7 — Nhập/Xuất & xử lý dữ liệu**
35. File I/O: `File`, `Path`, `Stream`, `StreamReader/Writer`
36. JSON/CSV: `System.Text.Json`, đọc-ghi dữ liệu có cấu trúc
37. Reflection & Attribute cơ bản; Serialization (khi nào nên/không)

**Phần 8 — Công cụ, kiểm thử & quản lý dự án**
38. Unit test: xUnit/NUnit, assertion, Moq/NSubstitute
39. Debug & logging: debugger, `ILogger`, Serilog
40. Git, quản lý solution, NuGet, cấu hình (`appsettings.json`), user secrets

**Phần 9 — Kiến trúc cơ bản & dự án tổng hợp**
41. Design pattern cơ bản: Singleton, Factory, Strategy, Observer
42. Nguyên lý SOLID qua ví dụ tái cấu trúc code thực tế
43. Dự án tổng hợp Tập 1: ứng dụng console quản lý (OOP + LINQ + I/O + test)

**Phụ lục Tập 1**: A — bảng lỗi thường gặp tra cứu nhanh; B — tài liệu tham khảo; C — cheat sheet cú pháp C#.

### TẬP 2 — ASP.NET Core (khung, chi tiết khi bắt đầu viết)

**Phần 10 — Dữ liệu**: SQL cơ bản & SQLite/SQL Server; Entity Framework Core (DbContext, migration, quan hệ, LINQ to Entities, tránh N+1); chống SQL injection.

**Phần 11 — Web với ASP.NET Core**: HTTP/REST cơ bản, cấu trúc project, middleware, routing; **Minimal API** & **Web API** (DTO, validation, OpenAPI — dùng lại `CustomersController`, `UserController`); Dependency Injection & Configuration; EF Core trong web.

**Phần 12 — Giao diện web**: **MVC** (controller, view, Razor, model binding); **Razor Pages**; **Blazor** (component, form, gọi API); ứng dụng web CRUD hoàn chỉnh.

### TẬP 3 — Web nâng cao & Design Pattern (khung)

**Phần 13 — Kiến trúc**: kiến trúc phân lớp / Clean Architecture / Onion; Repository & Unit of Work; Mediator & **CQRS** (MediatR); Domain-Driven Design cơ bản; AutoMapper/Mapster, FluentValidation, Result pattern.

**Phần 14 — Sản xuất thực tế**: Authentication/Authorization (Identity, JWT, OAuth2/OIDC); caching (Memory/Redis); background job & message queue; logging, health check, OpenTelemetry; hiệu năng; Docker & CI/CD; deploy (Azure/Linux/container); microservices tổng quan.

**Phần 15 — Dự án tổng hợp Tập 3**: web app nhiều lớp, có auth, test, Docker, CI.

## 7. Pipeline xuất bản (HTML → PDF → EPUB)

Tái sử dụng đúng pipeline của `java_book`: script Node.js tự viết (`markdown-it` + `highlight.js`),
không dùng mdBook/Pandoc.

- **HTML**: `tools/build.js` đọc `book/manifest.json` + từng `chXX-*.md` → `dist/` (sidebar, prev/next,
  syntax highlight, Mermaid có zoom/pan), sao chép `code/` vào `dist/code/` và viết lại link tương đối.
- **PDF**: `tools/build-pdf.js` gộp toàn bộ chương, Puppeteer in ra `dist/*.pdf`, link `code/...` trỏ
  sang GitHub.
- **EPUB**: dùng lại nguồn Markdown, không viết lại nội dung.
- Cần bổ sung: highlight.js đã hỗ trợ `csharp`; kiểm tra Mermaid + font tiếng Việt.

## 8. Lộ trình biên soạn (milestones)

1. ✅ Dựng khung: `book/`, `manifest.json`, `tools/` (copy từ `java_book`), `package.json`, `.gitignore` (đã có
   sẵn mẫu VisualStudio), 2 file controller cũ chuyển vào `code/legacy-unused/`.
2. ✅ Viết + code mẫu Phần 0 (môi trường) — mốc: chạy được Hello World bằng `dotnet run`.
3. ✅ Phần 1 (nền tảng).
4. ✅ Phần 2–3 (OOP).
5. ✅ Phần 4–5 (exception, generics, nullable, collection, LINQ, thuật toán).
6. ⬜ Phần 6 xong (delegate, C# hiện đại, async, đa luồng); Phần 7 (I/O) đang viết.
7. ⬜ Phần 8–9 (test/tooling, pattern, SOLID, dự án tổng hợp) + phụ lục → **hoàn thành Tập 1**.
8. ⬜ Build HTML + PDF Tập 1, rà soát link, publish.
9. ⬜ Tập 2: Dữ liệu → Web API → MVC/Razor Pages/Blazor.
10. ⬜ Tập 3: Kiến trúc → Sản xuất → Dự án tổng hợp.
11. ⬜ Xuất bản EPUB cho cả 3 tập.

## 9. Các quyết định cần chốt / đã chốt

- **Ngôn ngữ code mẫu**: C# thuần xuyên suốt; chỉ giới thiệu build tool/NuGet nâng cao từ Phần 8. Các chương đầu
  dùng `dotnet` CLI để không bị IDE làm rối.
- **Phiên bản chuẩn**: .NET LTS mới nhất (dự kiến **.NET 10 LTS**), ghi chú riêng cho tính năng chỉ có ở bản mới.
- **Nền tảng**: đa nền tảng (Windows/macOS/Linux) toàn bộ sách, dùng `dotnet` CLI.
- **Phạm vi công nghệ**: chỉ .NET hiện đại — ASP.NET Core (Minimal API, Web API, MVC, Razor Pages, Blazor) + EF Core; không đề cập WinForms/Web Forms/.NET Framework.
- **CSDL mẫu**: SQLite (dễ chạy, không cần cài) cho ví dụ cơ bản; SQL Server ở chương nâng cao.
- **Thứ tự dạy OOP**: tách "Nhập môn" (Phần 2) và "Nâng cao" (Phần 3) như `java_book`.
