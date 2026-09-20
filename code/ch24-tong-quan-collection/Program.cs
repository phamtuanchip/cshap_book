// List<T>: danh sach co thu tu, truy cap theo chi so
var ds = new List<string> { "An", "Binh" };
ds.Add("Chi");
ds.Insert(0, "Dung");
ds.Remove("Binh");
Console.WriteLine($"List: {string.Join(", ", ds)}  (Count = {ds.Count})");

// Dictionary<K,V>: tra cuu nhanh theo khoa
var tuoi = new Dictionary<string, int> { ["An"] = 20, ["Binh"] = 25 };
tuoi["Chi"] = 30;
if (tuoi.TryGetValue("An", out int tuoiAn)) Console.WriteLine($"Tuoi An = {tuoiAn}");
foreach (var (ten, t) in tuoi) Console.WriteLine($"  {ten}: {t}");

// HashSet<T>: tap hop khong trung lap
var so = new HashSet<int> { 1, 2, 3, 3, 3 };
Console.WriteLine($"HashSet: {string.Join(", ", so)}  (Count = {so.Count})");
Console.WriteLine($"Chua 2? {so.Contains(2)}");

// Queue<T>: vao truoc ra truoc (FIFO)
var hangDoi = new Queue<string>();
hangDoi.Enqueue("khach 1");
hangDoi.Enqueue("khach 2");
Console.WriteLine($"Queue: phuc vu {hangDoi.Dequeue()}, tiep theo {hangDoi.Peek()}");

// Stack<T>: vao sau ra truoc (LIFO)
var nganXep = new Stack<string>();
nganXep.Push("trang 1");
nganXep.Push("trang 2");
Console.WriteLine($"Stack: quay lai {nganXep.Pop()}");

// So sanh toc do tim kiem: List vs HashSet
var lon = Enumerable.Range(0, 100_000).ToList();
var lonSet = new HashSet<int>(lon);
var sw = System.Diagnostics.Stopwatch.StartNew();
int dem1 = 0;
for (int i = 99_000; i < 100_000; i++) if (lon.Contains(i)) dem1++;
Console.WriteLine($"List.Contains 1000 lan:    {sw.ElapsedMilliseconds} ms (tim {dem1})");
sw.Restart();
int dem2 = 0;
for (int i = 99_000; i < 100_000; i++) if (lonSet.Contains(i)) dem2++;
Console.WriteLine($"HashSet.Contains 1000 lan: {sw.ElapsedMilliseconds} ms (tim {dem2})");

// Collection expression & bat bien
IReadOnlyList<int> chiDoc = [1, 2, 3];
Console.WriteLine($"ReadOnly: {string.Join(",", chiDoc)}");
