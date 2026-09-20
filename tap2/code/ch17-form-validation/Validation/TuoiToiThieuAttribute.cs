using System.ComponentModel.DataAnnotations;

namespace WebForm.Validation;

// Validation attribute nhan tham so, doc duoc tu DateOnly?
[AttributeUsage(AttributeTargets.Property)]
public sealed class TuoiToiThieuAttribute(int tuoi) : ValidationAttribute($"Bạn phải từ {tuoi} tuổi trở lên")
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is null) return ValidationResult.Success;             // "bat buoc" la viec cua [Required]
        if (value is not DateOnly ngaySinh) return new ValidationResult("Ngày sinh không hợp lệ");

        var homNay = DateOnly.FromDateTime(DateTime.Today);
        int tuoiHienTai = homNay.Year - ngaySinh.Year - (homNay < ngaySinh.AddYears(homNay.Year - ngaySinh.Year) ? 1 : 0);
        return tuoiHienTai >= tuoi ? ValidationResult.Success : new ValidationResult(ErrorMessage, [ctx.MemberName!]);
    }
}
