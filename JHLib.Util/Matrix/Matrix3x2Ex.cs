using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Matrix
{
    public static class Matrix3x2Ex
    {
        private const float DEG2RAD = MathF.PI / 180.0f;
        private const ulong IDENTITY_M11 = 0x000000003F800000; // 1.0f, 0.0f
        private const ulong IDENTITY_M21 = 0x3F80000000000000; // 0.0f, 1.0f


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(float tx, float ty) => CreateT(tx, ty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(Float2D translation) => CreateT(ref Unsafe.As<Float2D, ulong>(ref translation));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x2 Create(Vector2 translation) => CreateT(ref Unsafe.As<Vector2, ulong>(ref translation));


        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static Matrix3x2 Create(
        //    float centerX, float centerY, float rotation, float scale, float translationX, float translationY) =>
        //    BitConverter.SingleToUInt32Bits(rotation) != 0 ?
        //    CreateTRST(centerX, centerY, rotation, scale, translationX, translationY) :
        //    CreateTST(centerX, centerY, scale, translationX, translationY);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Matrix3x2 CreateInline(
        //    float centerX, float centerY, float rotation, float scale, float translationX, float translationY) =>
        //    BitConverter.SingleToUInt32Bits(rotation) != 0 ?
        //    CreateTRST(centerX, centerY, rotation, scale, translationX, translationY) :
        //    CreateTST(centerX, centerY, scale, translationX, translationY);

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static Matrix3x2 Create(
        //    float centerX, float centerY, float rotation, float scaleX, float scaleY, float translationX, float translationY) =>
        //    BitConverter.SingleToUInt32Bits(rotation) != 0 ?
        //    CreateTRST(centerX, centerY, rotation, scaleX, scaleY, translationX, translationY) :
        //    CreateTST(centerX, centerY, scaleX, scaleY, translationX, translationY);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Matrix3x2 CreateInline(
        //    float centerX, float centerY, float rotation, float scaleX, float scaleY, float translationX, float translationY) =>
        //    BitConverter.SingleToUInt32Bits(rotation) != 0 ?
        //    CreateTRST(centerX, centerY, rotation, scaleX, scaleY, translationX, translationY) :
        //    CreateTST(centerX, centerY, scaleX, scaleY, translationX, translationY);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Matrix3x2 CreateT(float tx, float ty)
        {
            Unsafe.SkipInit(out Matrix3x2 mtx);
            Unsafe.As<float, ulong>(ref mtx.M11) = IDENTITY_M11;
            Unsafe.As<float, ulong>(ref mtx.M21) = IDENTITY_M21;
            mtx.M31 = tx; mtx.M32 = ty;
            return mtx;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Matrix3x2 CreateT(ref ulong vector2)
        {
            Unsafe.SkipInit(out Matrix3x2 mtx);
            Unsafe.As<float, ulong>(ref mtx.M11) = IDENTITY_M11;
            Unsafe.As<float, ulong>(ref mtx.M21) = IDENTITY_M21;
            Unsafe.As<float, ulong>(ref mtx.M31) = vector2;
            return mtx;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateInline(float cx, float cy, float rot, float scale, float tx, float ty, ref Matrix3x2 mtx)
        {
            var m1122 = scale;
            var m1221 = 0f;

            if (BitConverter.SingleToUInt32Bits(rot) != 0)
            {
                var (sin, cos) = MathF.SinCos(rot * DEG2RAD);
                m1122 = cos * scale;
                m1221 = sin * scale;
            }

            mtx.M11 = m1122;
            mtx.M12 = -m1221;
            mtx.M21 = m1221;
            mtx.M22 = m1122;
            mtx.M31 = tx - (cx * m1122 - cy * m1221);
            mtx.M32 = ty - (cx * m1221 + cy * m1122);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateInline2(float cx, float cy, float rot, float sx, float sy, float tx, float ty, ref Matrix3x2 mtx)
        {
            var (sin, cos) = BitConverter.SingleToUInt32Bits(rot) != 0 ? MathF.SinCos(rot * DEG2RAD) : (0, 1);

            var m11 = cos * sx;
            var m12 = sin * sy;
            var m21 = sin * sx;
            var m22 = cos * sy;

            mtx.M11 = m11;
            mtx.M12 = -m12;
            mtx.M21 = m21;
            mtx.M22 = m22;
            mtx.M31 = tx - (cx * m11 - cy * m12);
            mtx.M32 = ty - (cx * m21 + cy * m22);
        }
    }
}