using JHLib.Util.DataStream;
using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        /// <summary> DataHeaderWriter 인자 전용 추가 함수 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GeoRelation ClipInternal(Float2D* p0, int pn, FloatRect rect, in DataHeaderWriter rst, bool area = false)
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

        /// <summary> DataHeaderWriter 인자 전용 추가 함수 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ClippedPathMergeInternal(Float2D* p0, int pn, ClippedPath* cp0, int cpn, FloatRect rect, in DataHeaderWriter marea)
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

        /// <summary> DataHeaderWriter 인자 전용 추가 함수 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GeoRelation ClipInternalAvx(Float2D* p0, int pn, FloatRect rect, in DataHeaderWriter rst, bool area = false)
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

        /// <summary> DataHeaderWriter 인자 전용 추가 함수 </summary>
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ClippedPathMergeInternalAvx(Float2D* p0, int pn, ClippedPath* cp0, int cpn, FloatRect rect, in DataHeaderWriter marea)
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
    }
}