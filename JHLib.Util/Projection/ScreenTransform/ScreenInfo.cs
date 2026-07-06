using JHLib.Util.Struct;

namespace JHLib.Util.Projection.ScreenTransform
{
    public class ScreenInfo(int w, int h, double pivotX, double pivotY, double pixelMM)
    {
        public const int SCREEN_MARGINX = 0;
        public const int SCREEN_MARGINY = 0;

        /// <summary> 화면 영역 Bound (화면의 픽셀영역에서 Margin을 제외한 영역) </summary>
        public readonly FloatRect FloatBound = new(SCREEN_MARGINX, SCREEN_MARGINY, w - SCREEN_MARGINX, h - SCREEN_MARGINY);

        /// <summary> 화면 픽셀 너비 </summary>
        public readonly int Width = w;

        /// <summary> 화면 픽셀 높이 </summary>
        public readonly int Height = h;

        /// <summary> 화면 피봇 X (이동 및 회전시 센터지점) </summary>
        public readonly double PivotScreenX = (w - SCREEN_MARGINX * 2) * pivotX + SCREEN_MARGINX;

        /// <summary> 화면 피봇 Y (이동 및 회전시 센터지점) </summary>
        public readonly double PivotScreenY = (h - SCREEN_MARGINY * 2) * pivotY + SCREEN_MARGINY;

        /// <summary> 화면 픽셀 너비, 높이중 짧은길이 </summary>
        public readonly int ShorterLength = w < h ? w : h;

        /// <summary> 화면 대각 픽셀 길이 </summary>
        public readonly int DiagonalLength = (int)MathF.Ceiling(w * w + h * h);


        /// <summary> 화면 픽셀 사이즈(MM) </summary>
        public readonly double PixelMM = pixelMM;

        /// <summary> 1미터(Meter)의 화면픽셀(Pixel) </summary>
        public readonly double FactorMeToPixel = 1000.0d / pixelMM;

        /// <summary> 1화면픽셀(Pixel)의 미터(Meter) </summary>>
        public readonly double FactorPixelToMe = pixelMM / 1000.0d;

        /// <summary> 1밀리미터(Millimeter)의 화면픽셀(Pixel) </summary>
        public readonly double FactorMMToPixel = 1.0d / pixelMM;

        /// <summary> 1화면픽셀(Pixel)의 밀리미터(Millimeter) </summary>
        public readonly double FactorPixelToMM = pixelMM / 1.0d;
    }
}