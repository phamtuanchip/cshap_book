using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebForm.Services;

namespace WebForm.Pages;

public class ThanhCongModel(NguoiDungStore store) : PageModel
{
    public int Id { get; private set; }
    public string HoTen { get; private set; } = "";

    public IActionResult OnGet(int id)
    {
        var nd = store.Lay(id);
        if (nd is null) return NotFound();
        Id = nd.Id;
        HoTen = nd.HoTen;
        return Page();
    }
}
