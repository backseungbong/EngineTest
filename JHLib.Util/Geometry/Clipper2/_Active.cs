using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry.Clipper2
{

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal class Active
    {
        public readonly LocalMinima Minima;
        public OutRec OutRec;
        public Active PrevAEL;
        public Active NextAEL;
        public Active PrevSEL;
        public Active NextSEL;
        public Active Jump;
        public Vertex VertexTop;

        public Long2D Btm;
        public Long2D Top;
        public long CurrX;
        public double DX;
        public int WindingSubj;
        public int WindingClip;
        public int WindingDx;
        public bool IsHorizontal;
        public bool IsLeftBound;
        public JoinWith JoinWith;

        public PathType Pathtype => Minima.Pathtype;
        public bool IsSubj => Minima.Pathtype == PathType.Subject;
        public bool IsClip => Minima.Pathtype == PathType.Clip;
        public bool IsOpen => Minima.IsOpen;
        public bool IsOpenEnd => Minima.IsOpen && VertexTop.IsOpenEnd;
        public bool IsHeadingRightHorz => DX == double.NegativeInfinity;
        public bool IsHeadingLeftHorz => DX == double.PositiveInfinity;
        public bool IsMaxima => VertexTop.IsMaxima;
        public bool IsHotEdge => OutRec != null;
        public bool IsFront => this == OutRec.FrontEdge;
        public bool IsJoined => JoinWith != JoinWith.None;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Active(LocalMinima lm, Vertex vertexTop, int wdx)
        {
            Btm = lm.Vertex.Pt;
            Top = vertexTop.Pt;
            CurrX = lm.Vertex.Pt.X;
            WindingDx = wdx;
            VertexTop = vertexTop;
            Minima = lm;
            UpdateHorizontal(lm.Vertex.Pt, vertexTop.Pt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateHorizontal(in Long2D b, in Long2D t)
        {
            if (b.Y == t.Y)
            {
                IsHorizontal = true;
                if (b.X < t.X)
                    DX = double.NegativeInfinity;
                else
                    DX = double.PositiveInfinity;
            }
            else
            {
                IsHorizontal = false;
                DX = (t.X - b.X) / (double)(t.Y - b.Y);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HorzIsSpike() => Btm.X < Top.X != Top.X < NextVertex().Pt.X;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OutPt GetLastOp() => this == OutRec.FrontEdge ? OutRec.Pts : OutRec.Pts.Next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vertex NextVertex() => WindingDx > 0 ? VertexTop.Next : VertexTop.Prev;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vertex PrevPrevVertex() => WindingDx > 0 ? VertexTop.Prev.Prev : VertexTop.Next.Next;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Active GetMaximaPair()
        {
            var ae2 = NextAEL;
            if (ae2 != null)
            {
                do if (ae2.VertexTop == VertexTop) break;
                while ((ae2 = ae2.NextAEL) != null);
            }
            return ae2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Active ExtractFromSEL()
        {
            var res = NextSEL;
            if (res != null)
                res.PrevSEL = PrevSEL;
            PrevSEL.NextSEL = res;
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrimHorz()
        {
            ref var b = ref Btm;
            ref var t = ref Top;

            var nv = NextVertex();
            if (nv.Pt.Y == t.Y && (nv.Pt.X < t.X) == (b.X < t.X))
            {
                do
                {
                    VertexTop = nv;
                    t = nv.Pt;
                    if (nv.IsMaxima) break;
                    nv = NextVertex();
                }
                while (nv.Pt.Y == t.Y && (nv.Pt.X < t.X) == (b.X < t.X));
                UpdateHorizontal(b, t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Active FindEdgeWithMatchingLocMin()
        {
            var r = NextAEL;
            if (r != null)
            {
                do if (r.Minima.Vertex == Minima.Vertex) return r;
                while ((r.IsHorizontal || Btm == r.Btm) && (r = r.NextAEL) != null);
            }

            r = PrevAEL;
            if (r != null)
            {
                do if (r.Minima.Vertex == Minima.Vertex) return r;
                while ((r.IsHorizontal || Btm == r.Btm) && (r = r.PrevAEL) != null);
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Active GetPrevHotEdge()
        {
            var prev = PrevAEL;
            if (prev != null)
            {
                do if (prev.IsOpen == false && prev.IsHotEdge) break;
                while ((prev = prev.PrevAEL) != null);
            }
            return prev;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long TopX(long currY)
        {
            ref var b = ref Btm;
            ref var t = ref Top;

            if (currY != t.Y && t.X != b.X)
                if (currY != b.Y)
                    return b.X + (long)Math.Round(DX * (currY - b.Y), MidpointRounding.ToEven);
                else return b.X;
            else return t.X;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vertex GetCurrYMaximaVertex()
        {
            var r = VertexTop;
            if (WindingDx > 0)
            {
                if (r.Pt.Y == r.Next.Pt.Y)
                {
                    if (Minima.IsOpen == false) while ((r = r.Next).Pt.Y == r.Next.Pt.Y) ;
                    else
                    {
                        do if ((r.Flags & VertexFlags.EndMax) != 0) break;
                        while ((r = r.Next).Pt.Y == r.Next.Pt.Y);
                    }
                }
            }
            else
            {
                if (r.Pt.Y == r.Prev.Pt.Y)
                {
                    if (Minima.IsOpen == false) while ((r = r.Prev).Pt.Y == r.Prev.Pt.Y) ;
                    else
                    {
                        do if ((r.Flags & VertexFlags.EndMax) != 0) break;
                        while ((r = r.Prev).Pt.Y == r.Prev.Pt.Y);
                    }
                }
            }
            return r.IsMaxima ? r : null;
        }
    }
}