using JHLib.Util.ArrayControl;
using JHLib.Util.Struct;
using System.Buffers;
using System.Runtime.InteropServices;

namespace JHLib.Util.Projection
{
    public static class GreatCircle
    {
        private const double Rad2Deg = 180 / Math.PI;
        private const double Deg2Rad = Math.PI / 180;
        private const double Deg2RadHalf = Deg2Rad * 0.5d;

        /// <summary> 시작점에서 목표점까지의 초기 방위각(deg, 정북 기준 시계방향)을 반환한다 </summary>
        /// <returns>방위각 (정북베이스, 시계방향)</returns>
        public static double Bearing(double originLon, double originLat, double targetLon, double targetLat)
        {
            double lat1R = originLat * Deg2Rad;
            double lat2R = targetLat * Deg2Rad;
            double lonDR = (targetLon - originLon) * Deg2Rad;

            (double sinLat1, double cosLat1) = Math.SinCos(lat1R);
            (double sinLat2, double cosLat2) = Math.SinCos(lat2R);
            (double sinLonD, double cosLonD) = Math.SinCos(lonDR);

            double y = sinLonD * cosLat2;
            double x = cosLat1 * sinLat2 - sinLat1 * cosLat2 * cosLonD;

            return (Math.Atan2(y, x) * Rad2Deg + 360.0) % 360.0;
        }

        /// <summary> 두 점의 대권거리(구면 코사인 방식)를 계산한다 </summary>
        /// <returns>거리 (미터, meter)</returns>
        public static double Distance(double originLon, double originLat, double targetLon, double targetLat)
        {
            double lat1R = originLat * Deg2Rad;
            double lat2R = targetLat * Deg2Rad;
            double lonDR = (targetLon - originLon) * Deg2Rad;

            (double sinLat1, double cosLat1) = Math.SinCos(lat1R);
            (double sinLat2, double cosLat2) = Math.SinCos(lat2R);
            var cosLonD = Math.Cos(lonDR);

            double z = sinLat1 * sinLat2 + cosLat1 * cosLat2 * cosLonD;
            if (z < -1.0) { z = -1.0; }
            else if (z > 1.0) { z = 1.0; }

            return Math.Acos(z) * Earth.EARTH_RAD;
        }

        /// <summary>
        /// 두 점 사이의 대권거리(하버사인 방식)를 반환한다 <br/>
        /// 작은 거리,극지방에서도 높은 수치 안정성 (성능은 약 10-20% 정도 Distance보다 느림)
        /// </summary>
        /// <returns>거리 (미터, meter)</returns>
        public static double DistanceHaversine(double originLon, double originLat, double targetLon, double targetLat)
        {
            double lat1R = originLat * Deg2Rad;
            double lat2R = targetLat * Deg2Rad;
            double latDRH = (targetLat - originLat) * Deg2RadHalf;
            double lonDRH = (targetLon - originLon) * Deg2RadHalf;

            double cosLat1 = Math.Cos(lat1R);
            double cosLat2 = Math.Cos(lat2R);
            double sinLatD = Math.Sin(latDRH);
            double sinLonD = Math.Sin(lonDRH);

            double z = sinLatD * sinLatD + cosLat1 * cosLat2 * sinLonD * sinLonD;
            if (z < 0.0) { z = 0.0; }
            else if (z > 1.0) { z = 1.0; }

            return Math.Atan2(Math.Sqrt(z), Math.Sqrt(1.0 - z)) * Earth.EARTH_RADx2;
        }

