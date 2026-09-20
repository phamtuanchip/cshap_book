using System.Text.RegularExpressions;
using Kho.Domain.Common;

namespace Kho.Domain.SanPhams;

// Value object: KHONG co danh tinh, bat bien, so sanh theo GIA TRI, tu bao dam hop le.
// Tao qua factory tra Result: khong ton tai "ma san pham khong hop le" trong he thong.
public sealed partial record MaSanPham
{
    public string GiaTri { get; }

    private MaSanPham(string giaTri) => GiaTri = giaTri;

    public static Result<MaSanPham> Tao(string? ma)
    {
        if (string.IsNullOrWhiteSpace(ma)) return Result<MaSanPham>.Fail(Loi.DuLieuKhongHopLe("SanPham.Ma.Rong", "Ma san pham khong duoc rong"));
        ma = ma.Trim().ToUpperInvariant();
        return DinhDang().IsMatch(ma)
            ? new MaSanPham(ma)
            : Result<MaSanPham>.Fail(Loi.DuLieuKhongHopLe("SanPham.Ma.SaiDinhDang", "Ma gom 2 chu cai + 3 chu so, vi du LT001"));
    }

    // Chi dung cho tai tao tu CSDL (du lieu da hop le luc ghi)
    public static MaSanPham TuCsdl(string giaTri) => new(giaTri);

    public override string ToString() => GiaTri;

    [GeneratedRegex("^[A-Z]{2}[0-9]{3}$")]
    private static partial Regex DinhDang();
}

public sealed record Tien
{
    public decimal SoTien { get; }
    private Tien(decimal soTien) => SoTien = soTien;

    public static Tien Khong => new(0);
    public static Tien TuCsdl(decimal soTien) => new(soTien);          // tai tao tu CSDL

    public static Result<Tien> Tao(decimal soTien)
        => soTien < 0
            ? Result<Tien>.Fail(Loi.DuLieuKhongHopLe("Tien.Am", "So tien khong duoc am"))
            : new Tien(decimal.Round(soTien, 0));          // VND: khong co phan le

    public static Tien operator +(Tien a, Tien b) => new(a.SoTien + b.SoTien);
    public static Tien operator *(Tien a, int n) => new(a.SoTien * n);

    public override string ToString() => $"{SoTien:N0} d";
}
