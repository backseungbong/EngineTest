using JHLib.Util.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    internal class LocalMinima
    {
        public Vertex Vertex;
        public PathType Pathtype;
        public bool IsOpen;
    }

    internal class LocalMinimaList
    {
        private readonly static Sorter AlreadySorter = new();
        private class Sorter : IComparer<LocalMinima>
        {
            public int Compare(LocalMinima a, LocalMinima b) =>
                b.Vertex.Pt.Y.CompareTo(a.Vertex.Pt.Y);
        }

        private LocalMinima[] _buk;
        private int _cap;
        private int _cnt;
        public bool NeedSorting { get; set; }
        public int Count { get => _cnt; set => _cnt = value; }
        public LocalMinima this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buk[index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var cap = _cap;
            var buk = RefCommand.RefCopyNew(_buk, Math.Max(4, cap * 2), cap);

            do buk[cap] = new();
            while (++cap < buk.Length);

            _buk = buk;
            _cap = buk.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _cnt = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LocalMinimaList Sort()
        {
            if (NeedSorting && _cnt >= 2)
            {
                MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_buk), _cnt).Sort(AlreadySorter);
                NeedSorting = false;
            }
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LocalMinima AddGet()
        {
            var i = _cnt;
            if (i == _cap) Resize();
            _cnt = i + 1;

            return _buk[i];
        }
    }
}