using JHLib.ChartManager.Catalogue;
using JHLib.ChartManager.Record;
using JHLib.Graphics;
using JHLib.Graphics.SkiaExtention;
using JHLib.Util.Helper;
using JHLib.Util.Struct;
using JHLib.Util.Time;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.Chart
{
    public partial class S57ChartRenderer
    {
        public void ReadyToDrawing(GraphicsContext context)
        {
            // Manual Update 삭제
            if(_clearManualUpdate)
            {
                ManualUpdate.ClearAllManualUpdate();
                ClearManualUpdate();
                _clearManualUpdate = false;
            }

            LoadCharts(context.Transform);

            // VDR로 검색 차트 정보 전달 
            if(_usedVDR) OnS57ChartInfoToVDR?.Invoke(DicSendVdr);

            // 기존 인증시에 있었던 내용
            // Chart를 설치시 Sequence에 맞지 않거나, Edition Num의 잘못으로 Update가 실패한 경우에 
            // "Chart information not up to date"라고 표시하기 위한 Flag를 추가함
            OnS57PermanentIndication?.Invoke(OverscaleFactor, NonOfficialChart, NotUpToDate, OverlapChart);
        }

        public void Drawing(GraphicsContext context)
        {
            context.Clear();

            Query(context.Transform);
            ManualUpdateQuery(context.Transform);

            DrawCharts(context);
        }

        public void DrawCharts(GraphicsContext context)
        {
            lock (_chartLock)
            {
                if (CanvasUnder == null) CanvasUnder = new(context.Transform.RenderWidth, context.Transform.RenderHeight);
                else
                {
                    // 화면 사이즈가 변경되었을 경우 캔버스 새로 생성
                    if (CanvasUnder.Bitmap.Width != context.Transform.RenderWidth || CanvasUnder.Bitmap.Height != context.Transform.RenderHeight)
                    {
                        CanvasUnder.Dispose();
                        CanvasUnder = new(context.Transform.RenderWidth, context.Transform.RenderHeight);
                    }
                }

                if (CanvasRadar == null) CanvasRadar = new(context.Transform.RenderWidth, context.Transform.RenderHeight);
                else
                {
                    // 화면 사이즈가 변경되었을 경우 캔버스 새로 생성
                    if (CanvasRadar.Bitmap.Width != context.Transform.RenderWidth || CanvasRadar.Bitmap.Height != context.Transform.RenderHeight)
                    {
                        CanvasRadar.Dispose();
                        CanvasRadar = new(context.Transform.RenderWidth, context.Transform.RenderHeight);
                    }
                }

                if (CanvasOver == null) CanvasOver = new(context.Transform.RenderWidth, context.Transform.RenderHeight);
                else
                {
                    // 화면 사이즈가 변경되었을 경우 캔버스 새로 생성
                    if (CanvasOver.Bitmap.Width != context.Transform.RenderWidth || CanvasOver.Bitmap.Height != context.Transform.RenderHeight)
                    {
                        CanvasOver.Dispose();
                        CanvasOver = new(context.Transform.RenderWidth, context.Transform.RenderHeight);
                    }
                }

                CanvasUnder.Clear();
                CanvasRadar.Clear();
                CanvasOver.Clear();
                context.TargetCanvas = CanvasUnder;

                // World Map을 사용하지 않으면 NoData Pattern을 그린다.
                DrawNoDataPattern(context, S57ChartOption.UsedWorldMap);

                int nSize = ListChartInfo.Count;
                for (int i = nSize - 1; i >= 0; i--)
                {
                    var name = ListChartInfo[i].ChartName;
                    if (DicChart.TryGetValue(name, out var chart) == true)
                    {
                        if (CheckIndexMap(name) == false)
                        {
                            if(S57ChartOption.RadarOverlayOn)
                            {
                                context.TargetCanvas = CanvasOver;
                                ClearChartArea(context, ListChartInfo[i].PathsChart);
                            }

                            chart.Over.Agency = ListChartInfo[i].Agency;
                            chart.ChartInfo = ListChartInfo[i];
                            chart.DrawChart(context, CanvasUnder, S57ChartOption.RadarOverlayOn ? CanvasOver : null);
                            if (context.Transform.IsMultiTransform == true)
                            {
                                context.Transform.SetTransform2();
                                chart.DrawChart(context, CanvasUnder, S57ChartOption.RadarOverlayOn ? CanvasOver : null);
                                context.Transform.SetTransform1();
                            }
                        }
                        else
                        {
                            context.TargetCanvas = CanvasUnder;
                            chart.DrawWorldMap(context);
                            if (context.Transform.IsMultiTransform == true)
                            {
                                context.Transform.SetTransform2();
                                chart.DrawWorldMap(context);
                                context.Transform.SetTransform1();
                            }
                        }
                    }
                }

                // Over 정보 그리기
                for (int i = nSize - 1; i >= 0; i--)
                {
                    var name = ListChartInfo[i].ChartName;
                    if (DicChart.TryGetValue(name, out var chart) == true)
                    {
                        if (CheckIndexMap(name) == true) continue;
                        chart.DrawOver(context, CanvasUnder, S57ChartOption.RadarOverlayOn ? CanvasOver : null);
                        if (context.Transform.IsMultiTransform == true)
                        {
                            context.Transform.SetTransform2();
                            chart.DrawOver(context, CanvasUnder, S57ChartOption.RadarOverlayOn ? CanvasOver : null);
                            context.Transform.SetTransform1();
                        }

                    }
                }

                // 차트 바운더리를 그린다.
                if(S57ChartOption.RadarOverlayOn) context.TargetCanvas = CanvasOver;
                DrawChartBoundary(context);

                context.TargetCanvas = context.LayerCanvas;
                context.DrawBitmap(CanvasUnder.Bitmap);
                if (S57ChartOption.RadarOverlayOn)
                {
                    DrawRadar(context);
                    context.DrawBitmap(CanvasOver.Bitmap);
                }
                if (context.Transform.IsMultiTransform == true)
                {
                    context.Transform.SetTransform2();
                    context.DrawBitmap(CanvasUnder.Bitmap);
                    if (S57ChartOption.RadarOverlayOn)
                    {
                        DrawRadar(context);
                        context.DrawBitmap(CanvasOver.Bitmap);
                    }
                    context.Transform.SetTransform1();
                }
            }
        }

        private void DrawRadar(GraphicsContext context)
        {
            if (CanvasRadar == null) return;

            context.TargetCanvas = CanvasRadar;

            var screen = context.Transform.ScreenBound;
            var pt = new SKPoint[4];
            pt[0] = new SKPoint(screen.X1, screen.Y1);
            pt[1] = new SKPoint(screen.X2, screen.Y1);
            pt[2] = new SKPoint(screen.X2, screen.Y2);
            pt[3] = new SKPoint(screen.X1, screen.Y2);

            var path = new SKPath();
            path.AddPoly(pt);

            context.SetFillColor(SKColors.YellowGreen);
            context.FillPath(path);

            context.TargetCanvas = context.LayerCanvas;

            context.DrawBitmap(CanvasRadar.Bitmap);
        }

        // Chart Boundary를 그리는 함수
        public void DrawChartBoundary(GraphicsContext context)
        {
            if (S57ChartOption.ChartCatalogue == false) return;

            context.SetStrokeWidth(2);

            lock (_chartLock)
            {
                foreach(var chart in ChartCatalogue.catalogue)
                {
                    var c = chart.Value as ChartRecord;

                    var isWorldmap = c.name.Contains("WORLDMAP");
                    if (isWorldmap) continue;
                    if (c.IsChart1 && S57ChartOption.UsedChart1 == false) continue;

                    float fector = 10000000.0f;
                    var wgsRect = new FloatRect(
                        c.boundary.west / fector, c.boundary.south / fector,
                        c.boundary.east / fector, c.boundary.north / fector);
                    if (context.Transform.RectIntersectWGS84(wgsRect) == false) continue;

                    // 색상 설정
                    if (chart.Value.usage == 1) { context.SetStrokeColor(SKColors.Brown); context.SetTextColor(SKColors.Brown); }
                    else if (chart.Value.usage == 2) { context.SetStrokeColor(SKColors.DarkBlue); context.SetTextColor(SKColors.DarkBlue); }
                    else if (chart.Value.usage == 3) { context.SetStrokeColor(SKColors.Green); context.SetTextColor(SKColors.Green); }
                    else if (chart.Value.usage == 4) { context.SetStrokeColor(SKColors.Magenta); context.SetTextColor(SKColors.Magenta); }
                    else if (chart.Value.usage == 5) { context.SetStrokeColor(SKColors.Red); context.SetTextColor(SKColors.Red); }
                    else if (chart.Value.usage == 6) { context.SetStrokeColor(SKColors.Cyan); context.SetTextColor(SKColors.Cyan); }

                    context.Transform.WGS84ToScreen(wgsRect, out var spath);
                    context.DrawPath(spath.AsSpan(), true);

                    context.SetMatrix(spath.P3.X, spath.P3.Y, context.Transform.Rotation, 1, spath.P3.X, spath.P3.Y);
                    string strText = $"{c.name} [Edition : {c.baseVersion.EDTN}.{c.updateVersion} / IssuDate : {c.issueDate}]";
                    context.DrawText(strText, 15, spath.P3.X, spath.P3.Y, SKTextHorizental.Left, SKTextVertical.Up, SKTextFace.Bold);
                    context.ResetMatrix();
                }
            }
        }

        // 심볼을 그리는 함수
        public void DrawSymbol(GraphicsContext context, int symIndex, float angle, Float2D pivot, byte ruin = 0)
        {
            if (DaiPL4.dicSymbol.ContainsKey(symIndex) == false) return;

            if (DaiPL4.dicSymbol.TryGetValue(symIndex, out var symbol) == true && DicSymbol.ContainsKey(symbol.synm) == true)
            {
                context.SetMatrix(0, 0, angle, ScaleFactor, pivot.X, pivot.Y);
                DicSymbol[symbol.synm].Draw(context);
                context.ResetMatrix();

                // Update Symbol 그리기
                DrawUpateSymbol(context, ruin, pivot);
            }
        }

        // Update 심볼을 그리는 함수 
        public void DrawUpateSymbol(GraphicsContext context, byte ruin, Float2D pivot)
        {
            if (ruin == 0) return;
            if (S57ChartOption.UpdateReview == false) return;

            if (ruin == 11 || ruin == 13) DrawSymbol(context, 529, 0.0f, pivot);
            else if (ruin == 12) DrawSymbol(context, 528, 0.0f, pivot);
        }

        public void DrawMUsymbol(GraphicsContext context, int symIndex, float angle, Float2D pivot)
        {
            if(symIndex >= 0 && DaiPL4.dicSymbol.TryGetValue(symIndex, out var symbol) == true && DicSymbol.ContainsKey(symbol.synm) == true) 
            {
                context.SetMatrix(0, 0, angle, ScaleFactor, pivot.X, pivot.Y);
                DicSymbol[symbol.synm].Draw(context);
                context.ResetMatrix();
            }
        }

        public unsafe void DrawMUtidal(GraphicsContext context, bool predictedTidal, float angle, float strength, Float2D pivot)
        {
            context.SetMatrix(0, 0, angle, 1, pivot.X, pivot.Y);

            context.SetStrokeWidth(1);
            var rgb = WeatherColor.GetColor("CHBLK");
            context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
            if (predictedTidal) context.SetStrokeDash(new[] { 8.0f, 2.0f }, 0.0f);

            var mm1 = (int)(1 * context.Transform.FactorMMToPixel + 0.5f); 
            var mm2 = (int)(2 * context.Transform.FactorMMToPixel + 0.5f);
            var mm15 = (int)(15 * context.Transform.FactorMMToPixel + 0.5f) * -1;
            context.DrawLine(0, 0, 0, mm15);
            context.ClearStrokeDash();

            var buf = stackalloc float[6];
            buf[0] = -mm2;
            buf[1] = mm15 + mm2;
            buf[2] = 0;
            buf[3] = mm15;
            buf[4] = mm2;
            buf[5] = mm15 + mm2;
            context.DrawPath(ref *(Float2D*)buf, 3);

            buf[0] = -mm2;
            buf[1] = mm15 + mm2 + mm2;
            buf[2] = 0;
            buf[3] = mm15 + mm2;
            buf[4] = mm2;
            buf[5] = mm15 + mm2 + mm2;
            context.DrawPath(ref *(Float2D*)buf, 3);

            buf[0] = -mm2;
            buf[1] = mm15 + mm2 + mm2 + mm2;
            buf[2] = 0;
            buf[3] = mm15 + mm2 + mm2;
            buf[4] = mm2;
            buf[5] = mm15 + mm2 + mm2 + mm2;
            context.DrawPath(ref *(Float2D*)buf, 3);

            context.ResetMatrix();

            context.SetMatrix(0, 0, 0, 1, pivot.X, pivot.Y);

            float offset = 20f;
            var pos = GetPositionFromBearing(0, 0, angle - 90, offset);
            var text = $"{strength.ToString("0.0")}kn";
            context.DrawText(text, 15, (float)pos.X, (float)pos.Y, SKTextHorizental.Left, SKTextVertical.Up, SKTextFace.Bold);

            pos = GetPositionFromBearing(0, 0, angle + 90, offset);
            text = AppTime.Utc.ToString("HHmm");
            context.DrawText(text, 15, (float)pos.X, (float)pos.Y, SKTextHorizental.Right, SKTextVertical.Up, SKTextFace.Bold);

            context.ResetMatrix();
        }

        public static (double X, double Y) GetPositionFromBearing(double startX, double startY, double bearingInDegrees, double offsetDistance)
        {
            // 1. Degree를 Radian으로 변환
            double bearingInRadians = bearingInDegrees * (Math.PI / 180.0);

            // 2. 방위각 기준 X, Y 좌표 계산 (X는 Sin, Y는 Cos 사용 및 Y축 반전)
            double newX = startX + (offsetDistance * Math.Sin(bearingInRadians));
            double newY = startY - (offsetDistance * Math.Cos(bearingInRadians));

            return (newX, newY);
        }

        // Manual Update 심볼을 그리는 함수 
        public void DrawManualUpdateSymbol(GraphicsContext context, Float2D pivot, bool isDelete = false, bool isReview = false)
        {
            if (isReview)
            {
                if(isDelete) DrawSymbol(context, 528, 0.0f, pivot);
                else DrawSymbol(context, 529, 0.0f, pivot);
            }
            else
            {
                if (isDelete) DrawSymbol(context, 72, 0.0f, pivot);
                else DrawSymbol(context, 73, 0.0f, pivot);
            }
        }

        // 심볼 라인을 그리는 함수
        public void DrawSymbolizedLine(GraphicsContext context, int lineIndex, Float2D[] pathLine, bool reverse = false)
        {
            if (DaiPL4.dicLine.TryGetValue(lineIndex, out var daiLine) == false) return;

            var name = daiLine.linm;
            if (DaiPL4.dicSymbolLine.TryGetValue(name, out var listInfo) == false) return;
            if (DicLine.TryGetValue(name, out var line) == false) return;

            // Line안에 존재하는 Shape의 순번을 확인할 변수
            int shapeIndex = 0;

            // Shape의 개수 받기
            int shapeCount = listInfo.Count;

            // 라인의 컬러명을 받아놓음
            ColorRGB rgb = null;
            if (line.ListSL.Count > 0 && line.ListSL[0].ListSKPath.Count > 0)
            {
                rgb = WeatherColor.GetColor(line.ListSL[0].ListSKPath[0].LineColorName);
            }
            else return;

            Float2D[] pathLineResult = pathLine.ToArray();
            if (reverse) Array.Reverse(pathLineResult);

            var start = new Float2D(0, 0);
            bool isNewStart = false;
            for (int i = 0; i < pathLineResult.Length - 1; i++)
            {
                var pt1 = new Float2D(pathLineResult[i].X, pathLineResult[i].Y);
                var pt2 = new Float2D(pathLineResult[i + 1].X, pathLineResult[i + 1].Y);

                // 조건 확인
                int changeIndex = i;
                bool isCondition = false;
                // 두 점 사이의 거리를 계산
                var distance = PTtoDistance(pt1, pt2);

                // 두 점 사이의 거리가 라인을 그리기에 부적합하면 
                // 다음 점으로 이동하기
                var range = listInfo[shapeIndex] * ScaleFactor;
                if (distance < range)
                {
                    int index = i + 2;
                    for (int m = index; m <= pathLineResult.Length - 1; m++)
                    {
                        var ptTemp = new Float2D(pathLineResult[m].X, pathLineResult[m].Y);
                        // 두 점 사이의 거리를 계산
                        var fDist = PTtoDistance(pt1, ptTemp);
                        // 조건이 만족하는 경우
                        if (fDist >= range)
                        {
                            // pt2에서 지금점까지도 거리가 나오는지 확인한다.
                            var dist2 = PTtoDistance(pt2, ptTemp);
                            if (dist2 >= range)
                            {
                                // 선을 그어줌
                                if (rgb != null)
                                {
                                    context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                                    context.SetStrokeWidth(1);
                                    context.DrawLine(pt1, pt2);
                                }

                                pt1 = pt2;
                                pt2 = ptTemp;
                                distance = dist2;
                                changeIndex = m - 1;
                            }
                            else
                            {
                                distance = fDist;
                                changeIndex = m - 1;
                            }

                            isCondition = true;
                            break;
                        }
                        else pt2 = ptTemp;
                    }
                }
                else isCondition = true;

                // 조건이 만족하지 않으면 직선으로 그리고 다음으로 넘어감.
                if (isCondition == false)
                {
                    // 선을 그어줌
                    if (rgb != null)
                    {
                        context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                        context.SetStrokeWidth(1);
                        context.DrawLine(pt1, pt2);
                    }

                    isNewStart = true;
                    continue;
                }

                // 최초 이거나 새로 그려진 상태이면 시작점을 등록한다.
                if (i == 0 || isNewStart == true)
                {
                    start = pt1;
                    isNewStart = false;
                }

                // 첫점에서 두번째점을 바라보는 각도 계산
                var calcAngle = PTtoAngle(pt1, pt2);
                var rotation = MathHelper.Vec2Deg(pt2.X - pt1.X, pt2.Y - pt1.Y);

                // 시작점을 중심으로 각도 틀어 그리기
                context.SetMatrix(0, 0, rotation, ScaleFactor, start.X, start.Y);

                float fTotalDist = 0.0f;
                bool bLineDrawEnd = false;
                while (bLineDrawEnd == false)
                {
                    if (shapeIndex != 0)
                    {
                        float fRange = 0.0f;
                        for (int k = shapeIndex - 1; k >= 0; k--)
                        {
                            fRange += listInfo[k] * ScaleFactor;
                        }

                        start = CalcBrgDistToPoint(pt1, calcAngle, -fRange);
                        context.SetMatrix(0, 0, rotation, ScaleFactor, start.X, start.Y);
                    }

                    if(name == "SCLBDY51")
                    {
                        var lineCount = listInfo.Count;
                        for (int k = 0; k< lineCount; k++)
                        {
                            var fShapeW = listInfo[k] * ScaleFactor;
                            if(k==0) fTotalDist += fShapeW;

                            // Edge의 길이보다 현재 길이가 
                            if (fTotalDist > distance)
                            {
                                // Shape의 Index를 저장
                                shapeIndex = 0;
                                // 라인 그리기 종료
                                bLineDrawEnd = true;
                                break;
                            }

                            // 그려졌으므로 다시 처음부터 그리기 위해서 
                            isNewStart = true;
                            // 라인 그리기
                            line.Draw(context, k, rotation, ScaleFactor, start.X, start.Y);
                            // 라인을 그릴때 메트릭스를 초기화 함으로 다시 먹여줌
                            context.SetMatrix(0, 0, rotation, ScaleFactor, start.X, start.Y);
                        }
                    }
                    else
                    {
                        // Shape만큼 돌면서 그려준다.
                        for (int k = shapeIndex; k < shapeCount; k++)
                        {
                            var fShapeW = listInfo[k] * ScaleFactor;
                            fTotalDist += fShapeW;

                            // Edge의 길이보다 현재 길이가 
                            if (fTotalDist > distance)
                            {
                                // Shape의 Index를 저장
                                shapeIndex = k;
                                // 라인 그리기 종료
                                bLineDrawEnd = true;
                                break;
                            }

                            // Shape Index 초기화 (Distance가 길어서 다시 그리기 위해서)
                            if (k >= shapeCount - 1)
                            {
                                shapeIndex = 0;
                            }

                            // 그려졌으므로 다시 처음부터 그리기 위해서 
                            isNewStart = true;

                            // 라인 그리기
                            line.Draw(context, k, rotation, ScaleFactor, start.X, start.Y);

                            // 라인을 그릴때 메트릭스를 초기화 함으로 다시 먹여줌
                            context.SetMatrix(0, 0, rotation, ScaleFactor, start.X, start.Y);
                        }

                    }

                    // 시작점 다시 계산
                    start = CalcBrgDistToPoint(pt1, calcAngle, fTotalDist);

                    // 메트릭스 위치 변경
                    context.SetMatrix(0, 0, rotation, ScaleFactor, start.X, start.Y);
                }

                if (changeIndex != i) i = changeIndex;

                context.ResetMatrix();
            }
        }

        // Update 심볼라인을 그리는 함수
        public void DrawUpdateSymbolizedLine(GraphicsContext context, byte ruin, Float2D[] pathLine, bool reverse = false)
        {
            if (ruin == 0) return;
            if (S57ChartOption.UpdateReview == false) return;

            if (ruin == 11 || ruin == 13) DrawSymbolizedLine(context, 52, pathLine, reverse);
            else if (ruin == 12) DrawSymbolizedLine(context, 53, pathLine, reverse);
        }

        // Manual Update 심볼라인을 그리는 함수 
        public void DrawManualUpdateSymbolizedLine(GraphicsContext context, Float2D[] pathLine, bool isDelete = false, bool isReview = false)
        {
            if(isReview)
            {
                if (isDelete) DrawSymbolizedLine(context, 53, pathLine);
                else DrawSymbolizedLine(context, 52, pathLine);
            }
            else
            {
                if (isDelete) DrawSymbolizedLine(context, 6, pathLine);
                else DrawSymbolizedLine(context, 7, pathLine);
            }
        }

        // 패턴을 그리는 함수
        public void DrawPattern(GraphicsContext context, byte patterindex, SKPath path)
        {
            if (path == null) return;
            if (DaiPL4.dicPattern.TryGetValue(patterindex, out var daiPattern) == false) return;
            if (DicPattern.TryGetValue(daiPattern.panm, out var pattern) == false) return;

            pattern.DrawPatternBitmap(context, path);
        }

        public void DrawManualUpdatePattern(GraphicsContext context, byte patterindex, Float2D[] paths)
        {
            if (paths == null) return;
            if (DaiPL4.dicPattern.TryGetValue(patterindex, out var daiPattern) == false) return;
            if (DicPattern.TryGetValue(daiPattern.panm, out var pattern) == false) return;

            pattern.DrawPatternBitmap(context, paths);
        }

        // NoData 패턴을 그리는 함수
        public void DrawNoDataPattern(GraphicsContext context, bool usedWorldMap = false)
        {
            var screen = context.Transform.ScreenBound;
            var pt = new SKPoint[4];
            pt[0] = new SKPoint(screen.X1, screen.Y1);
            pt[1] = new SKPoint(screen.X2, screen.Y1);
            pt[2] = new SKPoint(screen.X2, screen.Y2);
            pt[3] = new SKPoint(screen.X1, screen.Y2);

            var path = new SKPath();
            path.AddPoly(pt);

            var rgb = WeatherColor.GetColor("DEPDW");
            context.SetFillColor(new SKColor(rgb.R, rgb.G, rgb.B));
            context.FillPath(path);

            if (usedWorldMap == false) DrawPattern(context, 16, path);
        }
    }
}
