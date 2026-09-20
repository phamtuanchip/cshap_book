using ApiSanPham.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ApiSanPham.Controllers;

[ApiController]
[Route("api/san-pham")]
public class SanPhamsController(KhoSanPham kho) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<SanPhamResponse>> LayTatCa()
        => Ok(kho.TatCa().Select(s => s.ToResponse()));

    [HttpGet("{id:int}")]
    public ActionResult<SanPhamResponse> LayTheoId(int id)
        => kho.Tim(id) is { } s ? Ok(s.ToResponse()) : NotFound();

    // Voi [ApiController], neu request khong hop le, action NAY KHONG DUOC GOI: framework tra 400 ngay.
    [HttpPost]
    public ActionResult<SanPhamResponse> Tao(TaoSanPhamRequest req)
    {
        // Rang buoc nghiep vu (can du lieu): khong the khai bao bang attribute
        if (kho.TonTaiMa(req.Ma!))
        {
            ModelState.AddModelError(nameof(req.Ma), $"Ma {req.Ma} da ton tai");
            return ValidationProblem(ModelState);    // 400 cung dinh dang
        }

        var moi = kho.Them(req.ToEntity());
        return CreatedAtAction(nameof(LayTheoId), new { id = moi.Id }, moi.ToResponse());
    }
}
