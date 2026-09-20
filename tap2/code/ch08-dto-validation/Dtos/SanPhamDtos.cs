using System.ComponentModel.DataAnnotations;
using ApiSanPham.Validation;

namespace ApiSanPham.Dtos;

// ---- DTO cho yeu cau tao moi: chi chua cac truong client duoc phep gui ----
public class TaoSanPhamRequest : IValidatableObject
{
    [Required(ErrorMessage = "Ten san pham la bat buoc")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ten phai tu 2 den 100 ky tu")]
    [KhongChuaTuCam("re rach", "hang gia", "lua dao")]
    public string? Ten { get; init; }

    [Required]
    [RegularExpression("^[A-Z]{2}[0-9]{3}$", ErrorMessage = "Ma phai co dang 2 chu HOA + 3 so, vi du LT001")]
    public string? Ma { get; init; }

    [Range(1_000, 1_000_000_000, ErrorMessage = "Gia phai tu 1.000 den 1 ty")]
    public decimal Gia { get; init; }

    [EmailAddress(ErrorMessage = "Email lien he khong dung dinh dang")]
    public string? EmailNhaCungCap { get; init; }

    [MaxLength(5, ErrorMessage = "Toi da 5 the")]
    public List<string> The { get; init; } = [];

    public DateOnly? KhuyenMaiTu { get; init; }
    public DateOnly? KhuyenMaiDen { get; init; }

    // Kiem tra CHEO nhieu truong: DataAnnotations don le khong lam duoc
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (KhuyenMaiTu is { } tu && KhuyenMaiDen is { } den && den < tu)
            yield return new ValidationResult("Ngay ket thuc khuyen mai phai sau ngay bat dau",
                [nameof(KhuyenMaiTu), nameof(KhuyenMaiDen)]);
    }
}

// ---- DTO cho phan hoi: chi lo ra nhung gi client can biet ----
public record SanPhamResponse(int Id, string Ma, string Ten, decimal Gia, IReadOnlyList<string> The);
