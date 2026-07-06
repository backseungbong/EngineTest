using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    public class StreamCreatorKeyArray : IStreamCreator
    {
        private readonly PoolKeyPos _kmap;
        private readonly PoolStream _sdat;
        private readonly int _code;

        public int StreamCode => _code;
        public int StreamSize => StreamHeader.ToStreamSize(_kmap.ByteLength + _sdat.Position);
        public int Count => _kmap.Count;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EntryKey(int edx) => _kmap.GetEntry(edx).Key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref DataHeader EntryHeader(int edx) => ref _sdat.Ref<DataHeader>(_kmap.GetEntry(edx).Pos);

        public DataHeaderReader this[int key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(ref GetHeader(key));
        }

        public void Dispose() { _kmap.Dispose(); _sdat.Dispose(); }
        public StreamCreatorKeyArray(int streamCode = 0, int capacity = 8)
        {
            _kmap = new PoolKeyPos(capacity);
            _sdat = new PoolStream(capacity);
            _code = streamCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderWriter AddHeader(int key, int code = 0, int count = 0) => _sdat.AddHeader(_kmap, key, code, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataHeaderReader<T> AsReader<T>(int key) where T : unmanaged => new(ref GetHeader(key));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref DataHeader GetHeader(int key)
        {
            if (_kmap.Get(key, out var pos))
                return ref _sdat.Ref<DataHeader>(pos);
            return ref DataHeader.Empty;
        }

        public void Clear() { _kmap.Clear(); _sdat.Clear(); }
        public void ClearEnsureCap() { _kmap.ClearEnsureCap(); _sdat.Clear(); }
        public void ClearEnsureCap(int capacity) { _kmap.ClearEnsureCap(capacity); _sdat.Clear(); }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CopyTo(PoolStream dest)
        {
            var map = _kmap;
            var dat = _sdat;
            var hdr = new StreamHeader(map.ByteLength + dat.Position, _code, map.Count, map.Capacity);

            var buf = dest.OccupyWriter(hdr.StreamSize);
            buf.Add(hdr);
            buf.Add(map);
            buf.Add(dat);
        }
    }
}