using JHLib.Util.Helper;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    [Flags]
    internal enum VertexFlags : int
    {
        None = 0,
        OpenStart = 1,
        OpenEnd = 2,
        LocalMax = 4,
        LocalMin = 8,

        StartEnd = OpenStart | OpenEnd,
        StartMax = OpenStart | LocalMax,
        StartMin = OpenStart | LocalMin,
        EndMax = OpenEnd | LocalMax,
        EndMin = OpenEnd | LocalMin
    }

    internal class Vertex
    {
        public Long2D Pt;
        public Vertex Prev;
        public Vertex Next;
        public VertexFlags Flags;
        public bool IsOpenEnd => (Flags & VertexFlags.StartEnd) != 0;
        public bool IsMaxima => (Flags & VertexFlags.LocalMax) != 0;
    }


    internal class VertexCreator
    {
        private Vertex[] _buk;
        private int _cap;
        private int _cnt;
        internal Vertex[] Bucket => _buk;
        public int Count { get => _cnt; set => _cnt = value; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int add)
        {
            var cap = _cap;
            var buk = RefCommand.RefCopyNew(_buk, MathHelper.RoundUpToPow2(8, _cnt + add), cap);

            do buk[cap] = new();
            while (++cap < buk.Length);

            _buk = buk;
            _cap = buk.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _cnt = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref Vertex EnsureBucket0(int size)
        {
            if (size > _cap) return ref EnsureBucket0Internal(size);
            return ref MemoryMarshal.GetArrayDataReference(_buk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref Vertex EnsureBucket0Internal(int size)
        {
            var cap = _cap;
            var buk = RefCommand.RefCopyNew(_buk, MathHelper.RoundUpToPow2(8, size), cap);

            do buk[cap] = new();
            while (++cap < buk.Length);

            _buk = buk;
            _cap = buk.Length;

            return ref MemoryMarshal.GetArrayDataReference(buk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ReadyVertexOpen(ref Float2D path0, int pathc, PathType type, double scale, LocalMinimaList minimas)
        {
            var idx = _cnt;
            if (idx + pathc > _cap) Resize(pathc);
            ref var p0 = ref Unsafe.Add(ref path0, 1);
            ref var pe = ref Unsafe.Add(ref path0, pathc);
            ref var b0 = ref MemoryMarshal.GetArrayDataReference(_buk);
            var cnt = idx;

            long x2 = (long)(path0.X * scale);
            long y2 = (long)(path0.Y * scale);
            long x1, y1, y0 = y2;
            var v0 = Unsafe.Add(ref b0, idx++);
            var vp = v0;

            var lminStartIndex = minimas.Count;
            LocalMinima lmin;

            vp.Pt = new(x2, y2);
            var i = 1;
            do
            {
                var y = (long)(Unsafe.Add(ref path0, i).Y * scale);
                if (y0 > y) break;
                if (y0 < y)
                {
                    vp.Flags = VertexFlags.StartMax;
                    goto DW_LOOP;
                }
            }
            while (++i < pathc);

            vp.Flags = VertexFlags.StartMin;
            lmin = minimas.AddGet();
            lmin.Vertex = vp;
            lmin.Pathtype = type;
            lmin.IsOpen = true;

        UP_LOOP:
            if (Unsafe.AreSame(ref p0, ref pe)) goto UP_END;
            x1 = x2; x2 = (long)(p0.X * scale);
            y1 = y2; y2 = (long)(p0.Y * scale); p0 = ref Unsafe.AddByteOffset(ref p0, 8);
            if (x1 == x2 && y1 == y2) goto UP_LOOP;
            vp = v0; v0 = Unsafe.Add(ref b0, idx++);
            v0.Pt = new(x2, y2);
            v0.Flags = 0;
            v0.Prev = vp;
            vp.Next = v0;
            if (y1 >= y2) goto UP_LOOP;
            vp.Flags = VertexFlags.LocalMax;

        DW_LOOP:
            if (Unsafe.AreSame(ref p0, ref pe)) goto DW_END;
            x1 = x2; x2 = (long)(p0.X * scale);
            y1 = y2; y2 = (long)(p0.Y * scale); p0 = ref Unsafe.AddByteOffset(ref p0, 8);
            if (x1 == x2 && y1 == y2) goto DW_LOOP;
            vp = v0; v0 = Unsafe.Add(ref b0, idx++);
            v0.Pt = new(x2, y2);
            v0.Flags = 0;
            v0.Prev = vp;
            vp.Next = v0;
            if (y1 <= y2) goto DW_LOOP;
            vp.Flags = VertexFlags.LocalMin;
            lmin = minimas.AddGet();
            lmin.Vertex = vp;
            lmin.Pathtype = type;
            lmin.IsOpen = true;
            goto UP_LOOP;

        UP_END:
            vp = v0; v0 = Unsafe.Add(ref b0, cnt);
            vp.Flags = VertexFlags.EndMax;
            goto FINAL;

        DW_END:
            vp = v0; v0 = Unsafe.Add(ref b0, cnt);
            vp.Flags = VertexFlags.EndMin;
            lmin = minimas.AddGet();
            lmin.Vertex = vp;
            lmin.Pathtype = type;
            lmin.IsOpen = true;

        FINAL:
            if (idx - cnt > 1)
            {
                v0.Prev = vp;
                vp.Next = v0;
                _cnt = idx;

                minimas.NeedSorting = true;
                return;
            }

            minimas.Count = lminStartIndex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ReadyVertexClose(ref Float2D path0, int pathc, PathType type, double scale, LocalMinimaList minimas)
        {
            var idx = _cnt;
            if (idx + pathc > _cap) Resize(pathc);
            ref var p0 = ref Unsafe.Add(ref path0, 1);
            ref var pe = ref Unsafe.Add(ref path0, pathc);
            ref var b0 = ref MemoryMarshal.GetArrayDataReference(_buk);
            var cnt = idx;

            long x2 = (long)(path0.X * scale);
            long y2 = (long)(path0.Y * scale);
            long x1, y1, y0 = y2;
            var v0 = Unsafe.Add(ref b0, idx++);
            var vp = v0;

            var lminStartIndex = minimas.Count;
            LocalMinima lmin;

            vp.Pt = new(x2, y2);
            vp.Flags = 0;
            do
            {
                var y = (long)(Unsafe.Add(ref path0, --pathc).Y * scale);
                if (y > y0) goto UP_LOOP;
                if (y < y0) goto DW_LOOP;
            }
            while (pathc != 0);
            return;

        UP_LOOP:
            if (Unsafe.AreSame(ref p0, ref pe)) goto UP_END;
            x1 = x2; x2 = (long)(p0.X * scale);
            y1 = y2; y2 = (long)(p0.Y * scale); p0 = ref Unsafe.AddByteOffset(ref p0, 8);
            if (x1 == x2 && y1 == y2) goto UP_LOOP;
            vp = v0;
            v0 = Unsafe.Add(ref b0, idx++);
            v0.Pt = new(x2, y2);
            v0.Flags = 0;
            v0.Prev = vp;
            vp.Next = v0;
            if (y1 >= y2) goto UP_LOOP;
            vp.Flags = VertexFlags.LocalMax;

        DW_LOOP:
            if (Unsafe.AreSame(ref p0, ref pe)) goto DW_END;
            x1 = x2; x2 = (long)(p0.X * scale);
            y1 = y2; y2 = (long)(p0.Y * scale); p0 = ref Unsafe.AddByteOffset(ref p0, 8);
            if (x1 == x2 && y1 == y2) goto DW_LOOP;
            vp = v0;
            v0 = Unsafe.Add(ref b0, idx++);
            v0.Pt = new(x2, y2);
            v0.Flags = 0;
            v0.Prev = vp;
            vp.Next = v0;
            if (y1 <= y2) goto DW_LOOP;
            vp.Flags = VertexFlags.LocalMin;
            lmin = minimas.AddGet();
            lmin.Vertex = vp;
            lmin.Pathtype = type;
            lmin.IsOpen = false;
            goto UP_LOOP;

        UP_END:
            vp = v0; v0 = Unsafe.Add(ref b0, cnt);
            if (y2 < y0)
                vp.Flags = VertexFlags.LocalMax;
            goto FINAL;

        DW_END:
            vp = v0; v0 = Unsafe.Add(ref b0, cnt);
            if (y2 > y0)
            {
                vp.Flags = VertexFlags.LocalMin;
                lmin = minimas.AddGet();
                lmin.Vertex = vp;
                lmin.Pathtype = type;
                lmin.IsOpen = false;
            }

        FINAL:
            if (idx - cnt > 2)
            {
                if (vp.Pt == v0.Pt)
                {
                    if (idx - cnt == 3) goto FAILED;
                    vp = vp.Prev;
                }
                v0.Prev = vp;
                vp.Next = v0;
                _cnt = idx;

                minimas.NeedSorting = true;
                return;
            }

        FAILED:
            minimas.Count = lminStartIndex;
        }
    }
}