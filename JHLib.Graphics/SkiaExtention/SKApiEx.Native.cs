using SkiaSharp;
using System.Runtime.InteropServices;
using System.Security;

namespace JHLib.Graphics.SkiaExtention
{
    /// <summary>
    /// PInvoke 함수 호출비용을 최소화 하기위해 Skia관련 API를 직접 호출, 일부 SuppressGCTransition 속성 적용으로 호출 성능 최적화
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static unsafe partial class SKApiEx
    {
        private const string LIB = "libSkiaSharp";


        // ============= Canvas =============

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_canvas_set_matrix(nint hCanvas, nint hMatrix);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_canvas_reset_matrix(nint hCanvas);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_clear(nint hCanvas, uint color);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_line(nint hCanvas, float x0, float y0, float x1, float y1, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_rect(nint hCanvas, nint hRect, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_circle(nint hCanvas, float cx, float cy, float rad, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_path(nint hCanvas, nint hPath, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_image(nint hCanvas, nint hImage, float x, float y, nint sampling, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_arc(nint hCanvas, nint hOval, float startAngle, float sweepAngle, byte useCenter, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_picture(nint hCanvas, nint hPicture, nint hMatrix, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_paint(nint hCanvas, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_simple_text(nint hCanvas, nint text, nint bytelen, SKTextEncoding encoding, float x, float y, nint hFont, nint hPaint);

        [LibraryImport(LIB)]
        private static partial void sk_canvas_draw_text_blob(nint hCanvas, nint hBlob, float x, float y, nint hPaint);



        // ============= Path =============

        [LibraryImport(LIB)]
        private static partial void sk_path_add_poly(nint hPath, nint points, int count, byte close);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_path_move_to(nint hPath, float x, float y);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_path_line_to(nint hPath, float x, float y);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_path_add_rect(nint hPath, nint hRect, SKPathDirection direction);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_path_rewind(nint hPath);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_path_set_filltype(nint hPath, SKPathFillType fillType);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial nint sk_path_effect_create_dash(nint intervals, int count, float phase);



        // ============= Paint =============

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_antialias(nint hPaint, byte isAntialias);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_color(nint hPaint, uint color);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_stroke_width(nint hPaint, float width);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_stroke_cap(nint hPaint, SKStrokeCap cap);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_stroke_join(nint hPaint, SKStrokeJoin join);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_shader(nint hPaint, nint hShader);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_path_effect(nint hPaint, nint hEffect);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_blendmode(nint hPaint, SKBlendMode blend);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_paint_set_style(nint hPaint, SKPaintStyle style);



        // ============= Font =============

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_font_set_size(nint hFont, float size);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_font_set_typeface(nint hFont, nint hFace);

        [LibraryImport(LIB)]
        private static partial void sk_font_get_pos(nint hFont, nint glyphs, int count, nint pos, nint origin);

        [LibraryImport(LIB)]
        private static partial void sk_font_measure_text_no_return(nint hFont, nint text, nint bytelen, SKTextEncoding encoding, nint bounds, nint hPaint, nint measuredWidth);

        [LibraryImport(LIB)]
        private static partial int sk_font_text_to_glyphs(nint hFont, nint text, nint bytelen, SKTextEncoding encoding, nint glyphs, int glyphCapacity);

        [LibraryImport(LIB)]
        private static partial void sk_textblob_builder_alloc_run_pos(nint hBuilder, nint hFont, int count, nint hRect, nint buffer);

        [LibraryImport(LIB)]
        private static partial nint sk_textblob_builder_make(nint hBuilder);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial nint sk_textblob_builder_new();

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_textblob_builder_delete(nint hBuilder);

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_textblob_unref(nint hBlob);


        // ============= ETC =============

        [LibraryImport(LIB), SuppressGCTransition]
        private static partial void sk_refcnt_safe_unref(nint hRef);

        [LibraryImport(LIB)]
        private static partial nint sk_image_new_from_bitmap(nint hBitmap);
    }
}