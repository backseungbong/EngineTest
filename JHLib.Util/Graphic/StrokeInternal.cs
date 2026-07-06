using JHLib.Util.Graphic.Helper;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Graphic
{
    internal static unsafe class StrokeInternal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawLine(in Float2D p1, in Float2D p2, LightGraphic lg)
        {
            if (Sse2.IsSupported)
                StrokeX8664.DrawLine(p1, p2, lg);
            else
                StrokeArm64.DrawLine(p1, p2, lg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawPathFlatJoin(Float2D* p0, int pn, Float2D head, Float2D tail, LightGraphic lg)
        {
            if (Sse2.IsSupported)
                StrokeX8664.DrawPathFlatJoin(p0, pn, head, tail, lg);
            else
                StrokeArm64.DrawPathFlatJoin(p0, pn, head, tail, lg);
        }
    }

    internal static unsafe class StrokeX8664
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Slope(in Float2D p1, in Float2D p2, float y, out float pos, out float add)
        {
            pos = p1.X; add = 0;
            if (p2.Y > p1.Y)
            {
                var dx = p2.X - p1.X;
                var dy = p2.Y - p1.Y;
                pos = p1.X + (y - p1.Y) / dy * dx;
                add = dx / dy;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawLine(in Float2D p1, in Float2D p2, LightGraphic lg)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            var sq = MathF.Sqrt(dx * dx + dy * dy);
            if (sq > 0f)
            {
                var wf = (float)lg.Width;
                var hf = (float)lg.Height;

                var fx = dy / sq * (lg.StrokeThicknessInternal * 0.5f);
                var fy = dx / sq * (lg.StrokeThicknessInternal * 0.5f);
                var t1 = new Float2D(p1.X - fx, p1.Y + fy);
                var t2 = new Float2D(p1.X + fx, p1.Y - fy);
                var t3 = new Float2D(p2.X + fx, p2.Y - fy);
                var t4 = new Float2D(p2.X - fx, p2.Y + fy);

                Float2D min, m1, m2, max;
                if (float.IsPositive(dy))
                {
                    if (float.IsPositive(dx)) { min = t2; m1 = t1; m2 = t3; max = t4; }
                    else { min = t1; m1 = t4; m2 = t2; max = t3; }
                }
                else
                {
                    if (float.IsPositive(dx)) { min = t3; m1 = t2; m2 = t4; max = t1; }
                    else { min = t4; m1 = t3; m2 = t1; max = t2; }
                }

                if (PixelHelper.XCheck(m1.X, m2.X, 0f, wf))
                {
                    if (PixelHelper.YCheck(min.Y, max.Y, 0f, hf, out var y1, out var y2))
                    {
                        var c32 = lg.StrokeColorInternal;
                        if (c32 < 0xFF000000) { c32 = PixelHelper.Premul(c32); }
                        var c64 = (ulong)c32 << 32 | c32;

                        var st = (uint)lg.Width * 4;
                        var dt = (byte*)lg.Bitmap0 + PixelHelper.ToUInt(y1) * st;
                        var a1 = 1.0f;
                        var f1 = 0.005f;

                        float pos1, add1, pos2, add2;
                        if (m1.Y < m2.Y)
                        {
                            Slope(min, m2, y1, out pos2, out add2);
                            if (y1 < m1.Y)
                            {
                                Slope(min, m1, y1, out pos1, out add1);
                            R1: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                if (y1 < y2 == false) { return; }
                                if (y1 < m1.Y) { goto R1; }
                            }
                            Slope(m1, max, y1, out pos1, out add1);
                            if (y1 < m2.Y)
                            {
                                var ym = PixelHelper.Min(m2.Y, y2); // 라인의 바디는 가장 많은 반복문으로 인해 성능 최적화
                            R2: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                if (y1 < ym) { goto R2; }
                                if (y1 < y2 == false) { return; }
                            }
                            Slope(m2, max, y1, out pos2, out add2);
                        }
                        else
                        {
                            Slope(min, m1, y1, out pos1, out add1);
                            if (y1 < m2.Y)
                            {
                                Slope(min, m2, y1, out pos2, out add2);
                            R1: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                if (y1 < y2 == false) { return; }
                                if (y1 < m2.Y) { goto R1; }
                            }
                            Slope(m2, max, y1, out pos2, out add2);
                            if (y1 < m1.Y)
                            {
                                var ym = PixelHelper.Min(m1.Y, y2); // 라인의 바디는 가장 많은 반복문으로 인해 성능 최적화
                            R2: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                if (y1 < ym) { goto R2; }
                                if (y1 < y2 == false) { return; }
                            }
                            Slope(m1, max, y1, out pos1, out add1);
                        }
                        {
                        R3: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                            dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                            if (y1 < y2) { goto R3; }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawPathFlatJoin(Float2D* p0, int pn, Float2D head, Float2D tail, LightGraphic lg)
        {
            var pc = p0;
            var pe = p0 + pn;
            var p1 = head;
            var p2 = *p0;

            var c32 = lg.StrokeColorInternal;
            if (c32 < 0xFF000000) { c32 = PixelHelper.Premul(c32); }
            var c64 = (ulong)c32 << 32 | c32;

            var d0 = (byte*)lg.Bitmap0;
            var ht = lg.StrokeThicknessInternal * 0.5f;
            var st = (uint)lg.Width * 4;
            var wf = (float)lg.Width;
            var hf = (float)lg.Height;

            var a1 = 1.0f;
            var f1 = 0.005f;

            var cx = 0f;
            var cy = 0f;
            Unsafe.SkipInit(out Float2D s3);
            Unsafe.SkipInit(out Float2D s4);

            while (true)
            {
                var dx = p2.X - p1.X;
                var dy = p2.Y - p1.Y;
                var sq = MathF.Sqrt(dx * dx + dy * dy);
                if (sq > 0f)
                {
                    var fx = dy / sq * ht;
                    var fy = dx / sq * ht;
                    var t1 = new Float2D(p1.X - fx, p1.Y + fy);
                    var t2 = new Float2D(p1.X + fx, p1.Y - fy);
                    var t3 = new Float2D(p2.X + fx, p2.Y - fy);
                    var t4 = new Float2D(p2.X - fx, p2.Y + fy);

                    Float2D min, m1, m2, max;
                    if (float.IsPositive(dy))
                    {
                        if (float.IsPositive(dx)) { min = t2; m1 = t1; m2 = t3; max = t4; }
                        else { min = t1; m1 = t4; m2 = t2; max = t3; }
                    }
                    else
                    {
                        if (float.IsPositive(dx)) { min = t3; m1 = t2; m2 = t4; max = t1; }
                        else { min = t4; m1 = t3; m2 = t1; max = t2; }
                    }

                    if (PixelHelper.XCheck(m1.X, m2.X, 0f, wf))
                    {
                        if (PixelHelper.YCheck(min.Y, max.Y, 0f, hf, out var y1, out var y2))
                        {
                            var dt = d0 + PixelHelper.ToUInt(y1) * st;

                            float pos1, add1, pos2, add2;
                            if (m1.Y < m2.Y)
                            {
                                Slope(min, m2, y1, out pos2, out add2);
                                if (y1 < m1.Y)
                                {
                                    Slope(min, m1, y1, out pos1, out add1);
                                R1: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                    dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                    if (y1 < y2 == false) { goto L2; }
                                    if (y1 < m1.Y) { goto R1; }
                                }
                                Slope(m1, max, y1, out pos1, out add1);
                                if (y1 < m2.Y)
                                {
                                    var ym = PixelHelper.Min(m2.Y, y2); // 라인의 바디는 가장 많은 반복문으로 인해 성능 최적화
                                R2: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                    dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                    if (y1 < ym) { goto R2; }
                                    if (y1 < y2 == false) { goto L2; }
                                }
                                Slope(m2, max, y1, out pos2, out add2);
                            }
                            else
                            {
                                Slope(min, m1, y1, out pos1, out add1);
                                if (y1 < m2.Y)
                                {
                                    Slope(min, m2, y1, out pos2, out add2);
                                R1: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                    dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                    if (y1 < y2 == false) { goto L2; }
                                    if (y1 < m2.Y) { goto R1; }
                                }
                                Slope(m2, max, y1, out pos2, out add2);
                                if (y1 < m1.Y)
                                {
                                    var ym = PixelHelper.Min(m1.Y, y2); // 라인의 바디는 가장 많은 반복문으로 인해 성능 최적화
                                R2: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                    dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                    if (y1 < ym) { goto R2; }
                                    if (y1 < y2 == false) { goto L2; }
                                }
                                Slope(m1, max, y1, out pos1, out add1);
                            }
                            {
                            R3: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                pos1 += add1; pos2 += add2; dt += st; y1 += a1;
                                if (y1 < y2) { goto R3; }
                            }
                        }
                    }

                L2: if (PixelHelper.XCheck(p1.X - ht, p1.X + ht, 0f, wf))
                    {
                        var xy = dx * cy;
                        var yx = dy * cx;
                        if (yx > xy) { if (t2.Y < s3.Y) { min = t2; max = s3; } else { min = s3; max = t2; } }
                        else if (yx < xy) { if (t1.Y < s4.Y) { min = t1; max = s4; } else { min = s4; max = t1; } }
                        else { goto RE; }

                        var md = p1;
                        if (md.Y < min.Y) { md = min; min = p1; }
                        else if (md.Y > max.Y) { md = max; max = p1; }

                        if (PixelHelper.YCheck(min.Y, max.Y, 0f, hf, out var y1, out var y2))
                        {
                            var dt = d0 + PixelHelper.ToUInt(y1) * st;

                            float pos1, add1, pos2, add2;
                            if ((max.X - min.X) * (md.Y - min.Y) > (max.Y - min.Y) * (md.X - min.X))
                            {
                                Slope(min, max, y1, out pos2, out add2);
                                if (y1 < md.Y)
                                {
                                    Slope(min, md, y1, out pos1, out add1);
                                R4: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                    dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                    if (y1 < y2 == false) { goto RE; }
                                    if (y1 < md.Y) { goto R4; }
                                }
                                Slope(md, max, y1, out pos1, out add1);
                            }
                            else
                            {
                                Slope(min, max, y1, out pos1, out add1);
                                if (y1 < md.Y)
                                {
                                    Slope(min, md, y1, out pos2, out add2);
                                R4: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                    dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                    if (y1 < y2 == false) { goto RE; }
                                    if (y1 < md.Y) { goto R4; }
                                }
                                Slope(md, max, y1, out pos2, out add2);
                            }
                            {
                            R5: PixelHelper.XFill(pos1, pos2 + f1, 0f, wf, dt, c32, c64);
                                dt += st; y1 += a1; pos1 += add1; pos2 += add2;
                                if (y1 < y2) { goto R5; }
                            }
                        }
                    }
                RE: cx = dx;
                    cy = dy;
                    s3 = t3;
                    s4 = t4;
                }
                if (pc == pe) { return; }
                p1 = p2; p2 = ++pc < pe ? *pc : tail;
            }
        }
    }

    internal static unsafe class StrokeArm64
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawLine(in Float2D p1, in Float2D p2, LightGraphic lg)
        {
            NativeGraphic.draw_line(p1, p2, lg);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawPathFlatJoin(Float2D* p0, int pn, Float2D head, Float2D tail, LightGraphic lg)
        {
            NativeGraphic.draw_path_flatjoin(p0, pn, head, tail, lg);
        }
    }
}