public class HinhChuNhat
{
    // field: du lieu cua doi tuong
    public double Rong;
    public double Cao;

    // constructor mac dinh (khong tham so)
    public HinhChuNhat() : this(1, 1) { }

    // constructor co tham so
    public HinhChuNhat(double rong, double cao)
    {
        Rong = rong;   // "this.Rong" cung duoc; o day ten tham so khac nen khong can this
        Cao = cao;
    }

    // phuong thuc: hanh vi cua doi tuong
    public double DienTich() => Rong * Cao;
    public double ChuVi() => 2 * (Rong + Cao);

    public void PhongTo(double heSo)
    {
        Rong *= heSo;
        Cao *= heSo;
    }

    public override string ToString() => $"HCN {Rong} x {Cao}";
}
