namespace KienTruc.Domain;

// Domain: lop TRONG CUNG. Khong biet EF, ASP.NET, JSON hay CSDL nao ton tai.
public class SanPham
{
    public int Id { get; }
    public string Ma { get; }
    public string Ten { get; private set; }
    public decimal Gia { get; private set; }

    public SanPham(int id, string ma, string ten, decimal gia)
    {
        if (string.IsNullOrWhiteSpace(ma)) throw new ArgumentException("Ma khong duoc rong", nameof(ma));
        if (gia < 0) throw new ArgumentOutOfRangeException(nameof(gia), "Gia khong duoc am");
        (Id, Ma, Ten, Gia) = (id, ma, ten, gia);
    }

    public void DoiGia(decimal giaMoi)
    {
        if (giaMoi < 0) throw new ArgumentOutOfRangeException(nameof(giaMoi), "Gia khong duoc am");
        Gia = giaMoi;
    }
}
