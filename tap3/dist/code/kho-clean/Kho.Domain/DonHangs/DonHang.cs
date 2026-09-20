using Kho.Domain.Common;
using Kho.Domain.SanPhams;

namespace Kho.Domain.DonHangs;

// ---- Su kien mien (se di qua outbox thanh su kien tich hop) ----
public sealed record DonHangDaDat(string MaDon, string KhachHang, decimal TongTien, int SoDong, DateTimeOffset XayRaLuc) : IDomainEvent;
public sealed record DonHangDaHuy(string MaDon, string LyDo, DateTimeOffset XayRaLuc) : IDomainEvent;

public enum TrangThaiDon { DaDat = 1, DaHuy = 2 }

// Dong don hang: BAN CHUP ma san pham + don gia tai luc dat (gia sau nay doi khong lam doi don cu). Thuoc aggregate DonHang.
public sealed class DongDon
{
    public string MaSanPham { get; private set; } = "";
    public int SoLuong { get; private set; }
    public Tien DonGia { get; private set; } = Tien.Khong;
    public Tien ThanhTien => DonGia * SoLuong;

    private DongDon() { }                                            // EF Core
    public DongDon(string maSanPham, int soLuong, Tien donGia) { MaSanPham = maSanPham; SoLuong = soLuong; DonGia = donGia; }
}

// Aggregate root thu hai. Chi tham chieu SanPham bang MA (khong giu doi tuong SanPham): moi aggregate la mot ranh gioi nhat quan rieng.
public sealed class DonHang : Entity<int>
{
    public const int SoDongToiDa = 50;

    private readonly List<DongDon> _dong = [];

    public string Ma { get; private set; } = "";
    public string KhachHang { get; private set; } = "";
    public TrangThaiDon TrangThai { get; private set; }
    public Tien TongTien { get; private set; } = Tien.Khong;
    public DateTimeOffset DatLuc { get; private set; }
    public IReadOnlyList<DongDon> Dong => _dong;

    private DonHang() { }

    public static Result<DonHang> Tao(string ma, string? khachHang, IEnumerable<DongDon> dong, DateTimeOffset bayGio)
    {
        if (string.IsNullOrWhiteSpace(khachHang))
            return Result<DonHang>.Fail(Loi.DuLieuKhongHopLe("DonHang.KhachHang", "Ten khach hang khong duoc rong"));

        var ds = dong.ToList();
        if (ds.Count == 0) return Result<DonHang>.Fail(Loi.DuLieuKhongHopLe("DonHang.Dong", "Don hang phai co it nhat mot dong"));
        if (ds.Count > SoDongToiDa) return Result<DonHang>.Fail(Loi.DuLieuKhongHopLe("DonHang.Dong", $"Toi da {SoDongToiDa} dong"));
        if (ds.Select(d => d.MaSanPham).Distinct().Count() != ds.Count)
            return Result<DonHang>.Fail(Loi.DuLieuKhongHopLe("DonHang.Dong", "Moi san pham chi xuat hien mot dong"));
        if (ds.Any(d => d.SoLuong <= 0)) return Result<DonHang>.Fail(Loi.DuLieuKhongHopLe("DonHang.SoLuong", "So luong moi dong phai lon hon 0"));

        var don = new DonHang { Ma = ma, KhachHang = khachHang.Trim(), TrangThai = TrangThaiDon.DaDat, DatLuc = bayGio };
        don._dong.AddRange(ds);
        don.TongTien = ds.Aggregate(Tien.Khong, (tong, d) => tong + d.ThanhTien);
        don.PhatSuKien(new DonHangDaDat(ma, don.KhachHang, don.TongTien.SoTien, ds.Count, bayGio));
        return don;
    }

    // Trang thai chi doi qua phuong thuc; quy tac "khong huy hai lan" nam DUNG O DAY
    public Result Huy(string? lyDo, DateTimeOffset bayGio)
    {
        if (TrangThai == TrangThaiDon.DaHuy)
            return Result.Fail(Loi.NghiepVu("DonHang.DaHuy", $"Don {Ma} da bi huy truoc do"));

        TrangThai = TrangThaiDon.DaHuy;
        PhatSuKien(new DonHangDaHuy(Ma, string.IsNullOrWhiteSpace(lyDo) ? "Khong neu ly do" : lyDo.Trim(), bayGio));
        return Result.Ok();
    }
}
