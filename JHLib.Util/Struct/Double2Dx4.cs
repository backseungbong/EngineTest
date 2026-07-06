using JHLib.Util.Geometry;
using JHLib.Util.Simd;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Struct
{
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Sequential)]
    public struct Double2Dx4
    {
        public Double2D P1;
        public Double2D P2;
        public Double2D P3;
        public Double2D P4;
        public readonly double CenterX => (P1.X + P2.X + P3.X + P4.X) * 0.25d;
        public readonly double CenterY => (P1.Y + P2.Y + P3.Y + P4.Y) * 0.25d;
        public readonly ref Double2D this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref Unsafe.As<Double2Dx4, Double2D>(ref Unsafe.AsRef(in this)), (uint)i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2Dx4(in Float2D p1, in Float2D p2, in Float2D p3, in Float2D p4)
        {
            P1 = p1.ToDouble2D();
            P2 = p2.ToDouble2D();
            P3 = p3.ToDouble2D();
            P4 = p4.ToDouble2D();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2Dx4(in Double2D p1, in Double2D p2, in Double2D p3, in Double2D p4)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly DoubleRect GetBound()
        {
            if (Avx.IsSupported)
            {
                ref var src = ref Unsafe.As<Double2Dx4, Double2D>(ref Unsafe.AsRef(in this));
                var v1 = SIMD.LoadDouble256(src);
                var v2 = SIMD.LoadDouble256(src, 2);
                var min1 = Avx.Min(v1, v2);
                var max1 = Avx.Max(v1, v2);
                var min2 = Sse2.Min(min1.GetUpper(), min1.GetLower());
                var max2 = Sse2.Max(max1.GetUpper(), max1.GetLower());
                return Unsafe.BitCast<Vector256<double>, DoubleRect>(Vector256.Create(min2, max2));
            }
            else
            {
                return GeometryHelper.GetBound(AsSpan());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToFloat2Dx4(out Float2Dx4 result)
        {
            if (Avx.IsSupported)
            {
                ref var src = ref Unsafe.As<Double2Dx4, Double2D>(ref Unsafe.AsRef(in this));
                var v1 = SIMD.LoadDouble256(src);
                var v2 = SIMD.LoadDouble256(src, 2);
                var r1 = Avx.ConvertToVector128Single(v1);
                var r2 = Avx.ConvertToVector128Single(v2);
                result = Unsafe.BitCast<Vector256<float>, Float2Dx4>(Vector256.Create(r1, r2));
            }
            else
            {
                result.P1 = P1.ToFloat2D();
                result.P2 = P2.ToFloat2D();
                result.P3 = P3.ToFloat2D();
                result.P4 = P4.ToFloat2D();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<Double2D> AsSpan() =>
            MemoryMarshal.CreateSpan(ref Unsafe.As<Double2Dx4, Double2D>(ref Unsafe.AsRef(in this)), 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<Double2D>(in Double2Dx4 value) => value.AsSpan();
    }
}