using JHLib.Graphics;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.Chart
{
    public class DrawSymbol
    {
        // 심볼 Index를 저장
        public int SymbolIndex = -1;
        // Geometry를 저장할 리스트
        public List<DrawSKPath> ListSKPath= new();
        // Symbol Box의 크기를 저장할 변수 
        public float BoxWidth = 0.0f;
        public float BoxHeight = 0.0f;
        // Symbol의 Pivot 위치를 저장할 변수
        public float PivotX = 0.0f;
        public float PivotY = 0.0f;

        // 모든 심볼의 Geo를 생성하는 함수 
        public bool CreateSymbol(DaiSymbol daiSymbol)
        {
            int nW = daiSymbol.sycl - daiSymbol.sbxc;
            if (nW > daiSymbol.syhl) BoxWidth = nW;
            else BoxWidth = daiSymbol.syhl;
            PivotX = nW;

            nW = daiSymbol.syrw - daiSymbol.sbxr;
            if (nW > daiSymbol.syvl) BoxHeight = nW;
            else BoxHeight = daiSymbol.syvl;
            PivotY = nW;

            string colorName = "";
            float transparency = 1.0F;
            float width = 0.0F;
            List<SKPoint> listPoint = new();

            DrawSKPath InnerPath = null;
            bool bPM1 = false;
            var lastPT = new SKPoint();
            foreach (var sym in daiSymbol.listSymbolVCT)
            {
                // 실직적으로 Geometry가 생성되는 명령어 
                // CI - 원 생성 
                // PD - 패스 지오메트리 생성 
                // SC - 콜 심볼
                if(sym.type == "SP")
                {
                    colorName = daiSymbol.dicScrf[sym.colorType];
                    listPoint.Clear();
                }
                // ST = 선의 스타일을 설정( 0 = 실선)
                else if (sym.type == "ST")
                {
                    if (sym.value1 == 0) transparency = 1.0f;
                    else if (sym.value1 == 1) transparency = 0.75f;
                    else if (sym.value1 == 2) transparency = 0.5f;
                    else if (sym.value1 == 3) transparency = 0.25f;

                    if(listPoint.Count > 0)
                    {
                        // 마지막 데이터에 Transparency값을 변경함
                        if(ListSKPath.Count > 0) ListSKPath.Last().Trans = transparency;
                    }
                }
                // SW = 선의 굵기를 설정(1단위 = 0.3mm)
                else if (sym.type == "SW")
                {
                    width = sym.value1;

                    if (listPoint.Count > 0)
                    {
                        // 마지막 데이터에 Width를 변경함
                        if (ListSKPath.Count > 0) ListSKPath.Last().LineWidth = width;
                    }
                }
                // PU = 펜을 떼고 중심점 좌표로 이동한다.
                else if (sym.type == "PU")
                {
                    listPoint.Clear();

                    var point = new SKPoint();
                    point.X = sym.value1 - daiSymbol.sycl;
                    point.Y = sym.value2 - daiSymbol.syrw;
                    lastPT = point;
                    listPoint.Add(point);

                    if(InnerPath != null)
                    {
                        if(ListSKPath.Count > 0)
                        {
                            ListSKPath.Last().CreateSKPath();
                            InnerPath.IsFill = true;
                            InnerPath.CreateSKPath();
                            ListSKPath.Last().CreateInnerSKPath(InnerPath, SKPathOp.Difference);
                        }

                        InnerPath.Dispose();
                        InnerPath = null;
                    }

                    if(bPM1 == false)
                    {
                        var skPath = new DrawSKPath();
                        skPath.AddPoint(point);
                        skPath.IsDraw = true;
                        skPath.LineColorName = colorName;
                        skPath.LineWidth = width;
                        ListSKPath.Add(skPath);
                    }
                    else
                    {
                        InnerPath = new DrawSKPath();
                        InnerPath.AddPoint(point);
                    }
                }
                else if (sym.type == "PD")
                {
                    listPoint.Clear();

                    var point = new SKPoint();
                    point.X = sym.value1 - daiSymbol.sycl;
                    point.Y = sym.value2 - daiSymbol.syrw;

                    if(listPoint.Count > 0)
                    {
                        if(bPM1 == false)
                        {
                            var skPath = new DrawSKPath();
                            skPath.AddPoint(lastPT);
                            skPath.IsDraw = true;
                            skPath.LineColorName = colorName;
                            skPath.LineWidth = width;
                            ListSKPath.Add(skPath);
                        }
                        else
                        {
                            InnerPath = new DrawSKPath();
                            InnerPath.AddPoint(lastPT);
                        }
                    }

                    lastPT = point;

                    if(bPM1 == false)
                    {
                        if (ListSKPath.Count > 0) ListSKPath.Last().AddPoint(point);
                    }
                    else
                    {
                        if(InnerPath != null)
                        {
                            InnerPath.AddPoint(point);
                        }
                    }
                }
                else if (sym.type == "CI")
                {
                    if(bPM1 == false)
                    {
                        if (ListSKPath.Count > 0) ListSKPath.Last().Radius = sym.value1;
                    }
                    else
                    {
                        if(InnerPath != null)
                        {
                            InnerPath.Radius = sym.value1;
                        }
                    }
                }
                // PM0 = 기존에 저장된 폴리곤 좌표를 모두 지우고, 새로운 폴리곤 정의를 시작
                // PM1 = 버퍼를 지우지 않고, 현재 폴리곤 내에 새로운 하위 경로를 추가함(구멍뚫린 도형을 그릴때 사용)
                // PM2 = 현재 정의 중인 폴리곤 경로를 닫고 폴리곤 모드를 완전히 종료
                else if (sym.type == "PM")
                {
                    switch(sym.value1)
                    {
                        case 1:
                            bPM1 = true;
                            if (ListSKPath.Count > 0) ListSKPath.Last().CreateSKPath();
                            break;
                        case 2:
                            bPM1 = false;
                            if(InnerPath != null)
                            {
                                if(ListSKPath.Count > 0)
                                {
                                    ListSKPath.Last().CreateSKPath();
                                    InnerPath.IsFill = true;
                                    InnerPath.CreateSKPath();
                                    ListSKPath.Last().CreateInnerSKPath(InnerPath, SKPathOp.Difference);
                                }

                                InnerPath.Dispose();
                                InnerPath = null;
                            }

                            if (ListSKPath.Count > 0)
                            {
                                ListSKPath.Last().IsFill = true;
                                ListSKPath.Last().IsDraw = false;
                                ListSKPath.Last().FillColorName = colorName;
                                ListSKPath.Last().Trans = transparency;
                            }
                            break;
                    }
                }
                else if (sym.type == "FP")
                {
                    // 마지막 Geo에 bFill = true, FillColor, Transparency 적용
                    if(ListSKPath.Count > 0)
                    {
                        ListSKPath.Last().IsFill = true;
                        ListSKPath.Last().IsDraw = false;
                        ListSKPath.Last().FillColorName = colorName;
                        ListSKPath.Last().Trans = transparency;
                    }
                }
            }

            foreach(var geo in ListSKPath)
            {
                geo.CreateSKPath();
            }

            return true;
        }

        // 심볼을 그려주는 함수 
        public void Draw(GraphicsContext context, byte weatherIndex = 255)
        {
            if (weatherIndex == 255) weatherIndex = WeatherColor.WeatherIndex;

            foreach (var path in ListSKPath)
            {
                path.Draw(context, weatherIndex);
            }
        }

        public void Draw(SKCanvas canvas, byte weatherIndex = 255)
        {
			if (weatherIndex == 255) weatherIndex = WeatherColor.WeatherIndex;

			foreach (var path in ListSKPath)
            {
                path.Draw(canvas, weatherIndex);
            }
        }

        // Geometry 어레이를 삭제하는 함수 
        public void Dispose()
        {
            foreach(var path in ListSKPath)
            {
                path.Dispose();
            }

            ListSKPath.Clear();
        }
    }
}
