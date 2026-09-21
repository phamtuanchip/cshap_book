using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace ChuongDanhGia;

// ================= Do tuong tu ngu nghia don gian (nhu Chuong 6), dung de SO SANH cau tra loi voi dap an mau =================
// KHONG dung Assert.Equal cho van ban LLM: cau chu doi tung chu nhung Y NGHIA giong nhau van la "dung".
//
// BAI HOC THUC TE khi viet chuong nay: do tuong tu THEO TU VUNG THUAN TUY co the bi "qua mat" boi mot cau tra loi
// dung nhieu tu giong dap an nhung SAI SO LIEU ("Don hang toi da 20 dong" vs "Don hang co toi da 50 dong tuy he thong"
// - hai cau chia se "don hang", "toi da" nhung so lieu cot loi (20 vs 50) hoan toan khac). Vi vay ham nay CO THEM
// buoc kiem tra so lieu rieng: neu dap an mau co so va cau tra loi KHONG chua dung so do, diem bi phat manh.
public static class DoTuongTu
{
    public static float Diem(string a, string b)
    {
        double diemTu = DiemTheoTu(a, b);
        var soTrongB = SoTrongCau(b);                             // "b" la dap an mau: so trong do la SU KIEN can khop dung
        if (soTrongB.Count > 0 && !soTrongB.All(SoTrongCau(a).Contains))
            diemTu *= 0.3;                                        // phat nang: sai so lieu la loi nghiem trong hon la khac tu vung
        return (float)diemTu;
    }

    private static double DiemTheoTu(string a, string b)
    {
        var va = VectorTu(a);
        var vb = VectorTu(b);
        var chung = va.Keys.Intersect(vb.Keys).ToList();
        if (chung.Count == 0) return 0;
        double tich = chung.Sum(t => (double)va[t] * vb[t]);
        double doDaiA = Math.Sqrt(va.Values.Sum(x => (double)x * x));
        double doDaiB = Math.Sqrt(vb.Values.Sum(x => (double)x * x));
        return tich / (doDaiA * doDaiB);
    }

    private static Dictionary<string, int> VectorTu(string s)
    {
        var d = new Dictionary<string, int>();
        foreach (var tu in Regex.Replace(s.ToLowerInvariant(), @"[.,;!?]", "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            d[tu] = d.GetValueOrDefault(tu) + 1;
        return d;
    }

    private static HashSet<string> SoTrongCau(string s) => Regex.Matches(s, @"\d+").Select(m => m.Value).ToHashSet();
}

// ================= Bo du lieu "vang" (golden set): cau hoi + dap an mong doi, dung lai qua nhieu lan chay =================
public sealed record CaVang(string Id, string CauHoi, string DapAnMongDoi, float NguongDiem = 0.5f);

public sealed record KetQuaCa(string Id, bool Dat, float Diem, string TraLoiThuc);
public sealed record BaoCaoDanhGia(IReadOnlyList<KetQuaCa> ChiTiet, float TiLeDat, float DiemTrungBinh)
{
    public override string ToString() => $"Dat {ChiTiet.Count(c => c.Dat)}/{ChiTiet.Count} ({TiLeDat:P0}), diem trung binh {DiemTrungBinh:F2}";
}

// ================= Bo may danh gia: chay tap ca vang qua MOT IChatClient, cham diem, tong hop =================
public static class BoDanhGia
{
    public static async Task<BaoCaoDanhGia> ChayAsync(IChatClient client, IEnumerable<CaVang> caVang, CancellationToken ct = default)
    {
        var ketQua = new List<KetQuaCa>();
        foreach (var ca in caVang)
        {
            var res = await client.GetResponseAsync(ca.CauHoi, cancellationToken: ct);
            float diem = DoTuongTu.Diem(res.Text, ca.DapAnMongDoi);
            ketQua.Add(new(ca.Id, diem >= ca.NguongDiem, diem, res.Text));
        }
        return new(ketQua, (float)ketQua.Count(k => k.Dat) / ketQua.Count, ketQua.Average(k => k.Diem));
    }
}

// ================= LLM lam giam khao (LLM-as-judge): mo hinh THU HAI cham diem theo tieu chi, tra JSON co cau truc =================
public sealed record PhanXu(int Diem, string LyDo, bool CoCanCu);      // Diem: 1..5. CoCanCu: co dua tren du lieu duoc cung cap khong

public static class GiamKhao
{
    public static async Task<PhanXu> ChamAsync(IChatClient giamKhao, string cauHoi, string ngoCanh, string traLoi, CancellationToken ct = default)
    {
        string heThong = """
            Ban la giam khao cham cau tra loi cua mot tro ly AI. Cham theo thang 1-5:
            5 = dung hoan toan, dua tren ngu canh, day du. 3 = dung mot phan. 1 = sai hoac bia dat.
            Tra loi CHI bang JSON: {"diem":so,"lyDo":"...","coCanCu":true/false}
            """;
        string nguoiDung = $"Ngu canh: {ngoCanh}\nCau hoi: {cauHoi}\nCau tra loi can cham: {traLoi}";
        var res = await giamKhao.GetResponseAsync([new(ChatRole.System, heThong), new(ChatRole.User, nguoiDung)], cancellationToken: ct);
        var j = JsonDocument.Parse(res.Text).RootElement;
        return new(j.GetProperty("diem").GetInt32(), j.GetProperty("lyDo").GetString() ?? "", j.GetProperty("coCanCu").GetBoolean());
    }
}
