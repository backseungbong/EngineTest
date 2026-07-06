using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    public unsafe class StreamCreatorArray1D<T> : IStreamCreator where T : unmanaged
    {
        private readonly PoolStream _sdat;
        private readonly int _code;

        public int StreamCode => _code;
        public int StreamSize => StreamHeader.ToStreamSize(_sdat.Position);
        public int Count => _sdat.Position / sizeof(T);

        public ref T this[int i] => ref _sdat.Ref<T>(i * sizeof(T));

        public void Dispose() => _sdat.Dispose();
        public StreamCreatorArray1D(int streamCode = 0, int capacity = 8)
        {
            _sdat = new PoolStream(capacity * sizeof(T));
            _code = streamCode;
        }

        public ref T AddRef() => ref _sdat.AddRef<T>();
        public void Add(T item) => _sdat.AddRef<T>() = item;
        public void Clear() => _sdat.Clear();


        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CopyTo(PoolStream dest)
        {
            var dat = _sdat;
            var hdr = new StreamHeader(dat.Position, _code, dat.Position / sizeof(T));
            var buf = dest.OccupyWriter(hdr.StreamSize);
            buf.Add(hdr);
            buf.Add(dat);
        }
    }
}