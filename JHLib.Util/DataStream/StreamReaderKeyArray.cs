using JHLib.Util.Hash;
using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    using static JHLib.Util.Helper.RefCommand;

    public class StreamReaderKeyArray
    {
        private const int BUK_SIZE = sizeof(int);
        private const int ENT_SIZE = EntryKeyPos.SIZE;
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
        public ref DataHeader EntryHeader(int i)
        {
            ref var s0 = ref RefB(_stream);
            return ref GetDataHeader(ref s0, GetEnt(ref s0, i).Pos);
        }
        public DataHeaderReader this[int key] => new(ref GetHeader(key));
        public DataHeaderReader<T> AsReader<T>(int key) where T : unmanaged => new(ref GetHeader(key));

        public StreamReaderKeyArray(PoolStream own, int streamPosition)
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
        private ref EntryKeyPos GetEnt(ref byte s0, int idx) =>
            ref AsTU<EntryKeyPos>(ref s0, _ent + idx * ENT_SIZE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref DataHeader GetDataHeader(ref byte s0, int pos) =>
            ref AsTU<DataHeader>(ref s0, _dat + pos);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGet(ref byte s0, int key, out int pos)
        {
            var i = GetBuk(ref s0, key);
            if (i != 0)
            {
                i--;
            RE: ref var e = ref GetEnt(ref s0, i);
                if (e.Key == key) { pos = e.Pos; return true; }
                if ((i = e.Idx) != NULL) { goto RE; }
            }
            pos = 0; return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref DataHeader GetHeader(int key) => ref GetHeaderInline(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref DataHeader GetHeaderInline(int key)
        {
            ref var s0 = ref RefB(_stream);
            if (TryGet(ref s0, key, out var p)) { return ref GetDataHeader(ref s0, p); }
            return ref HashUtil.Empty<DataHeader>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetHeaderPosition(int key)
        {
            ref var s0 = ref RefB(_stream);
            if (TryGet(ref s0, key, out var p)) { return _dat + p; }
            return 0;
        }
    }
}