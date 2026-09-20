using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebForm.Services;
using WebForm.Validation;

namespace WebForm.Pages;

public class DangKyModel(NguoiDungStore store) : PageModel
{
    [BindProperty] public DangKyInput Input { get; set; } = new();

    public void OnGet() { }

    public IActionResult OnPost()
    {
        // 1. Validation dinh dang da chay tu dong (ModelState). 2. Kiem tra NGHIEP VU can du lieu:
        if (!string.IsNullOrWhiteSpace(Input.Email) && store.TonTaiEmail(Input.Email))
            ModelState.AddModelError("Input.Email", "Email này đã được đăng ký");

        if (!ModelState.IsValid)
            return Page();                                        // hien lai form, GIU du lieu da nhap (tru mat khau)

        var nd = store.Them(Input.HoTen!.Trim(), Input.Email!, Input.NgaySinh!.Value, Input.MatKhau!);

        // Post-Redirect-Get: sau POST thanh cong LUON redirect -> bam F5 khong gui lai form (khong tao 2 tai khoan)
        return RedirectToPage("ThanhCong", new { id = nd.Id });
    }
}

public class DangKyInput
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 80 ký tự")]
    [Display(Name = "Họ và tên")]
    public string? HoTen { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập ngày sinh")]
    [TuoiToiThieu(16)]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateOnly? NgaySinh { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 64 ký tự")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có cả chữ và số")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string? MatKhau { get; set; }

    [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu")]
    public string? XacNhan { get; set; }

    // Checkbox "phai tich": Range(true, true) la meo kinh dien
    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý điều khoản")]
    [Display(Name = "Tôi đồng ý điều khoản")]
    public bool DongY { get; set; }
}
