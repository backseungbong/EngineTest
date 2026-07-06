using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Struct
{
    public readonly struct DoubleRect
    {
        public readonly double X1;
        public readonly double Y1;
        public readonly double X2;
        public readonly double Y2;

        public readonly ref Double2D P1 => ref Unsafe.As<DoubleRect, Double2D>(ref Unsafe.AsRef(in this));
        public readonly ref Double2D P2 => ref Unsafe.Add(ref P1, 1);
        public readonly double DX => X2 - X1;
        public readonly double DY => Y2 - Y1;
        public readonly double CenterX => (X1 + X2) * 0.5d;
        public readonly double CenterY => (Y1 + Y2) * 0.5d;
        public readonly bool IsPoint => X1 == X2 && Y1 == Y2;
        public readonly bool IsZero => X1 == 0 && Y1 == 0 && X2 == 0 && Y2 == 0;        


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleRect(in Double2D p) : this(p.X, p.Y) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleRect(in Double2D p1, in Double2D p2) : this(p1.X, p1.Y, p2.X, p2.Y) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleRect(double x, double y) { X1 = x; Y1 = y; X2 = x; Y2 = y; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleRect(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe FloatRect ToFloatRect()
        {
            if (Avx2.IsSupported)
            {
                var v1 = Avx.LoadVector256((double*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
                var v2 = Avx.ConvertToVector128Single(v1);
                return Unsafe.As<Vector128<float>, FloatRect>(ref v2);
            }
            else
            {
                return new((float)X1, (float)Y1, (float)X2, (float)Y2);
            }
        }
    }
}