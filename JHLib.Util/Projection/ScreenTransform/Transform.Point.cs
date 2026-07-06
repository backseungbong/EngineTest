using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    public partial class Transform
    {
        /// <summary> [화면좌표] Point와 화면영역의 교차 판단 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointContainScreen(in Float2D p) => _localBound.IsContain(p);

        /// <summary> [화면좌표] Point와 화면영역의 교차 판단 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointContainScreen(float sx, float sy) => _localBound.IsContain(sx, sy);

        /// <summary> [화면좌표] Point와 화면영역의 교차 판단 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointContainMarginScreen(in Float2D p, float marginPercent) => _localBound.IsMarginContain(p, marginPercent);

        /// <summary> [월드좌표] Point와 화면영역의 교차 판단 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointContainWorld(in Float2D p) => _worldBound.IsContain(p);

        /// <summary> [월드좌표] Point와 화면영역의 교차 판단 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointContainWorld(float wx, float wy) => _worldBound.IsContain(wx, wy);

        /// <summary> [WGS84좌표] Point와 화면영역의 교차 판단 </summary> 
        public bool PointContainWGS84(in Float2D w84) => _wgs84Bound.IsContain(w84);

        /// <summary> [WGS84좌표] Point와 화면영역의 교차 판단 </summary> 
        public bool PointContainWGS84(float lon, float lat) => _wgs84Bound.IsContain(lon, lat);


        /// <summary> [월드좌표] Point를 화면좌표로 변환하여 화면영역과의 교차 판단 및 변환좌표 출력 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointContainWorld(in Float2D wp, out Float2D sp)
        {
            sp = WorldToScreen(wp);
            return PointContainScreen(sp);
        }

        /// <summary> [월드좌표] Point를 화면좌표로 변환하여 화면영역과의 교차 판단 및 변환좌표 출력 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointContainWorld(float wx, float wy, out float sx, out float sy)
        {
            var sp = WorldToScreen(wx, wy); sx = sp.X; sy = sp.Y;
            return PointContainScreen(sp);
        }

        /// <summary> [WGS84좌표] Point를 화면좌표로 변환하여 화면영역과의 교차 판단 및 변환좌표 출력 </summary> 
        public bool PointContainWGS84(in Float2D w84, out Float2D sp)
        {
            sp = WGS84ToScreen(w84);
            return PointContainScreen(sp);
        }

        /// <summary> [WGS84좌표] Point를 화면좌표로 변환하여 화면영역과의 교차 판단 및 변환좌표 출력 </summary> 
        public bool PointContainWGS84(float lon, float lat, out float sx, out float sy)
        {
            var sp = WGS84ToScreen(lon, lat); sx = sp.X; sy = sp.Y;
            return PointContainScreen(sp);
        }

        /// <summary> [WGS84좌표] Point를 화면좌표로 변환하여 Margin(%)가 적용된 화면영역과의 교차 판단 및 변환좌표 출력 (bsb) </summary> 
        public bool PointContainMarginScreenWGS84(in Float2D w84, float margin = 10f)
        {
            var sp = WGS84ToScreen(w84);
            return PointContainMarginScreen(sp, margin);
        }

        /// <summary> [월드좌표] Point를 화면좌표로 변환하여 Margin(%)가 적용된 화면영역과의 교차 판단 및 변환좌표 출력 (bsb) </summary> 
        public bool PointContainMarginScreenWorld(in Float2D wp, float marginPercent = 10f)
        {
            var sp = WorldToScreen(wp);
            return PointContainMarginScreen(sp, marginPercent);
        }
    }
}