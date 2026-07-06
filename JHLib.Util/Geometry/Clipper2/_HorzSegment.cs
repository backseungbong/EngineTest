using JHLib.Util.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    internal class HorzSegment(OutPt leftOp, OutPt rightOp, bool leftToRight)
    {
        public OutPt LeftOp = leftOp;
        public OutPt RightOp = rightOp;
        public bool LeftToRight = leftToRight;
    }

    internal class HorzSegmentList
    {
        private readonly static Sorter AlreadySorter = new();
        private class Sorter : IComparer<HorzSegment>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(HorzSegment hs1, HorzSegment hs2)
            {
                if (hs1 != null && hs2 != null)
                    if (hs1.RightOp != null)
                        if (hs2.RightOp != null)
                            return hs1.LeftOp.Pt.X.CompareTo(hs2.LeftOp.Pt.X);
                        else return -1;
                    else return hs2.RightOp != null ? 1 : 0;
                else return 0;
            }
        }

        private HorzSegment[] _buk;
        private int _cap;
        private int _cnt;
        public HorzSegment[] Bucket => _buk;
        public int Count => _cnt;

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
        public void Sort()
        {
            if (_cnt >= 2)
                MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_buk), _cnt).Sort(AlreadySorter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(OutPt op)
        {
            var i = _cnt;
            if (i == _cap) Resize();
            _cnt = i + 1;

            _buk[i] = new(op, null, true);
        }
    }
}