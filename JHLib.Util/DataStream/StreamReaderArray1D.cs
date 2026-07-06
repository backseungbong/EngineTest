using JHLib.Util.ArrayControl;
using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    using static JHLib.Util.Helper.RefCommand;
    public class StreamReaderArray1D<T> where T : unmanaged
    {
        private readonly byte[] _stream;
        private readonly int _pos;
        private readonly int _cnt;
        public int Count => _cnt;
        public ref T this[int i] => ref GetDat(i);
        public StreamReaderArray1D(PoolStream own, int streamPosition)
        {
            ref var hdr = ref own.Ref<StreamHeader>(streamPosition);
            var cnt = hdr.DataCount;

            _stream = own.Stream;
            _pos = streamPosition + StreamHeader.SIZE;
            _cnt = cnt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetDat(int idx)
        {
            ref var s0 = ref RefB(_stream);
            return ref AsTU<T>(ref s0, _pos + idx * Unsafe.SizeOf<T>());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T[] ToArray() => AC.CopyNew(ref this[0], _cnt);
    }
}