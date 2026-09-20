int[] du = Enumerable.Range(0, 1000).Select(i => i * 3).ToArray();   // da sap xep: 0,3,6,...,2997
int can = 2001;   // 2001 = 3 * 667

Console.WriteLine($"Tim {can} trong {du.Length} phan tu:");
int v1 = TimTuanTu.Tim(du, can, out int b1);
int v2 = TimNhiPhan.Tim(du, can, out int b2);
int v3 = TimNhay.Tim(du, can, out int b3);
int v4 = TimNoiSuy.Tim(du, can, out int b4);
Console.WriteLine($"  Linear        : vi tri {v1}, {b1} buoc");
Console.WriteLine($"  Binary        : vi tri {v2}, {b2} buoc");
Console.WriteLine($"  Jump          : vi tri {v3}, {b3} buoc");
Console.WriteLine($"  Interpolation : vi tri {v4}, {b4} buoc");

Console.WriteLine();
Console.WriteLine($"Khong ton tai (2002): linear={TimTuanTu.Tim(du, 2002, out _)}, binary={TimNhiPhan.Tim(du, 2002, out _)}");

// Thu voi mang nho de kiem chung
int[] nho = [2, 5, 8, 12, 16, 23, 38, 56, 72, 91];
foreach (int x in new[] { 2, 23, 91, 7 })
    Console.WriteLine($"Tim {x}: {TimTuanTu.Tim(nho, x, out _)}, {TimNhiPhan.Tim(nho, x, out _)}, {TimNhay.Tim(nho, x, out _)}, {TimNoiSuy.Tim(nho, x, out _)}");

static class TimTuanTu
{
    // O(n): khong yeu cau mang da sap xep
    public static int Tim(int[] a, int x, out int buoc)
    {
        buoc = 0;
        for (int i = 0; i < a.Length; i++)
        {
            buoc++;
            if (a[i] == x) return i;
        }
        return -1;
    }
}

static class TimNhiPhan
{
    // O(log n): mang PHAI da sap xep
    public static int Tim(int[] a, int x, out int buoc)
    {
        buoc = 0;
        int lo = 0, hi = a.Length - 1;
        while (lo <= hi)
        {
            buoc++;
            int mid = lo + (hi - lo) / 2;   // tranh tran so cua (lo + hi) / 2
            if (a[mid] == x) return mid;
            if (a[mid] < x) lo = mid + 1;
            else hi = mid - 1;
        }
        return -1;
    }
}

static class TimNhay
{
    // O(sqrt n): nhay theo buoc sqrt(n), sau do tim tuan tu trong khoi
    public static int Tim(int[] a, int x, out int buoc)
    {
        buoc = 0;
        int n = a.Length;
        if (n == 0) return -1;
        int buocNhay = (int)Math.Sqrt(n);
        int truoc = 0;
        int cur = buocNhay;
        while (cur < n && a[cur - 1] < x)
        {
            buoc++;
            truoc = cur;
            cur += buocNhay;
        }
        int het = Math.Min(cur, n);
        for (int i = truoc; i < het; i++)
        {
            buoc++;
            if (a[i] == x) return i;
            if (a[i] > x) break;
        }
        return -1;
    }
}

static class TimNoiSuy
{
    // Trung binh O(log log n) voi du lieu phan bo deu; xau nhat O(n)
    public static int Tim(int[] a, int x, out int buoc)
    {
        buoc = 0;
        int lo = 0, hi = a.Length - 1;
        while (lo <= hi && x >= a[lo] && x <= a[hi])
        {
            buoc++;
            if (a[hi] == a[lo]) return a[lo] == x ? lo : -1;
            // uoc luong vi tri theo ti le tuyen tinh
            int pos = lo + (int)((long)(x - a[lo]) * (hi - lo) / (a[hi] - a[lo]));
            if (a[pos] == x) return pos;
            if (a[pos] < x) lo = pos + 1;
            else hi = pos - 1;
        }
        return -1;
    }
}
