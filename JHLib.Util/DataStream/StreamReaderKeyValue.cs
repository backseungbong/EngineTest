using JHLib.Util.Hash;
using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    using static JHLib.Util.Helper.RefCommand;

    public class StreamReaderKeyValue<T> where T : unmanaged
    {
        private const int BUK_SIZE = sizeof(int);
        private const int ENT_SIZE = EntryKeyIdx.SIZE;
        private const int NULL = -1;

        private readonly byte[] _stream;
        private readonly int _buk;
        private readonly int _ent;
        private readonly int _dat;
        private readonly int _cap;
        private readonly int _cnt;
        public int Count => _cnt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EntryKey(int i) => GetEnt(ref RefB(_stream), i).Key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T EntryValue(int i) => ref GetDat(ref RefB(_stream), i);
        public ref T this[int key] => ref GetRef(key);

        public StreamReaderKeyValue(PoolStream own, int streamPosition)
        {
            ref var hdr = ref own.Ref<StreamHeader>(streamPosition);
            var cnt = hdr.DataCount;
            var cap = hdr.DataCapacity;

            _stream = own.Stream;
            _buk = streamPosition + StreamHeader.SIZE;
            _ent = cap * BUK_SIZE + _buk;
            _dat = cnt * ENT_SIZE + _ent;
            _cap = cap;
            _cnt = cnt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetBuk(ref byte s0, int key) =>
            AsTU<int>(ref s0, _buk + (int)((uint)key % (uint)_cap) * BUK_SIZE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref EntryKeyIdx GetEnt(ref byte s0, int idx) =>
            ref AsTU<EntryKeyIdx>(ref s0, _ent + idx * ENT_SIZE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetDat(ref byte s0, int idx) =>
            ref AsTU<T>(ref s0, _dat + idx * Unsafe.SizeOf<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGet(ref byte s0, int key, out int idx)
        {
            var i = GetBuk(ref s0, key);
            if (i != 0)
            {
                i--;
            RE: ref var e = ref GetEnt(ref s0, i);
                if (e.Key == key) { idx = i; return true; }
                if ((i = e.Idx) != NULL) { goto RE; }
            }
            idx = 0; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Get(int key, out T val)
        {
            ref var s0 = ref RefB(_stream);
            if (TryGet(ref s0, key, out var i)) { val = GetDat(ref s0, i); return true; }
            val = default; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ref T GetRef(int key)
        {
            ref var s0 = ref RefB(_stream);
            if (TryGet(ref s0, key, out var idx)) { return ref GetDat(ref s0, idx); }
            return ref HashUtil.Empty<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRefInline(int key) => ref GetRefInline(key, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRefInline(int key, out int idx)
        {
            ref var s0 = ref RefB(_stream);
            if (TryGet(ref s0, key, out idx)) { return ref GetDat(ref s0, idx); }
            return ref HashUtil.Empty<T>();
        }
    }
}