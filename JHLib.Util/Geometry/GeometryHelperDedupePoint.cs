using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DedupePoints(ref Float2D src, int len, float dedupeSize = 1f)
        {
            if (len < 2)
                return len;

            fixed (Float2D* s = &src)
            {
                if (Sse2.IsSupported)
                {
                    return Avx2.IsSupported ?
                        DedupePointsAvx.DedupePoints(s, len, dedupeSize) :
                        DedupePointsSse.DedupePoints(s, len, dedupeSize);
                }
                else
                {
                    return DedupePointsFallback.DedupePoints(s, len, dedupeSize);
                }
            }
        }

        private static class DedupePointsFallback
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static int DedupePoints(Float2D* src, int len, float dedupeSize)
            {
                var s = src;
                var e = src + len;

                if (dedupeSize > 1f)
                {
                    var d = src;
                    var m = 1f / dedupeSize;
                    var x1 = (int)(src->X * m) + 1;
                    var y1 = (int)(src->Y * m) + 1;
                    do
                    {
                        var x2 = (int)(s->X * m);
                        var y2 = (int)(s->Y * m);
                        if (x1 != x2 || y1 != y2)
                        {
                            *d = *s; d++;
                            x1 = x2;
                            y1 = y2;
                        }
                    }
                    while (++s < e);
                    return (int)((byte*)d - (byte*)src) >> 3;
                }
                else
                {
                    var d = src;
                    var x1 = (int)src->X + 1;
                    var y1 = (int)src->Y + 1;
                    do
                    {
                        var x2 = (int)s->X;
                        var y2 = (int)s->Y;
                        if (x1 != x2 || y1 != y2)
                        {
                            *d = *s; d++;
                            x1 = x2;
                            y1 = y2;
                        }
                    }
                    while (++s < e);
                    return (int)((byte*)d - (byte*)src) >> 3;
                }
            }
        }
        private static class DedupePointsSse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<ulong> Convert64(in Vector128<float> v) =>
                Sse2.ConvertToVector128Int32WithTruncation(v).AsUInt64();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<ulong> Convert64(in Vector128<float> v, in Vector128<float> m) =>
                Sse2.ConvertToVector128Int32WithTruncation(Sse.Multiply(v, m)).AsUInt64();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Convert64Lower(Float2D* p) =>
                Convert64(Sse.LoadVector128((float*)p))[0];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Convert64Upper(Float2D* p) =>
                Convert64(Sse.LoadVector128((float*)p))[1];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Convert64Lower(Float2D* p, in Vector128<float> m) =>
                Convert64(Sse.LoadVector128((float*)p), m)[0];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Convert64Upper(Float2D* p, in Vector128<float> m) =>
                Convert64(Sse.LoadVector128((float*)p), m)[1];


            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static int DedupePoints(Float2D* src, int len, float dedupeSize)
            {
                var s = src;
                var e = src + len - 2;

                if (dedupeSize > 1f)
                {
                    var m = Vector128.Create(1f / dedupeSize);
                    var v = Convert64Lower(s, m) + 1;
                    while (s <= e)
                    {
                        var vf = Sse.LoadVector128((float*)s);
                        var vq = Convert64(vf, m);
                        var u1 = vq[1];
                        var u0 = vq[0];
                        if (u0 == v || u1 == u0) break;
                        v = u1;
                        s += 2;
                    }

                    var d = s;
                    if (s <= e)
                    {
                        do
                        {
                            var vf = Sse.LoadVector128((float*)s);
                            var vq = Convert64(vf, m);
                            var u0 = vq[0];

                            Sse2.StoreScalar((double*)d, vf.AsDouble());
                            d += Unsafe.BitCast<bool, byte>(u0 != v); v = vq[1];
                            Sse2.StoreHigh((double*)d, vf.AsDouble());
                            d += Unsafe.BitCast<bool, byte>(v != u0);
                        }
                        while ((s += 2) <= e);
                    }
                    if ((len & 1) != 0 && Convert64Upper(e, m) != v) { *d = *s; d++; }
                    return (int)((byte*)d - (byte*)src) >> 3;
                }
                else
                {
                    var v = Convert64Lower(s) + 1;
                    while (s <= e)
                    {
                        var vf = Sse.LoadVector128((float*)s);
                        var vq = Convert64(vf);
                        var u1 = vq[1];
                        var u0 = vq[0];
                        if (u0 == v || u1 == u0) break;
                        v = u1;
                        s += 2;
                    }

                    var d = s;
                    if (s <= e)
                    {
                        do
                        {
                            var vf = Sse.LoadVector128((float*)s);
                            var vq = Convert64(vf);
                            var u0 = vq[0];

                            Sse2.StoreScalar((double*)d, vf.AsDouble());
                            d += Unsafe.BitCast<bool, byte>(u0 != v); v = vq[1];
                            Sse2.StoreHigh((double*)d, vf.AsDouble());
                            d += Unsafe.BitCast<bool, byte>(v != u0);
                        }
                        while ((s += 2) <= e);
                    }
                    if ((len & 1) != 0 && Convert64Upper(e) != v) { *d = *s; d++; }
                    return (int)((byte*)d - (byte*)src) >> 3;
                }
            }
        }
        private static class DedupePointsAvx
        {
            private static readonly byte* PermuteMapPointer =
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(PERMUTE_COUNT_MAP));

            private static ReadOnlySpan<byte> PERMUTE_COUNT_MAP =>
            [
                // 8x32 Permute Values
                0, 1, 2, 3, 4, 5, 6, 7, // 0b_0000
                2, 3, 4, 5, 6, 7, 0, 1, // 0b_0001
                0, 1, 4, 5, 6, 7, 0, 1, // 0b_0010
                4, 5, 6, 7, 0, 1, 0, 1, // 0b_0011
                0, 1, 2, 3, 6, 7, 0, 1, // 0b_0100
                2, 3, 6, 7, 0, 1, 0, 1, // 0b_0101
                0, 1, 6, 7, 0, 1, 0, 1, // 0b_0110
                6, 7, 0, 1, 0, 1, 0, 1, // 0b_0111
                0, 1, 2, 3, 4, 5, 0, 1, // 0b_1000
                2, 3, 4, 5, 0, 1, 0, 1, // 0b_1001
                0, 1, 4, 5, 0, 1, 0, 1, // 0b_1010
                4, 5, 0, 1, 0, 1, 0, 1, // 0b_1011
                0, 1, 2, 3, 0, 1, 0, 1, // 0b_1100
                2, 3, 0, 1, 0, 1, 0, 1, // 0b_1101
                0, 1, 0, 1, 0, 1, 0, 1, // 0b_1110
                0, 1, 0, 1, 0, 1, 0, 1, // 0b_1111

                // Valid Permute Count * Float2D Size
                4 * 8, // 0b_0000
                3 * 8, // 0b_0001
                3 * 8, // 0b_0010
                2 * 8, // 0b_0011
                3 * 8, // 0b_0100
                2 * 8, // 0b_0101
                2 * 8, // 0b_0110
                1 * 8, // 0b_0111
                3 * 8, // 0b_1000
                2 * 8, // 0b_1001
                2 * 8, // 0b_1010
                1 * 8, // 0b_1011
                2 * 8, // 0b_1100
                1 * 8, // 0b_1101
                1 * 8, // 0b_1110
                0 * 8, // 0b_1111
            ];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<ulong> Convert64(in Vector128<float> v) =>
                Sse2.ConvertToVector128Int32WithTruncation(v).AsUInt64();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<ulong> Convert64(in Vector256<float> v) =>
                Avx.ConvertToVector256Int32WithTruncation(v).AsUInt64();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<ulong> Convert64(in Vector256<float> v, in Vector256<float> m) =>
                Avx.ConvertToVector256Int32WithTruncation(Avx.Multiply(v, m)).AsUInt64();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Convert64Lower(Float2D* p) =>
                Convert64(Sse.LoadVector128((float*)p))[0];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Convert64Upper(Float2D* p) =>
                Convert64(Sse.LoadVector128((float*)p))[1];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Convert64Lower(Float2D* p, in Vector128<float> m) =>
                Convert64(Sse.Multiply(Sse.LoadVector128((float*)p), m))[0];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Convert64Upper(Float2D* p, in Vector128<float> m) =>
                Convert64(Sse.Multiply(Sse.LoadVector128((float*)p), m))[1];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<float> Permute(in Vector256<float> s, byte* permuteMap, uint mask) =>
                Avx2.PermuteVar8x32(s, Avx2.ConvertToVector256Int32((byte*)((ulong*)permuteMap + mask)));


            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static int DedupePoints(Float2D* src, int len, float dedupeSize)
            {
                var per0 = PermuteMapPointer;
                var cnt0 = per0 + 8 * 16;

                var s = src;
                var e = src + len;

                if (dedupeSize > 1f)
                {
                    var d = (byte*)src;
                    var m = Vector256.Create(1f / dedupeSize);
                    var v = Convert64Lower(s, m.GetLower()) + 1;

                    if (len >= 4)
                    {
                        var e4 = e - 4;
                        var v1 = Vector256.CreateScalarUnsafe(v);

                        uint mm;
                        do
                        {
                            var vf = Avx.LoadVector256((float*)s);
                            var vq = Convert64(vf, m);
                            var v2 = Avx2.Permute4x64(vq, 0b_10_01_00_11);
                            var vb = Avx.Blend(v2.AsDouble(), v1.AsDouble(), 0b_0001); v1 = v2;
                            mm = (uint)Avx.MoveMask(Avx2.CompareEqual(vq, vb.AsUInt64()).AsDouble());
                        }
                        while (mm == 0 && (s += 4) <= e4);
                        d = (byte*)s;

                        if (mm != 0)
                        {
                            Avx.Store((float*)d, Permute(Avx.LoadVector256((float*)s), per0, mm));
                            d += *(cnt0 + mm);

                            if ((s += 4) <= e4)
                            {
                                do
                                {
                                    var vf = Avx.LoadVector256((float*)s);
                                    var vq = Convert64(vf, m);
                                    var v2 = Avx2.Permute4x64(vq, 0b_10_01_00_11);
                                    var vb = Avx.Blend(v2.AsDouble(), v1.AsDouble(), 0b_0001); v1 = v2;
                                    mm = (uint)Avx.MoveMask(Avx2.CompareEqual(vq, vb.AsUInt64()).AsDouble());
                                    Avx.Store((float*)d, Permute(vf, per0, mm));
                                    d += *(cnt0 + mm);
                                }
                                while ((s += 4) <= e4);
                            }
                        }
                        v = v1.ToScalar();
                        if ((len & 3) == 0) { goto RT; }
                    }

                    do
                    {
                        var u = Convert64Upper(s - 1, m.GetLower());
                        if (u != v) { v = u; *(Float2D*)d = *s; d += 8; }
                    }
                    while (++s < e);
                RT: return (int)(d - (byte*)src) >> 3;
                }
                else
                {
                    var d = (byte*)src;
                    var v = Convert64Lower(s) + 1;

                    if (len >= 4)
                    {
                        var e4 = e - 4;
                        var v1 = Vector256.CreateScalarUnsafe(v);

                        uint mm;
                        do
                        {
                            var vf = Avx.LoadVector256((float*)s);
                            var vq = Convert64(vf);
                            var v2 = Avx2.Permute4x64(vq, 0b_10_01_00_11);
                            var vb = Avx.Blend(v2.AsDouble(), v1.AsDouble(), 0b_0001); v1 = v2;
                            mm = (uint)Avx.MoveMask(Avx2.CompareEqual(vq, vb.AsUInt64()).AsDouble());
                        }
                        while (mm == 0 && (s += 4) <= e4);
                        d = (byte*)s;

                        if (mm != 0)
                        {
                            Avx.Store((float*)d, Permute(Avx.LoadVector256((float*)s), per0, mm));
                            d += *(cnt0 + mm);

                            if ((s += 4) <= e4)
                            {
                                do
                                {
                                    var vf = Avx.LoadVector256((float*)s);
                                    var vq = Convert64(vf);
                                    var v2 = Avx2.Permute4x64(vq, 0b_10_01_00_11);
                                    var vb = Avx.Blend(v2.AsDouble(), v1.AsDouble(), 0b_0001); v1 = v2;
                                    mm = (uint)Avx.MoveMask(Avx2.CompareEqual(vq, vb.AsUInt64()).AsDouble());
                                    Avx.Store((float*)d, Permute(vf, per0, mm));
                                    d += *(cnt0 + mm);
                                }
                                while ((s += 4) <= e4);
                            }
                        }
                        v = v1.ToScalar();
                        if ((len & 3) == 0) { goto EX; }
                    }

                    do
                    {
                        var u = Convert64Upper(s - 1);
                        if (u != v) { v = u; *(Float2D*)d = *s; d += 8; }
                    }
                    while (++s < e);
                EX: return (int)(d - (byte*)src) >> 3;
                }
            }
        }
    }
}