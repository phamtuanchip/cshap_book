using CuaHangApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CuaHangApi.Endpoints;

public static class SanPhamEndpoints
{
    public static IEndpointRouteBuilder MapSanPham(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/san-pham").WithTags("San pham");

        g.MapGet("/", LayDanhSach);
        g.MapGet("/{id:int}", LayTheoId).WithName("LaySanPham");
        g.MapPost("/", Tao).AddEndpointFilter<KiemTraHopLeFilter<TaoSanPhamRequest>>();
        g.MapPut("/{id:int}", CapNhat).AddEndpointFilter<KiemTraHopLeFilter<CapNhatSanPhamRequest>>();
        g.MapDelete("/{id:int}", Xoa);
        g.MapPost("/{id:int}/nhap-kho", NhapKho).AddEndpointFilter<KiemTraHopLeFilter<NhapKhoRequest>>();

        app.MapGet("/api/nhom", LayNhom).WithTags("San pham");
        return app;
    }

    // GET /api/san-pham?tim=chuot&nhomId=1&sapXep=-gia&trang=1&kichThuoc=10
    static async Task<Ok<TrangKetQua<SanPhamDto>>> LayDanhSach(
        CuaHangDbContext db, string? tim, int? nhomId, string? sapXep, CancellationToken ct, int trang = 1, int kichThuoc = 10)
    {
        trang = Math.Max(1, trang);
        kichThuoc = Math.Clamp(kichThuoc, 1, 100);                  // luon gioi han: khong cho client doi "tat ca"

        IQueryable<SanPham> q = db.SanPhams.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tim))
            // EF.Functions.Like: khong phan biet hoa/thuong (ASCII) tren SQLite; Contains() dich sang instr() PHAN BIET hoa/thuong
            q = q.Where(s => EF.Functions.Like(s.Ten, $"%{tim}%") || EF.Functions.Like(s.Ma, $"%{tim}%"));
        if (nhomId is not null)
            q = q.Where(s => s.NhomId == nhomId);

        q = sapXep switch                                           // whitelist cac cot duoc phep sap xep
        {
            "gia" => q.OrderBy(s => s.Gia),
            "-gia" => q.OrderByDescending(s => s.Gia),
            "ten" => q.OrderBy(s => s.Ten),
            _ => q.OrderBy(s => s.Id),
        };

        int tongSo = await q.CountAsync(ct);
        var muc = await q.Skip((trang - 1) * kichThuoc).Take(kichThuoc)
            .Select(s => new SanPhamDto(s.Id, s.Ma, s.Ten, s.Gia, s.Ton, s.Nhom.Ten))   // projection: chi lay cot can
            .ToListAsync(ct);

        return TypedResults.Ok(new TrangKetQua<SanPhamDto>(muc, trang, kichThuoc, tongSo));
    }

    static async Task<Results<Ok<SanPhamDto>, NotFound>> LayTheoId(int id, CuaHangDbContext db, CancellationToken ct)
    {
        var dto = await db.SanPhams.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new SanPhamDto(s.Id, s.Ma, s.Ten, s.Gia, s.Ton, s.Nhom.Ten))
            .FirstOrDefaultAsync(ct);
        return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
    }

    static async Task<Results<Created<SanPhamDto>, Conflict<string>, NotFound<string>>> Tao(
        TaoSanPhamRequest req, CuaHangDbContext db, CancellationToken ct)
    {
        var nhom = await db.Nhoms.FindAsync([req.NhomId], ct);
        if (nhom is null) return TypedResults.NotFound($"Khong tim thay nhom {req.NhomId}");

        var sp = new SanPham { Ma = req.Ma!, Ten = req.Ten!.Trim(), Gia = req.Gia, Ton = req.Ton, Nhom = nhom };
        db.SanPhams.Add(sp);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)                                   // vi pham UNIQUE(Ma) - CSDL la nguoi canh gac cuoi cung
        {
            return TypedResults.Conflict($"Ma san pham {req.Ma} da ton tai");
        }

        var dto = new SanPhamDto(sp.Id, sp.Ma, sp.Ten, sp.Gia, sp.Ton, nhom.Ten);
        return TypedResults.Created($"/api/san-pham/{sp.Id}", dto);
    }

    static async Task<Results<NoContent, NotFound>> CapNhat(int id, CapNhatSanPhamRequest req, CuaHangDbContext db, CancellationToken ct)
    {
        var sp = await db.SanPhams.FindAsync([id], ct);
        if (sp is null) return TypedResults.NotFound();

        sp.Ten = req.Ten!.Trim();
        sp.Gia = req.Gia;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    // Xoa MEM: giu lai du lieu (don hang cu van tham chieu duoc), query filter se an di
    static async Task<Results<NoContent, NotFound>> Xoa(int id, CuaHangDbContext db, CancellationToken ct)
    {
        int n = await db.SanPhams.Where(s => s.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.DaXoa, true), ct);
        return n == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    static async Task<Results<Ok<SanPhamDto>, NotFound>> NhapKho(int id, NhapKhoRequest req, CuaHangDbContext db, CancellationToken ct)
    {
        // ExecuteUpdate: mot cau UPDATE nguyen tu ("Ton = Ton + n"), khong doc-roi-ghi -> khong lo xung dot
        int n = await db.SanPhams.Where(s => s.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ton, x => x.Ton + req.SoLuong), ct);
        if (n == 0) return TypedResults.NotFound();
        return await LayTheoId(id, db, ct) is { Result: Ok<SanPhamDto> ok } ? ok : TypedResults.NotFound();
    }

    static async Task<Ok<List<NhomDto>>> LayNhom(CuaHangDbContext db, CancellationToken ct)
        => TypedResults.Ok(await db.Nhoms.AsNoTracking()
            .Select(n => new NhomDto(n.Id, n.Ten, n.SanPhams.Count, n.SanPhams.Sum(s => s.Ton)))
            .ToListAsync(ct));
}
