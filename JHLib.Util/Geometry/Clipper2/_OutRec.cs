using JHLib.Util.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    [StructLayout(LayoutKind.Sequential)]
    internal class OutRec(int idx)
    {
        public OutRec Owner;
        public Active FrontEdge;
        public Active BackEdge;
        public OutPt Pts;
        public readonly int Idx = idx;
        public bool IsOpen;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SwapFrontBackSides()
        {
            (BackEdge, FrontEdge) = (FrontEdge, BackEdge);
            Pts = Pts.Next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixOutRecPts()
        {
            var op = Pts;
            do op.Outrec = this;
            while ((op = op.Next) != Pts);
        }
    }

    internal class OutRecList
    {
        private OutRec[] _buk;
        private int _cap;
        private int _cnt;
        public int Count => _cnt;
        public OutRec this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buk[index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var cap = _cap;
            var buk = RefCommand.RefCopyNew(_buk, Math.Max(4, cap * 2), cap);

            _buk = buk;
            _cap = buk.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_cnt != 0)
            {
                RefCommand.RefClear(_buk, _cnt);
                _cnt = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OutRec AddGet()
        {
            var i = _cnt;
            if (i == _cap) Resize();
            _cnt = i + 1;

            return _buk[i] = new(i);
        }
    }
}