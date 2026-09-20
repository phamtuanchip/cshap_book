// 1. Tu cai dat mang dong de hieu List<T> ben trong
var mang = new MangDong<int>();
for (int i = 1; i <= 10; i++)
{
    mang.Them(i * 10);
    Console.WriteLine($"Them {i * 10}: Count={mang.Count}, Capacity={mang.Capacity}");
}
Console.WriteLine($"mang[3] = {mang[3]}");

// 2. List<T> that: Capacity tang gap doi
var ds = new List<int>();
int capCu = -1;
for (int i = 0; i < 20; i++)
{
    ds.Add(i);
    if (ds.Capacity != capCu)
    {
        capCu = ds.Capacity;
        Console.WriteLine($"List<T>: Count={ds.Count}, Capacity={ds.Capacity}");
    }
}

// 3. Cac thao tac cua List
var so = new List<int> { 5, 3, 8, 1, 9, 2 };
so.Sort();
Console.WriteLine($"Sort: {string.Join(",", so)}");
Console.WriteLine($"BinarySearch(8) = {so.BinarySearch(8)}");
Console.WriteLine($"IndexOf(9) = {so.IndexOf(9)}");
so.RemoveAll(x => x % 2 == 0);
Console.WriteLine($"RemoveAll chan: {string.Join(",", so)}");
Console.WriteLine($"Tim dau tien > 3: {so.Find(x => x > 3)}");

// 4. LinkedList<T>: them/xoa o giua nhanh neu da co node
var ll = new LinkedList<string>();
ll.AddLast("B");
ll.AddFirst("A");
var nodeB = ll.Find("B")!;
ll.AddAfter(nodeB, "C");
ll.AddBefore(nodeB, "b0");
Console.WriteLine($"LinkedList: {string.Join(" <-> ", ll)}");
ll.Remove(nodeB);   // O(1) vi da co node
Console.WriteLine($"Sau xoa B: {string.Join(" <-> ", ll)}");

// 5. Loi sua danh sach khi dang foreach
try
{
    var t = new List<int> { 1, 2, 3 };
    foreach (var x in t)
        if (x == 2) t.Remove(x);
}
catch (InvalidOperationException e)
{
    Console.WriteLine($"Loi: {e.Message}");
}

// Cach dung: lap nguoc bang for, hoac RemoveAll
var t2 = new List<int> { 1, 2, 3, 2 };
for (int i = t2.Count - 1; i >= 0; i--)
    if (t2[i] == 2) t2.RemoveAt(i);
Console.WriteLine($"Sau khi xoa an toan: {string.Join(",", t2)}");

// Mang dong tu cai dat (rut gon): minh hoa co che nhan doi
class MangDong<T>
{
    private T[] _data = new T[2];
    public int Count { get; private set; }
    public int Capacity => _data.Length;

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _data[index];
        }
    }

    public void Them(T item)
    {
        if (Count == _data.Length)
            Array.Resize(ref _data, _data.Length * 2);   // nhan doi + sao chep: O(n) nhung hiem
        _data[Count++] = item;
    }
}
