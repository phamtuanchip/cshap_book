public class SinhVien
{
    // static: thuoc ve CLASS, dung chung cho moi doi tuong
    private static int demSo = 0;
    public static int TongSoSinhVien => demSo;

    public int Ma;
    public string Ten;
    public double Diem;

    public SinhVien(string ten, double diem)
    {
        demSo++;
        Ma = demSo;
        Ten = ten;   // gan tham so vao field
        Diem = diem;
    }

    // this dung khi ten tham so trung ten field
    public void DoiTen(string ten) => this.Ten = ten;

    public override string ToString() => $"#{Ma} {Ten} ({Diem})";
}
