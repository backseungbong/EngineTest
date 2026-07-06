using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.DataStream
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe struct StreamHeader(int dataSize, int code, int dataCount, int dataCapacity)
    {
        public const int SIZE = 16;

        public readonly int StreamSize = ToStreamSize(dataSize);
        public readonly int StreamCode = code;
        public readonly int DataCount = dataCount;
        public readonly int DataCapacity = dataCapacity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StreamHeader(int dataSize, int code, int dataCount) : this(dataSize, code, dataCount, dataCount) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToStreamSize(int size) => SIZE + size + 7 & ~7;
    }
}