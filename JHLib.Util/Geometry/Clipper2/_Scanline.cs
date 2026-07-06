using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    internal class Scanline
    {
        private long[] _buk = new long[4];
        private int _cap = 4;
        private int _cnt = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref ulong AsUlong(ref byte ptr) =>
            ref Unsafe.As<byte, ulong>(ref ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref ulong AsUlong(ref byte ptr, nint byteOffset) =>
            ref Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref ptr, byteOffset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref long Add(ref long ptr, int index) =>
            ref Unsafe.Add(ref ptr, index);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var cap = _cap;
            var buk = new long[cap * 2];

            ref var s = ref Unsafe.As<long, byte>(ref MemoryMarshal.GetArrayDataReference(_buk));
            ref var d = ref Unsafe.As<long, byte>(ref MemoryMarshal.GetArrayDataReference(buk));
            do
            {
                AsUlong(ref d) = AsUlong(ref s);
                AsUlong(ref d, 8) = AsUlong(ref s, 8);
                AsUlong(ref d, 16) = AsUlong(ref s, 16);
                AsUlong(ref d, 24) = AsUlong(ref s, 24);
                s = ref Unsafe.AddByteOffset(ref s, 32);
                d = ref Unsafe.AddByteOffset(ref d, 32);
            }
            while ((cap -= 4) != 0);

            _buk = buk;
            _cap = buk.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Set(LocalMinimaList sortedlms)
        {
            var c = 0;
            var n = sortedlms.Count;
            if (n != 0)
            {
                if (n > _cap)
                {
                    var cap = (int)BitOperations.RoundUpToPowerOf2((uint)n);
                    _buk = new long[cap];
                    _cap = cap;
                }

                ref var b = ref MemoryMarshal.GetArrayDataReference(_buk);
                b = sortedlms[0].Vertex.Pt.Y;
                c = 1;

                if (n != 1)
                {
                    var i = 1;
                    do
                    {
                        var y = sortedlms[i].Vertex.Pt.Y;
                        if (y != b)
                        {
                            b = ref Unsafe.Add(ref b, 1);
                            b = y;
                            c++;
                        }
                    }
                    while (++i < n);
                }
            }
            _cnt = c;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Add(long y)
        {
            var c = _cnt;
            if (c != 0)
            {
                if (y != _buk[0])
                {
                    if (c == _cap)
                        Resize();

                    ref var b0 = ref MemoryMarshal.GetArrayDataReference(_buk);
                    var i = c;
                    var j = i - 1 >> 1;
                    if (y > Add(ref b0, j))
                    {
                        do { Add(ref b0, i) = Add(ref b0, j); i = j; }
                        while (j != 0 && y > Add(ref b0, j = i - 1 >> 1));
                    }
                    Add(ref b0, i) = y;
                    _cnt = c + 1;
                }
            }
            else
            {
                _buk[0] = y;
                _cnt = 1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Pop(out long result)
        {
            var c = _cnt;
            if (c != 0)
            {
                int i, j;
                ref var b0 = ref MemoryMarshal.GetArrayDataReference(_buk);
                var r = b0;
                do
                {
                    var l = c - 1;
                    if (l != 0)
                    {
                        var v = Add(ref b0, l); i = 0;
                        if (l > 1)
                        {
                            j = 1;
                        RE: if (Add(ref b0, j) < Add(ref b0, j + 1)) j++;
                            if (Add(ref b0, j) > v)
                            {
                                Add(ref b0, i) = Add(ref b0, j);
                                i = j;
                                j = i * 2 + 1;
                                if (j < l) goto RE;
                                if (j == l && Add(ref b0, j) > v)
                                {
                                    Add(ref b0, i) = Add(ref b0, j);
                                    i = j;
                                }
                            }
                        }
                        Add(ref b0, i) = v;
                    }
                }
                while (--c != 0 && r == b0);

                _cnt = c;
                result = r;
                return true;
            }
            result = default;
            return false;
        }

        public void Clear() => _cnt = 0;
    }
}