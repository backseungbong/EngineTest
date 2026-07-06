using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class DRGARE : ObjBase
    {
        public DRGARE(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public ST_DRGARE_HEADER Header = new ST_DRGARE_HEADER(0, -1, float.MaxValue, false, 0);
        public ST_COM Com;
        public ST_EDGE_ATTR[] EdgeAttr;
        public List<ST_COM_AC> ListComCsAC = new();
        public List<ST_EDGE_COM_CS> LiedgeComCS = new();

        public bool IsShallowPattern = false;
        public bool IsCS = false;

        public void Draw(GraphicsContext context, ref ST_OVER stOver)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            // Under일때
            if (Header.RadarOverlay == 0)
            {
                CreateSKPath_Shape(context.Transform, true);

                int nSize = ListComCsAC.Count;
                if (nSize > 0 && AreaSKPath.MainSkPath != null)
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
                            var rgb = WeatherColor.GetColor(Com.ArrAC[0].ColorIndex);
                            var alpha = ChartRenderer.TransparentToByte(Com.ArrAC[0].Trans);
                            AreaSKPath.DrawSKPathArea(context, new SKColor(rgb.R, rgb.G, rgb.B, alpha), IsQuerySelect);
                        }
                    }

                    if (Com.ArrAP != null && AreaSKPath.MainSkPath != null)
                    {
                        var nAP = Com.ArrAP.Length;
                        if (nAP > 0)
                        {
                            ChartRenderer.DrawPattern(context, Com.ArrAP[0], AreaSKPath.MainSkPath);
                        }
                    }

                    if (Com.ArrLS != null)
                    {
                        var nLS = Com.ArrLS.Length;
                        if (nLS > 0)
                        {
                            var rgb = WeatherColor.GetColor(Com.ArrLS[0].ColorIndex);

                            float[] intervals = null;
                            if (Com.ArrLS[0].Style == 1)
                            {
                                intervals = new[] { 10.0f, 10.0f };
                            }
                            else if (Com.ArrLS[0].Style == 2)
                            {
                                intervals = new[] { 3.0f, 3.0f };
                            }

                            for (int m = 0; m < Points.PointsHeader.Edge; m++)
                            {
                                if (Points.Shape.EdgeArr[m].Mask == 1) continue;

                                var chartPath = CreateSKPath_EdgeGeo(context.Transform, m);
                                if(chartPath != null) 
                                {
                                    chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), intervals, 0.0f, Com.ArrLS[0].Width, IsQuerySelect);
                                    if (chartPath.MainSkPath != null) ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine);
                                    chartPath.Dispose();
                                }
                            }
                        }
                    }

                    if (Com.ArrSY != null)
                    {
                        var nSY = Com.ArrSY.Length;
                        if (nSY > 0)
                        {
                            for (int k = 0; k < nSY; k++)
                            {
                                bool bAreaInCenterSym = true;

                                // 교차 영역이 있으면
                                if (AreaSKPath.PathsIntersect != null)
                                {
                                    bAreaInCenterSym = CheckAreaInCenterSymbol(AreaSKPath.PathsIntersect, Com.ArrSY[k].Index, AreaSKPath.Pivot);
                                }
                                else bAreaInCenterSym = false;

                                if (bAreaInCenterSym == true)
                                {
                                    float angle = 0.0f;
                                    if (Com.ArrSY[k].Angle != 0.0) angle = Com.ArrSY[k].Angle + (float)context.Transform.Rotation;
                                    else angle = Com.ArrSY[k].Angle;

                                    ChartRenderer.DrawSymbol(context, Com.ArrSY[k].Index, angle, AreaSKPath.Pivot, Header.UpdateType);
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

                // INFORM 속성이 있으면 
                if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, AreaSKPath.Pivot) == false)
                {
                    if (S57ChartOption.HighlightInfo == true && Header.Highlight == true)
                    {
                        // INFORM01은 Other일때만 나타난다.
                        if (ChartRenderer.DisplayLevel == 3)
                        {
                            // Delete된 Object가 아니거나 Update Review해야할 경우에만 추가한다.
                            if (Header.UpdateType != 12 || (Header.UpdateType == 12 && S57ChartOption.UpdateReview))
                            {
                                var stInform = new ST_OVER_INFORM();
                                stInform.Type = 0;
                                stInform.UpdateType = Header.UpdateType;
                                stInform.Pivot = AreaSKPath.Pivot;
                                stInform.ManualUpdateReview = false;
                                stOver.ListInform.Add(stInform);
                            }
                        }
                    }
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
            
            int nEdge = LiedgeComCS.Count;
            for (int k = 0; k < nEdge; k++)
            {
                int index = LiedgeComCS[k].EdgeIndex;
                if (Points.Shape.EdgeArr[index].Mask == 1) continue;

                var chartPath = CreateSKPath_EdgeGeo(context.Transform, index);
                if (chartPath == null) continue;

                var rgb = WeatherColor.GetColor(LiedgeComCS[k].ComLS.ColorIndex);

                float[] intervals = null;
                if (LiedgeComCS[k].ComLS.Style == 1)
                {
                    intervals = new[] { 10.0f, 10.0f };
                }
                else if (LiedgeComCS[k].ComLS.Style == 2)
                {
                    intervals = new[] { 3.0f, 3.0f };
                }

                chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), intervals, 0.0f, LiedgeComCS[k].ComLS.Width, IsQuerySelect);
                if (chartPath.MainSkPath != null) ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine);

                if (context.Transform.PointContainScreen(chartPath.Pivot) == true)
                {
                    if (LiedgeComCS[k].VALDCO != float.MaxValue)
                    {
                        List<short> listSY = new List<short>();
                        ChartRenderer.CS_SAFCON01(LiedgeComCS[k].VALDCO, listSY);
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
            if (ChartRenderer.IsQuery == false || S57ChartQueryOptions.QueryAreaOn == false) return false;

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
