using JHLib.Util.ArrayControl;
using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Graphic.Data
{
    public unsafe class EdgeManager
    {
        private const int MARGIN = 1;
        private readonly record struct LowHigh(int Low, int High)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Get(out int low, out int high) { low = Low; high = High; }
        }
        private readonly LowHigh[] _lowHighs = new LowHigh[256];

        private Edge[] _edges;
        private int _cap;
        private int _cnt;
        private float _ymin;
        private float _ymax;

        public void Clear() => _cnt = MARGIN;
        public ref Edge Edges0 => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_edges), MARGIN);
        public float YMin => _ymin;
        public float YMax => _ymax;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EdgeManager()
        {
            var edges = new Edge[256];
            edges[0] = new Edge(float.NegativeInfinity);
            _edges = edges;
            _cap = 256;
            _cnt = MARGIN;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int need)
        {
            var cap = _cap * 2;
            var cnt = _cnt;
            while (cap < need) cap *= 2;

            var edges = new Edge[cap];
            if (cnt != MARGIN) _edges.AsSpan(0, cnt).CopyTo(edges);
            edges[0] = new Edge(float.NegativeInfinity);
            _edges = edges;
            _cap = cap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadyEdge(float h1, float h2)
        {
            var cnt = _cnt; _cnt = MARGIN;
            if (cnt != MARGIN)
            {
                var ymin = SIMD.Max(h1, _ymin);
                var ymax = SIMD.Min(h2, _ymax);
                if (ymin < ymax)
                {
                    _ymin = ymin;
                    _ymax = ymax;
                    SortEdge(_edges, cnt, _lowHighs);
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AddRect(float x1, float y1, float x2, float y2)
        {
            var rst = false;
            if (x1 < x2 && y1 < y2)
            {
                int cnt = _cnt;
                int need = cnt + 2 + 2; // 최소 2개의 Edge 여유공간 필요
                if (need > _cap) Resize(need);

                ref var e0 = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_edges), (uint)cnt);
                Unsafe.Add(ref e0, 0) = new Edge(x1, y1, x1, y2);
                Unsafe.Add(ref e0, 1) = new Edge(x2, y1, x2, y2);

                var ymin = y1;
                var ymax = y2;
                if (cnt != MARGIN)
                {
                    ymin = SIMD.Min(y1, _ymin);
                    ymax = SIMD.Max(y2, _ymax);
                }
                _ymin = ymin;
                _ymax = ymax;
                _cnt = cnt + 2;
                rst = true;
            }
            return rst;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AddPoints(Float2D* p0, int pc, float h1, float h2)
        {
            var p = p0 + 1;
            var e = p0 + (uint)pc;

            var ymin = h2;
            var ymax = h1;

            int cnt = _cnt;
            int need = cnt + pc + 2; // 최소 2개의 Edge 여유공간 필요
            if (need > _cap) Resize(need);

            fixed (Edge* edge0 = &MemoryMarshal.GetArrayDataReference(_edges))
            {
                var e0 = edge0 + (uint)cnt;
                var f2 = (Float2D*)e0;
                var pz = *p0;
                var y1 = pz.Y;
                do
                {
                    var vp = Sse2.LoadVector128((double*)(p - 1));
                    var y2 = p->Y;
                    if (y2 > y1)
                    {
                        ymin = SIMD.Min(ymin, y1);
                        ymax = SIMD.Max(ymax, y2);
                        Sse2.Store((double*)f2, vp);
                        f2 += 2;
                    }
                    else if (y2 < y1)
                    {
                        ymin = SIMD.Min(ymin, y2);
                        ymax = SIMD.Max(ymax, y1);
                        Sse2.StoreHigh((double*)f2, vp);
                        Sse2.StoreScalar((double*)f2 + 1, vp);
                        f2 += 2;
                    }
                    y1 = y2;
                }
                while (++p < e);

                var ye = (e - 1)->Y;
                if (ye > pz.Y)
                {
                    ymin = SIMD.Min(ymin, pz.Y);
                    ymax = SIMD.Max(ymax, ye);
                    f2[0] = pz;
                    f2[1] = *(e - 1);
                    f2 += 2;
                }
                else if (ye < pz.Y)
                {
                    ymin = SIMD.Min(ymin, ye);
                    ymax = SIMD.Max(ymax, pz.Y);
                    f2[0] = *(e - 1);
                    f2[1] = pz;
                    f2 += 2;
                }

                var rst = false;
                var add = (int)((nint)f2 - (nint)e0 >> 4);
                if (add >= 2 && ymin < h2 && h1 < ymax)
                {
                    if (cnt != MARGIN)
                    {
                        ymin = SIMD.Min(ymin, _ymin);
                        ymax = SIMD.Max(ymax, _ymax);
                    }
                    _ymin = ymin;
                    _ymax = ymax;
                    _cnt = cnt + add;
                    rst = true;
                }
                return rst;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static LowHigh[] Resize(LowHigh[] b)
        {
            var l = b.Length;
            if (l != 0) return AC.CopyNew(b, l * 2, l);
            return new LowHigh[4];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void SortEdge(Edge[] edges, int edgen, LowHigh[] buk)
        {
            const int MAX_INSERT_SORT = 24;

            fixed (Edge* e0 = &MemoryMarshal.GetArrayDataReference(edges))
            {
                var idx1 = MARGIN;
                var idx2 = edgen - 1;

                if (idx2 - idx1 > MAX_INSERT_SORT) // 삽입정렬 갯수 초과시 퀵 정렬로 정렬
                {
                    var ldx = idx1;
                    var hdx = idx2;
                    var cnt = 0;

                    while (true)
                    {
                        var l = e0 + (uint)ldx;
                        var h = e0 + (uint)hdx;
                        var p = (e0 + (uint)(ldx + hdx >> 1))->YMin;

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

                        var hc = hdx - (int)((nint)l - (nint)e0 >> 4);
                        var lc = (int)((nint)h - (nint)e0 >> 4) - ldx;
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
                }

                // 0번째 배열에 미리 YMin 최소 값(-Infinity)을 정의하여 배열 경계체크를 제거
                var s = e0 + (uint)idx1;
                var e = e0 + (uint)idx2;
                do
                {
                    var y = (++s)->YMin;
                    if (y < (s - 1)->YMin)
                    {
                        var t = *s; var m = s;
                        while (true)
                        {
                            *(m - 0) = *(m - 1); if (y < (m - 2)->YMin == false) { goto J1; }
                            *(m - 1) = *(m - 2); if (y < (m - 3)->YMin == false) { goto J2; }
                            *(m - 2) = *(m - 3); if (y < (m - 4)->YMin == false) { goto J3; }
                            *(m - 3) = *(m - 4); m -= 4; if (y < (m - 1)->YMin == false) { break; }
                        }
                        m += 3;
                    J3: m -= 1;
                    J2: m -= 1;
                    J1: *(m - 1) = t;
                    }
                }
                while (s < e);
                *(e + 1) = new Edge(float.PositiveInfinity);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Edge* CleanSort(Edge* e, Edge* ea, float y)
        {
            var t = e;
            do
            {
                if (t->YMax > y)
                {
                    var x = t->XMin;
                    if (x < (e - 1)->XMin)
                    {
                        var v = *t; var m = e;
                        while (true)
                        {
                            *(m - 0) = *(m - 1); if (x < (m - 2)->XMin == false) { goto J1; }
                            *(m - 1) = *(m - 2); if (x < (m - 3)->XMin == false) { goto J2; }
                            *(m - 2) = *(m - 3); if (x < (m - 4)->XMin == false) { goto J3; }
                            *(m - 3) = *(m - 4); m -= 4; if (x < (m - 1)->XMin == false) { break; }
                        }
                        m += 3;
                    J3: m -= 1;
                    J2: m -= 1;
                    J1: *(m - 1) = v;
                    }
                    else if (e != t) { *e = *t; }
                    e++;
                }
            }
            while (++t < ea);
            return e;
        }
    }
}