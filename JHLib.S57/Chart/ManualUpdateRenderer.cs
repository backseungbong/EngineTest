using JHLib.Graphics;
using JHLib.S57ManualUpdate;
using JHLib.S57ManualUpdate.ManualUpdate;
using JHLib.Util.Geometry;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.Chart
{
    public partial class S57ChartRenderer
    {
        public void DrawManualUpdate(GraphicsContext context, List<MUmain> listMU)
        {
            if (_layer == null) return;
            if (listMU.Count <= 0) return;

            bool isDateDependent = false;
            foreach (var mu in listMU)
            {
                if (CheckDateDependent(mu.StartDate, mu.EndDate, ref isDateDependent, S57ChartOption.DateDependentType == 0 ? true : false, S57ChartOption.StartDate, S57ChartOption.EndDate) == false) continue;
                if (mu.GeoType != EnumGeoType.Official && mu.IsDelete && mu.IsReview == false) continue;

                switch (mu.GeoType)
                {
                    case EnumGeoType.Point:
                    case EnumGeoType.Official:
                        DrawMUpoint(context, mu);
                        break;
                    case EnumGeoType.Line:
                        DrawMUline(context, mu);
                        break;
                    case EnumGeoType.Area:
                        DrawMUarea(context, mu, isDateDependent);
                        break;
                }
            }
        }

        public void DrawMUpoint(GraphicsContext context, MUmain mu)
        {
            var pointObj = mu.PointObj as MUpoint;
            if (pointObj.Pivot.X == float.MaxValue || pointObj.Pivot.Y == float.MaxValue) return;

            var pivot = context.Transform.WGS84ToScreen(pointObj.Pivot);
            if (context.Transform.PointContainScreen(pivot) == false) return;

            foreach(var sym in pointObj.SymbolInfos)
            {
                var angle = 0.0f;
                if (sym.Angle != 0.0) angle = sym.Angle + (float)context.Transform.Rotation;
                else angle = sym.Angle;

                var strength = 0f;
                if (sym.Strength != 0.0) strength = sym.Strength + (float)context.Transform.Rotation;
                else strength = sym.Strength;

                // Actutal Tidal
                if(sym.SymbolIndex == -100)
                {
                    DrawMUtidal(context, false, angle, strength, pivot);
                }
                // Predicted Tidal
                if (sym.SymbolIndex == -200)
                {
                    DrawMUtidal(context, true, angle, strength, pivot);
                }
                else DrawMUsymbol(context, sym.SymbolIndex, angle, pivot);
            }

            // Official Object를 Delete만 한 것에 대해서 Delete일때만 심볼을 보여주려고 이렇게 처리함
            if (pointObj.SymbolInfos.Count <= 0 && mu.IsDelete == false) return;

            // Manual Update Symbol 그리기
            if(mu.DisplayName.Contains("Tidal") == false)  DrawManualUpdateSymbol(context, pivot, mu.IsDelete, mu.IsReview);
        }

        public void DrawMUline(GraphicsContext context, MUmain mu)
        {
            if (mu.LineObj.Points.Count < 2) return;

            var lineObj = mu.LineObj as MUline;

            var count = lineObj.Points.Count;
            var pathLine = new Float2D[count];
            for (int i = 0; i < count; i++) pathLine[i] = context.Transform.WGS84ToScreen(lineObj.Points[i]);

            switch (lineObj.LineType)
            {
                case EnumLineType.Symolized:
                    {
                        DrawSymbolizedLine(context, lineObj.Index, pathLine);
                    }
                    break;
                case EnumLineType.Plain:
                    {
                        var rgb = WeatherColor.GetColor(lineObj.Index);
                        float[] intervals = null;
                        if (lineObj.PlainLineType == EnumPlainLineType.Dash)
                        {
                            intervals = new[] { 10.0f, 10.0f };
                        }
                        else if (lineObj.PlainLineType == EnumPlainLineType.Dot)
                        {
                            intervals = new[] { 3.0f, 3.0f };
                        }

                        context.SetStrokeWidth(lineObj.Width);
                        context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                        context.SetStrokeDash(intervals, 0.0f);
                        context.DrawPath(pathLine);
                        context.SetStrokeDash(null);
                    }
                    break;
            }

            DrawManualUpdateSymbolizedLine(context, pathLine, mu.IsDelete, mu.IsReview);
        }


        public void DrawMUarea(GraphicsContext context, MUmain mu, bool isDateDependent = false)
        {
            if (mu.AreaObj.Points.Count < 3) return;

            var areaObj = mu.AreaObj as MUarea;

            var count = areaObj.Points.Count;
            var pathLine = new Float2D[count + 1];
            for (int i = 0; i < count; i++) pathLine[i] = context.Transform.WGS84ToScreen(areaObj.Points[i]);
            pathLine[count] = pathLine[0];

            Float2D pivot = GeometryHelper.GetCentroid(pathLine);

            switch (areaObj.AreaType)
            {
                case EnumAreaType.Pattern:
                    {
                        DrawManualUpdatePattern(context, (byte)areaObj.Index, pathLine);
                    }
                    break;
                case EnumAreaType.FillColor:
                    {
                        var rgb = WeatherColor.GetColor(areaObj.Index);
                        context.SetFillColor(new SKColor(rgb.R, rgb.G, rgb.B, areaObj.Arpha));
                        context.FillPath(pathLine);
                    }
                    break;
            }

            switch (areaObj.LineType)
            {
                case EnumLineType.Symolized:
                    {
                        DrawSymbolizedLine(context, areaObj.LineIndex, pathLine);
                    }
                    break;
                case EnumLineType.Plain:
                    {
                        var rgb = WeatherColor.GetColor(areaObj.LineIndex);
                        float[] intervals = null;
                        if (areaObj.PlainLineType == EnumPlainLineType.Dash)
                        {
                            intervals = new[] { 10.0f, 10.0f };
                        }
                        else if (areaObj.PlainLineType == EnumPlainLineType.Dot)
                        {
                            intervals = new[] { 3.0f, 3.0f };
                        }

                        context.SetStrokeWidth(areaObj.LineWidth);
                        context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                        context.SetStrokeDash(intervals, 0.0f);
                        context.DrawPath(pathLine, true);
                        context.SetStrokeDash(null);
                    }
                    break;
            }

            DrawManualUpdateSymbolizedLine(context, pathLine, mu.IsDelete, mu.IsReview);

            // Center Symbol이 존재하면
            if(mu.PointObj.SymbolInfos.Count > 0)
            {
                foreach(var sym in mu.PointObj.SymbolInfos)
                {
                    if(CheckAreaInCenterSymbol(pathLine, sym.SymbolIndex, pivot) == true)
                    {
                        DrawMUsymbol(context, sym.SymbolIndex, 0.0f, pivot);
                    }
                }
            }

            // Manual Update Symbol Pivot에 그리기
            if(mu.IsReview)
            {
                if (mu.IsDelete) DrawMUsymbol(context, 528, 0.0f, pivot);
                else DrawMUsymbol(context, 529, 0.0f, pivot);
            }
            else
            {
                if (mu.IsDelete) DrawMUsymbol(context, 72, 0.0f, pivot);
                else DrawMUsymbol(context, 73, 0.0f, pivot);
            }

            // DateDependent 심볼 그리기
            if (S57ChartOption.HighlightDateDependent == true && isDateDependent == true)
            {
                DrawSymbol(context, 530, 0.0f, pivot, 0);
            }
        }

        // 교차 영역안에 Center심볼이 들어가는지 확인하는 함수
        public bool CheckAreaInCenterSymbol(Float2D[] pathsIn, int nSymbolIndex, Float2D pivot)
        {
            if (nSymbolIndex < 0) return false;
            if (pathsIn == null) return false;

            // S-52에 Center Symbol은 화면과 교차되는 영역안에 심볼의 Bounding의 중심이 포함하지 않으면
            // 그리지 않도록 규정하고 있음
            bool bRtn = false;
            float fW, fH, pX, pY;
            fW = fH = pX = pY = 0.0f;
            if (GetSymbolBoundAndPivot(nSymbolIndex, ref fW, ref fH, ref pX, ref pY) == true)
            {
                Float2D temp = new Float2D(0, 0);
                temp = pivot;
                if (pX < 0.0f)
                {
                    temp.X = (-pX + (fW / 2.0f)) * ScaleFactor;
                    temp.X += pivot.X;
                }

                if (pY < 0.0f)
                {
                    temp.Y = (-pY + (fH / 2.0f)) * ScaleFactor;
                    temp.Y += pivot.Y;
                }

                bRtn = GeometryHelper.PointInPolygonWinding(temp, pathsIn);
            }

            return bRtn;
        }

    }
}
