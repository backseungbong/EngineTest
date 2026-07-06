using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
    public struct Float3D
    {
        public const int SIZE = 12;

        public float X;
        public float Y;
        public float Z;
        public readonly ref Float2D XY => ref Unsafe.As<Float3D, Float2D>(ref Unsafe.AsRef(in this));
        public Float3D(float x, float y, float z) { X = x; Y = y; Z = z; }
        public Float3D(double x, double y, double z) { X = (float)x; Y = (float)y; Z = (float)z; }
    }
}