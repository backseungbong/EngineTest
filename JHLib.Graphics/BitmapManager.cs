using JHLib.Graphics.SkiaExtention;
using JHLib.Util.Graphic;
using JHLib.Util.Graphic.Data;
using System.Runtime.CompilerServices;

namespace JHLib.Graphics
{
    internal class BitmapManager : IDisposable
    {
        private SKCanvasEx _canvas;
        private CacheRegion[] _regions;
        private int _regionCount;
        public unsafe nint Bitmap0 => (nint)_canvas.Bitmap0;
        public void Dispose() { _canvas?.Dispose(); _canvas = null; }
        internal void InitBitmap(int width, int height)
        {
            _canvas?.Dispose();
            _canvas = new(width, height);
            _regions = GC.AllocateUninitializedArray<CacheRegion>(_canvas.ByteSize / CacheRegion.MIN_REGION + 1);
            _regionCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ApplyToContext(GraphicsContext tc)
        {
            tc.LayerCanvas = _canvas;
            _regionCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void CopyTo(DoubleBufferedBitmap.BackBuffer back) => CopyTo((byte*)back.Buffer0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void CopyTo(byte* dst0)
        {
            var canvas = _canvas;
            LightGraphic.BitmapCopy(canvas.Bitmap0, dst0, canvas.ByteSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void CopyToUnaligned(byte* dst0)
        {
            var canvas = _canvas;            
            Unsafe.CopyBlock(dst0, canvas.Bitmap0, (uint)canvas.ByteSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void BlendTo(DoubleBufferedBitmap.BackBuffer back) => BlendTo((byte*)back.Buffer0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void BlendTo(byte* dst0)
        {
            var canvas = _canvas;
            if (_regionCount == 0)
                _regionCount = LightGraphic.BlendMakeRegion(canvas.Bitmap0, dst0, canvas.ByteSize, _regions);
            else
                LightGraphic.BlendWithRegion(canvas.Bitmap0, dst0, _regions, _regionCount);
        }
    }
}