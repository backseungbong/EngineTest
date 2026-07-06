using JHLib.Util.ArrayControl;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.ByteControl
{
    using static JHLib.Util.Helper.RefCommand;
    public unsafe static class FastASCII
    {
        private static readonly byte[] LowerMap;
        private static readonly byte[] UpperMap;
        static FastASCII()
        {
            LowerMap = new byte[256];
            UpperMap = new byte[256];

            for (var i = 0; i < 256; i++)
                LowerMap[i] = (byte)(065 <= i && i <= 090 ? i + 32 : i);
            for (var i = 0; i < 256; i++)
                UpperMap[i] = (byte)(097 <= i && i <= 122 ? i - 32 : i);
        }

        public static void ToLower(string s) => ToCase(s, LowerMap);
        public static void ToUpper(string s) => ToCase(s, UpperMap);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ToCase(string s, byte[] c)
        {
            var l = s.Length;
            if (l != 0)
            {
                fixed (char* f = s)
                {
                    var p = (byte*)f;
                    var e = f + l;
                    do { p[0] = c[p[0]]; p[2] = c[p[2]]; }
                    while ((p += 4) < e);
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string[] SplitTrim(string ascii, char seperator, int desireSplitCount = 4)
        {
            if (ascii != null && ascii.Length != 0)
            {
                fixed (char* c0 = ascii)
                {
                    var t = c0;
                    var p = c0;
                    var e = c0 + ascii.Length;
                    var l = new OffsetRange[desireSplitCount];
                    var c = 0;

                    do
                    {
                        if (*p == seperator)
                        {
                            if (t < p)
                            {
                                var d = p;
                                do
                                {
                                    if (*(t + 0) <= ASCII.SPACE) t++;
                                    else if (*(d - 1) <= ASCII.SPACE) d--;
                                    else
                                    {
                                        if (c == l.Length) l = AC.CopyNew(l, c * 2, c);
                                        l[c++] = new((int)(t - c0), (int)(d - t));
                                        break;
                                    }
                                }
                                while (t < d);
                            }
                            t = p + 1;
                        }
                    }
                    while (++p < e);

                    if (t < e)
                    {
                        do
                        {
                            if (*(t + 0) <= ASCII.SPACE) t++;
                            else if (*(e - 1) <= ASCII.SPACE) e--;
                            else
                            {
                                if (c == l.Length) l = AC.CopyNew(l, c * 2, c);
                                l[c++] = new((int)(t - c0), (int)(e - t));
                                break;
                            }
                        }
                        while (t < e);
                    }

                    if (c != 0)
                    {
                        var span = ascii.AsSpan();
                        var r = new string[c];
                        do r[--c] = new string(span.Slice(l[c].Offset, l[c].Length));
                        while (c != 0);
                        return r;
                    }
                }
            }
            return [];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareNumber(string ascii, uint num)
        {
            var c = ascii.Length;
            var n = num;
            if (n != 0)
            {
                if (c != 0)
                {
                    do if (ascii[--c] - '0' != n - (n /= 10) * 10) return false;
                    while (c != 0 && n != 0);
                }
                return c == 0 && n == 0;
            }
            return c == 1 && ascii[0] == '0';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareBytes(string ascii, byte[] bytes) =>
            CompareBytes(ascii, ref RefT(bytes), bytes.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CompareBytes(string ascii, ref byte ref0, int len)
        {
            if (ascii.Length == len)
            {
                fixed (char* char0 = ascii)
                fixed (byte* byte0 = &ref0)
                {
                    var c = (byte*)char0;
                    var b = byte0;
                    if (len > 4)
                    {
                        do
                        {
                            if (b[0] != c[0] || b[1] != c[2] || b[2] != c[4] || b[3] != c[6])
                                return false;
                            b += 4;
                            c += 8;
                        }
                        while ((len -= 4) > 4);
                    }

                    if (b[0] == c[0] && b[len - 1] == c[len * 2 - 2])
                        if (len <= 2 || (b[1] == c[2] && b[2] == c[4]))
                            return true;
                    return false;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareIgnoreCase(string ascii, string compare) => CompareIgnoreCase(ascii.AsSpan(), compare.AsSpan());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CompareIgnoreCase(ReadOnlySpan<char> ascii, ReadOnlySpan<char> compare)
        {
            var l = compare.Length;
            if (l == ascii.Length)
            {
                if (l > 1)
                {
                    fixed (char* a0 = ascii)
                    fixed (char* b0 = compare)
                        return CompareIgnoreCaseInternal(a0, b0, l);
                }
                return l == 0 || (ascii[0] & 0xDF) == (compare[0] & 0xDF);
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartWithIgnoreCase(string ascii, string startwith) => StartWithIgnoreCase(ascii.AsSpan(), startwith.AsSpan());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StartWithIgnoreCase(ReadOnlySpan<char> ascii, ReadOnlySpan<char> startwith)
        {
            var l = startwith.Length;
            if (l <= ascii.Length)
            {
                if (l > 1)
                {
                    fixed (char* a0 = ascii)
                    fixed (char* b0 = startwith)
                        return CompareIgnoreCaseInternal(a0, b0, l);
                }
                return l == 0 || (ascii[0] & 0xDF) == (startwith[0] & 0xDF);
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainIgnoreCase(string ascii, string contain) => ContainIgnoreCase(ascii.AsSpan(), contain.AsSpan());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool ContainIgnoreCase(ReadOnlySpan<char> ascii, ReadOnlySpan<char> contain)
        {
            var l = contain.Length;
            if (l <= ascii.Length)
            {
                fixed (char* a0 = ascii)
                fixed (char* b0 = contain)
                {
                    var a = a0;
                    var e = a0 + (ascii.Length - l);
                    var c = *b0 & 0xDF;

                    if (l > 1)
                    {
                        do if ((*a & 0xDF) == c && CompareIgnoreCaseInternal(a, b0, l)) return true;
                        while (++a <= e);
                    }
                    else
                    {
                        do if ((*a & 0xDF) == c) return true;
                        while (++a <= e);
                    }
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CompareIgnoreCaseInternal(char* a, char* b, int l)
        {
            const uint M = 0xDF << 16 | 0xDF;

            if ((*(uint*)a & M) == (*(uint*)b & M))
            {
                if (l > 4)
                {
                    var i = 2;
                    do if ((*(uint*)(a + i) & M) != (*(uint*)(b + i) & M)) return false;
                    while ((i += 2) < (l - 2));
                }
                return (*(uint*)(a + (l - 2)) & M) == (*(uint*)(b + (l - 2)) & M);
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static byte[] ToBytes(string str)
        {
            var l = str.Length;
            if (l > 0)
            {
                var r = new byte[l];
                ref var b = ref RefB(r);
                ref var c = ref RefB(str);

                if (l <= 4)
                {
                    b = c;
                    if (l > 2)
                    {
                        AddB(ref b, 1) = AddB(ref c, 2);
                        AddB(ref b, 2) = AddB(ref c, 4);
                    }
                    AddB(ref b, l - 1) = AddB(ref c, (l - 1) * 2);
                    return r;
                }

                var n = l - 4;
                do
                {
                    b = c;
                    AddB(ref b, 1) = AddB(ref c, 2);
                    AddB(ref b, 2) = AddB(ref c, 4);
                    AddB(ref b, 3) = AddB(ref c, 6);
                    c = ref AddB(ref c, 8);
                    b = ref AddB(ref b, 4);
                }
                while ((n -= 4) > 0);

                c = ref AddB(ref c, n * 2);
                b = ref AddB(ref b, n);
                b = c;
                AddB(ref b, 1) = AddB(ref c, 2);
                AddB(ref b, 2) = AddB(ref c, 4);
                AddB(ref b, 3) = AddB(ref c, 6);
                return r;
            }
            return [];
        }

        public static string ToASCII(byte[] bytes) => ToASCII(bytes, 0, bytes.Length);
        public static string ToASCII(byte[] bytes, int count) => ToASCII(bytes, 0, count);
        public static string ToASCII(byte[] bytes, int index, int count) => ToASCII(ref RefT(bytes, index), count);
        public static string ToASCII(byte* p, int l) => ToASCII(ref *p, l);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ToASCII(ref byte b, int l)
        {
            if (l > 0)
            {
                var r = new string(default, l);
                ref var c = ref RefB(r);

                if (l <= 4)
                {
                    c = b;
                    if (l > 2)
                    {
                        AddB(ref c, 2) = AddB(ref b, 1);
                        AddB(ref c, 4) = AddB(ref b, 2);
                    }
                    AddB(ref c, (l - 1) * 2) = AddB(ref b, l - 1);
                    return r;
                }

                var n = l - 4;
                do
                {
                    c = b;
                    AddB(ref c, 2) = AddB(ref b, 1);
                    AddB(ref c, 4) = AddB(ref b, 2);
                    AddB(ref c, 6) = AddB(ref b, 3);
                    b = ref AddB(ref b, 4);
                    c = ref AddB(ref c, 8);
                }
                while ((n -= 4) > 0);

                b = ref AddB(ref b, n);
                c = ref AddB(ref c, n * 2);
                c = b;
                AddB(ref c, 2) = AddB(ref b, 1);
                AddB(ref c, 4) = AddB(ref b, 2);
                AddB(ref c, 6) = AddB(ref b, 3);
                return r;
            }
            return null;
        }

        public static string ToASCIITrim(byte[] bytes) => ToASCIITrim(bytes, 0, bytes.Length);
        public static string ToASCIITrim(byte[] bytes, int count) => ToASCIITrim(bytes, 0, count);
        public static string ToASCIITrim(byte[] bytes, int index, int count) => ToASCIITrim(ref RefT(bytes, index), count);
        public static string ToASCIITrim(byte* p, int l) => ToASCIITrim(ref *p, l);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToASCIITrim(ref byte b, int l)
        {
            if (l > 0)
            {
                ref var end0 = ref AddB(ref b, l);
                do
                {
                    if (b > ASCII.SPACE)
                        if (Sub1(ref end0) > ASCII.SPACE)
                            return ToASCII(ref b, SubRef(ref b, ref end0));
                        else end0 = ref Sub1(ref end0);
                    else b = ref Add1(ref b);
                }
                while (LessThan(ref b, ref end0));
            }
            return null;
        }

        public static string ToASCIITrimLower(byte[] bytes) => ToASCIITrimLower(bytes, 0, bytes.Length);
        public static string ToASCIITrimLower(byte[] bytes, int count) => ToASCIITrimLower(bytes, 0, count);
        public static string ToASCIITrimLower(byte[] bytes, int index, int count) => ToASCIITrimLower(ref RefT(bytes, index), count);
        public static string ToASCIITrimLower(byte* p, int l) => ToASCIITrimLower(ref *p, l);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ToASCIITrimLower(ref byte b, int l)
        {
            if (l > 0)
            {
                ref var end0 = ref AddB(ref b, l);
                do
                {
                    if (b > ASCII.SPACE)
                        if (Sub1(ref end0) > ASCII.SPACE)
                            return ToASCIICaseInternal(ref b, SubRef(ref b, ref end0), LowerMap);
                        else end0 = ref Sub1(ref end0);
                    else b = ref Add1(ref b);
                }
                while (LessThan(ref b, ref end0));
            }
            return null;
        }

        public static string ToASCIITrimUpper(byte[] bytes) => ToASCIITrimUpper(bytes, 0, bytes.Length);
        public static string ToASCIITrimUpper(byte[] bytes, int count) => ToASCIITrimUpper(bytes, 0, count);
        public static string ToASCIITrimUpper(byte[] bytes, int index, int count) => ToASCIITrimUpper(ref RefT(bytes, index), count);
        public static string ToASCIITrimUpper(byte* p, int l) => ToASCIITrimUpper(ref *p, l);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ToASCIITrimUpper(ref byte b, int l)
        {
            if (l > 0)
            {
                ref var e = ref AddB(ref b, l);
                do
                {
                    if (b > ASCII.SPACE)
                        if (Sub1(ref e) > ASCII.SPACE)
                            return ToASCIICaseInternal(ref b, SubRef(ref b, ref e), UpperMap);
                        else e = ref Sub1(ref e);
                    else b = ref Add1(ref b);
                }
                while (LessThan(ref b, ref e));
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToASCIICaseInternal(ref byte b, int l, byte[] map)
        {
            if (l > 0)
            {
                var r = new string(default, l);
                ref var c = ref RefB(r);

                if (l <= 4)
                {
                    c = map[b];
                    if (l > 2)
                    {
                        AddB(ref c, 2) = map[AddB(ref b, 1)];
                        AddB(ref c, 4) = map[AddB(ref b, 2)];
                    }
                    AddB(ref c, (l - 1) * 2) = map[AddB(ref b, l - 1)];
                    return r;
                }

                var n = l - 4;
                do
                {
                    c = map[b];
                    AddB(ref c, 2) = map[AddB(ref b, 1)];
                    AddB(ref c, 4) = map[AddB(ref b, 2)];
                    AddB(ref c, 6) = map[AddB(ref b, 3)];
                    b = ref AddB(ref b, 4);
                    c = ref AddB(ref c, 8);
                }
                while ((n -= 4) > 0);

                b = ref AddB(ref b, n);
                c = ref AddB(ref c, n * 2);
                c = map[b];
                AddB(ref c, 2) = map[AddB(ref b, 1)];
                AddB(ref c, 4) = map[AddB(ref b, 2)];
                AddB(ref c, 6) = map[AddB(ref b, 3)];
                return r;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char* FindChar0(char* p, char* e, char seperator)
        {
            var t = p;
            if (t <= e - 4)
            {
            RE: if (t[0] == seperator) { goto J1; }
                if (t[1] == seperator) { goto J2; }
                if (t[2] == seperator) { goto J3; }
                if (t[3] == seperator) { goto J4; }
                if ((t += 4) <= e - 4) { goto RE; }
            }
            {
                if (t + 0 >= e || t[0] == seperator) { goto J1; }
                if (t + 1 >= e || t[1] == seperator) { goto J2; }
                if (t + 2 >= e || t[2] == seperator) { goto J3; }
            }
        J4: t += 1;
        J3: t += 1;
        J2: t += 1;
        J1: return t;
        }
    }
}