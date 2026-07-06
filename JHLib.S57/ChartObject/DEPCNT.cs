using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class DEPCNT : ObjBase
    {
        public DEPCNT(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public ST_DEPCNT_HEADER Header;

        // Draw함수 
        public void Draw(GraphicsContext context)
        {
            if (ChartRenderer.FindViewingGroup(17) == true && ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == true)
            {
                if (IsDraw(Header.UpdateType) == false) return;

                int nEdge = Points.Shape.EdgeArr.Length;
                for(int k=0; k<nEdge; k++)
                {
                    var edge = Points.Shape.EdgeArr[k];

                    if (edge.Mask == 1) continue;

                    var chartPath = CreateSKPath_EdgeGeo(context.Transform, k);
                    if (chartPath == null) continue;

                    var rgb = WeatherColor.GetColor("DEPCN");

                    if(edge.Quapos == false)
                    {
                        chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 10.0f, 10.0f }, 0.0f, 1.0f, IsQuerySelect);
                    }
                    else
                    {
                        chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), null, 0.0f, 1.0f, IsQuerySelect);
                    }

                    if(S57ChartOption.ContourLabel == true && Header.VALDCO != float.MaxValue)
                    {
                        List<short> listSY = new List<short>();
                        ChartRenderer.CS_SAFCON01(Header.VALDCO, listSY);
                        foreach(var sy in listSY)
                        {
                            ChartRenderer.DrawSymbol(context, sy, 0.0f, chartPath.Pivot, Header.UpdateType);
                        }
                    }

                    chartPath.Dispose();
                }
            }
        }

        // Query함수
        public bool Query(Transform projection, Float2D point)
        {
            if (ChartRenderer.IsQuery == false || S57ChartQueryOptions.QueryLineOn == false || Header.UpdateType == 2 || Header.UpdateType == 12) return false;

            IsQuerySelect = false;
            int size = Points.PointsHeader.Edge;
            for (int i = 0; i < size; i++)
            {
                if (Points.Shape.EdgeArr[i].Mask == 1) continue;

                var chartPath = CreateSKPath_EdgeGeo(projection, i);
                if(chartPath != null) 
                {
                    IsQuerySelect = chartPath.IsContainSKpathLine(point);
                    chartPath.Dispose();
                }

                if (IsQuerySelect == true) break;
            }

            return IsQuerySelect;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
