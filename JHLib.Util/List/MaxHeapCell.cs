using JHLib.Util.ArrayControl;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.List
{
    public unsafe class MaxHeapCell
    {
        private Cell[] _buk = new Cell[8];
        private int _cap = 8;
        private int _cnt = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var cap = _cap;
            var buk = new Cell[cap * 2];

            AC.Copy(_buk, buk, cap);

            _buk = buk;
            _cap = cap * 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Add(Cell cell)
        {
            var i = _cnt; _cnt = i + 1;
            if (i == _cap) Resize();

            fixed (Cell* b = &_buk[0])
            {
                if (i != 0)
                {
                    var j = i - 1 >> 1;
                    var m = cell.Max;
                    if (m > b[j].Max)
                    {
                        do { b[i] = b[j]; i = j; }
                        while (j != 0 && m > b[j = i - 1 >> 1].Max);
                    }
                }
                b[i] = cell;
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cell Pop()
        {
            var c = _cnt;
            if (c != 0) return PopInternal(c - 1);
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out Cell result)
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
        private Cell PopInternal(int l)
        {
            _cnt = l;
            fixed (Cell* b = &_buk[0])
            {
                int i, j;
                var r = *b;
                if (l != 0)
                {
                    var v = b[l];
                    var m = v.Max; i = 0;
                    if (l > 1)
                    {
                        j = 1;
                    RE: if (b[j].Max < b[j + 1].Max) j++;
                        if (b[j].Max > m)
                        {
                            b[i] = b[j]; i = j; j = i * 2 + 1;
                            if (j < l) goto RE;
                            if (j == l && b[j].Max > m) { b[i] = b[j]; i = j; }
                        }
                    }
                    b[i] = v;
                }
                return r;
            }
        }
    }
}