        /// <summary> 두 점의 방위각 및 대권거리(구면 코사인 방식) 계산한다 </summary>
        /// <param name="originLon">시작위치 Lon</param>
        /// <param name="originLat">시작위치 Lat</param>
        /// <param name="targetLon">목표위치 Lon</param>
        /// <param name="targetLat">목표위치 Lat</param>
        /// <param name="bearing">방위각 (정북베이스, 시계방향)</param>
        /// <param name="distance">거리 (미터, meter)</param>
        public static void BearingDistance(double originLon, double originLat, double targetLon, double targetLat, out double bearing, out double distance)
        {
            double lat1R = originLat * Deg2Rad;
            double lat2R = targetLat * Deg2Rad;
            double lonDR = (targetLon - originLon) * Deg2Rad;

            (double sinLat1, double cosLat1) = Math.SinCos(lat1R);
            (double sinLat2, double cosLat2) = Math.SinCos(lat2R);
            (double sinLonD, double cosLonD) = Math.SinCos(lonDR);

            double y = sinLonD * cosLat2;
            double x = cosLat1 * sinLat2 - sinLat1 * cosLat2 * cosLonD;
            double z = sinLat1 * sinLat2 + cosLat1 * cosLat2 * cosLonD;
            if (z < -1.0) { z = -1.0; }
            else if (z > 1.0) { z = 1.0; }

            bearing = (Math.Atan2(y, x) * Rad2Deg + 360.0) % 360.0;
            distance = Math.Acos(z) * Earth.EARTH_RAD;
        }

        /// <summary> 시작점, 초기 방위각, 거리로부터 도착점 좌표를 계산한다 </summary>
        /// <param name="lon">시작위치 Lon</param>
        /// <param name="lat">시작위치 Lat</param>
        /// <param name="brg">방위각 (정북베이스, 시계방향)</param>
        /// <param name="dis">거리 (미터, meter)</param>
        /// <param name="newLon">도착 위치 Lon</param>
        /// <param name="newLat">도착 위치 Lat</param>
        public static void Destination(double lon, double lat, double brg, double dis, out double newLon, out double newLat)
        {
            double latR = lat * Deg2Rad;
            double lonR = lon * Deg2Rad;
            double brgR = brg * Deg2Rad;
            double sigma = dis * Earth.EARTH_RADINV;

            (double sinLat, double cosLat) = Math.SinCos(latR);
            (double sinBrg, double cosBrg) = Math.SinCos(brgR);
            (double sinSig, double cosSig) = Math.SinCos(sigma);

            double z = sinLat * cosSig + cosLat * sinSig * cosBrg;
            if (z < -1.0) { z = -1.0; }
            else if (z > 1.0) { z = 1.0; }

            double y = sinBrg * sinSig * cosLat;
            double x = cosSig - sinLat * z;

            newLon = (Math.Atan2(y, x) + lonR) * Rad2Deg;
            newLat = Math.Asin(z) * Rad2Deg;
        }

        /// <summary> 
        /// wgs84 좌표배열의 통해 총 거리를 구한다 <br/>
        /// GPS 오차로 인해 지그재그 경로가 생성되면 실제 거리보다 10-15% 정도 거리가 늘어나는 경향이 있으므로 <br/>
        /// EMA (Exponential Moving Average) 필터 스무딩 및 잡음 제거를 통해 거리 계산을 수행한다<br/>
        /// 만약 좌표가 정확하고 잡음이 없다면 alpha=1.0, eps=0.0으로 설정하고, <br/>
        /// GPS 오차를 가정한 기본값은 alpha=0.7, eps=5.0으로 설정되었다 <br/>
        /// </summary>
        /// <param name="w84path">WGS84 좌표 배열</param>
        /// <param name="alpha">EMA 계수 (0~1)</param>
        /// <param name="eps">잡음 제거 거리 (미터, meter)</param>
        /// <returns>총거리 (미터, meter)</returns>
        public static unsafe double Distance(Span<Float2D> w84path, double alpha = 0.7d, double eps = 5.0d)
        {
            int n = w84path.Length;
            if (n < 2) return 0.0;

            var total = 0d;
            var pathd = ArrayPool<Double2D>.Shared.Rent(n);
            Filtering.SmoothEMAZeroPhase(w84path, pathd, alpha);

            fixed (Double2D* path0 = &MemoryMarshal.GetArrayDataReference(pathd))
            {
                var t = path0;
                var p = path0 + 1;
                var e = path0 + n;
                do
                {
                    var lat1R = t->Y * Deg2Rad;
                    var lat2R = p->Y * Deg2Rad;
                    var latDRH = (p->Y - t->Y) * Deg2RadHalf;
                    var lonDRH = (p->X - t->X) * Deg2RadHalf;

                    var cosLat1 = Math.Cos(lat1R);
                    var cosLat2 = Math.Cos(lat2R);
                    var sinLatD = Math.Sin(latDRH);
                    var sinLonD = Math.Sin(lonDRH);

                    var z = sinLatD * sinLatD + cosLat1 * cosLat2 * sinLonD * sinLonD;
                    if (z < 0.0) { z = 0.0; }
                    else if (z > 1.0) { z = 1.0; }

                    var d = Math.Atan2(Math.Sqrt(z), Math.Sqrt(1.0 - z)) * Earth.EARTH_RADx2;
                    if (d > eps) { total += d; t = p; }
                }
                while (++p < e);
            }

            ArrayPool<Double2D>.Shared.Return(pathd);
            return total;
        }

