namespace ThuVien.Models;   // file-scoped namespace: ap dung cho ca file

public class Sach
{
    public required string TieuDe { get; init; }
    public required string TacGia { get; init; }
    public int NamXuatBan { get; init; }

    public override string ToString() => $"{TieuDe} - {TacGia} ({NamXuatBan})";
}
