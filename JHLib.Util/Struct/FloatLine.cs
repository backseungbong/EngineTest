using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FloatLine(Float2D p1, Float2D p2)
    {
        public Float2D P1 = p1;
        public Float2D P2 = p2;
        public readonly float DX => P2.X - P1.X;
        public readonly float DY => P2.Y - P1.Y;
    }
}