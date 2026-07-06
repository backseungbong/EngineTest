using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Sequential)]
    public struct FloatExtents(in Float2D center, in Float2D halfExtents)
    {
        public Float2D Center = center;
        public Float2D HalfExtents = halfExtents;
    }
}