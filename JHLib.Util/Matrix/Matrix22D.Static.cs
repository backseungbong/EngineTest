using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Matrix
{
    using D128 = Vector128<double>;
    using D256 = Vector256<double>;
    public partial struct Matrix22D
    {
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Matrix22D Create(double pivotX, double pivotY, double rotation, double scale, double destX, double destY)
        {
            double m11, m12, m21, m22;
            if (BitConverter.DoubleToUInt64Bits(rotation) == 0)
            {
                m12 = 0;
                m11 = scale;
                m21 = destX - pivotX * scale;
                m22 = destY - pivotY * scale;
            }
            else
            {
                var (s, c) = Math.SinCos(rotation * (Math.PI / 180));
                m12 = s * scale;
                m11 = c * scale;
                m21 = destX - pivotX * m11 + pivotY * m12;
                m22 = destY - pivotX * m12 - pivotY * m11;
            }
            return new Matrix22D(m11, m12, m21, m22);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Matrix22D Invert(in Matrix22D mtx)
        {
            var m11 = mtx.M11;
            var m12 = mtx.M12;
            var det = m11 * m11 + m12 * m12;

            if (Math.Abs(det) >= double.Epsilon)
            {
                var inv = 1 / det;
                var i12 = -m12;

                return new(m11 * inv, i12 * inv,
                    (i12 * mtx.M22 - mtx.M21 * m11) * inv,
                    (mtx.M21 * m12 - m11 * mtx.M22) * inv);
            }
            return CreateIdentity();
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToMatrix3x2(in Matrix22D mtx, out Matrix3x2 result, bool roundTranslation = false)
        {
            Unsafe.SkipInit(out result);

            result.M11 = (float)mtx.M11;
            result.M12 = (float)mtx.M12;
            result.M21 = -(float)mtx.M12;
            result.M22 = (float)mtx.M11;

            if (roundTranslation)
            {
                result.M31 = SIMD.TryToInt(mtx.M21);
                result.M32 = SIMD.TryToInt(mtx.M22);
            }
            else
            {
                result.M31 = (float)mtx.M21;
                result.M32 = (float)mtx.M22;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Matrix22D CreateIdentity()
        {
            const ulong ONE = 0x3FF0000000000000;

            var r = default(Matrix22D);
            Unsafe.As<Matrix22D, ulong>(ref r) = ONE;
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckIdentity(in Matrix22D mtx)
        {
            const ulong ONE = 0x3FF0000000000000;

            var r = false;
            if (BitConverter.DoubleToUInt64Bits(mtx.M11) == ONE)
            {
                r = (BitConverter.DoubleToUInt64Bits(mtx.M12) |
                     BitConverter.DoubleToUInt64Bits(mtx.M21) |
                     BitConverter.DoubleToUInt64Bits(mtx.M22)) == 0;
            }
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D128 ToDV(float x, float y) => SIMD.ConvertDouble128(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D128 ToDV(in Float2D p) => SIMD.ConvertDouble128(p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D128 ToDV(double x, double y) => SIMD.LoadDouble128(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D128 ToDV(in Double2D p) => SIMD.LoadDouble128(p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D ToF2(in D128 xy) => SIMD.ConvertFloat2D(xy);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Double2D ToD2(in D128 xy) => SIMD.ConvertDouble2D(xy);


        private interface IReader<T>
        {
            static abstract Double2D GetXY(in T src);
            static abstract D128 GetD128(in T src);
            static abstract D256 GetD256(in T src, nint idx = 0);
        }
        private struct ReaderI2 : IReader<Int2D>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Double2D GetXY(in Int2D src) => new(src.X, src.Y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 GetD128(in Int2D src) => SIMD.ConvertDouble128(src);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D256 GetD256(in Int2D src, nint idx = 0) => SIMD.ConvertDouble256(src, idx);
        }
        private struct ReaderF2 : IReader<Float2D>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // ref Read시 레지스터 재사용 의존성으로 성능저하 발생, 값 복사로 레지스터 재할당 유도
            public static Double2D GetXY(in Float2D src) { var p = src; return new(p.X, p.Y); }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 GetD128(in Float2D src) => SIMD.ConvertDouble128(src);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D256 GetD256(in Float2D src, nint idx = 0) => SIMD.ConvertDouble256(src, idx);
        }
        private struct ReaderD2 : IReader<Double2D>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Double2D GetXY(in Double2D src) => src;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 GetD128(in Double2D src) => SIMD.LoadDouble128(src);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D256 GetD256(in Double2D src, nint idx = 0) => SIMD.LoadDouble256(src, idx);
        }

        private interface IOperation
        {
            static abstract double M11(in Matrix22D mtx);
            static abstract double M12(in Matrix22D mtx);
            static abstract double M21(in Matrix22D mtx);
            static abstract double M22(in Matrix22D mtx);
            static abstract D128 V128M1(in Matrix22D mtx);
            static abstract D128 V128M2(in Matrix22D mtx);
            static abstract D128 V128M3(in Matrix22D mtx);
            static abstract D256 V256M1(in Matrix22D mtx);
            static abstract D256 V256M2(in Matrix22D mtx);
            static abstract D256 V256M3(in Matrix22D mtx);
            static abstract Double2D Execute(in Matrix22D mtx, double x, double y);
            static abstract Double2D Execute(double x, double y, double m11, double m12, double m21, double m22);
            static abstract D128 Execute(in Matrix22D mtx, in D128 xy);
            static abstract D128 Execute(in D128 xy, in D128 m1, in D128 m2, in D128 m3);
            static abstract D256 Execute(in D256 xy, in D256 m1, in D256 m2, in D256 m3);
        }

        private struct OpNormal : IOperation
        {
            public static double M11(in Matrix22D mtx) => mtx.M11;
            public static double M12(in Matrix22D mtx) => mtx.M12;
            public static double M21(in Matrix22D mtx) => mtx.M21;
            public static double M22(in Matrix22D mtx) => mtx.M22;
            public static D128 V128M1(in Matrix22D mtx) => mtx.V128M1();
            public static D128 V128M2(in Matrix22D mtx) => mtx.V128M2();
            public static D128 V128M3(in Matrix22D mtx) => mtx.V128M3();
            public static D256 V256M1(in Matrix22D mtx) => mtx.V256M1();
            public static D256 V256M2(in Matrix22D mtx) => mtx.V256M2();
            public static D256 V256M3(in Matrix22D mtx) => mtx.V256M3();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Double2D Execute(in Matrix22D mtx, double x, double y) => Scalar(mtx, x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Double2D Execute(double x, double y, double m11, double m12, double m21, double m22) => Scalar(x, y, m11, m12, m21, m22);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 Execute(in Matrix22D mtx, in D128 xy) => FMA(mtx, xy);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 Execute(in D128 xy, in D128 m1, in D128 m2, in D128 m3) => FMA(xy, m1, m2, m3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D256 Execute(in D256 xy, in D256 m1, in D256 m2, in D256 m3) => FMA(xy, m1, m2, m3);

        }

        private struct OpPreY : IOperation
        {
            public static double M11(in Matrix22D mtx) => mtx.M11;
            public static double M12(in Matrix22D mtx) => mtx.M12;
            public static double M21(in Matrix22D mtx) => mtx.M21;
            public static double M22(in Matrix22D mtx) => mtx.M22;
            public static D128 V128M1(in Matrix22D mtx) => mtx.V128M1();
            public static D128 V128M2(in Matrix22D mtx) => mtx.V128M2();
            public static D128 V128M3(in Matrix22D mtx) => mtx.V128M3();
            public static D256 V256M1(in Matrix22D mtx) => mtx.V256M1();
            public static D256 V256M2(in Matrix22D mtx) => mtx.V256M2();
            public static D256 V256M3(in Matrix22D mtx) => mtx.V256M3();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Double2D Execute(in Matrix22D mtx, double x, double y) => ScalarPreY(mtx, x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Double2D Execute(double x, double y, double m11, double m12, double m21, double m22) => ScalarPreY(x, y, m11, m12, m21, m22);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 Execute(in Matrix22D mtx, in D128 xy) => FMAPreY(mtx, xy);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 Execute(in D128 xy, in D128 m1, in D128 m2, in D128 m3) => FMAPreY(xy, m1, m2, m3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D256 Execute(in D256 xy, in D256 m1, in D256 m2, in D256 m3) => FMAPreY(xy, m1, m2, m3);
        }

        private struct OpPostY : IOperation
        {
            public static double M11(in Matrix22D mtx) => mtx.M11;
            public static double M12(in Matrix22D mtx) => -mtx.M12;
            public static double M21(in Matrix22D mtx) => mtx.M21;
            public static double M22(in Matrix22D mtx) => mtx.M22;
            public static D128 V128M1(in Matrix22D mtx) => mtx.V128M1();
            public static D128 V128M2(in Matrix22D mtx) => mtx.V128M2Rev();
            public static D128 V128M3(in Matrix22D mtx) => mtx.V128M3();
            public static D256 V256M1(in Matrix22D mtx) => mtx.V256M1();
            public static D256 V256M2(in Matrix22D mtx) => mtx.V256M2Rev();
            public static D256 V256M3(in Matrix22D mtx) => mtx.V256M3();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Double2D Execute(in Matrix22D mtx, double x, double y) => ScalarPostY(mtx, x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Double2D Execute(double x, double y, double m11, double m12, double m21, double m22) => ScalarPostY(x, y, m11, m12, m21, m22);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 Execute(in Matrix22D mtx, in D128 xy) => FMAPostY(mtx, xy);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D128 Execute(in D128 xy, in D128 m1, in D128 m2, in D128 m3) => FMAPostY(xy, m1, m2, m3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static D256 Execute(in D256 xy, in D256 m1, in D256 m2, in D256 m3) => FMAPostY(xy, m1, m2, m3);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Scalar<TOp>(in Matrix22D mtx, in FloatRect rect, out Float2Dx4 path4) where TOp : IOperation
        {
            var m11 = TOp.M11(mtx);
            var m12 = TOp.M12(mtx);
            var m21 = TOp.M21(mtx);
            var m22 = TOp.M22(mtx);

            var x1 = (double)rect.X1;
            var y1 = (double)rect.Y1;
            var x2 = (double)rect.X2;
            var y2 = (double)rect.Y2;

            path4.P1 = TOp.Execute(x1, y1, m11, m12, m21, m22).ToFloat2D(false);
            path4.P2 = TOp.Execute(x2, y1, m11, m12, m21, m22).ToFloat2D(false);
            path4.P3 = TOp.Execute(x2, y2, m11, m12, m21, m22).ToFloat2D(false);
            path4.P4 = TOp.Execute(x1, y2, m11, m12, m21, m22).ToFloat2D(false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FMA<TOp>(in Matrix22D mtx, in FloatRect rect, out Float2Dx4 path4) where TOp : IOperation
        {
            var m1 = TOp.V256M1(mtx);
            var m2 = TOp.V256M2(mtx);
            var m3 = TOp.V256M3(mtx);

            var v1 = SIMD.LoadFloat128(rect);
            var t1 = SIMD.ConvertFloat128(TOp.Execute(SIMD.ConvertDouble256(Sse.Shuffle(v1, v1, 0b_11_00_01_10)), m1, m2, m3));
            var t2 = SIMD.ConvertFloat128(TOp.Execute(SIMD.ConvertDouble256(v1), m1, m2, m3));

            path4.P4 = Unsafe.BitCast<double, Float2D>(t1.AsDouble()[1]);
            path4.P2 = Unsafe.BitCast<double, Float2D>(t1.AsDouble()[0]);
            path4.P3 = Unsafe.BitCast<double, Float2D>(t2.AsDouble()[1]);
            path4.P1 = Unsafe.BitCast<double, Float2D>(t2.AsDouble()[0]);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Scalar<TOp>(in Matrix22D mtx, in FloatRect rect, out Double2Dx4 path4) where TOp : IOperation
        {
            var m11 = TOp.M11(mtx);
            var m12 = TOp.M12(mtx);
            var m21 = TOp.M21(mtx);
            var m22 = TOp.M22(mtx);

            var x1 = (double)rect.X1;
            var y1 = (double)rect.Y1;
            var x2 = (double)rect.X2;
            var y2 = (double)rect.Y2;

            path4.P1 = TOp.Execute(x1, y1, m11, m12, m21, m22);
            path4.P2 = TOp.Execute(x2, y1, m11, m12, m21, m22);
            path4.P3 = TOp.Execute(x2, y2, m11, m12, m21, m22);
            path4.P4 = TOp.Execute(x1, y2, m11, m12, m21, m22);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FMA<TOp>(in Matrix22D mtx, in FloatRect rect, out Double2Dx4 path4) where TOp : IOperation
        {
            var m1 = TOp.V256M1(mtx);
            var m2 = TOp.V256M2(mtx);
            var m3 = TOp.V256M3(mtx);

            var v1 = SIMD.LoadFloat128(rect);
            var t1 = TOp.Execute(SIMD.ConvertDouble256(Sse.Shuffle(v1, v1, 0b_11_00_01_10)), m1, m2, m3);
            var t2 = TOp.Execute(SIMD.ConvertDouble256(v1), m1, m2, m3);

            path4.P4 = Unsafe.BitCast<D128, Double2D>(t1.GetUpper());
            path4.P2 = Unsafe.BitCast<D128, Double2D>(t1.GetLower());
            path4.P3 = Unsafe.BitCast<D128, Double2D>(t2.GetUpper());
            path4.P1 = Unsafe.BitCast<D128, Double2D>(t2.GetLower());
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Scalar<T, TReader, TOp>(in Matrix22D mtx, ref T src, ref Float2D dst, int len)
            where TReader : IReader<T>
            where TOp : IOperation
        {
            if (len <= 0) return;
            Scalar<T, TReader, TOp>(ref src, ref dst, len,
                TOp.M11(mtx), TOp.M12(mtx), TOp.M21(mtx), TOp.M22(mtx));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Scalar<T, TReader, TOp>(in Matrix22D mtx, ref T src, ref Float2D dst, int len, double scale)
            where TReader : IReader<T>
            where TOp : IOperation
        {
            if (len <= 0) return;
            Scalar<T, TReader, TOp>(ref src, ref dst, len,
                TOp.M11(mtx) * scale, TOp.M12(mtx) * scale, TOp.M21(mtx), TOp.M22(mtx));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Scalar<T, TReader, TOp>(ref T src, ref Float2D dst, int len, double m11, double m12, double m21, double m22)
            where TReader : IReader<T>
            where TOp : IOperation
        {
            ref var s = ref src;
            ref var d = ref dst;
            ref var e = ref Unsafe.Add(ref s, len);
            do
            {
                var p = TReader.GetXY(s);
                d = TOp.Execute(p.X, p.Y, m11, m12, m21, m22).ToFloat2D(false);
                d = ref Unsafe.Add(ref d, 1);
                s = ref Unsafe.Add(ref s, 1);
            }
            while (Unsafe.IsAddressLessThan(ref s, ref e));
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FMA<T, TReader, TOp>(in Matrix22D mtx, ref T src, ref Float2D dst, int len)
            where TReader : IReader<T>
            where TOp : IOperation
        {
            FMA<T, TReader, TOp>(ref src, ref dst, len,
                TOp.V256M1(mtx), TOp.V256M2(mtx), TOp.V256M3(mtx));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FMA<T, TReader, TOp>(in Matrix22D mtx, ref T src, ref Float2D dst, int len, double scale)
            where TReader : IReader<T>
            where TOp : IOperation
        {
            FMA<T, TReader, TOp>(ref src, ref dst, len,
                TOp.V256M1(mtx) * scale, TOp.V256M2(mtx) * scale, TOp.V256M3(mtx));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FMA<T, TReader, TOp>(ref T src, ref Float2D dst, int len, in D256 m1, in D256 m2, in D256 m3)
            where TReader : IReader<T>
            where TOp : IOperation
        {
            if (len == 1)
            {
                var v1 = TReader.GetD128(src);
                dst = SIMD.ConvertFloat2D(TOp.Execute(v1, m1.GetLower(), m2.GetLower(), m3.GetLower()));
            }
            else
            {
                var l = (nint)len;
                if (len > 4)
                {
                    ref var se = ref Unsafe.Add(ref src, l - 4);
                    ref var de = ref Unsafe.Add(ref dst, l - 4);
                    ref var s = ref src;
                    ref var d = ref dst;

                    if (len > 8)
                    {
                        ref var e8 = ref Unsafe.Subtract(ref se, 4);
                        do
                        {
                            var v1 = TReader.GetD256(s);
                            var v2 = TReader.GetD256(s, 2);
                            var t1 = SIMD.ConvertFloat128(TOp.Execute(v1, m1, m2, m3));
                            var t2 = SIMD.ConvertFloat128(TOp.Execute(v2, m1, m2, m3));
                            SIMD.Store(d, 0, t1);
                            SIMD.Store(d, 2, t2);
                            d = ref Unsafe.Add(ref d, 4);
                            s = ref Unsafe.Add(ref s, 4);
                        }
                        while (Unsafe.IsAddressLessThan(ref s, ref e8));
                    }

                    // 나머지 5~8개 보장 (멱등성이 아니므로 OverlapRead & OverlapWrite)
                    var v3 = TReader.GetD256(s);
                    var v4 = TReader.GetD256(s, 2);
                    var v5 = TReader.GetD256(se);
                    var v6 = TReader.GetD256(se, 2);
                    var t3 = SIMD.ConvertFloat128(TOp.Execute(v3, m1, m2, m3));
                    var t4 = SIMD.ConvertFloat128(TOp.Execute(v4, m1, m2, m3));
                    var t5 = SIMD.ConvertFloat128(TOp.Execute(v5, m1, m2, m3));
                    var t6 = SIMD.ConvertFloat128(TOp.Execute(v6, m1, m2, m3));
                    SIMD.Store(d, 0, t3);
                    SIMD.Store(d, 2, t4);
                    SIMD.Store(de, 0, t5);
                    SIMD.Store(de, 2, t6);
                }
                else if (len > 0)
                {
                    var v2 = TReader.GetD256(src, l - 2);
                    var v1 = TReader.GetD256(src);
                    var t1 = SIMD.ConvertFloat128(TOp.Execute(v1, m1, m2, m3));
                    var t2 = SIMD.ConvertFloat128(TOp.Execute(v2, m1, m2, m3));
                    SIMD.Store(dst, 0, t1);
                    SIMD.Store(dst, l - 2, t2);
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Double2D Scalar(in Matrix22D mtx, double x, double y)
        {
            var rx = x * mtx.M11 - (y * mtx.M12 - mtx.M21); // x,y 계산그룹 분리로 병렬처리(ILP) 유도
            var ry = x * mtx.M12 + (y * mtx.M11 + mtx.M22);
            return new Double2D(rx, ry);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Double2D Scalar(double x, double y, double m11, double m12, double m21, double m22)
        {
            var rx = x * m11 - (y * m12 - m21); // x,y 계산그룹 분리로 병렬처리(ILP) 유도
            var ry = x * m12 + (y * m11 + m22);
            return new Double2D(rx, ry);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Double2D ScalarPreY(in Matrix22D mtx, double x, double y)
        {
            var rx = x * mtx.M11 + (y * mtx.M12 + mtx.M21); // x,y 계산그룹 분리로 병렬처리(ILP) 유도
            var ry = x * mtx.M12 - (y * mtx.M11 - mtx.M22);
            return new Double2D(rx, ry);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Double2D ScalarPreY(double x, double y, double m11, double m12, double m21, double m22)
        {
            var rx = x * m11 + (y * m12 + m21); // x,y 계산그룹 분리로 병렬처리(ILP) 유도
            var ry = x * m12 - (y * m11 - m22);
            return new Double2D(rx, ry);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Double2D ScalarPostY(in Matrix22D mtx, double x, double y)
        {
            var m12r = -mtx.M12;
            var rx = x * mtx.M11 + (y * m12r + mtx.M21); // x,y 계산그룹 분리로 병렬처리(ILP) 유도
            var ry = x * m12r - (y * mtx.M11 + mtx.M22);
            return new Double2D(rx, ry);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Double2D ScalarPostY(double x, double y, double m11, double m12r, double m21, double m22)
        {
            var rx = x * m11 + (y * m12r + m21); // x,y 계산그룹 분리로 병렬처리(ILP) 유도
            var ry = x * m12r - (y * m11 + m22);
            return new Double2D(rx, ry);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D128 FMA(in Matrix22D mtx, in D128 xy) => FMA(xy, mtx.V128M1(), mtx.V128M2(), mtx.V128M3());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static D128 FMA(in D128 xy, in D128 m1, in D128 m2, in D128 m3)
        {
            var yx = Avx.Permute(xy, 0b_01);
            var r1 = Fma.MultiplyAddSubtract(yx, m2, m3); // X = x * M11 - (y * M12 - M21)
            var r2 = Fma.MultiplyAddSubtract(xy, m1, r1); // Y = y * M11 + (x * M12 + M22)
            return r2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D256 FMA(in D256 xy, in D256 m1, in D256 m2, in D256 m3)
        {
            var yx = Avx.Permute(xy, 0b_0101);
            var r1 = Fma.MultiplyAddSubtract(yx, m2, m3); // X = x * M11 - (y * M12 - M21)
            var r2 = Fma.MultiplyAddSubtract(xy, m1, r1); // Y = y * M11 + (x * M12 + M22)
            return r2;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D128 FMAPreY(in Matrix22D mtx, in D128 xy) => FMAPreY(xy, mtx.V128M1(), mtx.V128M2(), mtx.V128M3());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static D128 FMAPreY(in D128 xy, in D128 m1, in D128 m2, in D128 m3)
        {
            var yx = Avx.Permute(xy, 0b_01);
            var r1 = Fma.MultiplySubtractAdd(xy, m1, m3); // X = y * M12 + (x * M11 + M21)
            var r2 = Fma.MultiplySubtractAdd(yx, m2, r1); // Y = x * M12 - (y * M11 - M22)
            return r2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D256 FMAPreY(in D256 xy, in D256 m1, in D256 m2, in D256 m3)
        {
            var yx = Avx.Permute(xy, 0b_0101);
            var r1 = Fma.MultiplySubtractAdd(xy, m1, m3); // X = y * M12 + (x * M11 + M21)
            var r2 = Fma.MultiplySubtractAdd(yx, m2, r1); // Y = x * M12 - (y * M11 - M22)
            return r2;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D128 FMAPostY(in Matrix22D mtx, in D128 xy) => FMAPostY(xy, mtx.V128M1(), mtx.V128M2Rev(), mtx.V128M3());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static D128 FMAPostY(in D128 xy, in D128 m1, in D128 m2r, in D128 m3)
        {
            var yx = Avx.Permute(xy, 0b_01);
            var r1 = Fma.MultiplyAdd(xy, m1, m3);          // X = y *-M12 + (x * M11 + M21)
            var r2 = Fma.MultiplySubtractAdd(yx, m2r, r1); // Y = x *-M12 - (y * M11 + M22)
            return r2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static D256 FMAPostY(in D256 xy, in D256 m1, in D256 m2r, in D256 m3)
        {
            var yx = Avx.Permute(xy, 0b_0101);
            var r1 = Fma.MultiplyAdd(xy, m1, m3);          // X = y *-M12 + (x * M11 + M21)
            var r2 = Fma.MultiplySubtractAdd(yx, m2r, r1); // Y = x *-M12 - (y * M11 + M22)
            return r2;
        }
    }
}