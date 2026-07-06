using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct OffsetRange(int offset, int length)
    {
        public readonly int Offset = offset;
        public readonly int Length = length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataRange ToDataRange(ref byte data0) =>
            new(ref Unsafe.AddByteOffset(ref data0, Offset), Length);
    }
}