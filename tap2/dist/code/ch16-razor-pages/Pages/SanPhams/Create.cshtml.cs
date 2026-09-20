using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebRazor.Data;

namespace WebRazor.Pages.SanPhams;

public class CreateModel(CuaHangDb db) : PageModel
{
    // [BindProperty]: gan tu du lieu form khi POST. CHI bind kieu "Input", khong bind entity -> chong over-posting.
    [BindProperty] public SanPhamInput Input { get; set; } = new();

    public SelectList DanhSachNhom { get; private set; } = null!;

    public async Task OnGetAsync() => await NapNhomAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (await db.SanPhams.AnyAsync(s => s.Ma == Input.Ma))
            ModelState.AddModelError("Input.Ma", "Mã sản phẩm đã tồn tại");    // tien to "Input." vi property ten Input

        if (!ModelState.IsValid)
        {
            await NapNhomAsync();
            return Page();                                                       // hien lai form + loi + du lieu da nhap
        }

        var sp = new SanPham { Ma = Input.Ma!, Ten = Input.Ten!.Trim(), Gia = Input.Gia, Ton = Input.Ton, NhomId = Input.NhomId };
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        TempData["ThongBao"] = $"Đã thêm {sp.Ten}";
        return RedirectToPage("Details", new { id = sp.Id });                    // Post-Redirect-Get
    }

    private async Task NapNhomAsync()
        => DanhSachNhom = new SelectList(await db.Nhoms.AsNoTracking().OrderBy(n => n.Ten).ToListAsync(), nameof(Nhom.Id), nameof(Nhom.Ten), Input.NhomId);
}
