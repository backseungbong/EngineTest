using JHLib.Util.Graphic.Data;
using JHLib.Util.Matrix;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Graphic
{
    public static unsafe class NativeGraphic
    {
        private const string LIBName = "libNativeGraphic";

        private static readonly delegate* unmanaged[Cdecl]<byte*, byte*, int, void> _bitmap_copy;
        private static readonly delegate* unmanaged[Cdecl]<byte*, byte*, int, CacheRegion*, int, int> _blend32_make_region;
        private static readonly delegate* unmanaged[Cdecl]<byte*, byte*, CacheRegion*, int, void> _blend32_with_region;

        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<void> _path_clear;
        private static readonly delegate* unmanaged[Cdecl]<nint, int, float, float, void> _path_add;
        private static readonly delegate* unmanaged[Cdecl]<float, float, float, float, void> _path_add_rect;
        private static readonly delegate* unmanaged[Cdecl]<nint, int, nint, int, int, uint, void> _path_fill_simple;

        private static readonly delegate* unmanaged[Cdecl]<nint, int, int, uint, void> _path_fill;
        private static readonly delegate* unmanaged[Cdecl]<nint, int, int, uint, void> _all_fill;
        private static readonly delegate* unmanaged[Cdecl]<nint, int, int, uint, int, int, int, int, void> _rect_fill;

        private static readonly delegate* unmanaged[Cdecl]<Float2D, Float2D, nint, int, int, uint, float, void> _draw_line;
        private static readonly delegate* unmanaged[Cdecl]<nint, int, nint, int, int, uint, float, Float2D, Float2D, void> _draw_path_flatjoin;

        private static readonly delegate* unmanaged[Cdecl]<nint, int, nint, int, int, int, int, int, int, uint, uint, void> _draw_raster_alpha;
        private static readonly delegate* unmanaged[Cdecl]<nint, int, nint, int, int, int, int, int, int, void> _draw_raster_image;

        private static readonly delegate* unmanaged[Cdecl]<nint, nint, int, nint, float, int> _world_to_screen;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, nint, int, nint, float, int> _world_to_screen_suppress;

        private static readonly delegate* unmanaged[Cdecl]<nint, nint, int, void> _wgs84_to_world;
        private static readonly delegate* unmanaged[Cdecl]<nint, nint, int, nint, void> _wgs84_to_screen;

        private static readonly delegate* unmanaged[Cdecl]<nint, int, float, int> _dedupe_points;
        private static readonly delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, int, float, int> _dedupe_points_suppress;

        private static nint Get(nint handle, string name) => NativeLibrary.GetExport(handle, name);
        static NativeGraphic()
        {
            var h = NativeLibrary.Load(LIBName);

            _bitmap_copy =
                (delegate* unmanaged[Cdecl]<byte*, byte*, int, void>)Get(h, "bitmap_copy");

            _blend32_make_region =
                (delegate* unmanaged[Cdecl]<byte*, byte*, int, CacheRegion*, int, int>)Get(h, "blend32_make_region");

            _blend32_with_region =
                (delegate* unmanaged[Cdecl]<byte*, byte*, CacheRegion*, int, void>)Get(h, "blend32_with_region");

            _path_clear =
                (delegate* unmanaged[Cdecl, SuppressGCTransition]<void>)Get(h, "path_clear");

            _path_add =
                (delegate* unmanaged[Cdecl]<nint, int, float, float, void>)Get(h, "path_add");

            _path_add_rect =
                (delegate* unmanaged[Cdecl]<float, float, float, float, void>)Get(h, "path_add_rect");

            _path_fill_simple =
                (delegate* unmanaged[Cdecl]<nint, int, nint, int, int, uint, void>)Get(h, "path_fill_simple");

            _path_fill =
                (delegate* unmanaged[Cdecl]<nint, int, int, uint, void>)Get(h, "path_fill");

            _all_fill =
                (delegate* unmanaged[Cdecl]<nint, int, int, uint, void>)Get(h, "all_fill");

            _rect_fill =
                (delegate* unmanaged[Cdecl]<nint, int, int, uint, int, int, int, int, void>)Get(h, "rect_fill");


            _draw_line =
                (delegate* unmanaged[Cdecl]<Float2D, Float2D, nint, int, int, uint, float, void>)Get(h, "draw_line");

            _draw_path_flatjoin =
                (delegate* unmanaged[Cdecl]<nint, int, nint, int, int, uint, float, Float2D, Float2D, void>)Get(h, "draw_path_flatjoin");


            _draw_raster_alpha =
                (delegate* unmanaged[Cdecl]<nint, int, nint, int, int, int, int, int, int, uint, uint, void>)Get(h, "draw_raster_alpha");

            _draw_raster_image =
                (delegate* unmanaged[Cdecl]<nint, int, nint, int, int, int, int, int, int, void>)Get(h, "draw_raster_image");


            _world_to_screen =
                (delegate* unmanaged[Cdecl]<nint, nint, int, nint, float, int>)Get(h, "world_to_screen");

            _world_to_screen_suppress =
                (delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, nint, int, nint, float, int>)Get(h, "world_to_screen");


            _wgs84_to_world =
                (delegate* unmanaged[Cdecl]<nint, nint, int, void>)Get(h, "wgs84_to_world");

            _wgs84_to_screen =
                (delegate* unmanaged[Cdecl]<nint, nint, int, nint, void>)Get(h, "wgs84_to_screen");


            _dedupe_points =
                (delegate* unmanaged[Cdecl]<nint, int, float, int>)Get(h, "dedupe_points");

            _dedupe_points_suppress =
                (delegate* unmanaged[Cdecl, SuppressGCTransition]<nint, int, float, int>)Get(h, "dedupe_points");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void bitmap_copy(byte* s0, byte* d0, int bytesize)
        {
            _bitmap_copy(s0, d0, bytesize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int blend32_make_region(byte* s0, byte* d0, int bytesize, CacheRegion[] regionBucket)
        {
            var func = _blend32_make_region;
            if (regionBucket == null || regionBucket.Length == 0)
                return func(s0, d0, bytesize, null, 0);

            fixed (CacheRegion* r0 = &MemoryMarshal.GetArrayDataReference(regionBucket))
                return func(s0, d0, bytesize, r0, regionBucket.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void blend32_with_region(byte* s0, byte* d0, CacheRegion[] regionBucket, int regionCount)
        {
            if (regionCount <= 0) { return; }
            fixed (CacheRegion* r0 = &MemoryMarshal.GetArrayDataReference(regionBucket))
            {
                _blend32_with_region(s0, d0, r0, regionCount);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void path_clear()
        {
            _path_clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void path_add(Float2D* p0, int pc, float y1, float y2)
        {
            _path_add((nint)p0, pc, y1, y2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void path_add_rect(float x1, float y1, float x2, float y2)
        {
            _path_add_rect(x1, y1, x2, y2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void path_fill(void* c0, int cw, int ch, uint c32)
        {
            _path_fill((nint)c0, cw, ch, c32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void path_fill_simple(Float2D* p0, int pc, void* c0, int cw, int ch, uint c32)
        {
            _path_fill_simple((nint)p0, pc, (nint)c0, cw, ch, c32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void all_fill(void* c0, int cw, int ch, uint c32)
        {
            _all_fill((nint)c0, cw, ch, c32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void rect_fill(void* c0, int cw, int ch, uint c32, int x0, int xc, int y0, int yc)
        {
            _rect_fill((nint)c0, cw, ch, c32, x0, xc, y0, yc);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void draw_line(in Float2D p1, in Float2D p2, LightGraphic lg)
        {
            _draw_line(p1, p2,
                (nint)lg.Bitmap0, lg.Width, lg.Height, lg.StrokeColorInternal, lg.StrokeThicknessInternal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void draw_path_flatjoin(Float2D* p0, int pc, Float2D head, Float2D tail, LightGraphic lg)
        {
            _draw_path_flatjoin((nint)p0, pc,
                (nint)lg.Bitmap0, lg.Width, lg.Height, lg.StrokeColorInternal, lg.StrokeThicknessInternal, head, tail);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void draw_raster_alpha(
            void* s0, int slen, void* c0, int cw, int ch, int x1, int y1, int x2, int y2, uint c32_1, uint c32_2)
        {
            _draw_raster_alpha((nint)s0, slen, (nint)c0, cw, ch, x1, y1, x2, y2, c32_1, c32_2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void draw_raster_image(
            void* s0, int slen, void* c0, int cw, int ch, int x1, int y1, int x2, int y2)
        {
            _draw_raster_image((nint)s0, slen, (nint)c0, cw, ch, x1, y1, x2, y2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int world_to_screen(Float2D* s0, Float2D* d0, int l, in Matrix22D mtx, float dudupePixel)
        {
            var m = mtx;
            int r;
            if (l < 24)
            {
                r = _world_to_screen_suppress((nint)s0, (nint)d0, l, (nint)(&m), dudupePixel);
            }
            else
            {
                r = _world_to_screen((nint)s0, (nint)d0, l, (nint)(&m), dudupePixel);
            }
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int dedupe_points(Float2D* s0, int l, float dedupePixel)
        {
            int r;
            if (l < 32)
            {
                r = _dedupe_points_suppress((nint)s0, l, dedupePixel);
            }
            else
            {
                r = _dedupe_points((nint)s0, l, dedupePixel);
            }
            return r;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void wgs84_to_world(ref Float2D s0, ref Float2D d0, int l)
        {
            fixed (Float2D* s = &s0, d = &d0)
            {
                _wgs84_to_world((nint)s, (nint)d, l);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void wgs84_to_screen(ref Float2D s0, ref Float2D d0, int l, in Matrix22D mtx)
        {
            fixed (Float2D* s = &s0, d = &d0)
            {
                var m = mtx;
                _wgs84_to_screen((nint)s, (nint)d, l, (nint)(&m));
                return;
            }
        }
    }
}