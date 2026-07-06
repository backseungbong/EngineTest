using JHLib.Util.Projection.ScreenTransform;
using SkiaSharp;

namespace JHLib.Graphics
{
    public class ImmediateSingleRenderer : TransformSetter, IDisposable
    {
        private readonly GraphicsContext _transformContext;
        private readonly GraphicsLayer _layer;
        private int _isDisposed;

        protected override void ChangedTransform(Transform transform) { }
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                _layer?.FreeLayer();
                GC.SuppressFinalize(this);
            }
        }

        public ImmediateSingleRenderer(GraphicsLayer layer)
        {
            _transformContext = new();
            _layer = layer;

            layer.Setter = this;
        }

        public void SetDrawingFontNormal(Stream stream) =>
            _transformContext.SetDrawingFontNormal(SKTypeface.FromStream(stream));
        public void SetDrawingFontBold(Stream stream) =>
            _transformContext.SetDrawingFontBold(SKTypeface.FromStream(stream));
        public void SetDrawingFontItalic(Stream stream) =>
            _transformContext.SetDrawingFontItalic(SKTypeface.FromStream(stream));
        public void SetDrawingFontBoldItalic(Stream stream) =>
            _transformContext.SetDrawingFontBoldItalic(SKTypeface.FromStream(stream));


        public unsafe void SetBitmap(int width, int height)
        {
            _layer.BitmapManager.InitBitmap(width, height);
        }

        public unsafe void DrawChart(nint destBitmap0)
        {
            DrawChart();
            _layer.BitmapManager.CopyToUnaligned((byte*)destBitmap0);
        }

        public unsafe nint DrawChart()
        {
            var context = _transformContext;
            context.Transform = _transform;

            var layer = _layer;
            layer.ReadyToDrawingInternal(context);
            layer.DrawingInternal(context);
            return layer.BitmapManager.Bitmap0;
        }
    }
}