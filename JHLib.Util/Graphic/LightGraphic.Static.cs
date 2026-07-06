using JHLib.Util.Graphic.Data;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Graphic
{
    public unsafe partial class LightGraphic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRasterAlpha(byte* s0, int slen, byte* c0, int w, int h, int x1, int y1, int x2, int y2, uint c32_1, uint c32_2) =>
            RasterInternal.DrawRasterAlpha(s0, slen, c0, w, h, x1, y1, x2, y2, c32_1, c32_2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRasterImage(byte* s0, int slen, byte* c0, int w, int h, int x1, int y1, int x2, int y2) =>
            RasterInternal.DrawRasterImage(s0, slen, c0, w, h, x1, y1, x2, y2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BitmapCopy(byte* s0, byte* d0, int len) =>
            BlendInternal.BitmapCopy(s0, d0, len);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BitmapCopyRow(byte* s0, byte* d0, int sPitch, int dPitch, int height) =>
            BlendInternal.BitmapCopyRow(s0, d0, sPitch, dPitch, height);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendSimple(byte* s0, byte* d0, int len) =>
            BlendInternal.BlendSimple(s0, d0, len);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BlendMakeRegion(byte* s0, byte* d0, int len, CacheRegion[] buk = null) =>
            BlendInternal.BlendMakeRegion(s0, d0, len, buk);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendWithRegion(byte* s0, byte* d0, CacheRegion[] buk, int cnt) =>
            BlendInternal.BlendWithRegion(s0, d0, buk, cnt);
    }
}