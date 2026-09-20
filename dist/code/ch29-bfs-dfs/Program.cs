// Do thi khong huong (danh sach ke)
//     A --- B --- E
//     |     |
//     C --- D --- F
var g = new DoThi();
g.ThemCanh("A", "B");
g.ThemCanh("A", "C");
g.ThemCanh("B", "D");
g.ThemCanh("B", "E");
g.ThemCanh("C", "D");
g.ThemCanh("D", "F");

Console.WriteLine($"BFS tu A: {string.Join(" -> ", g.BFS("A"))}");
Console.WriteLine($"DFS tu A: {string.Join(" -> ", g.DFS("A"))}");
Console.WriteLine($"DFS de quy: {string.Join(" -> ", g.DFSDeQuy("A"))}");

var duong = g.DuongDiNganNhat("A", "F");
Console.WriteLine($"Duong ngan nhat A->F: {(duong is null ? "khong co" : string.Join(" -> ", duong))}");
Console.WriteLine($"Duong A->Z: {(g.DuongDiNganNhat("A", "Z") is null ? "khong co" : "co")}");

// Cay: duyet cay thu muc gia lap
var goc = new Nut("root",
[
    new Nut("src", [new Nut("Program.cs", []), new Nut("Utils.cs", [])]),
    new Nut("docs", [new Nut("readme.md", [])]),
    new Nut("build.sh", []),
]);
Console.WriteLine("--- cay thu muc (DFS) ---");
goc.In();
Console.WriteLine($"Tong so nut: {goc.DemNut()}, do sau: {goc.DoSau()}");

class DoThi
{
    private readonly Dictionary<string, List<string>> _ke = [];

    public void ThemCanh(string a, string b)
    {
        if (!_ke.ContainsKey(a)) _ke[a] = [];
        if (!_ke.ContainsKey(b)) _ke[b] = [];
        _ke[a].Add(b);
        _ke[b].Add(a);
    }

    // BFS: dung Queue, duyet theo tung "lop" tu gan den xa
    public List<string> BFS(string batDau)
    {
        var kq = new List<string>();
        var daTham = new HashSet<string> { batDau };
        var hangDoi = new Queue<string>();
        hangDoi.Enqueue(batDau);
        while (hangDoi.Count > 0)
        {
            var u = hangDoi.Dequeue();
            kq.Add(u);
            foreach (var v in _ke[u])
                if (daTham.Add(v)) hangDoi.Enqueue(v);
        }
        return kq;
    }

    // DFS: dung Stack, di sau nhat co the roi moi quay lui
    public List<string> DFS(string batDau)
    {
        var kq = new List<string>();
        var daTham = new HashSet<string>();
        var nganXep = new Stack<string>();
        nganXep.Push(batDau);
        while (nganXep.Count > 0)
        {
            var u = nganXep.Pop();
            if (!daTham.Add(u)) continue;
            kq.Add(u);
            // dao thu tu de tham theo thu tu them vao
            for (int i = _ke[u].Count - 1; i >= 0; i--)
                if (!daTham.Contains(_ke[u][i])) nganXep.Push(_ke[u][i]);
        }
        return kq;
    }

    public List<string> DFSDeQuy(string batDau)
    {
        var kq = new List<string>();
        var daTham = new HashSet<string>();
        void Tham(string u)
        {
            if (!daTham.Add(u)) return;
            kq.Add(u);
            foreach (var v in _ke[u]) Tham(v);
        }
        Tham(batDau);
        return kq;
    }

    // BFS tim duong ngan nhat (theo so canh) tren do thi khong trong so
    public List<string>? DuongDiNganNhat(string tu, string den)
    {
        if (!_ke.ContainsKey(tu) || !_ke.ContainsKey(den)) return null;
        var cha = new Dictionary<string, string?> { [tu] = null };
        var hangDoi = new Queue<string>();
        hangDoi.Enqueue(tu);
        while (hangDoi.Count > 0)
        {
            var u = hangDoi.Dequeue();
            if (u == den)
            {
                var duong = new List<string>();
                for (string? x = den; x is not null; x = cha[x]) duong.Add(x);
                duong.Reverse();
                return duong;
            }
            foreach (var v in _ke[u])
            {
                if (cha.ContainsKey(v)) continue;
                cha[v] = u;
                hangDoi.Enqueue(v);
            }
        }
        return null;
    }
}

class Nut(string ten, List<Nut> con)
{
    public string Ten { get; } = ten;
    public List<Nut> Con { get; } = con;

    public void In(int thut = 0)
    {
        Console.WriteLine($"{new string(' ', thut * 2)}{Ten}");
        foreach (var c in Con) c.In(thut + 1);
    }

    public int DemNut() => 1 + Con.Sum(c => c.DemNut());
    public int DoSau() => 1 + (Con.Count == 0 ? 0 : Con.Max(c => c.DoSau()));
}
