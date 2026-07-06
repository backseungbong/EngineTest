namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        //public static Float2D GetPolyLabel(PoolStreamBucket streams, FloatRect[] mbrs, Float2D centroid, float precision)
        //{
        //    var bd = mbrs[0];
        //    var dx = Math.Max(bd.DX, precision);
        //    var dy = Math.Max(bd.DY, precision);
        //    var cs = dx < dy ? dx : dy;

        //    var heap = new MaxHeapCell();
        //    var xnum = (int)Math.Ceiling(dx / cs);
        //    var ynum = (int)Math.Ceiling(dy / cs);

        //    var half = cs / 2;
        //    var cpos = bd.Center;
        //    var xoff = bd.X1 + half - ((xnum * cs - dx) * 0.5f);
        //    var yoff = bd.Y1 + half - ((ynum * cs - dy) * 0.5f);

        //    dx = cpos.X - centroid.X;
        //    dy = cpos.Y - centroid.Y;

        //    var cenfac = -precision / half;
        //    var cendis = (float)Math.Sqrt(dx * dx + dy * dy) * cenfac;
        //    var dist = GetDis(cpos.X, cpos.Y, streams, mbrs, cs);
        //    var temp = new Cell(cpos.X, cpos.Y, half, InPoly(cpos.X, cpos.Y, streams, mbrs) ? cendis + dist : cendis - dist);
        //    var best = temp;

        //    for (var r = 0; r < ynum; r++)
        //    {
        //        for (var c = 0; c < xnum; c++)
        //        {
        //            var x = xoff + c * cs;
        //            var y = yoff + r * cs;

        //            dx = x - centroid.X;
        //            dy = y - centroid.Y;
        //            cendis = (float)Math.Sqrt(dx * dx + dy * dy) * cenfac;

        //            if (InPoly(x, y, streams, mbrs))
        //            {
        //                dist = cendis + GetDis(x, y, streams, mbrs, cs);
        //                temp = new Cell(x, y, half, dist);
        //                if (dist > best.Dis) best = temp;
        //                heap.Add(temp);
        //            }
        //            else if (half * Cell.SQUARE > best.Dis)
        //            {
        //                dist = cendis - GetDis(x, y, streams, mbrs, cs);
        //                temp = new Cell(x, y, half, dist);
        //                if (dist > best.Dis) best = temp;
        //                heap.Add(temp);
        //            }
        //        }
        //    }

        //    var quad = stackalloc float[8];
        //    var qend = quad + 8;
        //    while (true)
        //    {
        //        if (heap.TryPop(out var cell) == false) return new(best.X, best.Y);
        //        if (cell.Max - best.Dis > precision)
        //        {
        //            half = cell.Half * 0.5f;
        //            quad[0] = cell.X - half; quad[1] = cell.Y - half;
        //            quad[2] = cell.X + half; quad[3] = cell.Y - half;
        //            quad[4] = cell.X - half; quad[5] = cell.Y + half;
        //            quad[6] = cell.X + half; quad[7] = cell.Y + half;

        //            var t = quad;
        //            if (half * Cell.SQUARE < Math.Abs(cell.Dis))
        //            {
        //                if (cell.Dis >= 0)
        //                {
        //                    do
        //                    {
        //                        dx = t[0] - centroid.X;
        //                        dy = t[1] - centroid.Y;
        //                        cendis = (float)Math.Sqrt(dx * dx + dy * dy) * cenfac;

        //                        dist = cendis + GetDis(t[0], t[1], streams, mbrs, cell.Max);
        //                        temp = new Cell(t[0], t[1], half, dist);
        //                        if (dist > best.Dis) best = temp;
        //                        heap.Add(temp);
        //                    }
        //                    while ((t += 2) < qend);
        //                }
        //                else
        //                {
        //                    do
        //                    {
        //                        dx = t[0] - centroid.X;
        //                        dy = t[1] - centroid.Y;
        //                        cendis = (float)Math.Sqrt(dx * dx + dy * dy) * cenfac;

        //                        if (half * Cell.SQUARE <= best.Dis) break;
        //                        dist = cendis - GetDis(t[0], t[1], streams, mbrs, cell.Max);
        //                        temp = new Cell(t[0], t[1], half, dist);
        //                        if (dist > best.Dis) best = temp;
        //                        heap.Add(temp);
        //                    }
        //                    while ((t += 2) < qend);
        //                }
        //            }
        //            else
        //            {
        //                do
        //                {
        //                    if (InPoly(t[0], t[1], streams, mbrs))
        //                    {
        //                        dx = t[0] - centroid.X;
        //                        dy = t[1] - centroid.Y;
        //                        cendis = (float)Math.Sqrt(dx * dx + dy * dy) * cenfac;

        //                        dist = cendis + GetDis(t[0], t[1], streams, mbrs, cell.Max);
        //                        temp = new Cell(t[0], t[1], half, dist);
        //                        if (dist > best.Dis) best = temp;
        //                        heap.Add(temp);
        //                    }
        //                    else if (half * Cell.SQUARE > best.Dis)
        //                    {
        //                        dx = t[0] - centroid.X;
        //                        dy = t[1] - centroid.Y;
        //                        cendis = (float)Math.Sqrt(dx * dx + dy * dy) * cenfac;

        //                        dist = cendis - GetDis(t[0], t[1], streams, mbrs, cell.Max);
        //                        temp = new Cell(t[0], t[1], half, dist);
        //                        if (dist > best.Dis) best = temp;
        //                        heap.Add(temp);
        //                    }
        //                }
        //                while ((t += 2) < qend);
        //            }
        //        }
        //    }
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private unsafe static bool InPoly(float x, float y, PoolStreamBucket srs, FloatRect[] bds)
        //{
        //    var r = false;
        //    var i = 0;
        //    do r ^= bds[i].X1 <= x && x <= bds[i].X2 &&
        //            bds[i].Y1 <= y && y <= bds[i].Y2 &&
        //            PointInPolygonWinding(x, y, srs[i].DataRange<Float2D>());
        //    while (++i < srs.Count);
        //    return r;
        //}

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //private unsafe static float GetDis(float x, float y, PoolStreamBucket srs, FloatRect[] bds, float max)
        //{
        //    var md = max;
        //    var mq = md * md;
        //    var x1 = x - md;
        //    var y1 = y - md;
        //    var x2 = x + md;
        //    var y2 = y + md;

        //    var i = 0;
        //    do
        //    {
        //        if (x1 < bds[i].X2 && bds[i].X1 < x2 && y1 < bds[i].Y2 && bds[i].Y1 < y2)
        //        {
        //            var dr = srs[i];
        //            fixed (byte* p0 = &dr.Data0)
        //            {
        //                var pc = (Float2D*)p0;
        //                var pe = (Float2D*)p0 + dr.ItemCount;

        //                var x0 = pc->X;
        //                var y0 = pc->Y;
        //                var c1 = (OutCode)0;
        //                var c2 = ToCode(x0, y0, x1, y1, x2, y2);

        //                float sq;
        //                while (true)
        //                {
        //                    c1 = c2;
        //                    if (++pc == pe) break;
        //                    if (x1 <= pc->X)
        //                        if (pc->X <= x2)
        //                            if (y1 <= pc->Y)
        //                                if (pc->Y <= y2)
        //                                {
        //                                    c2 = 0;
        //                                    sq = PerpendicularDis((pc - 1)->X, (pc - 1)->Y, pc->X, pc->Y, x, y);
        //                                    if (sq < mq) { mq = sq; md = (float)Math.Sqrt(sq); x1 = x - md; y1 = y - md; x2 = x + md; y2 = y + md; }
        //                                    continue;
        //                                }
        //                                else c2 = OutCode.Bottom;
        //                            else c2 = OutCode.Top;
        //                        else if (y1 <= pc->Y)
        //                            if (pc->Y <= y2) c2 = OutCode.Right;
        //                            else c2 = OutCode.Bottom | OutCode.Right;
        //                        else c2 = OutCode.Top | OutCode.Right;
        //                    else if (y1 <= pc->Y)
        //                        if (pc->Y <= y2) c2 = OutCode.Left;
        //                        else c2 = OutCode.Bottom | OutCode.Left;
        //                    else c2 = OutCode.Top | OutCode.Left;

        //                    if ((c1 & c2) != 0)
        //                    {
        //                        if ((c1 & c2 & OutCode.Left) == 0)
        //                            if ((c1 & c2 & OutCode.Right) == 0)
        //                                if ((c1 & c2 & OutCode.Top) == 0)
        //                                {
        //                                R4: if (pc <= pe - 4) { if (y2 < pc->Y) { if (y2 < pc[1].Y) { if (y2 < pc[2].Y) { if (y2 < pc[3].Y) { pc += 4; goto R4; } pc++; } pc++; } pc++; } goto RE; }
        //                                R1: if (pc < pe) { if (y2 < pc->Y) { pc++; goto R1; } goto RE; } else break;
        //                                }
        //                                else
        //                                {
        //                                R4: if (pc <= pe - 4) { if (pc->Y < y1) { if (pc[1].Y < y1) { if (pc[2].Y < y1) { if (pc[3].Y < y1) { pc += 4; goto R4; } pc++; } pc++; } pc++; } goto RE; }
        //                                R1: if (pc < pe) { if (pc->Y < y1) { pc++; goto R1; } goto RE; } else break;
        //                                }
        //                            else
        //                            {
        //                            R4: if (pc <= pe - 4) { if (x2 < pc->X) { if (x2 < pc[1].X) { if (x2 < pc[2].X) { if (x2 < pc[3].X) { pc += 4; goto R4; } pc++; } pc++; } pc++; } goto RE; }
        //                            R1: if (pc < pe) { if (x2 < pc->X) { pc++; goto R1; } goto RE; } else break;
        //                            }
        //                        else
        //                        {
        //                        R4: if (pc <= pe - 4) { if (pc->X < x1) { if (pc[1].X < x1) { if (pc[2].X < x1) { if (pc[3].X < x1) { pc += 4; goto R4; } pc++; } pc++; } pc++; } goto RE; }
        //                        R1: if (pc < pe) { if (pc->X < x1) { pc++; goto R1; } goto RE; } else break;
        //                        }

        //                    RE: c1 = ToCode((pc - 1)->X, (pc - 1)->Y, x1, y1, x2, y2);
        //                        c2 = ToCode(pc->X, pc->Y, x1, y1, x2, y2);
        //                    }
        //                    sq = PerpendicularDis((pc - 1)->X, (pc - 1)->Y, pc->X, pc->Y, x, y);
        //                    if (sq < mq) { mq = sq; md = (float)Math.Sqrt(sq); x1 = x - md; y1 = y - md; x2 = x + md; y2 = y + md; }
        //                }
        //                sq = PerpendicularDis((pe - 1)->X, (pe - 1)->Y, x0, y0, x, y);
        //                if (sq < mq) { mq = sq; md = (float)Math.Sqrt(sq); x1 = x - md; y1 = y - md; x2 = x + md; y2 = y + md; }
        //            }
        //        }
        //    }
        //    while (++i < srs.Count);
        //    return md;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static float PerpendicularSqDis(float x1, float y1, float x2, float y2, float x, float y)
        //{
        //    var dx1 = x - x1;
        //    var dy1 = y - y1;
        //    var dx2 = x2 - x1;
        //    var dy2 = y2 - y1;

        //    var dot = dx1 * dx2 + dy1 * dy2;
        //    if (dot > 0)
        //    {
        //        var sql = dx2 * dx2 + dy2 * dy2;
        //        if (sql > dot)
        //        {
        //            var prj = dot / sql;
        //            dx1 = x - (x1 + prj * dx2);
        //            dy1 = y - (y1 + prj * dy2);
        //        }
        //        else
        //        {
        //            dx1 = x - x2;
        //            dy1 = y - y2;
        //        }
        //    }
        //    return dx1 * dx1 + dy1 * dy1;
        //}
    }
}