using JHLib.Util.ByteControl;

namespace JHLib.Util.AIS
{
    internal static class AISDec
    {
        public static readonly byte[] ASCII_TO_RBIT6;
        public static readonly byte[] RBIT6_TO_ASCII;
        static AISDec()
        {
            var a2b = new byte[256];
            var b2a = new byte[64];

            var i = 0;
            var a = 48;
            do
            {
                var b = BitReverse.Bit8(i << 2);
                a2b[a] = (byte)b;
                b2a[b] = (byte)(i + 64 - (i & 32) * 2);
                i++;
            }
            while (++a <= 87);

            a = 96;
            do
            {
                var b = BitReverse.Bit8(i << 2);
                a2b[a] = (byte)b;
                b2a[b] = (byte)(i + 64 - (i & 32) * 2);
                i++;
            }
            while (++a <= 119);

            ASCII_TO_RBIT6 = a2b;
            RBIT6_TO_ASCII = b2a;
        }
    }

    internal static class AISEnc
    {
        public static readonly byte[] RBIT6_TO_ASCII;
        public static readonly byte[] ASCII_TO_RBIT6;

        static AISEnc()
        {
            var b2a = new byte[64];
            var a2b = new byte[256];

            var i = 0;
            var a = 48;
            do
            {
                var b = BitReverse.Bit8(i << 2);
                b2a[b] = (byte)a;
                a2b[i + 64 - (i & 32) * 2] = (byte)b;
                a++;
            }
            while (++i <= 39);

            a = 96;
            do
            {
                var b = BitReverse.Bit8(i << 2);
                b2a[b] = (byte)a;
                a2b[i + 64 - (i & 32) * 2] = (byte)b;
                a++;
            }
            while (++i <= 63);

            RBIT6_TO_ASCII = b2a;
            ASCII_TO_RBIT6 = a2b;
        }
    }
}