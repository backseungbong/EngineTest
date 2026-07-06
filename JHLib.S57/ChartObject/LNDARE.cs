using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;
using System.Data;

namespace JHLib.S57.ChartObject
{
    public class LNDARE : ObjBase
    {
        public LNDARE(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public ST_LNDARE_HEADER Header;
        public ST_COM Com;

        // Draw함수 
        public void Draw(GraphicsContext context, ref ST_OVER over)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            float X = Header.Pivot.X / ScaleFactor;
            float Y = Header.Pivot.Y / ScaleFactor;
            var pivot = context.Transform.WGS84ToScreen((double)X, (double)Y);

            byte btComIndex = 0;
            if(Header.PRIM == 1)
            {
                if (S57ChartOption.PaperSimple == true) btComIndex = 1;
            }
            else if(Header.PRIM == 3)
            {
                if (S57ChartOption.PlainSymbolized == true) btComIndex = 1;

                bool bCalcCenter = false;
                if (Com.ArrSY != null && Com.ArrSY.Length > 0) bCalcCenter = true;
                CreateSKPath_Shape(context.Transform, bCalcCenter);
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
                                if (chartPath != null) 
                                {
                                    chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), intervals, 0.0f, Com.ArrLS[k].Width, IsQuerySelect);
                                    chartPath.Dispose();
                                }
                            }
                        }
                    }
                }

                if (Com.ArrSY != null)
                {
                    var nSY = Com.ArrSY.Length;
                    if (nSY > 0)
                    {
                        CheckComStartEnd(btComIndex, nSY, 4, Com.ArrComInfo, out int nComStart, out int nComEnd);
                        for (int k = nComStart; k < nComEnd; k++)
                        {
                            if (context.Transform.PointContainScreen(pivot) == false) continue;

                            float fAngle = 0.0f;
                            if (Com.ArrSY[k].Angle != 0.0) fAngle = Com.ArrSY[k].Angle + (float)context.Transform.Rotation;
                            else fAngle = Com.ArrSY[k].Angle;

                            ChartRenderer.DrawSymbol(context, Com.ArrSY[k].Index, fAngle, pivot, Header.UpdateType);
                        }
                    }
                }

                if(Com.ArrTX != null)
                {
                    var nTX = Com.ArrTX.Length;
                    if(nTX > 0)
                    {
                        CheckComStartEnd(btComIndex, nTX, 5, Com.ArrComInfo, out int nComStart, out int nComEnd);
                        for (int k = nComStart; k < nComEnd; k++)
                        {
                            if (context.Transform.PointContainScreen(pivot) == false) continue;
                            if (ChartRenderer.FindTextGroup(Com.ArrTX[k].TextGroup) == false) continue;
                            if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, pivot) == true) continue;
                            if (string.IsNullOrEmpty(Com.ArrTX[k].Text) == true) continue;

                            // Over에 그리기 위해서 아래와 같이 처리하였다
                            var stTX = new ST_OVER_TEXT();
                            stTX.Pivot = pivot;
                            stTX.ComTX.Offset = Com.ArrTX[k].Offset;
                            stTX.ComTX.TextAlign = Com.ArrTX[k].TextAlign;
                            stTX.ComTX.TextGroup = Com.ArrTX[k].TextGroup;
                            stTX.ComTX.TextColorIndex = Com.ArrTX[k].TextColorIndex;
                            stTX.ComTX.Text = Com.ArrTX[k].Text;
                            stTX.ComTX.NationalText = Com.ArrTX[k].NationalText;
                            over.ListText.Add(stTX);
                        }
                    }
                }

                // Pivot점이 다른 차트에 의해 가려지지 않으면
                if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, pivot) == false)
                {
                    // INFORM 속성이 있으면
                    if (S57ChartOption.HighlightInfo == true && Header.Highlight == true)
                    {
                        // INFORM01은 Other일때만 나타난다.
                        if (ChartRenderer.DisplayLevel == 3)
                        {
                            // Delete된 Object가 아니거나 Update Review해야할 경우에만 추가한다.
                            if (Header.UpdateType != 12 || (Header.UpdateType == 12 && S57ChartOption.UpdateReview))
                            {
                                // Over에 그리기 위해서 아래와 같이 처리하였다
                                var Inform = new ST_OVER_INFORM();
								Inform.Type = 0;
								Inform.UpdateType = Header.UpdateType;
								Inform.Pivot = pivot;
								Inform.ManualUpdateReview = false;
                                over.ListInform.Add(Inform);
                            }
                        }
                    }

                    // Query 선택 표시
                    if (IsQuerySelect == true && Header.PRIM == 1) DrawSelectQueryPoint(context, pivot);
                }
            }

            Dispose();
        }

        // Draw World Map 함수
        public void DrawWorldMap(GraphicsContext context)
        {
            CreateSKPath_Shape(context.Transform);

            if(AreaSKPath.MainSkPath != null)
            {
                var rgb = WeatherColor.GetColor("LANDA");
                AreaSKPath.DrawSKPathArea(context, new SKColor(rgb.R, rgb.G, rgb.B));
            }

            Dispose();
        }

        // Query함수
        public bool Query(Transform projection, Float2D point)
        {
            if (ChartRenderer.IsQuery == false || Header.UpdateType == 2 || Header.UpdateType == 12) return false;

            IsQuerySelect = false;
            switch (Header.PRIM)
            {
                case 1:         // Point
                    if (S57ChartQueryOptions.QueryPointOn == true)
                    {
                        float X = Header.Pivot.X / ScaleFactor;
                        float Y = Header.Pivot.Y / ScaleFactor;
                        var sxy = projection.WGS84ToScreen(X, Y);
                        var rect = new SKRect(sxy.X - 10, sxy.Y - 10, sxy.X + 10, sxy.Y + 10);
                        IsQuerySelect = rect.Contains(point.X, point.Y);
                    }
                    break;

                case 2:         // Line
                    if (S57ChartQueryOptions.QueryLineOn == true)
                    {
                        int nSize = Points.PointsHeader.Edge;
                        for (int i = 0; i < nSize; i++)
                        {
                            if (Points.Shape.EdgeArr[i].Mask == 1) continue;

                            var chartPath = CreateSKPath_EdgeGeo(projection, i);
                            if (chartPath != null)
                            {
                                IsQuerySelect = chartPath.IsContainSKpathLine(point);
                                chartPath.Dispose();
                            }

                            if (IsQuerySelect == true) break;
                        }
                    }
                    break;

                case 3:         // Area
                    if (S57ChartQueryOptions.QueryAreaOn == true)
                    {
                        CreateSKPath_Shape(projection);
                        IsQuerySelect = AreaSKPath.IsContainSKPathGroup(point);
                        Dispose();
                    }
                    break;
            }

            return IsQuerySelect;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
