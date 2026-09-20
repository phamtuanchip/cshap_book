using ThuVien.Models;

namespace ThuVien.Services;

public class KhoSach
{
    private readonly List<Sach> _sach = [];

    public void Them(Sach sach) => _sach.Add(sach);

    public IEnumerable<Sach> TimTheoTacGia(string tacGia)
        => _sach.Where(s => s.TacGia.Contains(tacGia, StringComparison.OrdinalIgnoreCase));

    public int SoLuong => _sach.Count;

    // internal: chi dung duoc trong cung assembly (project) nay
    internal void XoaHet() => _sach.Clear();
}
