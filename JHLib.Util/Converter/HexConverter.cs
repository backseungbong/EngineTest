namespace JHLib.Util.ByteControl
{
    using static JHLib.Util.Helper.RefCommand;
    public unsafe static class HexConverter
    {
        private static readonly byte[] _B2H;
        private static readonly byte[] _H2B;
        static HexConverter()
        {
            var b2h = new byte[128];
            b2h['0'] = 00;
            b2h['1'] = 01;
            b2h['2'] = 02;
            b2h['3'] = 03;
            b2h['4'] = 04;
            b2h['5'] = 05;
            b2h['6'] = 06;
            b2h['7'] = 07;
            b2h['8'] = 08;
            b2h['9'] = 09;
            b2h['a'] = 10;
            b2h['b'] = 11;
            b2h['c'] = 12;
            b2h['d'] = 13;
            b2h['e'] = 14;
            b2h['f'] = 15;
            b2h['A'] = 10;
            b2h['B'] = 11;
            b2h['C'] = 12;
            b2h['D'] = 13;
            b2h['E'] = 14;
            b2h['F'] = 15;

            var h2b = new byte[16];
            h2b[00] = (byte)'0';
            h2b[01] = (byte)'1';
            h2b[02] = (byte)'2';
            h2b[03] = (byte)'3';
            h2b[04] = (byte)'4';
            h2b[05] = (byte)'5';
            h2b[06] = (byte)'6';
            h2b[07] = (byte)'7';
            h2b[08] = (byte)'8';
            h2b[09] = (byte)'9';
            h2b[10] = (byte)'A';
            h2b[11] = (byte)'B';
            h2b[12] = (byte)'C';
            h2b[13] = (byte)'D';
            h2b[14] = (byte)'E';
            h2b[15] = (byte)'F';

            _B2H = b2h;
            _H2B = h2b;
        }

        private static readonly byte[] _empty16Byte = new byte[16];


        public static byte[] ToHexByte_AES_PKCS7_Pad16(string s)
        {
            if (s == null || s.Length < 2)
                return _empty16Byte;

            var h = _B2H;
            var n = s.Length >> 1;

            fixed (char* c = s)
            {
                var p = c;
                if (n < 16)
                {
                    var r = AES.Create_AES_PKCS7_Pad16(16 - n);
                    for (var i = 0; i < n; i++, p += 2)
                        r[i] = (byte)(h[p[0] & 127] << 4 | h[p[1] & 127]);
                    return r;
                }
                else
                {
                    var r = new byte[16];
                    for (var i = 0; i < 16; i++, p += 2)
                        r[i] = (byte)(h[p[0] & 127] << 4 | h[p[1] & 127]);
                    return r;
                }
            }
        }

        public static byte[] ToHexByte_AES_PKCS7_Pad16(byte[] b)
        {
            if (b != null && b.Length != 0)
                return ToHexByte_AES_PKCS7_Pad16(ref RefT(b), b.Length);
            return _empty16Byte;
        }

        public static byte[] ToHexByte_AES_PKCS7_Pad16(ref byte p, int l)
        {
            var n = l >> 1;
            if (n == 0) return _empty16Byte;

            var h = _B2H;
            if (n < 16)
            {
                var r = AES.Create_AES_PKCS7_Pad16(16 - n);
                var i = 0;
                do
                {
                    r[i] = (byte)(h[p & 127] << 4 | h[AddB(ref p, 1) & 127]);
                    p = ref AddB(ref p, 2);
                }
                while (++i < n);
                return r;
            }
            else
            {
                var r = new byte[16];
                var i = 0;
                do
                {
                    r[i] = (byte)(h[p & 127] << 4 | h[AddB(ref p, 1) & 127]);
                    p = ref AddB(ref p, 2);
                }
                while (++i < 16);
                return r;
            }
        }

        public static byte[] ToHexBytes(string s)
        {
            fixed (char* p = s)
                return ToHexBytes(p, s.Length);
        }
        public static byte[] ToHexBytes(char* s, int l)
        {
            var h = _B2H;
            var n = l >> 1;
            var r = new byte[n];
            for (var i = 0; i < n; i++, s += 2)
                r[i] = (byte)(h[s[0] & 127] << 4 | h[s[1] & 127]);
            return r;
        }

        public static byte[] ToHexBytes(byte[] bytes) => ToHexBytes(bytes, 0, bytes.Length);
        public static byte[] ToHexBytes(byte[] bytes, int count) => ToHexBytes(bytes, 0, count);
        public static byte[] ToHexBytes(byte[] bytes, int index, int count)
        {
            fixed (byte* p = &bytes[index])
                return ToHexBytes(p, count);
        }
        public static byte[] ToHexBytes(byte* pBytes, int l)
        {
            var h = _B2H;
            var n = l >> 1;
            var r = new byte[n];
            for (var i = 0; i < n; i++, pBytes += 2)
                r[i] = (byte)(h[pBytes[0] & 127] << 4 | h[pBytes[1] & 127]);
            return r;
        }

        public static string ToString(byte[] hexBytes)
        {
            fixed (byte* p = &hexBytes[0])
                return ToString(p, hexBytes.Length);
        }
        public static string ToString(byte* pHexBytes, int l)
        {
            if (l > 0)
            {
                var h = _H2B;
                var r = new string(default, l * 2);
                fixed (char* p = r)
                {
                    var d = p;
                    var i = 0;
                    do
                    {
                        var b = pHexBytes[i];
                        d[0] = (char)h[b >> 4];
                        d[1] = (char)h[b & 15];
                        d += 2;
                    }
                    while (++i < l);
                    return r;
                }
            }
            return null;
        }

        public static byte[] ToBytes(byte[] hexBytes)
        {
            fixed (byte* p = &hexBytes[0])
                return ToBytes(p, hexBytes.Length);
        }
        public static byte[] ToBytes(byte* pHexBytes, int l)
        {
            if (l > 0)
            {
                var h = _H2B;
                var r = new byte[l * 2];
                fixed (byte* p = &r[0])
                {
                    var d = p;
                    var i = 0;
                    do
                    {
                        var b = pHexBytes[i];
                        d[0] = h[b >> 4];
                        d[1] = h[b & 15];
                        d += 2;
                    }
                    while (++i < l);
                    return r;
                }
            }
            return null;
        }
    }
}