using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class UNSARE : ObjBase
    {
        public UNSARE(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public ST_UNSARE_HEADER Header;
        public ST_COM Com;

        public void Draw(GraphicsContext context)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            byte btComIndex = 0;
            if (S57ChartOption.PlainSymbolized == true) btComIndex = 1;

            if (Com.ComSize > 0)
            {
                CreateSKPath_Shape(context.Transform);

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
                                if (chartPath != null)
                                {
                                    chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), intervals, 0.0f, Com.ArrLS[k].Width, IsQuerySelect);
                                    if (chartPath.MainSkPath != null) ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine);
                                    chartPath.Dispose();
                                }
                            }
                        }
                    }
                }

                Dispose();
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
