using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using Legacy.ECM_Core.ENC;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class OBSTRN : ObjBase
    {
        public OBSTRN(S57ChartRenderer chartRenderer) : base(chartRenderer) { }

        public bool IsOBSTRN = true;
        public ST_OBSTRN_HEADER Header;
        public ST_COM Com;
        public ST_DANGER_ATTR DangerAttr;

        public bool IsDanger = false;
        public byte Priority = 0;
        public bool IsChangePriority = false;
        public byte OriViewingGroup;
        public bool IsLinkEdgeMaskOK = false;
        public bool IsMaskChange = false;

        // Draw함수 
        public void Draw(int layerIndex, GraphicsContext context, ref ST_OVER over)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            if (IsOBSTRN == true)
            {
                if (Header.PRIM >= 2) ChangeMask_Obstrn();

                if (layerIndex == 8 || IsChangePriority == false)
                {
                    if (IsDanger == true || (IsDanger == false && layerIndex == 8) ||
                        (IsDanger == false && ChartRenderer.FindViewingGroup(Header.ViewingGroup) == true))
                    {
                        if (ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == true)
                        {
                            if (DangerAttr.DEPTH_VALUE == float.MaxValue)
                            {
                                // Normal Draw
                                DrawNormal(context, ref over);
                            }
                            else
                            {
                                // CS Draw
                                DrawObstrnCS(context, ref over);
                            }
                        }
                    }
                }
            }
            // WRECKS일경우
            else
            {
                if (Header.PRIM == 3) ChangeMask_Obstrn();

                if (layerIndex == 8 || IsChangePriority == false)
                {
                    if (IsDanger == true || (IsDanger == false && layerIndex == 8) ||
                        (IsDanger == false && ChartRenderer.FindViewingGroup(Header.ViewingGroup) == true))
                    {
                        if (ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == true)
                        {
                            if (DangerAttr.DEPTH_VALUE == float.MaxValue)
                            {
                                // Normal Draw
                                DrawNormal(context, ref over);
                            }
                            else
                            {
                                // CS Draw
                                DrawWrecksCS(context, ref over);
                            }
                        }
                    }
                }
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
                        var sxy = projection.WGS84ToScreen(X,Y);
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

        // Manual Update Query함수
        public bool MUquery(Transform projection, Float2D point, ref Float2D pivot)
        {
            if (Header.UpdateType == 2 || Header.UpdateType == 12) return false;

            if(Header.PRIM == 1)
            {
                pivot.X = Header.Pivot.X / ScaleFactor;
                pivot.Y = Header.Pivot.Y / ScaleFactor;
                var sxy = projection.WGS84ToScreen(pivot);
                var rect = new SKRect(sxy.X - 10, sxy.Y - 10, sxy.X + 10, sxy.Y + 10);
                return rect.Contains(point.X, point.Y);
            }

            return false;
        }

        public void GetMUqueryResult(List<MUsymbolInfo> listSY)
        {
            byte comIndex = 0;
            if (S57ChartOption.PaperSimple == true) comIndex = 1;

            if (IsOBSTRN == true)
            {
                if (DangerAttr.DEPTH_VALUE == float.MaxValue)
                {
                    // Normal Draw
                    if (Com.ArrSY != null)
                    {
                        var nSY = Com.ArrSY.Length;
                        if (nSY > 0)
                        {
                            // Manual Update가 되어 있으면 기존 Update가 안된상태로 바꿔준다.
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
                else
                {
                    if (IsDanger == true)
                    {
                        if (S57ChartOption.ShallowWaterDangers == true || Header.ViewingGroup == 1)
                        {
                            var nSymIndex = ChartRenderer.GetSymbolIndex("ISODGR01");
                            listSY.Add(new MUsymbolInfo(nSymIndex));
                        }

                        if (S57ChartOption.LowAccuracy == true && DangerAttr.Accuracy == true)
                        {
                            var nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC01");
                            listSY.Add(new MUsymbolInfo(nSymIndex));
                        }
                    }
                    else
                    {
                        bool bSafety = false;
                        if (DangerAttr.VALSOU != float.MaxValue)
                        {
                            if (DangerAttr.VALSOU > S57ChartSafetyValue.SafetyDepth)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex("DANGER02");
                                listSY.Add(new MUsymbolInfo(nSymIndex));
                                bSafety = true;
                            }
                        }

                        if (bSafety == false)
                        {
                            if (Com.ArrSY != null)
                            {
                                int nSY = Com.ArrSY.Length;
                                if (nSY > 0)
                                {
                                    CheckComStartEnd(comIndex, nSY, 4, Com.ArrComInfo, out int nComStart, out int nComEnd);
                                    for (int k = nComStart; k < nComEnd; k++)
                                    {
                                        listSY.Add(new MUsymbolInfo(Com.ArrSY[k].Index, Com.ArrSY[k].Angle));
                                    }
                                }
                            }
                        }

                        if ((char)DangerAttr.Soundg1 != 'o')
                        {
                            string strSound = null;
                            if (S57ChartSafetyValue.SafetyDepth >= DangerAttr.DEPTH_VALUE) strSound = "SOUNDS";
                            else strSound = "SOUNDG";

                            string strAdd = null;
                            char[] chArr = new char[] { (char)DangerAttr.Soundg1, (char)DangerAttr.Soundg2 };
                            strAdd = new string(chArr);
                            int nLen = strAdd.Length / 2;
                            for (int m = 0; m < nLen; m++)
                            {
                                var strName = strSound + strAdd.Substring(m * 2, 2);
                                if (strName.Length == 8)
                                {
                                    var nSymIndex = ChartRenderer.GetSymbolIndex(strName);
                                    listSY.Add(new MUsymbolInfo(nSymIndex));
                                }
                            }
                        }

                        if (DangerAttr.VALSOU != float.MaxValue && DangerAttr.Sound == true)
                        {
                            List<int> listSoundIndex = new();
                            ChartRenderer.CS_SNDFRM04(DangerAttr.VALSOU, listSoundIndex);
                            foreach (var index in listSoundIndex)
                            {
                                listSY.Add(new MUsymbolInfo(index));
                            }
                        }
                    }
                }
            }
            // WRECKS일경우
            else
            {
                if (DangerAttr.DEPTH_VALUE == float.MaxValue)
                {
                    // Normal Draw
                    if (Com.ArrSY != null)
                    {
                        var nSY = Com.ArrSY.Length;
                        if (nSY > 0)
                        {
                            // Manual Update가 되어 있으면 기존 Update가 안된상태로 바꿔준다.
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
                else
                {
                    // CS Draw
                    if (IsDanger == true)
                    {
                        if (S57ChartOption.ShallowWaterDangers == true || Header.ViewingGroup == 1)
                        {
                            var nSymIndex = ChartRenderer.GetSymbolIndex("ISODGR01");
                            listSY.Add(new MUsymbolInfo(nSymIndex));
                        }
                    }
                    else
                    {
                        if (DangerAttr.VALSOU != float.MaxValue)
                        {
                            int nSymIndex = -1;
                            if (DangerAttr.VALSOU <= S57ChartSafetyValue.SafetyDepth) nSymIndex = ChartRenderer.GetSymbolIndex("DANGER01");
                            else nSymIndex = ChartRenderer.GetSymbolIndex("DANGER02");

                            listSY.Add(new MUsymbolInfo(nSymIndex));

                            if ((char)DangerAttr.Soundg1 != 'o')
                            {
                                string strSound = null;
                                if (S57ChartSafetyValue.SafetyDepth >= DangerAttr.DEPTH_VALUE) strSound = "SOUNDS";
                                else strSound = "SOUNDG";

                                string strAdd = null;
                                char[] chArr = new char[] { (char)DangerAttr.Soundg1, (char)DangerAttr.Soundg2 };
                                strAdd = new string(chArr);
                                int nLen = strAdd.Length / 2;
                                for (int m = 0; m < nLen; m++)
                                {
                                    var strName = strSound + strAdd.Substring(m * 2, 2);
                                    if (strName.Length == 8)
                                    {
                                        nSymIndex = ChartRenderer.GetSymbolIndex(strName);
                                        listSY.Add(new MUsymbolInfo(nSymIndex));
                                    }
                                }
                            }

                            if (DangerAttr.Sound == true)
                            {
                                List<int> listSoundIndex = new List<int>();
                                ChartRenderer.CS_SNDFRM04(DangerAttr.VALSOU, listSoundIndex);
                                foreach (var index in listSoundIndex)
                                {
                                    listSY.Add(new MUsymbolInfo(index));
                                }
                            }
                        }
                        else
                        {
                            if (Com.ArrSY != null)
                            {
                                var nSY = Com.ArrSY.Length;
                                if (nSY > 0)
                                {
                                    CheckComStartEnd(comIndex, nSY, 4, Com.ArrComInfo, out int nComStart, out int nComEnd);
                                    for (int k = nComStart; k < nComEnd; k++)
                                    {
                                        listSY.Add(new MUsymbolInfo(Com.ArrSY[k].Index, Com.ArrSY[k].Angle));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DrawNormal(GraphicsContext context, ref ST_OVER over, bool IsCS = false)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            float X = Header.Pivot.X / ScaleFactor;
            float Y = Header.Pivot.Y / ScaleFactor;
            Float2D pivot = context.Transform.WGS84ToScreen((double)X, (double)Y);

            byte comIndex = 0;
            if (Header.PRIM == 1)
            {
                if (S57ChartOption.PaperSimple == true) comIndex = 1;
            }
            else if (Header.PRIM == 3)
            {
                if (S57ChartOption.PlainSymbolized == true) comIndex = 1;

                bool calcCenterPoint = false;
                if (Com.ArrSY != null && Com.ArrSY.Length > 0) calcCenterPoint = true;
                CreateSKPath_Shape(context.Transform, calcCenterPoint);
            }

            if (Com.ComSize > 0)
            {
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
                                if (IsCS == false && Points.Shape.EdgeArr[m].Mask == 1) continue;

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
                                if(chartPath != null) 
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
                        // Manual Update가 되어 있으면 기존 Update가 안된상태로 바꿔준다.
                        CheckComStartEnd(comIndex, nSY, 4, Com.ArrComInfo, out int nComStart, out int nComEnd);
                        for (int k = nComStart; k < nComEnd; k++)
                        {
                            bool bAreaInCenterSym = true;
                            if (Header.PRIM == 3)
                            {
                                // 교차 영역이 있으면
                                if (AreaSKPath.PathsIntersect != null)
                                {
                                    bAreaInCenterSym = CheckAreaInCenterSymbol(AreaSKPath.PathsIntersect, Com.ArrSY[k].Index, AreaSKPath.Pivot);
                                    if (bAreaInCenterSym == true) pivot = AreaSKPath.Pivot;
                                }
                                else bAreaInCenterSym=false;
                            }

                            if (bAreaInCenterSym == true)
                            {
                                if (context.Transform.PointContainScreen(pivot) == true)
                                {
                                    float angle = 0.0f;
                                    if (Com.ArrSY[k].Angle != 0.0) angle = Com.ArrSY[k].Angle + (float)context.Transform.Rotation;
                                    else angle = Com.ArrSY[k].Angle;

                                    ChartRenderer.DrawSymbol(context, Com.ArrSY[k].Index, angle, pivot, Header.UpdateType);
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

                // Pivot점을 다른 차트가 가리지 않으면
                if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, pivot) == false)
                {
                    // Highlight Info
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

        public void DrawObstrnCS(GraphicsContext context, ref ST_OVER over, bool IsCS = false)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            float X = Header.Pivot.X / ScaleFactor;
            float Y = Header.Pivot.Y / ScaleFactor;
            var pivot = context.Transform.WGS84ToScreen((double)X, (double)Y);

            switch (Header.PRIM)
            {
                case 1:     // Point
                    {
                        if (IsDanger == true)
                        {
                            if (S57ChartOption.ShallowWaterDangers == true || Header.ViewingGroup == 1)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex("ISODGR01");
                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                            }

                            if (S57ChartOption.LowAccuracy == true && DangerAttr.Accuracy == true)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC01");
                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                            }
                        }
                        else
                        {
                            bool bSafety = false;
                            if (DangerAttr.VALSOU != float.MaxValue)
                            {
                                if (DangerAttr.VALSOU > S57ChartSafetyValue.SafetyDepth)
                                {
                                    var nSymIndex = ChartRenderer.GetSymbolIndex("DANGER02");
                                    ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                                    bSafety = true;
                                }
                            }

                            if (bSafety == false)
                            {
                                byte comIndex = 0;
                                if (S57ChartOption.PaperSimple == true) comIndex = 1;

                                if (Com.ArrSY != null)
                                {
                                    int nSY = Com.ArrSY.Length;
                                    if (nSY > 0)
                                    {
                                        CheckComStartEnd(comIndex, nSY, 4, Com.ArrComInfo, out int nComStart, out int nComEnd);
                                        for (int k = nComStart; k < nComEnd; k++)
                                        {
                                            ChartRenderer.DrawSymbol(context, Com.ArrSY[k].Index, Com.ArrSY[k].Angle, pivot, Header.UpdateType);
                                        }
                                    }
                                }
                            }

                            if ((char)DangerAttr.Soundg1 != 'o')
                            {
                                string strSound = null;
                                if (S57ChartSafetyValue.SafetyDepth >= DangerAttr.DEPTH_VALUE) strSound = "SOUNDS";
                                else strSound = "SOUNDG";

                                string strAdd = null;
                                char[] chArr = new char[] { (char)DangerAttr.Soundg1, (char)DangerAttr.Soundg2 };
                                strAdd = new string(chArr);
                                int nLen = strAdd.Length / 2;
                                for (int m = 0; m < nLen; m++)
                                {
                                    var strName = strSound + strAdd.Substring(m * 2, 2);
                                    if (strName.Length == 8)
                                    {
                                        var nSymIndex = ChartRenderer.GetSymbolIndex(strName);
                                        ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                                    }
                                }
                            }

                            if (DangerAttr.VALSOU != float.MaxValue && DangerAttr.Sound == true)
                            {
                                List<int> listSoundIndex = new();
                                ChartRenderer.CS_SNDFRM04(DangerAttr.VALSOU, listSoundIndex);
                                foreach (var index in listSoundIndex)
                                {
                                    ChartRenderer.DrawSymbol(context, index, 0.0f, pivot, Header.UpdateType);
                                }
                            }

                            if (S57ChartOption.LowAccuracy == true && DangerAttr.Accuracy == true)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC01");
                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                            }
                        }
                    }
                    break;

                case 2:     // Line
                    {
                        int nEdge = Points.Shape.EdgeArr.Length;
                        for (int k = 0; k < nEdge; k++)
                        {
                            if (IsCS == false && Points.Shape.EdgeArr[k].Mask == 1) continue;

                            var chartPath = CreateSKPath_EdgeGeo(context.Transform, k);
                            if (chartPath == null) continue;

                            if (chartPath.MainSkPath != null)
                            {
                                // LowAcc상태임
                                if (Points.Shape.EdgeArr[k].Quapos == false)
                                {
                                    int nSymIndex = -1;
                                    if (IsDanger == true) nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC41");
                                    else nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC31");

                                    ChartRenderer.DrawSymbolizedLine(context, nSymIndex, chartPath.PathLine);
                                }
                                else
                                {
                                    var rgb = WeatherColor.GetColor("CHBLK");

                                    if (IsDanger == true)
                                    {
                                        chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 3.0f, 3.0f }, 0.0f, 2.0f, IsQuerySelect);
                                    }
                                    else
                                    {
                                        if (DangerAttr.VALSOU != float.MaxValue)
                                        {
                                            if (DangerAttr.VALSOU <= S57ChartSafetyValue.SafetyDepth)
                                            {
                                                chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 3.0f, 3.0f }, 0.0f, 2.0f, IsQuerySelect);
                                            }
                                            else
                                            {
                                                chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 10.0f, 10.0f }, 0.0f, 2.0f, IsQuerySelect);
                                            }
                                        }
                                        else
                                        {
                                            chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 3.0f, 3.0f }, 0.0f, 2.0f, IsQuerySelect);
                                        }
                                    }
                                }
                            }

                            // 마지막 라인의 Pivot을 저장
                            if (k == nEdge - 1)
                            {
                                pivot = chartPath.Pivot;
                            }

                            chartPath.Dispose();

                        }

                        if (IsDanger == true)
                        {
                            if (S57ChartOption.ShallowWaterDangers == true || Header.ViewingGroup == 1)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex("ISODGR01");
                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                            }
                        }
                        else
                        {
                            if (DangerAttr.VALSOU != float.MaxValue)
                            {
                                if ((char)DangerAttr.Soundg1 != 'o')
                                {
                                    string strSound = null;
                                    if (S57ChartSafetyValue.SafetyDepth >= DangerAttr.DEPTH_VALUE) strSound = "SOUNDS";
                                    else strSound = "SOUNDG";

                                    string strAdd = null;
                                    char[] chArr = new char[] { (char)DangerAttr.Soundg1, (char)DangerAttr.Soundg2 };
                                    strAdd = new string(chArr);
                                    int nLen = strAdd.Length / 2;
                                    for (int m = 0; m < nLen; m++)
                                    {
                                        var strName = strSound + strAdd.Substring(m * 2, 2);
                                        if (strName.Length == 8)
                                        {
                                            var nSymIndex = ChartRenderer.GetSymbolIndex(strName);
                                            ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                                        }
                                    }
                                }

                                if (DangerAttr.Sound == true)
                                {
                                    List<int> listSoundIndex = new List<int>();
                                    ChartRenderer.CS_SNDFRM04(DangerAttr.VALSOU, listSoundIndex);
                                    foreach (var index in listSoundIndex)
                                    {
                                        ChartRenderer.DrawSymbol(context, index, 0.0f, pivot, Header.UpdateType);
                                    }
                                }
                            }
                        }
                    }
                    break;

                case 3:     // Area
                    {
                        CreateSKPath_Shape(context.Transform, true);

                        if (IsDanger == true)
                        {
                            var rgb = WeatherColor.GetColor("DEPVS");
                            if (AreaSKPath.MainSkPath != null)
                            {
                                AreaSKPath.DrawSKPathArea(context, new SKColor(rgb.R, rgb.G, rgb.B), IsQuerySelect);

                                var nPatterindex = ChartRenderer.GetPatterindex("FOULAR01");
                                ChartRenderer.DrawPattern(context, (byte)nPatterindex, AreaSKPath.MainSkPath);
                            }

                            rgb = WeatherColor.GetColor("CHBLK");

                            int nEdge = Points.Shape.EdgeArr.Length;
                            for (int k = 0; k < nEdge; k++)
                            {
                                if (IsCS == false && Points.Shape.EdgeArr[k].Mask == 1) continue;

                                var chartPath = CreateSKPath_EdgeGeo(context.Transform, k);
                                if (chartPath != null)
                                {
                                    chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 3.0f, 3.0f }, 0.0f, 2.0f, IsQuerySelect);
                                    chartPath.Dispose();
                                }
                            }

                            if (S57ChartOption.ShallowWaterDangers == true || Header.ViewingGroup == 1)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex("ISODGR01");
                                if (AreaSKPath.PathsIntersect != null)
                                {
                                    pivot = AreaSKPath.Pivot;
                                }

                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                            }
                        }
                        else
                        {
                            if (DangerAttr.VALSOU != float.MaxValue)
                            {
                                if (DangerAttr.VALSOU <= S57ChartSafetyValue.SafetyDepth)
                                {
                                    var rgb = WeatherColor.GetColor("CHBLK");

                                    int nEdge = Points.Shape.EdgeArr.Length;
                                    for (int k = 0; k < nEdge; k++)
                                    {
                                        if (IsCS == false && Points.Shape.EdgeArr[k].Mask == 1) continue;

                                        var chartPath = CreateSKPath_EdgeGeo(context.Transform, k);
                                        if (chartPath != null)
                                        {
                                            chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 3.0f, 3.0f }, 0.0f, 2.0f, IsQuerySelect);
                                            chartPath.Dispose();
                                        }
                                    }
                                }
                                else
                                {
                                    var rgb = WeatherColor.GetColor("CHGRD");

                                    int nEdge = Points.Shape.EdgeArr.Length;
                                    for (int k = 0; k < nEdge; k++)
                                    {
                                        if (IsCS == false && Points.Shape.EdgeArr[k].Mask == 1) continue;

                                        var chartPath = CreateSKPath_EdgeGeo(context.Transform, k);
                                        if (chartPath != null)
                                        {
                                            chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 10.0f, 10.0f }, 0.0f, 2.0f, IsQuerySelect);
                                            chartPath.Dispose();
                                        }
                                    }
                                }

                                if ((char)DangerAttr.Soundg1 != 'o')
                                {
                                    string strSound = null;
                                    if (S57ChartSafetyValue.SafetyDepth >= DangerAttr.DEPTH_VALUE) strSound = "SOUNDS";
                                    else strSound = "SOUNDG";

                                    string strAdd = null;
                                    char[] chArr = new char[] { (char)DangerAttr.Soundg1, (char)DangerAttr.Soundg2 };
                                    strAdd = new string(chArr);
                                    int nLen = strAdd.Length / 2;
                                    for (int m = 0; m < nLen; m++)
                                    {
                                        var strName = strSound + strAdd.Substring(m * 2, 2);
                                        if (strName.Length == 8)
                                        {
                                            var nSymIndex = ChartRenderer.GetSymbolIndex(strName);
                                            ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                                        }
                                    }
                                }

                                if (DangerAttr.Sound == true)
                                {
                                    List<int> listSoundIndex = new List<int>();
                                    ChartRenderer.CS_SNDFRM04(DangerAttr.VALSOU, listSoundIndex);
                                    foreach (var index in listSoundIndex)
                                    {
                                        ChartRenderer.DrawSymbol(context, index, 0.0f, pivot, Header.UpdateType);
                                    }
                                }
                            }
                            else
                            {
                                if (Com.ComSize > 0)
                                {
                                    byte comIndex = 0;
                                    if (S57ChartOption.PaperSimple == true) comIndex = 1;

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
                                                    if (IsCS == false && Points.Shape.EdgeArr[m].Mask == 1) continue;

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
                                }
                            }
                        }

                        if (S57ChartOption.LowAccuracy == true && DangerAttr.Accuracy == true)
                        {
                            var nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC01");
                            ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                        }

                        Dispose();
                    }
                    break;
            }

            // Pivot점이 다른 차트에 의해 가려지지 않으면
            if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, pivot) == false)
            {
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

        public void DrawWrecksCS(GraphicsContext context, ref ST_OVER over, bool IsCS = false)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            float X = Header.Pivot.X / ScaleFactor;
            float Y = Header.Pivot.Y / ScaleFactor;
            Float2D pivot = context.Transform.WGS84ToScreen((double)X, (double)Y);

            switch (Header.PRIM)
            {
                case 1:     // Point
                    {
                        if (IsDanger == true)
                        {
                            if (S57ChartOption.ShallowWaterDangers == true || Header.ViewingGroup == 1)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex("ISODGR01");
                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                            }
                        }
                        else
                        {
                            if (DangerAttr.VALSOU != float.MaxValue)
                            {
                                int nSymIndex = -1;
                                if (DangerAttr.VALSOU <= S57ChartSafetyValue.SafetyDepth) nSymIndex = ChartRenderer.GetSymbolIndex("DANGER01");
                                else nSymIndex = ChartRenderer.GetSymbolIndex("DANGER02");

                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);

                                if ((char)DangerAttr.Soundg1 != 'o')
                                {
                                    string strSound = null;
                                    if (S57ChartSafetyValue.SafetyDepth >= DangerAttr.DEPTH_VALUE) strSound = "SOUNDS";
                                    else strSound = "SOUNDG";

                                    string strAdd = null;
                                    char[] chArr = new char[] { (char)DangerAttr.Soundg1, (char)DangerAttr.Soundg2 };
                                    strAdd = new string(chArr);
                                    int nLen = strAdd.Length / 2;
                                    for (int m = 0; m < nLen; m++)
                                    {
                                        var strName = strSound + strAdd.Substring(m * 2, 2);
                                        if (strName.Length == 8)
                                        {
                                            nSymIndex = ChartRenderer.GetSymbolIndex(strName);
                                            ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                                        }
                                    }
                                }

                                if (DangerAttr.Sound == true)
                                {
                                    List<int> listSoundIndex = new List<int>();
                                    ChartRenderer.CS_SNDFRM04(DangerAttr.VALSOU, listSoundIndex);
                                    foreach (var index in listSoundIndex)
                                    {
                                        ChartRenderer.DrawSymbol(context, index, 0.0f, pivot, Header.UpdateType);
                                    }
                                }
                            }
                            else
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
                                            ChartRenderer.DrawSymbol(context, Com.ArrSY[k].Index, Com.ArrSY[k].Angle, pivot, Header.UpdateType);
                                        }
                                    }
                                }
                            }

                            if (S57ChartOption.LowAccuracy == true && DangerAttr.Accuracy == true)
                            {
                                var nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC01");
                                ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                            }
                        }
                    }
                    break;

                case 3:     // Area
                    {
                        CreateSKPath_Shape(context.Transform, true);

                        if (DangerAttr.VALSOU != float.MaxValue)
                        {
                            if (IsDanger == true)
                            {
                                if (S57ChartOption.ShallowWaterDangers == true || Header.ViewingGroup == 1)
                                {
                                    var nSymIndex = ChartRenderer.GetSymbolIndex("ISODGR01");
                                    if (AreaSKPath.PathsIntersect != null)
                                    {
                                        pivot = AreaSKPath.Pivot;
                                    }

                                    ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                                }
                            }
                            else
                            {
                                if ((char)DangerAttr.Soundg1 != 'o')
                                {
                                    string strSound = null;
                                    if (S57ChartSafetyValue.SafetyDepth >= DangerAttr.DEPTH_VALUE) strSound = "SOUNDS";
                                    else strSound = "SOUNDG";

                                    string strAdd = null;
                                    char[] chArr = new char[] { (char)DangerAttr.Soundg1, (char)DangerAttr.Soundg2 };
                                    strAdd = new string(chArr);
                                    int nLen = strAdd.Length / 2;
                                    for (int m = 0; m < nLen; m++)
                                    {
                                        var strName = strSound + strAdd.Substring(m * 2, 2);
                                        if (strName.Length == 8)
                                        {
                                            var nSymIndex = ChartRenderer.GetSymbolIndex(strName);
                                            ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                                        }
                                    }
                                }

                                if (DangerAttr.Sound == true)
                                {
                                    List<int> listSoundIndex = new List<int>();
                                    ChartRenderer.CS_SNDFRM04(DangerAttr.VALSOU, listSoundIndex);
                                    foreach (var index in listSoundIndex)
                                    {
                                        ChartRenderer.DrawSymbol(context, index, 0.0f, pivot, Header.UpdateType);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Com.ArrAC != null && AreaSKPath.MainSkPath != null)
                            {
                                for (int k = 0; k < Com.ArrAC.Length; k++)
                                {
                                    var rgb = WeatherColor.GetColor(Com.ArrAC[k].ColorIndex);
                                    var alpha = ChartRenderer.TransparentToByte(Com.ArrAC[k].Trans);
                                    AreaSKPath.DrawSKPathArea(context, new SKColor(rgb.R, rgb.G, rgb.B, alpha), IsQuerySelect);
                                }
                            }

                            if (IsDanger == true)
                            {
                                if (S57ChartOption.ShallowWaterDangers == true || Header.ViewingGroup == 1)
                                {
                                    var nSymIndex = ChartRenderer.GetSymbolIndex("ISODGR01");
                                    if (AreaSKPath.PathsIntersect != null)
                                    {
                                        pivot = AreaSKPath.Pivot;
                                    }

                                    ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                                }
                            }
                        }

                        int nEdge = Points.Shape.EdgeArr.Length;
                        for (int k = 0; k < nEdge; k++)
                        {
                            if (IsCS == false && Points.Shape.EdgeArr[k].Mask == 1) continue;

                            var chartPath = CreateSKPath_EdgeGeo(context.Transform, k);
                            if (chartPath == null) continue;

                            if (Points.Shape.EdgeArr[k].Quapos == false)
                            {
                                var nLineIndex = ChartRenderer.GetLineIndex("LOWACC41");
                                if (chartPath.MainSkPath != null)
                                {
                                    ChartRenderer.DrawSymbolizedLine(context, nLineIndex, chartPath.PathLine);
                                }
                            }
                            else
                            {
                                if (IsDanger == true)
                                {
                                    var rgb = WeatherColor.GetColor("CHBLK");
                                    AreaSKPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 3.0f, 3.0f }, 0.0f, 2.0f, IsQuerySelect);
                                }
                                else
                                {
                                    if (DangerAttr.VALSOU != float.MaxValue)
                                    {
                                        var rgb = WeatherColor.GetColor("CHBLK");

                                        if (DangerAttr.VALSOU <= S57ChartSafetyValue.SafetyDepth)
                                        {
                                            chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 3.0f, 3.0f }, 0.0f, 2.0f, IsQuerySelect);
                                        }
                                        else
                                        {
                                            chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), new[] { 10.0f, 10.0f }, 0.0f, 2.0f, IsQuerySelect);
                                        }
                                    }
                                    else
                                    {
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

                                                chartPath.DrawSKPathLine(context, new SKColor(rgb.R, rgb.G, rgb.B), intervals, 0.0f, Com.ArrLS[0].Width, IsQuerySelect);
                                            }
                                        }
                                    }
                                }
                            }

                            chartPath.Dispose();
                        }

                        if (S57ChartOption.LowAccuracy == true && DangerAttr.Accuracy == true)
                        {
                            var nSymIndex = ChartRenderer.GetSymbolIndex("LOWACC01");
                            ChartRenderer.DrawSymbol(context, nSymIndex, 0.0f, pivot, Header.UpdateType);
                        }

                        Dispose();
                    }
                    break;
            }

            // Pivot점이 다른 차트에 의해 가려지지 않으면
            if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, pivot) == false)
            {
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

        public void Draw_CS(int layerIndex, GraphicsContext context, ref ST_OVER over)
        {
            if (IsDraw(Header.UpdateType) == false) return;

            if (IsOBSTRN == true)
            {
                if (IsDanger == true || (IsDanger == false && layerIndex == 8) || 
                    (IsDanger == false && ChartRenderer.FindViewingGroup(Header.ViewingGroup) == true))
                {
                    if(ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == true)
                    {
                        if(DangerAttr.DEPTH_VALUE == float.MaxValue)
                        {
                            DrawNormal(context, ref over, true);
                        }
                        else
                        {
                            DrawObstrnCS(context, ref over, true);
                        }
                    }
                }
            }
            else
            {
                if(layerIndex == 8 || IsChangePriority == false)
                {
                    if (IsDanger == true || (IsDanger == false && layerIndex == 8) ||
                        (IsDanger == false && ChartRenderer.FindViewingGroup(Header.ViewingGroup) == true))
                    {
                        if (ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == true)
                        {
                            if (DangerAttr.DEPTH_VALUE == float.MaxValue)
                            {
                                DrawNormal(context, ref over, true);
                            }
                            else
                            {
                                DrawWrecksCS(context, ref over, true);
                            }
                        }
                    }
                }
            }
        }

        private void ChangeMask_Obstrn()
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
