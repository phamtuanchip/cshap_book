using Microsoft.Data.Sqlite;

// Co so du lieu SQLite trong bo nho: bien mat khi dong ket noi (khong de lai file)
await using var conn = new SqliteConnection("Data Source=:memory:");
await conn.OpenAsync();

await ChayAsync(conn, await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql")));
Console.WriteLine("Da tao luoc do va nap du lieu mau.\n");

// 1. SELECT co dieu kien, sap xep, gioi han
await InAsync(conn, "1. San pham gia tu 200.000 tro len, dat nhat truoc",
    "SELECT ma, ten, gia FROM san_pham WHERE gia >= 200000 ORDER BY gia DESC LIMIT 3");

// 2. JOIN
await InAsync(conn, "2. INNER JOIN: san pham kem ten nhom",
    """
    SELECT sp.ma, sp.ten, n.ten AS nhom, sp.gia
    FROM san_pham sp
    JOIN nhom n ON n.id = sp.nhom_id
    ORDER BY n.ten, sp.ten
    """);

// 3. LEFT JOIN: khach hang KE CA khi chua co don
await InAsync(conn, "3. LEFT JOIN: so don cua moi khach (ke ca chua co)",
    """
    SELECT k.ten, COUNT(d.id) AS so_don
    FROM khach_hang k
    LEFT JOIN don_hang d ON d.khach_hang_id = k.id
    GROUP BY k.id
    ORDER BY so_don DESC
    """);

// 4. GROUP BY + HAVING
await InAsync(conn, "4. Doanh thu tung khach hang (chi khach co doanh thu > 1 trieu)",
    """
    SELECT k.ten, SUM(c.so_luong * c.don_gia) AS doanh_thu
    FROM khach_hang k
    JOIN don_hang d ON d.khach_hang_id = k.id
    JOIN chi_tiet_don c ON c.don_hang_id = d.id
    GROUP BY k.id
    HAVING SUM(c.so_luong * c.don_gia) > 1000000
    ORDER BY doanh_thu DESC
    """);

// 5. Truy van con
await InAsync(conn, "5. San pham chua tung duoc ban (NOT EXISTS)",
    """
    SELECT ma, ten FROM san_pham sp
    WHERE NOT EXISTS (SELECT 1 FROM chi_tiet_don c WHERE c.san_pham_id = sp.id)
    """);

// 6. THAM SO HOA - cach duy nhat dung de dua du lieu nguoi dung vao SQL
Console.WriteLine("== 6. Truy van co tham so");
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT ten, gia FROM san_pham WHERE ten LIKE $tuKhoa AND gia <= $giaToiDa";
    cmd.Parameters.AddWithValue("$tuKhoa", "%an%");
    cmd.Parameters.AddWithValue("$giaToiDa", 1_000_000);
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        Console.WriteLine($"  {r.GetString(0)} - {r.GetInt64(1):N0}");
}

// 7. SQL INJECTION: minh hoa loi va cach phong
Console.WriteLine("\n== 7. SQL injection");
string dauVaoDocHai = "x' OR '1'='1";
string sqlNguyHiem = $"SELECT COUNT(*) FROM khach_hang WHERE email = '{dauVaoDocHai}'";   // NOI CHUOI: LO HONG
Console.WriteLine($"  SQL sinh ra (SAI): {sqlNguyHiem}");
Console.WriteLine($"  Ket qua: {await DemAsync(conn, sqlNguyHiem)} khach (dang le phai la 0!)");

await using (var an = conn.CreateCommand())
{
    an.CommandText = "SELECT COUNT(*) FROM khach_hang WHERE email = $email";                // THAM SO: AN TOAN
    an.Parameters.AddWithValue("$email", dauVaoDocHai);
    Console.WriteLine($"  Voi tham so (DUNG): {await an.ExecuteScalarAsync()} khach");
}

// 8. INSERT / UPDATE / DELETE: so dong bi anh huong
Console.WriteLine("\n== 8. Ghi du lieu");
Console.WriteLine($"  UPDATE: {await LenhAsync(conn, "UPDATE san_pham SET ton = ton + 10 WHERE ma = $ma", ("$ma", "TN001"))} dong");
Console.WriteLine($"  INSERT: {await LenhAsync(conn, "INSERT INTO khach_hang (ten, email) VALUES ($ten, $email)", ("$ten", "Pham Dung"), ("$email", "dung@example.com"))} dong");
Console.WriteLine($"  DELETE: {await LenhAsync(conn, "DELETE FROM khach_hang WHERE email = $email", ("$email", "dung@example.com"))} dong");

