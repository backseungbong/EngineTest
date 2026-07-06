using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Matrix
{
    /// <summary>    
    /// 고해상도 처리를 위한 64bit 2x2 행렬곱 처리 구조체 <br/>
    /// X,Y Scale 정비례 변환에서 최적의 성능을 위해 구현됨 <br/>
    /// SIMD(SSE,AVX,FMA)명령어 적극 활용 및 어셈블리 출력을 최적화함<br/>
    /// 제네릭기반으로 입력형식(Float2D, Double2D, Int2D)을 효율적으로 확장 및 분기 없는 고성능 코드로 유지
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly partial struct Matrix22D(double m11, double m12, double m21, double m22)
    {
        public readonly double M11 = m11;
        public readonly double M12 = m12;
        public readonly double M21 = m21;
        public readonly double M22 = m22;
        public bool IsIdentity => CheckIdentity(this);
        public Matrix22D Invert() => Invert(this);

        public Float2D Transform(in Float2D p) => Transform<OpNormal>(p);
        public Float2D Transform(in Double2D p) => Transform<OpNormal>(p);
        public Float2D Transform(float x, float y) => Transform<OpNormal>(x, y);
        public Float2D Transform(double x, double y) => Transform<OpNormal>(x, y);
        public Float2D TransformPreFlipY(in Float2D p) => Transform<OpPreY>(p);
        public Float2D TransformPreFlipY(in Double2D p) => Transform<OpPreY>(p);
        public Float2D TransformPreFlipY(float x, float y) => Transform<OpPreY>(x, y);
        public Float2D TransformPreFlipY(double x, double y) => Transform<OpPreY>(x, y);
        public Float2D TransformPostFlipY(in Float2D p) => Transform<OpPostY>(p);
        public Float2D TransformPostFlipY(in Double2D p) => Transform<OpPostY>(p);
        public Float2D TransformPostFlipY(float x, float y) => Transform<OpPostY>(x, y);
        public Float2D TransformPostFlipY(double x, double y) => Transform<OpPostY>(x, y);

        public Double2D Transform64(in Float2D p) => Transform64<OpNormal>(p);
        public Double2D Transform64(in Double2D p) => Transform64<OpNormal>(p);
        public Double2D Transform64(float x, float y) => Transform64<OpNormal>(x, y);
        public Double2D Transform64(double x, double y) => Transform64<OpNormal>(x, y);
        public Double2D Transform64PreFlipY(in Float2D p) => Transform64<OpPreY>(p);
        public Double2D Transform64PreFlipY(in Double2D p) => Transform64<OpPreY>(p);
        public Double2D Transform64PreFlipY(float x, float y) => Transform64<OpPreY>(x, y);
        public Double2D Transform64PreFlipY(double x, double y) => Transform64<OpPreY>(x, y);
        public Double2D Transform64PostFlipY(in Float2D p) => Transform64<OpPostY>(p);
        public Double2D Transform64PostFlipY(in Double2D p) => Transform64<OpPostY>(p);
        public Double2D Transform64PostFlipY(float x, float y) => Transform64<OpPostY>(x, y);
        public Double2D Transform64PostFlipY(double x, double y) => Transform64<OpPostY>(x, y);

        public void Transform(in FloatRect rect, out Float2Dx4 path4) => Transform<OpNormal>(rect, out path4);
        public void TransformPreFlipY(in FloatRect rect, out Float2Dx4 path4) => Transform<OpPreY>(rect, out path4);
        public void TransformPostFlipY(in FloatRect rect, out Float2Dx4 path4) => Transform<OpPostY>(rect, out path4);

        public void Transform64(in FloatRect rect, out Double2Dx4 path4) => Transform64<OpNormal>(rect, out path4);
        public void Transform64PreFlipY(in FloatRect rect, out Double2Dx4 path4) => Transform64<OpPreY>(rect, out path4);
        public void Transform64PostFlipY(in FloatRect rect, out Double2Dx4 path4) => Transform64<OpPostY>(rect, out path4);

        public void Transform(ref Float2D src, ref Float2D dst, int len) =>
            Transform<Float2D, ReaderF2, OpNormal>(ref src, ref dst, len);
        public void TransformPreFlipY(ref Float2D src, ref Float2D dst, int len) =>
            Transform<Float2D, ReaderF2, OpPreY>(ref src, ref dst, len);
        public void TransformPostFlipY(ref Float2D src, ref Float2D dst, int len) =>
            Transform<Float2D, ReaderF2, OpPostY>(ref src, ref dst, len);

        public void Transform(ref Double2D src, ref Float2D dst, int len) =>
            Transform<Double2D, ReaderD2, OpNormal>(ref src, ref dst, len);
        public void TransformPreFlipY(ref Double2D src, ref Float2D dst, int len) =>
            Transform<Double2D, ReaderD2, OpPreY>(ref src, ref dst, len);
        public void TransformPostFlipY(ref Double2D src, ref Float2D dst, int len) =>
            Transform<Double2D, ReaderD2, OpPostY>(ref src, ref dst, len);

        public void Transform(ref Int2D src, ref Float2D dst, int len, double scale) =>
            Transform<Int2D, ReaderI2, OpNormal>(ref src, ref dst, len, scale);
        public void TransformPreFlipY(ref Int2D src, ref Float2D dst, int len, double scale) =>
            Transform<Int2D, ReaderI2, OpPreY>(ref src, ref dst, len, scale);
        public void TransformPostFlipY(ref Int2D src, ref Float2D dst, int len, double scale) =>
            Transform<Int2D, ReaderI2, OpPostY>(ref src, ref dst, len, scale);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Float2D Transform<TOp>(in Float2D p) where TOp : IOperation => Fma.IsSupported ?
            ToF2(TOp.Execute(this, ToDV(p))) : TOp.Execute(this, p.X, p.Y).ToFloat2D(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Float2D Transform<TOp>(in Double2D p) where TOp : IOperation => Fma.IsSupported ?
            ToF2(TOp.Execute(this, ToDV(p))) : TOp.Execute(this, p.X, p.Y).ToFloat2D(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Float2D Transform<TOp>(float x, float y) where TOp : IOperation => Fma.IsSupported ?
            ToF2(TOp.Execute(this, ToDV(x, y))) : TOp.Execute(this, x, y).ToFloat2D(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Float2D Transform<TOp>(double x, double y) where TOp : IOperation => Fma.IsSupported ?
            ToF2(TOp.Execute(this, ToDV(x, y))) : TOp.Execute(this, x, y).ToFloat2D(false);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Double2D Transform64<TOp>(in Float2D p) where TOp : IOperation => Fma.IsSupported ?
            ToD2(TOp.Execute(this, ToDV(p))) : TOp.Execute(this, p.X, p.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Double2D Transform64<TOp>(in Double2D p) where TOp : IOperation => Fma.IsSupported ?
            ToD2(TOp.Execute(this, ToDV(p))) : TOp.Execute(this, p.X, p.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Double2D Transform64<TOp>(float x, float y) where TOp : IOperation => Fma.IsSupported ?
            ToD2(TOp.Execute(this, ToDV(x, y))) : TOp.Execute(this, x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Double2D Transform64<TOp>(double x, double y) where TOp : IOperation => Fma.IsSupported ?
            ToD2(TOp.Execute(this, ToDV(x, y))) : TOp.Execute(this, x, y);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Transform<TOp>(in FloatRect rect, out Float2Dx4 path4) where TOp : IOperation
        {
            if (Fma.IsSupported) FMA<TOp>(this, rect, out path4);
            else Scalar<TOp>(this, rect, out path4);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Transform64<TOp>(in FloatRect rect, out Double2Dx4 path4) where TOp : IOperation
        {
            if (Fma.IsSupported) FMA<TOp>(this, rect, out path4);
            else Scalar<TOp>(this, rect, out path4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Transform<T, TReader, TOp>(ref T src, ref Float2D dst, int len)
            where T : unmanaged
            where TReader : IReader<T>
            where TOp : IOperation
        {
            if (Fma.IsSupported) FMA<T, TReader, TOp>(this, ref src, ref dst, len);
            else Scalar<T, TReader, TOp>(this, ref src, ref dst, len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Transform<T, TReader, TOp>(ref T src, ref Float2D dst, int len, double scale)
            where T : unmanaged
            where TReader : IReader<T>
            where TOp : IOperation
        {
            if (Fma.IsSupported) FMA<T, TReader, TOp>(this, ref src, ref dst, len, scale);
            else Scalar<T, TReader, TOp>(this, ref src, ref dst, len, scale);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector128<double> V128M1() => Vector128.Create(M11);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector128<double> V128M2() => Vector128.Create(M12);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector128<double> V128M2Rev() => Sse2.Xor(Vector128.Create(M12), Vector128.Create(-0d));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector128<double> V128M3() =>
            Unsafe.As<double, Vector128<double>>(ref Unsafe.AsRef(in M21));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector256<double> V256M1() => Vector256.Create(M11);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector256<double> V256M2() => Vector256.Create(M12);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector256<double> V256M2Rev() => Avx.Xor(Vector256.Create(M12), Vector256.Create(-0d));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Vector256<double> V256M3() =>
            Avx.BroadcastVector128ToVector256((double*)Unsafe.AsPointer(ref Unsafe.AsRef(in M21)));
    }
}