using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class SLCONS : ObjBase
    {
        public SLCONS(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public ST_SLCONS_HEADER Header;
        public ST_COM Com;
        public List<ST_EDGE_COM> ListEdgeCom = new();

        // Draw함수 
        public void Draw(GraphicsContext context, ref ST_OVER over)
        {
            if (IsDraw(Header.UpdateType) == false) return;
            if (Com.ComSize == 0 || ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == false) return;

            float X = Header.Pivot.X / ScaleFactor;
            float Y = Header.Pivot.Y / ScaleFactor;
            var pivot = context.Transform.WGS84ToScreen((double)X, (double)Y);

            byte comIndex = 0;
            if (Header.PRIM == 1)
            {
                if (S57ChartOption.PaperSimple == true) comIndex = 1;
            }
            else if (Header.PRIM == 3)
            {
                if (S57ChartOption.PlainSymbolized == true) comIndex = 1;

                bool bCalcCenter = false;
                if (Com.ArrSY != null && Com.ArrSY.Length > 0) bCalcCenter = true;
                CreateSKPath_Shape(context.Transform, bCalcCenter);
            }

            if (Com.ComSize > 0)
            {
                var paint = new SKPaint()
                {
                    Color = new SKColor(0, 0, 0),
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round,
                    StrokeWidth = 1.0f
                };

                if (Com.ArrAC != null && AreaSKPath.MainSkPath != null)
                {
                    var nAC = Com.ArrAC.Length;
                    if (nAC > 0)
                    {
                        CheckComStartEnd(comIndex, nAC, 0, Com.ArrComInfo, out int nComStart, out int nComEnd);
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
                        CheckComStartEnd(comIndex, nAP, 1, Com.ArrComInfo, out int nComStart, out int nComEnd);
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
                        CheckComStartEnd(comIndex, nLS, 2, Com.ArrComInfo, out int nComStart, out int nComEnd);
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
                                    ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine);
                                    chartPath.Dispose();
                                }
                            }
                        }
                    }
                }

                if (Com.ArrLC != null)
                {
                    var nLC = Com.ArrLC.Length;
                    if (nLC > 0)
                    {
                        CheckComStartEnd(comIndex, nLC, 3, Com.ArrComInfo, out int nComStart, out int nComEnd);
                        for (int k = nComStart; k < nComEnd; k++)
                        {
                            for (int m = 0; m < Points.PointsHeader.Edge; m++)
                            {
                                if (Points.Shape.EdgeArr[m].Mask == 1) continue;

                                bool reverse = Points.Shape.EdgeArr[m].Reverse;

                                var chartPath = CreateSKPath_EdgeGeo(context.Transform, m);
                                if (chartPath != null)
                                {
                                    if (chartPath.MainSkPath != null)
                                    {
                                        ChartRenderer.DrawSymbolizedLine(context, Com.ArrLC[k], chartPath.PathLine, reverse);
                                        ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine, reverse);
                                    }
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
                        CheckComStartEnd(comIndex, nSY, 4, Com.ArrComInfo, out int nComStart, out int nComEnd);
                        for (int k = nComStart; k < nComEnd; k++)
                        {
                            bool bAreaInCenterSym = true;
                            if (Header.PRIM == 3 && k == nComStart)
                            {
                                // 교차 영역이 있으면
                                if (AreaSKPath.PathsIntersect != null)
                                {
                                    // Ownship과 Overlap된 Center Symbol에 대해서 Offset 처리함
                                    bool overlab = false;
                                    overlab = CheckOwnshipOverlabCenterSymbol(ChartRenderer.GetOwnshipArea(), Com.ArrSY[k].Index, AreaSKPath.Pivot);
                                    pivot = AreaSKPath.Pivot;
                                    if (overlab)
                                    {
                                        pivot.X += 50;
                                        pivot.Y += 50;
                                    }

                                    bAreaInCenterSym = CheckAreaInCenterSymbol(AreaSKPath.PathsIntersect, Com.ArrSY[k].Index, pivot);
                                }
                                else bAreaInCenterSym = false;
                            }

                            if (bAreaInCenterSym == true)
                            {
                                // Manual Update가 되어 있으면 기존 Update가 안된상태로 바꿔준다.
                                if (context.Transform.PointContainScreen(pivot) == true)
                                {
                                    float fAngle = 0.0f;
                                    if (Com.ArrSY[k].Angle != 0.0) fAngle = Com.ArrSY[k].Angle + (float)context.Transform.Rotation;
                                    else fAngle = Com.ArrSY[k].Angle;

                                    ChartRenderer.DrawSymbol(context, Com.ArrSY[k].Index, fAngle, pivot, Header.UpdateType);
                                }
                            }
                        }
                    }
                }

                if (Com.ArrTX != null)
                {
                    var nTX = Com.ArrTX.Length;
                    if (nTX > 0)
                    {
                        CheckComStartEnd(comIndex, nTX, 5, Com.ArrComInfo, out int nComStart, out int nComEnd);
                        for (int k = nComStart; k < nComEnd; k++)
                        {
                            if (ChartRenderer.FindTextGroup(Com.ArrTX[k].TextGroup) == false) continue;
                            if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, pivot) == true) continue;
                            if (string.IsNullOrEmpty(Com.ArrTX[k].Text) == true) continue;

                            // Over에 그리기 위해서 아래와 같이 처리하였다
                            var Tx = new ST_OVER_TEXT();
                            Tx.Pivot = pivot;
                            Tx.ComTX.Offset = Com.ArrTX[k].Offset;
                            Tx.ComTX.TextAlign = Com.ArrTX[k].TextAlign;
                            Tx.ComTX.TextGroup = Com.ArrTX[k].TextGroup;
                            Tx.ComTX.TextColorIndex = Com.ArrTX[k].TextColorIndex;
                            Tx.ComTX.Text = Com.ArrTX[k].Text;
							Tx.ComTX.NationalText = Com.ArrTX[k].NationalText;
                            over.ListText.Add(Tx);
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

                paint.Dispose();
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
