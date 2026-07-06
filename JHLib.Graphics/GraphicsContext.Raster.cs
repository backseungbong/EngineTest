using JHLib.Graphics.SkiaExtention;
using JHLib.Util.Graphic.Image;
using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Graphics
{
    //public unsafe partial class GraphicsContext
    //{
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public void DrawTextRaster(SKCanvasEx cv, float x, float y, char* text0, int textl, IntColor color)
    //    {
    //        RasterSmallTextAVX2.DrawFont(cv.Bitmap0, cv.Width, cv.Height, x, y, text0, textl, color);
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public void ReadyTextRaster(int fontSize, SKTextFace face = SKTextFace.Normal)
    //    {
    //        if (RasterSmallTextAVX2.ReadyRasterData(fontSize, (int)face) == false)
    //            ReadyTextRasterStreamInternal(fontSize, face);
    //    }

    //    [MethodImpl(MethodImplOptions.NoInlining)]
    //    private void ReadyTextRasterStreamInternal(int fontSize, SKTextFace face = SKTextFace.Normal)
    //    {
    //        var shf = Vector256.Create((byte)
    //            03, 07, 11, 15, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00,
    //            03, 07, 11, 15, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00);

    //        var character = 33;  // 33 ~ 126 까지 문자열만 처리
    //        var rect = GetTextBound((char)character++, fontSize, face);
    //        do rect.Combine(GetTextBound((char)character, fontSize, face));
    //        while (++character < 127);

    //        var dy = (int)MathF.Ceiling(rect.DY);
    //        var offx = -rect.X1 + (8 - rect.DX) / 2;
    //        var offy = -rect.Y1;

    //        var data = new RangeData[256];
    //        var list = new SList<ulong>(256);

    //        var cv = new SKCanvasEx(8, dy);
    //        var b0 = (uint*)cv.Bitmap0;
    //        var be = b0 + dy * 8;

    //        TargetCanvas = cv;
    //        character = 33;
    //        do
    //        {
    //            DrawTextUnaligned((char*)&character, 1, offx, offy);

    //            var b = b0;
    //            while ((Avx.LoadAlignedVector256(b) == Vector256<uint>.Zero) && (b += 8) < be) ;

    //            var start = list.Count;
    //            var blank = 0;
    //            if (b < be)
    //            {
    //                do
    //                {
    //                    var a8 = ExtractAlpha8(b, shf);
    //                    if (a8 == 0) ++blank;
    //                    else blank = 0;
    //                    list.Add(a8);
    //                }
    //                while ((b += 8) < be);
    //            }

    //            var count = list.Count - start;
    //            if (count != 0)
    //            {
    //                list.Count -= blank;
    //                data[character] = new RangeData(start, dy - count, count - blank);
    //            }
    //            cv.Clear();
    //        }
    //        while (++character < 127);

    //        RestorePreviousCanvas();
    //        cv.Dispose();

    //        RasterSmallTextAVX2.SetRasterData(new(fontSize, (int)face, data, list.ToArray(), 8, dy));
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    private static ulong ExtractAlpha8(uint* v256, in Vector256<byte> shf)
    //    {
    //        var v = Avx.LoadAlignedVector256(v256);
    //        var r = Avx2.Shuffle(v.AsByte(), shf);
    //        var a1 = r.GetLower().AsUInt32().ToScalar();
    //        var a2 = r.GetUpper().AsUInt32().ToScalar();
    //        return (ulong)a2 << 32 | a1;
    //    }
    //}
}