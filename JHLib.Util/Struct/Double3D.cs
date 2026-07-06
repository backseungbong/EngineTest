namespace JHLib.Util.Struct
{
    public struct Double3D
    {
        public double X;
        public double Y;
        public double Z;

        public Double3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Float3D ToFloat() => new Float3D((float)X, (float)Y, (float)Z);
    }
}