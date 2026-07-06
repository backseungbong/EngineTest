using JHLib.Util.Graphic.Data;
using JHLib.Util.Graphic.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Graphic
{
    internal static unsafe class PolygonX8664
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AllFill(void* c0, int w, int h, uint c32)
        {
            if (c32 < 0x01000000)
                return;

            if (c32 < 0xFF000000)
            {
                c32 = PixelHelper.Premul(c32);
                PixelHelper.BlendRange((byte*)c0, w * h, c32, 256 - (c32 >> 24));
            }
            else
            {
                var c64 = (ulong)c32 << 32 | c32;
                PixelHelper.FillRange((byte*)c0, w * h, c32, c64);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RectFill(void* c0, int w, int h, uint c32, int x0, int xw, int y0, int yh)
        {
            var d = (uint*)c0 + (uint)(y0 * w + x0);
            var e = d + (uint)(yh * w);

            if (c32 < 0x01000000)
                return;

            if (c32 < 0xFF000000)
            {
                c32 = PixelHelper.Premul(c32);
                var a32 = 256 - (c32 >> 24);
                do PixelHelper.BlendRange((byte*)d, xw, c32, a32);
                while ((d += (uint)w) < e);
            }
            else
            {
                var c64 = (ulong)c32 << 32 | c32;
                do PixelHelper.FillRange((byte*)d, xw, c32, c64);
                while ((d += (uint)w) < e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PathFill(EdgeManager em, void* c0, int w, int h, uint c32)
        {
            if (c32 < 0x01000000)
            {
                em.Clear();
            }
            else
            {
                if (em.ReadyEdge(0, h))
                {
                    if (c32 < 0xFF000000)
                        FillBlend(em, c0, w, h, c32);
                    else
                        FillCopy(em, c0, w, h, c32);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FillCopy(EdgeManager em, void* c0, int w, int h, uint c32)
        {
            if (PixelHelper.YCheck(em.YMin, em.YMax, 0f, h, out var y1, out var y2))
            {
                fixed (Edge* e0 = &em.Edges0)
                {
                    var eg = e0; // 전체 엣지
                    var ea = e0; // 활성 엣지

                    var f0 = 0.0f;
                    var f1 = 1.0f;
                    var wf = (float)w;
                    var st = (uint)w * 4;
                    var d0 = (byte*)c0 + PixelHelper.ToUInt(y1) * st;
                    var c64 = (ulong)c32 << 32 | c32;

                    do
                    {
                        if (eg->YMin <= y1)
                        {
                            var t = e0;
                            do
                            {
                                if (eg->YMax > y1)
                                {
                                    if (t < ea)
                                    {
                                        do if (t->YMax > y1 == false) { goto J1; }
                                        while (++t < ea);
                                    }
                                    ea++;
                                J1: *t = *eg; t->Ready(y1); t++;
                                }
                            }
                            while ((++eg)->YMin <= y1);
                        }
                        ea = EdgeManager.CleanSort(e0, ea, y1);

                        var e = e0;
                        do PixelHelper.XFill(e[0].Slope(), e[1].Slope(), f0, wf, d0, c32, c64);
                        while ((e += 2) < ea);

                        d0 += st;
                        y1 += f1;
                    }
                    while (y1 < y2);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FillBlend(EdgeManager em, void* c0, int w, int h, uint c32)
        {
            if (PixelHelper.YCheck(em.YMin, em.YMax, 0f, h, out var y1, out var y2))
            {
                fixed (Edge* e0 = &em.Edges0)
                {
                    var eg = e0; // 전체 엣지
                    var ea = e0; // 활성 엣지

                    var f0 = 0.0f;
                    var f1 = 1.0f;
                    var wf = (float)w;
                    var st = (uint)w * 4;
                    var d0 = (byte*)c0 + PixelHelper.ToUInt(y1) * st;
                    var a32 = 256 - (c32 >> 24);
                    c32 = PixelHelper.Premul(c32);

                    do
                    {
                        if (eg->YMin <= y1)
                        {
                            var t = e0;
                            do
                            {
                                if (eg->YMax > y1)
                                {
                                    if (t < ea)
                                    {
                                        do if (t->YMax > y1 == false) { goto J1; }
                                        while (++t < ea);
                                    }
                                    ea++;
                                J1: *t = *eg; t->Ready(y1); t++;
                                }
                            }
                            while ((++eg)->YMin <= y1);
                        }
                        ea = EdgeManager.CleanSort(e0, ea, y1);

                        var e = e0;
                        do PixelHelper.XBlend(e[0].Slope(), e[1].Slope(), f0, wf, d0, c32, a32);
                        while ((e += 2) < ea);

                        d0 += st;
                        y1 += f1;
                    }
                    while (y1 < y2);
                }
            }
        }
    }
}