using JHLib.Graphics;
using JHLib.Util.Struct;
using SkiaSharp;

namespace JHLib.S57.Chart
{
    public class DrawPattern
    {
        // Pattern Index를 저장할 변수 
        public byte Patterindex = 255;

        // SKPath를 저장할 리스트
        public List<DrawSKPath> ListSKPath = new();

        // Pattern Type을 저장할 변수 (0 = Staggered, 1 = Linear)
        public byte PatternType = 255;
        // Pattern Spacing Type을 저장할 변수 (0 = Constant, 1 = Scale dependent)
        public byte PatternSpacingType = 255;
        // Pattern min distance를 저장할 변수 
        public float MinDistance = 0.0f;
        // Pattern Box의 크기를 저장할 변수 
        private float _boxWidth = 0.0f;
        private float _boxHeight = 0.0f;
        // Pattern Bitmap을 만들때 Offset값을 저장할 변수
        private float _offsetX = 0.0f;
        private float _offsetY = 0.0f;

        // Pattern을 그릴 Bitmap Brush
        public SKBitmap SkPatternBitmap = null;
        public SKImage SkPatternImage = null;

        // 모든 심볼의 Geo를 생성하는 함수 
        public bool CreatePattern(DaiPattern pattern)
        {
            if (pattern.patp == "STG") PatternType = 0;
            else PatternType = 1;

            if (pattern.pasp == "CON") PatternSpacingType = 0;
            else PatternSpacingType = 1;

            MinDistance = pattern.pami;

            _offsetX = _offsetY = 0.0f;

            int nW = pattern.pacl - pattern.pbxc;
            if (nW > pattern.pahl) _boxWidth = nW;
            else _boxWidth = pattern.pahl;
            _offsetX = nW;

            nW = pattern.parw - pattern.pbxr;
            if (nW > pattern.pavl) _boxHeight = nW;
            else _boxHeight = pattern.pavl;
            _offsetY = nW;

            string colorName = null;
            float transparency = 1.0F;
            float width = 0.0F;
            List<SKPoint> listPoint = new();

            DrawSKPath InnerPath = null;
            bool bPM1 = false;
            var lastPT = new SKPoint();

            foreach (var pat in pattern.listPatternVCT)
            {
                // 실직적으로 Geometry가 생성되는 명령어 
                // CI - 원 생성 
                // PD - 패스 지오메트리 생성 
                // SC - 콜 심볼
                if (pat.type == "SP")
                {
                    colorName = pattern.dicPcrf[pat.colorType];
                    listPoint.Clear();
                }
                else if (pat.type == "ST")
                {
                    if (pat.value1 == 0) transparency = 1.0f;
                    else if (pat.value1 == 1) transparency = 0.75f;
                    else if (pat.value1 == 2) transparency = 0.5f;
                    else if (pat.value1 == 3) transparency = 0.25f;

                    if (listPoint.Count > 0)
                    {
                        // 마지막 데이터에 Transparency값을 변경함
                        if(ListSKPath.Count > 0) ListSKPath.Last().Trans = transparency;
                    }
                }
                else if (pat.type == "SW")
                {
                    width = pat.value1;

                    if (listPoint.Count > 0)
                    {
                        // 마지막 데이터에 Width를 변경함
                        if (ListSKPath.Count > 0) ListSKPath.Last().LineWidth = width;
                    }
                }
                else if (pat.type == "PU")
                {
                    listPoint.Clear();

                    var point = new SKPoint();
                    point.X = pat.value1 - pattern.pacl;
                    point.Y = pat.value2 - pattern.parw;
                    lastPT = point;
                    listPoint.Add(point);

                    if (InnerPath != null)
                    {
                        if (ListSKPath.Count > 0)
                        {
                            ListSKPath.Last().CreateSKPath();
                            InnerPath.IsFill = true;
                            InnerPath.CreateSKPath();
                            ListSKPath.Last().CreateInnerSKPath(InnerPath, SKPathOp.Difference);
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
                        ListSKPath.Add(skPath);
                    }
                    else
                    {
                        InnerPath = new DrawSKPath();
                        InnerPath.AddPoint(point);
                    }
                }
                else if (pat.type == "PD")
                {
                    listPoint.Clear();

                    var point = new SKPoint();
                    point.X = pat.value1 - pattern.pacl;
                    point.Y = pat.value2 - pattern.parw;

                    if (listPoint.Count > 0)
                    {
                        if (bPM1 == false)
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

                    if (bPM1 == false)
                    {
                        if (ListSKPath.Count > 0) ListSKPath.Last().AddPoint(point);
                    }
                    else
                    {
                        if (InnerPath != null)
                        {
                            InnerPath.AddPoint(point);
                        }
                    }
                }
                else if (pat.type == "CI")
                {
                    if (bPM1 == false)
                    {
                        if (ListSKPath.Count > 0) ListSKPath.Last().Radius = pat.value1;
                    }
                    else
                    {
                        if (InnerPath != null)
                        {
                            InnerPath.Radius = pat.value1;
                        }
                    }
                }
                else if (pat.type == "PM")
                {
                    switch (pat.value1)
                    {
                        case 1:
                            bPM1 = true;
                            if (ListSKPath.Count > 0) ListSKPath.Last().CreateSKPath();
                            break;
                        case 2:
                            bPM1 = false;
                            if (InnerPath != null)
                            {
                                if (ListSKPath.Count > 0)
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
                else if (pat.type == "FP")
                {
                    // 마지막 Geo에 bFill = true, FillColor, Transparency 적용
                    if (ListSKPath.Count > 0)
                    {
                        ListSKPath.Last().IsFill = true;
                        ListSKPath.Last().IsDraw = false;
                        ListSKPath.Last().FillColorName = colorName;
                        ListSKPath.Last().Trans = transparency;
                    }
                }
            }

            foreach (var path in ListSKPath)
            {
                path.CreateSKPath();
            }

            return true;
        }

        // Pattern을 그려주는 함수 
        public void Draw(GraphicsContext context, byte weatherIndex = 255)
        {
            foreach (var path in ListSKPath)
            {
                path.Draw(context, weatherIndex);
            }
        }

        // Pattern을 그려주는 함수 
        public void Draw(SKCanvas canvas, byte weatherIndex = 255)
        {
            foreach (var path in ListSKPath)
            {
                path.Draw(canvas, weatherIndex);
            }
        }

        // Pattern을 Bitmap으로 그리는 함수
        public void DrawPatternBitmap(GraphicsContext context, SKPath path)
        {
            if (path == null) return;
            if (SkPatternBitmap == null) return;

            context.SetFillShader(SKShader.CreateImage(SkPatternImage, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat));
            context.FillPath(path);
            context.SetFillShader(null);
        }

        public void DrawPatternBitmap(GraphicsContext context, Float2D[] paths)
        {
            if (paths == null) return;
            if (SkPatternBitmap == null) return;

            context.SetFillShader(SKShader.CreateImage(SkPatternImage, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat));
            context.FillPath(paths);
            context.SetFillShader(null);
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

        // Pattern Bitmap 를 만드는 함수 
        public void CreatePatternBitmap(float fScaleFector)
        {
            if (SkPatternBitmap != null)
            {
                SkPatternImage.Dispose();
                SkPatternBitmap.Dispose();
                SkPatternBitmap = null;
            }

            // Pattern Type에 따라 영역계산을 달리한다.
            int nWidth, nHeight;
            nWidth = nHeight = 0;
            if (PatternType == 0)               // Staggered
            {
                nWidth = (int)((_boxWidth + MinDistance) * fScaleFector);
                nHeight = (int)(((_boxHeight * 2) + (MinDistance * 2)) * fScaleFector);
            }
            else if (PatternType == 1)      // Linear
            {
                nWidth = (int)((_boxWidth + MinDistance) * fScaleFector);
                nHeight = (int)((_boxHeight + MinDistance) * fScaleFector);
            }

            SkPatternBitmap = new SKBitmap(new SKImageInfo(nWidth, nHeight, SKColorType.Rgba8888));
            using (SKCanvas canvas = new SKCanvas(SkPatternBitmap))
            {
                if (PatternType == 0)
                {
                    float X = ((MinDistance / 2.0f) + _offsetX) * fScaleFector;
                    float Y = _offsetY * fScaleFector;
                    SetMatrix(canvas, X, Y, 0.0f, fScaleFector);
                    Draw(canvas);

                    X = (-(_boxWidth / 2.0f) + _offsetX) * fScaleFector;
                    Y = (_boxHeight + MinDistance + _offsetY) * fScaleFector;
                    SetMatrix(canvas, X, Y, 0.0f, fScaleFector);
                    Draw(canvas);

                    X = ((_boxWidth / 2.0f) + MinDistance + _offsetX) * fScaleFector;
                    SetMatrix(canvas, X, Y, 0.0f, fScaleFector);
                    Draw(canvas);
                }
                else if (PatternType == 1)
                {
                    float X = _offsetX * fScaleFector;
                    float Y = _offsetY * fScaleFector;

                    SetMatrix(canvas, X, Y, 0.0f, fScaleFector);
                    Draw(canvas);
                }

                SkPatternImage = SKImage.FromBitmap(SkPatternBitmap);
            };
        }

        // Geometry 어레이를 삭제하는 함수 
        public void Dispose()
        {
            if (SkPatternBitmap != null)
            {
                SkPatternImage.Dispose();
                SkPatternBitmap.Dispose();
            }

            foreach (var path in ListSKPath)
            {
                path.Dispose();
            }
            ListSKPath.Clear();
        }
    }
}
