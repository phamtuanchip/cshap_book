using Microsoft.AspNetCore.Mvc;
using WebMvc.Models;

namespace WebMvc.Controllers;

// Route quy uoc: /SanPham/{action}/{id?}
public class SanPhamController(SanPhamStore store) : Controller
{
    private const int KichThuocTrang = 3;

    // GET /SanPham?tuKhoa=laptop&trang=1
    public IActionResult Index(string? tuKhoa, int trang = 1)
    {
        trang = Math.Max(1, trang);
        var (muc, tongSo) = store.Tim(tuKhoa, trang, KichThuocTrang);
        var vm = new DanhSachSanPhamViewModel
        {
            TuKhoa = tuKhoa,
            Trang = trang,
            TongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)KichThuocTrang)),
            SanPhams = muc,
        };
        return View(vm);                            // Views/SanPham/Index.cshtml, model = vm
    }

    // GET /SanPham/Details/2
    public IActionResult Details(int id)
        => store.Lay(id) is { } sp ? View(sp) : NotFound();

    // GET /SanPham/Create  -> hien form rong
    public IActionResult Create() => View("Form", new SanPhamForm());

    // POST /SanPham/Create -> xu ly form
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(SanPhamForm form)
    {
        if (store.TonTaiMa(form.Ma ?? ""))
            ModelState.AddModelError(nameof(form.Ma), "Mã sản phẩm đã tồn tại");   // loi nghiep vu -> gan vao o "Ma"

        if (!ModelState.IsValid)
            return View("Form", form);               // hien lai form KEM thong bao loi va du lieu da nhap

        var sp = store.Them(form);
        TempData["ThongBao"] = $"Đã thêm sản phẩm {sp.Ten}";     // hien 1 lan sau redirect
        return RedirectToAction(nameof(Details), new { id = sp.Id });    // Post-Redirect-Get (Chuong 17)
    }

    public IActionResult Edit(int id)
    {
        var sp = store.Lay(id);
        if (sp is null) return NotFound();
        return View("Form", new SanPhamForm { Id = sp.Id, Ma = sp.Ma, Ten = sp.Ten, Gia = sp.Gia, Ton = sp.Ton });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, SanPhamForm form)
    {
        if (store.TonTaiMa(form.Ma ?? "", boQuaId: id))
            ModelState.AddModelError(nameof(form.Ma), "Mã sản phẩm đã tồn tại");
        if (!ModelState.IsValid) return View("Form", form);
        if (!store.Sua(id, form)) return NotFound();

        TempData["ThongBao"] = "Đã cập nhật";
        return RedirectToAction(nameof(Details), new { id });
    }

    public IActionResult Delete(int id)
        => store.Lay(id) is { } sp ? View(sp) : NotFound();       // trang xac nhan xoa

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        store.Xoa(id);
        TempData["ThongBao"] = "Đã xoá sản phẩm";
        return RedirectToAction(nameof(Index));
    }
}
