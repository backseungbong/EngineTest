using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection
{
    /// <summary>
    /// 기존 EPSG3857 정적 클래스를 래핑하여 IMapProjection 인터페이스를 구현한 메르카토르 투영 클래스
    /// </summary>
    public class MercatorProjection : IMapProjection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D ToWorld(in Float2D w84) => EPSG3857.ToWorld(w84);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D ToWorld(float lon, float lat) => EPSG3857.ToWorld(lon, lat);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D ToWorldD(in Double2D w84) => EPSG3857.ToWorldD(w84);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D ToWorldD(double lon, double lat) => EPSG3857.ToWorldD(lon, lat);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D ToWGS84(in Float2D wp) => EPSG3857.ToWGS84(wp);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D ToWGS84(float wx, float wy) => EPSG3857.ToWGS84(wx, wy);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D ToWGS84D(in Double2D wp) => EPSG3857.ToWGS84D(wp);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D ToWGS84D(double wx, double wy) => EPSG3857.ToWGS84D(wx, wy);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleRect CalculateWGS84Bound(DoubleRect worldBound, Double2D[] worldVertices)
        {
            // 메르카토르는 기존 EPSG3857 로직을 그대로 사용합니다.
            return new DoubleRect(EPSG3857.ToWGS84D(worldBound.P1), EPSG3857.ToWGS84D(worldBound.P2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Double2D CheckProjectionRange(double wx, double wy) => EPSG3857.CheckProjectionRange(wx, wy);

        public bool SupportMultiTransform => true;
    }
}