        /// <summary>
        /// 두 선박의 CPA를 반환 <br/>
        /// 이 계산법은 지구의 곡률을 무시하고 2D 평면으로 근사(approximation)하여 계산하는 방식 <br/>
        /// CPA는 보통 단거리 상황에서 필요하므로 2D 평면으로도 충분한 정확도를 생성 <br/>
        /// 다만 초기 상대위치를 GreatCircle기반으로 계산
        /// </summary>
        /// <returns>CPA(NM) TCPA(초)</returns>
        public static (double CPA, double TCPA) CPACalculate(
            double originLon, double originLat, double originSpeedKn, double originCourse,
            double targetLon, double targetLat, double targetSpeedKn, double targetCourse)
        {
            const double Epsilon = 1e-9; // 부동소수점 비교를 위한 작은 값
            const double KnotsToMetersPerSecond = 1852.0 / 3600.0;
            const double MeterToNM = 1 / 1852d;

            double originSpeedMs = originSpeedKn * KnotsToMetersPerSecond;
            double targetSpeedMs = targetSpeedKn * KnotsToMetersPerSecond;

            // 상대 위치 벡터 계산
            var (rEast, rNorth) = ToRelativeCartesian(originLon, originLat, targetLon, targetLat);

            // 각 선박의 속도를 동쪽(vEast), 북쪽(vNorth) 성분으로 분해
            var (voEast, voNorth) = ToVelocityCartesian(originSpeedMs, originCourse);
            var (vtEast, vtNorth) = ToVelocityCartesian(targetSpeedMs, targetCourse);

            // 상대 속도 벡터 계산
            double rvEast = vtEast - voEast;
            double rvNorth = vtNorth - voNorth;

            // 예외 처리: 평행 항해 (상대 속도가 0에 가까울 때) 
            double relativeSpeedSq = rvEast * rvEast + rvNorth * rvNorth;
            if (relativeSpeedSq > Epsilon)
            {
                // TCPA 계산 
                double dotProduct = rEast * rvEast + rNorth * rvNorth;
                double tcpaSeconds = -dotProduct / relativeSpeedSq;

                // CPA 계산
                double cpaEast = rEast + rvEast * tcpaSeconds;
                double cpaNorth = rNorth + rvNorth * tcpaSeconds;
                double cpaMeters = Math.Sqrt(cpaEast * cpaEast + cpaNorth * cpaNorth);
                return (cpaMeters * MeterToNM, tcpaSeconds);
            }
            else
            {
                double cpaMeters = Math.Sqrt(rEast * rEast + rNorth * rNorth);
                return (cpaMeters * MeterToNM, double.PositiveInfinity);
            }
        }

        /// <summary> 
        /// Cartesian 좌표계 계산을 위해 시작지점을 기준으로 목표지점까지의 상대위치를 반환 
        /// </summary>
        private static (double rEast, double rNorth) ToRelativeCartesian(double originLon, double originLat, double targetLon, double targetLat)
        {
            BearingDistance(originLon, originLat, targetLon, targetLat, out var bearing, out var distance);
            var (sin, cos) = Math.SinCos(bearing * Deg2Rad);
            return (distance * sin, distance * cos);
        }

        /// <summary> 
        /// Cartesian 좌표계 계산을 위해 항해학적 침로(0=북, 90=동)를 수학적 각도(0=동, 90=북)로 변환
        /// </summary>
        private static (double vEast, double vNorth) ToVelocityCartesian(double speed, double course)
        {
            var (sin, cos) = Math.SinCos(course * Deg2Rad);
            return (speed * sin, speed * cos);
        }
    }
}