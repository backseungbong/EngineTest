using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Geometry
{
    using static JHLib.Util.Helper.UnsafeEx;
    public unsafe static partial class GeometryHelper
    {
        /// <summary> 폴리곤의 중심점(단순 평균법)를 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D GetCentroidSimple(List<Float2D> path) =>
            GetCentroidSimple(CollectionsMarshal.AsSpan(path));

        /// <summary> 폴리곤의 중심점(단순 평균법)를 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D GetCentroidSimple(Span<Float2D> path) =>
            GetCentroidSimple(ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 폴리곤의 중심점(단순 평균법)를 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D GetCentroidSimple(ref Float2D path0, int pathn)
        {
            fixed (Float2D* p0 = &path0)
                return GetCentroidSimple(p0, pathn);
        }

        /// <summary> 폴리곤의 중심점(단순 평균법)를 구한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Float2D GetCentroidSimple(Float2D* p0, int pn)
        {
            var result = default(Float2D);
            var pe = p0 + pn;

            if (pn > 2)
            {
                var cx = 0d;
                var cy = 0d;

                var pc = p0;
                var ps = *p0;

                if (pn > 4)
                {
                    do
                    {
                        cx += (pc + 0)->X;
                        cy += (pc + 0)->Y;
                        cx += (pc + 1)->X;
                        cy += (pc + 1)->Y;
                        cx += (pc + 2)->X;
                        cy += (pc + 2)->Y;
                        cx += (pc + 3)->X;
                        cy += (pc + 3)->Y;
                    }
                    while ((pc += 4) < pe - 4);
                }
                do
                {
                    cx += pc->X;
                    cy += pc->Y;
                }
                while (++pc < pe);

                if (*(pe - 1) == ps)
                {
                    cx -= ps.X;
                    cy -= ps.Y;
                    pn--;
                }
                result = new(cx / pn, cy / pn);
            }
            else if (pn > 0)
            {
                result = p0->Lerp(*(pe - 1), 0.5f);
            }
            return result;
        }



        /// <summary> 폴리곤의 중심점(면적 가중 중심)를 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D GetCentroid(List<Float2D> path) =>
            GetCentroid(CollectionsMarshal.AsSpan(path));

        /// <summary> 폴리곤의 중심점(면적 가중 중심)를 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D GetCentroid(Span<Float2D> path) =>
            GetCentroid(ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 폴리곤의 중심점(면적 가중 중심)를 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D GetCentroid(ref Float2D path0, int pathn)
        {
            fixed (Float2D* p0 = &path0)
                return GetCentroid(p0, pathn);
        }

        /// <summary> 폴리곤의 중심점(면적 가중 중심)를 구한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Float2D GetCentroid(Float2D* p0, int pn)
        {
            var result = default(Float2D);
            var pe = p0 + pn;

            if (pn > 2)
            {
                var ac = 0d;
                var cx = 0d;
                var cy = 0d;

                var pc = p0 + 1;
                var ps = *p0;

                var x1 = (double)p0->X;
                var y1 = (double)p0->Y;
                do
                {
                    var x2 = (double)pc->X;
                    var y2 = (double)pc->Y;
                    var t = x1 * y2 - x2 * y1;
                    ac += t;
                    cx += (x1 + x2) * t;
                    cy += (y1 + y2) * t;
                    x1 = x2;
                    y1 = y2;
                }
                while (++pc < pe);

                if (*(pe - 1) != ps)
                {
                    var t = x1 * ps.Y - ps.X * y1;
                    ac += t;
                    cx += (x1 + ps.X) * t;
                    cy += (y1 + ps.Y) * t;
                }

                if (ac * ac > 4)
                    result = new(cx / (ac * 3), cy / (ac * 3));
                else
                    result = ps;
            }
            else if (pn > 0)
            {
                result = p0->Lerp(*(pe - 1), 0.5f);
            }
            return result;
        }

        /// <summary> 폴리곤의 면적을 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetArea(List<Float2D> path) =>
            GetArea(CollectionsMarshal.AsSpan(path));

        /// <summary> 폴리곤의 면적을 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetArea(Span<Float2D> path) =>
            GetArea(ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 폴리곤의 면적을 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetArea(ref Float2D path0, int pathn)
        {
            fixed (Float2D* p0 = &path0)
                return GetArea(p0, pathn);
        }

        /// <summary> 폴리곤의 면적을 구한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetArea(Float2D* p0, int pn)
        {
            var result = 0f;
            if (pn >= 3) result = GetAreaInternal(p0, pn);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float GetAreaInternal(Float2D* p0, int pn)
        {
            var t = p0 + 1;
            var e = p0 + pn;
            var s = 0d;
            var zx = p0->X;
            var zy = p0->Y;

            if (t <= e - 4)
            {
                do s += ((t + 0)->X - (t - 1)->X) * ((t + 0)->Y + (t - 1)->Y) +
                        ((t + 1)->X - (t + 0)->X) * ((t + 1)->Y + (t + 0)->Y) +
                        ((t + 2)->X - (t + 1)->X) * ((t + 2)->Y + (t + 1)->Y) +
                        ((t + 3)->X - (t + 2)->X) * ((t + 3)->Y + (t + 2)->Y);
                while ((t += 4) <= e - 4);
            }

            if (t != e)
            {
                s += ((t + 0)->X - (t - 1)->X) * ((t + 0)->Y + (t - 1)->Y);
                if (t != e - 1)
                {
                    s += ((t + 1)->X - (t + 0)->X) * ((t + 1)->Y + (t + 0)->Y);
                    if (t != e - 2)
                    {
                        s += ((t + 2)->X - (t + 1)->X) * ((t + 2)->Y + (t + 1)->Y);
                    }
                }
            }
            return (float)(s + (zx - (e - 1)->X) * (zy + (e - 1)->Y));
        }


        /// <summary>
        /// 다각형이 퇴화 되었는지(면적이 0에 가까운지) 판단한다 <para/>
        /// 부동 소수점 오차를 고려하여 면적이 epsilon 이하이면 면적이 없는 것으로 간주한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPolygonDegenerate(Span<Float2D> span, float epsilon = 1e-6f) =>
            IsPolygonDegenerate(ref MemoryMarshal.GetReference(span), span.Length, epsilon);

        /// <summary>
        /// 다각형이 퇴화 되었는지(면적이 0에 가까운지) 판단한다 <para/>
        /// 부동 소수점 오차를 고려하여 면적이 epsilon 이하이면 면적이 없는 것으로 간주한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPolygonDegenerate(ref Float2D path0, int pathn, float epsilon = 1e-6f)
        {
            fixed (Float2D* p0 = &path0)
                return Math.Abs(GetAreaInternal(p0, pathn)) < epsilon;
        }

        /// <summary> 폴리곤이 시계 방향으로 진행하는지 판단한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClockwise(List<Float2D> path) =>
            IsClockwise(CollectionsMarshal.AsSpan(path));

        /// <summary> 폴리곤이 시계 방향으로 진행하는지 판단한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClockwise(Span<Float2D> span) =>
            IsClockwise(ref MemoryMarshal.GetReference(span), span.Length);

        /// <summary> 폴리곤이 시계 방향으로 진행하는지 판단한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClockwise(ref Float2D path0, int pathn)
        {
            fixed (Float2D* p0 = &path0)
                return IsClockwise(p0, pathn);
        }

        /// <summary> 폴리곤이 시계 방향으로 진행하는지 판단한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClockwise(Float2D* p0, int pn)
        {
            var result = false;
            if (pn >= 2) result = IsClockwiseInternal(p0, pn);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsClockwiseInternal(Float2D* p0, int pn)
        {
            var t = p0 + 1;
            var e = p0 + pn;
            var s = 0f;
            var zx = p0->X;
            var zy = p0->Y;

            if (t <= e - 4)
            {
                do s += ((t + 0)->X - (t - 1)->X) * ((t + 0)->Y + (t - 1)->Y) +
                        ((t + 1)->X - (t + 0)->X) * ((t + 1)->Y + (t + 0)->Y) +
                        ((t + 2)->X - (t + 1)->X) * ((t + 2)->Y + (t + 1)->Y) +
                        ((t + 3)->X - (t + 2)->X) * ((t + 3)->Y + (t + 2)->Y);
                while ((t += 4) <= e - 4);
            }

            if (t != e)
            {
                s += ((t + 0)->X - (t - 1)->X) * ((t + 0)->Y + (t - 1)->Y);
                if (t != e - 1)
                {
                    s += ((t + 1)->X - (t + 0)->X) * ((t + 1)->Y + (t + 0)->Y);
                    if (t != e - 2)
                    {
                        s += ((t + 2)->X - (t + 1)->X) * ((t + 2)->Y + (t + 1)->Y);
                    }
                }
            }
            return s + (zx - (e - 1)->X) * (zy + (e - 1)->Y) < 0;
        }


        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Crossing 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonCrossing(in Float2D point, List<Float2D> path) =>
            PointInPolygonCrossing(point.X, point.Y, CollectionsMarshal.AsSpan(path));

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Crossing 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonCrossing(float x, float y, List<Float2D> path) =>
            PointInPolygonCrossing(x, y, CollectionsMarshal.AsSpan(path));

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Crossing 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonCrossing(in Float2D point, Span<Float2D> path) =>
            PointInPolygonCrossing(point.X, point.Y, ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Crossing 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonCrossing(float x, float y, Span<Float2D> path) =>
            PointInPolygonCrossing(x, y, ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Crossing 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonCrossing(float x, float y, ref Float2D path0, int pathn)
        {
            fixed (Float2D* p0 = &path0)
                return PointInPolygonCrossing(x, y, p0, pathn);
        }

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Crossing 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonCrossing(float x, float y, Float2D* p0, int pn)
        {
            var result = false;
            if (pn > 2) result = PointInPolygonCrossingInternal(x, y, p0, pn);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PointInPolygonCrossingInternal(in Float2D p, Float2D* p0, int pn) =>
            PointInPolygonCrossingInternal(p.X, p.Y, p0, pn);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool PointInPolygonCrossingInternal(float x, float y, Float2D* p0, int pn)
        {
            var n = false;
            var pc = p0;
            var pe = p0 + pn;
            var ps = *p0;
            if (ps.Y > y) { goto G2; }

        G1: pc = PointInPolygonLEYPass(pc + 1, pe, y);
            if (pc == pe) { if (ps.Y > y) { goto E1; } else { goto EX; } }
            if (pc->X + (y - pc->Y) / ((pc - 1)->Y - pc->Y) * ((pc - 1)->X - pc->X) < x) { n = !n; }

        G2: pc = PointInPolygonGYPass(pc + 1, pe, y);
            if (pc == pe) { if (ps.Y <= y) { goto E1; } else { goto EX; } }
            if (pc->X + (y - pc->Y) / ((pc - 1)->Y - pc->Y) * ((pc - 1)->X - pc->X) < x) { n = !n; }
            goto G1;

        E1: if (ps.X + (y - ps.Y) / ((pe - 1)->Y - ps.Y) * ((pe - 1)->X - ps.X) < x) { n = !n; }
        EX: return n;
        }


        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Winding 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonWinding(in Float2D point, List<Float2D> path) =>
            PointInPolygonWinding(point.X, point.Y, CollectionsMarshal.AsSpan(path));

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Winding 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonWinding(float x, float y, List<Float2D> path) =>
            PointInPolygonWinding(x, y, CollectionsMarshal.AsSpan(path));

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Winding 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonWinding(in Float2D point, Span<Float2D> path) =>
            PointInPolygonWinding(point.X, point.Y, ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Winding 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonWinding(float x, float y, Span<Float2D> path) =>
            PointInPolygonWinding(x, y, ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Winding 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonWinding(float x, float y, ref Float2D path0, int pathn)
        {
            fixed (Float2D* p0 = &path0)
                return PointInPolygonWinding(x, y, p0, pathn);
        }

        /// <summary> 지정된 좌표가 폴리곤 안쪽에 위치하는지 판단한다 (Winding 방식) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInPolygonWinding(float x, float y, Float2D* p0, int pn)
        {
            var result = false;
            if (pn > 2) result = PointInPolygonWindingInternal(x, y, p0, pn);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PointInPolygonWindingInternal(in Float2D p, Float2D* p0, int pn) =>
            PointInPolygonWindingInternal(p.X, p.Y, p0, pn);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool PointInPolygonWindingInternal(float x, float y, Float2D* p0, int pn)
        {
            var n = 0;
            var pc = p0;
            var pe = p0 + pn;
            var ps = *p0;
            if (ps.Y > y) { goto G2; }

        G1: pc = PointInPolygonLEYPass(pc + 1, pe, y);
            if (pc == pe) { if (ps.Y > y && ((pe - 1)->X - ps.X) * (y - ps.Y) > (x - ps.X) * ((pe - 1)->Y - ps.Y)) { n++; } goto EX; }
            if (((pc - 1)->X - pc->X) * (y - pc->Y) > (x - pc->X) * ((pc - 1)->Y - pc->Y)) { n++; }

        G2: pc = PointInPolygonGYPass(pc + 1, pe, y);
            if (pc == pe) { if (ps.Y <= y && ((pe - 1)->X - ps.X) * (y - ps.Y) < (x - ps.X) * ((pe - 1)->Y - ps.Y)) { n--; } goto EX; }
            if (((pc - 1)->X - pc->X) * (y - pc->Y) < (x - pc->X) * ((pc - 1)->Y - pc->Y)) { n--; }
            goto G1;

        EX: return n != 0;
        }

        /// <summary> 지정된 좌표가 폴리곤(홀이 있을수 있는) 내부에 위치하는지 판단한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool PointInGeometry(Float2D point, Float2D[][] paths)
        {
            var result = false;
            if (paths.Length != 0)
            {
                ref var path0 = ref MemoryMarshal.GetArrayDataReference(paths);
                var i = 0;
                do result ^= PointInPolygonWinding(point, Unsafe.Add(ref path0, i));
                while (++i < paths.Length);
            }
            return result;
        }

        /// <summary>
        /// 지정된 좌표가 여러 폴리곤(홀이 있을수 있는) 중 어느 폴리곤 내부에 있는지 판단하고 해당 폴리곤의 인덱스를 반환한다 <para/>
        /// 폴리곤 내부에 위치하지 않으면 -1을 반환한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PointInGeometry(Float2D point, Float2D[][][] region)
        {
            var result = -1;
            if (region.Length != 0)
            {
                ref var region0 = ref MemoryMarshal.GetArrayDataReference(region);
                var i = 0;
                do if (PointInGeometry(point, Unsafe.Add(ref region0, i))) { result = i; break; }
                while (++i < region.Length);
            }
            return result;
        }


        /// <summary> 모든 좌표들을 포함하는 최소 경계 사각형을 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FloatRect GetBound(List<Float2D> path) =>
            GetBound(CollectionsMarshal.AsSpan(path));

        /// <summary> 모든 좌표들을 포함하는 최소 경계 사각형을 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FloatRect GetBound(Span<Float2D> path) =>
            GetBound(ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 모든 좌표들을 포함하는 최소 경계 사각형을 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FloatRect GetBound(ref Float2D src, int len)
        {
            if (len <= 0) return default;
            if (Sse2.IsSupported) return GetBoundSimd(ref src, len);
            else return GetBoundScalar(ref src, len);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static FloatRect GetBoundSimd(ref Float2D src, int len)
        {
            Vector128<float> rMin, rMax;

            if (len != 1)
            {
                var l = (nint)len;
                if (len > 4)
                {
                    ref var e = ref AddT(src, l - 4);
                    ref var p = ref AddT(src, 0);

                    if (Avx2.IsSupported)
                    {
                        var aMin = SIMD.LoadFloat256(p);
                        var aMax = aMin;
                        if (len > 8)
                        {
                            ref var e8 = ref SubT(e, 4);

                            var bMin = SIMD.LoadFloat256(p, 4);
                            var bMax = bMin;
                            if (len > 16)
                            {
                                p = ref AddT(p, 8);
                                do
                                {
                                    var v1 = SIMD.LoadFloat256(p);
                                    var v2 = SIMD.LoadFloat256(p, 4);
                                    aMin = Avx.Min(aMin, v1);
                                    aMax = Avx.Max(aMax, v1);
                                    bMin = Avx.Min(bMin, v2);
                                    bMax = Avx.Max(bMax, v2);
                                    p = ref AddT(p, 8);
                                }
                                while (Less(p, e8));
                            }
                            var v3 = SIMD.LoadFloat256(e8);
                            aMin = Avx.Min(Avx.Min(aMin, bMin), v3);
                            aMax = Avx.Max(Avx.Max(aMax, bMax), v3);
                        }
                        var v4 = SIMD.LoadFloat256(e);
                        aMin = Avx.Min(aMin, v4);
                        aMax = Avx.Max(aMax, v4);
                        rMin = Sse.Min(aMin.GetUpper(), aMin.GetLower());
                        rMax = Sse.Max(aMax.GetUpper(), aMax.GetLower());
                    }
                    else
                    {
                        var aMin = SIMD.LoadFloat128(p);
                        var aMax = aMin;
                        var bMin = SIMD.LoadFloat128(p, 2);
                        var bMax = bMin;
                        if (len > 8)
                        {
                            p = ref AddT(p, 4);
                            do
                            {
                                var v1 = SIMD.LoadFloat128(p);
                                var v2 = SIMD.LoadFloat128(p, 2);
                                aMin = Sse.Min(aMin, v1);
                                aMax = Sse.Max(aMax, v1);
                                bMin = Sse.Min(bMin, v2);
                                bMax = Sse.Max(bMax, v2);
                                p = ref AddT(p, 4);
                            }
                            while (Less(p, e));
                        }
                        var v3 = SIMD.LoadFloat128(e);
                        var v4 = SIMD.LoadFloat128(e, 2);
                        aMin = Sse.Min(aMin, v3);
                        aMax = Sse.Max(aMax, v3);
                        bMin = Sse.Min(bMin, v4);
                        bMax = Sse.Max(bMax, v4);
                        rMin = Sse.Min(aMin, bMin);
                        rMax = Sse.Max(aMax, bMax);
                    }
                }
                else
                {
                    var v1 = SIMD.LoadFloat128(src, l - 2);
                    var v2 = SIMD.LoadFloat128(src);
                    rMin = Sse.Min(v2, v1);
                    rMax = Sse.Max(v2, v1);
                }
                rMin = Sse.Min(rMin, Sse.Shuffle(rMin, rMin, 0b_01_00_11_10));
                rMax = Sse.Max(rMax, Sse.Shuffle(rMax, rMax, 0b_01_00_11_10));
                return Unsafe.BitCast<Vector128<float>, FloatRect>(Sse.MoveLowToHigh(rMin, rMax));
            }
            else
            {
                return Unsafe.BitCast<Vector128<float>, FloatRect>(SIMD.LoadFloat128Dup(src));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static FloatRect GetBoundScalar(ref Float2D src, int len)
        {
            var xMin = src.X;
            var yMin = src.Y;
            var xMax = xMin;
            var yMax = yMin;

            ref var p = ref AddT(src, 1);
            ref var e = ref AddT(src, len);
            do
            {
                var t = p;
                if (t.X < xMin) xMin = t.X;
                else if (xMax < t.X) xMax = t.X;
                if (t.Y < yMin) yMin = t.Y;
                else if (yMax < t.Y) yMax = t.Y;
                p = ref AddT(p, 1);
            }
            while (Less(p, e));
            return new(xMin, yMin, xMax, yMax);
        }

        /// <summary> 모든 좌표들을 포함하는 최소 경계 사각형을 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DoubleRect GetBound(List<Double2D> path) =>
            GetBound(CollectionsMarshal.AsSpan(path));

        /// <summary> 모든 좌표들을 포함하는 최소 경계 사각형을 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DoubleRect GetBound(Span<Double2D> span) =>
            GetBound(ref MemoryMarshal.GetReference(span), span.Length);

        /// <summary> 모든 좌표들을 포함하는 최소 경계 사각형을 반환한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static DoubleRect GetBound(ref Double2D path0, int pathn)
        {
            var xMin = path0.X;
            var yMin = path0.Y;
            var xMax = xMin;
            var yMax = yMin;

            if (pathn >= 2)
            {
                ref var p = ref Unsafe.Add(ref path0, 1);
                ref var e = ref Unsafe.Add(ref path0, pathn);
                do
                {
                    var t = p;
                    if (t.X < xMin) xMin = t.X;
                    else if (xMax < t.X) xMax = t.X;
                    if (t.Y < yMin) yMin = t.Y;
                    else if (yMax < t.Y) yMax = t.Y;
                    p = ref Unsafe.Add(ref p, 1);
                }
                while (Unsafe.IsAddressLessThan(ref p, ref e));
            }
            return new(xMin, yMin, xMax, yMax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* PointInPolygonLEYPass(Float2D* p, Float2D* e, float y)
        {
            if (Avx.IsSupported)
                return PointInPolygonLEYPassAvx(p, e, y);

            if (p <= e - 4)
            {
                do
                {
                    if (y < (p + 0)->Y) { return p; }
                    if (y < (p + 1)->Y) { return p + 1; }
                    if (y < (p + 2)->Y) { return p + 2; }
                    if (y < (p + 3)->Y) { return p + 3; }
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e) { while (y < p->Y == false && ++p < e) ; }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* PointInPolygonLEYPassAvx(Float2D* p, Float2D* e, float y)
        {
            if (p <= e - 4)
            {
                var vy = Vector256.Create(y);
                do
                {
                    var v = Avx.LoadVector256((float*)p);
                    var c = Avx.CompareGreaterThan(v, vy);
                    var m = Avx.MoveMask(c) & 0b_10101010;
                    if (m != 0) return p + ((uint)BitOperations.TrailingZeroCount(m) >> 1);
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e) { while (y < p->Y == false && ++p < e) ; }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* PointInPolygonGYPass(Float2D* p, Float2D* e, float y)
        {
            if (Avx.IsSupported)
                return PointInPolygonGYPassAvx(p, e, y);

            if (p <= e - 4)
            {
                do
                {
                    if ((p + 0)->Y <= y) { return p; }
                    if ((p + 1)->Y <= y) { return p + 1; }
                    if ((p + 2)->Y <= y) { return p + 2; }
                    if ((p + 3)->Y <= y) { return p + 3; }
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e) { while (p->Y <= y == false && ++p < e) ; }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* PointInPolygonGYPassAvx(Float2D* p, Float2D* e, float y)
        {
            if (p <= e - 4)
            {
                var vy = Vector256.Create(y);
                do
                {
                    var v = Avx.LoadVector256((float*)p);
                    var c = Avx.CompareLessThanOrEqual(v, vy);
                    var m = Avx.MoveMask(c) & 0b_10101010;
                    if (m != 0) return p + ((uint)BitOperations.TrailingZeroCount(m) >> 1);
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e) { while (p->Y <= y == false && ++p < e) ; }
            return p;
        }
    }
}