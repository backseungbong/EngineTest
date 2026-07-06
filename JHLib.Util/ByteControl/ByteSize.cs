using System.Runtime.CompilerServices;

namespace JHLib.Util.ByteControl
{
    public static class ByteSize
    {
        public const int KB = 1024;
        public const int MB = KB * 1024;
        public const int GB = MB * 1024;
        public const long TB = GB * 1024L;
        public const long PB = TB * 1024L;
        public const long EB = PB * 1024L;

        public const int MB2 = MB * 2;
        public const int MB3 = MB * 3;
        public const int MB4 = MB * 4;
        public const int MB5 = MB * 5;
        public const int MB6 = MB * 6;
        public const int MB7 = MB * 7;
        public const int MB8 = MB * 8;
        public const int MB9 = MB * 9;
        public const int MB10 = MB * 10;
        public const int MB11 = MB * 11;
        public const int MB12 = MB * 12;
        public const int MB13 = MB * 13;
        public const int MB14 = MB * 14;
        public const int MB15 = MB * 15;
        public const int MB16 = MB * 16;
        public const int MB32 = MB * 32;
        public const int MB64 = MB * 64;
        public const int MB128 = MB * 128;
        public const int MB256 = MB * 256;
        public const int MB512 = MB * 512;

        public static string ToReadable(long byteSize)
        {
            var abs = Math.Abs(byteSize);
            if (abs < KB) return $"{byteSize:0Byte}";
            if (abs < MB) return $"{(double)(byteSize) / KB:0.0##KByte}";
            if (abs < GB) return $"{(double)(byteSize >> 10) / KB:0.0##MByte}";
            if (abs < TB) return $"{(double)(byteSize >> 20) / KB:0.0##GByte}";
            if (abs < PB) return $"{(double)(byteSize >> 30) / KB:0.0##TByte}";
            if (abs < EB) return $"{(double)(byteSize >> 40) / KB:0.0##PByte}";
            return $"{(double)(byteSize >> 50) / KB:0.0##EByte}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Align32(int size) => size + 31 & ~31;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Align64(int size) => size + 63 & ~63;
    }
}