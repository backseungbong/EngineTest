using JHLib.Util.Helper;
using JHLib.Util.Matrix;
using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Projection
{
    /// <summary> WGS84 Pseudo-Mercator </summary>
    public static class EPSG3857
    {
        private const double R2D = 180 / Math.PI;
        private const double D2R = Math.PI / 180;

        private const double EARTH_RAD = Earth.EARTH_RAD;
        private const double EARTH_RADINV = Earth.EARTH_RADINV;

        private const double D2R_DIV2 = D2R / 2;
        private const double PI_DIV2 = Math.PI / 2;
        private const double PI_DIV4 = Math.PI / 4;

        private const double LON2PROJ = D2R * EARTH_RAD;
        private const double PROJ2LON = R2D * EARTH_RADINV;

        public const double MIN_LON = -180.0d;
        public const double MAX_LON = 180.0d;
        public const double MIN_LAT = -85.06d;
        public const double MAX_LAT = 85.06d;
        public const double MAX_LONDIS = MAX_LON - MIN_LON;
        public const double MAX_LATDIS = MAX_LAT - MIN_LAT;

        public const double MIN_PJX = -20037508.34d;
        public const double MAX_PJX = 20037508.34d;
        public const double MIN_PJY = -20048966.10d;
        public const double MAX_PJY = 20048966.10d;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ToPJX(double lon) =>
            LON2PROJ * lon;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ToPJY(double lat) =>
            Math.Log(Math.Tan(lat * D2R_DIV2 + PI_DIV4)) * EARTH_RAD;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ToLon(double x) =>
            PROJ2LON * x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ToLat(double y) =>
            (Math.Atan(Math.Exp(y * EARTH_RADINV)) * 2 - PI_DIV2) * R2D;


        public static Double2D CheckProjectionRange(in Double2D wp) => CheckProjectionRange(wp.X, wp.Y);
        public static Double2D CheckProjectionRange(double wx, double wy)
        {
            var rx = MathHelper.Cycle(wx, MIN_PJX, MAX_PJX, 0);
            var ry = MathHelper.Cycle(wy, MIN_PJY, MAX_PJY, 0);
            return new Double2D(rx, ry);
        }

        /// <summary> [월드좌표 기반] <para/>        
        /// 특정 위치에서의 Great Circle기반의 1000미터 거리의 두점과, 그 두 점을 투영한 후의 거리를 비율로 반환한다<para/>
        /// 고위도 지역에서 Mercator투영은 더 많은 왜곡이 발생하므로, 이를 보정하기 위해 쓰일 수 있는 비율이다 <para/>
        /// 결과값은 최소 1 이상이며, 1에 가까워질수록 적도, 커질수록 극지역에 가까워진다. 대한민국은 1.2정도의 비율을 가진다<para/>
        /// 투영된 월드좌표에서의 거리에 이 비율을 곱하면, Greate Circle기반 거리와 비슷한 값이 나온다<para/>
        /// 정확한 용도로 사용하는 목적이 아닌 대략적인 용도로 사용하기위한 비율이다
        /// </summary> 
        /// <param name="pjy">투영된 Y 위치</param>
        /// <returns> 1이상의 값, (투영된 거리 / 1000) </returns>
        public static double RatioProjWorld(double pjy) => RatioProjWGS84(ToLat(pjy));

        /// <summary> [WGS84좌표 기반] <para/>        
        /// 특정 위치에서의 Great Circle기반의 1000미터 거리의 두점과, 그 두 점을 투영한 후의 거리를 비율로 반환한다<para/>
        /// 고위도 지역에서 Mercator투영은 더 많은 왜곡이 발생하므로, 이를 보정하기 위해 쓰일 수 있는 비율이다 <para/>
        /// 결과값은 최소 1 이상이며, 1에 가까워질수록 적도, 커질수록 극지역에 가까워진다. 대한민국은 1.2정도의 비율을 가진다<para/>
        /// 투영된 월드좌표에서의 거리에 이 비율을 곱하면, Greate Circle기반 거리와 비슷한 값이 나온다<para/>
        /// 정확한 용도로 사용하는 목적이 아닌 대략적인 용도로 사용하기위한 비율이다
        /// </summary> 
        /// <param name="lat">위도</param>
        /// <returns> 1이상의 값, (투영된 거리 / 1000) </returns>
        public static double RatioProjWGS84(double lat)
        {
            var projMin = ToPJY(lat - (Earth.LAT_1KM / 2));
            var projMax = ToPJY(lat + (Earth.LAT_1KM / 2));
            var projDis = projMax - projMin;
            if (projDis > 1000) return projDis / 1000;
            return 1;
        }


        /// <summary> WGS84 -> World 변환 </summary>    
        public static Float2D ToWorld(in Float2D lonlat) => ToWorld(lonlat.X, lonlat.Y);
        public static Float2D ToWorld(in Double2D lonlat) => ToWorld(lonlat.X, lonlat.Y);
        public static Float2D ToWorld(float lon, float lat) => ToWorld((double)lon, (double)lat);
        public static Float2D ToWorld(double lon, double lat) => new(ToPJX(lon), ToPJY(lat));
        public static Double2D ToWorldD(in Float2D lonlat) => ToWorldD(lonlat.X, lonlat.Y);
        public static Double2D ToWorldD(in Double2D lonlat) => ToWorldD(lonlat.X, lonlat.Y);
        public static Double2D ToWorldD(float lon, float lat) => ToWorldD((double)lon, (double)lat);
        public static Double2D ToWorldD(double lon, double lat) => new(ToPJX(lon), ToPJY(lat));

        /// <summary> WGS84 -> World 변환 (자체 Array에 변환된 좌표 저장) </summary>     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToWorld(Span<Float2D> src)
        {
            if (src.Length <= 0) return;
            ref var s = ref MemoryMarshal.GetReference(src);
            ToWorldCore(ref s, ref s, src.Length);
        }

        /// <summary> WGS84 -> World 변환 (다른 Array에 변환좌표 저장) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToWorld(Span<Float2D> src, Span<Float2D> dst)
        {
            if (src.Length <= 0) return;
            if (src.Length <= dst.Length)
            {
                ref var s = ref MemoryMarshal.GetReference(src);
                ref var d = ref MemoryMarshal.GetReference(dst);
                ToWorldCore(ref s, ref d, src.Length);
            }
            else ThrowOverRange();
        }

        /// <summary> WGS84 -> World 변환 </summary>    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToWorld(ref Float2D src, ref Float2D dst, int len)
        {
            if (len <= 0) return;
            ToWorldCore(ref src, ref dst, len);
        }


        /// <summary> World -> WGS84 변환 </summary>   
        public static Float2D ToWGS84(in Float2D xy) => ToWGS84(xy.X, xy.Y);
        public static Float2D ToWGS84(in Double2D xy) => ToWGS84(xy.X, xy.Y);
        public static Float2D ToWGS84(float x, float y) => ToWGS84((double)x, (double)y);
        public static Float2D ToWGS84(double x, double y) => new(ToLon(x), ToLat(y));
        public static Double2D ToWGS84D(in Float2D xy) => ToWGS84D(xy.X, xy.Y);
        public static Double2D ToWGS84D(in Double2D xy) => ToWGS84D(xy.X, xy.Y);
        public static Double2D ToWGS84D(float x, float y) => ToWGS84D((double)x, (double)y);
        public static Double2D ToWGS84D(double x, double y) => new(ToLon(x), ToLat(y));

        /// <summary> World -> WGS84 변환 (자체 Array에 변환된 좌표 저장) </summary>     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToWGS84(Span<Float2D> src)
        {
            if (src.Length <= 0) return;
            ref var s = ref MemoryMarshal.GetReference(src);
            ToWGS84Core(ref s, ref s, src.Length);
        }

        /// <summary> World -> WGS84 변환 (다른 Array에 변환좌표 저장) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToWGS84(Span<Float2D> src, Span<Float2D> dst)
        {
            if (src.Length <= 0) return;
            if (src.Length <= dst.Length)
            {
                ref var s = ref MemoryMarshal.GetReference(src);
                ref var d = ref MemoryMarshal.GetReference(dst);
                ToWGS84Core(ref s, ref d, src.Length);
            }
            else ThrowOverRange();
        }

        /// <summary> World -> WGS84 변환 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToWGS84(ref Float2D src, ref Float2D dst, int len)
        {
            if (len <= 0) return;
            ToWGS84Core(ref src, ref dst, len);
        }




        public static Float2D WGS84ToScreen(in Matrix22D mtx, in Float2D w84) => mtx.TransformPreFlipY(ToWorldD(w84));
        public static Float2D WGS84ToScreen(in Matrix22D mtx, in Double2D w84) => mtx.TransformPreFlipY(ToWorldD(w84));
        public static Float2D WGS84ToScreen(in Matrix22D mtx, float lon, float lat) => mtx.TransformPreFlipY(ToWorldD(lon, lat));
        public static Float2D WGS84ToScreen(in Matrix22D mtx, double lon, double lat) => mtx.TransformPreFlipY(ToWorldD(lon, lat));
        public static Double2D WGS84ToScreenD(in Matrix22D mtx, in Float2D w84) => mtx.Transform64PreFlipY(ToWorldD(w84));
        public static Double2D WGS84ToScreenD(in Matrix22D mtx, in Double2D w84) => mtx.Transform64PreFlipY(ToWorldD(w84));
        public static Double2D WGS84ToScreenD(in Matrix22D mtx, float lon, float lat) => mtx.Transform64PreFlipY(ToWorldD(lon, lat));
        public static Double2D WGS84ToScreenD(in Matrix22D mtx, double lon, double lat) => mtx.Transform64PreFlipY(ToWorldD(lon, lat));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WGS84ToScreen(in Matrix22D mtx, Span<Float2D> src)
        {
            if (src.Length <= 0) return;
            ref var s = ref MemoryMarshal.GetReference(src);
            WGS84ToScreenCore(mtx, ref s, ref s, src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WGS84ToScreen(in Matrix22D mtx, Span<Float2D> src, Span<Float2D> dst)
        {
            if (src.Length <= 0) return;
            if (src.Length <= dst.Length)
            {
                ref var s = ref MemoryMarshal.GetReference(src);
                ref var d = ref MemoryMarshal.GetReference(dst);
                WGS84ToScreenCore(mtx, ref s, ref d, src.Length);
            }
            else ThrowOverRange();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WGS84ToScreen(in Matrix22D mtx, ref Float2D src, ref Float2D dst, int len)
        {
            if (len <= 0) return;
            WGS84ToScreenCore(mtx, ref src, ref dst, len);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WGS84ToScreen(in Matrix22D mtx, in FloatRect w84rect, out Float2Dx4 path4)
        {
            var p1 = ToWorldD(w84rect.P1);
            var p2 = ToWorldD(w84rect.P2);
            path4.P1 = mtx.TransformPreFlipY(p1);
            path4.P2 = mtx.TransformPreFlipY(p2.X, p1.Y);
            path4.P3 = mtx.TransformPreFlipY(p2);
            path4.P4 = mtx.TransformPreFlipY(p1.X, p2.Y);
        }


        public static Float2D ScreenToWGS84(in Matrix22D mtx, in Float2D sp) => ToWGS84(mtx.Transform64PostFlipY(sp));
        public static Float2D ScreenToWGS84(in Matrix22D mtx, in Double2D sp) => ToWGS84(mtx.Transform64PostFlipY(sp));
        public static Float2D ScreenToWGS84(in Matrix22D mtx, float sx, float sy) => ToWGS84(mtx.Transform64PostFlipY(sx, sy));
        public static Float2D ScreenToWGS84(in Matrix22D mtx, double sx, double sy) => ToWGS84(mtx.Transform64PostFlipY(sx, sy));
        public static Double2D ScreenToWGS84D(in Matrix22D mtx, in Float2D sp) => ToWGS84D(mtx.Transform64PostFlipY(sp));
        public static Double2D ScreenToWGS84D(in Matrix22D mtx, in Double2D sp) => ToWGS84D(mtx.Transform64PostFlipY(sp));
        public static Double2D ScreenToWGS84D(in Matrix22D mtx, float sx, float sy) => ToWGS84D(mtx.Transform64PostFlipY(sx, sy));
        public static Double2D ScreenToWGS84D(in Matrix22D mtx, double sx, double sy) => ToWGS84D(mtx.Transform64PostFlipY(sx, sy));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenToWGS84(in Matrix22D mtx, Span<Float2D> src)
        {
            if (src.Length <= 0) return;
            ref var s = ref MemoryMarshal.GetReference(src);
            ScreenToWGS84Core(mtx, ref s, ref s, src.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenToWGS84(in Matrix22D mtx, Span<Float2D> src, Span<Float2D> dst)
        {
            if (src.Length <= 0) return;
            if (src.Length <= dst.Length)
            {
                ref var s = ref MemoryMarshal.GetReference(src);
                ref var d = ref MemoryMarshal.GetReference(dst);
                ScreenToWGS84Core(mtx, ref s, ref d, src.Length);
            }
            else ThrowOverRange();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenToWGS84(in Matrix22D mtx, ref Float2D src, ref Float2D dst, int len)
        {
            if (len <= 0) return;
            ScreenToWGS84Core(mtx, ref src, ref dst, len);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ScreenToWGS84(in Matrix22D mtx, in FloatRect srect, out Float2Dx4 path4)
        {
            FloatRect srect2;
            if (Sse.IsSupported)
            {
                var vr = SIMD.LoadFloat128(srect);
                var v2 = Sse.Shuffle(vr, vr, 0b_11_00_01_10);
                srect2 = Unsafe.As<Vector128<float>, FloatRect>(ref v2);
            }
            else
            {
                srect2 = new FloatRect(srect.X2, srect.Y1, srect.X1, srect.Y2);
            }

            var p1 = mtx.Transform64PostFlipY(srect.P1);
            var p2 = mtx.Transform64PostFlipY(srect2.P1);
            var p3 = mtx.Transform64PostFlipY(srect.P2);
            var p4 = mtx.Transform64PostFlipY(srect2.P2);
            path4.P1 = ToWGS84(p1);
            path4.P2 = ToWGS84(p2);
            path4.P3 = ToWGS84(p3);
            path4.P4 = ToWGS84(p4);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ToWorldCore(ref Float2D src, ref Float2D dst, int len)
        {
            ref var s = ref src;
            ref var d = ref dst;
            ref var e = ref Unsafe.Add(ref src, len);

            if (Sse2.IsSupported)
            {
                do
                {
                    var w84 = SIMD.ConvertDouble128(s);
                    var wy = ToPJY(w84[1]); // SIMD특성상 Upper->Lower 순으로 접근시 명령어 낭비 없이 깔끔하게 생성
                    var wx = ToPJX(w84[0]);
                    d = new(wx, wy);
                    d = ref Unsafe.Add(ref d, 1);
                    s = ref Unsafe.Add(ref s, 1);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
            }
            else
            {
                do
                {
                    var w84 = s; // ref Read시 레지스터 재사용 의존성으로 성능저하 발생, 값 복사로 레지스터 재할당 유도
                    d = ToWorld(w84);
                    d = ref Unsafe.Add(ref d, 1);
                    s = ref Unsafe.Add(ref s, 1);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ToWGS84Core(ref Float2D src, ref Float2D dst, int len)
        {
            ref var s = ref src;
            ref var d = ref dst;
            ref var e = ref Unsafe.Add(ref src, len);

            if (Sse2.IsSupported)
            {
                do
                {
                    var wp = SIMD.ConvertDouble128(s);
                    var lat = ToLat(wp[1]); // SIMD특성상 Upper->Lower 순으로 접근시 명령어 낭비 없이 깔끔하게 생성
                    var lon = ToLon(wp[0]);
                    d = new(lon, lat);
                    d = ref Unsafe.Add(ref d, 1);
                    s = ref Unsafe.Add(ref s, 1);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
            }
            else
            {
                do
                {
                    var wp = s; // ref Read시 레지스터 재사용 의존성으로 성능저하 발생, 값 복사로 레지스터 재할당 유도
                    d = ToWGS84(wp);
                    d = ref Unsafe.Add(ref d, 1);
                    s = ref Unsafe.Add(ref s, 1);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WGS84ToScreenCore(in Matrix22D mtx, ref Float2D src, ref Float2D dst, int len)
        {
            ref var s = ref src;
            ref var d = ref dst;
            ref var e = ref Unsafe.Add(ref src, len);

            if (Fma.IsSupported)
            {
                var m1 = mtx.V128M1();
                var m2 = mtx.V128M2();
                var m3 = mtx.V128M3();
                do
                {
                    var w84 = SIMD.ConvertDouble128(s);
                    var wy = ToPJY(w84[1]); // SIMD특성상 Upper->Lower 순으로 접근시 명령어 낭비 없이 깔끔하게 생성
                    var wx = ToPJX(w84[0]);
                    var sp = Matrix22D.FMAPreY(SIMD.LoadDouble128(wx, wy), m1, m2, m3);
                    d = SIMD.ConvertFloat2D(sp);
                    d = ref Unsafe.Add(ref d, 1);
                    s = ref Unsafe.Add(ref s, 1);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
            }
            else
            {
                var m11 = mtx.M11;
                var m12 = mtx.M12;
                var m21 = mtx.M21;
                var m22 = mtx.M22;
                do
                {
                    var w84 = s; // ref Read시 레지스터 재사용 의존성으로 성능저하 발생, 값 복사로 레지스터 재할당 유도
                    var wx = ToPJX(w84.X); // X값의 계산이 먼저 이루어져야 벤치마크 성능이 좀더 좋아짐
                    var wy = ToPJY(w84.Y);
                    d = Matrix22D.ScalarPreY(wx, wy, m11, m12, m21, m22).ToFloat2D(false);
                    d = ref Unsafe.Add(ref d, 1);
                    s = ref Unsafe.Add(ref s, 1);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ScreenToWGS84Core(in Matrix22D mtx, ref Float2D src, ref Float2D dst, int len)
        {
            ref var s = ref src;
            ref var d = ref dst;
            ref var e = ref Unsafe.Add(ref src, len);

            if (Fma.IsSupported)
            {
                var m1 = mtx.V128M1();
                var m2 = mtx.V128M2Rev();
                var m3 = mtx.V128M3();
                do
                {
                    var sp = SIMD.ConvertDouble128(s);
                    var wp = Matrix22D.FMAPostY(sp, m1, m2, m3);
                    var lat = ToLat(wp[1]); // SIMD특성상 Upper->Lower 순으로 접근시 명령어 낭비 없이 깔끔하게 생성
                    var lon = ToLon(wp[0]);
                    d = new(lon, lat);
                    d = ref Unsafe.Add(ref d, 1);
                    s = ref Unsafe.Add(ref s, 1);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
            }
            else
            {
                var m11 = mtx.M11;
                var m12 = -mtx.M12;
                var m21 = mtx.M21;
                var m22 = mtx.M22;
                do
                {
                    var sp = s; // ref Read시 레지스터 재사용 의존성으로 성능저하 발생, 값 복사로 레지스터 재할당 유도
                    var wp = Matrix22D.ScalarPostY(sp.X, sp.Y, m11, m12, m21, m22);
                    var lat = ToLat(wp.Y);
                    var lon = ToLon(wp.X);
                    d = new(lon, lat);
                    d = ref Unsafe.Add(ref d, 1);
                    s = ref Unsafe.Add(ref s, 1);
                }
                while (Unsafe.IsAddressLessThan(ref s, ref e));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOverRange() =>
            throw new Exception("변환좌표가 저장될 배열의 길이는 원본 배열의 길이보다 크거나 같아야 합니다");
    }
}