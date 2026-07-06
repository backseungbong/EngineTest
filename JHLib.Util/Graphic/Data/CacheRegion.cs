using System.Runtime.InteropServices;

namespace JHLib.Util.Graphic.Data
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public struct CacheRegion(uint nonRepeatedBytes, uint repeatedBytes)
    {
        public const int MIN_REGION = 2048;

        public uint NonRepeatedBytes = nonRepeatedBytes;
        public uint RepeatedBytes = repeatedBytes;
    }
}