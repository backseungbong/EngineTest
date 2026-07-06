using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Performance
{
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe readonly ref struct PinnedBuffer
    {
        public readonly byte[] Buffer;
        public readonly nint Pointer;
        public readonly int AlignOffset =>
            (int)(Pointer - (nint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(Buffer)));
        public readonly ref byte Byte0 =>
            ref Unsafe.AsRef<byte>((void*)Pointer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PinnedBuffer(int capacity, int alignment = 32, bool clear = false)
        {
            var alignmask = alignment - 1;
            if (alignment < 8 || (alignment & alignmask) != 0)
                ExceptionInvalidAlignment(alignment);

            var buffer = GC.AllocateUninitializedArray<byte>(capacity + alignmask, true);
            var pointer = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(buffer));

            if (clear)
                buffer.AsSpan().Clear();

            Buffer = buffer;
            Pointer = (pointer + alignmask) & ~alignmask;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ExceptionInvalidAlignment(int alignment) =>
            throw new ArgumentException("Alignment must be a power of two and at least 8.", nameof(alignment));
    }
}