using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Geometry
{
    using static JHLib.Util.Helper.RefCommand;
    using static System.Runtime.CompilerServices.Unsafe;
    public unsafe static partial class GeometryHelper
    {
        /// <summary> Path의 보간 위치를 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D PathLerp(Float2D[] path, double amount) =>
            PathLerp(ref MemoryMarshal.GetArrayDataReference(path), path.Length, amount);

        /// <summary> Path의 보간 위치를 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D PathLerp(ref Float2D path0, int count, double amount) =>
            count > 0 ? PathLerpInternal(ref path0, count, amount) : default;

        /// <summary> Path의 보간 위치를 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D PathLerp(Float2D* path, int count, double amount) =>
            count > 0 ? PathLerpInternal(ref *path, count, amount) : default;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Float2D PathLerpInternal(ref Float2D path0, int count, double amount)
        {
            if (count > 1)
            {
                if (count > 2)
                {
                    ref var p = ref AsB(ref path0);
                    ref var e = ref AddB(ref p, (count - 1) * Float2D.SIZE);
                    double dx, dy, dis, sum = 0;

                    do
                    {
                        dx = As<byte, float>(ref AddByteOffset(ref p, 8)) - As<byte, float>(ref p);
                        dy = As<byte, float>(ref AddByteOffset(ref p, 12)) - As<byte, float>(ref AddByteOffset(ref p, 4));
                        sum += Math.Sqrt(dx * dx + dy * dy);
                        p = ref AddByteOffset(ref p, 8);
                    }
                    while (IsAddressLessThan(ref p, ref e));

                    e = ref AsB(ref path0);
                    sum *= 1 - amount;

                    do
                    {
                        p = ref SubtractByteOffset(ref p, 8);
                        dx = As<byte, float>(ref AddByteOffset(ref p, 8)) - As<byte, float>(ref p);
                        dy = As<byte, float>(ref AddByteOffset(ref p, 12)) - As<byte, float>(ref AddByteOffset(ref p, 4));
                        dis = Math.Sqrt(dx * dx + dy * dy);
                    }
                    while ((sum -= dis) > 0 && IsAddressGreaterThan(ref p, ref e));

                    return new(
                        As<byte, float>(ref p) - sum / dis * dx,
                        As<byte, float>(ref AddByteOffset(ref p, 4)) - sum / dis * dy);
                }
                return new(
                    path0.X * (1 - amount) + AddT(ref path0, 1).X * amount,
                    path0.Y * (1 - amount) + AddT(ref path0, 1).Y * amount);
            }
            return path0;
        }


        /// <summary> Path와 사각형 영역의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PathIntersect(Float2D[] path, float x1, float y1, float x2, float y2) =>
            PathIntersect(ref MemoryMarshal.GetArrayDataReference(path), path.Length, new FloatRect(x1, y1, x2, y2));

        /// <summary> Path와 사각형 영역의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PathIntersect(Float2D[] path, in FloatRect rect) =>
            PathIntersect(ref MemoryMarshal.GetArrayDataReference(path), path.Length, rect);

        /// <summary> Path와 사각형 영역의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PathIntersect(ref Float2D path0, int pathn, in FloatRect rect)
        {
            fixed (Float2D* p0 = &path0)
                return PathIntersect(p0, pathn, rect);
        }

        /// <summary> Path와 사각형 영역의 교차여부를 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PathIntersect(Float2D* p0, int pn, in FloatRect rect)
        {
            var r = false;
            if (pn >= 2)
            {
                r = Avx.IsSupported ?
                    GeoRelationInternalAvx(p0, pn, rect) != 0 :
                    GeoRelationInternal(p0, pn, rect) != 0;
            }
            return r;
        }


        /// <summary>
        /// Path를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다<para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>         
        /// <param name="path">Path 좌표 배열</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <returns>Path와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation PathClip(Float2D[] path, in FloatRect rect, SList<ClippedPath> result) =>
            PathClip(ref MemoryMarshal.GetArrayDataReference(path), path.Length, rect, result);

        /// <summary>
        /// Path를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다<para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>         
        /// <param name="path0">Path 좌표의 첫 포인터</param>
        /// <param name="pathn">Path 좌표의 수</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <returns>Path와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation PathClip(ref Float2D path0, int pathn, in FloatRect rect, SList<ClippedPath> result)
        {
            fixed (Float2D* p0 = &path0)
                return PathClip(p0, pathn, rect, result);
        }

        /// <summary>
        /// Path를 사각형 영역으로 클리핑하여 클리핑된 경로(ClippedPath)를 반환한다<para/>
        /// 이 함수는 AVX 명령어를 지원하는 경우 하드웨어 명령어를 사용하여 최적화된다
        /// </summary>         
        /// <param name="p0">Path 좌표의 첫 포인터</param>
        /// <param name="pn">Path 좌표의 수</param>
        /// <param name="rect">클리핑 영역</param>
        /// <param name="result">클리핑된 경로가 저장될 리스트</param>
        /// <returns>Path와 사각형 간의 기하학적 관계</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeoRelation PathClip(Float2D* p0, int pn, in FloatRect rect, SList<ClippedPath> result)
        {
            var r = GeoRelation.Disjoint;
            if (pn >= 2)
            {
                r = Avx.IsSupported ?
                    ClipInternalAvx(p0, pn, rect, result) :
                    ClipInternal(p0, pn, rect, result);
            }
            return r;
        }
    }
}