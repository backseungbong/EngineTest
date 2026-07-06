using JHLib.Util.Graphic.Data;
using JHLib.Util.Simd;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Graphic
{
    internal abstract unsafe class BlendInternal
    {
        /// <summary> 비트맵을 복사한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BitmapCopy(byte* s0, byte* d0, int len)
        {
            if (Sse2.IsSupported)
                if (Avx2.IsSupported) { BlendAvx2.BitmapCopy(s0, d0, len); }
                else { BlendSse2.BitmapCopy(s0, d0, len); }
            else { BlendArm64.BitmapCopy(s0, d0, len); }
        }

        /// <summary> 비트맵을 복사한다 (이 함수는 반드시 소스 Pitch가 복사될 Pitch보다 작을때만 호출되어야한다) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BitmapCopyRow(byte* s0, byte* d0, int sPitch, int dPitch, int height)
        {
            if (Avx2.IsSupported &&
                SIMD.AlignCheck(s0) && SIMD.AlignCheck(d0) &&
                SIMD.AlignCheck(sPitch) && SIMD.AlignCheck(dPitch))
            {
                // avx512 코드 지원 필요
                //if (Avx512F.IsSupported)
                //{
                //}
                BlendAvx2.BitmapCopyRow(s0, d0, sPitch, dPitch, height);
            }
            else
            {
                var sp = (nuint)sPitch;
                var dp = (nuint)dPitch;
                var se = s0 + sp * (uint)height;
                var s = s0;
                var d = d0;
                do { NativeMemory.Copy(s, d, sp); d += dp; }
                while ((s += sp) != se);
            }
        }

        /// <summary> BGRA8888 혹은 RGBA8888 비트맵 블랜드를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendSimple(byte* s0, byte* d0, int len)
        {
            if (Sse2.IsSupported)
                if (Avx2.IsSupported) { BlendAvx2.BlendSimple(s0, d0, len); }
                else { BlendSse2.BlendSimple(s0, d0, len); }
            else { throw new Exception("not yet supported"); } // 단순 비트맵 블랜드는 아직 Arm64 코드가 작성되지 않음
        }

        /// <summary> BGRA8888 혹은 RGBA8888 비트맵 블랜드를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BlendMakeRegion(byte* s0, byte* d0, int len, CacheRegion[] buk = null)
        {
            if (Sse2.IsSupported)
                if (Avx2.IsSupported) { return BlendAvx2.BlendMakeRegion(s0, d0, len, buk); }
                else { return BlendSse2.BlendMakeRegion(s0, d0, len, buk); }
            else { return BlendArm64.BlendMakeRegion(s0, d0, len, buk); }
        }

        /// <summary> BGRA8888 혹은 RGBA8888 비트맵 블랜드를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendWithRegion(byte* s0, byte* d0, CacheRegion[] buk, int cnt)
        {
            if (Sse2.IsSupported)
                if (Avx2.IsSupported) { BlendAvx2.BlendWithRegion(s0, d0, buk, cnt); }
                else { BlendSse2.BlendWithRegion(s0, d0, buk, cnt); }
            else { BlendArm64.BlendWithRegion(s0, d0, buk, cnt); }
        }
    }

    internal static unsafe class BlendSse2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<uint> ToAlpha(in Vector128<ushort> c) =>
            Sse2.ShiftRightLogical(c.AsUInt32(), 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ushort> ToScale(in Vector128<uint> a, in Vector128<byte> s, in Vector128<ushort> i)
        {
            if (Ssse3.IsSupported)
                return Sse2.Subtract(i, Ssse3.Shuffle(a.AsByte(), s).AsUInt16());
            else
                return Sse2.Subtract(i, Sse2.Or(a, Sse2.ShiftLeftLogical128BitLane(a, 2)).AsUInt16());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEqual<T>(in Vector128<T> v, void* address) =>
            v.AsUInt64() == Sse2.LoadAlignedVector128((ulong*)address);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEqual<T>(in Vector128<T> v1, in Vector128<T> v2) =>
            v1.AsUInt64() == v2.AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend(byte* dest, in Vector128<ushort> v, in Vector128<ushort> c, in Vector128<ushort> s)
        {
            var d1 = Sse2.MultiplyHigh(Sse2.ShiftLeftLogical128BitLane(v, 1), s);
            var d2 = Sse2.ShiftLeftLogical128BitLane(Sse2.MultiplyHigh(v, s), 1);
            Sse2.StoreAligned((ushort*)dest, Sse2.Add(c, Sse2.Or(d1, d2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendUnaligned(byte* dest, in Vector128<ushort> v, in Vector128<ushort> c, in Vector128<ushort> s)
        {
            var d1 = Sse2.MultiplyHigh(Sse2.ShiftLeftLogical128BitLane(v, 1), s);
            var d2 = Sse2.ShiftLeftLogical128BitLane(Sse2.MultiplyHigh(v, s), 1);
            Sse2.Store((ushort*)dest, Sse2.Add(c, Sse2.Or(d1, d2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendUnaligned8x2(byte* dest1, byte* dest2, in Vector128<ushort> c, in Vector128<ushort> s)
        {
            var dv = Sse2.LoadHigh(Sse2.LoadScalarVector128((double*)dest1), (double*)dest2).AsUInt16();
            var d1 = Sse2.MultiplyHigh(Sse2.ShiftLeftLogical128BitLane(dv, 1), s);
            var d2 = Sse2.ShiftLeftLogical128BitLane(Sse2.MultiplyHigh(dv, s), 1);
            var dr = Sse2.Add(c, Sse2.Or(d1, d2)).AsDouble();
            Sse2.StoreScalar((double*)dest1, dr);
            Sse2.StoreHigh((double*)dest2, dr);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BitmapCopy(byte* s0, byte* d0, int len)
        {
            if (len < 64) // 64바이트 이상의 비트맵만 처리
                return;

            var s = s0;
            var d = d0;
            var r = len >> 6;

            if (len < SIMDParam.NonTemporalStoreThresholdForCopy)
            {
                do
                {
                    Sse2.StoreAligned(d + 00, Sse2.LoadAlignedVector128(s + 00));
                    Sse2.StoreAligned(d + 16, Sse2.LoadAlignedVector128(s + 16));
                    Sse2.StoreAligned(d + 32, Sse2.LoadAlignedVector128(s + 32));
                    Sse2.StoreAligned(d + 48, Sse2.LoadAlignedVector128(s + 48));
                    s += 64; d += 64;
                }
                while (--r != 0);
            }
            else
            {
                if (Sse41.IsSupported)
                {
                    do
                    {
                        Sse2.StoreAlignedNonTemporal(d + 00, Sse41.LoadAlignedVector128NonTemporal(s + 00));
                        Sse2.StoreAlignedNonTemporal(d + 16, Sse41.LoadAlignedVector128NonTemporal(s + 16));
                        Sse2.StoreAlignedNonTemporal(d + 32, Sse41.LoadAlignedVector128NonTemporal(s + 32));
                        Sse2.StoreAlignedNonTemporal(d + 48, Sse41.LoadAlignedVector128NonTemporal(s + 48));
                        s += 64; d += 64;
                    }
                    while (--r != 0);
                }
                else
                {
                    do
                    {
                        Sse2.StoreAlignedNonTemporal(d + 00, Sse2.LoadAlignedVector128(s + 00));
                        Sse2.StoreAlignedNonTemporal(d + 16, Sse2.LoadAlignedVector128(s + 16));
                        Sse2.StoreAlignedNonTemporal(d + 32, Sse2.LoadAlignedVector128(s + 32));
                        Sse2.StoreAlignedNonTemporal(d + 48, Sse2.LoadAlignedVector128(s + 48));
                        s += 64; d += 64;
                    }
                    while (--r != 0);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BlendSimple(byte* s0, byte* d0, int len)
        {
            var opa = Vector128.Create((uint)0x000000FF);
            var inv = Vector128.Create((ushort)0x0100);
            var shf = Vector128.Create((byte)
                00, 01, 00, 01, 04, 05, 04, 05, 08, 09, 08, 09, 12, 13, 12, 13);

            var src = s0;
            var e64 = s0 + len - 64;

            while (src <= e64)
            {
                var v1 = Sse2.LoadAlignedVector128((ushort*)(src + 00));
                var v2 = Sse2.LoadAlignedVector128((ushort*)(src + 16));
                var v3 = Sse2.LoadAlignedVector128((ushort*)(src + 32));
                var v4 = Sse2.LoadAlignedVector128((ushort*)(src + 48));

                var d = d0 + (src - s0); src += 64;
                if (IsEqual(v1, v4) && IsEqual(v2, v3) && IsEqual(v1, v2) && src <= e64)
                {
                    while (IsEqual(v1, src + 48) && IsEqual(v1, src + 32) &&
                        IsEqual(v1, src + 16) && IsEqual(v1, src + 00) && (src += 64) <= e64) ;

                    var alpha = ToAlpha(v1);
                    if (alpha != Vector128<uint>.Zero)
                    {
                        var e = d0 + (src - s0);
                        if (IsEqual(alpha, opa))
                        {
                            do
                            {
                                Sse2.StoreAligned((ushort*)(d + 00), v1);
                                Sse2.StoreAligned((ushort*)(d + 16), v1);
                                Sse2.StoreAligned((ushort*)(d + 32), v1);
                                Sse2.StoreAligned((ushort*)(d + 48), v1);
                            }
                            while ((d += 64) < e);
                        }
                        else
                        {
                            var scale = ToScale(alpha, shf, inv);
                            do
                            {
                                Blend(d + 00, Sse2.LoadAlignedVector128((ushort*)(d + 00)), v1, scale);
                                Blend(d + 16, Sse2.LoadAlignedVector128((ushort*)(d + 16)), v1, scale);
                                Blend(d + 32, Sse2.LoadAlignedVector128((ushort*)(d + 32)), v1, scale);
                                Blend(d + 48, Sse2.LoadAlignedVector128((ushort*)(d + 48)), v1, scale);
                            }
                            while ((d += 64) < e);
                        }
                    }
                }
                else
                {
                    Blend(d + 00, Sse2.LoadAlignedVector128((ushort*)(d + 00)), v1, ToScale(ToAlpha(v1), shf, inv));
                    Blend(d + 16, Sse2.LoadAlignedVector128((ushort*)(d + 16)), v2, ToScale(ToAlpha(v2), shf, inv));
                    Blend(d + 32, Sse2.LoadAlignedVector128((ushort*)(d + 32)), v3, ToScale(ToAlpha(v3), shf, inv));
                    Blend(d + 48, Sse2.LoadAlignedVector128((ushort*)(d + 48)), v4, ToScale(ToAlpha(v4), shf, inv));
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int BlendMakeRegion(byte* s0, byte* d0, int len, CacheRegion[] buk)
        {
            var opa = Vector128.Create((uint)0x000000FF);
            var inv = Vector128.Create((ushort)0x0100);
            var shf = Vector128.Create((byte)00, 01, 00, 01, 04, 05, 04, 05, 08, 09, 08, 09, 12, 13, 12, 13);

            var src = s0;
            var e64 = s0 + len - 64;

            var s = s0;
            var t = s0;

            ref var b0 = ref Unsafe.NullRef<CacheRegion>();
            if (buk != null) b0 = ref MemoryMarshal.GetArrayDataReference(buk);
            ref var b = ref b0;

            while (src <= e64)
            {
                var v1 = Sse2.LoadAlignedVector128((ushort*)(src + 00));
                var v2 = Sse2.LoadAlignedVector128((ushort*)(src + 16));
                var v3 = Sse2.LoadAlignedVector128((ushort*)(src + 32));
                var v4 = Sse2.LoadAlignedVector128((ushort*)(src + 48));
                s = src; src += 64;

                var d = d0 + (s - s0);
                if (IsEqual(v1, v4) && IsEqual(v2, v3) && IsEqual(v1, v2) && src <= e64)
                {
                    while (IsEqual(v1, src + 48) && IsEqual(v1, src + 32) &&
                        IsEqual(v1, src + 16) && IsEqual(v1, src + 00) && (src += 64) <= e64) ;

                    var l = (uint)(src - s);
                    if (l >= CacheRegion.MIN_REGION && Unsafe.IsNullRef(ref b0) == false)
                    {
                        b = new CacheRegion((uint)(s - t), l);
                        b = ref Unsafe.Add(ref b, 1);
                        t = src;
                    }

                    var alpha = ToAlpha(v1);
                    if (alpha != Vector128<uint>.Zero)
                    {
                        var e = d0 + (src - s0);
                        if (IsEqual(alpha, opa))
                        {
                            do
                            {
                                Sse2.StoreAligned((ushort*)(d + 00), v1);
                                Sse2.StoreAligned((ushort*)(d + 16), v1);
                                Sse2.StoreAligned((ushort*)(d + 32), v1);
                                Sse2.StoreAligned((ushort*)(d + 48), v1);
                            }
                            while ((d += 64) < e);
                        }
                        else
                        {
                            var scale = ToScale(alpha, shf, inv);
                            do
                            {
                                Blend(d + 00, Sse2.LoadAlignedVector128((ushort*)(d + 00)), v1, scale);
                                Blend(d + 16, Sse2.LoadAlignedVector128((ushort*)(d + 16)), v1, scale);
                                Blend(d + 32, Sse2.LoadAlignedVector128((ushort*)(d + 32)), v1, scale);
                                Blend(d + 48, Sse2.LoadAlignedVector128((ushort*)(d + 48)), v1, scale);
                            }
                            while ((d += 64) < e);
                        }
                    }
                }
                else
                {
                    Blend(d + 00, Sse2.LoadAlignedVector128((ushort*)(d + 00)), v1, ToScale(ToAlpha(v1), shf, inv));
                    Blend(d + 16, Sse2.LoadAlignedVector128((ushort*)(d + 16)), v2, ToScale(ToAlpha(v2), shf, inv));
                    Blend(d + 32, Sse2.LoadAlignedVector128((ushort*)(d + 32)), v3, ToScale(ToAlpha(v3), shf, inv));
                    Blend(d + 48, Sse2.LoadAlignedVector128((ushort*)(d + 48)), v4, ToScale(ToAlpha(v4), shf, inv));
                }
            }

            if (Unsafe.IsNullRef(ref b0) == false && src != t)
            {
                b = new((uint)(s - t), (uint)(src - s));
                b = ref Unsafe.Add(ref b, 1);
            }
            return (int)Unsafe.ByteOffset(ref b0, ref b) >> 3; // == div 8(sizeof(CacheRegion))
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BlendWithRegion(byte* s0, byte* d0, CacheRegion[] buk, int cnt)
        {
            var opa = Vector128.Create((uint)0x000000FF);
            var inv = Vector128.Create((ushort)0x0100);
            var shf = Vector128.Create((byte)
                00, 01, 00, 01, 04, 05, 04, 05, 08, 09, 08, 09, 12, 13, 12, 13);

            var e = d0;
            ref var r0 = ref MemoryMarshal.GetArrayDataReference(buk);
            ref var re = ref Unsafe.Add(ref r0, (uint)cnt);

            while (Unsafe.IsAddressLessThan(ref r0, ref re))
            {
                var s = s0 + (e - d0);
                var d = e;

                var nonRepeatedBytes = r0.NonRepeatedBytes;
                if (nonRepeatedBytes != 0)
                {
                    e += nonRepeatedBytes;
                    do
                    {
                        var p1 = Sse2.LoadAlignedVector128((ushort*)(s + 00));
                        var p2 = Sse2.LoadAlignedVector128((ushort*)(s + 16));
                        Blend(d + 00, Sse2.LoadAlignedVector128((ushort*)(d + 00)), p1, ToScale(ToAlpha(p1), shf, inv));
                        Blend(d + 16, Sse2.LoadAlignedVector128((ushort*)(d + 16)), p2, ToScale(ToAlpha(p2), shf, inv));
                        var p3 = Sse2.LoadAlignedVector128((ushort*)(s + 32));
                        var p4 = Sse2.LoadAlignedVector128((ushort*)(s + 48));
                        Blend(d + 32, Sse2.LoadAlignedVector128((ushort*)(d + 32)), p3, ToScale(ToAlpha(p3), shf, inv));
                        Blend(d + 48, Sse2.LoadAlignedVector128((ushort*)(d + 48)), p4, ToScale(ToAlpha(p4), shf, inv));
                        s += 64;
                    }
                    while ((d += 64) < e);
                }

                var repeatedBytes = r0.RepeatedBytes; r0 = ref Unsafe.Add(ref r0, 1);
                if (repeatedBytes != 0)
                {
                    var v = Sse2.LoadAlignedVector128((ushort*)s);
                    e += repeatedBytes;

                    var alpha = ToAlpha(v);
                    if (alpha != Vector128<uint>.Zero)
                    {
                        if (IsEqual(alpha, opa))
                        {
                            do
                            {
                                Sse2.StoreAligned((ushort*)(d + 00), v);
                                Sse2.StoreAligned((ushort*)(d + 16), v);
                                Sse2.StoreAligned((ushort*)(d + 32), v);
                                Sse2.StoreAligned((ushort*)(d + 48), v);
                            }
                            while ((d += 64) < e);
                        }
                        else
                        {
                            var scale = ToScale(alpha, shf, inv);
                            do
                            {
                                Blend(d + 00, Sse2.LoadAlignedVector128((ushort*)(d + 00)), v, scale);
                                Blend(d + 16, Sse2.LoadAlignedVector128((ushort*)(d + 16)), v, scale);
                                Blend(d + 32, Sse2.LoadAlignedVector128((ushort*)(d + 32)), v, scale);
                                Blend(d + 48, Sse2.LoadAlignedVector128((ushort*)(d + 48)), v, scale);
                            }
                            while ((d += 64) < e);
                        }
                    }
                }
            }
        }
    }

    internal static unsafe class BlendAvx2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<uint> ToAlpha(in Vector256<ushort> c) =>
            Avx2.ShiftRightLogical(c.AsUInt32(), 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ushort> ToScale(in Vector256<uint> a, in Vector256<byte> s, in Vector256<ushort> i) =>
            Avx2.Subtract(i, Avx2.Shuffle(a.AsByte(), s).AsUInt16());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEqual<T>(in Vector256<T> v, void* address) =>
            v.AsUInt64() == Avx.LoadAlignedVector256((ulong*)address);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEqual<T>(in Vector256<T> v1, in Vector256<T> v2) =>
            v1.AsUInt64() == v2.AsUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend(byte* d, in Vector256<ushort> v, in Vector256<ushort> c, in Vector256<ushort> s)
        {
            var d1 = Avx2.MultiplyHigh(Avx2.ShiftLeftLogical128BitLane(v, 1), s);
            var d2 = Avx2.ShiftLeftLogical128BitLane(Avx2.MultiplyHigh(v, s), 1);
            Avx.StoreAligned((ushort*)d, Avx2.Add(c, Avx2.Or(d1, d2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendUnaligned(byte* d, in Vector256<ushort> v, in Vector256<ushort> c, in Vector256<ushort> s)
        {
            var d1 = Avx2.MultiplyHigh(Avx2.ShiftLeftLogical128BitLane(v, 1), s);
            var d2 = Avx2.ShiftLeftLogical128BitLane(Avx2.MultiplyHigh(v, s), 1);
            Avx.Store((ushort*)d, Avx2.Add(c, Avx2.Or(d1, d2)));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BitmapCopy(byte* s0, byte* d0, int len)
        {
            if (len < 128) // 128바이트 이상의 비트맵만 처리
                return;

            var s = s0;
            var d = d0;
            var sEnd = s0 + (uint)(len & ~127);

            if (len < SIMDParam.NonTemporalStoreThresholdForCopy)
            {
                do
                {
                    Avx.StoreAligned(d + 00, Avx.LoadAlignedVector256(s + 00));
                    Avx.StoreAligned(d + 32, Avx.LoadAlignedVector256(s + 32));
                    Avx.StoreAligned(d + 64, Avx.LoadAlignedVector256(s + 64));
                    Avx.StoreAligned(d + 96, Avx.LoadAlignedVector256(s + 96));
                    d += 128;
                }
                while ((s += 128) != sEnd);
            }
            else
            {
                do
                {
                    Avx.StoreAlignedNonTemporal(d + 00, Avx2.LoadAlignedVector256NonTemporal(s + 00));
                    Avx.StoreAlignedNonTemporal(d + 32, Avx2.LoadAlignedVector256NonTemporal(s + 32));
                    Avx.StoreAlignedNonTemporal(d + 64, Avx2.LoadAlignedVector256NonTemporal(s + 64));
                    Avx.StoreAlignedNonTemporal(d + 96, Avx2.LoadAlignedVector256NonTemporal(s + 96));
                    d += 128;
                }
                while ((s += 128) != sEnd);
            }
        }

        /// <summary> 이 함수는 sPitch가 dPitch보다 반드시 작을때만 호출하여야 한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BitmapCopyRow(byte* s0, byte* d0, int sPitch, int dPitch, int height)
        {
            var l128 = (uint)(sPitch & ~127);
            var sRem = (uint)sPitch - l128;
            var dRem = (uint)dPitch - l128;
            var sEnd = s0 + (uint)(sPitch * height);

            var s = s0;
            var d = d0;

            if (l128 != 0)
            {
                if (sPitch * height < SIMDParam.NonTemporalStoreThresholdForCopy)
                {
                    do
                    {
                        var e = s + l128;
                        do
                        {
                            Avx.StoreAligned(d + 00, Avx.LoadAlignedVector256(s + 00));
                            Avx.StoreAligned(d + 32, Avx.LoadAlignedVector256(s + 32));
                            Avx.StoreAligned(d + 64, Avx.LoadAlignedVector256(s + 64));
                            Avx.StoreAligned(d + 96, Avx.LoadAlignedVector256(s + 96));
                            d += 128;
                        }
                        while ((s += 128) != e);

                        if (sRem != 0)
                        {
                            Avx.StoreAligned(d + 00, Avx.LoadAlignedVector256(s + 00));
                            if (sRem > 32)
                            {
                                Avx.StoreAligned(d + 32, Avx.LoadAlignedVector256(s + 32));
                                if (sRem > 64)
                                {
                                    Avx.StoreAligned(d + 64, Avx.LoadAlignedVector256(s + 64));
                                }
                            }
                        }
                        d += dRem;
                    }
                    while ((s += sRem) < sEnd);
                }
                else
                {
                    do
                    {
                        var e = s + l128;
                        do
                        {
                            Avx.StoreAlignedNonTemporal(d + 00, Avx2.LoadAlignedVector256NonTemporal(s + 00));
                            Avx.StoreAlignedNonTemporal(d + 32, Avx2.LoadAlignedVector256NonTemporal(s + 32));
                            Avx.StoreAlignedNonTemporal(d + 64, Avx2.LoadAlignedVector256NonTemporal(s + 64));
                            Avx.StoreAlignedNonTemporal(d + 96, Avx2.LoadAlignedVector256NonTemporal(s + 96));
                            d += 128;
                        }
                        while ((s += 128) != e);

                        if (sRem != 0)
                        {
                            Avx.StoreAlignedNonTemporal(d + 00, Avx2.LoadAlignedVector256NonTemporal(s + 00));
                            if (sRem > 32)
                            {
                                Avx.StoreAlignedNonTemporal(d + 32, Avx2.LoadAlignedVector256NonTemporal(s + 32));
                                if (sRem > 64)
                                {
                                    Avx.StoreAlignedNonTemporal(d + 64, Avx2.LoadAlignedVector256NonTemporal(s + 64));
                                }
                            }
                        }
                        d += dRem;
                    }
                    while ((s += sRem) < sEnd);
                }
            }
            else
            {
                do
                {
                    Avx.StoreAligned(d + 00, Avx.LoadAlignedVector256(s + 00));
                    if (sRem > 32)
                    {
                        Avx.StoreAligned(d + 32, Avx.LoadAlignedVector256(s + 32));
                        if (sRem > 64)
                        {
                            Avx.StoreAligned(d + 64, Avx.LoadAlignedVector256(s + 64));
                        }
                    }
                    d += dRem;
                }
                while ((s += sRem) < sEnd);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BlendSimple(byte* s0, byte* d0, int len)
        {
            var opa = Vector256.Create((uint)0x000000FF);
            var inv = Vector256.Create((ushort)0x0100);
            var shf = Vector256.Create((byte)
                00, 01, 00, 01, 04, 05, 04, 05, 08, 09, 08, 09, 12, 13, 12, 13,
                16, 17, 16, 17, 20, 21, 20, 21, 24, 25, 24, 25, 28, 29, 28, 29);

            var src = s0;
            var e64 = s0 + len - 64;

            while (src <= e64)
            {
                var v1 = Avx.LoadAlignedVector256((ushort*)(src + 00));
                var v2 = Avx.LoadAlignedVector256((ushort*)(src + 32));

                var d = d0 + (src - s0); src += 64;
                if (IsEqual(v1, v2) && src <= e64)
                {
                    while (IsEqual(v1, src + 32) && IsEqual(v1, src + 00) && (src += 64) <= e64) ;

                    var alpha = ToAlpha(v1);
                    if (alpha != Vector256<uint>.Zero)
                    {
                        var e = d0 + (src - s0);
                        if (IsEqual(alpha, opa))
                        {
                            do
                            {
                                Avx.StoreAligned((ushort*)(d + 00), v1);
                                Avx.StoreAligned((ushort*)(d + 32), v1);
                            }
                            while ((d += 64) < e);
                        }
                        else
                        {
                            var scale = ToScale(alpha, shf, inv);
                            do
                            {
                                Blend(d + 00, Avx.LoadAlignedVector256((ushort*)(d + 00)), v1, scale);
                                Blend(d + 32, Avx.LoadAlignedVector256((ushort*)(d + 32)), v1, scale);
                            }
                            while ((d += 64) < e);
                        }
                    }
                }
                else
                {
                    Blend(d + 00, Avx.LoadAlignedVector256((ushort*)(d + 00)), v1, ToScale(ToAlpha(v1), shf, inv));
                    Blend(d + 32, Avx.LoadAlignedVector256((ushort*)(d + 32)), v2, ToScale(ToAlpha(v2), shf, inv));
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int BlendMakeRegion(byte* s0, byte* d0, int len, CacheRegion[] buk)
        {
            var opa = Vector256.Create((uint)0x000000FF);
            var inv = Vector256.Create((ushort)0x0100);
            var shf = Vector256.Create((byte)
                00, 01, 00, 01, 04, 05, 04, 05, 08, 09, 08, 09, 12, 13, 12, 13,
                16, 17, 16, 17, 20, 21, 20, 21, 24, 25, 24, 25, 28, 29, 28, 29);

            var src = s0;
            var e64 = s0 + len - 64;

            var s = s0;
            var t = s0;

            ref var b0 = ref Unsafe.NullRef<CacheRegion>();
            if (buk != null) b0 = ref MemoryMarshal.GetArrayDataReference(buk);
            ref var b = ref b0;

            while (src <= e64)
            {
                var v1 = Avx.LoadAlignedVector256((ushort*)(src + 00));
                var v2 = Avx.LoadAlignedVector256((ushort*)(src + 32));
                s = src; src += 64;

                var d = d0 + (s - s0);
                if (IsEqual(v1, v2) && src <= e64)
                {
                    while (IsEqual(v1, src + 32) && IsEqual(v1, src + 00) && (src += 64) <= e64) ;

                    var l = (uint)(src - s);
                    if (l >= CacheRegion.MIN_REGION && Unsafe.IsNullRef(ref b0) == false)
                    {
                        b = new CacheRegion((uint)(s - t), l);
                        b = ref Unsafe.Add(ref b, 1);
                        t = src;
                    }

                    var alpha = ToAlpha(v1);
                    if (alpha != Vector256<uint>.Zero)
                    {
                        var e = d0 + (src - s0);
                        if (IsEqual(alpha, opa))
                        {
                            do
                            {
                                Avx.StoreAligned((ushort*)(d + 00), v1);
                                Avx.StoreAligned((ushort*)(d + 32), v1);
                            }
                            while ((d += 64) < e);
                        }
                        else
                        {
                            var scale = ToScale(alpha, shf, inv);
                            do
                            {
                                Blend(d + 00, Avx.LoadAlignedVector256((ushort*)(d + 00)), v1, scale);
                                Blend(d + 32, Avx.LoadAlignedVector256((ushort*)(d + 32)), v1, scale);
                            }
                            while ((d += 64) < e);
                        }
                    }
                }
                else
                {
                    Blend(d + 00, Avx.LoadAlignedVector256((ushort*)(d + 00)), v1, ToScale(ToAlpha(v1), shf, inv));
                    Blend(d + 32, Avx.LoadAlignedVector256((ushort*)(d + 32)), v2, ToScale(ToAlpha(v2), shf, inv));
                }
            }

            if (Unsafe.IsNullRef(ref b0) == false && src != t)
            {
                b = new((uint)(s - t), (uint)(src - s));
                b = ref Unsafe.Add(ref b, 1);
            }
            return (int)Unsafe.ByteOffset(ref b0, ref b) >> 3; // == div 8(sizeof(CacheRegion))
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BlendWithRegion(byte* s0, byte* d0, CacheRegion[] buk, int cnt)
        {
            var opa = Vector256.Create((uint)0x000000FF);
            var inv = Vector256.Create((ushort)0x0100);
            var shf = Vector256.Create((byte)
                00, 01, 00, 01, 04, 05, 04, 05, 08, 09, 08, 09, 12, 13, 12, 13,
                16, 17, 16, 17, 20, 21, 20, 21, 24, 25, 24, 25, 28, 29, 28, 29);

            var e = d0;
            ref var r0 = ref MemoryMarshal.GetArrayDataReference(buk);
            ref var re = ref Unsafe.Add(ref r0, (uint)cnt);

            while (Unsafe.IsAddressLessThan(ref r0, ref re))
            {
                var s = s0 + (e - d0);
                var d = e;

                var nonRepeatedBytes = r0.NonRepeatedBytes;
                if (nonRepeatedBytes != 0)
                {
                    e += nonRepeatedBytes;
                    do
                    {
                        var v1 = Avx.LoadAlignedVector256((ushort*)(s + 00));
                        Blend(d + 00, Avx.LoadAlignedVector256((ushort*)(d + 00)), v1, ToScale(ToAlpha(v1), shf, inv));
                        var v2 = Avx.LoadAlignedVector256((ushort*)(s + 32));
                        Blend(d + 32, Avx.LoadAlignedVector256((ushort*)(d + 32)), v2, ToScale(ToAlpha(v2), shf, inv));
                        s += 64;
                    }
                    while ((d += 64) < e);
                }

                var repeatedBytes = r0.RepeatedBytes; r0 = ref Unsafe.Add(ref r0, 1);
                if (repeatedBytes != 0)
                {
                    var v = Avx.LoadAlignedVector256((ushort*)s);
                    e += repeatedBytes;

                    var alpha = ToAlpha(v);
                    if (alpha != Vector256<uint>.Zero)
                    {
                        if (IsEqual(alpha, opa))
                        {
                            do
                            {
                                Avx.StoreAligned((ushort*)(d + 00), v);
                                Avx.StoreAligned((ushort*)(d + 32), v);
                            }
                            while ((d += 64) < e);
                        }
                        else
                        {
                            var scale = ToScale(alpha, shf, inv);
                            do
                            {
                                Blend(d + 00, Avx.LoadAlignedVector256((ushort*)(d + 00)), v, scale);
                                Blend(d + 32, Avx.LoadAlignedVector256((ushort*)(d + 32)), v, scale);
                            }
                            while ((d += 64) < e);
                        }
                    }
                }
            }
        }
    }

    internal static unsafe class BlendArm64
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BitmapCopy(byte* s0, byte* d0, int len) =>
            NativeGraphic.bitmap_copy(s0, d0, len);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int BlendMakeRegion(byte* s0, byte* d0, int len, CacheRegion[] buk) =>
            NativeGraphic.blend32_make_region(s0, d0, len, buk);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BlendWithRegion(byte* s0, byte* d0, CacheRegion[] buk, int cnt) =>
            NativeGraphic.blend32_with_region(s0, d0, buk, cnt);
    }
}