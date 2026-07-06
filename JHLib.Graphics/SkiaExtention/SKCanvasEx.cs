using JHLib.Util.ArrayControl;
using JHLib.Util.Helper;
using SkiaSharp;
using System.Runtime.CompilerServices;

namespace JHLib.Graphics.SkiaExtention
{
    public unsafe class SKCanvasEx : IDisposable
    {
        private nint _bitmap0;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        public byte* Bitmap0 => (byte*)_bitmap0;
        public SKBitmap Bitmap => _bitmap;
        public SKCanvas Canvas => _canvas ??= new(_bitmap);

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int ByteSize { get; private set; }
        public int RowBytes => Width * 4;
        public int Size => Width * Height;

        public void Dispose()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
            MemoryHelper.Free(ref _bitmap0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKCanvasEx() { Width = -1; Height = -1; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKCanvasEx(int width, int height) => Initializer(width, height);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckSize(int width, int height)
        {
            if (Width != width || Height != height)
                Initializer(width, height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckSize(SKCanvasEx another)
        {
            if (Width != another.Width || Height != another.Height)
                Initializer(another.Width, another.Height);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initializer(int w, int h)
        {
            if (w > 0 && h > 0)
            {
                _canvas?.Dispose();
                _bitmap?.Dispose();
                MemoryHelper.Free(ref _bitmap0);

                _bitmap0 = MemoryHelper.Alloc(w * h * 4, 64, true);
                _bitmap = new SKBitmap();
                _bitmap.InstallPixels(new SKImageInfo(w, h), _bitmap0);
                _canvas = null;
            }

            Width = w;
            Height = h;
            ByteSize = w * h * 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKCanvasEx ClearGet() { Clear(); return this; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            // skia bitmap clear 함수대신 c# 메모리 클리어 함수를 사용하여 성능 향상
            AC.ZeroFill(ref *(byte*)_bitmap0, ByteSize);
        }
    }
}