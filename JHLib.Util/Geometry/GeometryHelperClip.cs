using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        // ============================================================================================================
        // 클리핑의 핵심 개념은 Cohen–Sutherland와 Liang–Barsky 알고리즘을 기반으로 설계
        // Cohen–Sutherland 알고리즘의 9분면 영역 판별 기법을 활용하여 좌표의 내부/외부 여부를 효율적으로 판단 후
        // Liang–Barsky 알고리즘을 통해 선분과 클리핑 경계 간의 교차 가능성을 분석하고, 교차점 좌표를 계산한다
        // 고성능을 위해 두 알고리즘을 효율적으로 조합 및 분기 처리를 최적화 하였으며, AVX가속이 가능할 경우 SIMD를 통해 성능을 극대화한다
        // 2025.5.1 WRLEE
        // ============================================================================================================

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GeoRelation GeoRelationInternal(Float2D* p0, int pn, FloatRect rect, bool area = false, bool contain = false)
        {
            var x1 = rect.X1;
            var y1 = rect.Y1;
            var x2 = rect.X2;
            var y2 = rect.Y2;

            var pc = p0 + 1;
            var pe = p0 + pn;

            var c1 = CalCode(p0, x1, y1, x2, y2);
            var cc = c1;
            var c0 = c1;

            var gr = GeoRelation.Overlap;
            if (c1 == 0) { if (contain) gr = IsContains(p0, pe, rect); return gr; }

            while (true)
            {
                var c2 = CalCode(pc, x1, y1, x2, y2); cc |= c2;

                if ((c1 & c2) != 0)
                {
                RE: pc = OutsidePass(pc + 1, pe, rect, c1 & c2);
                    c1 = CalCode(pc - 1, x1, y1, x2, y2); cc |= c1;
                    if (pc == pe) { break; }
                    c2 = CalCode(pc, x1, y1, x2, y2); cc |= c2;
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

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GeoRelation ClipInternal(Float2D* p0, int pn, FloatRect rect, SList<ClippedPath> rst, bool area = false)
        {
            var x1 = rect.X1;
            var y1 = rect.Y1;
            var x2 = rect.X2;
            var y2 = rect.Y2;

            var pc = p0 + 1;
            var pe = p0 + pn;

            var c1 = CalCode(p0, x1, y1, x2, y2);
            var cc = c1;
            var cp = false;
            var c0 = c1;

            var p1 = *p0;
            var p2 = p1;
            var pt = pc;

            while (true)
            {
                var c2 = CalCode(pc, x1, y1, x2, y2); cc |= c2;

                if ((c1 & c2) != 0)
                {
                RE: pc = OutsidePass(pc + 1, pe, rect, c1 & c2);
                    c1 = CalCode(pc - 1, x1, y1, x2, y2); cc |= c1;
                    if (pc == pe) { break; }
                    c2 = CalCode(pc, x1, y1, x2, y2); cc |= c2;
                    if ((c1 & c2) != 0) { goto RE; }
                    if ((c1 | c2) == 0) { p1 = *(pc - 1); pt = pc; }
                }

                if (c1 == OutCode.Inside)
                {
                    if (c2 == OutCode.Inside)
                    {
                        pc = InsidePass(pc + 1, pe, rect);
                        if (pc == pe) { break; }
                        c2 = CalCode(pc, x1, y1, x2, y2); cc |= c2;
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
        private static void ClippedPathMergeInternal(Float2D* p0, int pn, ClippedPath* cp0, int cpn, FloatRect rect, SList<Float2D> marea)
        {
            var x1 = rect.X1;
            var y1 = rect.Y1;
            var x2 = rect.X2;
            var y2 = rect.Y2;
            var cn = new Float2Dx4(new Float2D(x1, y1), new(x2, y1), new(x1, y2), new(x2, y2));

            var pc = p0 + 1;
            var c1 = CalCode(p0, x1, y1, x2, y2);
            var c0 = c1;

            var i = -1;
            while (true)
            {
                var pe = p0 + (++i < cpn ? (cp0 + i)->Offset : pn);
                if (pc < pe)
                {
                    do
                    {
                        var c2 = CalCode(pc, x1, y1, x2, y2);
                        if (c1 != 0)
                        {
                            if ((c1 & c2) != 0)
                            {
                            RE: if ((c1 & c1 - 1) != 0) { AddCorner(marea, cn, c1); }
                                pc = OutsidePass(pc + 1, pe, rect, c1 & c2);
                                c1 = CalCode(pc - 1, x1, y1, x2, y2);
                                if (pc == pe) { break; }
                                c2 = CalCode(pc, x1, y1, x2, y2);
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
        private static GeoRelation AreaClipAndMergeInternal(Float2D* p0, int pn, FloatRect rect, SList<ClippedPath> rst, SList<Float2D> marea)
        {
            var x1 = rect.X1;
            var y1 = rect.Y1;
            var x2 = rect.X2;
            var y2 = rect.Y2;
            var cn = new Float2Dx4(new Float2D(x1, y1), new(x2, y1), new(x1, y2), new(x2, y2));

            var pc = p0 + 1;
            var pe = p0 + pn;

            var c1 = CalCode(p0, x1, y1, x2, y2);
            var cc = c1;
            var cp = false;

            var p1 = *p0;
            var p2 = p1;
            var pt = pc;
            var c0 = c1;

            while (true)
            {
                var c2 = CalCode(pc, x1, y1, x2, y2); cc |= c2;

                if ((c1 & c2) != 0)
                {
                RE: if ((c1 & c1 - 1) != 0) { AddCorner(marea, cn, c1); }
                    pc = OutsidePass(pc + 1, pe, rect, c1 & c2);
                    c1 = CalCode(pc - 1, x1, y1, x2, y2); cc |= c1;
                    if (pc == pe) { break; }
                    c2 = CalCode(pc, x1, y1, x2, y2); cc |= c2;
                    if ((c1 & c2) != 0) { goto RE; }
                    if ((c1 | c2) == 0) { p1 = *(pc - 1); pt = pc; }
                }

                if (c1 == OutCode.Inside)
                {
                    if (c2 == OutCode.Inside)
                    {
                        pc = InsidePass(pc + 1, pe, rect);
                        if (pc == pe) { break; }
                        c2 = CalCode(pc, x1, y1, x2, y2); cc |= c2;
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

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OutCode CalCode(Float2D* p, float x1, float y1, float x2, float y2)
        {
            var c = OutCode.Inside;

            var t = p->X;
            if (t >= x1 == false) c = OutCode.Left;
            else if (t <= x2 == false) c = OutCode.Right;

            t = p->Y;
            if (t >= y1 == false) c |= OutCode.Top;
            else if (t <= y2 == false) c |= OutCode.Bottom;

            return c;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GeoRelation IsContains(Float2D* p, Float2D* e, in FloatRect rect)
        {
            var x1 = rect.X1;
            var y1 = rect.Y1;
            var x2 = rect.X2;
            var y2 = rect.Y2;

            var r = GeoRelation.Overlap;
            if (p <= e - 4)
            {
                do
                {
                    if ((x1 <= p[0].X && p[0].X <= x2 && y1 <= p[0].Y && p[0].Y <= y2) == false) { goto EX; }
                    if ((x1 <= p[1].X && p[1].X <= x2 && y1 <= p[1].Y && p[1].Y <= y2) == false) { goto EX; }
                    if ((x1 <= p[2].X && p[2].X <= x2 && y1 <= p[2].Y && p[2].Y <= y2) == false) { goto EX; }
                    if ((x1 <= p[3].X && p[3].X <= x2 && y1 <= p[3].Y && p[3].Y <= y2) == false) { goto EX; }
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e)
            {
                do if ((x1 <= p->X && p->X <= x2 && y1 <= p->Y && p->Y <= y2) == false) { goto EX; }
                while (++p < e);
            }
            r = GeoRelation.Contains;
        EX: return r;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Float2D* InsidePass(Float2D* p, Float2D* e, in FloatRect rect)
        {
            var x1 = rect.X1;
            var y1 = rect.Y1;
            var x2 = rect.X2;
            var y2 = rect.Y2;

            if (p <= e - 4)
            {
                do
                {
                    if ((x1 <= p[0].X && p[0].X <= x2 && y1 <= p[0].Y && p[0].Y <= y2) == false) { return p; }
                    if ((x1 <= p[1].X && p[1].X <= x2 && y1 <= p[1].Y && p[1].Y <= y2) == false) { return p + 1; }
                    if ((x1 <= p[2].X && p[2].X <= x2 && y1 <= p[2].Y && p[2].Y <= y2) == false) { return p + 2; }
                    if ((x1 <= p[3].X && p[3].X <= x2 && y1 <= p[3].Y && p[3].Y <= y2) == false) { return p + 3; }
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e)
            {
                while (x1 <= p->X && p->X <= x2 && y1 <= p->Y && p->Y <= y2 && ++p < e) ;
            }
            return p;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Float2D* OutsidePass(Float2D* p, Float2D* e, in FloatRect rect, OutCode oc)
        {
            if ((oc & OutCode.Top) != 0)
                return LEYPass(p, e, rect.Y1);
            else if ((oc & OutCode.Left) != 0)
                return LEXPass(p, e, rect.X1);
            else if ((oc & OutCode.Bottom) != 0)
                return GEYPass(p, e, rect.Y2);
            else
                return GEXPass(p, e, rect.X2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* LEYPass(Float2D* p, Float2D* e, float y)
        {
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
            if (p < e)
            {
                while (y < p->Y == false && ++p < e) ;
            }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* LEXPass(Float2D* p, Float2D* e, float x)
        {
            if (p <= e - 4)
            {
                do
                {
                    if (x < (p + 0)->X) { return p; }
                    if (x < (p + 1)->X) { return p + 1; }
                    if (x < (p + 2)->X) { return p + 2; }
                    if (x < (p + 3)->X) { return p + 3; }
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e)
            {
                while (x < p->X == false && ++p < e) ;
            }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* GEYPass(Float2D* p, Float2D* e, float y)
        {
            if (p <= e - 4)
            {
                do
                {
                    if ((p + 0)->Y < y) { return p; }
                    if ((p + 1)->Y < y) { return p + 1; }
                    if ((p + 2)->Y < y) { return p + 2; }
                    if ((p + 3)->Y < y) { return p + 3; }
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e)
            {
                while (p->Y < y == false && ++p < e) ;
            }
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Float2D* GEXPass(Float2D* p, Float2D* e, float x)
        {
            if (p <= e - 4)
            {
                do
                {
                    if ((p + 0)->X < x) { return p; }
                    if ((p + 1)->X < x) { return p + 1; }
                    if ((p + 2)->X < x) { return p + 2; }
                    if ((p + 3)->X < x) { return p + 3; }
                }
                while ((p += 4) <= e - 4);
            }
            if (p < e)
            {
                while (p->X < x == false && ++p < e) ;
            }
            return p;
        }
    }
}