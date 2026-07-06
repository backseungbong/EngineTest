using JHLib.Util.ArrayControl;
using JHLib.Util.List;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Sort
{
    public unsafe class QuickSort
    {
        private readonly record struct LowHigh(int Low, int High)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Get(out int low, out int high) { low = Low; high = High; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static LowHigh[] Resize(LowHigh[] b)
        {
            var l = b.Length;
            if (l != 0) return AC.CopyNew(b, l * 2, l);
            return new LowHigh[4];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run<T>(SList<T> list) where T : unmanaged, INumber<T> =>
            Run(ref list.Ref0, list.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run<T>(T[] array) where T : unmanaged, INumber<T> =>
            Run(ref MemoryMarshal.GetArrayDataReference(array), array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run<T>(Span<T> span) where T : unmanaged, INumber<T> =>
            Run(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run<T>(ref T array0, int count) where T : unmanaged, INumber<T>
        {
            fixed (T* p0 = &array0)
            {
                Run(p0, count);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run<T>(T* p0, int count) where T : unmanaged, INumber<T>
        {
            if (count > 1)
                RunInternal(p0, 0, count - 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunInternal<T>(T* p0, int idx1, int idx2) where T : unmanaged, INumber<T>
        {
            const int MAX_INSERT_SORT = 32;

            if (idx2 - idx1 > MAX_INSERT_SORT) // 삽입정렬 갯수 초과시 퀵 정렬로 정렬
            {
                var ldx = idx1;
                var hdx = idx2;
                var buk = Array.Empty<LowHigh>();
                var cnt = 0;

                while (true)
                {
                    var l = p0 + (uint)ldx;
                    var h = p0 + (uint)hdx;
                    var p = *(p0 + (uint)(ldx + hdx >> 1));

                    do
                    {
                        if (*(l + 0) < p == false) { goto L1; }
                        if (*(l + 1) < p == false) { goto L2; }
                        if (*(l + 2) < p == false) { goto L3; }
                        if (*(l + 3) < p == false) { goto L4; }
                    L0: l += 4;
                        if (*(l + 0) < p == false) { goto L1; }
                        if (*(l + 1) < p == false) { goto L2; }
                        if (*(l + 2) < p == false) { goto L3; }
                        if (*(l + 3) < p == false) { goto L4; } else { goto L0; }
                    L4: l++;
                    L3: l++;
                    L2: l++;
                    L1: if (*(h - 0) > p == false) { goto H1; }
                        if (*(h - 1) > p == false) { goto H2; }
                        if (*(h - 2) > p == false) { goto H3; }
                        if (*(h - 3) > p == false) { goto H4; }
                    H0: h -= 4;
                        if (*(h - 0) > p == false) { goto H1; }
                        if (*(h - 1) > p == false) { goto H2; }
                        if (*(h - 2) > p == false) { goto H3; }
                        if (*(h - 3) > p == false) { goto H4; } else { goto H0; }
                    H4: h--;
                    H3: h--;
                    H2: h--;
                    H1: if (l > h) { break; }
                        if (l < h) { var t = *l; *l = *h; *h = t; }
                    }
                    while (++l <= --h);

                    var hc = hdx - (int)(l - p0);
                    var lc = (int)(h - p0) - ldx;
                    if (lc >= MAX_INSERT_SORT && hc >= MAX_INSERT_SORT)
                    {
                        if (cnt == buk.Length) { buk = Resize(buk); }
                        if (lc <= hc)
                        {
                            buk[cnt++] = new(hdx - hc, hdx);
                            hdx = ldx + lc;
                        }
                        else
                        {
                            buk[cnt++] = new(ldx, ldx + lc);
                            ldx = hdx - hc;
                        }
                    }
                    else if (lc >= MAX_INSERT_SORT) { hdx = ldx + lc; }
                    else if (hc >= MAX_INSERT_SORT) { ldx = hdx - hc; }
                    else if (cnt != 0) { buk[--cnt].Get(out ldx, out hdx); }
                    else { break; }
                }

                var s = p0 + (uint)idx1;
                var e = p0 + (uint)idx2;
                var n = s;
                do
                {
                    n++;
                    if (*n < *(n - 1))
                    {
                        var t = *n; var m = n;
                        do { m--; *(m + 1) = *m; }
                        while (m > s && *(m - 1) > t);
                        *m = t;
                    }
                }
                while (n < s + MAX_INSERT_SORT);

                // 앞부분은 정렬이 되었으므로 이후의 데이타는 범위 검사(m > s)를 빼고 값비교만으로 루프 언롤링 정렬진행
                do
                {
                    n++;
                    if (*n < *(n - 1))
                    {
                        var t = *n; var m = n;
                        while (true)
                        {
                            *(m - 0) = *(m - 1); if (t < *(m - 2) == false) { goto G1; }
                            *(m - 1) = *(m - 2); if (t < *(m - 3) == false) { goto G2; }
                            *(m - 2) = *(m - 3); if (t < *(m - 4) == false) { goto G3; }
                            *(m - 3) = *(m - 4); m -= 4; if (t < *(m - 1) == false) { goto G4; }
                        }
                    G4: m += 3;
                    G3: m -= 1;
                    G2: m -= 1;
                    G1: *(m - 1) = t;
                    }
                }
                while (n < e);
            }
            else
            {
                var s = p0 + (uint)idx1;
                var e = p0 + (uint)idx2;
                var n = s;
                do
                {
                    n++;
                    if (*n < *(n - 1))
                    {
                        var t = *n; var m = n;
                        do { m--; *(m + 1) = *m; }
                        while (m > s && *(m - 1) > t);
                        *m = t;
                    }
                }
                while (n < e);
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public readonly struct IndexFloat(float value, int index)
        {
            public readonly float Value = value;
            public readonly int Index = index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator float(IndexFloat f) => f.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run(IndexFloat[] array) => Run(ref MemoryMarshal.GetArrayDataReference(array), array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run(ref IndexFloat array0, int count)
        {
            fixed (IndexFloat* p0 = &array0)
            {
                Run(p0, count);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run(IndexFloat* p0, int count)
        {
            if (count > 1)
                RunInternal(p0, 0, count - 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunInternal(IndexFloat* p0, int idx1, int idx2)
        {
            const int MAX_INSERT_SORT = 32;

            if (idx2 - idx1 > MAX_INSERT_SORT) // 삽입정렬 갯수 초과시 퀵 정렬로 정렬
            {
                var ldx = idx1;
                var hdx = idx2;
                var buk = Array.Empty<LowHigh>();
                var cnt = 0;

                while (true)
                {
                    var l = p0 + (uint)ldx;
                    var h = p0 + (uint)hdx;
                    var p = *(p0 + (uint)(ldx + hdx >> 1));

                    do
                    {
                        if (*(l + 0) < p == false) { goto L1; }
                        if (*(l + 1) < p == false) { goto L2; }
                        if (*(l + 2) < p == false) { goto L3; }
                        if (*(l + 3) < p == false) { goto L4; }
                    L0: l += 4;
                        if (*(l + 0) < p == false) { goto L1; }
                        if (*(l + 1) < p == false) { goto L2; }
                        if (*(l + 2) < p == false) { goto L3; }
                        if (*(l + 3) < p == false) { goto L4; } else { goto L0; }
                    L4: l++;
                    L3: l++;
                    L2: l++;
                    L1: if (*(h - 0) > p == false) { goto H1; }
                        if (*(h - 1) > p == false) { goto H2; }
                        if (*(h - 2) > p == false) { goto H3; }
                        if (*(h - 3) > p == false) { goto H4; }
                    H0: h -= 4;
                        if (*(h - 0) > p == false) { goto H1; }
                        if (*(h - 1) > p == false) { goto H2; }
                        if (*(h - 2) > p == false) { goto H3; }
                        if (*(h - 3) > p == false) { goto H4; } else { goto H0; }
                    H4: h--;
                    H3: h--;
                    H2: h--;
                    H1: if (l > h) { break; }
                        if (l < h) { var t = *l; *l = *h; *h = t; }
                    }
                    while (++l <= --h);

                    var hc = hdx - (int)(l - p0);
                    var lc = (int)(h - p0) - ldx;
                    if (lc >= MAX_INSERT_SORT && hc >= MAX_INSERT_SORT)
                    {
                        if (cnt == buk.Length) { buk = Resize(buk); }
                        if (lc <= hc)
                        {
                            buk[cnt++] = new(hdx - hc, hdx);
                            hdx = ldx + lc;
                        }
                        else
                        {
                            buk[cnt++] = new(ldx, ldx + lc);
                            ldx = hdx - hc;
                        }
                    }
                    else if (lc >= MAX_INSERT_SORT) { hdx = ldx + lc; }
                    else if (hc >= MAX_INSERT_SORT) { ldx = hdx - hc; }
                    else if (cnt != 0) { buk[--cnt].Get(out ldx, out hdx); }
                    else { break; }
                }

                var s = p0 + (uint)idx1;
                var e = p0 + (uint)idx2;
                var n = s;
                do
                {
                    n++;
                    if (*n < *(n - 1))
                    {
                        var t = *n; var m = n;
                        do { m--; *(m + 1) = *m; }
                        while (m > s && *(m - 1) > t);
                        *m = t;
                    }
                }
                while (n < s + MAX_INSERT_SORT);

                // 앞부분은 정렬이 되었으므로 이후의 데이타는 범위 검사(m > s)를 빼고 값비교만으로 루프 언롤링 정렬진행
                do
                {
                    n++;
                    if (*n < *(n - 1))
                    {
                        var t = *n; var m = n;
                        while (true)
                        {
                            *(m - 0) = *(m - 1); if (t < *(m - 2) == false) { goto G1; }
                            *(m - 1) = *(m - 2); if (t < *(m - 3) == false) { goto G2; }
                            *(m - 2) = *(m - 3); if (t < *(m - 4) == false) { goto G3; }
                            *(m - 3) = *(m - 4); m -= 4; if (t < *(m - 1) == false) { goto G4; }
                        }
                    G4: m += 3;
                    G3: m -= 1;
                    G2: m -= 1;
                    G1: *(m - 1) = t;
                    }
                }
                while (n < e);
            }
            else
            {
                var s = p0 + (uint)idx1;
                var e = p0 + (uint)idx2;
                var n = s;
                do
                {
                    n++;
                    if (*n < *(n - 1))
                    {
                        var t = *n; var m = n;
                        do { m--; *(m + 1) = *m; }
                        while (m > s && *(m - 1) > t);
                        *m = t;
                    }
                }
                while (n < e);
            }
        }
    }
}