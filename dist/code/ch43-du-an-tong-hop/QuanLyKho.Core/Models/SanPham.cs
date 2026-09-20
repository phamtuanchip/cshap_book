namespace QuanLyKho.Core.Models;

public record SanPham(string Ma, string Ten, string Nhom, decimal DonGia, int TonKho, int MucCanhBao = 5)
{
    public decimal GiaTriTon => DonGia * TonKho;
}
