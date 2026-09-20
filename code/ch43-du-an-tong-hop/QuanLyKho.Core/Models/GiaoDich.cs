namespace QuanLyKho.Core.Models;

public enum LoaiGiaoDich
{
    Nhap,
    Xuat,
}

public record GiaoDich(DateTime ThoiGian, string MaSanPham, LoaiGiaoDich Loai, int SoLuong, string? GhiChu);
