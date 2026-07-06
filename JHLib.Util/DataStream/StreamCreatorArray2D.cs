using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    public unsafe class StreamCreatorArray2D : IStreamCreator
    {
        private const int IDX_SIZE = sizeof(int);

        private readonly PoolStream _spos;
        private readonly PoolStream _sdat;
        private readonly int _code;

        public int StreamCode => _code;
        public int StreamSize => StreamHeader.ToStreamSize(_spos.Position + _sdat.Position);
        public int Count => _spos.Position / IDX_SIZE;

        public void Dispose() { _spos.Dispose(); _sdat.Dispose(); }
        public StreamCreatorArray2D(int streamCode = 0, int capacity = 8, int dataSize = 1024)
        {
            _spos = new PoolStream(capacity * IDX_SIZE);
            _sdat = new PoolStream(capacity * DataHeader.SIZE + dataSize);
            _code = streamCode;
        }

        public DataHeaderWriter AddHeader(int dataCode = 0, int itemCount = 0) =>
            _sdat.AddHeader(_spos, dataCode, itemCount);

        public DataHeaderReader<T> AsReader<T>(int i) where T : unmanaged =>
            new(ref _sdat.Stream0, _spos.Ref<int>(i * 4));

        public void Clear() { _spos.Clear(); _sdat.Clear(); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CopyTo(PoolStream dest)
        {
            var pos = _spos;
            var dat = _sdat;
            var hdr = new StreamHeader(pos.Position + dat.Position, _code, pos.Position / IDX_SIZE);
            var buf = dest.OccupyWriter(hdr.StreamSize);
            buf.Add(hdr);
            buf.Add(pos);
            buf.Add(dat);
        }
    }
}