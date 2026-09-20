using CuaHang.Data;
using CuaHang.Models;
using Microsoft.EntityFrameworkCore;

// Xoa CSDL cu roi ap dung migration -> luon bat dau tu trang thai sach
await using (var db = new CuaHangDbContext())
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();          // chay cac migration trong thu muc Migrations/
    Console.WriteLine("Da tao CSDL bang migration.\n");
}

// ============ 1. CREATE: Add + SaveChanges ============
Console.WriteLine("== 1. Them du lieu");
await using (var db = new CuaHangDbContext())
{
    db.SanPhams.Add(new SanPham { Ma = "CH001", Ten = "Chuot khong day", Gia = 150_000, Ton = 30, NhomId = 1 });
    db.SanPhams.AddRange(
        new SanPham { Ma = "BP001", Ten = "Ban phim co", Gia = 500_000, Ton = 12, NhomId = 1 },
        new SanPham { Ma = "MH001", Ten = "Man hinh 24 inch", Gia = 3_500_000, Ton = 5, NhomId = 2 },
        new SanPham { Ma = "LT001", Ten = "Laptop Dell", Gia = 18_000_000, Ton = 3, NhomId = 2 });

    int soDong = await db.SaveChangesAsync();   // MOI THAY DOI chi thuc su ghi xuong CSDL o day (trong 1 giao dich)
    Console.WriteLine($"  SaveChanges ghi {soDong} dong");
}

// ============ 2. READ: LINQ -> SQL ============
Console.WriteLine("\n== 2. Doc du lieu (LINQ)");
await using (var db = new CuaHangDbContext())
{
    var truyVan = db.SanPhams
        .Where(s => s.Gia >= 200_000)
        .OrderByDescending(s => s.Gia)
        .Select(s => new { s.Ma, s.Ten, s.Gia });

    Console.WriteLine("  SQL sinh ra:");
    foreach (var dong in truyVan.ToQueryString().Split('\n')) Console.WriteLine($"    {dong.TrimEnd()}");

    foreach (var s in await truyVan.ToListAsync())
        Console.WriteLine($"  {s.Ma} {s.Ten} {s.Gia:N0}");

    var mot = await db.SanPhams.FirstOrDefaultAsync(s => s.Ma == "BP001");
    var theoKhoa = await db.SanPhams.FindAsync(1);       // Find: tim theo khoa chinh (kiem tra bo nho truoc)
    Console.WriteLine($"  FirstOrDefault: {mot?.Ten}; Find(1): {theoKhoa?.Ten}");
    Console.WriteLine($"  Any/Count/Sum: {await db.SanPhams.AnyAsync(s => s.Ton == 0)}, {await db.SanPhams.CountAsync()}, {await db.SanPhams.SumAsync(s => s.Ton)}");
}

// ============ 3. UPDATE: change tracking ============
Console.WriteLine("\n== 3. Cap nhat (EF tu theo doi thay doi)");
await using (var db = new CuaHangDbContext())
{
    var sp = await db.SanPhams.SingleAsync(s => s.Ma == "CH001");   // "tracked": EF nho gia tri goc
    sp.Gia = 175_000;                                                // chi sua object trong bo nho...
    Console.WriteLine($"  Trang thai: {db.Entry(sp).State}");        // Modified
    await db.SaveChangesAsync();                                     // ...EF tu sinh UPDATE (chi cot Gia)
    Console.WriteLine($"  Sau SaveChanges: {db.Entry(sp).State}");   // Unchanged
}

// ============ 4. DELETE ============
Console.WriteLine("\n== 4. Xoa");
await using (var db = new CuaHangDbContext())
{
    var sp = await db.SanPhams.SingleAsync(s => s.Ma == "MH001");
    db.SanPhams.Remove(sp);
    await db.SaveChangesAsync();
    Console.WriteLine($"  Con lai {await db.SanPhams.CountAsync()} san pham");
}

// ============ 5. AsNoTracking: doc-only nhanh hon ============
Console.WriteLine("\n== 5. AsNoTracking");
await using (var db = new CuaHangDbContext())
{
    var ds = await db.SanPhams.AsNoTracking().ToListAsync();     // khong theo doi -> nhanh, it bo nho
    Console.WriteLine($"  Doc {ds.Count} dong, dang theo doi: {db.ChangeTracker.Entries().Count()} doi tuong");
    var ds2 = await db.SanPhams.ToListAsync();
    Console.WriteLine($"  Khong AsNoTracking -> theo doi: {db.ChangeTracker.Entries().Count()} doi tuong");
}

// ============ 6. Bulk: ExecuteUpdate / ExecuteDelete (khong nap du lieu len) ============
Console.WriteLine("\n== 6. Cap nhat hang loat");
await using (var db = new CuaHangDbContext())
{
    int n = await db.SanPhams.Where(s => s.NhomId == 1).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ton, x => x.Ton + 10));
    Console.WriteLine($"  Tang ton kho {n} san pham bang MOT cau UPDATE");
}

// ============ 7. Rang buoc CSDL thanh exception ============
Console.WriteLine("\n== 7. Vi pham rang buoc");
await using (var db = new CuaHangDbContext())
{
    db.SanPhams.Add(new SanPham { Ma = "CH001", Ten = "Trung ma", Gia = 1, NhomId = 1 });
    try { await db.SaveChangesAsync(); }
    catch (DbUpdateException e) { Console.WriteLine($"  DbUpdateException -> {e.InnerException?.Message}"); }
}

// ============ 8. Nhieu thay doi -> mot giao dich ============
Console.WriteLine("\n== 8. Giao dich ngam dinh cua SaveChanges");
await using (var db = new CuaHangDbContext())
{
    db.SanPhams.Add(new SanPham { Ma = "OK001", Ten = "Hop le", Gia = 10, NhomId = 1 });
    db.SanPhams.Add(new SanPham { Ma = "CH001", Ten = "Trung ma", Gia = 10, NhomId = 1 });   // se lam CA HAI that bai
    try { await db.SaveChangesAsync(); } catch (DbUpdateException) { }
}
await using (var db = new CuaHangDbContext())
    Console.WriteLine($"  OK001 co ton tai khong? {await db.SanPhams.AnyAsync(s => s.Ma == "OK001")} (rollback ca hai)");
