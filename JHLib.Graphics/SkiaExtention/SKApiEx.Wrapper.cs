using JHLib.Util.Struct;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Graphics.SkiaExtention
{
    public static unsafe partial class SKApiEx
    {
        // ============= Canvas =============

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMatrix(nint hCanvas, nint hMatrix) =>
            sk_canvas_set_matrix(hCanvas, hMatrix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetMatrix(nint hCanvas) =>
            sk_canvas_reset_matrix(hCanvas);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Clear(nint hCanvas, uint color) =>
            sk_canvas_clear(hCanvas, color);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawLine(nint hCanvas, float x0, float y0, float x1, float y1, nint hPaint) =>
            sk_canvas_draw_line(hCanvas, x0, y0, x1, y1, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawRect(nint hCanvas, nint hRect, nint hPaint) =>
            sk_canvas_draw_rect(hCanvas, hRect, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawCircle(nint hCanvas, float cx, float cy, float rad, nint hPaint) =>
            sk_canvas_draw_circle(hCanvas, cx, cy, rad, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawPath(nint hCanvas, nint hPath, nint hPaint) =>
            sk_canvas_draw_path(hCanvas, hPath, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawImage(nint hCanvas, nint hImage, float x, float y, nint sampling, nint hPaint) =>
            sk_canvas_draw_image(hCanvas, hImage, x, y, sampling, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawArc(nint hCanvas, nint hOval, float startAngle, float sweepAngle, byte useCenter, nint hPaint) =>
            sk_canvas_draw_arc(hCanvas, hOval, startAngle, sweepAngle, useCenter, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawPicture(nint hCanvas, nint hPicture, nint hMatrix, nint hPaint) =>
            sk_canvas_draw_picture(hCanvas, hPicture, hMatrix, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawPaint(nint hCanvas, nint hPaint) =>
            sk_canvas_draw_paint(hCanvas, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawSimpleText(nint hCanvas, nint text, nint bytelen, SKTextEncoding encoding, float x, float y, nint hFont, nint hPaint) =>
            sk_canvas_draw_simple_text(hCanvas, text, bytelen, encoding, x, y, hFont, hPaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawTextBlob(nint hCanvas, nint hBlob, float x, float y, nint hPaint) =>
            sk_canvas_draw_text_blob(hCanvas, hBlob, x, y, hPaint);


        // ============= Path =============

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PathAddPoly(nint hPath, nint points, int count, bool close) =>
            sk_path_add_poly(hPath, points, count, Unsafe.BitCast<bool, byte>(close));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PathMoveTo(nint hPath, float x, float y) =>
            sk_path_move_to(hPath, x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PathLineTo(nint hPath, float x, float y) =>
            sk_path_line_to(hPath, x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PathAddRect(nint hPath, nint hRect, SKPathDirection direction) =>
            sk_path_add_rect(hPath, hRect, direction);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PathRewind(nint hPath) =>
            sk_path_rewind(hPath);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PathSetFillType(nint hPath, SKPathFillType fillType) =>
            sk_path_set_filltype(hPath, fillType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint PathEffectCreateDash(nint intervals, int count, float phase) =>
            sk_path_effect_create_dash(intervals, count, phase);


        // ============= Paint =============

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetAntialias(nint hPaint, byte isAntialias) =>
            sk_paint_set_antialias(hPaint, isAntialias);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetColor(nint hPaint, uint color) =>
            sk_paint_set_color(hPaint, color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetStrokeWidth(nint hPaint, float width) =>
            sk_paint_set_stroke_width(hPaint, width);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetStrokeCap(nint hPaint, SKStrokeCap cap) =>
            sk_paint_set_stroke_cap(hPaint, cap);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetStrokeJoin(nint hPaint, SKStrokeJoin join) =>
            sk_paint_set_stroke_join(hPaint, join);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetShader(nint hPaint, nint hShader) =>
            sk_paint_set_shader(hPaint, hShader);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetPathEffect(nint hPaint, nint hEffect) =>
            sk_paint_set_path_effect(hPaint, hEffect);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetBlendMode(nint hPaint, SKBlendMode blend) =>
            sk_paint_set_blendmode(hPaint, blend);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PaintSetStyle(nint hPaint, SKPaintStyle style) =>
            sk_paint_set_style(hPaint, style);


        // ============= Font =============

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FontSetSize(nint hFont, float size) =>
            sk_font_set_size(hFont, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FontSetTypeface(nint hFont, nint hFace) =>
            sk_font_set_typeface(hFont, hFace);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void FontGetPos(nint hFont, nint glyphs, int count, nint pos, nint origin) =>
            sk_font_get_pos(hFont, glyphs, count, pos, origin);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void FontMeasureText(nint hFont, nint text, nint bytelen, SKTextEncoding encoding, nint bounds, nint hPaint, nint measuredWidth) =>
            sk_font_measure_text_no_return(hFont, text, bytelen, encoding, bounds, hPaint, measuredWidth);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int FontTextToGlyphs(nint hFont, nint text, nint bytelen, SKTextEncoding encoding, nint glyphs, int glyphCapacity) =>
            sk_font_text_to_glyphs(hFont, text, bytelen, encoding, glyphs, glyphCapacity);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TextBlobBuilderAllocRunPos(nint hBuilder, nint hFont, int count, nint hRect, nint buffer) =>
            sk_textblob_builder_alloc_run_pos(hBuilder, hFont, count, hRect, buffer);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static nint TextBlobBuilderMake(nint hBuilder) =>
            sk_textblob_builder_make(hBuilder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint TextBlobBuilderNew() =>
            sk_textblob_builder_new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TextBlobBuilderDelete(nint hBuilder) =>
            sk_textblob_builder_delete(hBuilder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TextBlobUnref(nint hBlob) =>
            sk_textblob_unref(hBlob);


        // ============= ETC =============

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RefCntSafeUnref(nint hRef) =>
            sk_refcnt_safe_unref(hRef);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static nint ImageNewFromBitmap(nint hBitmap) =>
            sk_image_new_from_bitmap(hBitmap);


        [StructLayout(LayoutKind.Sequential)]
        private struct SKRunBuffer { public nint glyphs; public nint pos; public nint utf8text; public nint clusters; }

        private const SKTextEncoding TextEncoding = SKTextEncoding.Utf16;


        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FloatRect MeasureTextBound(nint hFont, nint hPaint, void* char0, int charlen)
        {
            FloatRect bound; float width;
            sk_font_measure_text_no_return(hFont, (nint)char0, charlen * 2, TextEncoding, (nint)(&bound), hPaint, (nint)(&width));
            return bound;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FloatRect MeasureTextBound(nint hFont, nint hPaint, void* char0, int charlen, float strokewidth)
        {
            FloatRect bound; float width;
            sk_paint_set_style(hPaint, SKPaintStyle.Stroke);
            sk_paint_set_stroke_width(hPaint, strokewidth);
            sk_font_measure_text_no_return(hFont, (nint)char0, charlen * 2, TextEncoding, (nint)(&bound), hPaint, (nint)(&width));
            sk_paint_set_style(hPaint, SKPaintStyle.Fill);
            return bound;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static nint CreateBlob(nint hFont, void* char0, int charlen)
        {
            var glyphCount = sk_font_text_to_glyphs(hFont, (nint)char0, charlen * 2, TextEncoding, 0, 0);
            var hBuilder = sk_textblob_builder_new();

            SKRunBuffer buffer; Float2D origin = default;
            sk_textblob_builder_alloc_run_pos(hBuilder, hFont, glyphCount, 0, (nint)(&buffer));
            sk_font_text_to_glyphs(hFont, (nint)char0, charlen * 2, TextEncoding, buffer.glyphs, glyphCount);
            sk_font_get_pos(hFont, buffer.glyphs, glyphCount, buffer.pos, (nint)(&origin));

            var hBlob = sk_textblob_builder_make(hBuilder);
            sk_textblob_builder_delete(hBuilder);
            return hBlob;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawText(nint hCanvas, nint hFont, nint hPaint, void* char0, int charlen, float x, float y)
        {
            sk_canvas_draw_simple_text(hCanvas, (nint)char0, charlen * 2, TextEncoding, x, y, hFont, hPaint);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawText(nint hCanvas, nint hFont, nint hPaint, void* char0, int charlen, float x, float y, IntColor fill, IntColor stroke, float width)
        {
            var hBlob = CreateBlob(hFont, char0, charlen);

            sk_paint_set_color(hPaint, stroke);
            sk_paint_set_style(hPaint, SKPaintStyle.Stroke);
            sk_paint_set_stroke_width(hPaint, width);
            sk_canvas_draw_text_blob(hCanvas, hBlob, x, y, hPaint);

            sk_paint_set_color(hPaint, fill);
            sk_paint_set_style(hPaint, SKPaintStyle.Fill);
            sk_canvas_draw_text_blob(hCanvas, hBlob, x, y, hPaint);

            sk_textblob_unref(hBlob);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawBlob(nint hCanvas, nint hPaint, nint hBlob, float x, float y)
        {
            sk_canvas_draw_text_blob(hCanvas, hBlob, x, y, hPaint);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DrawBlob(nint hCanvas, nint hPaint, nint hBlob, float x, float y, IntColor fill, IntColor stroke, float width)
        {
            sk_paint_set_color(hPaint, stroke);
            sk_paint_set_style(hPaint, SKPaintStyle.Stroke);
            sk_paint_set_stroke_width(hPaint, width);
            sk_canvas_draw_text_blob(hCanvas, hBlob, x, y, hPaint);

            sk_paint_set_color(hPaint, fill);
            sk_paint_set_style(hPaint, SKPaintStyle.Fill);
            sk_canvas_draw_text_blob(hCanvas, hBlob, x, y, hPaint);
        }
    }
}