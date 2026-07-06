using JHLib.Util.ArrayControl;
using JHLib.Util.Hash;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Pool
{
    using static JHLib.Util.Helper.RefCommand;

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
    internal struct EntryKeyPos
    {
        public const int SIZE = 12;
        public int Key;
        public int Idx;
        public int Pos;
    }
    internal class PoolKeyPos : IDisposable
    {
        private const int NULL = -1;
        private const int BUK_SIZE = sizeof(int);
        private const int ENT_SIZE = EntryKeyPos.SIZE;
        private const int ITEM_SIZE = BUK_SIZE + ENT_SIZE;

        private readonly PoolSpace _pool;
        private int _cap;
        private int _cnt;

        public int Capacity => _cap;
        public int Count => _cnt;
        public int ByteLength => _cap * BUK_SIZE + _cnt * ENT_SIZE;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntryKeyPos GetEntry(int i) => ref AsT<EntryKeyPos>(ref _pool.Space0, i * ENT_SIZE);

        public void Dispose() => _pool.Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PoolKeyPos() { _pool = new(); Initialize(HashUtil.MinPrime); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PoolKeyPos(int cap) { _pool = new(); Initialize(HashUtil.GetPrime(cap)); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int cap)
        {
            ref var ref0 = ref _pool.Resize0(cap * ITEM_SIZE);
            ref var b0 = ref AsT<int>(ref ref0, cap * ENT_SIZE);

            AC.ZeroFill(ref b0, cap);

            _cap = cap;
            _cnt = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var cnt = _cap;
            var cap = cnt * 2;

            ref var ref0 = ref _pool.Resize0(cap * ITEM_SIZE, cnt * ENT_SIZE);
            ref var b0 = ref AsT<int>(ref ref0, cap * ENT_SIZE);
            ref var e0 = ref AsT<EntryKeyPos>(ref ref0);

            AC.ZeroFill(ref b0, cap);

            var i = 0;
            do
            {
                ref var e = ref AddT(ref e0, i);
                ref var b = ref AddT(ref b0, (int)((uint)e.Key % (uint)cap));
                e.Idx = b - 1; b = i + 1;
            }
            while (++i < cnt);

            _cap = cap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int key, int pos)
        {
            var i = _cnt; _cnt = i + 1;
            if (i == _cap) Resize();

            ref var ref0 = ref _pool.Space0;
            ref var e = ref AsT<EntryKeyPos>(ref ref0, i * ENT_SIZE);
            ref var b = ref AsT<int>(ref ref0, _cap * ENT_SIZE + (int)((uint)key % (uint)_cap) * BUK_SIZE);
            e.Key = key;
            e.Idx = b - 1; b = i + 1;
            e.Pos = pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int key, out int pos)
        {
            ref var ref0 = ref _pool.Space0;
            var i = AsT<int>(ref ref0, _cap * ENT_SIZE + (int)((uint)key % (uint)_cap) * BUK_SIZE);
            if (i != 0)
            {
                i--;
            RE: ref var e = ref AsT<EntryKeyPos>(ref ref0, i * ENT_SIZE);
                if (e.Key == key) { pos = e.Pos; return true; }
                if ((i = e.Idx) != NULL) goto RE;
            }
            pos = NULL;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_cnt != 0)
                Initialize(HashUtil.MinPrime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearEnsureCap()
        {
            if (_cnt != 0)
                Initialize(_cap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearEnsureCap(int capacity)
        {
            var cap = HashUtil.GetPrime(capacity);
            if (_cnt != 0 || _cap != cap)
                Initialize(cap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(ref byte dest0)
        {
            var preSize = _cap * BUK_SIZE;
            var entSize = _cnt * ENT_SIZE;

            ref var ent0 = ref _pool.Space0;
            ref var pre0 = ref AddB(ref ent0, _cap * ENT_SIZE);

            AC.Copy(ref pre0, ref dest0, preSize);
            AC.Copy(ref ent0, ref AddB(ref dest0, preSize), entSize);
        }
    }
}