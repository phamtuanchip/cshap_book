using System.Globalization;
using System.Text;
using Microsoft.Extensions.AI;

// EMBEDDING GIA de chay khong can khoa API: bieu dien van ban bang "tui tu" (bag-of-words) bam vao 512 chieu.
// CHI GIONG embedding that o cho: van ban giong nhau ve TU thi vector gan nhau.
// KHAC embedding that o cho QUAN TRONG: khong hieu dong nghia/ngu nghia ("laptop" != "may tinh xach tay").
public sealed class HashEmbeddingGenerator(int soChieu = 512) : IEmbeddingGenerator<string, Embedding<float>>
{
    private static readonly HashSet<string> TuBo = ["la", "va", "cua", "cho", "co", "khong", "mot", "the", "nao", "bao", "nhieu", "the", "nay", "de", "khi", "duoc", "o", "tren", "trong"];

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken ct = default)
    {
        var ds = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var v in values) ds.Add(new Embedding<float>(TaoVector(v)));
        return Task.FromResult(ds);
    }

    public float[] TaoVector(string van)
    {
        var vec = new float[soChieu];
        foreach (var tu in TachTu(van))
        {
            uint h = 2166136261;                                           // FNV-1a: ham bam on dinh (khong dung string.GetHashCode: doi moi lan chay)
            foreach (char c in tu) { h ^= c; h *= 16777619; }
            vec[h % (uint)soChieu] += 1f;
        }
        // Chuan hoa do dai 1: khi do cosine similarity = tich vo huong
        float dodai = MathF.Sqrt(vec.Sum(x => x * x));
        if (dodai > 0) for (int i = 0; i < vec.Length; i++) vec[i] /= dodai;
        return vec;
    }

    public static IEnumerable<string> TachTu(string van)
    {
        var sb = new StringBuilder();
        foreach (char c in van.Normalize(NormalizationForm.FormD))
        {
            var loai = CharUnicodeInfo.GetUnicodeCategory(c);
            if (loai == UnicodeCategory.NonSpacingMark) continue;           // bo dau tieng Viet
            char x = c is 'đ' or 'Đ' ? 'd' : char.ToLowerInvariant(c);
            sb.Append(char.IsLetterOrDigit(x) ? x : ' ');
        }
        return sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !TuBo.Contains(t));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
