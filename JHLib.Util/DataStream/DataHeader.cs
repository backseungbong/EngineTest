using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.DataStream
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public struct DataHeader(int dataCode, int itemCount)
    {
        private static ReadOnlySpan<uint> EmptyHeader => [0, 0];
        public static ref DataHeader Empty =>
            ref Unsafe.As<uint, DataHeader>(ref MemoryMarshal.GetReference(EmptyHeader));

        public const int SIZE = 8;

        public int DataCode = dataCode;
        public int ItemCount = itemCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ref T Data0<T>() =>
            ref Unsafe.As<DataHeader, T>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in this), SIZE));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ref T Data0<T>(int byteOffset) =>
            ref Unsafe.As<DataHeader, T>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in this), SIZE + byteOffset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ref DataHeader Offset(int offset) =>
            ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in this), SIZE + offset);
    }
}