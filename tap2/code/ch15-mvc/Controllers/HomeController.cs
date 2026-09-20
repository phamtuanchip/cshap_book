using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebMvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Trang chủ";        // ViewData: dictionary truyen du lieu nho toi view
        ViewBag.ThoiGian = DateTime.Now;         // ViewBag: cach viet dong khac cua ViewData
        return View();                           // tim Views/Home/Index.cshtml
    }

    public IActionResult Loi()
    {
        ViewData["Title"] = "Lỗi";
        return View(model: Activity.Current?.Id ?? HttpContext.TraceIdentifier);   // model la chuoi traceId
    }
}
