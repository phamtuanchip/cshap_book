namespace QuanLyKho.Core.Models;

// Loi nghiep vu: co lop rieng de tang giao dien phan biet duoc voi loi he thong
public class KhoException(string message) : Exception(message);

public class KhongTimThayException(string ma) : KhoException($"Khong tim thay san pham {ma}")
{
    public string Ma { get; } = ma;
}

public class KhongDuHangException(string ma, int can, int con)
    : KhoException($"San pham {ma} khong du hang: can {can}, con {con}")
{
    public int Can { get; } = can;
    public int Con { get; } = con;
}
