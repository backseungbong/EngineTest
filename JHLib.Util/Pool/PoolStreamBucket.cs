using JHLib.Util.ArrayControl;
using JHLib.Util.DataStream;
using JHLib.Util.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Pool
{
    using static JHLib.Util.Helper.UnsafeEx;
    public class PoolStreamBucket
    {
        public readonly struct PoolIndexPos(int i, int p)
        {
            public readonly int Idx = i;
            public readonly int Pos = p;
        }

        private const int MIN_CAPAC = 8;

        private readonly PoolStream[] _streams;
        private PoolIndexPos[] _buk;
        private int _cap;
        private int _cnt;
        public PoolIndexPos[] Bucket => _buk;
        public int Count => _cnt;

        public PoolStreamBucket(params PoolStream[] streams)
        {
            _streams = streams;
            _buk = new PoolIndexPos[8];
            _cap = 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var cap = _cap;
            var dst = AC.CopyNew(_buk, cap * 2, cap);

            _buk = dst;
            _cap = dst.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int idx, int pos)
        {
            var cnt = _cnt;
            if (cnt == _cap) Resize();
            _cnt = cnt + 1;

            _buk[cnt] = new(idx, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUnsafe(int idx, int pos) => _buk[_cnt++] = new(idx, pos);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader<T> AsReader<T>(int i) where T : unmanaged
        {
            ref var range = ref Arr0(_buk, i);
            ref var stream0 = ref Arr0(_streams, range.Idx).Stream0;
            return new(ref AddT(stream0, range.Pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _cnt = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PoolStreamBucket ClearSetCount(int setCount)
        {
            if (setCount > _cap)
            {
                var cap = MathHelper.RoundUpToPow2(MIN_CAPAC, setCount);
                _buk = new PoolIndexPos[cap];
                _cap = cap;
            }
            _cnt = setCount;
            return this;
        }
    }
}