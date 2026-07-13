using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection
{
    public class PolarNorthProjection : IMapProjection
    {
        // WGS84 기준 지구 반경 (미터)
        private const double EarthRadius = 6378137.0;
        private const double RadToDeg = 180.0 / Math.PI;
        private const double DegToRad = Math.PI / 180.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D ToWorldD(double lon, double lat)
        {
            double latRad = lat * DegToRad;
            double lonRad = lon * DegToRad;

            // 북극(PI/2)에서 현재 위도까지의 호의 길이(거리)를 미터 단위로 계산
            double radiusMeters = EarthRadius * ((Math.PI / 2.0) - latRad);

            // 월드 좌표계 중심(0,0)을 북극으로 두고, 미터 단위 X, Y 계산
            // 경도 0도가 -Y축(아래쪽)을 향하도록 설정 (일반적인 지도 방위 기준)
            double x = radiusMeters * Math.Sin(lonRad);
            double y = -radiusMeters * Math.Cos(lonRad);

            return new Double2D(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D ToWGS84D(double wx, double wy)
        {
            // 중심(북극)으로부터의 미터 단위 반경 계산
            double radiusMeters = Math.Sqrt(wx * wx + wy * wy);

            // 반경을 다시 위도 라디안으로 역산
            double latRad = (Math.PI / 2.0) - (radiusMeters / EarthRadius);

            // X, Y 좌표를 통해 경도 라디안 역산 (경도 0도가 -Y축 기준이므로 wx, -wy 전달)
            double lonRad = Math.Atan2(wx, -wy);

            double lat = latRad * RadToDeg;
            double lon = lonRad * RadToDeg;

            // 값 범위 정규화
            if (lat < -90.0) lat = -90.0;
            if (lat > 90.0) lat = 90.0;
            while (lon <= -180.0) lon += 360.0;
            while (lon > 180.0) lon -= 360.0;

            return new Double2D(lon, lat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D ToWorld(float lon, float lat)
        {
            var d = ToWorldD(lon, lat);
            return new Float2D((float)d.X, (float)d.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D ToWorld(in Float2D w84) => ToWorld(w84.X, w84.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D ToWorldD(in Double2D w84) => ToWorldD(w84.X, w84.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D ToWGS84(float wx, float wy)
        {
            var d = ToWGS84D(wx, wy);
            return new Float2D((float)d.X, (float)d.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D ToWGS84(in Float2D wp) => ToWGS84(wp.X, wp.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D ToWGS84D(in Double2D wp) => ToWGS84D(wp.X, wp.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleRect CalculateWGS84Bound(DoubleRect worldBound, Double2D[] worldVertices)
        {
            // 1. 북극점(0,0)이 현재 화면 바운드 안에 포함되어 있는지 검사
            bool containsNorthPole = worldBound.X1 <= 0 && 0 <= worldBound.X2 &&
                                     worldBound.Y1 <= 0 && 0 <= worldBound.Y2;

            // 2. 날짜변경선(경도 180도 / -180도) 횡단 여부 검사
            // 극방위 도법에서 날짜변경선은 월드 좌표계 기준 X=0, Y>0 인 선분입니다.
            // 화면 사각형이 X=0을 포함하고, Y축 양수 영역에 걸쳐있다면 날짜변경선을 넘은 것입니다.
            bool crossesDateLine = worldBound.X1 <= 0 && 0 <= worldBound.X2 &&
                                   worldBound.Y2 > 0;

            if (containsNorthPole || crossesDateLine)
            {
                // [해결 포인트] 북극점이 보이거나 날짜변경선을 화면이 가로지르는 경우
                // 공간 쿼리(DB 등)에서 데이터 누락이 발생하지 않도록 경도를 -180 ~ 180으로 강제 오픈합니다.
                double minLon = -180.0;
                double maxLon = 180.0;

                // 위도의 최대값은 북극점이 보이면 90도, 안 보이면 꼭지점 중 가장 높은 위도
                double maxLat = containsNorthPole ? 90.0 : double.MinValue;
                double minLat = 90.0;

                foreach (var vertex in worldVertices)
                {
                    var wgs = ToWGS84D(vertex.X, vertex.Y);
                    if (wgs.Y < minLat) minLat = wgs.Y;
                    if (!containsNorthPole && wgs.Y > maxLat) maxLat = wgs.Y;
                }

                return new DoubleRect(new Double2D(minLon, minLat), new Double2D(maxLon, maxLat));
            }
            else
            {
                // 북극점도 안 보이고 날짜변경선도 가로지르지 않는 일반 케이스
                double minLon = double.MaxValue, maxLon = double.MinValue;
                double minLat = double.MaxValue, maxLat = double.MinValue;

                foreach (var vertex in worldVertices)
                {
                    var wgs = ToWGS84D(vertex.X, vertex.Y);

                    if (wgs.X < minLon) minLon = wgs.X;
                    if (wgs.X > maxLon) maxLon = wgs.X;
                    if (wgs.Y < minLat) minLat = wgs.Y;
                    if (wgs.Y > maxLat) maxLat = wgs.Y;
                }

                return new DoubleRect(new Double2D(minLon, minLat), new Double2D(maxLon, maxLat));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D CheckProjectionRange(double wx, double wy)
        {
            // 북극 도법에서 지도를 무한히 벗어나는 것을 막으려면 지구 반지름의 일정 배율로 제한합니다.
            // 필요에 따라 적도까지만 제한하려면 반경을 EarthRadius * (Math.PI / 2.0) 로 클램핑.
            // 당장 제한이 필요 없다면 입력값을 그대로 반환해도 무방합니다.
            return new Double2D(wx, wy);
        }

        public bool SupportMultiTransform => false;
    }
}
