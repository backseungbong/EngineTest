using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Hash
{
    using static JHLib.Util.Helper.RefCommand;
    internal static class HashUtil
    {
        private static ReadOnlySpan<ulong> Zero128 => [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        private static class EmptyValue<T> { public static readonly T Value = default; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Empty<T>()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() || Unsafe.SizeOf<T>() > 128)
                return ref Unsafe.AsRef(in EmptyValue<T>.Value);
            else
                return ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(Zero128));
        }

        /// <summary> 양수 인덱스가 보장되는 Hash전용, Bucket 참조 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref int Buk0<T>(int[] b, int cap, T key)  =>
            ref RefT(b, (uint)key.GetHashCode() % (uint)cap);

        /// <summary> 양수 인덱스가 보장되는 Hash전용, Bucket 참조 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref int Buk0<T>(ref int b, int cap, T key) =>
            ref AddT(ref b, (uint)key.GetHashCode() % (uint)cap);

        /// <summary> 양수 인덱스가 보장되는 Hash전용, 첫 인덱스 참조 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref char Ref0(string s) => ref RefT(s);

        /// <summary> 양수 인덱스가 보장되는 Hash전용, 첫 인덱스 참조 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Ref0<T>(T[] a) => ref RefT(a);

        /// <summary> 양수 인덱스가 보장되는 Hash전용, 지정 인덱스 참조 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Ref0<T>(T[] a, int i) => ref RefT(a, (uint)i);

        /// <summary> 양수 인덱스가 보장되는 Hash전용, 지정 인덱스 참조 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Ref0<T>(ref T a, int i) => ref AddT(ref a, (uint)i);

        /// <summary> 양수 인덱스가 보장되는 Hash전용, 기존 값 반환 및 새 값 적용 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RepV<T>(T[] v, int i, in T nv, out T ov) { ov = Ref0(v, i); Ref0(v, i) = nv; }


        private static ReadOnlySpan<int> Primes =>
        [
            3, 7, 11, 17, 23, // 5
            29, 37, 47, 59, 71, // 10
            89, 107, 131, 163, 197, // 15
            239, 293, 353, 431, 521, // 20
            631, 761, 919, 1103, 1327, // 25
            1597, 1931, 2333, 2801, 3371, // 30
            4049, 4861, 5839, 7013, 8419, // 35
            10103, 12143, 14591, 17519, 21023, // 40
            25229, 30293, 36353, 43627, 52361, // 45
            62851, 75431, 90523, 108631, 130363, // 50
            156437, 187751, 225307, 270371, 324449, // 55
            389357, 467237, 560689, 672827, 807403, // 60
            968897, 1162687, 1395263, 1674319, 2009191, // 65
            2411033, 2893249, 3471899, 4166287, 4999559, // 70
            5999471, 7199369 // 72
        ];

        private static ReadOnlySpan<byte> JumpTable =>
        [
            0,0,0,1,2,3,6,9,12,16,19,23,27,31,34,38,42,46,50,53,57,61,65,69,69,69,69,69,69,69,69,69,0
        ];

        public const int MinPow2 = 8;
        public const int MinPrime = 3;
        public const int HashPrime = 101;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int GetPrime(int min)
        {
            var j = 32 - BitOperations.LeadingZeroCount((uint)min);
            var i = Unsafe.Add(ref MemoryMarshal.GetReference(JumpTable), j);
            ref var p = ref MemoryMarshal.GetReference(Primes);

            if (Unsafe.Add(ref p, i) < min)
            {
                do if (++i == 72) goto HardWay;
                while (Unsafe.Add(ref p, i) < min);
            }
            return Unsafe.Add(ref p, i);

        HardWay:
            // Outside of our predefined table. Compute the hard way.
            for (var n = (min | 1); n < int.MaxValue; n += 2)
            {
                if (IsPrime(n) && ((n - 1) % HashPrime != 0))
                    return n;
            }
            return min;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int limit = (int)Math.Sqrt(candidate);
                for (int divisor = 3; divisor <= limit; divisor += 2)
                {
                    if ((candidate % divisor) == 0)
                        return false;
                }
                return true;
            }
            return candidate == 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool Cmp(byte[] key1, ref byte key2, int l)
        {
            ref var a = ref RefT(key1);
            ref var b = ref key2;

            if (l > 2)
            {
                if (l > 4)
                {
                    if (l > 8)
                    {
                        if (l > 32)
                        {
                        RE: if (AsT<ulong>(ref a) == AsT<ulong>(ref b) &&
                                AsT<ulong>(ref a, 8) == AsT<ulong>(ref b, 8) &&
                                AsT<ulong>(ref a, 16) == AsT<ulong>(ref b, 16) &&
                                AsT<ulong>(ref a, 24) == AsT<ulong>(ref b, 24))
                            {
                                a = ref AddB(ref a, 32);
                                b = ref AddB(ref b, 32);
                                if ((l -= 32) > 32) goto RE;

                                if (AsT<ulong>(ref a, l - 8) == AsT<ulong>(ref b, l - 8) &&
                                    AsT<ulong>(ref a, l - 16) == AsT<ulong>(ref b, l - 16))
                                {
                                    if (l <= 16 ||
                                        (AsT<ulong>(ref a, l - 24) == AsT<ulong>(ref b, l - 24) &&
                                        AsT<ulong>(ref a, l - 32) == AsT<ulong>(ref b, l - 32)))
                                        return true;
                                }
                            }
                            return false;
                        }

                        if (AsT<ulong>(ref a) == AsT<ulong>(ref b) &&
                            AsT<ulong>(ref a, l - 8) == AsT<ulong>(ref b, l - 8))
                        {
                            if (l <= 16 ||
                                (AsT<ulong>(ref a, 8) == AsT<ulong>(ref b, 8) &&
                                AsT<ulong>(ref a, l - 16) == AsT<ulong>(ref b, l - 16)))
                                return true;
                        }
                        return false;
                    }

                    if (AsT<uint>(ref a) == AsT<uint>(ref b))
                        return AsT<uint>(ref a, l - 4) == AsT<uint>(ref b, l - 4);
                    return false;
                }

                if (AsT<ushort>(ref a) == AsT<ushort>(ref b))
                    return AsT<ushort>(ref a, l - 2) == AsT<ushort>(ref b, l - 2);
                return false;
            }

            if (l == 0 || (a == b && AddB(ref a, l - 1) == AddB(ref b, l - 1)))
                return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool Cmp(string key1, ref char key2, int l)
        {
            ref var a = ref RefB(key1);
            ref var b = ref AsB(ref key2);

            if (l > 2)
            {
                if (l > 4)
                {
                    if (l > 16)
                    {
                        var s = l * 2;
                    RE: if (AsT<ulong>(ref a) == AsT<ulong>(ref b) &&
                            AsT<ulong>(ref a, 8) == AsT<ulong>(ref b, 8) &&
                            AsT<ulong>(ref a, 16) == AsT<ulong>(ref b, 16) &&
                            AsT<ulong>(ref a, 24) == AsT<ulong>(ref b, 24))
                        {
                            a = ref AddB(ref a, 32);
                            b = ref AddB(ref b, 32);
                            if ((s -= 32) > 32) goto RE;

                            if (AsT<ulong>(ref a, s - 8) == AsT<ulong>(ref b, s - 8) &&
                                AsT<ulong>(ref a, s - 16) == AsT<ulong>(ref b, s - 16))
                            {
                                if (s <= 16 ||
                                    (AsT<ulong>(ref a, s - 24) == AsT<ulong>(ref b, s - 24) &&
                                    AsT<ulong>(ref a, s - 32) == AsT<ulong>(ref b, s - 32)))
                                    return true;
                            }
                        }
                        return false;
                    }

                    if (AsT<ulong>(ref a) == AsT<ulong>(ref b) &&
                        AsT<ulong>(ref a, l * 2 - 8) == AsT<ulong>(ref b, l * 2 - 8))
                    {
                        if (l <= 8 ||
                            (AsT<ulong>(ref a, 8) == AsT<ulong>(ref b, 8) &&
                            AsT<ulong>(ref a, l * 2 - 16) == AsT<ulong>(ref b, l * 2 - 16)))
                            return true;
                    }
                    return false;
                }

                if (AsT<uint>(ref a) == AsT<uint>(ref b))
                    return AsT<uint>(ref a, l * 2 - 4) == AsT<uint>(ref b, l * 2 - 4);
                return false;
            }

            if (l == 0 ||
                (AsT<ushort>(ref a) == AsT<ushort>(ref b) &&
                AsT<ushort>(ref a, l * 2 - 2) == AsT<ushort>(ref b, l * 2 - 2)))
                return true;
            return false;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static uint Mix(uint x)
        //{
        //    // https://github.com/skeeto/hash-prospector/issues/19 2026년 년 1월기주능로 낮은 편향 해시 함수
        //    // score: 0.10734781817103507

        //    x ^= x >> 16;
        //    x *= 0x21f0aaad;
        //    x ^= x >> 15;
        //    x *= 0xf35a2d97;
        //    x ^= x >> 15;
        //    return x;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static uint Mix2(uint x)
        //{
        //    // https://github.com/skeeto/hash-prospector/issues/19
        //    // Score: 0.05108489705921976

        //    x = Sse42.Crc32(x, 0x7cdff266);
        //    x *= 0x9c80bf99;
        //    x = Sse42.Crc32(x, 0xf789c7a9);
        //    x *= 0x0c9cb5b5;
        //    return x;
        //}

    }
}