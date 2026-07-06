using JHLib.Util.DataStream;
using JHLib.Util.Geometry;
using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    public partial class Transform
    {
        /// <summary> [화면좌표] Area와 화면영역의 기하학적 관계를 반환한다 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaRelationScreen(Float2D[] path, bool containsCheck = false) =>
            GeometryHelper.AreaRelation(path, _localBound, containsCheck);

        /// <summary> [화면좌표] Area와 화면영역의 기하학적 관계를 반환한다 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaRelationScreen(ref Float2D path0, int count, bool containsCheck = false) =>
            GeometryHelper.AreaRelation(ref path0, count, _localBound, containsCheck);

        /// <summary> [월드좌표] Area와 화면영역의 기하학적 관계를 반환한다 </summary>  
        public GeoRelation AreaRelationWorld(Float2D[] path, bool containsCheck = false) =>
            GeometryHelper.AreaRelation(path, _worldBound, containsCheck);

        /// <summary> [월드좌표] Area와 화면영역의 기하학적 관계를 반환한다 </summary>  
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaRelationWorld(ref Float2D path0, int pathn, bool containsCheck = false) =>
            GeometryHelper.AreaRelation(ref path0, pathn, _worldBound, containsCheck);


        /// <summary> [화면좌표] Area를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 </summary>        
        /// <returns> Area와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaClipScreen(Float2D[] path, SList<ClippedPath> result) =>
            GeometryHelper.AreaClip(path, _localBound, result);

        /// <summary> [화면좌표] Area를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 </summary>        
        /// <returns> Area와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaClipScreen(ref Float2D path0, int pathn, SList<ClippedPath> result) =>
            GeometryHelper.AreaClip(ref path0, pathn, _localBound, result);

        /// <summary> [월드좌표] Area를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 </summary>        
        /// <returns> Area와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaClipWorld(Float2D[] path, SList<ClippedPath> result) =>
            GeometryHelper.AreaClip(path, _worldBound, result);

        /// <summary> [월드좌표] Area를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 </summary>        
        /// <returns> Area와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaClipWorld(ref Float2D path0, int pathn, SList<ClippedPath> result) =>
            GeometryHelper.AreaClip(ref path0, pathn, _worldBound, result);


        /// <summary> [화면좌표] 클리핑된 경로(ClippedPath) 리스트를 병합하여 하나의 Area로 반환한다 </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClippedPathMergeScreen(ref Float2D origin0, int originLength, SList<ClippedPath> cpaths, SList<Float2D> mergedArea) =>
            GeometryHelper.ClippedPathMerge(ref origin0, originLength, cpaths, _localBound, mergedArea);

        /// <summary> [화면좌표] Area를 화면영역으로 클리핑하여 클리핑된 경로(ClippedPath)와 병합된 Area를 동시에 반환한다 </summary>   
        /// <returns> Area와 화면영역 간의 기하학적 관계 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaClipAndMergeScreen(ref Float2D origin0, int originLength, SList<ClippedPath> result, SList<Float2D> mergedArea) =>
            GeometryHelper.AreaClipAndMerge(ref origin0, originLength, _localBound, result, mergedArea);

        /// <summary> [화면좌표] DataHeaderWriter 인자 전용 추가 함수 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeoRelation AreaClipScreen(ref Float2D path0, int pathn, in DataHeaderWriter result) =>
            GeometryHelper.AreaClip(ref path0, pathn, _localBound, result);
    }
}