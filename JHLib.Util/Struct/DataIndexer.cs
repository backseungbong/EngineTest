using System.Runtime.CompilerServices;

namespace JHLib.Util.Struct
{
    public readonly ref struct DataIndexer<T>
    {
        public readonly ref T Data0;
        public readonly ref T this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref Data0, i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataIndexer(ref byte data0) => Data0 = ref Unsafe.As<byte, T>(ref data0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataIndexer(ref T data0) => Data0 = ref data0;
    }
}