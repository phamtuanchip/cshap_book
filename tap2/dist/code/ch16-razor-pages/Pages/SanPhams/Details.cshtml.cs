using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebRazor.Data;

namespace WebRazor.Pages.SanPhams;

public class DetailsModel(CuaHangDb db) : PageModel
{
    public SanPham SanPham { get; private set; } = null!;

    // Tham so "id" lay tu route: @page "{id:int}"
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sp = await db.SanPhams.AsNoTracking().Include(s => s.Nhom).FirstOrDefaultAsync(s => s.Id == id);
        if (sp is null) return NotFound();
        SanPham = sp;
        return Page();
    }
}
