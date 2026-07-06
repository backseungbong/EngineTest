using System.Runtime.CompilerServices;

namespace JHLib.Util.Struct
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public struct Cell(float x, float y, float h, float d)
    {
        public const float SQUARE = 1.414213562f;

        public readonly float X = x;
        public readonly float Y = y;
        public readonly float Half = h;
        public readonly float Dis = d;
        public readonly float Max => Dis + Half * SQUARE;
    }
}