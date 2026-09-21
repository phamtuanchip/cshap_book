using System.ComponentModel;
using Kho.Application.Abstractions;
using Kho.Application.Messaging;
using Kho.Application.SanPhams;
using Microsoft.Extensions.AI;

namespace TroLyKho;

// ================= Cong cu cho AI, xay tren CHINH mediator that cua kho-clean (Tap 3) =================
// Nguyen tac Chuong 5: cong cu CHI DOC dung ISender bam thang; cong cu GHI (co tac dung phu that) KHONG duoc phep -
// o day chi co "de xuat", giong nguyen tac Chuong 8 (agent chi de xuat, nguoi duyet moi thuc hien).
public sealed class CongCuKho(ISender sender)
{
    [Description("Tim san pham trong kho theo tu khoa hoac nhom. Chi doc, tra ve toi da 5 ket qua.")]
    public async Task<string> TimSanPham(
        [Description("Tu khoa tim trong ten/ma san pham, de trong neu khong loc")] string? tuKhoa,
        [Description("Loc theo nhom, de trong neu khong loc")] string? nhom)
    {
        var kq = await sender.Send(new TimSanPhamQuery(tuKhoa, nhom, Trang: 1, KichThuoc: 5));
        if (!kq.ThanhCong) return $"{{\"loi\":\"{kq.Loi!.MoTa}\"}}";
        var ds = kq.GiaTri.Muc.Select(s => $"{{\"ma\":\"{s.Ma}\",\"ten\":\"{s.Ten}\",\"ton\":{s.TonKho},\"sapHet\":{(s.SapHet ? "true" : "false")}}}");
        return $"[{string.Join(",", ds)}]";
    }

    [Description("Tra ve chi tiet ton kho hien tai cua MOT san pham theo ma chinh xac. Chi doc.")]
    public async Task<string> TraTonKho([Description("Ma san pham, vi du LT001")] string ma)
    {
        var kq = await sender.Send(new LaySanPhamQuery(ma));
        return kq.ThanhCong
            ? $"{{\"ma\":\"{kq.GiaTri.Ma}\",\"ten\":\"{kq.GiaTri.Ten}\",\"ton\":{kq.GiaTri.TonKho},\"donGia\":{kq.GiaTri.DonGia}}}"
            : $"{{\"loi\":\"{kq.Loi!.MoTa}\"}}";
    }

    // CHI DE XUAT: khong goi XuatKhoCommand That. Nguoi dung phai xac nhan qua kenh rieng (vi du mot API /xac-nhan) - Chuong 5 va 8.
    [Description("DE XUAT xuat kho mot so luong san pham (CHUA thuc hien - can nguoi duyet). Dung khi nguoi dung yeu cau xuat/ban hang.")]
    public async Task<string> DeXuatXuatKho(
        [Description("Ma san pham")] string ma,
        [Description("So luong de xuat xuat, phai la so nguyen duong")] int soLuong)
    {
        if (soLuong <= 0) return "{\"loi\":\"so luong phai lon hon 0\"}";

        // Van kiem tra truoc voi du lieu THAT de de xuat co y nghia (khong de xuat xuat qua so ton hien co)
        var sp = await sender.Send(new LaySanPhamQuery(ma));
        if (!sp.ThanhCong) return $"{{\"loi\":\"{sp.Loi!.MoTa}\"}}";
        if (soLuong > sp.GiaTri.TonKho) return $"{{\"loi\":\"de xuat vuot ton kho hien co ({sp.GiaTri.TonKho})\"}}";

        return $"{{\"deXuat\":\"Xuat {soLuong} {ma}\",\"trangThai\":\"cho_nguoi_duyet\",\"tonHienTai\":{sp.GiaTri.TonKho}}}";
    }

    public IReadOnlyList<AITool> ThanhDanhSachCongCu() =>
    [
        AIFunctionFactory.Create(TimSanPham, "tim_san_pham"),
        AIFunctionFactory.Create(TraTonKho, "tra_ton_kho"),
        AIFunctionFactory.Create(DeXuatXuatKho, "de_xuat_xuat_kho"),
    ];
}
