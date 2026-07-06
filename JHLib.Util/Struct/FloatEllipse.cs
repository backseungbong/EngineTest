using System.Drawing;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
    public struct FloatEllipse
    {
        public const int SIZE = 12;

        public float CenterX;
        public float CenterY;
        public float Radius;

        public FloatEllipse(float cx, float cy, float radius)
        {
            CenterX = cx;
            CenterY = cy;
            Radius = radius;
        }

        public FloatEllipse(double cx, double cy, double radius)
        {
            CenterX = (float)cx;
            CenterY = (float)cy;
            Radius = (float)radius;
        }

        public DoubleCircle ToDouble() => new DoubleCircle(CenterX, CenterY, Radius);
    }
}