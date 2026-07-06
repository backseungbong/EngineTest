using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Geometry
{
    public static partial class GeometryHelper
    {
        // =================================================================================================================
        // 라인 클리핑은 liang-barsky / Slab Method based Ray-AABB 알고리즘을 사용하여 구현됨
        // 화면에 걸친 라인을 X혹은 Y좌표 기준으로 잘라가는 알고리즘으로, +Inf, -Inf, NaN 케이스를 고려하여 구현됨
        // 고빈도 호출 상황을 대비하여 성능을 최적화함 (Simd 처리, 병렬처리 유도, 분기 최적화 등)
        // 2026.5.13 WRLEE
        // =================================================================================================================

        /// <summary>
        /// 선분(Line)과 사각형(Rect)의 교차 여부 판단 <br/>
        /// 교차점 좌표 계산을 생략하므로 LineClip 대비 고성능
        /// </summary>
        /// <param name="p1">선분의 시작점</param>
        /// <param name="p2">선분의 끝점</param>
        /// <param name="rect">클리핑 기준이 되는 사각형(Bounds)</param>
        /// <returns>선분이 사각형에 걸치거나 포함되면 true, 완전히 벗어나면 false</returns>    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LineIntersect(in Float2D p1, in Float2D p2, in FloatRect rect) =>
            Avx.IsSupported ?
            LineIntersectSimd(p1, p2, rect) :
            LineIntersectScalar(p1, p2, rect);

        /// <summary> 
        /// 선분(Line)을 사각형(Rect) 경계에 맞게 클리핑
        /// </summary> 
        /// <param name="p1">선분의 시작점</param>
        /// <param name="p2">선분의 끝점</param>
        /// <param name="rect">클리핑 기준이 되는 사각형(Bounds)</param>
        /// <param name="cp1">클리핑된 새로운 시작점</param>
        /// <param name="cp2">클리핑된 새로운 끝점</param>
        /// <returns>선분이 사각형에 걸치거나 포함되면 true, 완전히 벗어나면 false</returns>      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LineClip(in Float2D p1, in Float2D p2, in FloatRect rect, out Float2D cp1, out Float2D cp2) =>
            Avx.IsSupported ?
            LineClipSimd(p1, p2, rect, out cp1, out cp2) :
            LineClipScalar(p1, p2, rect, out cp1, out cp2);

        /// <summary> 선분(Line)의 시작점이 사각형(Rect)내부인 경우의 교차점 반환 <br/>
        /// [주의] 반드시 p1은 Rect 내부, p2는 Rect 외부가 보장된 상태에서만 정상동작한다
        /// </summary> 
        /// <param name="p1">사각형 내부에 위치한 시작점</param>
        /// <param name="p2">사각형 외부에 위치한 끝점</param>
        /// <param name="rect">클리핑 기준 사각형</param>
        /// <returns>선분이 사각형 경계와 교차하는 지점의 좌표</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float2D LineClipInsideOut(in Float2D p1, in Float2D p2, in FloatRect rect) =>
            Avx.IsSupported ?
            LineClipInsideOutSimd(p1, p2, rect) :
            LineClipInsideOutScalar(p1, p2, rect);


        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LineIntersectScalar(in Float2D p1, in Float2D p2, in FloatRect rect) =>
            LineClipCoreScalar(p1, p2, rect, out _, out _);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LineClipScalar(in Float2D p1, in Float2D p2, in FloatRect rect, out Float2D cp1, out Float2D cp2) =>
            LineClipCoreScalar(p1, p2, rect, out cp1, out cp2);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LineClipCoreScalar(in Float2D p1, in Float2D p2, in FloatRect rect, out Float2D cp1, out Float2D cp2)
        {
            Unsafe.SkipInit(out cp1);
            Unsafe.SkipInit(out cp2);

            var t1 = 0f;
            var t2 = 1f;
            float t;

            var dx = p2.X - p1.X;
            var x1 = p1.X;
            var x2 = p2.X;
            if (p1.X < p2.X)
            {
                if (!(rect.X1 <= p2.X && p1.X <= rect.X2)) { return false; }
                if (p1.X < rect.X1) { t1 = (rect.X1 - p1.X) / dx; x1 = rect.X1; }
                if (rect.X2 < p2.X) { t2 = (rect.X2 - p1.X) / dx; x2 = rect.X2; }
            }
            else if (p2.X < p1.X)
            {
                if (!(rect.X1 <= p1.X && p2.X <= rect.X2)) { return false; }
                if (rect.X2 < p1.X) { t1 = (rect.X2 - p1.X) / dx; x1 = rect.X2; }
                if (p2.X < rect.X1) { t2 = (rect.X1 - p1.X) / dx; x2 = rect.X1; }
            }
            else if (!(rect.X1 <= p1.X && p1.X <= rect.X2)) { return false; }

            var dy = p2.Y - p1.Y;
            var y1 = p1.Y;
            var y2 = p2.Y;
            if (p1.Y < p2.Y)
            {
                if (!(rect.Y1 <= p2.Y && p1.Y <= rect.Y2)) { return false; }
                if (p1.Y < rect.Y1 && t1 < (t = (rect.Y1 - p1.Y) / dy)) { t1 = t; x1 = p1.X + dx * t; y1 = rect.Y1; }
                else { y1 = p1.Y + dy * t1; }
                if (rect.Y2 < p2.Y && t2 > (t = (rect.Y2 - p1.Y) / dy)) { t2 = t; x2 = p1.X + dx * t; y2 = rect.Y2; }
                else { y2 = p1.Y + dy * t2; }
            }
            else if (p2.Y < p1.Y)
            {
                if (!(rect.Y1 <= p1.Y && p2.Y <= rect.Y2)) { return false; }
                if (rect.Y2 < p1.Y && t1 < (t = (rect.Y2 - p1.Y) / dy)) { t1 = t; x1 = p1.X + dx * t; y1 = rect.Y2; }
                else { y1 = p1.Y + dy * t1; }
                if (p2.Y < rect.Y1 && t2 > (t = (rect.Y1 - p1.Y) / dy)) { t2 = t; x2 = p1.X + dx * t; y2 = rect.Y1; }
                else { y2 = p1.Y + dy * t2; }
            }
            else if (!(rect.Y1 <= p1.Y && p1.Y <= rect.Y2)) { return false; }

            cp1 = new(x1, y1);
            cp2 = new(x2, y2);

            return t1 <= t2 && float.IsFinite(dx) && float.IsFinite(dy);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LineIntersectSimd(in Float2D p1, in Float2D p2, in FloatRect rect) =>
            LineClipCoreSimd(p1, p2, rect, out _, out _);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LineClipSimd(in Float2D p1, in Float2D p2, in FloatRect rect, out Float2D cp1, out Float2D cp2) =>
            LineClipCoreSimd(p1, p2, rect, out cp1, out cp2);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LineClipCoreSimd(in Float2D p1, in Float2D p2, in FloatRect rect, out Float2D cp1, out Float2D cp2)
        {
            var vP1 = SIMD.LoadFloat128Dup(p1);
            var vP2 = SIMD.LoadFloat128Dup(p2);
            var vRect = SIMD.LoadFloat128(rect);

            var vRectDis = Sse.Subtract(vRect, vP1);
            var vLineDis = Sse.Subtract(vP2, vP1);

            var vRatio1 = DistanceRatio(vRectDis, vLineDis);
            var vRatio2 = Sse.Shuffle(vRatio1, vRatio1, 0b_01_00_11_10);
            var vEnterXY = Sse.Min(vRatio1, vRatio2); // 각 축의 진입 시간 (t_min)
            var vLeaveXY = Sse.Max(vRatio1, vRatio2); // 각 축의 이탈 시간 (t_max)
            var vEnterYX = Sse.Shuffle(vEnterXY, vEnterXY, 0b_00_01_00_01);
            var vLeaveYX = Sse.Shuffle(vLeaveXY, vLeaveXY, 0b_00_01_00_01);
            var vEnter = Sse.Max(Sse.Max(vEnterXY, vEnterYX), Vector128<float>.Zero); // 진입 시간 (t_min)
            var vLeave = Sse.Min(Sse.Min(vLeaveXY, vLeaveYX), Vector128<float>.One); // 이탈 시간 (t_min)

            var vCP1 = Vector128.MultiplyAddEstimate(vLineDis, vEnter, vP1);
            var vCP2 = Vector128.MultiplyAddEstimate(vLineDis, vLeave, vP1);
            cp1 = SIMD.ConvertFloat2D(vCP1);
            cp2 = SIMD.ConvertFloat2D(vCP2);

            var result = false;
            if (vEnter.ToScalar() <= vLeave.ToScalar())
                result = SIMD.IsFinite(vRectDis, vLineDis);
            return result;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Float2D LineClipInsideOutScalar(in Float2D p1, in Float2D p2, in FloatRect rect)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            if (!float.IsFinite(dx) || !float.IsFinite(dy))
                return p1; // 방향 벡터가 유한하지 않으면 계산이 불가능하다고 판단하여 원점 반환

            var t2 = 1f;
            float t;

            var x2 = p2.X;
            var y2 = p2.Y;
            if (p1.X < p2.X)
            {
                if (rect.X2 < p2.X) { t2 = (rect.X2 - p1.X) / dx; x2 = rect.X2; }
            }
            else if (p2.X < p1.X)
            {
                if (p2.X < rect.X1) { t2 = (rect.X1 - p1.X) / dx; x2 = rect.X1; }
            }

            if (p1.Y < p2.Y)
            {
                if (rect.Y2 < p2.Y && t2 > (t = (rect.Y2 - p1.Y) / dy)) { x2 = p1.X + t * dx; y2 = rect.Y2; }
                else { y2 = p1.Y + dy * t2; }
            }
            else if (p2.Y < p1.Y)
            {
                if (p2.Y < rect.Y1 && t2 > (t = (rect.Y1 - p1.Y) / dy)) { x2 = p1.X + t * dx; y2 = rect.Y1; }
                else { y2 = p1.Y + dy * t2; }
            }
            return new(x2, y2);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Float2D LineClipInsideOutSimd(in Float2D p1, in Float2D p2, in FloatRect rect)
        {
            var vP1 = SIMD.LoadFloat128Dup(p1);
            var vP2 = SIMD.LoadFloat128Dup(p2);
            var vRect = SIMD.LoadFloat128(rect);

            var vRectDis = Sse.Subtract(vRect, vP1);
            var vLineDis = Sse.Subtract(vP2, vP1);
            if (SIMD.IsFinite(vLineDis) == false)
                return p1; // 방향 벡터가 유한하지 않으면 계산이 불가능하다고 판단하여 원점 반환

            var vRatio1 = DistanceRatio(vRectDis, vLineDis);
            var vRatio2 = Sse.Shuffle(vRatio1, vRatio1, 0b_01_00_11_10);
            var vLeaveXY = Sse.Max(vRatio1, vRatio2);
            var vLeaveYX = Sse.Shuffle(vLeaveXY, vLeaveXY, 0b_00_01_00_01);
            var vLeave = Sse.Min(vLeaveXY, vLeaveYX);

            return SIMD.ConvertFloat2D(Vector128.MultiplyAddEstimate(vLineDis, vLeave, vP1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> DistanceRatio(in Vector128<float> vRectDis, in Vector128<float> vLineDis)
        {
            var vRatio = Sse.Divide(vRectDis, vLineDis);
            var vBlend = Sse.CompareUnordered(vRatio, vRatio);
            if (Sse.MoveMask(vBlend) == 0)
                return vRatio;

            // NaN값을 -Inf/+Inf로 대체하여 이후 Min/Max 연산에서 자연스럽게 흘러가도록 함
            var vInfBounds = Vector128.Create(
                float.NegativeInfinity, float.NegativeInfinity,
                float.PositiveInfinity, float.PositiveInfinity);
            return Sse41.BlendVariable(vRatio, vInfBounds, vBlend);
        }
    }
}