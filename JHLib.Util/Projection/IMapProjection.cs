using JHLib.Util.Struct;

namespace JHLib.Util.Projection
{
    /// <summary>
    /// WGS84(경위도)와 평면 World(미터) 간의 좌표 변환 인터페이스
    /// </summary>
    public interface IMapProjection
    {
        Float2D ToWorld(in Float2D w84);
        Float2D ToWorld(float lon, float lat);
        Double2D ToWorldD(in Double2D w84);
        Double2D ToWorldD(double lon, double lat);

        Float2D ToWGS84(in Float2D wp);
        Float2D ToWGS84(float wx, float wy);
        Double2D ToWGS84D(in Double2D wp);
        Double2D ToWGS84D(double wx, double wy);

        DoubleRect CalculateWGS84Bound(DoubleRect worldBound, Double2D[] worldVertices);
        Double2D CheckProjectionRange(double wx, double wy);

        bool SupportMultiTransform { get; }
    }
}
