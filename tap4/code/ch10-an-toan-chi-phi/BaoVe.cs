using System.Text.RegularExpressions;

namespace ChuongAnToan;

// ================= 1) CHONG PROMPT INJECTION GIAN TIEP: du lieu ben ngoai (ket qua tool, tai lieu, trang web...) =================
// KHONG BAO GIO duoc coi la chi dan. Phong thu O TANG UNG DUNG (khong chi dua vao mo hinh "tu biet"):
// (a) boc trong the ro rang, (b) do va CANH BAO/CHAN cac cau lenh nghi ngo nam trong du lieu.
public static class BaoVeDuLieuNgoai
{
    // Cac cum tu THUONG gap trong tan cong "bo qua chi dan truoc do". Danh sach nay KHONG DAY DU (khong co danh sach nao du),
    // day la MOT LOP phong thu trong nhieu lop (defense in depth), khong phai giai phap duy nhat.
    private static readonly string[] MauNghiNgo =
    [
        "bo qua", "quen di", "khong can tuan thu", "tu bay gio hay", "ignore previous", "ignore all previous",
        "disregard", "new instructions", "you are now", "system:", "gui toan bo", "xoa toan bo", "chuyen tien du lieu",
    ];

    public sealed record KetQuaLoc(string NoiDungAnToan, bool CoNghiNgo, IReadOnlyList<string> MauKhopDuoc);

    // Boc du lieu ngoai trong the ro rang + neu du lieu chua mau nghi ngo, DANH DAU de he thong (khong phai mo hinh) quyet dinh tiep
    public static KetQuaLoc Loc(string duLieuNgoai)
    {
        string chuanHoa = duLieuNgoai.ToLowerInvariant();
        var khop = MauNghiNgo.Where(m => chuanHoa.Contains(m)).ToList();
        string boc = $"<du_lieu_ngoai nguon=\"khong_tin_cay\">\n{duLieuNgoai}\n</du_lieu_ngoai>";
        return new(boc, khop.Count > 0, khop);
    }
}

// ================= 2) CHE THONG TIN CA NHAN (PII) TRUOC KHI GUI DI HOAC GHI LOG =================
// Nguyen tac: TOI THIEU HOA du lieu gui cho ben thu ba (nha cung cap LLM) va trong log/telemetry (Tap 3, Chuong 13).
public static partial class CheThongTinCaNhan
{
    public static string Che(string vanBan)
    {
        string s = RegexEmail().Replace(vanBan, "[EMAIL_DA_CHE]");
        s = RegexSoDienThoaiVn().Replace(s, "[SDT_DA_CHE]");
        s = RegexTheTinDung().Replace(s, "[THE_DA_CHE]");
        return s;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex RegexEmail();

    [GeneratedRegex(@"(?:\+84|0)(3|5|7|8|9)\d{8}\b")]
    private static partial Regex RegexSoDienThoaiVn();

    [GeneratedRegex(@"\b(?:\d[ -]?){13,16}\b")]
    private static partial Regex RegexTheTinDung();
}

// ================= 3) BO LOC NOI DUNG DAU VAO: chan som cac yeu cau ro rang nam ngoai pham vi/chinh sach =================
// Loi ich kep: AN TOAN (khong chuyen tiep yeu cau nguy hiem cho mo hinh) va CHI PHI (khong ton tien goi mo hinh vo ich).
public static class BoLocNoiDung
{
    private static readonly string[] TuKhoaChan = ["mat khau quan tri", "khoa api", "thong tin the tin dung cua nguoi khac", "bo qua chinh sach"];

    public sealed record KetQuaKiemTra(bool ChoPhep, string? LyDoTuChoi);

    public static KetQuaKiemTra KiemTra(string yeuCauNguoiDung)
    {
        string chuanHoa = yeuCauNguoiDung.ToLowerInvariant();
        var viPham = TuKhoaChan.FirstOrDefault(chuanHoa.Contains);
        return viPham is null ? new(true, null) : new(false, $"Yeu cau cham chinh sach noi dung (chua '{viPham}')");
    }
}
