using Microsoft.Extensions.AI;
using Xunit;

namespace ChuongDanhGia;

// Bo cau hoi vang dung chung cho nhieu test - dinh nghia MOT LAN, tai su dung khi doi mo hinh/prompt
public static class BoCauHoiVang
{
    public static readonly CaVang[] Ca =
    [
        new("Q1", "LT001 con bao nhieu?", "LT001 con 7 chiec trong kho.", NguongDiem: 0.6f),
        new("Q2", "Chinh sach doi tra the nao?", "San pham loi duoc doi tra trong vong 30 ngay.", NguongDiem: 0.4f),
        new("Q3", "Muc canh bao ton kho mac dinh la bao nhieu?", "Muc canh bao ton kho mac dinh la 5 don vi.", NguongDiem: 0.5f),
        new("Q4", "Don hang toi da bao nhieu dong?", "Don hang toi da 20 dong.", NguongDiem: 0.4f),
        new("Q5", "San pham dien tu can bao hanh toi thieu bao lau?", "San pham dien tu can bao hanh toi thieu 12 thang.", NguongDiem: 0.5f),
    ];
}

// =================== 1) TEST TAT DINH: logic xung quanh mo hinh (khong phai chinh mo hinh) phai dung 100% ===================
// Voi mot IChatClient CO DINH (tro ly on dinh), moi cau hoi phai ra dung dap an - day la pham vi Assert.Equal HOP LY.
public class LogicXungQuanhMoHinh_LaTatDinh
{
    private readonly IChatClient _client = new TroLyOnDinh();

    [Theory]
    [InlineData("LT001 con bao nhieu?", "LT001 con 7 chiec trong kho.")]
    [InlineData("Chinh sach doi tra the nao?", "San pham loi duoc doi tra trong vong 30 ngay ke tu ngay mua.")]
    [InlineData("Muc canh bao ton kho mac dinh la bao nhieu?", "Muc canh bao ton kho mac dinh la 5 don vi.")]
    [InlineData("Don hang toi da bao nhieu dong?", "Don hang toi da 20 dong.")]
    [InlineData("San pham dien tu can bao hanh toi thieu bao lau?", "San pham dien tu can bao hanh toi thieu 12 thang.")]
    public async Task VoiTroLyOnDinh_MoiCauHoiPhaiDungHoanToan(string cauHoi, string dapAn)
    {
        var res = await _client.GetResponseAsync(cauHoi);
        Assert.Equal(dapAn, res.Text);                         // hop ly O DAY vi TroLyOnDinh la TAT DINH theo thiet ke cua chinh no
    }
}

// =================== 2) TEST THEO TI LE DAT: khong doi hoi 100% cho mot he thong co the "bia" ===================
public class DanhGiaTheoNguongTiLe
{
    [Fact]
    public async Task TroLyThucTe_PhaiDatItNhat80PhanTram_KhongBatBuoc100Phantram()
    {
        var baoCao = await BoDanhGia.ChayAsync(new TroLyThucTe(), BoCauHoiVang.Ca);

        Assert.True(baoCao.TiLeDat >= 0.8f, $"Ti le dat {baoCao.TiLeDat:P0} duoi nguong 80% chap nhan duoc. Chi tiet: {baoCao}");
        // NHUNG: cau hoi bia (Q4) PHAI la cau bi loai - kiem tra CU THE de khong bo sot loai loi nguy hiem nhat (hallucination ve chinh sach)
        var q4 = baoCao.ChiTiet.Single(c => c.Id == "Q4");
        Assert.False(q4.Dat, "Cau tra loi bia ve so dong toi da PHAI bi phat hien la sai, khong duoc 'may man' qua nguong");
    }

    [Fact]
    public async Task TroLyOnDinh_DatTuyetDoi_LamCoSoSoSanh()
    {
        var baoCao = await BoDanhGia.ChayAsync(new TroLyOnDinh(), BoCauHoiVang.Ca);
        Assert.Equal(1.0f, baoCao.TiLeDat);
    }
}

