using JHLib.Util.Helper;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    internal class IntersectNode(in Long2D pt, Active edge1, Active edge2)
    {
        public readonly Long2D Pt = pt;
        public readonly Active Edge1 = edge1;
        public readonly Active Edge2 = edge2;
        public bool EdgesAdjacentInAEL => (Edge1.NextAEL == Edge2) || (Edge1.PrevAEL == Edge2);
    }

    internal class IntersectNodeList
    {
        private readonly static Sorter AlreadySorter = new();
        private class Sorter : IComparer<IntersectNode>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(IntersectNode a, IntersectNode b)
            {
                if (a.Pt.Y == b.Pt.Y)
                    if (a.Pt.X == b.Pt.X) return 0;
                    else return a.Pt.X < b.Pt.X ? -1 : 1;
                else return a.Pt.Y > b.Pt.Y ? -1 : 1;
            }
        }

        private IntersectNode[] _buk;
        private int _cap;
        private int _cnt;
        public IntersectNode[] Bucket => _buk;
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
            var cnt = _cnt;
            if (cnt != 0)
            {
                RefCommand.RefClear(_buk, cnt);
                _cnt = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntersectNodeList Sort()
        {
            if (_cnt >= 2)            
                MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_buk), _cnt).Sort(AlreadySorter);            
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in Long2D pt, Active edge1, Active edge2)
        {
            var i = _cnt;
            if (i == _cap) Resize();
            _cnt = i + 1;

            _buk[i] = new(pt, edge1, edge2);
        }
    }
}