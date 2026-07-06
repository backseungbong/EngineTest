using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Graphic.Helper
{
    public static unsafe class PixelHelper
    {
        /// <summary> 
        /// 부동 소수점 누적 계산 시 소수점 손실로 인해 <para/>
        /// 픽셀이 일부 누실 되는 문제가 발생할 수 있으므로 <para/>
        /// 이를 보완하기 위해 0.005f를 더한 값을 사용 <para/>
        /// 일부 픽셀이 약간 더 그려질 수 있지만 알고리즘상 문제가 없고, 계산 성능 하락이 없음 <para/>
        /// 이미지 크기가 1000 이상으로 커질때 float의 유효숫자의 한계로 인해 <para/>
        /// 소수점이 많이 소실되므로 0.005f이 적당한 최소값으로 판단 <para/>
        /// 필요하다면 Kahan summation algorithm 같은 방법을 사용하는 방법도 고려될수 있음
        /// </summary>  
        public const float ROUND_FACTOR = 0.005f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> Load(float v) => Vector128.CreateScalarUnsafe(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float v1, float v2) => Sse.MinScalar(Load(v1), Load(v2)).ToScalar();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float v1, float v2) => Sse.MaxScalar(Load(v1), Load(v2)).ToScalar();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool XCheck(float v1, float v2, float min, float max)
        {
            var f1 = Sse.MaxScalar(Load(min), Load(v1));
            var f2 = Sse.MinScalar(Load(max), Load(v2));
            return Sse.CompareScalarUnorderedLessThan(f1, f2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool YCheck(float v1, float v2, float min, float max, out float y1, out float y2)
        {
            var f1 = Sse.MaxScalar(Load(min), Load(v1));
            y1 = MathF.Truncate(f1.ToScalar() + 0.5f) + 0.5f;
            y2 = Sse.MinScalar(Load(max), Load(v2)).ToScalar();
            return y1 <= y2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XFill(float v1, float v2, float min, float max, byte* dt, uint c32, ulong c64)
        {
            var f1 = Sse.MaxScalar(Load(min), Load(v1));
            var f2 = Sse.MinScalar(Load(max), Load(v2));
            if (Sse.CompareScalarUnorderedLessThan(f1, f2))
            {
                var x1 = Sse.ConvertToInt32(f1);
                var x2 = Sse.ConvertToInt32(f2);
                if (x2 > x1) { FillRange(dt, x1, x2, c32, c64); }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XBlend(float v1, float v2, float min, float max, byte* dt, uint c32, uint a32)
        {
            var f1 = Sse.MaxScalar(Load(min), Load(v1));
            var f2 = Sse.MinScalar(Load(max), Load(v2));
            if (Sse.CompareScalarUnorderedLessThan(f1, f2))
            {
                var x1 = Sse.ConvertToInt32(f1);
                var x2 = Sse.ConvertToInt32(f2);
                if (x2 > x1) { BlendRange(dt, x1, x2, c32, a32); }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(float v) =>
            Sse.ConvertToInt32WithTruncation(Load(v));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt(float v) =>
            (uint)Sse.ConvertToInt32WithTruncation(Load(v));

        /// <summary> round-to-nearest-even </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIntRound(float v) =>
            Sse.ConvertToInt32(Load(v));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Premul(uint c32)
        {
            var a = c32 >> 24;
            var c1 = (c32 & 0x00FF00FF) * (a + 1) & 0xFF00FF00;
            var c2 = (c32 & 0x0000FF00) * (a + 1) & 0x00FF0000;
            return a << 24 | (c1 | c2) >> 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint BlendColor(uint originColor, uint blendColor)
        {
            var scale = 256 - (blendColor >> 24);
            var d1 = ((originColor & 0x00FF00FF) * scale >> 8) & 0x00FF00FF;
            var d2 = ((originColor & 0xFF00FF00) >> 8) * scale & 0xFF00FF00;
            return (d1 | d2) + blendColor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend32(byte* d, uint color, uint scale) => Blend32(d, *(uint*)d, color, scale);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend32(byte* d, uint value, uint color, uint scale)
        {
            var d1 = ((value & 0x00FF00FF) * scale >> 8) & 0x00FF00FF;
            var d2 = ((value & 0xFF00FF00) >> 8) * scale & 0xFF00FF00;
            *(uint*)d = (d1 | d2) + color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend64(byte* d, ulong color, uint scale) => Blend64(d, *(ulong*)d, color, scale);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend64(byte* d, ulong value, ulong color, uint scale)
        {
            var d1 = ((value & 0x00FF00FF00FF00FF) * scale >> 8) & 0x00FF00FF00FF00FF;
            var d2 = ((value & 0xFF00FF00FF00FF00) >> 8) * scale & 0xFF00FF00FF00FF00;
            *(ulong*)d = (d1 | d2) + color;
        }


        /// <summary> 인라인 목적 고성능 픽셀 채우기. 조건분기 최소화, 코드수 최소화, 대량 채우기시 SIMD명령어 연계 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillRange(byte* d0, int xn, uint c32, ulong c64) =>
            FillRange(d0, (byte*)((uint*)d0 + (uint)xn), xn, c32, c64);

        /// <summary> 인라인 목적 고성능 픽셀 채우기. 조건분기 최소화, 코드수 최소화, 대량 채우기시 SIMD명령어 연계 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillRange(byte* d0, int x1, int x2, uint c32, ulong c64) =>
            FillRange((byte*)((uint*)d0 + (uint)x1), (byte*)((uint*)d0 + (uint)x2), x2 - x1, c32, c64);

        /// <summary> 인라인 목적 고성능 픽셀 채우기. 조건분기 최소화, 코드수 최소화, 대량 채우기시 SIMD명령어 연계 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillRange(byte* s, byte* e, int c, uint c32, ulong c64)
        {
            if (c > 2)
            {
                if (c > 6)
                {
                    if (c <= 12)
                    {
                        *(uint*)(s + 12) = c32;
                        *(ulong*)(s + 16) = c64;
                        *(ulong*)(e - 24) = c64;
                        *(uint*)(e - 16) = c32;
                    }
                    else { SimdFill(s, e, c, c64); return; }
                }
                *(ulong*)(s + 4) = c64;
                *(ulong*)(e - 12) = c64;
            }
            *(uint*)(s + 0) = c32;
            *(uint*)(e - 4) = c32;
        }

        /// <summary> 인라인 목적 고성능 픽셀 채우기. 조건분기 최소화, 코드수 최소화, 대량 채우기시 SIMD명령어 연계 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillRange(byte* s, byte* e, int c, uint c32)
        {
            if (c > 2)
            {
                var c64 = (ulong)c32 << 32 | c32;
                if (c > 6)
                {
                    if (c <= 12)
                    {
                        *(uint*)(s + 12) = c32;
                        *(ulong*)(s + 16) = c64;
                        *(ulong*)(e - 24) = c64;
                        *(uint*)(e - 16) = c32;
                    }
                    else { SimdFill(s, e, c, c64); return; }
                }
                *(ulong*)(s + 4) = c64;
                *(ulong*)(e - 12) = c64;
            }
            *(uint*)(s + 0) = c32;
            *(uint*)(e - 4) = c32;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SimdFill(byte* s, byte* e, int c, ulong c64)
        {
            if (Avx2.IsSupported)
            {
                var c256 = Vector256.Create(c64);
                Avx.Store((ulong*)s, c256);
                if (c > 16)
                {
                    var e64 = e - 64;
                    if (c > 24)
                    {
                        var a32 = (byte*)((nint)s + 32 & ~31);
                    RE: Avx.StoreAligned((ulong*)(a32 + 00), c256);
                        Avx.StoreAligned((ulong*)(a32 + 32), c256);
                        if ((a32 += 64) < e64) { goto RE; }
                    }
                    Avx.Store((ulong*)e64, c256);
                }
                Avx.Store((ulong*)(e - 32), c256);
            }
            else
            {
                var c128 = Vector128.Create(c64);
                Sse2.Store((ulong*)s, c128);
                var e32 = e - 32;
                var a16 = (byte*)((nint)s + 16 & ~15);
            RE: Sse2.StoreAligned((ulong*)(a16 + 00), c128);
                Sse2.StoreAligned((ulong*)(a16 + 16), c128);
                if ((a16 += 32) < e32) { goto RE; }
                Sse2.Store((ulong*)(e32 + 00), c128);
                Sse2.Store((ulong*)(e32 + 16), c128);
            }
        }

        /// <summary> 인라인 목적 고성능 픽셀 블렌드. 조건분기 최소화, 코드수 최소화, 대량 블렌드시 SIMD명령어 연계 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendRange(byte* d0, int xn, uint c32, uint a32) =>
            BlendRange(d0, (byte*)((uint*)d0 + (uint)xn), xn, c32, a32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendRange(byte* d0, int x1, int x2, uint c32, uint a32) =>
            BlendRange((byte*)((uint*)d0 + (uint)x1), (byte*)((uint*)d0 + (uint)x2), x2 - x1, c32, a32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendRange(byte* s, byte* e, int c, uint c32, uint a32)
        {
            if (c == 1)
            {
                var d1 = ((*(uint*)s & 0x00FF00FF) * a32 >> 8) & 0x00FF00FF;
                var d2 = ((*(uint*)s & 0xFF00FF00) >> 8) * a32 & 0xFF00FF00;
                *(uint*)s = (d1 | d2) + c32;
            }
            else if (c > 16) { BlendAligned(s, e, c, c32, a32); }
            else
            {
                var c128 = Vector128.Create(c32).AsUInt16();
                var a128 = Vector128.Create((ushort)a32);

                if (c > 4)
                {
                    e -= 16;
                    var l128 = Sse2.LoadVector128((ushort*)e);
                    do BlendSse2.BlendUnaligned(s, Sse2.LoadVector128((ushort*)s), c128, a128);
                    while ((s += 16) < e);
                    BlendSse2.BlendUnaligned(e, l128, c128, a128);
                }
                else
                {
                    BlendSse2.BlendUnaligned8x2(s, e - 8, c128, a128);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BlendAligned(byte* s, byte* e, int c, uint c32, uint a32)
        {
            if (Avx2.IsSupported)
            {
                var c256 = Vector256.Create(c32).AsUInt16();
                var a256 = Vector256.Create((ushort)a32).AsUInt16();

                if (c > 64)
                {
                    var l256 = Avx.LoadVector256((ushort*)s);
                    var a = (byte*)((nint)s + 32 & ~31);
                    BlendAvx2.Blend(a, Avx.LoadAlignedVector256((ushort*)a), c256, a256);
                    BlendAvx2.BlendUnaligned(s, l256, c256, a256); s = a + 32;
                    do
                    {
                        BlendAvx2.Blend(s + 00, Avx.LoadAlignedVector256((ushort*)(s + 00)), c256, a256);
                        BlendAvx2.Blend(s + 32, Avx.LoadAlignedVector256((ushort*)(s + 32)), c256, a256);
                    }
                    while ((s += 64) < e - 96);
                }
                {
                    var e32 = e - 32;
                    var l256 = Avx.LoadVector256((ushort*)e32);
                    do BlendAvx2.BlendUnaligned(s, Avx.LoadVector256((ushort*)s), c256, a256);
                    while ((s += 32) < e32);
                    BlendAvx2.BlendUnaligned(e32, l256, c256, a256);
                }
            }
            else
            {
                var c128 = Vector128.Create(c32).AsUInt16();
                var a128 = Vector128.Create((ushort)a32).AsUInt16();

                if (c > 32)
                {
                    var l128 = Sse2.LoadVector128((ushort*)s);
                    var a = (byte*)((nint)s + 16 & ~15);
                    BlendSse2.Blend(a, Sse2.LoadAlignedVector128((ushort*)a), c128, a128);
                    BlendSse2.BlendUnaligned(s, l128, c128, a128); s = a + 16;
                    do
                    {
                        BlendSse2.Blend(s + 00, Sse2.LoadAlignedVector128((ushort*)(s + 00)), c128, a128);
                        BlendSse2.Blend(s + 16, Sse2.LoadAlignedVector128((ushort*)(s + 16)), c128, a128);
                        BlendSse2.Blend(s + 32, Sse2.LoadAlignedVector128((ushort*)(s + 32)), c128, a128);
                    }
                    while ((s += 48) < e - 64);
                }
                {
                    var e16 = e - 16;
                    var l128 = Sse2.LoadVector128((ushort*)e16);
                    do BlendSse2.BlendUnaligned(s, Sse2.LoadVector128((ushort*)s), c128, a128);
                    while ((s += 16) < e16);
                    BlendSse2.BlendUnaligned(e16, l128, c128, a128);
                }
            }
        }
    }
}