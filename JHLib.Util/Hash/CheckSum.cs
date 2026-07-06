using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Hash
{
    public static unsafe class CheckSum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ByteXOR(byte[] data) => ByteXOR(ref MemoryMarshal.GetArrayDataReference(data), data.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ByteXOR(byte[] data, int count) => ByteXOR(ref MemoryMarshal.GetArrayDataReference(data), count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ByteXOR(in Span<byte> span) => ByteXOR(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ByteXOR(ref byte data0, int datal)
        {
            fixed (byte* p = &data0)
            {
                byte r = 0;
                if (datal > 0) r = ByteXORInternal(p, datal);
                return r;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte ByteXORInternal(byte* p, int l)
        {
            uint r;
            if (l < 4) { r = *p; if (l != 1) { if (l != 2) { r ^= p[2]; } r ^= p[1]; } }
            else
            {
                r = *(uint*)p;
                if (l > 4)
                {
                    var e = p + (uint)(l - 4);
                    if (l > 8) { p += 4; do r ^= *(uint*)p; while ((p += 4) < e); }
                    r ^= *(uint*)e >> (4 - (l & 3)) * 8;
                }
                r ^= r >> 16;
                r ^= r >> 8;
            }
            return (byte)r;
        }
    }
}