using JHLib.Util.ArrayControl;
using JHLib.Util.ByteControl;
using JHLib.Util.List;
using JHLib.Util.Simd;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct Double2D(double x, double y) : IEquatable<Double2D>
    {
        public readonly static Double2D Zero = new(0, 0);
        public readonly static Double2D NaN = new(double.NaN, double.NaN);

        public double X = x;
        public double Y = y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2D operator +(in Double2D a, in Double2D b) => new(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2D operator -(in Double2D a, in Double2D b) => new(a.X - b.X, a.Y - b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2D operator *(in Double2D a, double m) => new(a.X * m, a.Y * m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2D operator /(in Double2D a, double d) => new(a.X / d, a.Y / d);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Double2D a, in Double2D b) => a.X == b.X && a.Y == b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Double2D a, in Double2D b) => a.X != b.X || a.Y != b.Y;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Double2D Swap() => new(Y, X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Double2D Lerp(in Double2D to, double t) => new(X * (1 - t) + to.X * t, Y * (1 - t) + to.Y * t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Double2D Normalize()
        {
            var x = X;
            var y = Y;
            var l = Math.Sqrt(x * x + y * y);
            return new(x / l, y / l);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Float2D ToFloat2D(bool trySimd = true) => trySimd ?
            SIMD.TryToFloat2D(in this) : new Float2D((float)X, (float)Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Int2D ToInt2D(bool trySimd = true) => trySimd ?
            SIMD.TryToInt2D(in this) : new Int2D((int)X, (int)Y);

        public readonly bool Equals(Double2D point) => X == point.X && Y == point.Y;
        public override readonly bool Equals(object obj) => obj is Double2D point && Equals(point);
        public override readonly int GetHashCode() => HashCode.Combine(X, Y);

        public static unsafe Double2D TextToPoint(ReadOnlySpan<char> text, char seperator)
        {
            var point = new Double2D();
            if (text.Length != 0)
            {
                var r = (double*)&point;
                var n = 0;
                fixed (char* text0 = text)
                {
                    var p = text0;
                    var e = p + text.Length;
                    do
                    {
                        var t = FastASCII.FindChar0(p, e, seperator);
                        if (t > p && double.TryParse(MemoryMarshal.CreateReadOnlySpan(ref *p, (int)(t - p)), out var v))
                            r[n++] = v;
                        p = t + 1;
                    }
                    while (p < e && n < 2);
                }
            }
            return point;
        }

        public static unsafe Double2D[] TextToPoints(ReadOnlySpan<char> text, char seperator)
        {
            if (text.Length != 0)
            {
                var approxCapacity = text.Length / 7; // 숫자당 7자 정도 예상
                if (approxCapacity < 2) approxCapacity = 2;
                else if (approxCapacity > 256) approxCapacity = 256;

                var r = new FList<double>(approxCapacity);
                fixed (char* text0 = text)
                {
                    var p = text0;
                    var e = p + text.Length;
                    do
                    {
                        var t = FastASCII.FindChar0(p, e, seperator);
                        if (t > p && double.TryParse(MemoryMarshal.CreateReadOnlySpan(ref *p, (int)(t - p)), out var v))
                            r.Add(v);
                        p = t + 1;
                    }
                    while (p < e);
                }

                if (r.Count >= 2)
                {
                    var points = new Double2D[r.Count / 2];
                    AC.Copy(
                        ref Unsafe.As<double, Double2D>(ref r.Ref0),
                        ref MemoryMarshal.GetArrayDataReference(points), points.Length);
                    return points;
                }
            }
            return null;
        }
    }
}