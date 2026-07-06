using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    public class StreamCreatorKeyValue<T> : IStreamCreator where T : unmanaged
    {
        private static T Empty = default;

        private readonly PoolKeyIdx _kmap;
        private readonly PoolStream _sdat;
        private readonly int _code;

        public int StreamCode => _code;
        public int StreamSize => StreamHeader.ToStreamSize(_kmap.ByteLength + _sdat.Position);
        public int Count => _kmap.Count;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EntryKey(int edx) => _kmap.GetEntry(edx).Key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T EntryValue(int edx) => ref _sdat.Ref<T>(edx * Unsafe.SizeOf<T>());

        public ref T this[int key]
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (_kmap.Get(key, out var idx))
                    return ref _sdat.Ref<T>(idx * Unsafe.SizeOf<T>());
                return ref Empty;
            }
        }

        public void Dispose() { _kmap.Dispose(); _sdat.Dispose(); }
        public StreamCreatorKeyValue(int streamCode = 0, int capacity = 8)
        {
            _kmap = new PoolKeyIdx(capacity);
            _sdat = new PoolStream(capacity * Unsafe.SizeOf<T>());
            _code = streamCode;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ref T SetRef(int key)
        {
            _kmap.Set(key);
            return ref _sdat.AddRef<T>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Get(int key, out T val)
        {
            if (_kmap.Get(key, out var idx))
            {
                val = _sdat.Ref<T>(idx * Unsafe.SizeOf<T>());
                return true;
            }
            val = default;
            return false;
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