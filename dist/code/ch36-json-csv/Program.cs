using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var sv = new SinhVien
{
    Ma = 1,
    Ten = "Nguyen Van An",
    Diem = 8.5,
    XepLoai = XepLoai.Gioi,
    NgaySinh = new DateOnly(2004, 5, 20),
    MatKhau = "bi-mat",
    MonHoc = ["Toan", "Ly"],
};

// 1. Object -> JSON
var tuyChon = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};
string json = JsonSerializer.Serialize(sv, tuyChon);
Console.WriteLine(json);

// 2. JSON -> Object
var sv2 = JsonSerializer.Deserialize<SinhVien>(json, tuyChon);
Console.WriteLine($"Doc lai: {sv2?.Ten}, {sv2?.Diem}, {sv2?.XepLoai}, mat khau = '{sv2?.MatKhau}'");

// 3. JSON co du lieu sai
try
{
    JsonSerializer.Deserialize<SinhVien>("{ \"ma\": \"khong-phai-so\" }", tuyChon);
}
catch (JsonException e)
{
    Console.WriteLine($"Bat duoc {e.GetType().Name} khi du lieu sai kieu");
}

// 4. Danh sach & Dictionary
var ds = new List<SinhVien> { sv, new() { Ma = 2, Ten = "Tran Thi B", Diem = 6.0 } };
string jsonDs = JsonSerializer.Serialize(ds.Select(s => new { s.Ma, s.Ten }));
Console.WriteLine(jsonDs);
var tuDien = JsonSerializer.Deserialize<Dictionary<string, int>>("""{"a": 1, "b": 2}""")!;
Console.WriteLine($"Dictionary: a={tuDien["a"]}, b={tuDien["b"]}");

// 5. JsonDocument: doc tuy y khi khong co class
using var doc = JsonDocument.Parse("""{"cua_hang": {"ten": "Sach Xanh", "sach": [{"gia": 100}, {"gia": 250}]}}""");
var goc = doc.RootElement;
string tenCh = goc.GetProperty("cua_hang").GetProperty("ten").GetString()!;
decimal tong = goc.GetProperty("cua_hang").GetProperty("sach").EnumerateArray().Sum(x => x.GetProperty("gia").GetDecimal());
Console.WriteLine($"{tenCh}: tong gia = {tong}");

// 6. Ghi / doc file JSON
string file = Path.Combine(Path.GetTempPath(), "sv_ch36.json");
await File.WriteAllTextAsync(file, json);
await using (var fs = File.OpenRead(file))
{
    var t = await JsonSerializer.DeserializeAsync<SinhVien>(fs, tuyChon);
    Console.WriteLine($"Doc tu file: {t?.Ten}");
}
File.Delete(file);

// 7. CSV: doc bang tay, xu ly dau ngoac kep
string csv = """
    ma,ten,diem
    1,An,8.5
    2,"Binh, Nguyen",7.0
    3,"Chi ""Cool""",9.0
    """;
var bang = new List<(int Ma, string Ten, double Diem)>();
foreach (var dong in csv.Split('\n').Skip(1))
{
    var o = TachCsv(dong.TrimEnd('\r'));
    bang.Add((int.Parse(o[0]), o[1], double.Parse(o[2], System.Globalization.CultureInfo.InvariantCulture)));
}
foreach (var (ma, ten, diem) in bang) Console.WriteLine($"  {ma} | {ten} | {diem}");

// 8. Ghi CSV: escape dung
var sb = new StringBuilder("ma,ten,diem\n");
foreach (var (ma, ten, diem) in bang)
    sb.Append(ma).Append(',').Append(DongGoiCsv(ten)).Append(',').AppendLine(diem.ToString(System.Globalization.CultureInfo.InvariantCulture));
Console.Write(sb);

// Tach 1 dong CSV, ho tro "..." va "" (dau nhay kep trong o)
static List<string> TachCsv(string dong)
{
    var kq = new List<string>();
    var cur = new StringBuilder();
    bool trongNgoac = false;
    for (int i = 0; i < dong.Length; i++)
    {
        char c = dong[i];
        if (trongNgoac)
        {
            if (c == '"' && i + 1 < dong.Length && dong[i + 1] == '"') { cur.Append('"'); i++; }
            else if (c == '"') trongNgoac = false;
            else cur.Append(c);
        }
        else if (c == '"') trongNgoac = true;
        else if (c == ',') { kq.Add(cur.ToString()); cur.Clear(); }
        else cur.Append(c);
    }
    kq.Add(cur.ToString());
    return kq;
}

static string DongGoiCsv(string s)
    => s.Contains(',') || s.Contains('"') || s.Contains('\n') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

enum XepLoai { Yeu, TrungBinh, Kha, Gioi }

class SinhVien
{
    public int Ma { get; set; }

    [JsonPropertyName("hoTen")]
    public string Ten { get; set; } = "";

    public double Diem { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public XepLoai XepLoai { get; set; }

    public DateOnly NgaySinh { get; set; }

    [JsonIgnore]
    public string MatKhau { get; set; } = "";

    public List<string> MonHoc { get; set; } = [];
}
