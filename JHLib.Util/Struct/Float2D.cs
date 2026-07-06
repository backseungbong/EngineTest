using JHLib.Util.ArrayControl;
using JHLib.Util.ByteControl;
using JHLib.Util.List;
using JHLib.Util.Simd;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public struct Float2D : IEquatable<Float2D>
    {
        public readonly static Float2D Zero = new(0, 0);
        public readonly static Float2D NaN = new(float.NaN, float.NaN);
        public readonly static Float2D[] EmptyArray = [];

        public const int SIZE = 8;
        public float X;
        public float Y;
        public readonly bool IsNaN => float.IsNaN(X + Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D(float x, float y) { X = x; Y = y; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D(double x, double y) { X = (float)x; Y = (float)y; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D(ulong bit64) { this = Unsafe.As<ulong, Float2D>(ref bit64); }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D operator +(in Float2D a, in Float2D b) => new(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D operator -(in Float2D a, in Float2D b) => new(a.X - b.X, a.Y - b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D operator *(in Float2D a, float m) => new(a.X * m, a.Y * m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D operator /(in Float2D a, float d) => new(a.X / d, a.Y / d);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(in Float2D f2) => Unsafe.As<Float2D, Vector2>(ref Unsafe.AsRef(in f2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Float2D(in Vector2 f2) => Unsafe.As<Vector2, Float2D>(ref Unsafe.AsRef(in f2));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Float2D a, in Float2D b) =>
            Unsafe.As<Float2D, ulong>(ref Unsafe.AsRef(in a)) == Unsafe.As<Float2D, ulong>(ref Unsafe.AsRef(in b));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Float2D a, in Float2D b) =>
            Unsafe.As<Float2D, ulong>(ref Unsafe.AsRef(in a)) != Unsafe.As<Float2D, ulong>(ref Unsafe.AsRef(in b));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Float2D Swap() => new(Y, X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Float2D Lerp(in Float2D to, float t) => new(X * (1 - t) + to.X * t, Y * (1 - t) + to.Y * t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float LerpX(in Float2D to, float targetY) => (targetY - Y) / (to.Y - Y) * (to.X - X) + X;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float LerpY(in Float2D to, float targetX) => (targetX - X) / (to.X - X) * (to.Y - Y) + Y;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Float2D Normalize()
        {
            var x = X;
            var y = Y;
            var l = MathF.Sqrt(x * x + y * y);
            return new(x / l, y / l);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Double2D ToDouble2D() => SIMD.TryToDouble2D(in this);
        public readonly bool Equals(Float2D point) => this == point;
        public override readonly bool Equals(object obj) => obj is Float2D point && Equals(point);
        public override readonly int GetHashCode() => HashCode.Combine(X, Y);
        public override readonly string ToString() => $"({X}, {Y})";


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToBit64(float x, float y) => ToBit64(new Float2D(x, y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToBit64(in Float2D point) => Unsafe.As<Float2D, ulong>(ref Unsafe.AsRef(in point));


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe Float2D TextToPoint(ReadOnlySpan<char> text, char seperator, bool swapXY = false)
        {
            if (text.Length != 0)
            {
                var f2 = new Float2D();
                var f0 = (float*)&f2;

                fixed (char* text0 = text)
                {
                    var n = 0;
                    var p = text0;
                    var e = p + text.Length;
                    do
                    {
                        var t = FastASCII.FindChar0(p, e, seperator);
                        if (t > p && float.TryParse(MemoryMarshal.CreateReadOnlySpan(ref *p, (int)(t - p)), out var v))
                            f0[n++] = v;
                        p = t + 1;
                    }
                    while (p < e && n < 2);
                }
                return swapXY ? f2.Swap() : f2;
            }
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe void TextToPoints(ReadOnlySpan<char> text, char seperator, SList<Float2D> result, bool swapXY = false)
        {
            if (text.Length != 0)
            {
                var f2 = new Float2D();
                var f0 = (float*)&f2;

                fixed (char* text0 = text)
                {
                    var n = 0;
                    var p = text0;
                    var e = p + text.Length;
                    do
                    {
                        var t = FastASCII.FindChar0(p, e, seperator);
                        if (t > p && float.TryParse(MemoryMarshal.CreateReadOnlySpan(ref *p, (int)(t - p)), out var v))
                        {
                            f0[n & 1] = v;
                            if ((++n & 1) == 0)
                                result.Add(swapXY ? f2.Swap() : f2);
                        }
                        p = t + 1;
                    }
                    while (p < e);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(ref Float2D source, ref Float2D dest, int count, bool reverseCopy)
        {
            if (count > 0)
            {
                if (reverseCopy)
                    CopyReverse(ref source, ref dest, (uint)count);
                else
                    AC.Copy(ref source, ref dest, count);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CopyReverse(ref Float2D s0, ref Float2D d0, uint c)
        {
            if (c > 2)
            {
                if (c > 4)
                {
                    ref var d = ref d0;
                    ref var e = ref Unsafe.Add(ref d0, c - 4);
                    ref var s = ref Unsafe.Add(ref s0, c);
                    do
                    {
                        s = ref Unsafe.Subtract(ref s, 4);
                        Unsafe.Add(ref d, 0) = Unsafe.Add(ref s, 3);
                        Unsafe.Add(ref d, 1) = Unsafe.Add(ref s, 2);
                        Unsafe.Add(ref d, 2) = Unsafe.Add(ref s, 1);
                        Unsafe.Add(ref d, 3) = Unsafe.Add(ref s, 0);
                        d = ref Unsafe.Add(ref d, 4);
                    }
                    while (Unsafe.IsAddressLessThan(ref d, ref e));

                    Unsafe.Add(ref e, 0) = Unsafe.Add(ref s0, 3);
                    Unsafe.Add(ref e, 1) = Unsafe.Add(ref s0, 2);
                    Unsafe.Add(ref e, 2) = Unsafe.Add(ref s0, 1);
                    Unsafe.Add(ref e, 3) = Unsafe.Add(ref s0, 0);
                    return;
                }
                Unsafe.Add(ref d0, 1) = Unsafe.Add(ref s0, c - 2);
                Unsafe.Add(ref d0, 2) = Unsafe.Add(ref s0, c - 3);
            }
            Unsafe.Add(ref d0, 0) = Unsafe.Add(ref s0, c - 1);
            Unsafe.Add(ref d0, c - 1) = Unsafe.Add(ref s0, 0);
        }
    }
}