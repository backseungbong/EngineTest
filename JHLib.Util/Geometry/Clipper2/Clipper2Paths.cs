using JHLib.Util.ArrayControl;
using JHLib.Util.Helper;
using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    using static JHLib.Util.Helper.RefCommand;

    public class Clipper2Paths
    {
        private const int MIN_ENTRY = 4;
        private const int MIN_FLOAT2D = 8;

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly struct Entry(int index, int count)
        {
            public readonly int Index = index;
            public readonly int Count = count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DataRange<Float2D> GetDataRange(Float2D[] fbuk) => new(ref RefT(fbuk, Index), Count);
        }

        private Entry[] _ebuk;
        private int _ecap;
        private int _ecnt;

        private Float2D[] _fbuk;
        private int _fcap;
        private int _head;
        private int _tail;

        public int Count => _ecnt;
        public DataRange<Float2D> this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ebuk[i].GetDataRange(_fbuk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Clipper2Paths()
        {
            _ebuk = new Entry[MIN_ENTRY];
            _ecap = MIN_ENTRY;
            _fbuk = new Float2D[MIN_FLOAT2D];
            _fcap = MIN_FLOAT2D;
        }

        public Clipper2Paths(in FloatRect initRect) : this(initRect.ToFloat2Dx4().AsSpan()) { }
        public Clipper2Paths(Span<Float2D> initPath) => Initialize(ref MemoryMarshal.GetReference(initPath), initPath.Length);
        public Clipper2Paths(ref Float2D initPath0, int count) => Initialize(ref initPath0, count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(ref Float2D initPath0, int count)
        {
            if (count >= 2)
            {
                var ebuk = new Entry[MIN_ENTRY] { new(0, count), default, default, default };
                var fbuk = new Float2D[MathHelper.RoundUpToPow2(MIN_FLOAT2D, count)];
                AC.Copy(ref initPath0, fbuk, 0, count);

                _ebuk = ebuk;
                _ecap = ebuk.Length;
                _ecnt = 1;

                _fbuk = fbuk;
                _fcap = fbuk.Length;
                _head = count;
                _tail = count;
            }
            else
            {
                _ebuk = new Entry[MIN_ENTRY];
                _ecap = MIN_ENTRY;
                _fbuk = new Float2D[MIN_FLOAT2D];
                _fcap = MIN_FLOAT2D;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeEntryBucket()
        {
            var ecap = _ecap;
            var ebuk = new Entry[ecap * 2];
            ref var s = ref RefB(_ebuk);
            ref var d = ref RefB(ebuk);
            do
            {
                AsT<ulong>(ref d) = AsT<ulong>(ref s);
                AsT<ulong>(ref d, 8) = AsT<ulong>(ref s, 8);
                AsT<ulong>(ref d, 16) = AsT<ulong>(ref s, 16);
                AsT<ulong>(ref d, 24) = AsT<ulong>(ref s, 24);
                s = ref AddB(ref s, 32);
                d = ref AddB(ref d, 32);
            }
            while ((ecap -= 4) != 0);
            _ebuk = ebuk;
            _ecap = ebuk.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeFloatBucket()
        {
            var fcap = _fcap;
            var fbuk = new Float2D[fcap * 2];
            ref var s = ref RefB(_fbuk);
            ref var d = ref RefB(fbuk);
            do
            {
                AsT<ulong>(ref d) = AsT<ulong>(ref s);
                AsT<ulong>(ref d, 8) = AsT<ulong>(ref s, 8);
                AsT<ulong>(ref d, 16) = AsT<ulong>(ref s, 16);
                AsT<ulong>(ref d, 24) = AsT<ulong>(ref s, 24);
                s = ref AddB(ref s, 32);
                d = ref AddB(ref d, 32);
            }
            while ((fcap -= 4) > 0);
            _fbuk = fbuk;
            _fcap = fbuk.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeFloatBucket(int head, int count)
        {
            var fcap = _fcap;
            var fbuk = new Float2D[(int)BitOperations.RoundUpToPowerOf2((uint)(head + count))];
            ref var s = ref RefB(_fbuk);
            ref var d = ref RefB(fbuk);
            do
            {
                AsT<ulong>(ref d) = AsT<ulong>(ref s);
                AsT<ulong>(ref d, 8) = AsT<ulong>(ref s, 8);
                AsT<ulong>(ref d, 16) = AsT<ulong>(ref s, 16);
                AsT<ulong>(ref d, 24) = AsT<ulong>(ref s, 24);
                s = ref AddB(ref s, 32);
                d = ref AddB(ref d, 32);
            }
            while ((head -= 4) > 0);
            _fbuk = fbuk;
            _fcap = fbuk.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool AddPathClose(OutPt pts, double mf)
        {
            if (pts != null)
            {
                var head = _head;
                var hidx = head;

                ref var buk0 = ref RefT(_fbuk);
                var fcap = _fcap;
                var pts0 = pts.Next;
                var curr = pts0;

                do
                {
                    if (hidx == fcap)
                    {
                        ResizeFloatBucket();
                        buk0 = ref RefT(_fbuk);
                        fcap = _fcap;
                    }
                    Unsafe.Add(ref buk0, hidx++) = new((float)(curr.Pt.X * mf), (float)(curr.Pt.Y * mf));
                    curr = curr.Next;
                }
                while (curr != pts0);

                var count = hidx - head;
                if (count > 3 || (count == 3 && Utils.IsSmallTriangle(pts) == false))
                {
                    var ecnt = _ecnt;
                    if (ecnt == _ecap) ResizeEntryBucket();
                    _ebuk[ecnt] = new(head, count);
                    _ecnt = ecnt + 1;
                    _head = hidx;
                    _tail = hidx;
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool AddPathOpen(OutPt pts, double mf)
        {
            if (pts != null)
            {
                var head = _head;
                var hidx = head;
                if (hidx == _fcap) ResizeFloatBucket();

                ref var buk0 = ref RefT(_fbuk);
                var fcap = _fcap;
                var pts0 = pts.Next;
                var curr = pts0;

                Unsafe.Add(ref buk0, hidx++) = new((float)(curr.Pt.X * mf), (float)(curr.Pt.Y * mf));
                var prev = curr; curr = curr.Next;

                do
                {
                    if (hidx == fcap)
                    {
                        ResizeFloatBucket();
                        buk0 = ref RefT(_fbuk);
                        fcap = _fcap;
                    }
                    if (prev.Pt.X != curr.Pt.X || prev.Pt.Y != curr.Pt.Y)
                        Unsafe.Add(ref buk0, hidx++) = new((float)(curr.Pt.X * mf), (float)(curr.Pt.Y * mf));
                    prev = curr; curr = curr.Next;
                }
                while (curr != pts0);

                var count = hidx - head;
                if (count >= 2)
                {
                    var ecnt = _ecnt;
                    if (ecnt == _ecap) ResizeEntryBucket();
                    _ebuk[ecnt] = new(head, count);
                    _ecnt = ecnt + 1;
                    _head = hidx;
                    _tail = hidx;
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPoint(float x, float y)
        {
            var head = _head;
            if (head == _fcap) ResizeFloatBucket();
            _head = head + 1;
            _fbuk[head] = new(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPoint(Float2D point)
        {
            var head = _head;
            if (head == _fcap) ResizeFloatBucket();
            _head = head + 1;
            _fbuk[head] = point;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddPoints(Float2D[] points)
        {
            if (points != null)
            {
                var count = points.Length;
                if (count != 0)
                {
                    var head = _head;
                    if (head + count > _fcap) ResizeFloatBucket(head, count);
                    AC.Copy(points, 0, _fbuk, head, count);
                    _head = head + count;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool CompletePath()
        {
            var tail = _tail;
            var head = _head;
            var plen = head - tail;
            if (plen >= 2)
            {
                var ecnt = _ecnt;
                if (ecnt == _ecap) ResizeEntryBucket();
                _ebuk[ecnt] = new(tail, plen);
                _ecnt = ecnt + 1;
                _tail = head;
                return true;
            }
            _head = tail;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddPaths(Float2D[][] paths)
        {
            if (paths != null && paths.Length != 0)
            {
                var ecnt = _ecnt;
                var head = _head;

                var i = 0;
                do
                {
                    var path = paths[i];
                    if (path != null)
                    {
                        var count = path.Length;
                        if (count >= 2)
                        {
                            if (ecnt == _ecap) ResizeEntryBucket();
                            _ebuk[ecnt] = new(head, count);
                            ecnt++;

                            if (head + count > _fcap) ResizeFloatBucket(head, count);
                            AC.Copy(path, 0, _fbuk, head, count);
                            head += count;
                        }
                    }
                }
                while (++i < paths.Length);

                _ecnt = ecnt;
                _tail = head;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CancelPath() => _head = _tail;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() { _ecnt = 0; _head = 0; _tail = 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Clipper2Paths ClearGet()
        {
            _ecnt = 0;
            _head = 0;
            _tail = 0;
            return this;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Float2D[][] ToArrayClear()
        {
            var c = _ecnt;
            if (c != 0)
            {
                var ebuk = _ebuk;
                ref var fbuk0 = ref RefT(_fbuk);

                var r = new Float2D[c][];
                do
                {
                    var e = ebuk[--c];
                    var d = AC.UninitializedArray<Float2D>(e.Count);
                    AC.Copy(ref AddT(ref fbuk0, e.Index), ref RefT(d), e.Count);
                    r[c] = d;
                }
                while (c != 0);
                _ecnt = 0;
                _head = 0;
                _tail = 0;
                return r;
            }
            _head = 0;
            _tail = 0;
            return null;
        }
    }
}