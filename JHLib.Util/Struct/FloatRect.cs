using JHLib.Util.Simd;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
    public struct FloatRect
    {
        public const int SIZE = 16;
        public readonly static FloatRect NaN = new(float.NaN, float.NaN, float.NaN, float.NaN);

        public float X1;
        public float Y1;
        public float X2;
        public float Y2;
        public readonly ref Float2D P1 => ref Unsafe.As<FloatRect, Float2D>(ref Unsafe.AsRef(in this));
        public readonly ref Float2D P2 => ref Unsafe.Add(ref P1, 1);
        public readonly float DX => X2 - X1;
        public readonly float DY => Y2 - Y1;
        public readonly float CenterX => (X1 + X2) * 0.5f;
        public readonly float CenterY => (Y1 + Y2) * 0.5f;
        public readonly bool IsZero => CheckZero(in this);
        public readonly bool IsPoint => CheckPoint(in this);
        public readonly bool IsArea => CheckArea(in this);
        public readonly bool IsNaNExist
        {
            get
            {
                if (Sse.IsSupported)
                {
                    var v1 = SIMD.LoadFloat128(this);
                    return Sse.MoveMask(Sse.CompareUnordered(v1, v1)) != 0;
                }
                return float.IsNaN(X1 + Y1 + X2 + Y2);
            }
        }     

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatRect(in Float2D p) : this(p.X, p.Y) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatRect(in Float2D p1, in Float2D p2) : this(p1.X, p1.Y, p2.X, p2.Y) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatRect(float x, float y) { X1 = x; Y1 = y; X2 = x; Y2 = y; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatRect(float x1, float y1, float x2, float y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CombineOrSet(in FloatRect r)
        {
            if (r.IsZero == false)
            {
                if (IsZero == false)
                {
                    Combine(r);
                }
                else
                {
                    this = r;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Combine(in FloatRect r)
        {
            if (r.X1 < X1) X1 = r.X1;
            if (r.Y1 < Y1) Y1 = r.Y1;
            if (r.X2 > X2) X2 = r.X2;
            if (r.Y2 > Y2) Y2 = r.Y2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsIntersect(in FloatRect test)
        {
            if (Sse2.IsSupported)
                return IsIntersectSse2(this, test);

            var b = false;
            if (X1 < test.X2 && test.X1 < X2 && Y1 < test.Y2) b = test.Y1 < Y2;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsIntersect(float x1, float y1, float x2, float y2)
        {
            var b = false;
            if (X1 < x2 && x1 < X2 && Y1 < y2) b = y1 < Y2;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsIntersect(in Float2D p, float half) => IsIntersect(p.X, p.Y, half);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsIntersect(float x, float y, float half)
        {
            var b = false;
            if (X1 < (x + half) && (x - half) < X2 && Y1 < (y + half)) b = (y - half) < Y2;
            return b;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsContain(in FloatRect test)
        {
            if (Sse2.IsSupported)
                return IsContainSse2(this, test);

            var b = false;
            if (X1 <= test.X1 && test.X2 <= X2 && Y1 <= test.Y1) b = test.Y2 <= Y2;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsContain(float x1, float y1, float x2, float y2)
        {
            var b = false;
            if (X1 <= x1 && x2 <= X2 && Y1 <= y1) b = y2 <= Y2;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsContain(in Float2D p, float half) => IsContain(p.X, p.Y, half);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsContain(float x, float y, float half)
        {
            var b = false;
            if (X1 <= (x - half) && (x + half) <= X2 && Y1 <= (y - half)) b = (y + half) <= Y2;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsContain(in Float2D p)
        {
            if (Sse2.IsSupported)
                return IsContainSse2(this, p);

            var b = false;
            if (X1 <= p.X && p.X <= X2 && Y1 <= p.Y) b = p.Y <= Y2;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsContain(float x, float y)
        {
            var b = false;
            if (X1 <= x && x <= X2 && Y1 <= y) b = y <= Y2;
            return b;
        }

        // bsb Add 2026.06.16
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsMarginContain(in Float2D p, float marginPercent)
        {
            if(marginPercent <= 0f) return X1 <= p.X && p.X <= X2 && Y1 <= p.Y && p.Y <= Y2;

            float ratio = marginPercent / 100f;
            float marginX = (X2 - X1) * ratio;
            float marginY = (Y2 - Y1) * ratio;

            return (X1 + marginX) <= p.X && p.X <= (X2 - marginX) && (Y1 + marginY) <= p.Y && p.Y <= (Y2 - marginY);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsLeft(in Float2D p1, in Float2D p2) => IsLeft(p1.X, p1.Y, p2.X - p1.X, p2.Y - p1.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsLeft(float x, float y, float vx, float vy)
        {
            var b = false;
            if (vx * (Y1 - y) >= vy * (X1 - x) &&
                vx * (Y1 - y) >= vy * (X2 - x) &&
                vx * (Y2 - y) >= vy * (X2 - x)) b = vx * (Y2 - y) >= vy * (X1 - x);
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsRight(in Float2D p1, in Float2D p2) => IsRight(p1.X, p1.Y, p2.X - p1.X, p2.Y - p1.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsRight(float x, float y, float vx, float vy)
        {
            var b = false;
            if (vx * (Y1 - y) < vy * (X1 - x) &&
                vx * (Y1 - y) < vy * (X2 - x) &&
                vx * (Y2 - y) < vy * (X2 - x)) b = vx * (Y2 - y) < vy * (X1 - x);
            return b;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public readonly bool IsSimilarArea(in FloatRect other, float tolerancePercent = 1)
        {
            var r = 0f;
            if (tolerancePercent > 0)
            {
                if (tolerancePercent < 100)
                    r = tolerancePercent / 100;
                else
                    r = 1;
            }

            var dx1 = DX;
            var dx2 = other.DX;
            if (dx1 * r >= Math.Abs(dx1 - dx2))
            {
                var dy1 = DY;
                var dy2 = other.DY;
                if (dy1 * r >= Math.Abs(dy1 - dy2))
                {
                    return
                        (dx1 + dx2) * 0.5f * r >= Math.Abs(CenterX - other.CenterX) &&
                        (dy1 + dy2) * 0.5f * r >= Math.Abs(CenterY - other.CenterY);
                }
            }
            return false;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FloatExtents ToExtents() { ToExtents(out var result); return result; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToExtents(out FloatExtents extents)
        {
            if (Sse2.IsSupported)
            {
                var vRect1 = Sse.Multiply(SIMD.LoadFloat128(this), Vector128.Create(0.5f));
                var vRect2 = Sse.Shuffle(vRect1, vRect1, 0b_01_00_11_10);
                var vCenter = Sse.Add(vRect1, vRect2);
                var vHalfExtents = Sse.Subtract(vRect2, vRect1);
                extents.Center = SIMD.ConvertFloat2D(vCenter);
                extents.HalfExtents = SIMD.ConvertFloat2D(vHalfExtents);
            }
            else
            {
                var x1 = X1 * 0.5f;
                var y1 = Y1 * 0.5f;
                var x2 = X2 * 0.5f;
                var y2 = Y2 * 0.5f;
                extents.Center = new(x1 + x2, y1 + y2);
                extents.HalfExtents = new(x2 - x1, y2 - y1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Float2D[] ToPath()
        {
            var x1 = X1;
            var y1 = Y1;
            var x2 = X2;
            var y2 = Y2;
            return [new(x1, y1), new(x2, y1), new(x2, y2), new(x1, y2)];
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Float2Dx4 ToFloat2Dx4() { ToFloat2Dx4(out var f4); return f4; }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToFloat2Dx4(out Float2Dx4 f4)
        {
            var x1 = X1;
            var y1 = Y1;
            var x2 = X2;
            var y2 = Y2;
            f4 = new(new Float2D(x1, y1), new(x2, y1), new(x2, y2), new(x1, y2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckZero(in FloatRect r)
        {
            var b = false;
            ref var p = ref Unsafe.As<FloatRect, ulong>(ref Unsafe.AsRef(in r));
            if (p == 0) b = Unsafe.Add(ref p, 1) == 0;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckArea(in FloatRect r)
        {
            var b = false;
            if (r.X1 < r.X2) b = r.Y1 < r.Y2;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckPoint(in FloatRect r)
        {
            var b = false;
            if (BitConverter.SingleToUInt32Bits(r.X1) == BitConverter.SingleToUInt32Bits(r.X2))
                b = BitConverter.SingleToUInt32Bits(r.Y1) == BitConverter.SingleToUInt32Bits(r.Y2);
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIntersectSse2(in FloatRect main, in FloatRect test)
        {
            var v1 = SIMD.LoadFloat128(main);
            var v2 = SIMD.LoadFloat128(test); 
            var s1 = Sse.MoveLowToHigh(v1, v2);
            var s2 = Sse.MoveHighToLow(v1, v2);
            return Sse.MoveMask(Sse.CompareNotLessThan(s1, s2)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsContainSse2(in FloatRect main, in FloatRect test)
        {
            var v1 = SIMD.LoadFloat128(main);
            var v2 = SIMD.LoadFloat128(test);
            var s1 = Sse.Shuffle(v1, v2, 0b_11_10_01_00);
            var s2 = Sse.Shuffle(v2, v1, 0b_11_10_01_00);
            return Sse.MoveMask(Sse.CompareNotLessThanOrEqual(s1, s2)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsContainSse2(in FloatRect main, in Float2D point)
        {
            var v1 = SIMD.LoadFloat128(main);
            var v2 = SIMD.LoadFloat128Dup(point);
            var s1 = Sse.MoveLowToHigh(v1, v2);
            var s2 = Sse.MoveHighToLow(v1, v2);
            return Sse.MoveMask(Sse.CompareNotLessThanOrEqual(s1, s2)) == 0;
        }
    }
}