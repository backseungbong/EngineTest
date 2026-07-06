using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    internal enum ClipType : byte { Intersection, Union, Difference, Xor }
    internal enum FillRule : byte { EvenOdd, NonZero, Positive, Negative }
    internal enum PathType : byte { Subject, Clip }
    internal enum JoinWith : byte { None, Left, Right }

    internal static class Utils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Cross0(in Long2D pt1, in Long2D pt2, in Long2D pt3) =>
            (double)(pt2.X - pt1.X) * (pt3.Y - pt2.Y) == (double)(pt2.Y - pt1.Y) * (pt3.X - pt2.X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Cross(in Long2D pt1, in Long2D pt2, in Long2D pt3) =>
            (double)(pt2.X - pt1.X) * (pt3.Y - pt2.Y) - (double)(pt2.Y - pt1.Y) * (pt3.X - pt2.X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Dot(in Long2D pt1, in Long2D pt2, in Long2D pt3) =>
            (double)(pt2.X - pt1.X) * (pt3.X - pt2.X) + (double)(pt2.Y - pt1.Y) * (pt3.Y - pt2.Y);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double AreaTriangle(in Long2D pt1, in Long2D pt2, in Long2D pt3) =>
            (double)(pt3.Y + pt1.Y) * (pt3.X - pt1.X) +
            (double)(pt1.Y + pt2.Y) * (pt1.X - pt2.X) +
            (double)(pt2.Y + pt3.Y) * (pt2.X - pt3.X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool PtsReallyClose(in Long2D pt1, in Long2D pt2) =>
            Math.Abs(pt1.X - pt2.X) < 2 && Math.Abs(pt1.Y - pt2.Y) < 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSmallTriangle(OutPt op)
        {
            if (op.Next.Next == op.Prev)
            {
                if (PtsReallyClose(op.Prev.Pt, op.Next.Pt) ||
                    PtsReallyClose(op.Pt, op.Next.Pt) ||
                    PtsReallyClose(op.Pt, op.Prev.Pt))
                    return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetIntersectPoint(in Long2D p11, in Long2D p12, in Long2D p21, in Long2D p22, out Long2D ip)
        {
            var dy1 = (double)(p12.Y - p11.Y);
            var dx1 = (double)(p12.X - p11.X);
            var dy2 = (double)(p22.Y - p21.Y);
            var dx2 = (double)(p22.X - p21.X);
            var det = dy1 * dx2 - dy2 * dx1;
            if (det != 0)
            {
                var t = ((p11.X - p21.X) * dy2 - (p11.Y - p21.Y) * dx2) / det;
                if (t > 0)
                    if (t < 1) ip = new((long)(p11.X + t * dx1), (long)(p11.Y + t * dy1));
                    else ip = p12;
                else ip = p11;
                return true;
            }
            ip = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool SegsIntersect(in Long2D seg1a, in Long2D seg1b, in Long2D seg2a, in Long2D seg2b) =>
            Cross(seg1a, seg2a, seg2b) * Cross(seg1b, seg2a, seg2b) < 0 &&
            Cross(seg2a, seg1a, seg1b) * Cross(seg2b, seg1a, seg1b) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetClosestPtOnSegment(in Long2D offPt, in Long2D seg1, in Long2D seg2, out Long2D result)
        {
            if (seg1.X != seg2.X || seg1.Y != seg2.Y)
            {
                var dx = (double)(seg2.X - seg1.X);
                var dy = (double)(seg2.Y - seg1.Y);
                var q = ((offPt.X - seg1.X) * dx + (offPt.Y - seg1.Y) * dy) / (dx * dx + dy * dy);
                if (q > 0)
                {
                    if (q < 1)
                    {
                        result = new(
                            (long)(seg1.X + Math.Round(q * dx, MidpointRounding.ToEven)),
                            (long)(seg1.Y + Math.Round(q * dy, MidpointRounding.ToEven)));
                        return;
                    }
                    result = seg2;
                    return;
                }
            }
            result = seg1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Sq(double value) => value * value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double PerpendicDistFromLineSqrd(in Long2D pt, in Long2D l1, in Long2D l2)
        {
            double c = l2.X - l1.X;
            double d = l2.Y - l1.Y;
            return c == 0 && d == 0 ? 0 : Sq((pt.X - l1.X) * d - c * (pt.Y - l1.Y)) / (c * c + d * d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InitVertexOnOpen(Vertex vtx0, PathType type, LocalMinimaList minimas)
        {
            minimas.NeedSorting = true;

            var prev = vtx0;
            var curr = vtx0.Next;
            do
            {
                if (prev.Pt.Y > curr.Pt.Y) break;
                if (prev.Pt.Y < curr.Pt.Y)
                {
                    prev.Flags = VertexFlags.StartMax;
                    goto D1;
                }
            }
            while ((curr = curr.Next) != prev);

            prev.Flags = VertexFlags.StartMin;
            var lmin = minimas.AddGet();
            lmin.Vertex = prev;
            lmin.Pathtype = type;
            lmin.IsOpen = true;

        U1: prev = curr; curr = curr.Next;
            if (curr == vtx0) { goto U2; }
            if (prev.Pt.Y >= curr.Pt.Y) goto U1;
            prev.Flags = VertexFlags.LocalMax;

        D1: prev = curr; curr = curr.Next;
            if (curr == vtx0) { goto D2; }
            if (prev.Pt.Y <= curr.Pt.Y) goto D1;
            prev.Flags = VertexFlags.LocalMin;
            lmin = minimas.AddGet();
            lmin.Vertex = prev;
            lmin.Pathtype = type;
            lmin.IsOpen = true;
            goto U1;

        U2: prev.Flags = VertexFlags.EndMax; return;
        D2: prev.Flags = VertexFlags.EndMin;
            lmin = minimas.AddGet();
            lmin.Vertex = prev;
            lmin.Pathtype = type;
            lmin.IsOpen = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InitVertexOnClose(Vertex vtx0, PathType type, LocalMinimaList minimas)
        {
            minimas.NeedSorting = true;

            var prev = vtx0.Prev;
            var curr = vtx0;
            do
            {
                if (prev.Pt.Y > curr.Pt.Y) goto U0;
                if (prev.Pt.Y < curr.Pt.Y) goto D0;
            }
            while ((prev = prev.Prev) != curr);

        U1: if (curr == vtx0) { goto EX; }
        U0: prev = curr; curr = curr.Next;
            if (prev.Pt.Y >= curr.Pt.Y) goto U1;
            prev.Flags = VertexFlags.LocalMax;

        D1: if (curr == vtx0) { goto EX; }
        D0: prev = curr; curr = curr.Next;
            if (prev.Pt.Y <= curr.Pt.Y) goto D1;
            prev.Flags = VertexFlags.LocalMin;
            var lmin = minimas.AddGet();
            lmin.Vertex = prev;
            lmin.Pathtype = type;
            lmin.IsOpen = false;
            goto U1;

        EX: return;
        }
    }
}