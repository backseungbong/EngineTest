using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Helper.RefCommand;
    /// <summary>
    /// RAM 제한 속도에 가깝게 작동하는 매우 빠른 비 암호화 해시 알고리즘 <para/>
    /// 데이타의 크기가 64 바이트 크기 이하인 경우 H32가 더 빠르고, 그 이상의 경우는 H64의 속도가 더 빠르다 <para/>
    /// 성능을 위해 알고리즘의 일부를 최적화 하였고, 기존 해쉬 결과와 비교하여 검증을 완료하였다 <para/>
    /// 실제 이 소스코드로 테스트 결과(i7-4790K DDR3 RAM) H64의 경우 최대 15.4 GB/s, H32의 경우 = 7.8 GB/s 까지의 속도를 보여준다 <para/>
    /// 결과는 같은 메모리 공간을 반복하여 얻은 결과이므로 실제 처리 속도는 다를 수 있다 <para/>
    /// </summary>
    public unsafe static class XXHash
    {
        private const uint P321 = 0x9E3779B1U; /* 0b10011110001101110111100110110001 */
        private const uint P322 = 0x85EBCA77U; /* 0b10000101111010111100101001110111 */
        private const uint P323 = 0xC2B2AE3DU; /* 0b11000010101100101010111000111101 */
        private const uint P324 = 0x27D4EB2FU; /* 0b00100111110101001110101100101111 */
        private const uint P325 = 0x165667B1U; /* 0b00010110010101100110011110110001 */
        private const uint P3212 = unchecked(P321 + P322);
        private const uint P321M = unchecked(0 - P321);

        private const ulong P641 = 0x9E3779B185EBCA87UL; /* 0b1001111000110111011110011011000110000101111010111100101010000111 */
        private const ulong P642 = 0xC2B2AE3D27D4EB4FUL; /* 0b1100001010110010101011100011110100100111110101001110101101001111 */
        private const ulong P643 = 0x165667B19E3779F9UL; /* 0b0001011001010110011001111011000110011110001101110111100111111001 */
        private const ulong P644 = 0x85EBCA77C2B2AE63UL; /* 0b1000010111101011110010100111011111000010101100101010111001100011 */
        private const ulong P645 = 0x27D4EB2F165667C5UL; /* 0b0010011111010100111010110010111100010110010101100110011111000101 */
        private const ulong P6412 = unchecked(P641 + P642);
        private const ulong P641M = unchecked(0 - P641);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32<T>(ref T v) where T : unmanaged =>
            H32(ref AsB(ref v), sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32<T>(ref T v, int count) where T : unmanaged =>
            H32(ref AsB(ref v), count * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32(string str) =>
            H32(ref RefB(str), str.Length * 2);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32<T>(T[] array) where T : unmanaged =>
            H32(ref RefB(array), array.Length * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32<T>(T[] array, int count) where T : unmanaged =>
            H32(ref RefB(array), count * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32<T>(T[] array, int index, int count) where T : unmanaged =>
            H32(ref RefB(array, index), count * sizeof(T));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32(byte[] array) =>
            H32(ref RefT(array), array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32(byte[] array, int count) =>
            H32(ref RefT(array), count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32(byte[] array, int index, int count) =>
            H32(ref RefT(array, index), count);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32(Span<byte> span) => H32(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32(ReadOnlySpan<byte> span) => H32(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int H32(byte* p, int l) => H32(ref *p, l);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int H32(ref byte p, int l)
        {
            var h = P325 + (uint)l;
            var n = l;
            if (n >= 16)
            {
                var a = P3212;
                var b = P322;
                var c = 0u;
                var d = P321M;

            RE: a += AsT<uint>(ref p) * P322;
                b += AsT<uint>(ref p, 4) * P322;
                c += AsT<uint>(ref p, 8) * P322;
                d += AsT<uint>(ref p, 12) * P322; p = ref AddB(ref p, 16);
                a = BitOperations.RotateLeft(a, 13) * P321;
                b = BitOperations.RotateLeft(b, 13) * P321;
                c = BitOperations.RotateLeft(c, 13) * P321;
                d = BitOperations.RotateLeft(d, 13) * P321;
                if ((n -= 16) >= 16) goto RE;

                h = BitOperations.RotateLeft(a, 01) +
                    BitOperations.RotateLeft(b, 07) +
                    BitOperations.RotateLeft(c, 12) +
                    BitOperations.RotateLeft(d, 18) + (uint)l;
            }

            if (n >= 4)
            {
            RE: h += AsT<uint>(ref p) * P323;
                h = BitOperations.RotateLeft(h, 17) * P324;
                p = ref AddB(ref p, 4);
                if ((n -= 4) >= 4) goto RE;
            }

            if (n > 0)
            {
                h += p * P325;
                h = BitOperations.RotateLeft(h, 11) * P321;
                if (n > 1)
                {
                    h += AddB(ref p, 1) * P325;
                    h = BitOperations.RotateLeft(h, 11) * P321;
                    if (n > 2)
                    {
                        h += AddB(ref p, 2) * P325;
                        h = BitOperations.RotateLeft(h, 11) * P321;
                    }
                }
            }

            h = (h ^ (h >> 15)) * P322;
            h = (h ^ (h >> 13)) * P323;
            return (int)(h ^ (h >> 16));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64<T>(ref T v) where T : unmanaged =>
            H64(ref AsB(ref v), sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64<T>(ref T v, int count) where T : unmanaged =>
            H64(ref AsB(ref v), count * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64(string str) =>
            H64(ref RefB(str), str.Length * 2);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64<T>(T[] array) where T : unmanaged =>
            H64(ref RefB(array), array.Length * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64<T>(T[] array, int count) where T : unmanaged =>
            H64(ref RefB(array), count * sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64<T>(T[] array, int index, int count) where T : unmanaged =>
            H64(ref RefB(array, index), count * sizeof(T));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64(byte[] array) =>
            H64(ref RefT(array), array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64(byte[] array, int count) =>
            H64(ref RefT(array), count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64(byte[] array, int index, int count) =>
            H64(ref RefT(array, index), count);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64(Span<byte> span) => H64(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64(ReadOnlySpan<byte> span) => H64(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long H64(byte* p, int l) => H64(ref *p, l);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long H64(ref byte p, int l)
        {
            var h = P645 + (ulong)l;
            var n = l;
            if (n >= 32)
            {
                var a = P6412;
                var b = P642;
                var c = 0UL;
                var d = P641M;

            RE: a += AsT<ulong>(ref p) * P642;
                b += AsT<ulong>(ref p, 08) * P642;
                c += AsT<ulong>(ref p, 16) * P642;
                d += AsT<ulong>(ref p, 24) * P642; p = ref AddB(ref p, 32);
                a = BitOperations.RotateLeft(a, 31) * P641;
                b = BitOperations.RotateLeft(b, 31) * P641;
                c = BitOperations.RotateLeft(c, 31) * P641;
                d = BitOperations.RotateLeft(d, 31) * P641;
                if ((n -= 32) >= 32) goto RE;

                h = BitOperations.RotateLeft(a, 01) +
                    BitOperations.RotateLeft(b, 07) +
                    BitOperations.RotateLeft(c, 12) +
                    BitOperations.RotateLeft(d, 18);

                h = (BitOperations.RotateLeft(a * P642, 31) * P641 ^ h) * P641 + P644;
                h = (BitOperations.RotateLeft(b * P642, 31) * P641 ^ h) * P641 + P644;
                h = (BitOperations.RotateLeft(c * P642, 31) * P641 ^ h) * P641 + P644;
                h = (BitOperations.RotateLeft(d * P642, 31) * P641 ^ h) * P641 + P644 + (ulong)l;
            }

            if (n >= 8)
            {
            RE: h ^= BitOperations.RotateLeft(AsT<ulong>(ref p) * P642, 31) * P641;
                h = BitOperations.RotateLeft(h, 27) * P641 + P644;
                p = ref AddB(ref p, 8);
                if ((n -= 8) >= 8) goto RE;
            }

            if (n >= 4)
            {
                h ^= AsT<uint>(ref p) * P641;
                h = BitOperations.RotateLeft(h, 23) * P642 + P643;
                p = ref AddB(ref p, 4);
                n -= 4;
            }

            if (n > 0)
            {
                h ^= p * P645;
                h = BitOperations.RotateLeft(h, 11) * P641;
                if (n > 1)
                {
                    h ^= AddB(ref p, 1) * P645;
                    h = BitOperations.RotateLeft(h, 11) * P641;
                    if (n > 2)
                    {
                        h ^= AddB(ref p, 2) * P645;
                        h = BitOperations.RotateLeft(h, 11) * P641;
                    }
                }
            }

            h = (h ^ (h >> 33)) * P642;
            h = (h ^ (h >> 29)) * P643;
            return (long)(h ^ (h >> 32));
        }
    }
}