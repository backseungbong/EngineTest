using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Simd
{
    public static unsafe class SIMD
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Load128Scalar<T>(T v) where T : INumber<T> => Vector128.CreateScalarUnsafe(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<TType> Load128<T, TType>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector128<TType>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<TType> Load256<T, TType>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector256<TType>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<TType> Load512<T, TType>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector512<TType>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> LoadInt128Scalar(in Int2D src, nint idx = 0) =>
            Vector128.CreateScalarUnsafe(Unsafe.As<Int2D, double>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx))).AsInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> LoadFloat128Scalar(in Float2D src, nint idx = 0) =>
            Vector128.CreateScalarUnsafe(Unsafe.As<Float2D, double>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx))).AsSingle();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> LoadFloat128Dup(in Float2D src, nint idx = 0) =>
            Vector128.Create(Unsafe.As<Float2D, double>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx))).AsSingle();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> LoadFloat256Dup(in Float2D src, nint idx = 0) =>
            Vector256.Create(Unsafe.As<Float2D, double>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx))).AsSingle();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> LoadFloat512Dup(in Float2D src, nint idx = 0) =>
            Vector512.Create(Unsafe.As<Float2D, double>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx))).AsSingle();



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> LoadInt128<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector128<int>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> LoadInt256<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector256<int>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<int> LoadInt512<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector512<int>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> LoadFloat128(float x, float y) =>
            Vector128.Create(x, y, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> LoadFloat128(float x1, float y1, float x2, float y2) =>
            Vector128.Create(x1, y1, x2, y2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> LoadFloat128<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector128<float>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> LoadFloat256<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector256<float>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<float> LoadFloat512<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector512<float>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> LoadDouble128(double x, double y) => Vector128.Create(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> LoadDouble128<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector128<double>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> LoadDouble256<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector256<double>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> LoadDouble512<T>(in T src, nint idx = 0) =>
            Unsafe.As<T, Vector512<double>>(ref Unsafe.Add(ref Unsafe.AsRef(in src), idx));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> ConvertDouble128(in Vector128<float> src) =>
            Sse2.ConvertToVector128Double(src);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> ConvertDouble128(float x, float y) =>
            Sse2.ConvertToVector128Double(LoadFloat128(x, y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> ConvertDouble128(in Int2D src, nint idx = 0) =>
            Sse2.ConvertToVector128Double(LoadInt128Scalar(Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> ConvertDouble128(in Float2D src, nint idx = 0) =>
            Sse2.ConvertToVector128Double(LoadFloat128Scalar(Unsafe.AsRef(in src), idx));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> ConvertDouble256(in Vector128<float> src) =>
            Avx.ConvertToVector256Double(src);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> ConvertDouble256(in Int2D src, nint idx = 0) =>
            Avx.ConvertToVector256Double(LoadInt128(Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<double> ConvertDouble256(in Float2D src, nint idx = 0) =>
            Avx.ConvertToVector256Double(LoadFloat128(Unsafe.AsRef(in src), idx));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> ConvertDouble512(in Vector256<float> src) =>
            Avx512F.ConvertToVector512Double(src);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> ConvertDouble512(in Int2D src, nint idx = 0) =>
            Avx512F.ConvertToVector512Double(LoadInt256(Unsafe.AsRef(in src), idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<double> ConvertDouble512(in Float2D src, nint idx = 0) =>
            Avx512F.ConvertToVector512Double(LoadFloat256(Unsafe.AsRef(in src), idx));



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> ConvertInt128(in Vector128<float> src) =>
            Sse2.ConvertToVector128Int32(src);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> ConvertInt128(in Vector128<double> src) =>
            Sse2.ConvertToVector128Int32(src);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> ConvertInt128(in Vector256<double> src) =>
            Avx.ConvertToVector128Int32(src);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<int> ConvertInt256(in Vector512<double> src) =>
            Avx512F.ConvertToVector256Int32(src);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D ConvertInt2D(in Vector128<int> src) =>
            Unsafe.BitCast<double, Int2D>(src.AsDouble().ToScalar());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D ConvertInt2D(in Vector128<float> src) =>
            ConvertInt2D(ConvertInt128(src));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D ConvertInt2D(in Vector128<double> src) =>
            ConvertInt2D(ConvertInt128(src));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D ConvertInt2D(double x, double y) =>
            ConvertInt2D(LoadDouble128(x, y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D ConvertInt2D(in Double2D src) =>
            ConvertInt2D(LoadDouble128(src));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> ConvertFloat128(in Vector128<double> src) =>
            Sse2.ConvertToVector128Single(src);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> ConvertFloat128(in Vector256<double> src) =>
            Avx.ConvertToVector128Single(src);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<float> ConvertFloat256(in Vector512<double> src) =>
            Avx512F.ConvertToVector256Single(src);



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D ConvertFloat2D(in Vector128<float> src) =>
            Unsafe.BitCast<double, Float2D>(src.AsDouble().ToScalar());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D ConvertFloat2D(in Vector128<double> src) =>
            ConvertFloat2D(ConvertFloat128(src));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D ConvertFloat2D(double x, double y) =>
            ConvertFloat2D(LoadDouble128(x, y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D ConvertFloat2D(in Double2D src) =>
            ConvertFloat2D(LoadDouble128(src));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2D ConvertDouble2D(in Vector128<double> src) =>
            Unsafe.As<Vector128<double>, Double2D>(ref Unsafe.AsRef(in src));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2D ConvertDouble2D(in Float2D src) =>
            ConvertDouble2D(ConvertDouble128(src));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConvertDoubleRect(in FloatRect src, out Vector256<double> drect)
        {
            var rect = ConvertDouble256(LoadFloat128(src));
            drect = rect;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreScalar<T1, T2>(in T1 dst, in Vector128<T2> value) =>
            Unsafe.As<T1, T2>(ref Unsafe.AsRef(in dst)) = value.ToScalar();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUpper<T1, T2>(in T1 dst, in Vector128<T2> value) =>
            Unsafe.As<T1, T2>(ref Unsafe.AsRef(in dst)) = value.GetUpper().ToScalar();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store<T1, T2>(in T1 dst, nint idx, in Vector128<T2> value) =>
            Unsafe.As<T1, Vector128<T2>>(ref Unsafe.Add(ref Unsafe.AsRef(in dst), idx)) = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store<T1, T2>(in T1 dst, nint idx, in Vector256<T2> value) =>
            Unsafe.As<T1, Vector256<T2>>(ref Unsafe.Add(ref Unsafe.AsRef(in dst), idx)) = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store<T1, T2>(in T1 dst, nint idx, in Vector512<T2> value) =>
            Unsafe.As<T1, Vector512<T2>>(ref Unsafe.Add(ref Unsafe.AsRef(in dst), idx)) = value;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(in Vector128<float> v1)
        {
            var vSub1 = Sse.Subtract(v1, v1); // Subtract 통해 Inf->NaN으로 유도

            // NaN인 경우 최상단 비트가 0 or 1이 가능하므로, 4바이트 마스크 검출이 안되는경우가 생김
            // NaN값은 최상단 비트에 이어진 8비트가 1로 채워지므로 byte단위 체크시 검출가능
            return Sse2.MoveMask(vSub1.AsByte()) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(in Vector128<float> v1, in Vector128<float> v2)
        {
            var vSub1 = Sse.Subtract(v1, v1); // Subtract 통해 Inf->NaN으로 유도
            var vSub2 = Sse.Subtract(v2, v2);

            // NaN인 경우 최상단 비트가 0 or 1이 가능하므로, 4바이트 마스크처리로 검출이 안되는경우가 생김
            // NaN값은 연속된 8비트가 1로 채워지므로 byte단위 체크시 검출가능
            return Sse2.MoveMask(Sse.Or(vSub1, vSub2).AsByte()) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIntRound(float v) => Sse.ConvertToInt32(Load128Scalar(v));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIntRound(double v) => Sse2.ConvertToInt32(Load128Scalar(v));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64Round(float v) => Sse.X64.ConvertToInt64(Load128Scalar(v));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64Round(double v) => Sse2.X64.ConvertToInt64(Load128Scalar(v));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(float v) => Sse.ConvertToInt32WithTruncation(Load128Scalar(v));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(double v) => Sse2.ConvertToInt32WithTruncation(Load128Scalar(v));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(float v) => Sse.X64.ConvertToInt64WithTruncation(Load128Scalar(v));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(double v) => Sse2.X64.ConvertToInt64WithTruncation(Load128Scalar(v));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryToIntRound(float v) => Sse.IsSupported ? ToIntRound(v) : (int)MathF.Round(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryToIntRound(double v) => Sse2.IsSupported ? ToIntRound(v) : (int)Math.Round(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long TryToInt64Round(float v) => Sse.X64.IsSupported ? ToInt64Round(v) : (long)MathF.Round(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long TryToInt64Round(double v) => Sse2.X64.IsSupported ? ToInt64Round(v) : (long)Math.Round(v);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryToInt(float v) => Sse.IsSupported ? ToInt(v) : (int)MathF.Truncate(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryToInt(double v) => Sse2.IsSupported ? ToInt(v) : (int)Math.Truncate(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long TryToInt64(float v) => Sse.X64.IsSupported ? ToInt64(v) : (long)MathF.Truncate(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long TryToInt64(double v) => Sse2.X64.IsSupported ? ToInt64(v) : (long)Math.Truncate(v);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Double2D TryToDouble2D(in Float2D f2) { TryConvert(f2, out Double2D d2); return d2; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D TryToFloat2D(in Double2D d2) { TryConvert(d2, out Float2D f2); return f2; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2D TryToInt2D(in Double2D d2) { TryConvert(d2, out Int2D i2); return i2; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryConvert(in Float2D f2, Double2D* d2) => TryConvert(f2, out *d2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryConvert(in Float2D f2, out Double2D d2)
        {
            if (Sse2.IsSupported) { d2 = ConvertDouble2D(f2); }
            else { d2.X = f2.X; d2.Y = f2.Y; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryConvert(in Double2D d2, Float2D* f2) => TryConvert(d2, out *f2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryConvert(in Double2D d2, out Float2D f2)
        {
            if (Sse2.IsSupported) { f2 = ConvertFloat2D(d2); }
            else { f2.X = (float)d2.X; f2.Y = (float)d2.Y; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryConvert(in Double2D d2, Int2D* i2) => TryConvert(d2, out *i2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryConvert(in Double2D d2, out Int2D i2)
        {
            if (Sse2.IsSupported) { i2 = ConvertInt2D(d2); }
            else { i2.X = (int)d2.X; i2.Y = (int)d2.Y; }
        }

        /// <summary> 두 값을 비교하여 작은 값을 반환 (NaN 존재시 두번재 값을 반환한다) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float a, float b) =>
            Sse.MinScalar(Load128Scalar(a), Load128Scalar(b)).ToScalar();

        /// <summary> 두 값을 비교하여 큰 값을 반환 (NaN 존재시 두번재 값을 반환한다) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float a, float b) =>
            Sse.MaxScalar(Load128Scalar(a), Load128Scalar(b)).ToScalar();

        /// <summary> 두 값을 비교하여 작은값, 큰값을 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MinMax(float a, float b, out float min, out float max)
        {
            var va = Load128Scalar(a);
            var vb = Load128Scalar(b);
            min = Sse.MinScalar(va, vb).ToScalar();
            max = Sse.MaxScalar(va, vb).ToScalar();
        }

        /// <summary> 값을 최소, 최대값 내로 제한 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float a, float min, float max) =>
            Sse.MaxScalar(Sse.MinScalar(Load128Scalar(a), Load128Scalar(max)), Load128Scalar(min)).ToScalar();


        /// <summary> 두 값을 비교하여 큰 값을 반환 (NaN 존재시 두번재 값을 반환한다) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Min(double a, double b) =>
            Sse2.MinScalar(Load128Scalar(a), Load128Scalar(b)).ToScalar();

        /// <summary> 두 값을 비교하여 큰 값을 반환 (NaN 존재시 두번재 값을 반환한다) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Max(double a, double b) =>
            Sse2.MaxScalar(Load128Scalar(a), Load128Scalar(b)).ToScalar();

        /// <summary> 두 값을 비교하여 작은값, 큰값을 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MinMax(double a, double b, out double min, out double max)
        {
            var va = Load128Scalar(a);
            var vb = Load128Scalar(b);
            min = Sse2.MinScalar(va, vb).ToScalar();
            max = Sse2.MaxScalar(va, vb).ToScalar();
        }

        /// <summary> 값을 최소, 최대값 내로 제한 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double a, double min, double max) =>
            Sse2.MaxScalar(Sse2.MinScalar(Load128Scalar(a), Load128Scalar(max)), Load128Scalar(min)).ToScalar();


        /// <summary> 크기순서가 없는 value1과 value2의 값이 min부터 max사이에 교차하는지 여부 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersect(float value1, float value2, float min, float max)
        {
            var v1 = Load128Scalar(value1);
            var v2 = Load128Scalar(value2);
            var c1 = Sse.CompareScalarLessThan(Sse.MinScalar(v1, v2), Load128Scalar(max)).AsUInt32().ToScalar();
            var c2 = Sse.CompareScalarLessThan(Load128Scalar(min), Sse.MaxScalar(v1, v2)).AsUInt32().ToScalar();
            return (c1 & c2) != 0;
        }

        /// <summary> 크기순서가 없는 value1과 value2의 값이 min부터 max사이에 포함하는지 여부 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contain(float value1, float value2, float min, float max)
        {
            var v1 = Load128Scalar(value1);
            var v2 = Load128Scalar(value2);
            var c1 = Sse.CompareScalarLessThanOrEqual(Load128Scalar(min), Sse.MinScalar(v1, v2)).AsUInt32().ToScalar();
            var c2 = Sse.CompareScalarLessThanOrEqual(Sse.MaxScalar(v1, v2), Load128Scalar(max)).AsUInt32().ToScalar();
            return (c1 & c2) != 0;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AlignCheck(void* ptr) => AlignCheck((nint)ptr);

        /// <summary> SIMD처리에 적합한 포인터 주소인지 체크 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AlignCheck(nint ptr)
        {
            if (Sse2.IsSupported)
                if (Avx2.IsSupported)
                    if (Avx512F.IsSupported) return (ptr & 63) == 0;
                    else return (ptr & 31) == 0;
                else return (ptr & 15) == 0;
            else return false;
        }

        /// <summary> SIMD처리에 적합한 길이인지 체크 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AlignCheck(uint len) => AlignCheck((int)len);

        /// <summary> SIMD처리에 적합한 길이인지 체크 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AlignCheck(int len)
        {
            if (Sse2.IsSupported)
                if (Avx2.IsSupported)
                    if (Avx512F.IsSupported) return (len & 63) == 0;
                    else return (len & 31) == 0;
                else return (len & 15) == 0;
            else return false;
        }

        /// <summary> SIMD처리에 적합한 같거나 큰 길이로 변환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignFit(int len)
        {
            if (Sse2.IsSupported)
                if (Avx2.IsSupported)
                    if (Avx512F.IsSupported) return len + 63 & ~63;
                    else return len + 31 & ~31;
                else return len + 15 & ~15;
            else return len;
        }
    }
}