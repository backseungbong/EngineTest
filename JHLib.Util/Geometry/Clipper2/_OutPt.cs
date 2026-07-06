using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal class OutPt(in Long2D pt)
    {
        public readonly Long2D Pt = pt;
        public OutRec Outrec;
        public OutPt Prev;
        public OutPt Next;
        public HorzSegment Horz;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Area()
        {
            var op2 = this;
            var area = 0d;
            do area += (double)(op2.Prev.Pt.Y + op2.Pt.Y) * (op2.Prev.Pt.X - op2.Pt.X);
            while ((op2 = op2.Next) != this);
            return area * 0.5;
        }
    }
}