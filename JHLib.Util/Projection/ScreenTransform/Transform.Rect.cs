using JHLib.Util.Geometry;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    public partial class Transform
    {
        /// <summary> [화면좌표] Rect와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectScreen(in FloatRect test) => _localBound.IsIntersect(test);

        /// <summary> [월드좌표] Rect와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectWorld(in FloatRect test) => _worldBound.IsIntersect(test);

        /// <summary> [WGS84좌표] Rect와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectWGS84(in FloatRect test) => _wgs84Bound.IsIntersect(test);


        /// <summary> [화면좌표] Rect(중심점과 반변)와 화면영역의 교차 판단 </summary>    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectScreen(in Float2D center, float half) => _localBound.IsIntersect(center, half);

        /// <summary> [화면좌표] Rect(중심점과 반변)와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectScreen(float sx, float sy, float half) => _localBound.IsIntersect(sx, sy, half);

        /// <summary> [월드좌표] Rect(중심점과 반변)와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectWorld(in Float2D center, float half) => _worldBound.IsIntersect(center, half);

        /// <summary> [월드좌표] Rect(중심점과 반변)와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectWorld(float wx, float wy, float half) => _worldBound.IsIntersect(wx, wy, half);


        /// <summary>  
        /// [월드좌표-> 화면좌표] 
        /// 중심점에서부터 반변을 가지는 Rect와의 교차 판단 <para/>
        /// 월드좌표를 화면좌표로 변환후의 화면 교차 판단 및 변환된 화면 좌표 출력
        /// </summary>  
        /// <param name="wp">중심점 (월드좌표)</param>
        /// <param name="half">반변 (화면좌표)</param>
        /// <param name="sx">변환된 화면좌표 x</param>
        /// <param name="sy">변환된 화면좌표 y</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectScreen(in Float2D wp, float half, out float sx, out float sy) =>
            RectIntersectScreen(wp.X, wp.Y, half, out sx, out sy);

        /// <summary>  
        /// [월드좌표-> 화면좌표] 
        /// 중심점에서부터 반변을 가지는 Rect와의 교차 판단 <para/>
        /// 월드좌표를 화면좌표로 변환후의 화면 교차 판단 및 변환된 화면 좌표 출력
        /// </summary>  
        /// <param name="wx">중심점 X(월드좌표)</param>
        /// <param name="wy">중심점 Y(월드좌표)</param>
        /// <param name="half">반변 (화면좌표)</param>
        /// <param name="sx">변환된 화면좌표 x</param>
        /// <param name="sy">변환된 화면좌표 y</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RectIntersectScreen(float wx, float wy, float half, out float sx, out float sy)
        {
            var sp = WorldToScreen(wx, wy); sx = sp.X; sy = sp.Y;
            return RectIntersectScreen(sp, half);
        }

        /// <summary> 
        /// [월드좌표] 화면 회전값을 고려한 OBB(화면)과 AABB(오브젝트)교차 관계 반환<br/>
        /// 이 함수는 AABB 교차검사(RectIntersectWorld)가 호출자 측에서 선행되었다고 가정하고 결과를 반환한다<br/>
        /// 회전값이 없는 경우는 AABB검사로 진행하며 IsContain검사가 실패했을경우 GeoRelation.Overlap으로 반환된다
        /// </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation RectRelationWorld(in FloatRect test)
        {
            if (IsZeroRotation)
            {
                if (_worldBound.IsContain(test))
                    return GeoRelation.Contains;
                return GeoRelation.Overlap;
            }
            else
            {
                return _worldOBB.TestLocalAxes(test);
            }
        }

        // 아래 두 함수는 RectRelationWorld 함수로 대체됨
        // 추후 이 함수를 참조하는 에러들은 RectRelationWorld 함수로 변경하여 사용하도록 수정할것

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //private bool RectContainWorldWithRotationCore(in FloatRect rect)
        //{
        //    var b = false;
        //    var lb = _wlb;
        //    var rb = _wrb;
        //    if (rect.IsLeft(lb, rb))
        //    {
        //        var rt = _wrt;
        //        if (rect.IsLeft(rb, rt))
        //        {
        //            var lt = _wlt;
        //            if (rect.IsLeft(rt, lt))
        //            {
        //                b = rect.IsLeft(lt, lb);
        //            }
        //        }
        //    }
        //    return b;
        //}

        ///// <summary> [월드좌표] 화면의 회전됬을경우 Rect가 화면에 완전히 벗어났는지 판단 </summary>   
        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public bool RectDisjointWorldOnRotation(in FloatRect rect)
        //{
        //    var b = true;
        //    var lb = _wlb;
        //    var rb = _wrb;
        //    if (rect.IsRight(lb, rb) == false)
        //    {
        //        var rt = _wrt;
        //        if (rect.IsRight(rb, rt) == false)
        //        {
        //            var lt = _wlt;
        //            if (rect.IsRight(rt, lt) == false)
        //            {
        //                b = rect.IsRight(lt, lb);
        //            }
        //        }
        //    }
        //    return b;
        //}
    }
}