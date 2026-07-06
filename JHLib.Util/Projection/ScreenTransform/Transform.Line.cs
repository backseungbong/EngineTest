using JHLib.Util.Geometry;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    public partial class Transform
    {
        /// <summary> [화면좌표] Line과 화면영역의 교차 판단 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LineIntersectScreen(in Float2D sp1, in Float2D sp2) =>
            GeometryHelper.LineIntersect(sp1, sp2, _localBound);

        /// <summary> [월드좌표] Line과 화면영역의 교차 판단 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LineIntersectWorld(in Float2D wp1, in Float2D wp2) =>
            GeometryHelper.LineIntersect(wp1, wp2, _worldBound);

        /// <summary> 
        /// [화면좌표] Line과 화면영역의 클리핑 <para/>
        /// Line이 화면영역에 걸치거나 완전히 포함하는경우 true <para/>
        /// Line과 화면영역이 교차하지 않는경우 false <para/>
        /// 클리핑된 좌표는 입력한 좌표가 수정되어 반환되며, 완전히 포함하는경우 입력좌표 그대로 반환된다 <para/>
        /// 클리핑된 좌표의 순서는 입력한 순으로 유지한다
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LineClipScreen(ref Float2D p1, ref Float2D p2) =>
            GeometryHelper.LineClip(p1, p2, _localBound, out p1, out p2);

        /// <summary> 
        /// [화면좌표] Line과 화면영역의 클리핑 <para/>
        /// Line이 화면영역에 걸치거나 완전히 포함하는경우 true <para/>
        /// Line과 화면영역이 교차하지 않는경우 false <para/>
        /// 클리핑된 좌표는 cp1, cp2로 반환되며, 완전히 포함하는경우 입력좌표 그대로 반환된다 <para/>
        /// 클리핑된 좌표의 순서는 입력한 순으로 유지한다
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LineClipScreen(in Float2D sp1, in Float2D sp2, out Float2D cp1, out Float2D cp2) =>
            GeometryHelper.LineClip(sp1, sp2, _localBound, out cp1, out cp2);


        /// <summary> 
        /// [월드좌표] Line과 화면영역의 클리핑 <para/>
        /// Line이 화면영역에 걸치거나 완전히 포함하는경우 true <para/>
        /// Line과 화면영역이 교차하지 않는경우 false <para/>
        /// 클리핑된 좌표는 입력한 좌표가 수정되어 반환되며, 완전히 포함하는경우 입력좌표 그대로 반환된다 <para/>
        /// 클리핑된 좌표의 순서는 입력한 순으로 유지한다
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LineClipWorld(ref Float2D p1, ref Float2D p2) =>
            GeometryHelper.LineClip(p1, p2, _worldBound, out p1, out p2);

        /// <summary> 
        /// [월드좌표] Line과 화면영역의 클리핑 <para/>
        /// Line이 화면영역에 걸치거나 완전히 포함하는경우 true <para/>
        /// Line과 화면영역이 교차하지 않는경우 false <para/>
        /// 클리핑된 좌표는 cp1, cp2로 반환되며, 완전히 포함하는경우 입력좌표 그대로 반환된다 <para/>
        /// 클리핑된 좌표의 순서는 입력한 순으로 유지한다
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LineClipWorld(in Float2D wp1, in Float2D wp2, out Float2D cp1, out Float2D cp2) =>
            GeometryHelper.LineClip(wp1, wp2, _worldBound, out cp1, out cp2);
    }
}