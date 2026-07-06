namespace JHLib.Util.Struct
{
    public struct DoubleLine
    {
        public double X1;
        public double Y1;
        public double X2;
        public double Y2;

        public Double2D P1 => new Double2D(X1, Y1);
        public Double2D P2 => new Double2D(X2, Y2);
        public double DX => X2 - X1;
        public double DY => Y2 - Y1;

        public DoubleLine(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
    }
}