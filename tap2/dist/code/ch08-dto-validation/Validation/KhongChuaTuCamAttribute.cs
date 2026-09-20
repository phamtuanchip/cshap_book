using System.ComponentModel.DataAnnotations;

namespace ApiSanPham.Validation;

// Attribute validation tu viet (Tap 1, Chuong 37: attribute + reflection): dung lai duoc o moi DTO
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class KhongChuaTuCamAttribute(params string[] tuCam) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is string s)
        {
            foreach (var tu in tuCam)
                if (s.Contains(tu, StringComparison.OrdinalIgnoreCase))
                    return new ValidationResult($"{ctx.DisplayName} khong duoc chua tu '{tu}'", [ctx.MemberName!]);
        }
        return ValidationResult.Success;
    }
}
