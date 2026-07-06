using JHLib.Graphics;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.Chart
{
    public class DrawSKPath
    {
        public DrawSKPath()
        {
            IsFill = false;
            IsDraw = false;
            Trans = 1.0f;
            LineWidth = 1.0f;
            Radius = 0.0f;

            LineColorName = null;
            FillColorName = null;

            MainPath = null;
        }

        // Fill을 할 것인지 설정 변수
        public bool IsFill { get; set; }
        // Draw를 할 것인지 설정 변수
        public bool IsDraw { get; set; }
        // Transparency 설정 변수 (불투명도)
        public float Trans { get; set; }
        // 라인의 두께 설정 변수 
        public float LineWidth { get; set; }
        // 원일 경우 반지름을 설정할 변수 
        public float Radius { get; set; }

        // Line 색상명
        public string LineColorName { get; set; }
        // Fill 색상명
        public string FillColorName { get; set; }
        // Geometry
        public SKPath MainPath { get; set; }

        // Geometry를 구성할 위치 정보 저장 어레이
        public List<SKPoint> ListPos = new();

        // Matrix에 적용할 Scale Factor값
        private const float ScaleFactor = 0.0333f;

        // mainGeo를 삭제하는 함수 
        public void Dispose()
        {
            if (MainPath != null)
            {
                MainPath.Dispose();
            }

            ListPos.Clear();
        }

        // Point를 추가하는 함수 
        public void AddPoint(SKPoint point)
        {
            ListPos.Add(point);
        }

        // SKPath를 만드는 함수 
        public bool CreateSKPath()
        {
            if (MainPath != null) return false;

            // 원 / 점을 그리는 로직이면
            var nCount = ListPos.Count;
            if (nCount == 1 && Radius >= 0.0f)
            {
                CreateEllipseSKPath(ListPos[0]);
            }
            // 이어진 영역을 그리는 로직이면
            else if (nCount > 1)
            {
                CreateMainSKPath(IsFill);
            }
            else
            {
                return false;
            }

            return true;
        }

        // Main SKPath를 생성하는 함수 
        public void CreateMainSKPath(bool bClose = false)
        {
            var pathGeo = new SKPath();
            pathGeo.FillType = SKPathFillType.EvenOdd;
            pathGeo.AddPoly(ListPos.ToArray(), bClose);

            if (MainPath == null)
            {
                MainPath = pathGeo;
            }
            else
            {
                var tempPath = MainPath.Op(pathGeo, SKPathOp.Difference);
                pathGeo.Dispose();
                MainPath.Dispose();
                MainPath = tempPath;
            }
        }

        // Ellipse SKPath를 생성하는 함수 
        public void CreateEllipseSKPath(SKPoint centerPos)
        {
            var path = new SKPath();
            path.FillType = SKPathFillType.EvenOdd;
            if (Radius == 0.0f) path.AddCircle(centerPos.X, centerPos.Y, 2.0f);
            else path.AddCircle(centerPos.X, centerPos.Y, Radius);

            if (MainPath == null) MainPath = path;
            else
            {
                var tempPath = MainPath.Op(path, SKPathOp.Difference);
                path.Dispose();
                MainPath.Dispose();
                MainPath = tempPath;
            }
        }

        // Inner SKPath를 생성하는 함수 
        public bool CreateInnerSKPath(DrawSKPath clsInner, SKPathOp mode)
        {
            if (MainPath == null) return false;

            var tempPath = MainPath.Op(clsInner.MainPath, mode);
            MainPath.Dispose();
            MainPath = tempPath;

            return true;
        }

        // 실제 그리는 함수 
        public void Draw(GraphicsContext context, byte weatherIndex = 255)
        {
            if (weatherIndex == 255) weatherIndex = WeatherColor.WeatherIndex;

            if (IsFill == true)
            {
                var rgb = WeatherColor.GetColor(FillColorName, weatherIndex);
                context.SetFillColor(new SKColor(rgb.R, rgb.G, rgb.B, (byte)(Trans * 255)));
                context.FillPath(MainPath);
            }

            if (IsDraw == true)
            {
                var rgb = WeatherColor.GetColor(LineColorName, weatherIndex);
                context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B, (byte)(Trans * 255)));
                context.SetStrokeWidth(LineWidth / ScaleFactor);
                context.DrawPath(MainPath);
            }
        }

        public void Draw(SKCanvas canvas, byte weatherIndex = 255)
        {
            if (weatherIndex == 255) weatherIndex = WeatherColor.WeatherIndex;

            var paint = new SKPaint()
            {
                IsAntialias = true,
            };

            if (IsFill == true)
            {
                paint.Style = SKPaintStyle.Fill;
                var rgb = WeatherColor.GetColor(FillColorName, weatherIndex);
                paint.Color = new SKColor(rgb.R, rgb.G, rgb.B, (byte)(Trans * 255));
                canvas.DrawPath(MainPath, paint);
            }

            if (IsDraw == true)
            {
                // 실선 스타일 설정
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = LineWidth / ScaleFactor;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;

                var rgb = WeatherColor.GetColor(LineColorName, weatherIndex);
                paint.Color = new SKColor(rgb.R, rgb.G, rgb.B, (byte)(Trans * 255));
                canvas.DrawPath(MainPath, paint);
            }

            paint.Dispose();
        }
    }
}
