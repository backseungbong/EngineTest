using JHLib.Graphics;
using JHLib.NavigationLayer.InteractionMode;
using JHLib.NavigationLayer.Interface;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.WPFUtil.Gesture;

namespace JHApp.ECDIS
{
    public class S57ChartManager : IRenderer, IModeRenderer
    {
        public readonly S57ChartRenderer S57ChartRenderer = null;

        public S57ChartManager(string exePath)
        {
            S57ChartRenderer = new S57ChartRenderer(exePath);
        }

        void IRenderer.ReadyToDrawing(GraphicsContext tc)
        {
            S57ChartRenderer?.ReadyToDrawing(tc);
        }
        void IRenderer.Drawing(GraphicsContext tc)
        {
            S57ChartRenderer?.Drawing(tc);
        }

        void IRenderer.OnLayerSet(GraphicsLayer layer)
        {
            if (S57ChartRenderer != null)
            {
                S57ChartRenderer.SetLayer(layer);
            }
        }

        GestureResultType IModeRenderer.ModeGesture(GestureType type, float x, float y, Transform transform)
        {
            if (type == GestureType.Tap)
            {
                S57ChartRenderer.SetQuery(new Float2D(x, y));
                return GestureResultType.Handled;
            }
            return GestureResultType.None;
        }
        public void ModeOnLayerSet(GraphicsLayer layer)
        {
        }
        void IModeRenderer.ModeReadyToDrawing(GraphicsContext tc)
        {
        }
        void IModeRenderer.ModeDrawing(GraphicsContext tc)
        {
        }

        //public async Task<bool> SetMode()
        //{
        //    var msg = EcdisApp.ModeLayer.SetMode(InteractionModeType.ChartQuery);
        //    if (string.IsNullOrEmpty(msg) == false)
        //    {
        //        await MessageBoxService.ShowAsync(msg, "Edit Mode Change Error", EnumMessageBoxType.Ok);
        //        return false;
        //    }

        //    return true;
        //}

        //public bool CancelMode()
        //{
        //    return EcdisApp.ModeLayer.CancelMode(InteractionModeType.ChartQuery);
        //}
    }
}