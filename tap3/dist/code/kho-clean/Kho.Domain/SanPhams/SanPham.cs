using Kho.Domain.Common;

namespace Kho.Domain.SanPhams;

// ---- Su kien mien ----
public sealed record SanPhamDaTao(string Ma, int TonDau, DateTimeOffset XayRaLuc) : IDomainEvent;
public sealed record TonKhoThayDoi(string Ma, int TonCu, int TonMoi, string LyDo, DateTimeOffset XayRaLuc) : IDomainEvent;
public sealed record SapHetHang(string Ma, int TonKho, int MucCanhBao, DateTimeOffset XayRaLuc) : IDomainEvent;

// ---- Aggregate root ----
// Nguyen tac: TRANG THAI chi doi qua PHUONG THUC nghiep vu (khong set tu do); bat bien duoc bao ve o CHINH lop nay.
public sealed class SanPham : Entity<int>
{
    public MaSanPham Ma { get; private set; } = null!;
    public string Ten { get; private set; } = "";
    public string Nhom { get; private set; } = "";
    public Tien DonGia { get; private set; } = Tien.Khong;
    public int TonKho { get; private set; }
    public int MucCanhBao { get; private set; }

    private SanPham() { }                                         // danh cho EF Core

    public static Result<SanPham> Tao(string? ma, string? ten, string? nhom, decimal donGia, int tonDau, DateTimeOffset bayGio, int mucCanhBao = 5)
    {
        var maKq = MaSanPham.Tao(ma);
        if (!maKq.ThanhCong) return Result<SanPham>.Fail(maKq.Loi!);
        if (string.IsNullOrWhiteSpace(ten) || ten.Trim().Length < 2)
            return Result<SanPham>.Fail(Loi.DuLieuKhongHopLe("SanPham.Ten", "Ten toi thieu 2 ky tu"));
        if (string.IsNullOrWhiteSpace(nhom))
            return Result<SanPham>.Fail(Loi.DuLieuKhongHopLe("SanPham.Nhom", "Nhom khong duoc rong"));
        var giaKq = Tien.Tao(donGia);
        if (!giaKq.ThanhCong) return Result<SanPham>.Fail(giaKq.Loi!);
        if (tonDau < 0) return Result<SanPham>.Fail(Loi.DuLieuKhongHopLe("SanPham.TonDau", "Ton dau khong duoc am"));

        var sp = new SanPham
        {
            Ma = maKq.GiaTri,
            Ten = ten.Trim(),
            Nhom = nhom.Trim(),
            DonGia = giaKq.GiaTri,
            TonKho = tonDau,
            MucCanhBao = mucCanhBao,
        };
        sp.PhatSuKien(new SanPhamDaTao(sp.Ma.GiaTri, tonDau, bayGio));
        return sp;
    }

    public Result NhapKho(int soLuong, DateTimeOffset bayGio)
    {
        if (soLuong <= 0) return Result.Fail(Loi.DuLieuKhongHopLe("SanPham.SoLuong", "So luong nhap phai lon hon 0"));

        int cu = TonKho;
        TonKho += soLuong;
        PhatSuKien(new TonKhoThayDoi(Ma.GiaTri, cu, TonKho, "Nhap kho", bayGio));
        return Result.Ok();
    }

    public Result XuatKho(int soLuong, DateTimeOffset bayGio)
    {
        if (soLuong <= 0) return Result.Fail(Loi.DuLieuKhongHopLe("SanPham.SoLuong", "So luong xuat phai lon hon 0"));
        if (soLuong > TonKho)
            return Result.Fail(Loi.NghiepVu("SanPham.KhongDuHang", $"San pham {Ma} khong du hang: can {soLuong}, con {TonKho}"));

        int cu = TonKho;
        TonKho -= soLuong;
        PhatSuKien(new TonKhoThayDoi(Ma.GiaTri, cu, TonKho, "Xuat kho", bayGio));

        // Quy tac nghiep vu: vua CHAM nguong canh bao (tu tren xuong) thi phat su kien - chi mot lan
        if (cu > MucCanhBao && TonKho <= MucCanhBao)
            PhatSuKien(new SapHetHang(Ma.GiaTri, TonKho, MucCanhBao, bayGio));
        return Result.Ok();
    }

    public Result DoiGia(decimal giaMoi)
    {
        var kq = Tien.Tao(giaMoi);
        if (!kq.ThanhCong) return Result.Fail(kq.Loi!);
        DonGia = kq.GiaTri;
        return Result.Ok();
    }
}
