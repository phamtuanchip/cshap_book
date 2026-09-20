using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebRazor.Data;

namespace WebRazor.Pages.SanPhams;

public class EditModel(CuaHangDb db) : PageModel
{
    [BindProperty] public SanPhamInput Input { get; set; } = new();
    public SelectList DanhSachNhom { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sp = await db.SanPhams.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (sp is null) return NotFound();

        Input = new SanPhamInput { Ma = sp.Ma, Ten = sp.Ten, Gia = sp.Gia, Ton = sp.Ton, NhomId = sp.NhomId };
        await NapNhomAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (await db.SanPhams.AnyAsync(s => s.Ma == Input.Ma && s.Id != id))
            ModelState.AddModelError("Input.Ma", "Mã sản phẩm đã tồn tại");
        if (!ModelState.IsValid)
        {
            await NapNhomAsync();
            return Page();
        }

        var sp = await db.SanPhams.FindAsync(id);                 // tai entity that roi CHI gan cac truong cho phep
        if (sp is null) return NotFound();
        sp.Ma = Input.Ma!; sp.Ten = Input.Ten!.Trim(); sp.Gia = Input.Gia; sp.Ton = Input.Ton; sp.NhomId = Input.NhomId;
        await db.SaveChangesAsync();

        TempData["ThongBao"] = "Đã cập nhật";
        return RedirectToPage("Details", new { id });
    }

    private async Task NapNhomAsync()
        => DanhSachNhom = new SelectList(await db.Nhoms.AsNoTracking().OrderBy(n => n.Ten).ToListAsync(), nameof(Nhom.Id), nameof(Nhom.Ten), Input.NhomId);
}
