using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> gen = new HashEmbeddingGenerator();

// ---- 1. Vector la gi ----
var mau = await gen.GenerateAsync(["Laptop Dell XPS 13, man hinh 13 inch"]);
var v = mau[0].Vector.ToArray();
Console.WriteLine($"=== 1. Mot embedding la mang so: {v.Length} chieu, {v.Count(x => x != 0)} chieu khac 0, do dai = {MathF.Sqrt(v.Sum(x => x * x)):F2}");

// ---- 2. Do tuong dong (cosine) ----
string a = "Laptop Dell XPS 13 man hinh 13 inch";
string[] cacCau = ["Laptop Dell XPS 13 nhe va mong", "Chuot khong day Logitech", "Ban phim co Keychron K2", "May tinh xach tay Dell XPS"];
var ea = (await gen.GenerateAsync([a]))[0].Vector.ToArray();
Console.WriteLine($"\n=== 2. Do tuong dong voi: \"{a}\"");
foreach (var c in cacCau)
{
    var ec = (await gen.GenerateAsync([c]))[0].Vector.ToArray();
    Console.WriteLine($"   {System.Numerics.Tensors.TensorPrimitives.CosineSimilarity(ea, ec),6:F3}  {c}");
}
Console.WriteLine("   (Luu y: 'May tinh xach tay Dell XPS' cung nghia voi 'Laptop' nhung diem chi cao nho tu 'Dell XPS' — embedding gia KHONG hieu dong nghia)");

// ---- 3. Kho vector + tim kiem ----
var kho = new KhoVector();
var tuLieu = new (string Id, string Text, string Nhom)[]
{
    ("LT001", "Laptop Dell XPS 13 - man hinh 13 inch, RAM 16GB, pin 12 gio", "laptop"),
    ("LT002", "Laptop Asus Vivobook 15 - man hinh 15.6 inch, RAM 8GB, gia re", "laptop"),
    ("CH001", "Chuot khong day Logitech M331 - pin AA, yen tinh", "phu-kien"),
    ("BP001", "Ban phim co Keychron K2 - ket noi Bluetooth, switch do", "phu-kien"),
    ("MH001", "Man hinh Dell U2720Q 27 inch 4K - cong USB-C", "man-hinh"),
};
var vecs = await gen.GenerateAsync(tuLieu.Select(t => t.Text));
for (int i = 0; i < tuLieu.Length; i++)
    kho.Them(new Muc(tuLieu[i].Id, tuLieu[i].Text, vecs[i].Vector.ToArray(), new Dictionary<string, string> { ["nhom"] = tuLieu[i].Nhom }));

async Task Hoi(string q, int k = 2, float nguong = 0f, string? nhom = null)
{
    var vq = (await gen.GenerateAsync([q]))[0].Vector.ToArray();
    var kq = kho.Tim(vq, k, nguong, nhom is null ? null : m => m.SieuDuLieu["nhom"] == nhom);
    Console.WriteLine($"\n?  {q}   [k={k}, nguong={nguong}{(nhom is null ? "" : ", nhom=" + nhom)}]");
    if (kq.Count == 0) Console.WriteLine("   (khong co ket qua vuot nguong)");
    foreach (var r in kq) Console.WriteLine($"   {r.Diem,6:F3}  {r.Muc.Id}  {r.Muc.VanBan}");
}

Console.WriteLine("\n=== 3. Tim kiem tren kho vector ===");
await Hoi("laptop man hinh 13 inch pin lau");
await Hoi("chuot pin AA");
Console.WriteLine("\n=== 4. Nguong diem: cau hoi KHONG lien quan gi den kho ===");
await Hoi("cong thuc nau pho bo", k: 2, nguong: 0f);
await Hoi("cong thuc nau pho bo", k: 2, nguong: 0.2f);
Console.WriteLine("\n=== 5. Loc theo sieu du lieu (vd: quyen truy cap / nhom) ===");
await Hoi("Dell 13 inch", k: 3);
await Hoi("Dell 13 inch", k: 3, nhom: "man-hinh");
