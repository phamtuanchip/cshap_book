namespace QuanLyKho.Core.Models;

// Toan bo trang thai can luu xuong dia
public class DuLieuKho
{
    public List<SanPham> SanPham { get; set; } = [];
    public List<GiaoDich> GiaoDich { get; set; } = [];
}
