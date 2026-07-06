using JHLib.Graphics;
using JHLib.Graphics.SkiaExtention;
using JHLib.S57.Chart;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class LIGHTS
    {
        public LIGHTS(S57ChartRenderer chartRenderer)
        {
			ChartRenderer = chartRenderer;
		}

        public const float ScaleFactor = 10000000.0f;

        // Main정보를 저장할 인스턴스 
        public S57ChartRenderer ChartRenderer = null;

        // 현재 그려질 차트의 Usage를 가지고 있을 변수 
        public byte Usage = 255;

        public ST_LIGHTS_HEADER Header;
        public ST_LIGHTS_ATTR LightsAttr;
        public string LITDSN = null;

        public bool IsQuerySelect = false;

        // Draw함수 
        public void Draw(GraphicsContext context, ref ST_OVER over)
        {
            if (IsDraw(Header.UpdateType) == false) return;
            if (ChartRenderer.FindViewingGroup(5) == false || ChartRenderer.CheckScaleMin(Header.ScaleMin, context.Transform) == false) return;

            float X = Header.Pivot.X / ScaleFactor;
            float Y = Header.Pivot.Y / ScaleFactor;
            var pivot = context.Transform.WGS84ToScreen((double)X, (double)Y);

            // Pivot을 덮을 다음 Usage의 차트가 존재하면 그리지 않는다.
            if (ChartRenderer.CheckOverUsageChartInPivot(context.Transform, Usage, pivot) == true) return;

            if (LightsAttr.CATLIT_8_11 == true)
            {
                var symIndex = ChartRenderer.GetSymbolIndex("LIGHTS82");
                ChartRenderer.DrawSymbol(context, symIndex, 0.0f, pivot, Header.UpdateType);
            }
            else if(LightsAttr.CATLIT_9 == true)
            {
                var symIndex = ChartRenderer.GetSymbolIndex("LIGHTS81");
                ChartRenderer.DrawSymbol(context, symIndex, 0.0f, pivot, Header.UpdateType);
            }
            else
            {
                if(LightsAttr.CATLIT_1_16 == true && LightsAttr.ORIENT != float.MaxValue)
                {
                    var p2 = GetRealDistancePoint(context.Transform, pivot, LightsAttr.ORIENT, LightsAttr.VALNMR);

                    var rgb = WeatherColor.GetColor("CHBLK");
                    context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                    context.SetStrokeWidth(1.0f);

                    context.SetStrokeDash(new[] { 10.0f, 10.0f }, 0.0f);

                    context.DrawLine(pivot, p2);

                    context.SetStrokeDash(null);
                }

                if (LightsAttr.ALL_ROUND_LIGHT == true)
                {
                    if (LightsAttr.Radius26 == true)
                    {
                        string name = "CHMGD";
                        if (LightsAttr.COLOUR_ATTR == 1) name = "LITRD";
                        else if (LightsAttr.COLOUR_ATTR == 2) name = "LITGN";
                        else if (LightsAttr.COLOUR_ATTR == 3) name = "LITYW";

                        var rad = (float)(26.0f / context.Transform.FactorPixelToMM);

                        var rgb = WeatherColor.GetColor("OUTLW");
                        context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                        context.SetStrokeWidth(4.0f);
                        context.DrawEllipse(rad, pivot);

                        rgb = WeatherColor.GetColor(name);
                        context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                        context.SetStrokeWidth(2.0f);
                        context.DrawEllipse(rad, pivot);
                    }
                    else
                    {
                        string name = "LITDEF11";
                        if (LightsAttr.COLOUR_ATTR == 1) name = "LIGHTS11";
                        else if (LightsAttr.COLOUR_ATTR == 2) name = "LIGHTS12";
                        else if (LightsAttr.COLOUR_ATTR == 3) name = "LIGHTS13";

                        if (LightsAttr.CATLIT_1_16 == true)
                        {
                            if (LightsAttr.ORIENT != float.MaxValue)
                            {
                                // SY(SELECT,ORIENT+/180°); TE(‘% 03.0lfdeg’,’ORIENT’...)
                                var symIndex = ChartRenderer.GetSymbolIndex(name);
                                ChartRenderer.DrawSymbol(context, symIndex, LightsAttr.ORIENT + 180.0f, pivot, Header.UpdateType);

                                if (ChartRenderer.FindTextGroup(23) == true)
                                {
                                    float offset = 32f;
                                    var rgb = WeatherColor.GetColor(2);
                                    context.SetTextColor(new SKColor(rgb.R, rgb.G, rgb.B));
                                    var orient = $"{LightsAttr.ORIENT.ToString("000")} deg";
                                    context.DrawText(orient, 16, pivot.X + offset, pivot.Y + offset, SKTextHorizental.Right, SKTextVertical.Up);
                                }
                            }
                            else
                            {
                                var symIndex = ChartRenderer.GetSymbolIndex("QUESMRK1");
                                ChartRenderer.DrawSymbol(context, symIndex, 0.0f, pivot, Header.UpdateType);
                            }
                        }
                        else
                        {
                            var symIndex = ChartRenderer.GetSymbolIndex(name);
                            if (LightsAttr.FLARE_AT_45_DEGREES == true)
                            {
                                ChartRenderer.DrawSymbol(context, symIndex, 45.0f, pivot, Header.UpdateType);
                            }
                            else
                            {
                                ChartRenderer.DrawSymbol(context, symIndex, 135.0f, pivot, Header.UpdateType);
                            }
                        }
                    }

                    if (ChartRenderer.FindTextGroup(23) == true)
                    {
                        var rgb = WeatherColor.GetColor(2);
                        context.SetTextColor(new SKColor(rgb.R, rgb.G, rgb.B));

                        float fOffsetX = 2f * 16f;
                        if (LightsAttr.FLARE_AT_45_DEGREES == true)
                        {
                            float fOffsetY = -1f * 16f;
                            context.DrawText(LITDSN, 16, pivot.X + fOffsetX, pivot.Y + fOffsetY, SKTextHorizental.Right, SKTextVertical.Down);
                        }
                        else
                        {
                            context.DrawText(LITDSN, 16, pivot.X + fOffsetX, pivot.Y, SKTextHorizental.Right, SKTextVertical.Center);
                        }
                    }
                }
                else
                {
                    var rgb = WeatherColor.GetColor("CHBLK");
                    context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                    context.SetStrokeWidth(1.0f);
                    context.SetStrokeDash(new[] { 10.0f, 10.0f }, 0.0f);

                    if (S57ChartOption.FullLightLine == true)
                    {
                        var p2 = GetRealDistancePoint(context.Transform, pivot, LightsAttr.SECTR1, LightsAttr.VALNMR);
                        context.DrawLine(pivot, p2);

                        p2 = GetRealDistancePoint(context.Transform, pivot, LightsAttr.SECTR2, LightsAttr.VALNMR);
                        context.DrawLine(pivot, p2);
                    }
                    else
                    {
                        var p2 = GetViewDistancePoint(context.Transform, pivot, LightsAttr.SECTR1, 25.0f);
                        context.DrawLine(pivot, p2);

                        p2 = GetViewDistancePoint(context.Transform, pivot, LightsAttr.SECTR2, 25.0f);
                        context.DrawLine(pivot, p2);
                    }

                    context.SetStrokeDash(null);

                    float fRad = 20.0f;
                    if (LightsAttr.EXTENDED_ARC_RADIUS == true) fRad = 25.0f;

                    //var ptStart = GetViewDistancePoint(pivot, LightsAttr.fSECTR1, fRad);
                    //var ptEnd = GetViewDistancePoint(pivot, LightsAttr.fSECTR2, fRad);

                    var fRadius = (float)(fRad / context.Transform.FactorPixelToMM);

                    float fStartDeg = LightsAttr.SECTR1 + 180.0f + (float)context.Transform.Rotation;

                    float fSweepDeg = 0.0f;
                    if(LightsAttr.SECTR2 < LightsAttr.SECTR1)
                    {
                        fSweepDeg = 360.0f - LightsAttr.SECTR2;
                        fSweepDeg -= LightsAttr.SECTR1;
                    }
                    else
                    {
                        fSweepDeg = LightsAttr.SECTR2 - LightsAttr.SECTR1;
                    }


                    if(LightsAttr.LITVIS_3_7_8 == true)
                    {
                        rgb = WeatherColor.GetColor("CHBLK");
                        context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                        context.SetStrokeWidth(1.0f);
                        context.SetStrokeDash(new[] { 10.0f, 10.0f }, 0.0f);
                        context.DrawArc(fRadius, fStartDeg, fSweepDeg, pivot.X, pivot.Y);
                        context.SetStrokeDash(null);
                    }
                    else
                    {
                        string name = "CHMGD";
                        if (LightsAttr.COLOUR_ATTR == 1) name = "LITRD";
                        else if (LightsAttr.COLOUR_ATTR == 2) name = "LITGN";
                        else if (LightsAttr.COLOUR_ATTR == 3) name = "LITYW";

                        rgb = WeatherColor.GetColor("OUTLW");
                        context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                        context.SetStrokeWidth(4.0f);
                        context.DrawArc(fRadius, fStartDeg, fSweepDeg, pivot.X, pivot.Y);

                        rgb = WeatherColor.GetColor(name);
                        context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
                        context.SetStrokeWidth(2.0f);
                        context.DrawArc(fRadius, fStartDeg, fSweepDeg, pivot.X, pivot.Y);
                    }
                }
            }

            // Query 선택 표시
            if (IsQuerySelect == true) DrawSelectQueryPoint(context, pivot);
        }

        Float2D GetRealDistancePoint(Transform projection, Float2D pivot, float orient, float valnmr)
        {
            Float2D ptRtn = new Float2D(0,0);

            float fAngle = 0.0f;
            if (orient != 0.0) fAngle = orient + (float)projection.Rotation;

            float fRadius = (float)(valnmr * 1852000.0f / projection.Scale / projection.FactorPixelToMM);

            var rad = -(fAngle + 90.0f) / (180.0f / Math.PI);
            var cosAngle = Math.Cos(rad);
            var sinAngle = Math.Sin(rad);

            ptRtn.X = (float)(pivot.X + (fRadius * cosAngle));
            ptRtn.Y = (float)(pivot.Y - (fRadius * sinAngle));

            return ptRtn;
        }

        Float2D GetViewDistancePoint(Transform projection, Float2D pivot, float orient, float fMM)
        {
            Float2D ptRtn = new Float2D(0, 0);

            float fRadius = (float)(fMM / projection.FactorPixelToMM);

            float fAngle = 0.0f;
            if (orient != 0.0) fAngle = orient + (float)projection.Rotation;

            var rad = -(fAngle + 90.0f) / (180.0f / Math.PI);
            var cosAngle = Math.Cos(rad);
            var sinAngle = Math.Sin(rad);

            ptRtn.X = (float)(pivot.X + (fRadius * cosAngle));
            ptRtn.Y = (float)(pivot.Y - (fRadius * sinAngle));

            return ptRtn;
        }

        // Query함수
        public bool Query(Transform projection, Float2D point)
        {
            if (ChartRenderer.IsQuery == false || S57ChartQueryOptions.QueryPointOn == false || Header.UpdateType == 2 || Header.UpdateType == 12) return false;
            if (ChartRenderer.CheckScaleMin(Header.ScaleMin, projection) == false) return false;

            float X = Header.Pivot.X / ScaleFactor;
            float Y = Header.Pivot.Y / ScaleFactor;
            Float2D pivot = projection.WGS84ToScreen((double)X, (double)Y);

            var rect = new SKRect(pivot.X - 10, pivot.Y - 10, pivot.X + 10, pivot.Y + 10);

            IsQuerySelect = rect.Contains(point.X, point.Y);

            return IsQuerySelect;
        }

        // Query정보를 Reset하는 함수 
        public void ResetQuery()
        {
            IsQuerySelect = false;
        }

        public void DrawSelectQueryPoint(GraphicsContext context, Float2D pivot)
        {
            if (IsQuerySelect == false) return;
            if (pivot.X == float.MaxValue || pivot.Y == float.MaxValue) return;

            var rgb = WeatherColor.GetColor("CURSR");
            context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
            context.SetStrokeWidth(2);

            var offset = 20f;
            var space = 8f;
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y - offset), new Float2D(pivot.X - offset + space, pivot.Y - offset));
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y - offset), new Float2D(pivot.X - offset, pivot.Y - offset + space));

            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y - offset), new Float2D(pivot.X + offset - space, pivot.Y - offset));
            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y - offset), new Float2D(pivot.X + offset, pivot.Y - offset + space));

            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y + offset), new Float2D(pivot.X - offset + space, pivot.Y + offset));
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y + offset), new Float2D(pivot.X - offset, pivot.Y + offset - space));

            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y + offset), new Float2D(pivot.X + offset - space, pivot.Y + offset));
            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y + offset), new Float2D(pivot.X + offset, pivot.Y + offset - space));
        }

        public bool MUquery(Transform projection, Float2D point, ref Float2D pivot)
        {
            if (Header.UpdateType == 2 || Header.UpdateType == 12) return false;
            if (ChartRenderer.CheckScaleMin(Header.ScaleMin, projection) == false) return false;

            pivot.X = Header.Pivot.X / ScaleFactor;
            pivot.Y = Header.Pivot.Y / ScaleFactor;
            var sxy = projection.WGS84ToScreen(pivot);
            var rect = new SKRect(sxy.X - 10, sxy.Y - 10, sxy.X + 10, sxy.Y + 10);

            return rect.Contains(point.X, point.Y);
        }

        public void GetMUqueryResult(List<MUsymbolInfo> listSY)
        {
            if (LightsAttr.CATLIT_8_11 == true)
            {
                var symIndex = ChartRenderer.GetSymbolIndex("LIGHTS82");
                listSY.Add(new MUsymbolInfo(symIndex));
            }
            else if (LightsAttr.CATLIT_9 == true)
            {
                var symIndex = ChartRenderer.GetSymbolIndex("LIGHTS81");
                listSY.Add(new MUsymbolInfo(symIndex));
            }
            else
            {
                if (LightsAttr.ALL_ROUND_LIGHT == true && LightsAttr.Radius26 == false)
                {

                    {
                        string name = "LITDEF11";
                        if (LightsAttr.COLOUR_ATTR == 1) name = "LIGHTS11";
                        else if (LightsAttr.COLOUR_ATTR == 2) name = "LIGHTS12";
                        else if (LightsAttr.COLOUR_ATTR == 3) name = "LIGHTS13";

                        if (LightsAttr.CATLIT_1_16 == true)
                        {
                            if (LightsAttr.ORIENT != float.MaxValue)
                            {
                                var symIndex = ChartRenderer.GetSymbolIndex(name);
                                listSY.Add(new MUsymbolInfo(symIndex, LightsAttr.ORIENT + 180.0f));
                            }
                            else
                            {
                                var symIndex = ChartRenderer.GetSymbolIndex("QUESMRK1");
                                listSY.Add(new MUsymbolInfo(symIndex));
                            }
                        }
                        else
                        {
                            var symIndex = ChartRenderer.GetSymbolIndex(name);
                            if (LightsAttr.FLARE_AT_45_DEGREES == true)
                            {
                                listSY.Add(new MUsymbolInfo(symIndex, 45.0f));
                            }
                            else
                            {
                                listSY.Add(new MUsymbolInfo(symIndex, 135.0f));
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {

        }

        // Object를 그릴 조건이 되는지 확인하는 함수
        public bool IsDraw(byte updateType)
        {
            bool drawFlag = true;

            // Delete를 2->12로 만든 이유는 업데이트를 .001, .002 순차적으로 진행할 때 .001에서 삭제한 Object도 2 이고, .002에서 삭제한 Object도 2라서 
            // .001에서 삭제된 Object는 아예 Update Review를 하더라도 그리지 않게 해야 해서 마지막 Update의 경우에만 11,12,13처럼 10을 붙여서 보내주기로 함
            // Delete일때는 Review가 켜져있을 때만 그린다.
            if (updateType == 12) drawFlag = S57ChartOption.UpdateReview;
            else if (updateType == 2) drawFlag = false;

            return drawFlag;
        }
    }
}
