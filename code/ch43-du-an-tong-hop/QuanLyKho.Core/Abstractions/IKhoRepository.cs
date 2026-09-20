using QuanLyKho.Core.Models;

namespace QuanLyKho.Core.Abstractions;

// Nghiep vu phu thuoc vao interface nay, khong biet du lieu nam o JSON, CSDL hay bo nho (DIP)
public interface IKhoRepository
{
    Task<DuLieuKho> TaiAsync(CancellationToken ct = default);
    Task LuuAsync(DuLieuKho duLieu, CancellationToken ct = default);
}
