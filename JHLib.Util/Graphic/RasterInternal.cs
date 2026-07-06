using JHLib.Util.Graphic.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Graphic
{
    internal static unsafe class RasterInternal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRasterAlpha(byte* s0, int slen, byte* c0, int w, int h, int x1, int y1, int x2, int y2, uint c32_1, uint c32_2)
        {
            if (Sse2.IsSupported)
                RasterX8664.DrawRasterAlpha(s0, slen, c0, w, h, x1, y1, x2, y2, c32_1, c32_2);
            else
                RasterArm64.DrawRasterAlpha(s0, slen, c0, w, h, x1, y1, x2, y2, c32_1, c32_2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRasterImage(byte* s0, int slen, byte* c0, int w, int h, int x1, int y1, int x2, int y2)
        {
            if (Sse2.IsSupported)
                RasterX8664.DrawRasterImage(s0, slen, c0, w, h, x1, y1, x2, y2);
            else
                RasterArm64.DrawRasterImage(s0, slen, c0, w, h, x1, y1, x2, y2);
        }
    }

    internal static unsafe class RasterX8664
    {
        private const int SIZE_HEADER8 = 2;
        private const int SIZE_BODY8 = 3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetY8(byte* p) => *p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetCount8(byte* p) => *(p + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetXS8(byte* p) => *p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetXE8(byte* p) => *(p + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetAlpha(byte* p) => *(p + 2);


        private const uint SIZE_HEADER16 = 4;
        private const uint SIZE_BODY16 = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort GetY16(byte* p) => *(ushort*)p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetCount16(byte* p) => *(ushort*)(p + 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetXS16(byte* p) => *(ushort*)p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetXE16(byte* p) => *(ushort*)(p + 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetColor(byte* p) => *(uint*)(p + 4);


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawRasterAlpha(byte* s0, int sn, byte* c0, int w, int h, int x1, int y1, int x2, int y2, uint c32_1, uint c32_2)
        {
            var p = s0;
            var e = s0 + sn;

            var c1 = c32_2 >> 8 & 0x00FF00FF;
            var c2 = c32_2 & 0x00FF00FF;

            var st = w * 4;
            var d0 = c0 + (y1 * st + x1 * 4);

            if (0 <= x1 && x2 <= w && 0 <= y1 && y2 <= h)
            {
                while (true)
                {
                    var xn = GetCount8(p);
                    var dt = d0 + GetY8(p) * (uint)st; p += SIZE_HEADER8;
                    while (true)
                    {
                        var xs = GetXS8(p);
                        var xe = GetXE8(p);
                        var a32 = GetAlpha(p); p += SIZE_BODY8;

                        var a = dt + (uint)xs * 4;
                        var b = dt + (uint)xe * 4;
                        var c = xe - xs;

                        if (a32 != 255)
                        {
                            var cc = c1 * (a32 + 1) & 0xFF00FF00 | c2 * (a32 + 1) >> 8 & 0x00FF00FF;
                            PixelHelper.BlendRange(a, b, c, cc, 256 - a32);
                            if (--xn == 0) { if (p < e) { break; } else { return; } }
                        }
                        else
                        {
                            PixelHelper.FillRange(a, b, c, c32_1);
                            if (--xn == 0) { if (p < e) { break; } else { return; } }
                        }
                    }
                }
            }
            else
            {
                x2 = x2 > w ? w - x1 : x2 - x1;
                y2 = y2 > h ? h - y1 : y2 - y1;
                x1 = x1 < 0 ? -x1 : 0;
                y1 = y1 < 0 ? -y1 : 0;

                if (y1 != 0)
                {
                    do { p += SIZE_HEADER8 + GetCount8(p) * SIZE_BODY8; } while (GetY8(p) < y1);
                    if (GetY8(p) >= y2) { return; }
                }

                do
                {
                    var xn = GetCount8(p);
                    var dt = d0 + GetY8(p) * (uint)st; p += SIZE_HEADER8;
                    do
                    {
                        var xs = GetXS8(p);
                        var xe = GetXE8(p);
                        var a32 = GetAlpha(p); p += SIZE_BODY8;

                        if (x1 < xe && xs < x2)
                        {
                            if (xs < x1) { xs = x1; }
                            if (x2 < xe) { xe = x2; }

                            var a = dt + (uint)xs * 4;
                            var b = dt + (uint)xe * 4;
                            var c = xe - xs;

                            if (a32 != 255)
                            {
                                var cc = c1 * (a32 + 1) & 0xFF00FF00 | c2 * (a32 + 1) >> 8 & 0x00FF00FF;
                                PixelHelper.BlendRange(a, b, c, cc, 256 - a32);
                            }
                            else
                            {
                                PixelHelper.FillRange(a, b, c, c32_1);
                            }
                        }
                    }
                    while (--xn != 0);
                }
                while (p < e && GetY8(p) < y2);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawRasterImage(byte* s0, int sn, byte* c0, int w, int h, int x1, int y1, int x2, int y2)
        {
            var p = s0;
            var e = s0 + sn;

            var st = w * 4;
            var d0 = c0 + (y1 * st + x1 * 4);

            if (0 <= x1 && x2 <= w && 0 <= y1 && y2 <= h)
            {
                while (true)
                {
                    var xn = GetCount16(p);
                    var dt = d0 + GetY16(p) * (uint)st; p += SIZE_HEADER16;
                    while (true)
                    {
                        var xs = GetXS16(p);
                        var xe = GetXE16(p);
                        var c32 = GetColor(p); p += SIZE_BODY16;

                        var a = dt + (uint)xs * 4;
                        var b = dt + (uint)xe * 4;
                        var c = xe - xs;

                        if (c32 < 0xF0000000)
                        {
                            PixelHelper.BlendRange(a, b, c, c32, 256 - (c32 >> 24));
                            if (--xn == 0) { if (p < e) { break; } else { return; } }
                        }
                        else
                        {
                            PixelHelper.FillRange(a, b, c, c32);
                            if (--xn == 0) { if (p < e) { break; } else { return; } }
                        }
                    }
                }
            }
            else
            {
                x2 = x2 > w ? w - x1 : x2 - x1;
                y2 = y2 > h ? h - y1 : y2 - y1;
                x1 = x1 < 0 ? -x1 : 0;
                y1 = y1 < 0 ? -y1 : 0;

                if (y1 != 0)
                {
                    do { p += SIZE_HEADER16 + GetCount16(p) * SIZE_BODY16; } while (GetY16(p) < y1);
                    if (GetY16(p) >= y2) { return; }
                }

                do
                {
                    var xn = GetCount16(p);
                    var dt = d0 + GetY16(p) * (uint)st; p += SIZE_HEADER16;
                    do
                    {
                        var xs = GetXS16(p);
                        var xe = GetXE16(p);
                        var c32 = GetColor(p); p += SIZE_BODY16;

                        if (x1 < xe && xs < x2)
                        {
                            if (xs < x1) { xs = x1; }
                            if (x2 < xe) { xe = x2; }

                            var a = dt + (uint)xs * 4;
                            var b = dt + (uint)xe * 4;
                            var c = xe - xs;

                            if (c32 < 0xF0000000)
                                PixelHelper.BlendRange(a, b, c, c32, 256 - (c32 >> 24));
                            else
                                PixelHelper.FillRange(a, b, c, c32);
                        }
                    }
                    while (--xn != 0);
                }
                while (p < e && GetY16(p) < y2);
            }
        }
    }
    internal static unsafe class RasterArm64
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawRasterAlpha(byte* s0, int slen, byte* c0, int w, int h, int x1, int y1, int x2, int y2, uint c32_1, uint c32_2)
        {
            NativeGraphic.draw_raster_alpha(s0, slen, c0, w, h, x1, y1, x2, y2, c32_1, c32_2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawRasterImage(byte* s0, int slen, byte* c0, int w, int h, int x1, int y1, int x2, int y2)
        {
            NativeGraphic.draw_raster_image(s0, slen, c0, w, h, x1, y1, x2, y2);
        }
    }
}