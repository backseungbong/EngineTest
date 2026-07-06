using JHLib.Util.Geometry;
using JHLib.Util.Simd;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Struct
{
    /// <summary>
    /// 2D 공간에서의 회전된 바운딩 박스(Oriented Bounding Box, OBB)를 표현하고 교차 검사 수행<br/>
    /// 성능 최적화를 위해 생성 시점에 회전 축과 그 절댓값을 미리 계산하여 캐싱
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FloatOBB
    {
        public FloatExtents Extents;
        public readonly Float2D AxisX;
        public readonly Float2D AxisY;
        public readonly Float2D AxisXAbs;
        public readonly Float2D AxisYAbs;

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatOBB(in FloatRect rect, float rotation)
        {
            rect.ToExtents(out Extents);

            var (sin, cos) = MathF.SinCos(rotation * (MathF.PI / 180f));
            var sinabs = MathF.Abs(sin);
            var cosabs = MathF.Abs(cos);
            AxisX = new Float2D(cos, sin);
            AxisY = new Float2D(-sin, cos);
            AxisXAbs = new Float2D(cosabs, sinabs);
            AxisYAbs = new Float2D(sinabs, cosabs);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatOBB(in FloatExtents extents, float rotation)
        {
            Extents = extents;

            var (sin, cos) = MathF.SinCos(rotation * (MathF.PI / 180f));
            var sinabs = MathF.Abs(sin);
            var cosabs = MathF.Abs(cos);
            AxisX = new Float2D(cos, sin);
            AxisY = new Float2D(-sin, cos);
            AxisXAbs = new Float2D(cosabs, sinabs);
            AxisYAbs = new Float2D(sinabs, cosabs);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatOBB(Float2D center, Float2D half, float rotation)
        {
            Extents.Center = center;
            Extents.HalfExtents = half;

            var (sin, cos) = MathF.SinCos(rotation * (MathF.PI / 180f));
            var sinabs = Math.Abs(sin);
            var cosabs = Math.Abs(cos);
            AxisX = new Float2D(cos, sin);
            AxisY = new Float2D(-sin, cos);
            AxisXAbs = new Float2D(cosabs, sinabs);
            AxisYAbs = new Float2D(sinabs, cosabs);
        }

        /// <summary>
        /// 대상 AABB(FloatRect)가 현재 OBB의 로컬 축(Local Axes) 상에서 교차하는지 검사한다<br/>
        /// 결과는 대상을 기준으로 OBB과의 관계(Disjoint, Overlap, Contains)를 반환된다
        /// </summary>
        /// <remarks>
        /// [중요] 분리 축 이론(SAT) 조건검사에서 3, 4번 조건(OBB의 두 로컬 축)만 검사<br/>
        /// AABB의 축에 대한 교차검사(1, 2번 조건)는 호출자 측에서 선행되어야 정상적인 결과를 보장한다<br/>
        /// - AABB의 X축 (검사X) <br/>
        /// - AABB의 Y축 (검사X) <br/>
        /// - OBB의 X축 (검사O)<br/>
        /// - OBB의 Y축 (검사O)<br/>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly GeoRelation TestLocalAxes(in FloatRect rect) =>
            Sse2.IsSupported ? RelationSIMD(rect) : RelationScalar(rect);

        /// <summary>
        /// 대상 AABB의 중심점과 절반 크기를 받아 현재 OBB의 로컬 축 상에서 교차하는지 검사한다<br/>
        /// 결과는 대상을 기준으로 OBB과의 관계(Disjoint, Overlap, Contains)를 반환된다
        /// </summary>
        /// <remarks>
        /// [중요] 분리 축 이론(SAT) 조건검사에서 3, 4번 조건(OBB의 두 로컬 축)만 검사<br/>
        /// AABB의 축에 대한 교차검사(1, 2번 조건)는 호출자 측에서 선행되어야 정상적인 결과를 보장한다<br/>
        /// - AABB의 X축 (검사X) <br/>
        /// - AABB의 Y축 (검사X) <br/>
        /// - OBB의 X축 (검사O)<br/>
        /// - OBB의 Y축 (검사O)<br/>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly GeoRelation TestLocalAxes(in FloatExtents extent) =>
            Sse2.IsSupported ? RelationSIMD(extent) : RelationScalar(extent);

        /// <summary>
        /// 대상 AABB의 중심점과 절반 크기를 받아 현재 OBB의 로컬 축 상에서 교차하는지 검사한다<br/>
        /// 결과는 대상을 기준으로 OBB과의 관계(Disjoint, Overlap, Contains)를 반환된다
        /// </summary>
        /// <remarks>
        /// [중요] 분리 축 이론(SAT) 조건검사에서 3, 4번 조건(OBB의 두 로컬 축)만 검사<br/>
        /// AABB의 축에 대한 교차검사(1, 2번 조건)는 호출자 측에서 선행되어야 정상적인 결과를 보장한다<br/>
        /// - AABB의 X축 (검사X) <br/>
        /// - AABB의 Y축 (검사X) <br/>
        /// - OBB의 X축 (검사O)<br/>
        /// - OBB의 Y축 (검사O)<br/>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly GeoRelation TestLocalAxes(in Float2D center, in Float2D half) =>
            Sse2.IsSupported ? RelationSIMD(center, half) : RelationScalar(center, half);



        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly GeoRelation RelationScalar(in FloatRect rect)
        {
            var ex = rect.ToExtents();
            return RelationScalarCore(ex.Center, ex.HalfExtents);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly GeoRelation RelationScalar(in FloatExtents extent) =>
            RelationScalarCore(extent.Center, extent.HalfExtents);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly GeoRelation RelationScalar(in Float2D center, in Float2D half) =>
            RelationScalarCore(center, half);

        /// <summary> 스칼라(Scalar) 기반의 OBB 3, 4번 축 교차 및 포함 판정 핵심 로직 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly GeoRelation RelationScalarCore(Float2D center, Float2D half)
        {
            var dx = Extents.Center.X - center.X;
            var dy = Extents.Center.Y - center.Y;

            // OBB의 X축 검사
            // AABB의 절반 크기를 OBB의 X축 상에 투영 및 검사
            var halfPrjX = (half.X * AxisXAbs.X) + (half.Y * AxisXAbs.Y);
            var prjX = MathF.Abs(dx * AxisX.X + dy * AxisX.Y);
            if (prjX <= halfPrjX + Extents.HalfExtents.X == false) // NaN 값 대비를 위한 부정조건문 사용
                return GeoRelation.Disjoint;

            // OBB의 Y축 검사
            // AABB의 절반 크기를 OBB의 Y축 상에 투영 및 검사
            var halfPrjY = (half.X * AxisYAbs.X) + (half.Y * AxisYAbs.Y);
            var prjY = MathF.Abs(dx * AxisY.X + dy * AxisY.Y);
            if (prjY <= halfPrjY + Extents.HalfExtents.Y == false) // NaN 값 대비를 위한 부정조건문 사용
                return GeoRelation.Disjoint;

            // 포함 검사
            // (중심 거리 투영 길이 + AABB의 투영 반경)이 현재 OBB의 고유 반경(Half) 이내라면
            // AABB가 OBB내부의 해당 축 구간에 완전히 포함됨을 의미
            if (prjX + halfPrjX <= Extents.HalfExtents.X && prjY + halfPrjY <= Extents.HalfExtents.Y)
                return GeoRelation.Contains;

            return GeoRelation.Overlap;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly GeoRelation RelationSIMD(in FloatRect rect)
        {
            var vRectP1 = SIMD.LoadFloat128Dup(rect.P1);
            var vRectP2 = SIMD.LoadFloat128Dup(rect.P2);
            var vHalfExtents = Sse.Multiply(Sse.Subtract(vRectP2, vRectP1), Vector128.Create(0.5f));
            var vCenter = Sse.Add(vRectP1, vHalfExtents);
            return RelationSIMDCore(vCenter, vHalfExtents);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly GeoRelation RelationSIMD(in FloatExtents extent) =>
            RelationSIMDCore(SIMD.LoadFloat128Dup(extent.Center), SIMD.LoadFloat128Dup(extent.HalfExtents));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly GeoRelation RelationSIMD(in Float2D center, in Float2D half) =>
            RelationSIMDCore(SIMD.LoadFloat128Dup(center), SIMD.LoadFloat128Dup(half));

        /// <summary> SIMD 기반의 OBB 3, 4번 축 교차 및 포함 판정 핵심 로직, X축과 Y축 계산을 한 번에 수행 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly GeoRelation RelationSIMDCore(in Vector128<float> center, in Vector128<float> half)
        {
            var vPrjAABB = Dot(half, SIMD.LoadFloat128(AxisXAbs));

            var vHalf = SIMD.LoadFloat128Scalar(Extents.HalfExtents);
            var vHalfOBB = Sse.Shuffle(vHalf, vHalf, 0b_01_01_00_00); // x,x,y,y 형태로 복제

            var vDist = Sse.Subtract(center, SIMD.LoadFloat128Dup(Extents.Center));
            var vPrjDist = Vector128.Abs(Dot(vDist, SIMD.LoadFloat128(AxisX)));

            var vSumExtents = Sse.Add(vPrjAABB, vHalfOBB);
            if (Sse.MoveMask(Sse.CompareLessThanOrEqual(vPrjDist, vSumExtents)) != 0b_1111)
                return GeoRelation.Disjoint;

            var vPrjDistMax = Sse.Add(vPrjDist, vPrjAABB);
            if (Sse.MoveMask(Sse.CompareLessThanOrEqual(vPrjDistMax, vHalfOBB)) != 0b_1111)
                return GeoRelation.Overlap;

            return GeoRelation.Contains;
        }

        // 2D 내적(Dot Product) 함수
        // 수평 덧셈(HADD)을 사용하는 대신, Shuffle과 Add로 파이프라인 지연을 최소화
        // 결과는 x1+y1, y1+x1, x2+y2, y2+x2를 유도하며 a,a,b,b 형태의 값으로 반환됨
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> Dot(in Vector128<float> src, in Vector128<float> mul)
        {
            var v1 = Sse.Multiply(src, mul);
            var v2 = Sse.Shuffle(v1, v1, 0b_10_11_00_01);
            var v3 = Sse.Add(v1, v2);
            return v3;
        }
    }
}