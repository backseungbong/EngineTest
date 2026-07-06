using JHLib.Util.DataStream;
using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        /// <summary> Area와 사각형 영역의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreaIntersect(Float2D[] area, in FloatRect rect) =>
            AreaIntersect(ref MemoryMarshal.GetArrayDataReference(area), area.Length, rect);

        /// <summary> Area와 사각형 영역의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreaIntersect(ref Float2D path0, int count, in FloatRect rect)
        {
            fixed (Float2D* p0 = &path0)
                return AreaIntersect(p0, count, rect);
        }

        /// <summary> Area와 사각형 영역의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreaIntersect(Float2D* p0, int count, in FloatRect rect)
        {
            var r = false;
            if (count >= 2)
            {
                r = Avx.IsSupported ?
                    GeoRelationInternalAvx(p0, count, rect, true) != 0 :
                    GeoRelationInternal(p0, count, rect, true) != 0;
            }
            return r;
        }


        /// <summary> 
        /// Area와 사각형 영역의 기하학적 관계를 반환한다 <para/>
        /// Area와 사각형이 교차하지 않으면 Disjoint <para/>
        /// Area가 사각형에 교차한다면 Overlap <para/>
        /// 사각형 내부에 Area가 완전히 포함된다면 Contains (기본은 Overlap으로 통합됨, Contains 판단 필요시 containsCheck = true 설정 필요) <para/>
        /// Area 내부에 사각형이 완전히 포함된다면 IsContained <para/>
        /// </summary>     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaRelation(Float2D[] area, in FloatRect rect, bool containsCheck = false) =>
            AreaRelation(ref MemoryMarshal.GetArrayDataReference(area), area.Length, rect, containsCheck);

        /// <summary> 
        /// Area와 사각형 영역의 기하학적 관계를 반환한다 <para/>
        /// Area와 사각형이 교차하지 않으면 Disjoint <para/>
        /// Area가 사각형에 교차한다면 Overlap <para/>
        /// 사각형 내부에 Area가 완전히 포함된다면 Contains (기본은 Overlap으로 통합됨, Contains 판단 필요시 containsCheck = true 설정 필요) <para/>
        /// Area 내부에 사각형이 완전히 포함된다면 IsContained <para/>
        /// </summary>      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaRelation(ref Float2D path0, int pathn, in FloatRect rect, bool containsCheck = false)
        {
            fixed (Float2D* p0 = &path0)
                return AreaRelation(p0, pathn, rect, containsCheck);
        }

        /// <summary> 
        /// Area와 사각형 영역의 기하학적 관계를 반환한다 <para/>
        /// Area와 사각형이 교차하지 않으면 Disjoint <para/>
        /// Area가 사각형에 교차한다면 Overlap <para/>
        /// 사각형 내부에 Area가 완전히 포함된다면 Contains (기본은 Overlap으로 통합됨, Contains 판단 필요시 containsCheck = true 설정 필요) <para/>
        /// Area 내부에 사각형이 완전히 포함된다면 IsContained <para/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaRelation(Float2D* p0, int pn, in FloatRect rect, bool containsCheck = false)
        {
            var r = GeoRelation.Disjoint;
            if (pn >= 2)
            {
                r = Avx.IsSupported ?
                    GeoRelationInternalAvx(p0, pn, rect, true, containsCheck) :
                    GeoRelationInternal(p0, pn, rect, true, containsCheck);
            }
            return r;
        }



        /// <summary>
        /// Area를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 <para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>         
        /// <param name="path">Area 좌표 배열</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <returns>Area와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaClip(Float2D[] path, in FloatRect rect, SList<ClippedPath> result) =>
            AreaClip(ref MemoryMarshal.GetArrayDataReference(path), path.Length, rect, result);

        /// <summary>
        /// Area를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 <para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>         
        /// <param name="path0">Area 좌표 첫 포인터</param>
        /// <param name="pathn">Area 좌표 수</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <returns>Area와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaClip(ref Float2D path0, int pathn, in FloatRect rect, SList<ClippedPath> result)
        {
            fixed (Float2D* p0 = &path0)
                return AreaClip(p0, pathn, rect, result);
        }

        /// <summary>
        /// Area를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다 <para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>         
        /// <param name="p0">Area 좌표 첫 포인터</param>
        /// <param name="pn">Area 좌표 수</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <returns>Area와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaClip(Float2D* p0, int pn, in FloatRect rect, SList<ClippedPath> result)
        {
            var r = GeoRelation.Disjoint;
            if (pn >= 2)
            {
                r = Avx.IsSupported ?
                    ClipInternalAvx(p0, pn, rect, result, true) :
                    ClipInternal(p0, pn, rect, result, true);
            }
            return r;
        }


        /// <summary>
        /// 클리핑된 경로(ClippedPath) 리스트를 병합하여 하나의 Area로 반환한다 <para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>
        /// <param name="path">Area 좌표 배열</param>
        /// <param name="cpaths">클리핑된 경로의 리스트</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="mergedArea">병합된 좌표가 저장될 리스트</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClippedPathMerge(Float2D[] path, SList<ClippedPath> cpaths, in FloatRect rect, SList<Float2D> mergedArea) =>
            ClippedPathMerge(ref MemoryMarshal.GetArrayDataReference(path), path.Length, cpaths, rect, mergedArea);

        /// <summary>
        /// 클리핑된 경로(ClippedPath) 리스트를 병합하여 하나의 Area로 반환한다 <para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>
        /// <param name="path0">Area 좌표 첫 포인터</param>
        /// <param name="pathn">Area 좌표 수</param>
        /// <param name="cpaths">클리핑된 경로의 리스트</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="mergedArea">병합된 좌표가 저장될 리스트</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClippedPathMerge(ref Float2D path0, int pathn, SList<ClippedPath> cpaths, in FloatRect rect, SList<Float2D> mergedArea)
        {
            fixed (Float2D* p0 = &path0)
            fixed (ClippedPath* cp0 = &cpaths.Ref0)
            {
                ClippedPathMerge(p0, pathn, cp0, cpaths.Count, rect, mergedArea);
                return;
            }
        }

        /// <summary>
        /// 클리핑된 경로(ClippedPath) 리스트를 병합하여 하나의 Area로 반환한다 <para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>
        /// <param name="p0">Area 좌표 첫 포인터</param>
        /// <param name="pn">Area 좌표 수</param>
        /// <param name="cp0">클리핑된 경로의 첫 포인터</param>
        /// <param name="cpn">클리핑된 경로의 수</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="mergedArea">병합된 경로가 저장될 리스트</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClippedPathMerge(Float2D* p0, int pn, ClippedPath* cp0, int cpn, in FloatRect rect, SList<Float2D> mergedArea)
        {
            if (Avx.IsSupported)
                ClippedPathMergeInternalAvx(p0, pn, cp0, cpn, rect, mergedArea);
            else
                ClippedPathMergeInternal(p0, pn, cp0, cpn, rect, mergedArea);
        }



        /// <summary>
        /// Area를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)와 병합된 Area를 동시에 반환한다<para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다<para/>
        /// 기하학적 관계가 Contains인 경우 병합된 Area는 비어있는 상태로 반환된다
        /// </summary>
        /// <param name="path">Area 좌표 배열</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <param name="mergedArea">병합된 경로가 저장될 리스트</param>
        /// <returns>Area와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaClipAndMerge(Float2D[] path, in FloatRect rect, SList<ClippedPath> result, SList<Float2D> mergedArea) =>
            AreaClipAndMerge(ref MemoryMarshal.GetArrayDataReference(path), path.Length, rect, result, mergedArea);

        /// <summary>
        /// Area를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)와 병합된 Area를 동시에 반환한다<para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다<para/>
        /// 기하학적 관계가 Contains인 경우 병합된 Area는 비어있는 상태로 반환된다
        /// </summary>
        /// <param name="path0">Area 좌표의 첫 포인터</param>
        /// <param name="pathn">Area 좌표의 수</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <param name="mergedArea">병합된 경로가 저장될 리스트</param>
        /// <returns>Area와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaClipAndMerge(ref Float2D path0, int pathn, in FloatRect rect, SList<ClippedPath> result, SList<Float2D> mergedArea)
        {
            fixed (Float2D* p0 = &path0)
                return AreaClipAndMerge(p0, pathn, rect, result, mergedArea);
        }

        /// <summary>
        /// Area를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)와 병합된 Area를 동시에 반환한다<para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다<para/>
        /// 기하학적 관계가 Contains인 경우 병합된 Area는 비어있는 상태로 반환된다
        /// </summary>     
        /// <param name="p0">Area 좌표의 첫 포인터</param>
        /// <param name="pn">Area 좌표의 수</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <param name="mergedArea">병합된 경로가 저장될 리스트</param>
        /// <returns>Area와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaClipAndMerge(Float2D* p0, int pn, in FloatRect rect, SList<ClippedPath> result, SList<Float2D> mergedArea)
        {
            var r = GeoRelation.Disjoint;
            if (pn >= 3)
            {
                r = Avx.IsSupported ?
                    AreaClipAndMergeInternalAvx(p0, pn, rect, result, mergedArea) :
                    AreaClipAndMergeInternal(p0, pn, rect, result, mergedArea);
            }
            return r;
        }

        /// <summary> DataHeaderWriter 인자 전용 함수 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation AreaClip(ref Float2D path0, int pathn, in FloatRect rect, in DataHeaderWriter result)
        {
            var r = GeoRelation.Disjoint;
            if (pathn >= 2)
            {
                fixed (Float2D* p0 = &path0)
                {
                    r = Avx.IsSupported ?
                        ClipInternalAvx(p0, pathn, rect, result, true) :
                        ClipInternal(p0, pathn, rect, result, true);

                    result.UpdateCount<ClippedPath>();
                }
            }
            return r;
        }

        /// <summary> DataHeaderWriter 인자 전용 함수 </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClippedPathMerge(DataHeaderReader<Float2D> pathReader, DataHeaderReader<ClippedPath> cpathReader, in FloatRect rect, in DataHeaderWriter mergedArea)
        {
            fixed (Float2D* p0 = &pathReader.Data0)
            fixed (ClippedPath* cp0 = &cpathReader.Data0)
            {
                if (Avx.IsSupported)
                    ClippedPathMergeInternalAvx(p0, pathReader.Count, cp0, cpathReader.Count, rect, mergedArea);
                else
                    ClippedPathMergeInternal(p0, pathReader.Count, cp0, cpathReader.Count, rect, mergedArea);

                mergedArea.UpdateCount<Float2D>();
                return;
            }
        }
    }
}