using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;
using System.Data;

namespace JHLib.S57.ChartObject
{
    public class META : ObjBase
    {
        public META(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public ST_META_HEADER Header;
        public ST_COM Com;

        public bool IsLinkEdgeMaskOK = false;
        public bool IsMaskChange = false;

        // Point 정보를 저장할 어레이
        public Float2D Pivot = new Float2D(float.MinValue, float.MinValue);

        // Point World좌표로 정보를 만드는 함수 
        public void CreatePoint(Transform projection)
        {
            if(Points.Shape.PathPoint.Length > 0)
            {
                float X = Points.Shape.PathPoint[0].X / ScaleFactor;
                float Y = Points.Shape.PathPoint[0].Y / ScaleFactor;
                Pivot = projection.WGS84ToScreen(new Float2D(X, Y));
            }
        }

        // Draw함수 
        public void Draw(GraphicsContext context, ref ST_OVER stOver, SKCanvas canvasOver = null)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            // Header.OBJL == 308 : M_QUAL에 대해서 처리 
            if (S57ChartOption.LowAccuracy == false && Header.OBJL == 308) return;

            if (ChartRenderer.FindViewingGroup(Header.ViewingGroup) == false) return;

            byte btComIndex = 0;
            if (Header.PRIM == 1)
            {
                if (S57ChartOption.PaperSimple == true) btComIndex = 1;
                CreatePoint(context.Transform);
            }
            else if (Header.PRIM == 3)
            {
                if (S57ChartOption.PlainSymbolized == true) btComIndex = 1;
                CreateSKPath_Shape(context.Transform, true);
                Pivot = AreaSKPath.Pivot;
            }

            // Mask에 대한 처리를 위해서 추가함
            // Chart 1에서는 Mask를 해지하지 않아야 해서 다시 막음 <- 이 부분이 추후 인증 때 어떤 영향을 줄 지 다시 확인이 필요함
            //if (Header.PRIM >= 2) ChangeMask_Meta();

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
                        CheckComStartEnd(btComIndex, nLC, 3, Com.ArrComInfo, out int nComStart, out int nComEnd);
                        for (int k = nComStart; k < nComEnd; k++)
                        {
                            for (int m = 0; m < Points.PointsHeader.Edge; m++)
                            {
                                if (Points.Shape.EdgeArr[m].Mask == 1) continue;

                                var chartPath = CreateSKPath_EdgeGeo(context.Transform, m);
                                if(chartPath != null)
                                {
                                    ChartRenderer.DrawSymbolizedLine(context, Com.ArrLC[k], chartPath.PathLine);
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
                            bool bAreaInCenterSym = true;
                            if (Header.PRIM == 3)
                            {
                                // 교차 영역이 있으면
                                if (AreaSKPath.PathsIntersect != null)
                                {
                                    bAreaInCenterSym = CheckAreaInCenterSymbol(AreaSKPath.PathsIntersect, Com.ArrSY[k].Index, AreaSKPath.Pivot);
                                    if (bAreaInCenterSym == true) Pivot = AreaSKPath.Pivot;
                                }
                                else bAreaInCenterSym = false;
                            }

                            if (bAreaInCenterSym == true)
                            {
                                if (context.Transform.PointContainScreen(Pivot) == true)
                                {
                                    float angle = 0.0f;
                                    if (Com.ArrSY[k].Angle != 0.0) angle = Com.ArrSY[k].Angle + (float)context.Transform.Rotation;
                                    else angle = Com.ArrSY[k].Angle;

                                    ChartRenderer.DrawSymbol(context, Com.ArrSY[k].Index, angle, Pivot, Header.UpdateType);
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
                        CheckComStartEnd(btComIndex, nTX, 5, Com.ArrComInfo, out int nComStart, out int nComEnd);
                        for (int k = nComStart; k < nComEnd; k++)
                        {
                            if (ChartRenderer.FindTextGroup(Com.ArrTX[k].TextGroup) == false) continue;
                            if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, Pivot) == true) continue;
                            if (string.IsNullOrEmpty(Com.ArrTX[k].Text) == true) continue;

                            // Over에 그리기 위해서 아래와 같이 처리하였다
                            var stTX = new ST_OVER_TEXT();
                            stTX.Pivot = Pivot;
                            stTX.ComTX.Offset = Com.ArrTX[k].Offset;
                            stTX.ComTX.TextAlign = Com.ArrTX[k].TextAlign;
                            stTX.ComTX.TextGroup = Com.ArrTX[k].TextGroup;
                            stTX.ComTX.TextColorIndex = Com.ArrTX[k].TextColorIndex;
                            stTX.ComTX.Text = Com.ArrTX[k].Text;
                            stTX.ComTX.NationalText = Com.ArrTX[k].NationalText;
                            stOver.ListText.Add(stTX);
                        }
                    }
                }

                if(S57ChartOption.LowAccuracy == true && Header.LowAccuracy == true && Header.PRIM != 3)
                {
                    var nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC01");
                    ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, Pivot, Header.UpdateType);
                }

                // Pivot점이 다른 차트에 의해 가려지지 않으면
                if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, Pivot) == false)
                {
                    // INFORM 속성이 있으면
                    if (S57ChartOption.HighlightInfo == true && Header.Highlight >= 10)
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
								Inform.Pivot = Pivot;
								Inform.ManualUpdateReview = false;
                                stOver.ListInform.Add(Inform);
                            }
                        }
                    }
                }
            }

            Dispose();
        }

        // Query함수
        public bool Query(Transform projection, Float2D point)
        {
            if (ChartRenderer.IsQuery == false || S57ChartQueryOptions.QueryAreaOn == false || Header.UpdateType == 2 || Header.UpdateType == 12) return false;

            CreateSKPath_Shape(projection);

            if(AreaSKPath != null)
            {
                IsQuerySelect = AreaSKPath.IsContainSKPathGroup(point);
            }

            Dispose();

            return IsQuerySelect;
        }

        private void ChangeMask_Meta()
        {
            // 현재 Display되는 상태와 같은 CategoryNum를 가지고 있고, Manager에서 LinkEdgeMask를 거친 Object이면 vecEdge의 Mask를 모두 풀어준다.
            if (IsLinkEdgeMaskOK == true)
            {
                byte btCategoryNum = ChartRenderer.GetViewingGroupToCategoryNum(Header.ViewingGroup);
                if ((ChartRenderer.DisplayLevel == 0 && btCategoryNum == 0) || (ChartRenderer.DisplayLevel >= 1 && btCategoryNum == 1))
                {
                    int nPt = Points.Shape.EdgeArr.Length;
                    for (int i = 0; i < nPt; i++)
                    {
                        if (Points.Shape.EdgeArr[i].Mask == 1)
                        {
                            Points.Shape.EdgeArr[i].Mask = 3;
                            IsMaskChange = true;
                        }
                    }
                }
                else
                {
                    // Mask를 변경하였으면 원상태로 돌려 놓는다.
                    if (IsMaskChange == true)
                    {
                        IsMaskChange = false;

                        int nPt = Points.Shape.EdgeArr.Length;
                        for (int i = 0; i < nPt; i++)
                        {
                            if (Points.Shape.EdgeArr[i].Mask == 3)
                            {
                                Points.Shape.EdgeArr[i].Mask = 1;
                            }
                        }
                    }
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
