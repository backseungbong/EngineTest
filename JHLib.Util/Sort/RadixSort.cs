using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Sort
{
    using static JHLib.Util.Helper.UnsafeEx;

    public unsafe delegate void RadixSortSetup32Handler(uint* destinationBuffer, int itemCount);
    public unsafe delegate void RadixSortSorted32Handler(uint* sortedBuffer, int itemCount);
    public unsafe delegate void RadixSortSetup64Handler(ulong* destinationBuffer, int itemCount);
    public unsafe delegate void RadixSortSorted64Handler(ulong* sortedBuffer, int itemCount);

    public static unsafe class RadixSort
    {
        // 256 for counts, 64 for alignment padding
        private const int BASE_BUFFER_SIZE = 256 * sizeof(uint) + 64;
        private static byte[] GetPoolBuffer32(int itemCount) =>
            ArrayPool<byte>.Shared.Rent(BASE_BUFFER_SIZE + itemCount * sizeof(uint));
        private static byte[] GetPoolBuffer64(int itemCount) =>
            ArrayPool<byte>.Shared.Rent(BASE_BUFFER_SIZE + itemCount * sizeof(ulong));
        private static void ReturnPoolBuffer(byte[] buffer) =>
            ArrayPool<byte>.Shared.Return(buffer);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run(uint[] items) => RunInternal(Arr0(items), items.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunInternal(in uint items, int itemCount)
        {
            var buffer = GetPoolBuffer32(itemCount);
            fixed (byte* buffer0 = &Arr0(buffer))
            fixed (uint* item0 = &AsRef(items))
            {
                var cnt0 = (uint*)((nint)buffer0 + 63 & ~63);
                var dst0 = cnt0 + 256;

                Calculate0(cnt0, item0, dst0, itemCount);
                CalculateN(cnt0, dst0, item0, itemCount, 1);
                CalculateN(cnt0, item0, dst0, itemCount, 2);
                CalculateN(cnt0, dst0, item0, itemCount, 3);
            }
            ReturnPoolBuffer(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Run(ulong[] items) => RunInternal(Arr0(items), items.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunInternal(in ulong items, int itemCount)
        {
            var buffer = GetPoolBuffer64(itemCount);
            fixed (byte* buffer0 = &Arr0(buffer))
            fixed (ulong* item0 = &AsRef(items))
            {
                var cnt0 = (uint*)((nint)buffer0 + 63 & ~63);
                var dst0 = (ulong*)(cnt0 + 256);

                Calculate0(cnt0, item0, dst0, itemCount);
                CalculateN(cnt0, dst0, item0, itemCount, 1);
                CalculateN(cnt0, item0, dst0, itemCount, 2);
                CalculateN(cnt0, dst0, item0, itemCount, 3);
                CalculateN(cnt0, item0, dst0, itemCount, 4);
                CalculateN(cnt0, dst0, item0, itemCount, 5);
                CalculateN(cnt0, item0, dst0, itemCount, 6);
                CalculateN(cnt0, dst0, item0, itemCount, 7);
            }
            ReturnPoolBuffer(buffer);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Run32Callbacks(
            int itemCount,
            RadixSortSetup32Handler initializer,
            RadixSortSorted32Handler resultHandler,
            uint sortKeyStartByte = 0,
            uint sortKeyByteCount = sizeof(uint))
        {
            if (sortKeyStartByte < 4 && sortKeyByteCount - 1 < 4)
            {
                var alignCount = itemCount + 15 & ~15;
                var endByte = sortKeyStartByte + sortKeyByteCount;
                if (endByte > 4) endByte = 4;

                var buffer = GetPoolBuffer32(alignCount * 2);
                fixed (byte* buffer0 = &Arr0(buffer))
                {
                    var cnt0 = (uint*)((nint)buffer0 + 63 & ~63);
                    var src0 = cnt0 + 256;
                    var dst0 = cnt0 + 256 + (uint)alignCount;

                    initializer(src0, itemCount);

                    var posByte = sortKeyStartByte;
                    do
                    {
                        if (posByte == 0)
                            Calculate0(cnt0, src0, dst0, itemCount);
                        else
                            CalculateN(cnt0, src0, dst0, itemCount, posByte);

                        var temp = dst0;
                        dst0 = src0;
                        src0 = temp;
                    }
                    while (++posByte < endByte);
                    resultHandler(src0, itemCount);
                }
                ReturnPoolBuffer(buffer);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Run64Callbacks(
           int itemCount,
           RadixSortSetup64Handler initializer,
           RadixSortSorted64Handler resultHandler,
           uint sortKeyStartByte = 0,
           uint sortKeyByteCount = sizeof(ulong))
        {
            if (sortKeyStartByte < 4 && sortKeyByteCount - 1 < 4)
            {
                var alignCount = itemCount + 7 & ~7;
                var endByte = sortKeyStartByte + sortKeyByteCount;
                if (endByte > 4) endByte = 4;

                var buffer = GetPoolBuffer64(alignCount * 2);
                fixed (byte* buffer0 = &Arr0(buffer))
                {
                    var cnt0 = (uint*)((nint)buffer0 + 63 & ~63);
                    var src0 = (ulong*)(cnt0 + 256);
                    var dst0 = (ulong*)(cnt0 + 256) + (uint)alignCount;

                    initializer(src0, itemCount);

                    var posByte = sortKeyStartByte;
                    do
                    {
                        if (posByte == 0)
                            Calculate0(cnt0, src0, dst0, itemCount);
                        else
                            CalculateN(cnt0, src0, dst0, itemCount, posByte);

                        var temp = dst0;
                        dst0 = src0;
                        src0 = temp;
                    }
                    while (++posByte < endByte);
                    resultHandler(src0, itemCount);
                }
                ReturnPoolBuffer(buffer);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Calculate0(uint* c0, uint* s0, uint* d0, int count)
        {
            Clear(c0, c0 + 256);
            Frequencies(c0, s0, (uint)count, 0);
            PrefixSum(c0, c0 + 256);
            Distribute(c0, d0, s0, (uint)count, 0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CalculateN(uint* c0, uint* s0, uint* d0, int count, uint bp)
        {
            Clear(c0, c0 + 256);
            Frequencies(c0, s0, (uint)count, bp);
            PrefixSum(c0, c0 + 256);
            Distribute(c0, d0, s0, (uint)count, (int)(bp * 8));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Calculate0(uint* c0, ulong* s0, ulong* d0, int count)
        {
            Clear(c0, c0 + 256);
            Frequencies(c0, s0, (uint)count, 0);
            PrefixSum(c0, c0 + 256);
            Distribute(c0, d0, s0, (uint)count, 0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CalculateN(uint* c0, ulong* s0, ulong* d0, int count, uint bp)
        {
            Clear(c0, c0 + 256);
            Frequencies(c0, s0, (uint)count, bp);
            PrefixSum(c0, c0 + 256);
            Distribute(c0, d0, s0, (uint)count, (int)(bp * 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Clear(uint* c0, uint* ce)
        {
            if (Sse2.IsSupported)
            {
                if (Avx.IsSupported)
                {
                    if (Avx512F.IsSupported)
                    {
                        var c = c0;
                        var z = Vector512<uint>.Zero;
                        do Avx512F.StoreAligned(c, z);
                        while ((c += 16) < ce);
                    }
                    else
                    {
                        var c = c0;
                        var z = Vector256<uint>.Zero;
                        do { Avx.StoreAligned(c + 0, z); Avx.StoreAligned(c + 8, z); }
                        while ((c += 16) < ce);
                    }
                }
                else
                {
                    var c = c0;
                    var z = Vector128<uint>.Zero;
                    do
                    {
                        Sse2.StoreAligned(c + 00, z);
                        Sse2.StoreAligned(c + 04, z);
                        Sse2.StoreAligned(c + 08, z);
                        Sse2.StoreAligned(c + 12, z);
                    }
                    while ((c += 16) < ce);
                }
            }
            else
            {
                var c = c0;
                var z = 0ul;
                do
                {
                    *(ulong*)(c + 00) = z;
                    *(ulong*)(c + 02) = z;
                    *(ulong*)(c + 04) = z;
                    *(ulong*)(c + 06) = z;
                }
                while ((c += 8) < ce);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Frequencies(uint* c0, uint* t0, uint tn, uint bp)
        {
            var t = t0;
            var e = t0 + tn;

            if (tn > 4)
            {
                do
                {
                    ++c0[((byte*)(t + 0))[bp]];
                    ++c0[((byte*)(t + 1))[bp]];
                    ++c0[((byte*)(t + 2))[bp]];
                    ++c0[((byte*)(t + 3))[bp]];
                }
                while ((t += 4) < e - 4);
            }

            do ++c0[((byte*)(t + 0))[bp]];
            while (++t < e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Frequencies(uint* c0, ulong* t0, uint tn, uint bp)
        {
            var t = t0;
            var e = t0 + tn;

            if (tn > 4)
            {
                do
                {
                    ++c0[((byte*)(t + 0))[bp]];
                    ++c0[((byte*)(t + 1))[bp]];
                    ++c0[((byte*)(t + 2))[bp]];
                    ++c0[((byte*)(t + 3))[bp]];
                }
                while ((t += 4) < e - 4);
            }

            do ++c0[((byte*)(t + 0))[bp]];
            while (++t < e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PrefixSum(uint* c0, uint* ce)
        {
            if (Sse2.IsSupported)
            {
                var c = c0;
                var s = Vector128<uint>.Zero;
                do
                {
                    var v = Sse2.LoadAlignedVector128(c);
                    v = Sse2.Add(v, Sse2.ShiftLeftLogical128BitLane(v, 4));
                    v = Sse2.Add(v, Sse2.ShiftLeftLogical128BitLane(v, 8));
                    s = Sse2.Add(v, Sse2.Shuffle(s, 0b11_11_11_11));
                    Sse2.StoreAligned(c, s);

                    v = Sse2.LoadAlignedVector128(c + 4);
                    v = Sse2.Add(v, Sse2.ShiftLeftLogical128BitLane(v, 4));
                    v = Sse2.Add(v, Sse2.ShiftLeftLogical128BitLane(v, 8));
                    s = Sse2.Add(v, Sse2.Shuffle(s, 0b11_11_11_11));
                    Sse2.StoreAligned(c + 4, s);
                }
                while ((c += 8) < ce);
            }
            else
            {
                var c = c0;
                var s = 0u;
                do
                {
                    // 계산 순서를 재구성하여 의존성 체인을 끊고 성능을 최적화
                    // 누적 합 알고리즘은 본질적으로 이전 결과에 대한 의존성을 가질 수밖에 없으므로
                    // 이전 결과에 대한 의존성을 줄이고 병렬 처리를 극대화하는 것이 중요
                    // 개선코드로 약 25%이상 성능 향상

                    // 기존코드
                    // s += c[0]; c[0] = s;
                    // s += c[1]; c[1] = s;
                    // s += c[2]; c[2] = s;
                    // s += c[3]; c[3] = s;

                    // 개선코드
                    var c123 = c[0] + c[1] + c[2];
                    c[1] = s + c123 - c[2];
                    c[0] += s;
                    c[2] = s + c123;
                    c[3] = s += c123 + c[3];
                }
                while ((c += 4) < ce);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Distribute(uint* c0, uint* o0, uint* t0, uint tn, int s)
        {
            var t = t0 + tn;

            if (tn > 4)
            {
                do
                {
                    t -= 4;
                    DistributeValue(c0, o0, t[3], s);
                    DistributeValue(c0, o0, t[2], s);
                    DistributeValue(c0, o0, t[1], s);
                    DistributeValue(c0, o0, t[0], s);
                }
                while (t0 + 4 < t);
            }

            do DistributeValue(c0, o0, *--t, s);
            while (t0 < t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Distribute(uint* c0, ulong* o0, ulong* t0, uint tn, int s)
        {
            var t = t0 + tn;

            if (tn > 4)
            {
                do
                {
                    t -= 4;
                    DistributeValue(c0, o0, t[3], s);
                    DistributeValue(c0, o0, t[2], s);
                    DistributeValue(c0, o0, t[1], s);
                    DistributeValue(c0, o0, t[0], s);
                }
                while (t0 + 4 < t);
            }

            do DistributeValue(c0, o0, *--t, s);
            while (t0 < t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DistributeValue(uint* c0, uint* o0, uint v, int s) =>
            o0[--c0[(byte)(v >> s)]] = v;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DistributeValue(uint* c0, ulong* o0, ulong v, int s) =>
            o0[--c0[(byte)(v >> s)]] = v;
    }
}