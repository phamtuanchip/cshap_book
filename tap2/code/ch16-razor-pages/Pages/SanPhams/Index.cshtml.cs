using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebRazor.Data;

namespace WebRazor.Pages.SanPhams;

// PageModel = "code-behind" cua trang. DI qua constructor nhu moi lop khac.
public class IndexModel(CuaHangDb db) : PageModel
{
    private const int KichThuocTrang = 3;

    // SupportsGet = true: cho phep gan tu QUERY STRING khi GET (mac dinh [BindProperty] chi gan khi POST)
    [BindProperty(SupportsGet = true)] public string? TuKhoa { get; set; }
    [BindProperty(SupportsGet = true)] public int Trang { get; set; } = 1;

    public List<SanPham> SanPhams { get; private set; } = [];
    public int TongTrang { get; private set; } = 1;

    // Handler mac dinh cho GET
    public async Task OnGetAsync()
    {
        Trang = Math.Max(1, Trang);
        var q = db.SanPhams.AsNoTracking().Include(s => s.Nhom).AsQueryable();
        if (!string.IsNullOrWhiteSpace(TuKhoa))
            q = q.Where(s => EF.Functions.Like(s.Ten, $"%{TuKhoa}%") || EF.Functions.Like(s.Ma, $"%{TuKhoa}%"));

        int tongSo = await q.CountAsync();
        TongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)KichThuocTrang));
        SanPhams = await q.OrderBy(s => s.Id).Skip((Trang - 1) * KichThuocTrang).Take(KichThuocTrang).ToListAsync();
    }

    // Handler co ten: <button asp-page-handler="Xoa" asp-route-id="..."> goi OnPostXoaAsync (POST /SanPham?handler=Xoa&id=..)
    public async Task<IActionResult> OnPostXoaAsync(int id)
    {
        int n = await db.SanPhams.Where(s => s.Id == id).ExecuteDeleteAsync();
        TempData["ThongBao"] = n > 0 ? "Đã xoá sản phẩm" : "Không tìm thấy sản phẩm";
        return RedirectToPage(new { TuKhoa, Trang });             // Post-Redirect-Get
    }
}
