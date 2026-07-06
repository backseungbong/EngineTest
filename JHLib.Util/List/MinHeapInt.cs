using JHLib.Util.ArrayControl;
using System.Runtime.CompilerServices;

namespace JHLib.Util.List
{
    public unsafe class MinHeapInt
    {
        private int[] _buk;
        private int _cap;
        private int _cnt;

        public int Count => _cnt;
        public int Min => _buk[0];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MinHeapInt() { _buk = new int[2]; _cap = 2; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MinHeapInt(int cap)
        {
            if (cap < 2) cap = 2;
            _buk = new int[cap];
            _cap = cap;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var c = _cap;
            var buk = new int[c * 2];

            AC.Copy(_buk, buk, c);

            _buk = buk;
            _cap = c * 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Add(int v)
        {
            var i = _cnt; _cnt = i + 1;
            if (i == _cap) Resize();

            fixed (int* b = &_buk[0])
            {
                if (i != 0)
                {
                    var j = i - 1 >> 1;
                    if (v < b[j])
                    {
                        do { b[i] = b[j]; i = j; }
                        while (j != 0 && v < b[j = i - 1 >> 1]);
                    }
                }
                b[i] = v;
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Pop()
        {
            var c = _cnt;
            if (c != 0) return PopInternal(c - 1);
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out int result)
        {
            var c = _cnt;
            if (c != 0)
            {
                result = PopInternal(c - 1);
                return true;
            }
            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int PopInternal(int l)
        {
            _cnt = l;
            fixed (int* b = &_buk[0])
            {
                int i, j;
                var r = *b;
                if (l != 0)
                {
                    var v = b[l]; i = 0;
                    if (l > 1)
                    {
                        j = 1;
                    RE: if (b[j] > b[j + 1]) j++;
                        if (b[j] < v)
                        {
                            b[i] = b[j]; i = j; j = i * 2 + 1;
                            if (j < l) goto RE;
                            if (j == l && b[j] < v) { b[i] = b[j]; i = j; }
                        }
                    }
                    b[i] = v;
                }
                return r;
            }
        }

        public void Clear() => _cnt = 0;
    }
}