using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace JHLib.Util.ByteControl
{
    /// <summary> BigEndian 기반 스트림 컨버터 </summary>
    public unsafe static class BigEndian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt(byte* p)
        {
            if (BitConverter.IsLittleEndian)
                return (uint)(p[0] << 24 | p[1] << 16 | p[2] << 8 | p[3]);
            return *(uint*)p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt3(byte* p)
        {
            if (BitConverter.IsLittleEndian)
                return (uint)(p[0] << 16 | p[1] << 8 | p[2]);
            return *(uint*)p >> 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUShort(byte* p)
        {
            if (BitConverter.IsLittleEndian)
                return (ushort)(p[0] << 8 | p[1]);
            return *(ushort*)p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToBigEndian(uint v)
        {
            if (BitConverter.IsLittleEndian)
                return BinaryPrimitives.ReverseEndianness(v);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToBigEndian(ushort v)
        {
            if (BitConverter.IsLittleEndian)
                return BinaryPrimitives.ReverseEndianness(v);
            return v;
        }
    }
}