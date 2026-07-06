using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.ByteControl
{
    public static class BitReverse
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit16x2(uint v) => BitOperations.RotateLeft(Bit32(v), 16);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit32(int v) => Bit32((uint)v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit32(uint v)
        {
            v = ((v & 0x55555555) << 1) | ((v >> 1) & 0x55555555);
            v = ((v & 0x33333333) << 2) | ((v >> 2) & 0x33333333);
            v = ((v & 0x0F0F0F0F) << 4) | ((v >> 4) & 0x0F0F0F0F);
            return BinaryPrimitives.ReverseEndianness(v);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit16(int v) => Bit16((uint)v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit16(uint v)
        {
            v = ((v & 0x5555) << 1) | ((v >> 1) & 0x5555);
            v = ((v & 0x3333) << 2) | ((v >> 2) & 0x3333);
            v = ((v & 0x0F0F) << 4) | ((v >> 4) & 0x0F0F);
            return BinaryPrimitives.ReverseEndianness(v << 16);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit8(int v) => Bit8((uint)v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit8(uint v)
        {
            v = ((v & 0x55) << 1) | ((v >> 1) & 0x55);
            v = ((v & 0x33) << 2) | ((v >> 2) & 0x33);
            return ((v & 0x0F) << 4) | (v >> 4);
        }

        /// <summary>
        /// 어셈블리상 Bit8보다 좀더 고성능이지만 결과에 쓰레기 데이타가 발생함(left 4비트)<br/>
        /// uint결과에서 left 4비트를 사용하지 않는 상황에서만 사용해야함
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bit8Unsafe(uint v)
        {
            v = ((v & 0x55) << 1) | ((v >> 1) & 0x55);
            v = ((v & 0x33) << 2) | ((v >> 2) & 0x33);
            v = BitOperations.RotateRight(v, 4); // 0x000000AB -> 0xB000000A
            return v | (v >> 24); // 0xB000000A | 0x000000B0 -> 0xB00000BA 왼쪽 끝에 쓰레기가 남음
        }
    }
}