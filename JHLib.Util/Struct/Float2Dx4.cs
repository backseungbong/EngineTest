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
    public struct Float2Dx4
    {
        public Float2D P1;
        public Float2D P2;
        public Float2D P3;
        public Float2D P4;
        public readonly float CenterX => (P1.X + P2.X + P3.X + P4.X) * 0.25f;
        public readonly float CenterY => (P1.Y + P2.Y + P3.Y + P4.Y) * 0.25f;
        public readonly ref Float2D this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref Unsafe.As<Float2Dx4, Float2D>(ref Unsafe.AsRef(in this)), (uint)i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2Dx4(in Float2D p1, in Float2D p2, in Float2D p3, in Float2D p4)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2Dx4(in Double2D p1, in Double2D p2, in Double2D p3, in Double2D p4)
        {
            P1 = p1.ToFloat2D();
            P2 = p2.ToFloat2D();
            P3 = p3.ToFloat2D();
            P4 = p4.ToFloat2D();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FloatRect GetBound()
        {
            if (Sse2.IsSupported)
            {
                ref var src = ref Unsafe.As<Float2Dx4, Float2D>(ref Unsafe.AsRef(in this));
                var v1 = SIMD.LoadFloat128(src);
                var v2 = SIMD.LoadFloat128(src, 2);
                var min1 = Sse.Min(v1, v2);
                var max1 = Sse.Max(v1, v2);
                var min2 = Sse.Min(min1, Sse.Shuffle(min1, min1, 0b_01_00_11_10));
                var max2 = Sse.Max(max1, Sse.Shuffle(max1, max1, 0b_01_00_11_10));
                return Unsafe.BitCast<Vector128<float>, FloatRect>(Sse.MoveLowToHigh(min2, max2));
            }
            else
            {
                return GeometryHelper.GetBound(AsSpan());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<Float2D> AsSpan() =>
            MemoryMarshal.CreateSpan(ref Unsafe.As<Float2Dx4, Float2D>(ref Unsafe.AsRef(in this)), 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<Float2D>(in Float2Dx4 value) => value.AsSpan();
    }
}