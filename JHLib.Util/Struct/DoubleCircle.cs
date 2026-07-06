namespace JHLib.Util.Struct
{
    public struct DoubleCircle
    {
        public double CX;
        public double CY;
        public double Radius;

        public DoubleCircle(double cx, double cy, double radius)
        {
            CX = cx;
            CY = cy;
            Radius = radius;
        }

        public FloatEllipse ToFloat() => new FloatEllipse((float)CX, (float)CY, (float)Radius);
    }
}