using System.Numerics.Tensors;

public sealed record Muc(string Id, string VanBan, float[] Vector, IReadOnlyDictionary<string, string> SieuDuLieu);
public sealed record KetQuaTim(Muc Muc, float Diem);

// Kho vector TRONG BO NHO: du cho vai chuc nghin muc. Vuot xa hon -> dung CSDL vector (pgvector, Azure AI Search, Qdrant...).
public sealed class KhoVector
{
    private readonly List<Muc> _muc = [];
    public int SoMuc => _muc.Count;

    public void Them(Muc muc) => _muc.Add(muc);

    // Tim k muc gan nhat. Vector da chuan hoa -> cosine = tich vo huong. Brute force O(n*d): doc het, chinh xac 100%.
    public IReadOnlyList<KetQuaTim> Tim(float[] truyVan, int k, float diemToiThieu = 0f, Func<Muc, bool>? loc = null)
    {
        return _muc
            .Where(m => loc?.Invoke(m) ?? true)                              // LOC THEO SIEU DU LIEU truoc (quyen, nhom, ngay)
            .Select(m => new KetQuaTim(m, TensorPrimitives.CosineSimilarity(truyVan, m.Vector)))
            .Where(r => r.Diem >= diemToiThieu)                              // NGUONG: khong tra ket qua "gan nhat nhung khong lien quan"
            .OrderByDescending(r => r.Diem)
            .Take(k)
            .ToList();
    }
}
