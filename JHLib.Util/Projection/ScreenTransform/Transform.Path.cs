using JHLib.Util.Geometry;
using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    public partial class Transform
    {
        /// <summary> [화면좌표] Path와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PathIntersectScreen(Float2D[] path) =>
            GeometryHelper.PathIntersect(path, _localBound);

        /// <summary> [화면좌표] Path와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PathIntersectScreen(ref Float2D path0, int pathn) =>
            GeometryHelper.PathIntersect(ref path0, pathn, _localBound);

        /// <summary> [월드좌표] Path와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PathIntersectWorld(Float2D[] path) =>
            GeometryHelper.PathIntersect(path, _worldBound);

        /// <summary> [월드좌표] Path와 화면영역의 교차 판단 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PathIntersectWorld(ref Float2D path0, int pathn) =>
            GeometryHelper.PathIntersect(ref path0, pathn, _worldBound);


        /// <summary> [화면좌표] Path를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 </summary>        
        /// <returns> Path와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation PathClipScreen(Float2D[] path, SList<ClippedPath> result) =>
            GeometryHelper.PathClip(path, _localBound, result);

        /// <summary> [화면좌표] Path를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 </summary>        
        /// <returns> Path와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation PathClipScreen(ref Float2D path0, int pathn, SList<ClippedPath> result) =>
            GeometryHelper.PathClip(ref path0, pathn, _localBound, result);

        /// <summary> [월드좌표] Path를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 </summary>        
        /// <returns> Path와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation PathClipWorld(Float2D[] path, SList<ClippedPath> result) =>
            GeometryHelper.PathClip(path, _worldBound, result);

        /// <summary> [월드좌표] Path를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 </summary>        
        /// <returns> Path와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation PathClipWorld(ref Float2D path0, int pathn, SList<ClippedPath> result) =>
            GeometryHelper.PathClip(ref path0, pathn, _worldBound, result);
    }
}