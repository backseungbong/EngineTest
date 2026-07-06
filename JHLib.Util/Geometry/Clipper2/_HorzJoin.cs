using JHLib.Util.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Geometry.Clipper2
{
    internal class HorzJoin(OutPt op1, OutPt op2)
    {
        public readonly OutPt Op1 = op1;
        public readonly OutPt Op2 = op2;
    }

    internal class HorzJoinList
    {
        private HorzJoin[] _buk;
        private int _cap;
        private int _cnt;
        public HorzJoin[] Bucket => _buk;
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
        public void Add(OutPt op1, OutPt op2)
        {
            var i = _cnt;
            if (i == _cap) Resize();
            _cnt = i + 1;

            _buk[i] = new(op1, op2);
        }
    }
}
