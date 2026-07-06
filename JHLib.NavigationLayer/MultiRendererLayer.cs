using JHLib.Graphics;
using JHLib.NavigationLayer.Interface;
using JHLib.Util.Helper;

namespace JHLib.NavigationLayer
{
    /// <summary> 여러 IRenderer를 등록 및 관리 </summary>
    public sealed class MultiRendererLayer : GraphicsLayer
    {
        private volatile IRenderer[] _renderers;
        private volatile int _rendererCount;
        private int _drawingCount;

        public MultiRendererLayer()
        {
            _renderers = new IRenderer[8];
            _rendererCount = 0;
        }

        /// <summary> 
        /// IRenderer 등록<br/>
        /// 레이어 업데이트시 등록된 순서대로 ReadyToDrawing 및 Drawing이 호출된다
        /// </summary>
        public void Register(IRenderer renderer)
        {
            lock (this)
            {
                renderer.OnLayerSet(this);

                var rendererCount = _rendererCount;
                if (rendererCount == _renderers.Length)
                {
                    var newList = new IRenderer[rendererCount * 2];
                    _renderers.AsSpan().CopyTo(newList);
                    _renderers = newList;
                }
                _renderers[rendererCount] = renderer;
                _rendererCount = rendererCount + 1;
            }
        }

        protected override void ReadyToDrawing(GraphicsContext tc)
        {
            _drawingCount = _rendererCount;

            foreach (var item in Etor.NewUnsafe(_renderers, _drawingCount))
                item.ReadyToDrawing(tc);
        }

        protected override void Drawing(GraphicsContext tc)
        {
            tc.Clear();

            foreach (var item in Etor.NewUnsafe(_renderers, _drawingCount))
                item.Drawing(tc);
        }
    }
}