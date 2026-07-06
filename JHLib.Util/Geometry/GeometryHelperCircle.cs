using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        /// <summary> 원과 사각형의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CircleIntersect(in Float2D center, float radius, in FloatRect rect) =>
            CircleIntersect(center.X, center.Y, radius, rect);

        /// <summary> 원과 사각형의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CircleIntersect(float cx, float cy, float radius, in FloatRect rect)
        {
            var gr = false;
            var hw = (rect.X2 - rect.X1) * 0.5f;
            var dx = MathF.Abs(cx - (rect.X1 + hw)) - hw;
            if (dx < radius)
            {
                var hh = (rect.Y2 - rect.Y1) * 0.5f;
                var dy = MathF.Abs(cy - (rect.Y1 + hh)) - hh;
                if (dy < radius)
                {
                    if (dx < 0 || dy < 0 || dx * dx + dy * dy < radius * radius)
                        gr = true;
                }
            }
            return gr;
        }

        /// <summary> 원과 사각형의 교차관계를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation CircleRelation(in Float2D center, float radius, in FloatRect rect) =>
            CircleRelation(center.X, center.Y, radius, rect);

        /// <summary> 원과 사각형의 교차관계를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static GeoRelation CircleRelation(float cx, float cy, float radius, in FloatRect rect)
        {
            var hw = (rect.X2 - rect.X1) * 0.5f;
            var dx = MathF.Abs(cx - (rect.X1 + hw)) - hw;
            if (dx < radius)
            {
                var hh = (rect.Y2 - rect.Y1) * 0.5f;
                var dy = MathF.Abs(cy - (rect.Y1 + hh)) - hh;
                if (dy < radius)
                {
                    if (dx < 0 || dy < 0 || dx * dx + dy * dy < radius * radius)
                    {
                        var ri = -radius;
                        if (ri < dx || ri < dy)
                        {
                            dx += rect.X2 - rect.X1;
                            dy += rect.Y2 - rect.Y1;

                            if (dx * dx + dy * dy < radius * radius)
                                return GeoRelation.ContainedBy;

                            return GeoRelation.Overlap;
                        }
                        return GeoRelation.Contains;
                    }
                }
            }
            return GeoRelation.Disjoint;
        }
    }
}