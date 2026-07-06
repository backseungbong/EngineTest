using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Geometry
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public readonly struct CornerInfo(OutCode corner, int clippedIndex)
    {
        private readonly uint _info = ((uint)clippedIndex << 2) | (uint)corner >> 2;
        public readonly int CornerIndex => (int)(_info & 3);
        public readonly int ClippedPathIndex => (int)(_info >> 2);
    }
}