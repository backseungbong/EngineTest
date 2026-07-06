using JHLib.Graphics;
using JHLib.NavigationLayer;
using JHLib.NavigationLayer.InteractionMode;

namespace JHApp.ECDIS
{
    public static partial class EcdisApp
    {
        public static GraphicsLayerManager LayerManager;
        public static MultiRendererLayer ChartLayer;
        public static InteractionModeLayer ModeLayer;

        public static S57ChartManager S57Chart;

        public static void Init()
        {
            var exePath = Environment.GetEnvironmentVariable("ExeDirectory");

            LayerManager = new GraphicsLayerManager();

            ChartLayer = new MultiRendererLayer();
            ModeLayer = new InteractionModeLayer { TransformSetter = LayerManager };

            LayerManager.AddLayer(ChartLayer, 1);
            LayerManager.AddLayer(ModeLayer, 5);

            S57Chart = new S57ChartManager(exePath);

            // 모니터 정보 및 시작위치, 시작Scale 적용
            LayerManager.SetMoniter(1920, 1080, 24);
            LayerManager.MoveToWGS84(128, 37);
            LayerManager.SetScale(250000);

            ChartLayer.Register(S57Chart);

            ModeLayer.Register(InteractionModeType.ChartQuery, S57Chart);
        }
    }
}
