using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    public unsafe class StreamCreatorItem<T> : IStreamCreator where T : unmanaged
    {
        private readonly int _code;
        public T Data;

        public int StreamCode => _code;
        public int StreamSize => StreamHeader.ToStreamSize(sizeof(T));
        public int Count => 1;

        public void Dispose() { }
        public StreamCreatorItem(int streamCode = 0) => _code = streamCode;


        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CopyTo(PoolStream dest)
        {
            var hdr = new StreamHeader(sizeof(T), _code, 1);
            var buf = dest.OccupyWriter(hdr.StreamSize);
            buf.Add(hdr);
            buf.Add(Data);
        }
    }
}