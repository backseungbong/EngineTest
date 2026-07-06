using JHLib.Graphics;

namespace JHLib.NavigationLayer.Interface
{
    public interface IRenderer
    {
        void OnLayerSet(GraphicsLayer layer);
        void ReadyToDrawing(GraphicsContext tc);
        void Drawing(GraphicsContext tc);
    }
}