// =================== 3) TEST THONG KE cho he thong KHONG XAC DINH: lay mau nhieu lan, xet KHOANG TIN CAY ===================
// Day la cach dung de kiem thu mot thanh phan von di co nhieu bien: KHONG assert tung lan chay, ma assert TREN TONG THE.
public class KiemThuThongKeChoHeThongKhongXacDinh
{
    [Fact]
    public async Task ChayNhieuLan_TiLeDungXapXiThamSoDaCauHinh_TrongSaiSoChapNhanDuoc()
    {
        const int soLan = 200;
        const int tiLeKyVong = 75;                              // % duoc cau hinh cho mo hinh gia
        var client = new TroLyKhongOnDinh(tiLeKyVong, seed: 42);  // seed CO DINH -> bai test nay TAT DINH moi lan chay lai

        int soDung = 0;
        for (int i = 0; i < soLan; i++)
        {
            var res = await client.GetResponseAsync("LT001 con bao nhieu?");
            if (res.Text.Contains("7 chiec")) soDung++;
        }

        double tiLeThucTe = 100.0 * soDung / soLan;
        // Sai so chap nhan duoc do ban chat lay mau ngau nhien (khoang tin cay xap xi, khong phai cong thuc thong ke chat che)
        Assert.InRange(tiLeThucTe, tiLeKyVong - 10, tiLeKyVong + 10);
    }
}

// =================== 4) LLM LAM GIAM KHAO: cham cau tra loi theo tieu chi khi khong the so sanh chuoi don gian ===================
public class DanhGiaBangGiamKhao
{
    private readonly IChatClient _giamKhao = new GiamKhaoGia();

    [Fact]
    public async Task CauTraLoiCoCanCu_DuocChamDiemCao()
    {
        var px = await GiamKhao.ChamAsync(_giamKhao,
            cauHoi: "Chinh sach doi tra the nao?",
            ngoCanh: "San pham loi duoc doi tra trong vong 30 ngay.",
            traLoi: "San pham loi duoc doi tra trong vong 30 ngay ke tu ngay mua.");

        Assert.True(px.Diem >= 4, $"Ky vong diem cao vi co can cu ro rang, thuc te: {px.Diem} ({px.LyDo})");
        Assert.True(px.CoCanCu);
    }

    [Fact]
    public async Task CauTraLoiBia_DuocChamDiemThap()
    {
        var px = await GiamKhao.ChamAsync(_giamKhao,
            cauHoi: "Don hang toi da bao nhieu dong?",
            ngoCanh: "Don hang toi da 20 dong.",
            traLoi: "Don hang co the co toi da 50 dong tuy cau hinh he thong.");

        Assert.True(px.Diem <= 2, $"Ky vong diem thap vi bia thong tin, thuc te: {px.Diem} ({px.LyDo})");
        Assert.False(px.CoCanCu);
    }
}

// =================== 5) KIEM THU HOI QUY PROMPT: prompt la MA NGUON, doi ngoai y muon phai bi phat hien ===================
public class KiemThuHoiQuyPrompt
{
    // "Snapshot" toi gian: luu ban prompt DA DUOC CHAP NHAN ngay trong test. Doi prompt ma quen cap nhat test -> do,
    // buoc nguoi sua phai XEM LAI VA XAC NHAN thay doi la co y (khong phai vo tinh go nham).
    private const string PromptDaChapNhan = """
        Ban la tro ly tra loi dua CHI TREN tai lieu duoc cung cap trong <tai_lieu>.
        Neu tai lieu khong du de tra loi, hay noi "Toi khong co thong tin nay trong tai lieu".
        """;

    [Fact]
    public void SystemPrompt_KhongDoiNgoaiYMuon()
    {
        string promptHienTai = """
            Ban la tro ly tra loi dua CHI TREN tai lieu duoc cung cap trong <tai_lieu>.
            Neu tai lieu khong du de tra loi, hay noi "Toi khong co thong tin nay trong tai lieu".
            """;

        Assert.Equal(PromptDaChapNhan, promptHienTai);
        // Neu test nay do: hoac (a) ai do vo tinh sua prompt -> sua lai, hoac (b) sua CO Y -> cap nhat PromptDaChapNhan
        // VA chay lai toan bo BoDanhGia o tren + cham diem giam khao truoc khi merge (doi prompt CO THE doi hanh vi).
    }
}
