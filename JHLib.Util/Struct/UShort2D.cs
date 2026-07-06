using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public struct UShort2D
    {
        public ushort X;
        public ushort Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UShort2D(int x, int y)
        {
            X = (ushort)x;
            Y = (ushort)y;
        }
    }
}