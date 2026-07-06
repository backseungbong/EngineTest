using JHLib.Util.ArrayControl;
using JHLib.Util.DataStream;
using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Geometry
{
    public unsafe static partial class GeometryHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OutCode CalCorner(Float2D* pc, OutCode c1, OutCode c2, in FloatRect rect) => CalCorner(*(pc - 1), *pc, c1, c2, rect);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OutCode CalCorner(in Float2D p1, in Float2D p2, OutCode c1, OutCode c2, in FloatRect rect)
        {
            OutCode oc;
            if ((c1 & c1 - 1) == 0)
            {
                if ((c1 & OutCode.XAxis) != 0)
                    oc = (c1 & OutCode.XAxis) | (c2 & OutCode.YAxis);
                else
                    oc = (c1 & OutCode.YAxis) | (c2 & OutCode.XAxis);
            }
            else if ((c2 & c2 - 1) == 0)
            {
                if ((c2 & OutCode.XAxis) != 0)
                    oc = (c2 & OutCode.XAxis) | (c1 & OutCode.YAxis);
                else
                    oc = (c2 & OutCode.YAxis) | (c1 & OutCode.XAxis);
            }
            else
            {
                var x = p1.LerpX(p2, rect.Y1);
                if (c1 == OutCode.LT || c1 == OutCode.RB)
                    oc = rect.X1 < x ? OutCode.RT : OutCode.LB;
                else
                    oc = x < rect.X2 ? OutCode.LT : OutCode.RB;
            }
            return oc;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ProcessLastPoint(Float2D* pe, Float2D* p0, OutCode c1, OutCode c2, in FloatRect rect, in Float2Dx4 cn, SList<Float2D> rst)
        {
            var p1 = *pe;
            var p2 = *p0;

            var isIntersect = false;
            if (c1 != OutCode.Inside)
            {
                if ((c1 & c1 - 1) != 0) { AddCorner(rst, cn, c1); }
                if (c2 != OutCode.Inside)
                {
                    if ((c1 & c2) != 0) { return isIntersect; }
                    if (LineClip(p1, p2, rect, out p1, out p2))
                    {
                        AddPoint(rst, p1, p2);
                        isIntersect = true;
                    }
                    else
                    {
                        AddCorner(rst, cn, CalCorner(p1, p2, c1, c2, rect));
                    }
                }
                else
                {
                    AddPoint(rst, LineClipInsideOut(p2, p1, rect));
                    isIntersect = true;
                }
            }
            else
            {
                AddPoint(rst, LineClipInsideOut(p1, p2, rect));
                isIntersect = true;
            }
            return isIntersect;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ProcessLastPoint(Float2D* pe, Float2D* p0, OutCode c1, OutCode c2, in FloatRect rect, in Float2Dx4 cn, in DataHeaderWriter rst)
        {
            var p1 = *pe;
            var p2 = *p0;

            var isIntersect = false;
            if (c1 != OutCode.Inside)
            {
                if ((c1 & c1 - 1) != 0) { AddCorner(rst, cn, c1); }
                if (c2 != OutCode.Inside)
                {
                    if ((c1 & c2) != 0) { return isIntersect; }
                    if (LineClip(p1, p2, rect, out p1, out p2))
                    {
                        AddPoint(rst, p1, p2);
                        isIntersect = true;
                    }
                    else
                    {
                        AddCorner(rst, cn, CalCorner(p1, p2, c1, c2, rect));
                    }
                }
                else
                {
                    AddPoint(rst, LineClipInsideOut(p2, p1, rect));
                    isIntersect = true;
                }
            }
            else
            {
                AddPoint(rst, LineClipInsideOut(p1, p2, rect));
                isIntersect = true;
            }
            return isIntersect;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddCPath(SList<ClippedPath> rst, in Float2D p1, in Float2D p2, Float2D* p0, Float2D* pt) =>
            AddCPath(ref Unsafe.As<ClippedPath, ClippedPathRaw>(ref rst.AddRef()), p1, p2, p0, pt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddCPath(SList<ClippedPath> rst, in Float2D p1, in Float2D p2, Float2D* p0, Float2D* pt, Float2D* pc) =>
            AddCPath(ref Unsafe.As<ClippedPath, ClippedPathRaw>(ref rst.AddRef()), p1, p2, p0, pt, pc);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddCPath(in DataHeaderWriter rst, in Float2D p1, in Float2D p2, Float2D* p0, Float2D* pt) =>
            AddCPath(ref rst.AddRef<ClippedPathRaw>(), p1, p2, p0, pt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddCPath(in DataHeaderWriter rst, in Float2D p1, in Float2D p2, Float2D* p0, Float2D* pt, Float2D* pc) =>
            AddCPath(ref rst.AddRef<ClippedPathRaw>(), p1, p2, p0, pt, pc);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddCPath(ref ClippedPathRaw t, in Float2D p1, in Float2D p2, Float2D* p0, Float2D* pt)
        {
            t.Length = 0;
            t.Offset = (int)((nint)pt - (nint)p0) >> 3;
            t.Tail = p2;
            t.Head = p1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddCPath(ref ClippedPathRaw t, in Float2D p1, in Float2D p2, Float2D* p0, Float2D* pt, Float2D* pc)
        {
            t.Length = (int)((nint)pc - (nint)pt) >> 3;
            t.Offset = (int)((nint)pt - (nint)p0) >> 3;
            t.Tail = p2;
            t.Head = p1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddCorner(SList<Float2D> rmg, in Float2Dx4 cn, OutCode code) => AddPoint(rmg, cn[(int)code >> 2]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddPoint(SList<Float2D> rmg, in Float2D p1) => rmg.AddRef() = p1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddPoint(SList<Float2D> rmg, in Float2D p1, in Float2D p2)
        {
            ref var t = ref rmg.Occupy0(2);
            Unsafe.Add(ref t, 0) = p1;
            Unsafe.Add(ref t, 1) = p2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddCorner(in DataHeaderWriter rmg, in Float2Dx4 cn, OutCode code) => AddPoint(rmg, cn[(int)code >> 2]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddPoint(in DataHeaderWriter rmg, in Float2D p1) => rmg.AddRef<Float2D>() = p1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddPoint(in DataHeaderWriter rmg, in Float2D p1, in Float2D p2)
        {
            ref var t = ref rmg.Occupy0<Float2D>(2);
            Unsafe.Add(ref t, 0) = p1;
            Unsafe.Add(ref t, 1) = p2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddCPathCorner(SList<ClippedPath> rst, SList<Float2D> marea, in Float2D p1, in Float2D p2, Float2D* p0, Float2D* pt, Float2D* pc)
        {
            ref var tp = ref Unsafe.As<ClippedPath, ClippedPathRaw>(ref rst.AddRef());
            tp.Head = p1;
            tp.Tail = p2;
            tp.Offset = (int)((nint)pt - (nint)p0) >> 3;
            var l = (int)((nint)pc - (nint)pt) >> 3;
            tp.Length = l;

            ref var ta = ref marea.Occupy0(l + 2);
            Unsafe.Add(ref ta, 0) = p1;
            AC.Copy(ref *pt, ref Unsafe.Add(ref ta, 1), l);
            Unsafe.Add(ref ta, (uint)(l + 1)) = p2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddCPathCorner(SList<ClippedPath> rst, SList<Float2D> marea, in Float2D p1, in Float2D p2, Float2D* p0, Float2D* pt)
        {
            ref var tp = ref Unsafe.As<ClippedPath, ClippedPathRaw>(ref rst.AddRef());
            tp.Head = p1;
            tp.Tail = p2;
            tp.Offset = (int)((nint)pt - (nint)p0) >> 3;
            tp.Length = 0;

            ref var ta = ref marea.Occupy0(2);
            Unsafe.Add(ref ta, 0) = p1;
            Unsafe.Add(ref ta, 1) = p2;
        }
    }
}