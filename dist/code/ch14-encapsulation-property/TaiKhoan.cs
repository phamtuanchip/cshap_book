public class TaiKhoan
{
    private decimal _soDu;                 // field private: chi class nay truy cap duoc
    private readonly List<string> _lichSu = [];

    // property chi doc tu ben ngoai
    public string SoTaiKhoan { get; }
    public string ChuTaiKhoan { get; set; }

    // property co logic kiem tra
    public decimal SoDu
    {
        get => _soDu;
        private set
        {
            if (value < 0) throw new InvalidOperationException("So du khong duoc am");
            _soDu = value;
        }
    }

    // property tinh toan (khong co field di kem)
    public bool ConTien => _soDu > 0;

    // chi cho ben ngoai doc, khong cho them/xoa
    public IReadOnlyList<string> LichSu => _lichSu;

    public TaiKhoan(string soTaiKhoan, string chuTaiKhoan, decimal soDuBanDau = 0)
    {
        SoTaiKhoan = soTaiKhoan;
        ChuTaiKhoan = chuTaiKhoan;
        SoDu = soDuBanDau;
    }

    public void NapTien(decimal soTien)
    {
        if (soTien <= 0) throw new ArgumentOutOfRangeException(nameof(soTien), "So tien phai duong");
        SoDu += soTien;
        _lichSu.Add($"+{soTien}");
    }

    public bool RutTien(decimal soTien)
    {
        if (soTien <= 0 || soTien > SoDu) return false;
        SoDu -= soTien;
        _lichSu.Add($"-{soTien}");
        return true;
    }
}
