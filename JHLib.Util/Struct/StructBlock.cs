using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public readonly struct Block16 { }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public readonly struct Block32 { }

    public readonly unsafe struct StructBlock8
    {
        public readonly ulong Block;
        public readonly int ByteSize;
        public StructBlock8(ReadOnlySpan<char> str)
        {
            var block = new ulong();
            var block0 = (char*)&block;

            var c = Math.Min(str.Length, 4);
            if (c != 0)
            {
                var i = 0;
                do block0[i] = str[i];
                while (++i < c);
            }

            Block = block;
            ByteSize = c * 2;
        }
    }
}