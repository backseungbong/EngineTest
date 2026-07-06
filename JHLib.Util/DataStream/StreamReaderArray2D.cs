using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    using static JHLib.Util.Helper.RefCommand;
    public class StreamReaderArray2D
    {
        private const int IDX_SIZE = sizeof(int);

        private readonly byte[] _stream;
        private readonly int _pos;
        private readonly int _dat;
        private readonly int _cnt;
        public int Count => _cnt;
        public DataHeaderReader this[int i] => GetReader(i);
        public StreamReaderArray2D(PoolStream own, int streamPosition)
        {
            ref var hdr = ref own.Ref<StreamHeader>(streamPosition);
            var cnt = hdr.DataCount;

            _stream = own.Stream;
            _pos = streamPosition + StreamHeader.SIZE;
            _dat = cnt * IDX_SIZE + _pos;
            _cnt = cnt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DataHeaderReader GetReader(int idx)
        {
            ref var s0 = ref RefB(_stream);
            var pos = AsTU<int>(ref s0, _pos + idx * IDX_SIZE);
            return new(ref AddBU(ref s0, _dat + pos));
        }

        public T[][] ToArray2D<T>() where T : unmanaged
        {
            var cnt = _cnt;
            var rst = new T[cnt][];

            for (var i = 0; i < cnt; i++)
                rst[i] = this[i].ToArray<T>();

            return rst;
        }

        public string[] ToArrayString()
        {
            var cnt = _cnt;
            var rst = new string[cnt];

            for (var i = 0; i < cnt; i++)
                rst[i] = this[i].ToUTF16();

            return rst;
        }
    }
}