// 9. Rang buoc bao ve du lieu
Console.WriteLine("\n== 9. Vi pham rang buoc -> loi tu CSDL");
foreach (var (mota, sql) in new[]
{
    ("Trung ma (UNIQUE)", "INSERT INTO san_pham (ma, ten, gia, ton, nhom_id) VALUES ('CH001','x',1,1,1)"),
    ("Gia am (CHECK)", "INSERT INTO san_pham (ma, ten, gia, ton, nhom_id) VALUES ('ZZ001','x',-5,1,1)"),
    ("Nhom khong ton tai (FOREIGN KEY)", "INSERT INTO san_pham (ma, ten, gia, ton, nhom_id) VALUES ('ZZ002','x',1,1,999)"),
})
{
    try { await LenhAsync(conn, sql); }
    catch (SqliteException e) { Console.WriteLine($"  {mota}: {e.Message}"); }
}

// 10. Giao dich (transaction): tat ca hoac khong gi ca
Console.WriteLine("\n== 10. Transaction");
await using (var tx = (SqliteTransaction)await conn.BeginTransactionAsync())
{
    try
    {
        await LenhTrongGiaoDichAsync(conn, tx, "UPDATE san_pham SET ton = ton - 1 WHERE ma = 'LT001'");
        await LenhTrongGiaoDichAsync(conn, tx, "INSERT INTO chi_tiet_don VALUES (999, 5, 1, 1)");   // don_hang 999 khong ton tai -> loi
        await tx.CommitAsync();
    }
    catch (SqliteException e)
    {
        await tx.RollbackAsync();
        Console.WriteLine($"  Loi: {e.Message} -> ROLLBACK");
    }
}
Console.WriteLine($"  Ton LT001 sau rollback: {await ScalarAsync(conn, "SELECT ton FROM san_pham WHERE ma = 'LT001'")} (van la 3)");

// 11. Chi muc va ke hoach thuc thi
Console.WriteLine("\n== 11. EXPLAIN QUERY PLAN");
await InAsync(conn, "Tim theo nhom_id (co chi muc ix_san_pham_nhom)", "EXPLAIN QUERY PLAN SELECT * FROM san_pham WHERE nhom_id = 1");
await InAsync(conn, "Tim theo ten (khong co chi muc -> quet toan bang)", "EXPLAIN QUERY PLAN SELECT * FROM san_pham WHERE ten = 'Ban phim co'");

// ---------------- ham ho tro ----------------
static async Task ChayAsync(SqliteConnection c, string sql)
{
    await using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

static async Task<int> LenhAsync(SqliteConnection c, string sql, params (string Ten, object Gia)[] thamSo)
{
    await using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    foreach (var (ten, gia) in thamSo) cmd.Parameters.AddWithValue(ten, gia);
    return await cmd.ExecuteNonQueryAsync();
}

static async Task<int> LenhTrongGiaoDichAsync(SqliteConnection c, SqliteTransaction tx, string sql)
{
    await using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    cmd.Transaction = tx;
    return await cmd.ExecuteNonQueryAsync();
}

static async Task<object?> ScalarAsync(SqliteConnection c, string sql)
{
    await using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    return await cmd.ExecuteScalarAsync();
}

static async Task<long> DemAsync(SqliteConnection c, string sql) => Convert.ToInt64(await ScalarAsync(c, sql));

static async Task InAsync(SqliteConnection c, string tieuDe, string sql)
{
    Console.WriteLine($"== {tieuDe}");
    await using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    await using var r = await cmd.ExecuteReaderAsync();
    var cot = Enumerable.Range(0, r.FieldCount).Select(r.GetName).ToArray();
    var dong = new List<string[]>();
    while (await r.ReadAsync())
        dong.Add(Enumerable.Range(0, r.FieldCount).Select(i => r.IsDBNull(i) ? "NULL" : Convert.ToString(r.GetValue(i))!).ToArray());
    var rong = cot.Select((t, i) => Math.Max(t.Length, dong.Count == 0 ? 0 : dong.Max(d => d[i].Length))).ToArray();
    Console.WriteLine("  " + string.Join(" | ", cot.Select((t, i) => t.PadRight(rong[i]))));
    Console.WriteLine("  " + string.Join("-+-", rong.Select(w => new string('-', w))));
    foreach (var d in dong) Console.WriteLine("  " + string.Join(" | ", d.Select((v, i) => v.PadRight(rong[i]))));
    Console.WriteLine();
}
