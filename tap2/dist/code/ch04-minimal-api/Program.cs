using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();                       // sinh tai lieu OpenAPI (JSON)
builder.Services.AddSingleton<TodoStore>();          // kho du lieu trong bo nho (chuong sau dung EF Core)

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();   // GET /openapi/v1.json

// Nhom route: chung tien to /api/todos, chung metadata
var todos = app.MapGroup("/api/todos").WithTags("Todos");

// GET /api/todos?done=true&tim=hoc
todos.MapGet("/", (TodoStore kho, bool? done, string? tim) =>
{
    var ds = kho.TatCa().AsEnumerable();
    if (done is not null) ds = ds.Where(t => t.Done == done);
    if (!string.IsNullOrWhiteSpace(tim)) ds = ds.Where(t => t.Title.Contains(tim, StringComparison.OrdinalIgnoreCase));
    return TypedResults.Ok(ds.ToList());
});

// GET /api/todos/5  -> 200 hoac 404 (kieu tra ve duoc khai bao ro cho OpenAPI)
todos.MapGet("/{id:int}", Results<Ok<Todo>, NotFound> (int id, TodoStore kho)
    => kho.Tim(id) is { } t ? TypedResults.Ok(t) : TypedResults.NotFound())
    .WithName("LayTodo");

// POST /api/todos  -> 201 Created + header Location
todos.MapPost("/", Results<Created<Todo>, ValidationProblem> (TaoTodoRequest req, TodoStore kho) =>
{
    var loi = req.KiemTra();
    if (loi.Count > 0) return TypedResults.ValidationProblem(loi);

    var moi = kho.Them(req.Title!.Trim());
    return TypedResults.Created($"/api/todos/{moi.Id}", moi);
});

// PUT /api/todos/5  -> 204 No Content, hoac 404
todos.MapPut("/{id:int}", Results<NoContent, NotFound, ValidationProblem> (int id, CapNhatTodoRequest req, TodoStore kho) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
        return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["Tieu de khong duoc rong"] });
    return kho.CapNhat(id, req.Title.Trim(), req.Done) ? TypedResults.NoContent() : TypedResults.NotFound();
});

// DELETE /api/todos/5
todos.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id, TodoStore kho)
    => kho.Xoa(id) ? TypedResults.NoContent() : TypedResults.NotFound());

// Cac cach lay du lieu tu request
app.MapGet("/vi-du/rang-buoc/{id:int}", (int id, [AsParameters] TimKiem tk, [FromHeader(Name = "X-Khach")] string? khach, HttpRequest req)
    => new { id, tk.Trang, tk.KichThuoc, khach, duongDan = req.Path.Value });

app.Run();

// -------- Mo hinh --------
record Todo(int Id, string Title, bool Done);

record TaoTodoRequest(string? Title)
{
    public Dictionary<string, string[]> KiemTra()
    {
        var loi = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Title)) loi["title"] = ["Tieu de khong duoc rong"];
        else if (Title.Length > 100) loi["title"] = ["Tieu de toi da 100 ky tu"];
        return loi;
    }
}

record CapNhatTodoRequest(string? Title, bool Done);

record TimKiem(int Trang = 1, int KichThuoc = 10);

// Kho in-memory: an toan da luong vi Singleton co the duoc goi dong thoi
class TodoStore
{
    private readonly Lock _khoa = new();
    private readonly List<Todo> _ds = [new(1, "Hoc C#", true), new(2, "Hoc ASP.NET Core", false)];
    private int _idKeTiep = 3;

    public List<Todo> TatCa() { lock (_khoa) return [.. _ds]; }
    public Todo? Tim(int id) { lock (_khoa) return _ds.FirstOrDefault(t => t.Id == id); }

    public Todo Them(string title)
    {
        lock (_khoa)
        {
            var t = new Todo(_idKeTiep++, title, false);
            _ds.Add(t);
            return t;
        }
    }

    public bool CapNhat(int id, string title, bool done)
    {
        lock (_khoa)
        {
            int vt = _ds.FindIndex(t => t.Id == id);
            if (vt < 0) return false;
            _ds[vt] = new Todo(id, title, done);
            return true;
        }
    }

    public bool Xoa(int id) { lock (_khoa) return _ds.RemoveAll(t => t.Id == id) > 0; }
}
