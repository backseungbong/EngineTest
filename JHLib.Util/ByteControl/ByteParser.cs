using JHLib.Util.ArrayControl;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JHLib.Util.ByteControl
{
    using static JHLib.Util.Helper.RefCommand;

    public unsafe class ByteParser
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AsType<T>(byte[] bytes, ref uint byteOffset) where T : unmanaged
        {
            var f = byteOffset; byteOffset = f + (uint)Unsafe.SizeOf<T>();
            return RefT<T>(bytes, (int)f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] AsTypeArray<T>(byte[] bytes, int count, ref uint byteOffset) where T : unmanaged
        {
            var f = byteOffset; byteOffset = f + (uint)(Unsafe.SizeOf<T>() * count);
            return AC.CopyNew(ref RefT<T>(bytes, (int)f), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToASCII(byte* p, int l, string defaultValue = null) => l > 0 ?
            FastASCII.ToASCII(p, l) : defaultValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToASCII(ref byte ref0, int l, string defaultValue = null) => l > 0 ?
            FastASCII.ToASCII(ref ref0, l) : defaultValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToUTF8(byte* p, int l, string defaultValue = null)
        {
            if (l > 0)
                return Encoding.UTF8.GetString(p, l);
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToUTF8(ref byte ref0, int l, string defaultValue = null)
        {
            if (l > 0)
                fixed (byte* p0 = &ref0)
                    return Encoding.UTF8.GetString(p0, l);
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToUTF16(byte* p, int l, string defaultValue = null) => l > 0 ?
            new(MemoryMarshal.CreateReadOnlySpan(ref *(char*)p, l)) : defaultValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToUTF16(ref byte ref0, int l, string defaultValue = null) => l > 0 ?
            new(MemoryMarshal.CreateReadOnlySpan(ref AsT<char>(ref ref0), l)) : defaultValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBool(byte* p, int l, bool defaultValue = false) => ToBool(ref *p, l, defaultValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool ToBool(ref byte p, int l, bool defaultValue = false)
        {
            if (l > 0)
            {
                do
                {
                    if (p > ASCII.SPACE) return (p & 0xDF) == ASCII.T;
                    p = ref Add1(ref p);
                }
                while (--l != 0);
            }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt(byte* p, int l, uint defaultValue = 0) => ToUInt(ref *p, l, defaultValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static uint ToUInt(ref byte p, int l, uint defaultValue = 0)
        {
            if (l > 0)
            {
                var v = 0u;
                var d = 10;
                ref var t = ref p;
                ref var e = ref AddB(ref t, l);
                do
                {
                    if (t > ASCII.SPACE)
                    {
                        do { if (t < '0' || '9' < t) break; v = (uint)t - '0' + v * 10; }
                        while (LessThan(ref t = ref Add1(ref t), ref e) && --d != 0);
                        return v;
                    }
                }
                while (LessThan(ref t = ref Add1(ref t), ref e));
            }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(byte* p, int l, int defaultValue = 0) => ToInt(ref *p, l, defaultValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ToInt(ref byte p, int l, int defaultValue = 0)
        {
            if (l > 0)
            {
                var v = 0;
                var d = 10;
                ref var t = ref p;
                ref var e = ref AddB(ref t, l);
                do
                {
                    if (t > ASCII.SPACE)
                    {
                        if (t == ASCII.MINUS)
                        {
                            if (LessThan(ref t = ref Add1(ref t), ref e) == false)
                                break;

                            do { if (t < '0' || '9' < t) { break; } v = t - '0' + v * 10; }
                            while (LessThan(ref t = ref Add1(ref t), ref e) && --d != 0);
                            return -v;
                        }
                        else
                        {
                            do { if (t < '0' || '9' < t) { break; } v = t - '0' + v * 10; }
                            while (LessThan(ref t = ref Add1(ref t), ref e) && --d != 0);
                            return v;
                        }
                    }
                }
                while (LessThan(ref t = ref Add1(ref t), ref e));
            }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float ToFloat(ref byte p, int l, float defaultValue = 0)
        {
            if (l > 0)
            {
                var v = 0;
                var m = 1;
                var d = 8;
                ref var t = ref p;
                ref var e = ref AddB(ref t, l);
                do
                {
                    if (t > ASCII.SPACE)
                    {
                        if (t == ASCII.MINUS)
                        {
                            if (LessThan(ref t = ref Add1(ref t), ref e) == false) break;
                            m = -1;
                        }

                        do
                        {
                            if (t < '0' || '9' < t)
                            {
                                if (t == ASCII.DOT && LessThan(ref t = ref Add1(ref t), ref e))
                                {
                                    do { if (t < '0' || '9' < t) break; v = t - '0' + v * 10; m *= 10; }
                                    while (LessThan(ref t = ref Add1(ref t), ref e) && --d != 0);
                                }
                                break;
                            }
                            v = t - '0' + v * 10;
                        }
                        while (LessThan(ref t = ref Add1(ref t), ref e) && --d != 0);
                        return v / (float)m;
                    }
                }
                while (LessThan(ref t = ref Add1(ref t), ref e));
            }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double ToDouble(ref byte p, int l, double defaultValue = 0)
        {
            if (l > 0)
            {
                var v = 0L;
                var m = 1L;
                var d = 16;

                ref var t = ref p;
                ref var e = ref AddB(ref t, l);
                do
                {
                    if (t > ASCII.SPACE)
                    {
                        if (t == ASCII.MINUS)
                        {
                            if (LessThan(ref t = ref Add1(ref t), ref e) == false) break;
                            m = -1;
                        }

                        do
                        {
                            if (t < '0' || '9' < t)
                            {
                                if (t == ASCII.DOT && LessThan(ref t = ref Add1(ref t), ref e))
                                {
                                    do { if (t < '0' || '9' < t) break; v = t - '0' + v * 10; m *= 10; }
                                    while (LessThan(ref t = ref Add1(ref t), ref e) && --d != 0);
                                }
                                break;
                            }
                            v = t - '0' + v * 10;
                        }
                        while (LessThan(ref t = ref Add1(ref t), ref e) && --d != 0);
                        return v / (double)m;
                    }
                }
                while (LessThan(ref t = ref Add1(ref t), ref e));
            }
            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static byte* MatchByte(byte* p, byte* e, byte b)
        {
            var t = p;
            if (t <= e - 4)
            {
            RE: if (t[0] == b) { goto J1; }
                if (t[1] == b) { goto J2; }
                if (t[2] == b) { goto J3; }
                if (t[3] == b) { goto J4; }
                if ((t += 4) <= e - 4) { goto RE; }
            }
            {
                if (t + 0 >= e || t[0] == b) { goto J1; }
                if (t + 1 >= e || t[1] == b) { goto J2; }
                if (t + 2 >= e || t[2] == b) { goto J3; }
            }
        J4: t += 1;
        J3: t += 1;
        J2: t += 1;
        J1: return t;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int MatchIndex(ref byte ref0, int read, int end, byte b)
        {
            var r = read;
            if (r <= end - 4)
            {
            RE: ref var t = ref Unsafe.AddByteOffset(ref ref0, (uint)r);
                if (Unsafe.AddByteOffset(ref t, 0) == b) { goto J1; }
                if (Unsafe.AddByteOffset(ref t, 1) == b) { goto J2; }
                if (Unsafe.AddByteOffset(ref t, 2) == b) { goto J3; }
                if (Unsafe.AddByteOffset(ref t, 3) == b) { goto J4; }
                if ((r += 4) <= end - 4) { goto RE; }
            }
            {
                ref var t = ref Unsafe.AddByteOffset(ref ref0, (uint)r);
                if (r + 0 >= end || Unsafe.AddByteOffset(ref t, 0) == b) { goto J1; }
                if (r + 1 >= end || Unsafe.AddByteOffset(ref t, 1) == b) { goto J2; }
                if (r + 2 >= end || Unsafe.AddByteOffset(ref t, 2) == b) { goto J3; }
            }
        J4: r += 1;
        J3: r += 1;
        J2: r += 1;
        J1: return r;
        }
    }

    public unsafe readonly ref struct SplitBucket
    {
        private readonly ref byte _data0;
        private readonly ref OffsetRange _range0;
        public readonly int Count;

        public DataRange this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Unsafe.Add(ref _range0, (uint)i).ToDataRange(ref _data0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SplitBucket(ref byte data0, int datal, byte seperator, ref OffsetRange range0, int rangeMax)
        {
            var count = 0;
            var start = 0;
            do
            {
                var read = ByteParser.MatchIndex(ref data0, start, datal, seperator);
                Unsafe.Add(ref range0, (uint)count) = new(start, read - start);
                start = read + 1; count++;
            }
            while (start < datal && count < rangeMax);

            _data0 = ref data0;
            _range0 = ref range0;
            Count = count;
        }
    }
}