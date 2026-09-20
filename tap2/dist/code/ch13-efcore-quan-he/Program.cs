using CuaHang.Data;
using Microsoft.EntityFrameworkCore;

// ---------- Khoi tao va nap du lieu ----------
await using (var db = new CuaHangDbContext())
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();          // (thuc te dung Migrate - Chuong 12)

    var phuKien = new Nhom { Ten = "Phu kien" };
    var thietBi = new Nhom { Ten = "Thiet bi" };
    var theRe = new The { Ten = "gia-re" };
    var theMoi = new The { Ten = "moi" };

    var chuot = new SanPham { Ma = "CH001", Ten = "Chuot", Gia = 150_000, Ton = 30, Nhom = phuKien, The = [theRe, theMoi] };
    var banPhim = new SanPham { Ma = "BP001", Ten = "Ban phim", Gia = 500_000, Ton = 12, Nhom = phuKien, The = [theMoi] };
    var manHinh = new SanPham { Ma = "MH001", Ten = "Man hinh", Gia = 3_500_000, Ton = 5, Nhom = thietBi };
    var cu = new SanPham { Ma = "OLD01", Ten = "Hang ngung ban", Gia = 1, Ton = 0, Nhom = thietBi, DaXoa = true };
    db.SanPhams.AddRange(chuot, banPhim, manHinh, cu);

    var an = new KhachHang { Ten = "An", DiaChi = new DiaChi { Duong = "1 Le Loi", ThanhPho = "Ha Noi" } };
    var binh = new KhachHang { Ten = "Binh", DiaChi = new DiaChi { Duong = "2 Nguyen Hue", ThanhPho = "TP HCM" } };
    var chi = new KhachHang { Ten = "Chi" };
    db.KhachHangs.AddRange(an, binh, chi);

    for (int i = 1; i <= 20; i++)                    // 20 don hang cho an, moi don 2 dong chi tiet
    {
        db.DonHangs.Add(new DonHang
        {
            KhachHang = i <= 12 ? an : binh,
            Ngay = new DateOnly(2026, 9, i),
            ChiTiets =
            [
                new ChiTietDon { SanPham = chuot, SoLuong = i, DonGia = chuot.Gia },
                new ChiTietDon { SanPham = manHinh, SoLuong = 1, DonGia = manHinh.Gia },
            ],
        });
    }
    await db.SaveChangesAsync();
}

// ============ 1. Nhac lai: N+1 la gi? ============
Console.WriteLine("== 1. Tai du lieu lien quan: 4 cach voi so cau SQL khac nhau");

// Cach 1 (SAI): N+1 - mot cau lay danh sach, roi moi phan tu THEM mot cau nua
await using (var db = new CuaHangDbContext())
{
    CuaHangDbContext.SoCauLenh = 0;
    var ds = await db.DonHangs.ToListAsync();                                  // 1 cau
    foreach (var d in ds)
        await db.Entry(d).Collection(x => x.ChiTiets).LoadAsync();              // + 20 cau (explicit loading)
    Console.WriteLine($"  Explicit loading trong vong lap (N+1): {CuaHangDbContext.SoCauLenh} cau SQL");
}

// Cach 2: Include - JOIN mot lan
await using (var db = new CuaHangDbContext())
{
    CuaHangDbContext.SoCauLenh = 0;
    var ds = await db.DonHangs.Include(d => d.ChiTiets).ThenInclude(c => c.SanPham).ToListAsync();
    Console.WriteLine($"  Include + ThenInclude: {CuaHangDbContext.SoCauLenh} cau SQL, {ds.Count} don, {ds.Sum(d => d.ChiTiets.Count)} dong chi tiet");
}

// Cach 3: Split query - tach thanh vai cau don gian thay vi 1 JOIN khong lo (tranh "cartesian explosion")
await using (var db = new CuaHangDbContext())
{
    CuaHangDbContext.SoCauLenh = 0;
    var ds = await db.DonHangs.Include(d => d.ChiTiets).AsSplitQuery().ToListAsync();
    Console.WriteLine($"  AsSplitQuery: {CuaHangDbContext.SoCauLenh} cau SQL");
}

// Cach 4 (TOT NHAT cho doc): Projection - chi lay dung cot can, EF tu JOIN/tinh trong SQL
await using (var db = new CuaHangDbContext())
{
    CuaHangDbContext.SoCauLenh = 0;
    var tong = await db.DonHangs
        .Select(d => new
        {
            d.Id,
            KhachHang = d.KhachHang.Ten,
            SoDong = d.ChiTiets.Count,
            TongTien = d.ChiTiets.Sum(c => c.SoLuong * c.DonGia),
        })
        .OrderByDescending(x => x.TongTien)
        .Take(3)
        .ToListAsync();
    Console.WriteLine($"  Projection: {CuaHangDbContext.SoCauLenh} cau SQL");
    foreach (var t in tong) Console.WriteLine($"    Don {t.Id} ({t.KhachHang}): {t.SoDong} dong, {t.TongTien:N0}");
}

