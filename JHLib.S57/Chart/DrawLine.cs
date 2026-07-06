using JHLib.Graphics;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.Chart
{
    public class SymbolLine
    {
        public List<DrawSKPath> ListSKPath = new();
        public List<DrawSymbol> ListSymbol = new();
        public List<SKPoint> ListSymbolPoint = new();

        public bool IsNull()
        {
            bool bPath = ListSKPath.Count > 0 ? true : false;
            bool bSym = ListSymbol.Count > 0 ? true : false;

            return bPath | bSym;
        }
    }

    public class DrawLine
    {
        // 현재 선택된 라인 번호를 저장할 변수
        public int LineIndex = -1;

        // SKPath를 저장할 리스트
        public List<DrawSKPath> ListSKPath = new();

        // DAI Symbol정보를 저장할 어레이
        public List<DrawSymbol> ListSymbol = new();
        // Symbol을 찍어줄 위치점을 가지고 있을 어레이
        public List<SKPoint> ListSymbolPoint = new();

        // Line Box의 크기를 저장할 변수 
        private float _boxWidth = 0.0f;
        private float _boxHeight = 0.0f;
        // Line Bitmap을 만들때 Offset값을 저장할 변수
        private float _offsetX = 0.0f;
        private float _offsetY = 0.0f;

        public List<SymbolLine> ListSL = new();

        // Line을 그릴 Bitmap Brush
        public SKBitmap SkLineBitmap = null;
        public SKImage SkLineImage = null;

        //public float ScaleFactor = 0.0333f;
        // 모든 Line의 Geo를 생성하는 함수 
        public bool CreateLine(DaiLine dailine, Dictionary<string, DrawSymbol> dicSym)
        {
            ListSL.Clear();

            ListSymbol.Clear();
            ListSymbolPoint.Clear();

            _offsetX = _offsetY = 0.0f;

            int nW = dailine.licl - dailine.lbxc;
            if (nW > dailine.lihl) _boxWidth = nW;
            else _boxWidth = dailine.lihl;
            _offsetX = nW;

            int nH = dailine.lirw - dailine.lbxr;
            if (nH > dailine.livl) _boxHeight = nH;
            else _boxHeight = dailine.livl;
            _offsetY = nH;

            string colorName = "";
            float transparency = 1.0F;
            float width = 0.0F;
            List<SKPoint> listPoint = new();

            DrawSKPath InnerPath = null;
            bool bPM1 = false;
            var lastPT = new SKPoint();
            foreach (var line in dailine.listLineVCT)
            {
                if (line.type == "SS")
                {
                    ListSL.Add(new SymbolLine());
                    continue;
                }

                // 실직적으로 Geometry가 생성되는 명령어 
                // CI - 원 생성 
                // PD - 패스 지오메트리 생성 
                // SC - 콜 심볼
                if (line.type == "SP")
                {
                    colorName = dailine.dicLcrf[line.colorType];
                    listPoint.Clear();
                    if(ListSL.Count <= 0) ListSL.Add(new SymbolLine());
                }
                else if (line.type == "ST")
                {
                    if (line.value1 == 0) transparency = 1.0f;
                    else if (line.value1 == 1) transparency = 0.75f;
                    else if (line.value1 == 2) transparency = 0.5f;
                    else if (line.value1 == 3) transparency = 0.25f;

                    if (listPoint.Count > 0)
                    {
                        // 마지막 데이터에 Transparency값을 변경함
                        if(ListSL.Count >0)  ListSL.Last().ListSKPath.Last().Trans = transparency;
                    }
                }
                else if (line.type == "SW")
                {
                    width = line.value1;

                    if (listPoint.Count > 0)
                    {
                        // 마지막 데이터에 Width를 변경함
                        if (ListSL.Count > 0) ListSL.Last().ListSKPath.Last().LineWidth = width;
                    }
                }
                else if (line.type == "PU")
                {
                    listPoint.Clear();

                    var point = new SKPoint();
                    point.X = line.value1 - dailine.licl;
                    point.Y = line.value2 - dailine.lirw;
                    lastPT = point;
                    listPoint.Add(point);

                    if (InnerPath != null)
                    {
                        if (ListSL.Count > 0 && ListSL.Last().ListSKPath.Count > 0)
                        {
                            ListSL.Last().ListSKPath.Last().CreateSKPath();
                            InnerPath.IsFill = true;
                            InnerPath.CreateSKPath();
                            ListSL.Last().ListSKPath.Last().CreateInnerSKPath(InnerPath, SKPathOp.Difference);
                        }

                        InnerPath.Dispose();
                        InnerPath = null;
                    }

                    if (bPM1 == false)
                    {
                        var skPath = new DrawSKPath();
                        skPath.AddPoint(point);
                        skPath.IsDraw = true;
                        skPath.LineColorName = colorName;
                        skPath.LineWidth = width;
                        if (ListSL.Count > 0) ListSL.Last().ListSKPath.Add(skPath);
                    }
                    else
                    {
                        InnerPath = new DrawSKPath();
                        InnerPath.AddPoint(point);
                    }
                }
                else if (line.type == "PD")
                {
                    listPoint.Clear();

                    var point = new SKPoint();
                    point.X = line.value1 - dailine.licl;
                    point.Y = line.value2 - dailine.lirw;

                    if (listPoint.Count > 0)
                    {
                        if (bPM1 == false)
                        {
                            var skPath = new DrawSKPath();
                            skPath.AddPoint(lastPT);
                            skPath.IsDraw = true;
                            skPath.LineColorName = colorName;
                            skPath.LineWidth = width;
                            if (ListSL.Count > 0) ListSL.Last().ListSKPath.Add(skPath);
                        }
                        else
                        {
                            InnerPath = new DrawSKPath();
                            InnerPath.AddPoint(lastPT);
                        }
                    }

                    lastPT = point;

                    if (bPM1 == false)
                    {
                        if (ListSL.Count > 0) ListSL.Last().ListSKPath.Last().AddPoint(point);
                    }
                    else
                    {
                        if (InnerPath != null)
                        {
                            InnerPath.AddPoint(point);
                        }
                    }
                }
                else if (line.type == "CI")
                {
                    if (bPM1 == false)
                    {
                        if (ListSL.Count > 0) ListSL.Last().ListSKPath.Last().Radius = line.value1;
                    }
                    else
                    {
                        if (InnerPath != null)
                        {
                            InnerPath.Radius = line.value1;
                        }
                    }
                }
                else if (line.type == "PM")
                {
                    switch (line.value1)
                    {
                        case 1:
                            bPM1 = true;
                            if (ListSL.Count > 0) ListSL.Last().ListSKPath.Last().CreateSKPath();
                            break;
                        case 2:
                            bPM1 = false;
                            if (InnerPath != null)
                            {
                                if (ListSL.Count > 0 && ListSKPath.Count > 0)
                                {
                                    ListSL.Last().ListSKPath.Last().CreateSKPath();
                                    InnerPath.IsFill = true;
                                    InnerPath.CreateSKPath();
                                    ListSL.Last().ListSKPath.Last().CreateInnerSKPath(InnerPath, SKPathOp.Difference);
                                }

                                InnerPath.Dispose();
                                InnerPath = null;
                            }

                            if (ListSL.Count > 0)
                            {
                                ListSL.Last().ListSKPath.Last().IsFill = true;
                                ListSL.Last().ListSKPath.Last().IsDraw = false;
                                ListSL.Last().ListSKPath.Last().FillColorName = colorName;
                                ListSL.Last().ListSKPath.Last().Trans = transparency;
                            }
                            break;
                    }
                }
                else if (line.type == "FP")
                {
                    // 마지막 Geo에 bFill = true, FillColor, Transparency 적용
                    if (ListSL.Count > 0 && ListSL.Last().ListSKPath.Count > 0)
                    {
                        ListSL.Last().ListSKPath.Last().IsFill = true;
                        ListSL.Last().ListSKPath.Last().IsDraw = false;
                        ListSL.Last().ListSKPath.Last().FillColorName = colorName;
                        ListSL.Last().ListSKPath.Last().Trans = transparency;
                    }
                }
                else if (line.type == "SC")
                {
                    // 추가할 심볼 이름, 로테이션(0 = 직립, 1 = 펜의 이동방향으로, 2 = 이동각도 - 90도로)
                    string strSymName = line.colorType;
                    if (dicSym.ContainsKey(strSymName) == true)
                    {
                        // 심볼 저장
                        if (ListSL.Count > 0) ListSL.Last().ListSymbol.Add(dicSym[strSymName]);

                        // 심볼 위치점 저장
                        if (listPoint.Count > 0)
                        {
                            if (ListSL.Count > 0) ListSL.Last().ListSymbolPoint.Add(listPoint.Last());
                            listPoint.Clear();
                        }

                        // PU에서 만들어 놓은 Geometry 삭제
                        if (ListSL.Count > 0 && ListSL.Last().ListSKPath.Count > 0)
                        {
                            ListSL.Last().ListSKPath.Last().Dispose();
                            ListSL.Last().ListSKPath.RemoveAt(ListSL.Last().ListSKPath.Count - 1);
                        }
                    }
                }
            }

            foreach (var SL in ListSL)
            {
                if (SL.IsNull() == false) continue;

                foreach (var path in SL.ListSKPath)
                {
                    path.CreateSKPath();
                }
            }

            return true;
        }

        public void Draw(GraphicsContext context, int shapeIndex = 0, double rotation = 0, double scale = 1, double drawX = 0, double drawY = 0)
        {
            var SL = ListSL[shapeIndex];
            if (SL.IsNull() == false) return;

            foreach (var path in SL.ListSKPath)
            {
                path.Draw(context);
            }

            int index = 0;
            foreach (var sym in SL.ListSymbol)
            {
                context.SetMatrix(-SL.ListSymbolPoint[index].X, -SL.ListSymbolPoint[index].Y, rotation, scale, drawX, drawY);
                sym.Draw(context);
                index++;
            }

            context.ResetMatrix();
        }


        // Line을 그려주는 함수 
        public void Draw(GraphicsContext context, float fOffsetX, float fOffsetY, float fScaleFactor, byte weatherIndex = 255)
        {
            if (weatherIndex == 255) weatherIndex = WeatherColor.WeatherIndex;

            foreach (var path in ListSKPath)
            {
                path.Draw(context, weatherIndex);
            }

            // 심볼 그리기
            int index = 0;
            foreach (var sym in ListSymbol)
            {
                context.SetMatrix(fOffsetX, fOffsetY, 0.0f, fScaleFactor, ListSymbolPoint[index].X, ListSymbolPoint[index].Y);
                sym.Draw(context, weatherIndex);
                index++;
            }

            context.ResetMatrix();
        }

        public void SetMatrix(SKCanvas canvas, float offX, float offY, float fAngle, float fScale, float transX = 0f, float transY = 0f)
        {
            var mtx = SKMatrix.Identity;
            mtx = SKMatrix.Concat(mtx, SKMatrix.CreateTranslation(offX, offY));
            mtx = SKMatrix.Concat(mtx, SKMatrix.CreateRotation(fAngle));
            mtx = SKMatrix.Concat(mtx, SKMatrix.CreateScale(fScale, fScale));
            // transX , transY는 Matrix를 변경하여 모두 그린 후 화면상의 어디로 이동시켜서 찍을 것인지를 결정하는 변수임
            mtx = SKMatrix.Concat(mtx, SKMatrix.CreateTranslation(transX, transY));
            canvas.SetMatrix(mtx);
        }

        public void Draw(SKCanvas canvas, float fSX, float fSY, float fAngle, float fScaleFactor)
        {
            foreach (var SL in ListSL)
            {
                if (SL.IsNull() == false) continue;

                SetMatrix(canvas, 0, 0, fAngle, fScaleFactor, fSX, fSY);
                foreach (var path in SL.ListSKPath)
                {
                    path.Draw(canvas);
                }

                int index = 0;
                foreach (var sym in SL.ListSymbol)
                {
                    SetMatrix(canvas, 0, 0, fAngle, fScaleFactor, SL.ListSymbolPoint[index].X, SL.ListSymbolPoint[index].Y);
                    sym.Draw(canvas);
                    index++;
                }
            }

            canvas.ResetMatrix();
        }

        // Bitmap을 만드는 함수 
        public void CreateBitmap(float fScaleFactor)
        {
            if (SkLineBitmap != null)
            {
                SkLineImage.Dispose();
                SkLineBitmap.Dispose();
                SkLineBitmap = null;
            }

            // Line Type에 따라 영역계산을 달리한다.
            int nWidth, nHeight;
            nWidth = nHeight = 0;
            if (_offsetX < 0.0f) nWidth = (int)(((_boxWidth - _offsetX) * fScaleFactor) + 0.5f);
            else nWidth = (int)((_boxWidth * fScaleFactor) + 0.5f);
            if (nWidth == 0) nWidth = 2;

            if (_offsetY < 0.0f) nHeight = (int)(((_boxWidth - _offsetX) * fScaleFactor) + 0.5f);
            else nHeight = (int)((_boxHeight * fScaleFactor) + 0.5f);
            if (nHeight == 0) nHeight = 2;

            SkLineBitmap = new SKBitmap(new SKImageInfo(nWidth, nHeight, SKColorType.Rgba8888));
            using (SKCanvas canvas = new SKCanvas(SkLineBitmap))
            {
                float X = 0.0f;
                float Y = _offsetY * fScaleFactor;
                Draw(canvas, X, Y, 0.0f, fScaleFactor);

                SkLineImage = SKImage.FromBitmap(SkLineBitmap);
            }
        }

        // Line SKPath 어레이를 삭제하는 함수 
        public void Dispose()
        {
            if (SkLineBitmap != null)
            {
                SkLineImage.Dispose();
                SkLineBitmap.Dispose();
                SkLineBitmap = null;
            }

            foreach (var path in ListSKPath)
            {
                path.Dispose();
            }
            ListSKPath.Clear();

            foreach (var sym in ListSymbol)
            {
                sym.Dispose();
            }
            ListSymbol.Clear();
            ListSymbolPoint.Clear();
        }

    }
}
