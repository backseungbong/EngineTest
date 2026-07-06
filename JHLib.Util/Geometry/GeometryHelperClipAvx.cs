using JHLib.Util.List;
using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        // ============================================================================================================
        // 클리핑의 핵심 개념은 Cohen–Sutherland와 Liang–Barsky 알고리즘을 기반으로 설계
        // Cohen–Sutherland 알고리즘의 9분면 영역 판별 기법을 활용하여 좌표의 내부/외부 여부를 효율적으로 판단후
        // Liang–Barsky 알고리즘을 통해 선분과 클리핑 경계 간의 교차 가능성을 분석하고, 교차점 좌표를 계산한다
        // 고성능을 위해 두 알고리즘을 효율적으로 조합 및 분기 처리를 최적화 하였으며, AVX가속이 가능할 경우 SIMD를 통해 성능을 극대화한다
        // 2025.5.1 WRLEE
        // ============================================================================================================

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GeoRelation GeoRelationInternalAvx(Float2D* p0, int pn, FloatRect rect, bool area = false, bool contain = false)
        {
            var lt = SIMD.LoadFloat256Dup(rect.P1);
            var rb = SIMD.LoadFloat256Dup(rect.P2);

            var pc = p0 + 1;
            var pe = p0 + pn;

            var c1 = CalCodeAvx(p0, lt, rb);
            var cc = c1;
            var c0 = c1;

            var gr = GeoRelation.Overlap;
            if (c1 == 0) { if (contain) gr = IsContainsAvx(p0, pe, lt, rb); return gr; }

            while (true)
            {
                var c2 = CalCodeAvx(pc, lt, rb); cc |= c2;

                if ((c1 & c2) != 0)
                {
                RE: pc = OutsidePassAvx(pc + 1, pe, lt, rb, c1 & c2);
                    c1 = CalCodeAvx(pc - 1, lt, rb); cc |= c1;
                    if (pc == pe) { break; }
                    c2 = CalCodeAvx(pc, lt, rb); cc |= c2;
                    if ((c1 & c2) != 0) { goto RE; }
                }

                c1 = c2;
                if (c1 == 0 || LineIntersect(*(pc - 1), *pc, rect)) { return gr; }
                if (++pc == pe) { break; }
            }

            if (area && pn >= 3)
            {
                if (c1 != c0 && LineIntersect(*(pe - 1), *p0, rect)) { return gr; }
                if (cc == OutCode.All)
                {
                    gr = GeoRelation.ContainedBy;
                    if (PointInPolygonWindingInternal(rect.P1, p0, pn)) { return gr; }
                }
            }
            return GeoRelation.Disjoint;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GeoRelation ClipInternalAvx(Float2D* p0, int pn, FloatRect rect, SList<ClippedPath> rst, bool area = false)
        {
            var lt = SIMD.LoadFloat256Dup(rect.P1);
            var rb = SIMD.LoadFloat256Dup(rect.P2);

            var pc = p0 + 1;
            var pe = p0 + pn;

            var c1 = CalCodeAvx(p0, lt, rb);
            var cc = c1;
            var cp = false;
            var c0 = c1;

            var p1 = *p0;
            var p2 = p1;
            var pt = pc;

            while (true)
            {
                var c2 = CalCodeAvx(pc, lt, rb); cc |= c2;

                if ((c1 & c2) != 0)
                {
                RE: pc = OutsidePassAvx(pc + 1, pe, lt, rb, c1 & c2);
                    c1 = CalCodeAvx(pc - 1, lt, rb); cc |= c1;
                    if (pc == pe) { break; }
                    c2 = CalCodeAvx(pc, lt, rb); cc |= c2;
                    if ((c1 & c2) != 0) { goto RE; }
                    if ((c1 | c2) == 0) { p1 = *(pc - 1); pt = pc; }
                }

                if (c1 == OutCode.Inside)
                {
                    if (c2 == OutCode.Inside)
                    {
                        pc = InsidePassAvx(pc + 1, pe, lt, rb);
                        if (pc == pe) { break; }
                        c2 = CalCodeAvx(pc, lt, rb); cc |= c2;
                    }

                    c1 = c2;
                    p2 = LineClipInsideOut(*(pc - 1), *pc, rect);
                    AddCPath(rst, p1, p2, p0, pt, pc); cp = true;
                    if (++pc == pe) { break; }
                }
                else
                {
                    if (c2 == OutCode.Inside)
                    {
                        c1 = c2;
                        pt = pc; p1 = LineClipInsideOut(*pc, *(pc - 1), rect);
                        if (++pc == pe) { break; }
                    }
                    else
                    {
                        c1 = c2;
                        if (LineClip(*(pc - 1), *pc, rect, out p1, out p2)) { AddCPath(rst, p1, p2, p0, pc); cp = true; }
                        if (++pc == pe) { break; }
                    }
                }
            }

            var gr = GeoRelation.Contains;
            if (cc != 0)
            {
                if (c1 == 0) { AddCPath(rst, p1, *(pe - 1), p0, pt, pe - 1); cp = true; }
                if (cp) { gr = GeoRelation.Overlap; }
                else
                {
                    gr = GeoRelation.Disjoint;
                    if (area && pn >= 3)
                    {
                        if (c1 != c0 && LineIntersect(*(pe - 1), *p0, rect))
                            gr = GeoRelation.Overlap;
                        else if (cc == OutCode.All && PointInPolygonWindingInternal(rect.P1, p0, pn))
                            gr = GeoRelation.ContainedBy;
                    }
                }
            }
            return gr;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ClippedPathMergeInternalAvx(Float2D* p0, int pn, ClippedPath* cp0, int cpn, FloatRect rect, SList<Float2D> marea)
        {
            var lt = SIMD.LoadFloat256Dup(rect.P1);
            var rb = SIMD.LoadFloat256Dup(rect.P2);
            LoadCornerAvx(rect, out var cn);

            var pc = p0 + 1;
            var c1 = CalCodeAvx(p0, lt, rb);
            var c0 = c1;

            var i = -1;
            while (true)
            {
                var pe = p0 + (++i < cpn ? (cp0 + i)->Offset : pn);
                if (pc < pe)
                {
                    do
                    {
                        var c2 = CalCodeAvx(pc, lt, rb);
                        if (c1 != 0)
                        {
                            if ((c1 & c2) != 0)
                            {
                            RE: if ((c1 & c1 - 1) != 0) { AddCorner(marea, cn, c1); }
                                pc = OutsidePassAvx(pc + 1, pe, lt, rb, c1 & c2);
                                c1 = CalCodeAvx(pc - 1, lt, rb);
                                if (pc == pe) { break; }
                                c2 = CalCodeAvx(pc, lt, rb);
                                if ((c1 & c2) != 0) { goto RE; }
                            }
                            if ((c1 & c1 - 1) != 0) { AddCorner(marea, cn, c1); }
                            if (c2 != 0) { AddCorner(marea, cn, CalCorner(pc, c1, c2, rect)); }
                        }
                        c1 = c2;
                    }
                    while (++pc < pe);
                }

                if (i >= cpn)
                {
                    if (c1 != c0) { ProcessLastPoint(pe - 1, p0, c1, c0, rect, cn, marea); }
                    return;
                }
                else
                {
                    if ((c1 & c1 - 1) != 0) { AddCorner(marea, cn, c1); }
                    var cp = cp0 + i;
                    cp->CopyTo(p0, marea);
                    pc = p0 + cp->EndPosition;
                    c1 = OutCode.Inside;
                }
            }
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GeoRelation AreaClipAndMergeInternalAvx(Float2D* p0, int pn, FloatRect rect, SList<ClippedPath> rst, SList<Float2D> marea)
        {
            var lt = SIMD.LoadFloat256Dup(rect.P1);
            var rb = SIMD.LoadFloat256Dup(rect.P2);
            LoadCornerAvx(rect, out var cn);

            var pc = p0 + 1;
            var pe = p0 + pn;

            var c1 = CalCodeAvx(p0, lt, rb);
            var cc = c1;
            var cp = false;

            var p1 = *p0;
            var p2 = p1;
            var pt = pc;
            var c0 = c1;

            while (true)
            {
                var c2 = CalCodeAvx(pc, lt, rb); cc |= c2;

                if ((c1 & c2) != 0)
                {
                RE: if ((c1 & c1 - 1) != 0) { AddCorner(marea, cn, c1); }
                    pc = OutsidePassAvx(pc + 1, pe, lt, rb, c1 & c2);
                    c1 = CalCodeAvx(pc - 1, lt, rb); cc |= c1;
                    if (pc == pe) { break; }
                    c2 = CalCodeAvx(pc, lt, rb); cc |= c2;
                    if ((c1 & c2) != 0) { goto RE; }
                    if ((c1 | c2) == 0) { p1 = *(pc - 1); pt = pc; }
                }

                if (c1 == OutCode.Inside)
                {
                    if (c2 == OutCode.Inside)
                    {
                        pc = InsidePassAvx(pc + 1, pe, lt, rb);
                        if (pc == pe) { break; }
                        c2 = CalCodeAvx(pc, lt, rb); cc |= c2;
                    }

                    c1 = c2;
                    p2 = LineClipInsideOut(*(pc - 1), *pc, rect);
                    AddCPathCorner(rst, marea, p1, p2, p0, pt, pc); cp = true;
                    if (++pc == pe) { break; }
                }
                else
                {
                    if ((c1 & c1 - 1) != 0) { AddCorner(marea, cn, c1); }
                    if (c2 == OutCode.Inside)
                    {
                        c1 = c2;
                        pt = pc; p1 = LineClipInsideOut(*pc, *(pc - 1), rect);
                        if (++pc == pe) { break; }
                    }
                    else if (LineClip(*(pc - 1), *pc, rect, out p1, out p2))
                    {
                        c1 = c2;
                        AddCPathCorner(rst, marea, p1, p2, p0, pc); cp = true;
                        if (++pc == pe) { break; }
                    }
                    else
                    {
                        AddCorner(marea, cn, CalCorner(pc, c1, c2, rect));
                        c1 = c2;
                        if (++pc == pe) { break; }
                    }
                }
            }

            var gr = GeoRelation.Contains;
            if (cc != 0)
            {
                gr = GeoRelation.Overlap;
                if (c1 == 0) { AddCPathCorner(rst, marea, p1, *(pe - 1), p0, pt, pe - 1); cp = true; }
                if (c1 != c0 && ProcessLastPoint(pe - 1, p0, c1, c0, rect, cn, marea)) { cp = true; }
                if (cp == false)
                {
                    gr = GeoRelation.Disjoint;
                    if (cc == OutCode.All && IsPolygonDegenerate(marea) == false)
                        gr = GeoRelation.ContainedBy;
                }
            }
            return gr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadCornerAvx(in FloatRect rect, out Float2Dx4 corner)
        {
            Unsafe.SkipInit(out corner);
            var vr = SIMD.LoadFloat128(rect);
            var p12 = Sse.Shuffle(vr, vr, 0b_01_10_01_00);
            var p34 = Sse.Shuffle(vr, vr, 0b_11_10_11_00);
            Unsafe.As<Float2D, Vector128<float>>(ref corner.P1) = p12;
            Unsafe.As<Float2D, Vector128<float>>(ref corner.P3) = p34;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OutCode CalCodeAvx(Float2D* p, in Vector256<float> lt, in Vector256<float> rb)
        {
            var vv = SIMD.LoadFloat128Dup(*p);
            var c1 = Sse.CompareNotGreaterThanOrEqual(vv, lt.GetLower());
            var c2 = Sse.CompareNotLessThanOrEqual(vv, rb.GetLower());
            return (OutCode)Sse.MoveMask(Sse41.Blend(c1, c2, 0b_1100));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* InsidePassAvx(Float2D* p, Float2D* e, in Vector256<float> lt, in Vector256<float> rb)
        {
            if (p <= e - 4)
            {
                do
                {
                    var vv = Avx.LoadVector256((float*)p);
                    var c1 = Avx.CompareNotGreaterThanOrEqual(vv, lt);
                    var c2 = Avx.CompareNotLessThanOrEqual(vv, rb);
                    var cc = Avx.Or(c1, c2);
                    var mm = Avx.MoveMask(cc);
                    if (mm != 0) return p + ((uint)BitOperations.TrailingZeroCount(mm) >> 1);
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e)
            {
                do
                {
                    var vv = Sse2.LoadScalarVector128((double*)p).AsSingle();
                    var c1 = Sse.CompareNotGreaterThanOrEqual(vv, lt.GetLower());
                    var c2 = Sse.CompareNotLessThanOrEqual(vv, rb.GetLower());
                    var cc = Sse.Or(c1, c2);
                    if (cc.AsUInt64().ToScalar() != 0) break;
                }
                while (++p < e);
            }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GeoRelation IsContainsAvx(Float2D* p, Float2D* e, in Vector256<float> lt, in Vector256<float> rb)
        {
            var r = GeoRelation.Overlap;
            if (p <= e - 4)
            {
                do
                {
                    var vv = Avx.LoadVector256((float*)p);
                    var c1 = Avx.CompareNotGreaterThanOrEqual(vv, lt);
                    var c2 = Avx.CompareNotLessThanOrEqual(vv, rb);
                    var cc = Avx.Or(c1, c2);
                    var mm = Avx.MoveMask(cc);
                    if (mm != 0) { goto EX; }
                }
                while ((p += 4) < e - 4);
            }
            if (p < e)
            {
                do
                {
                    var vv = Sse2.LoadScalarVector128((double*)p).AsSingle();
                    var c1 = Sse.CompareNotGreaterThanOrEqual(vv, lt.GetLower());
                    var c2 = Sse.CompareNotLessThanOrEqual(vv, rb.GetLower());
                    var cc = Sse.Or(c1, c2);
                    if (cc.AsUInt64().ToScalar() != 0) { goto EX; }
                }
                while (++p < e);
            }
            r = GeoRelation.Contains;
        EX: return r;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* OutsidePassAvx(Float2D* p, Float2D* e, in Vector256<float> lt, in Vector256<float> rb, OutCode oc)
        {
            if ((oc & OutCode.LT) != 0)
                return LEPassAvx(p, e, lt, oc);
            else
                return GEPassAvx(p, e, rb, oc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* LEPassAvx(Float2D* p, Float2D* e, in Vector256<float> lt, OutCode oc)
        {
            var xyMask = 0b_01010101 << ((int)(oc & OutCode.LT) >> 1);
            if (p <= e - 4)
            {
                do
                {
                    var vv = Avx.LoadVector256((float*)p);
                    var cc = Avx.CompareGreaterThan(vv, lt);
                    var mm = Avx.MoveMask(cc) & xyMask;
                    if (mm != 0) return p + ((uint)BitOperations.TrailingZeroCount(mm) >> 1);
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e)
            {
                do
                {
                    var vv = Sse2.LoadScalarVector128((double*)p).AsSingle();
                    var cc = Sse.CompareGreaterThan(vv, lt.GetLower());
                    var mm = Sse.MoveMask(cc) & xyMask & 0b_11;
                    if (mm != 0) break;
                }
                while (++p < e);
            }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* GEPassAvx(Float2D* p, Float2D* e, in Vector256<float> rb, OutCode oc)
        {
            var xyMask = 0b_01010101 << ((int)(oc & OutCode.RB) >> 3);
            if (p <= e - 4)
            {
                do
                {
                    var vv = Avx.LoadVector256((float*)p);
                    var cc = Avx.CompareLessThan(vv, rb);
                    var mm = Avx.MoveMask(cc) & xyMask;
                    if (mm != 0) return p + ((uint)BitOperations.TrailingZeroCount(mm) >> 1);
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e)
            {
                do
                {
                    var vv = Sse2.LoadScalarVector128((double*)p).AsSingle();
                    var cc = Sse.CompareLessThan(vv, rb.GetLower());
                    var mm = Sse.MoveMask(cc) & xyMask & 0b_11;
                    if (mm != 0) break;
                }
                while (++p < e);
            }
            return p;
        }
    }
}