// ============ 2. Nhieu - nhieu ============
Console.WriteLine("\n== 2. Nhieu-nhieu (SanPham <-> The)");
await using (var db = new CuaHangDbContext())
{
    var sp = await db.SanPhams.Include(s => s.The).OrderBy(s => s.Ma).ToListAsync();
    foreach (var s in sp) Console.WriteLine($"  {s.Ma}: [{string.Join(", ", s.The.Select(t => t.Ten))}]");
    var coTheMoi = await db.SanPhams.Where(s => s.The.Any(t => t.Ten == "moi")).Select(s => s.Ma).ToListAsync();
    Console.WriteLine($"  San pham co the 'moi': {string.Join(", ", coTheMoi)}");
}

// ============ 3. Owned type ============
Console.WriteLine("\n== 3. Value object (owned type)");
await using (var db = new CuaHangDbContext())
{
    var ds = await db.KhachHangs.OrderBy(k => k.Id).ToListAsync();
    foreach (var k in ds) Console.WriteLine($"  {k.Ten}: {(k.DiaChi is null ? "(chua co)" : $"{k.DiaChi.Duong}, {k.DiaChi.ThanhPho}")}");
    Console.WriteLine($"  Khach o Ha Noi: {await db.KhachHangs.CountAsync(k => k.DiaChi!.ThanhPho == "Ha Noi")}");
}

// ============ 4. Global query filter (xoa mem) ============
Console.WriteLine("\n== 4. Query filter (xoa mem)");
await using (var db = new CuaHangDbContext())
{
    Console.WriteLine($"  Mac dinh: {await db.SanPhams.CountAsync()} san pham (an OLD01)");
    Console.WriteLine($"  IgnoreQueryFilters: {await db.SanPhams.IgnoreQueryFilters().CountAsync()} san pham");
}

// ============ 5. Xung dot dong thoi (optimistic concurrency) ============
Console.WriteLine("\n== 5. Xung dot dong thoi");
await using (var a = new CuaHangDbContext())
await using (var b = new CuaHangDbContext())
{
    var spA = await a.SanPhams.SingleAsync(s => s.Ma == "CH001");
    var spB = await b.SanPhams.SingleAsync(s => s.Ma == "CH001");         // hai nguoi cung doc ban ghi

    spA.Ton -= 5; spA.PhienBan++;
    await a.SaveChangesAsync();                                              // nguoi A luu truoc: OK
    Console.WriteLine("  Nguoi A luu thanh cong");

    spB.Ton -= 3; spB.PhienBan++;
    try { await b.SaveChangesAsync(); }                                      // nguoi B: du lieu goc da doi
    catch (DbUpdateConcurrencyException) { Console.WriteLine("  Nguoi B: DbUpdateConcurrencyException -> phai tai lai va thu lai"); }
}

// ============ 6. SQL thuan khi can ============
Console.WriteLine("\n== 6. SQL thuan (van tham so hoa)");
await using (var db = new CuaHangDbContext())
{
    decimal giaToiThieu = 200_000;
    var ds = await db.SanPhams
        .FromSqlInterpolated($"SELECT * FROM SanPhams WHERE Gia >= {giaToiThieu} ORDER BY Gia")   // {giaToiThieu} tro thanh THAM SO, khong noi chuoi
        .ToListAsync();
    Console.WriteLine($"  {string.Join(", ", ds.Select(s => s.Ma))}");
}

// ============ 7. Phan trang ============
Console.WriteLine("\n== 7. Phan trang");
await using (var db = new CuaHangDbContext())
{
    const int kichThuoc = 5;
    int tongSo = await db.DonHangs.CountAsync();
    Console.WriteLine($"  Tong {tongSo} don, {(int)Math.Ceiling(tongSo / (double)kichThuoc)} trang");

    // Offset: Skip/Take (don gian, cham dan o trang sau)
    var trang3 = await db.DonHangs.OrderBy(d => d.Id).Skip(2 * kichThuoc).Take(kichThuoc).Select(d => d.Id).ToListAsync();
    Console.WriteLine($"  Trang 3 (offset): {string.Join(",", trang3)}");

    // Keyset (cursor): "sau khoa cuoi cung da xem" - on dinh va nhanh o moi trang
    int idCuoi = trang3[^1];
    var trangKe = await db.DonHangs.Where(d => d.Id > idCuoi).OrderBy(d => d.Id).Take(kichThuoc).Select(d => d.Id).ToListAsync();
    Console.WriteLine($"  Trang ke tiep (keyset): {string.Join(",", trangKe)}");
}
