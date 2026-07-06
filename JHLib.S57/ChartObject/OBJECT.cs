using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Util.Time;
using JHLib.Weather;
using Legacy.ECM_Core.Definition;
using Legacy.ECM_Core.ENC;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class OBJECT : ObjBase
    {
        public OBJECT(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public ST_OBJECT_HEADER Header;
        public ST_COM Com;
        public ST_EDGE_MASK[] EdgeMask;

        public bool IsLinkEdgeMaskOK = false;
        public bool IsMaskChange = false;
        public bool IsDateDependent = false;

        // 현재 Object와 Edge가 겹쳐있는 Object가 최초에는 Priority가 낮아서 Edge Mask를 하지 않았음
        // 이 Object와 겹쳐있는 Object가 CS에 의해서 Priority가 승격되면서 현재 Object의 Edge를 Mask해야 하는경우 사용
        public bool IsEdgeMask = false;
        public int MaskEdgeNum = -1;

        // Draw함수 
        public void Draw(GraphicsContext context, ref ST_OVER over)
        {
            if(IsDraw(Header.UpdateType) == false) return;

            float X = Header.Pivot.X / ScaleFactor;
            float Y = Header.Pivot.Y / ScaleFactor;
            Float2D ptPivot = context.Transform.WGS84ToScreen((double)X, (double)Y);

            if (ChartRenderer.FindViewingGroup(Header.GroupLayer) == true && 
                ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == true &&
                ChartRenderer.CheckDateDependent(Header.StartDate, Header.EndDate, ref IsDateDependent, S57ChartOption.DateDependentType == 0 ? true : false, S57ChartOption.StartDate, S57ChartOption.EndDate) == true)
            {
                byte btComIndex = 0;
                if (Header.PRIM == 1)
                {
                    if (S57ChartOption.PaperSimple == true) btComIndex = 1;
                }
                else if (Header.PRIM == 3)
                {
                    if (S57ChartOption.PlainSymbolized == true) btComIndex = 1;

                    bool bCalcCenter = false;
                    if (Com.ArrSY != null && Com.ArrSY.Length > 0) bCalcCenter = true;
                    CreateSKPath_Shape(context.Transform, bCalcCenter);
                }

                // Mask 처리 해지를 위해서 추가함
                //if (Header.PRIM >= 2) ChangeMask_Object();

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
                                    if (IsEdgeMask && m == MaskEdgeNum) continue;

                                    var chartPath = CreateSKPath_EdgeGeo(context.Transform, m);
                                    if(chartPath != null) 
                                    {
                                        chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), intervals, 0.0f, Com.ArrLS[k].Width, IsQuerySelect);
                                        if(chartPath.PathLine != null) ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine);
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
                                    if (IsEdgeMask && m == MaskEdgeNum) continue;

                                    bool reverse = Points.Shape.EdgeArr[m].Reverse;

                                    var chartPath = CreateSKPath_EdgeGeo(context.Transform, m);
                                    if(chartPath == null) continue;

                                    if (chartPath.MainSkPath != null)
                                    {
                                        // INDHLT02 심볼로 라인을 그리라는 것이면 그냥 BKAJ1컬러, Width = 4로 그리고 
                                        // 그 위에 CHYLW컬러, Width = 2로 그린다.
                                        if(Com.ArrLC[k] == 54)
                                        {
                                            var rgb = WeatherColor.GetColor("BKAJ1");
                                            chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), null, 0.0f, 4, false);
                                            rgb = WeatherColor.GetColor("CHYLW");
                                            chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), null, 0.0f, 2, false);
                                        }
                                        else ChartRenderer.DrawSymbolizedLine(context, Com.ArrLC[k], chartPath.PathLine, reverse);
                                        ChartRenderer.DrawUpdateSymbolizedLine(context, Header.UpdateType, chartPath.PathLine, reverse);
                                    }
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
                            CheckComStartEnd(btComIndex, nSY, 4, Com.ArrComInfo, out int nComStart, out int nComEnd);
                            for (int k = nComStart; k < nComEnd; k++)
                            {
                                bool bAreaInCenterSym = true;
                                if (Header.PRIM == 3)
                                {
                                    // 교차 영역이 있으면
                                    if (AreaSKPath.PathsIntersect != null)
                                    {
                                        // Ownship과 Overlap된 Center Symbol에 대해서 Offset 처리함
                                        bool overlab = false;
                                        overlab = CheckOwnshipOverlabCenterSymbol(ChartRenderer.GetOwnshipArea(), Com.ArrSY[k].Index, AreaSKPath.Pivot);

                                        ptPivot = AreaSKPath.Pivot;
                                        if (overlab)
                                        {
                                            ptPivot.X += 50;
                                            ptPivot.Y += 50;
                                        }

                                        bAreaInCenterSym = CheckAreaInCenterSymbol(AreaSKPath.PathsIntersect, Com.ArrSY[k].Index, ptPivot);
                                    }
                                    else bAreaInCenterSym = false;
                                }

                                if (bAreaInCenterSym == true)
                                {
                                    if (context.Transform.PointContainScreen(ptPivot) == true)
                                    {
                                        float angle = 0.0f;
                                        if (Com.ArrSY[k].Angle != 0.0) angle = Com.ArrSY[k].Angle + (float)context.Transform.Rotation;
                                        else angle = Com.ArrSY[k].Angle;

                                        ChartRenderer.DrawSymbol(context, Com.ArrSY[k].Index, angle, ptPivot, Header.UpdateType);
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
                                if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, ptPivot) == true) continue;
                                if (string.IsNullOrEmpty(Com.ArrTX[k].Text) == true) continue;

                                // Over에 그리기 위해서 아래와 같이 처리하였다
                                var stTX = new ST_OVER_TEXT();
                                stTX.Pivot = ptPivot;
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
                }

                if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, ptPivot) == false)
                {
                    // INFORM 속성이 있으면
                    if (S57ChartOption.HighlightInfo == true && (Header.Highlight == 1 || Header.Highlight == 11))
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
                                Inform.Pivot = ptPivot;
                                Inform.ManualUpdateReview = false;
                                over.ListInform.Add(Inform);
                            }
                        }
                    }

                    // Highlight Document
                    if (S57ChartOption.HighlightDocument == true && Header.Highlight >= 10)
                    {
                        // INFORM01은 Other일때만 나타난다.
                        if (ChartRenderer.DisplayLevel == 3)
                        {
                            // Delete된 Object가 아니거나 Update Review해야할 경우에만 추가한다.
                            if (Header.UpdateType != 12 || (Header.UpdateType == 12 && S57ChartOption.UpdateReview))
                            {
                                // Over에 그리기 위해서 아래와 같이 처리하였다
                                var Inform = new ST_OVER_INFORM();
                                Inform.Type = 1;
                                Inform.UpdateType = Header.UpdateType;
                                Inform.Pivot = ptPivot;
                                Inform.ManualUpdateReview = false;
                                over.ListInform.Add(Inform);
                            }
                        }
                    }

                    // Highlight Date Dependent
                    if (S57ChartOption.HighlightDateDependent == true && IsDateDependent == true)
                    {
                        // Delete된 Object가 아니거나 Update Review해야할 경우에만 추가한다.
                        if (Header.UpdateType != 12 || (Header.UpdateType == 12 && S57ChartOption.UpdateReview))
                        {
                            // Over에 그리기 위해서 아래와 같이 처리하였다
                            var Inform = new ST_OVER_INFORM();
                            Inform.Type = 2;
                            Inform.UpdateType = Header.UpdateType;
                            Inform.Pivot = ptPivot;
                            Inform.ManualUpdateReview = false;
                            over.ListInform.Add(Inform);
                        }
                    }

                    // Query 선택 표시
                    if (IsQuerySelect == true && Header.PRIM == 1) DrawSelectQueryPoint(context, ptPivot);
                }

                Dispose();
            }
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
                            if(chartPath != null)
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

        public bool MUquery(Transform projection, Float2D point, ref Float2D pivot)
        {
            if (Header.UpdateType == 2 || Header.UpdateType == 12) return false;

            pivot.X = Header.Pivot.X / ScaleFactor;
            pivot.Y = Header.Pivot.Y / ScaleFactor;
            var sxy = projection.WGS84ToScreen(pivot);
            var rect = new SKRect(sxy.X - 10, sxy.Y - 10, sxy.X + 10, sxy.Y + 10);
            return rect.Contains(point.X, point.Y);
        }

        public void GetMUqueryResult(List<MUsymbolInfo> listSY)
        {
            byte comIndex = 0;
            if (S57ChartOption.PaperSimple == true) comIndex = 1;

            if (Com.ArrSY != null)
            {
                var nSY = Com.ArrSY.Length;
                if (nSY > 0)
                {
                    CheckComStartEnd(comIndex, nSY, 4, Com.ArrComInfo, out int nComStart, out int nComEnd);
                    for (int k = nComStart; k < nComEnd; k++)
                    {
                        float angle = 0.0f;
                        if (Com.ArrSY[k].Angle != 0.0) angle = Com.ArrSY[k].Angle;
                        else angle = Com.ArrSY[k].Angle;
                        listSY.Add(new MUsymbolInfo(Com.ArrSY[k].Index, angle));
                    }
                }
            }
        }

        private void ChangeMask_Object()
        {
            // 현재 Display되는 상태와 같은 CategoryNum를 가지고 있고, Manager에서 LinkEdgeMask를 거친 Object이면 vecEdge의 Mask를 모두 풀어준다.
            if (IsLinkEdgeMaskOK == true)
            {
                byte btCategoryNum = ChartRenderer.GetViewingGroupToCategoryNum(Header.GroupLayer);
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
