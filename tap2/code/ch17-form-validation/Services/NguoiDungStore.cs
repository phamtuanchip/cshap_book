using Microsoft.AspNetCore.Identity;

namespace WebForm.Services;

public record NguoiDung(int Id, string HoTen, string Email, DateOnly NgaySinh, string MatKhauBam);

public class NguoiDungStore
{
    private readonly Lock _khoa = new();
    private readonly List<NguoiDung> _ds = [];
    private readonly PasswordHasher<object> _bam = new();       // PBKDF2 + muoi ngau nhien, do Microsoft duy tri
    private int _id = 1;

    public bool TonTaiEmail(string email)
    {
        lock (_khoa) return _ds.Any(n => string.Equals(n.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public NguoiDung Them(string hoTen, string email, DateOnly ngaySinh, string matKhau)
    {
        lock (_khoa)
        {
            var nd = new NguoiDung(_id++, hoTen, email.Trim().ToLowerInvariant(), ngaySinh, _bam.HashPassword(new object(), matKhau));
            _ds.Add(nd);
            return nd;
        }
    }

    public NguoiDung? Lay(int id) { lock (_khoa) return _ds.FirstOrDefault(n => n.Id == id); }
}

// Kiem tra "chu ky" (magic bytes) de biet tep co PHAI anh khong, thay vi tin duoi tep
public static class KiemTraDauTep
{
    public static bool LaAnh(IFormFile tep)
    {
        Span<byte> dau = stackalloc byte[8];
        using var s = tep.OpenReadStream();
        int n = s.Read(dau);
        if (n < 4) return false;
        bool png = dau[0] == 0x89 && dau[1] == 0x50 && dau[2] == 0x4E && dau[3] == 0x47;
        bool jpg = dau[0] == 0xFF && dau[1] == 0xD8 && dau[2] == 0xFF;
        return png || jpg;
    }
}
