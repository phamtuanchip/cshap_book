namespace CuaHang.Models;

// Entity = class C# anh xa toi mot bang. Property = cot. Quy uoc: property "Id" la khoa chinh.
public class Nhom
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";

    // Navigation property (Chuong 13): "mot nhom co nhieu san pham"
    public List<SanPham> SanPhams { get; set; } = [];
}

public class SanPham
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public decimal Gia { get; set; }
    public int Ton { get; set; }

    public int NhomId { get; set; }          // khoa ngoai
    public Nhom? Nhom { get; set; }          // navigation toi entity cha

    public override string ToString() => $"{Ma,-6} {Ten,-18} {Gia,12:N0} ton {Ton,3}";
}
