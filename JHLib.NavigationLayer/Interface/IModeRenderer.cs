using JHLib.Graphics;
using JHLib.NavigationLayer.InteractionMode;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.WPFUtil.Gesture;

namespace JHLib.NavigationLayer.Interface
{
    public interface IModeRenderer
    {
        GestureResultType ModeGesture(GestureType type, float x, float y, Transform transform);        
        void ModeOnLayerSet(GraphicsLayer layer);
        void ModeReadyToDrawing(GraphicsContext tc);
        void ModeDrawing(GraphicsContext tc);
    }
}