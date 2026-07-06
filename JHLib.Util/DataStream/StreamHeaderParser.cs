using JHLib.Util.Pool;
using System.Runtime.CompilerServices;

namespace JHLib.Util.DataStream
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref struct StreamHeaderParser(PoolStream stream)
    {
        private int _read = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool MatchOrNext(int streamCode, out int streamPosition)
        {
            var read = _read;
            if (read + StreamHeader.SIZE <= stream.Position)
            {
                ref var header = ref stream.Ref<StreamHeader>(read);
                if (header.StreamSize >= StreamHeader.SIZE &&
                    header.StreamCode == streamCode)
                {
                    _read = read + header.StreamSize;

                    streamPosition = read;
                    return true;
                }
            }
            streamPosition = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T AsItem<T>(int streamPosition) where T : unmanaged =>
            ref stream.Ref<T>(streamPosition + StreamHeader.SIZE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StreamReaderArray1D<T> AsArray1D<T>(int streamPosition) where T : unmanaged =>
            new(stream, streamPosition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StreamReaderArray2D AsArray2D(int streamPosition) =>
            new(stream, streamPosition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StreamReaderKeyValue<T> AsKeyValue<T>(int streamPosition) where T : unmanaged =>
            new(stream, streamPosition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StreamReaderKeyArray AsKeyArray(int streamPosition) =>
            new(stream, streamPosition);
    }
}