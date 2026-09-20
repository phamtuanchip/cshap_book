namespace ShopLib;

// Phu thuoc vao TRUU TUONG (interface) de co the thay bang doi tuong gia khi test
public interface IKhoHang
{
    int SoLuongTon(string maSp);
    void GiamTon(string maSp, int soLuong);
}

public interface IGuiEmail
{
    void Gui(string den, string tieuDe, string noiDung);
}

public class DonHangService(IKhoHang kho, IGuiEmail email)
{
    public KetQuaDatHang DatHang(GioHang gio, string emailKhach)
    {
        if (gio.Dong.Count == 0)
            return KetQuaDatHang.Loi("Gio hang trong");

        foreach (var d in gio.Dong)
            if (kho.SoLuongTon(d.MaSp) < d.SoLuong)
                return KetQuaDatHang.Loi($"Khong du hang: {d.Ten}");

        foreach (var d in gio.Dong)
            kho.GiamTon(d.MaSp, d.SoLuong);

        email.Gui(emailKhach, "Dat hang thanh cong", $"Tong tien: {gio.TongTien:N0}");
        return KetQuaDatHang.ThanhCong(gio.TongTien);
    }
}

public record KetQuaDatHang(bool OK, decimal TongTien, string? LoiMoTa)
{
    public static KetQuaDatHang ThanhCong(decimal tong) => new(true, tong, null);
    public static KetQuaDatHang Loi(string moTa) => new(false, 0, moTa);
}
