using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class DEPARE : ObjBase
    {
        public DEPARE(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public ST_DEPARE_HEADER Header = new ST_DEPARE_HEADER(0, -1, float.MaxValue, float.MaxValue, 0);
        public ST_COM Com;
        public ST_EDGE_ATTR[] EdgeAttr;
        public List<ST_COM_AC> ListComCsAC = new();
        public List<ST_EDGE_COM_CS> ListEdgeComCS = new();

        public bool IsShallowPattern = false;
        public bool IsCS = false;

        public void Draw(GraphicsContext context)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            // Under일때
            if (Header.RadarOverlay == 0)
            {
                CreateSKPath_Shape(context.Transform);

                byte btComIndex = 0;
                if (S57ChartOption.PlainSymbolized == true) btComIndex = 1;

                var nCsAC = ListComCsAC.Count;
                if(nCsAC > 0 && AreaSKPath.MainSkPath != null)
                {
                    var rgb = WeatherColor.GetColor(ListComCsAC[0].ColorIndex);
                    var alpha = ChartRenderer.TransparentToByte(ListComCsAC[0].Trans);
                    AreaSKPath.DrawSKPathArea(context, new SKColor(rgb.R, rgb.G, rgb.B, alpha), IsQuerySelect);
                }

                if (Com.ComSize > 0)
                {
                    if (Com.ArrAC != null && AreaSKPath.MainSkPath != null)
                    {
                        var nAC = Com.ArrAC.Length;
                        if (nAC > 0)
                        {
                            CheckComStartEnd(btComIndex, nAC, 0, Com.ArrComInfo, out int nComStart, out int nComEnd);
                            for (int k = nComStart; k < nComEnd; k++)
                            {
                                var rgb = WeatherColor.GetColor(Com.ArrAC[k].ColorIndex);
                                var alpha = ChartRenderer.TransparentToByte(Com.ArrAC[k].Trans);
                                AreaSKPath.DrawSKPathArea(context, new SKColor(rgb.R, rgb.G, rgb.B, alpha), IsQuerySelect);
                            }
                        }
                    }

                    if (Com.ArrAP != null && AreaSKPath.MainSkPath != null)
                    {
                        var nAP = Com.ArrAP.Length;
                        if (nAP > 0)
                        {
                            CheckComStartEnd(btComIndex, nAP, 1, Com.ArrComInfo, out int nComStart, out int nComEnd);
                            for (int k = nComStart; k < nComEnd; k++)
                            {
                                ChartRenderer.DrawPattern(context, Com.ArrAP[k], AreaSKPath.MainSkPath);
                            }
                        }
                    }

                    if (Com.ArrLS != null)
                    {
                        var nLS = Com.ArrLS.Length;
                        if (nLS > 0)
                        {
                            CheckComStartEnd(btComIndex, nLS, 2, Com.ArrComInfo, out int nComStart, out int nComEnd);
                            for (int k = nComStart; k < nComEnd; k++)
                            {
                                var rgb = WeatherColor.GetColor(Com.ArrLS[k].ColorIndex);

                                float[] intervals = null;
                                if (Com.ArrLS[k].Style == 1)
                                {
                                    intervals = new[] { 10.0f, 10.0f };
                                }
                                else if (Com.ArrLS[k].Style == 2)
                                {
                                    intervals = new[] { 3.0f, 3.0f };
                                }

                                for (int m = 0; m < Points.PointsHeader.Edge; m++)
                                {
                                    if (Points.Shape.EdgeArr[m].Mask == 1) continue;

                                    var chartPath = CreateSKPath_EdgeGeo(context.Transform, m);
                                    if(chartPath != null)
                                    {
                                        chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), intervals, 0.0f, Com.ArrLS[k].Width, IsQuerySelect);
                                        if (chartPath.MainSkPath != null) ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine);
                                        chartPath.Dispose();
                                    }
                                }
                            }
                        }
                    }
                }

                // Shallow Pattern을 그려보았음
                if (S57ChartOption.ShallowPattern == true && IsShallowPattern == true && AreaSKPath.MainSkPath != null)
                {
                    // Shallow Pattern은 Category가 Standard이다.
                    if (ChartRenderer.DisplayLevel != 0) ChartRenderer.DrawPattern(context, 1, AreaSKPath.MainSkPath);
                }

                Dispose();
            }
            // Over일때
            else
            {
                Draw_CS(context);
            }
        }

        public void Draw_CS(GraphicsContext context)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            int nEdge = ListEdgeComCS.Count;
            for (int k = 0; k < nEdge; k++)
            {
                int index = ListEdgeComCS[k].EdgeIndex;
                if (Points.Shape.EdgeArr[index].Mask == 1) continue;

                var chartPath = CreateSKPath_EdgeGeo(context.Transform, index);
                if (chartPath == null) continue;

                var rgb = WeatherColor.GetColor(ListEdgeComCS[k].ComLS.ColorIndex);

                float[] intervals = null;
                if (ListEdgeComCS[k].ComLS.Style == 1)
                {
                    intervals = new[] { 10.0f, 10.0f };
                }
                else if (ListEdgeComCS[k].ComLS.Style == 2)
                {
                    intervals = new[] { 3.0f, 3.0f };
                }

                chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), intervals, 0.0f, ListEdgeComCS[k].ComLS.Width, IsQuerySelect);
                if (chartPath.MainSkPath != null) ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine);

                if (context.Transform.PointContainScreen(chartPath.Pivot) == true)
                {
                    if (ListEdgeComCS[k].VALDCO != float.MaxValue)
                    {
                        List<short> listSY = new List<short>();
                        ChartRenderer.CS_SAFCON01(ListEdgeComCS[k].VALDCO, listSY);
                        foreach (var sy in listSY)
                        {
                            ChartRenderer.DrawSymbol(context, sy, 0.0f, chartPath.Pivot, Header.UpdateType);
                        }
                    }
                }

                chartPath.Dispose();
            }
        }

        // Query함수
        public bool Query(Transform projection, Float2D point)
        {
            if (ChartRenderer.IsQuery == false || S57ChartQueryOptions.QueryAreaOn == false || Header.UpdateType == 2 || Header.UpdateType == 12) return false;

            CreateSKPath_Shape(projection);
            IsQuerySelect = AreaSKPath.IsContainSKPathGroup(point);
            Dispose();

            return IsQuerySelect;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
