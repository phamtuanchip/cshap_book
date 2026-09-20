using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace QuanLyKhoWeb.Services;

public static class VaiTro
{
    public const string Admin = "Admin";
    public const string NhanVien = "NhanVien";
}

public class JwtOptions
{
    public string Khoa { get; set; } = "";
    public string PhatHanh { get; set; } = "quanlykho-api";
    public string DoiTuong { get; set; } = "quanlykho-client";
    public int PhutHetHan { get; set; } = 30;
}

public record TaiKhoan(string Ten, string[] VaiTro, string MatKhauBam);

// Tap 3 se thay bang ASP.NET Core Identity / nha cung cap danh tinh; day de tap trung vao API
public class TaiKhoanService
{
    private readonly PasswordHasher<object> _bam = new();
    private readonly List<TaiKhoan> _ds;

    public TaiKhoanService() => _ds =
    [
        new("admin", [VaiTro.Admin, VaiTro.NhanVien], _bam.HashPassword(new object(), "Admin@123")),
        new("nhanvien", [VaiTro.NhanVien], _bam.HashPassword(new object(), "NhanVien@123")),
    ];

    public TaiKhoan? XacThuc(string ten, string matKhau)
    {
        var tk = _ds.FirstOrDefault(t => string.Equals(t.Ten, ten, StringComparison.OrdinalIgnoreCase));
        if (tk is null) { _bam.HashPassword(new object(), matKhau); return null; }      // thoi gian dong deu
        return _bam.VerifyHashedPassword(new object(), tk.MatKhauBam, matKhau) != PasswordVerificationResult.Failed ? tk : null;
    }
}

public class PhatHanhToken(Microsoft.Extensions.Options.IOptions<JwtOptions> opt, TimeProvider dongHo)
{
    public (string Token, DateTimeOffset HetHan) Tao(TaiKhoan tk)
    {
        var o = opt.Value;
        var claims = new List<Claim> { new(ClaimTypes.Name, tk.Ten) };
        claims.AddRange(tk.VaiTro.Select(v => new Claim(ClaimTypes.Role, v)));

        var het = dongHo.GetUtcNow().AddMinutes(o.PhutHetHan);
        var jwt = new JwtSecurityToken(o.PhatHanh, o.DoiTuong, claims, notBefore: dongHo.GetUtcNow().UtcDateTime, expires: het.UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Khoa)), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), het);
    }
}
