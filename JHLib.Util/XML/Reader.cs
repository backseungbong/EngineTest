using JHLib.Util.ArrayControl;
using JHLib.Util.ByteControl;
using JHLib.Util.Pool;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.XML
{
    using static JHLib.Util.Helper.RefCommand;
    public partial class FXReader : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Size = SIZE)]
        private struct EndSigns // 데이타 끝에 Ascii 코드를 추가하여 오버 플로우 대비
        {
            private static readonly byte[] Signs =
            [
                ASCII.LTHAN,
                ASCII.GTHAN,
                ASCII.DQUOTE,
                ASCII.SQUOTE,
                ASCII.SPACE,
                ASCII.EQUAL,
                ASCII.SLASH,
                ASCII.QUESTION,
                ASCII.MINUS,
                ASCII.MINUS,
                ASCII.GTHAN,
            ];

            public const int SIZE = 64;
            public static readonly EndSigns Block;
            static unsafe EndSigns()
            {
                var t = new EndSigns();
                var b = (byte*)&t;
                for (var i = 0; i < SIZE; i++)
                    b[i] = Signs[i % Signs.Length];
                Block = t;
            }
        }

        [StructLayout(LayoutKind.Sequential, Size = 24)]
        private readonly struct Attr(int name0, int namel, int namefl, int value0, int valuel, int valuefl)
        {
            public readonly int NOffset = name0;
            public readonly int NLength = namel;
            public readonly int NFLByte = namefl;
            public readonly int VOffset = value0;
            public readonly int VLength = valuel;
            public readonly int VFLByte = valuefl;

            public bool NameEqual(int flByte) => NFLByte == flByte;
            public bool NameEqual(byte[] bytes, PoolStream stream) => RangeCompare(bytes, stream, NOffset, NLength);
            public bool NameEqual(string ascii, PoolStream stream) => RangeCompare(ascii, stream, NOffset, NLength);
            public string NameASCII(PoolStream stream) => RangeToASCII(stream, NOffset, NLength);
            public string NameUTF8(PoolStream stream) => RangeToUTF8(stream, NOffset, NLength);

            public bool ValueEqual(int flByte) => VFLByte == flByte;
            public bool ValueEqual(byte[] bytes, PoolStream stream) => RangeCompare(bytes, stream, VOffset, VLength);
            public bool ValueEqual(string ascii, PoolStream stream) => RangeCompare(ascii, stream, VOffset, VLength);
            public string ValueASCII(PoolStream stream) => RangeToASCII(stream, VOffset, VLength);
            public string ValueUTF8(PoolStream stream) => RangeToUTF8(stream, VOffset, VLength);
            public bool ValueBool(PoolStream stream) => RangeToBool(stream, VOffset, VLength);
            public int ValueInt(PoolStream stream) => RangeToInt(stream, VOffset, VLength);
            public uint ValueUInt(PoolStream stream) => RangeToUInt(stream, VOffset, VLength);
            public float ValueFloat(PoolStream stream) => RangeToFloat(stream, VOffset, VLength);
            public double ValueDouble(PoolStream stream) => RangeToDouble(stream, VOffset, VLength);
            public byte[] ValueBytes(PoolStream stream) => RangeToBytes(stream, VOffset, VLength);
            public float[] ValueFloatArray(PoolStream stream, byte seperator) =>
                RangeToFloatArray(stream, VOffset, VLength, seperator);
            public bool ValueRange(PoolStream stream, out DataRange range) =>
                (range = stream.ToDataRange(VOffset, VLength)).Count > 0;
        }

        private const int DEFAULT_ATTRCOUNT = 2;

        private readonly PoolStream _stream;
        private readonly int _end;

        private int _eoffset;
        private int _elength;
        private int _eflbyte;

        private int _coffset;
        private int _clength;
        private int _cflbyte;

        private Attr[] _attrbuk;
        private int _attrcnt;
        private int _attridx;
        private int _attrtar;

        private int _read;
        private int _depth;

        public int Depth => _depth;
        public int Length => _stream.Position;

        public void Dispose() => _stream.Dispose();
        public FXReader(string path, bool passProlog = true)
        {
            _stream = new PoolStream(path, EndSigns.SIZE);
            _stream.Ref<EndSigns>(_stream.Position) = EndSigns.Block;
            _end = _stream.Position;
            _attrbuk = new Attr[DEFAULT_ATTRCOUNT];

            if (passProlog)
                while (PrologNext()) ;
        }

        public FXReader(MemoryStream ms, bool passProlog = true) : this(ms.GetBuffer(), (int)ms.Length, passProlog) { }
        public FXReader(byte[] bytes, bool passProlog = true) : this(bytes, bytes != null ? bytes.Length : 0, passProlog) { }
        public FXReader(byte[] bytes, int length, bool passProlog = true)
        {
            _stream = new PoolStream(length + EndSigns.SIZE);
            _stream.Add(bytes, length);
            _stream.Ref<EndSigns>(length) = EndSigns.Block;
            _end = length;
            _attrbuk = new Attr[DEFAULT_ATTRCOUNT];

            if (passProlog)
                while (PrologNext()) ;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool PrologNext()
        {
            ref var data0 = ref _stream.Stream0;

            var read = _read;
        R1: if ((read = MatchIndex(ref data0, read, ASCII.LTHAN)) < _end)
            {
                if (AddB(ref data0, read + 1) != ASCII.QUESTION)
                {
                    if (ESeq(ref data0, read + 1, ASCII.EXMARK, ASCII.MINUS, ASCII.MINUS))
                    {
                        do read = MatchIndex(ref data0, read + 3, ASCII.GTHAN);
                        while (ESeq(ref data0, read - 2, ASCII.MINUS, ASCII.MINUS) == false);
                        goto R1;
                    }
                }
                else
                {
                    var offset = read += 2;
                    do read = LessIndex(ref data0, read + 1, ASCII.A);
                    while (EAny(ref data0, read, ASCII.SPACE, ASCII.QUESTION) == false);

                    _eoffset = offset;
                    _elength = read - offset;
                    _eflbyte = (AddB(ref data0, offset) << 8) | AddB(ref data0, read - 1);

                    var attrcnt = 0;
                    if (AddB(ref data0, read) == ASCII.SPACE)
                    {
                    R2: while (AddB(ref data0, ++read) <= ASCII.SPACE) ;
                        if (EAny(ref data0, read, ASCII.GTHAN, ASCII.QUESTION) == false)
                        {
                            var name0 = read;
                            read = MatchIndex(ref data0, name0, ASCII.EQUAL);
                            var namel = read - name0;
                            var namefl = (AddB(ref data0, name0) << 8) | AddB(ref data0, read - 1);

                            while (AddB(ref data0, ++read) <= ASCII.SPACE) ;
                            if (EAny(ref data0, read, ASCII.DQUOTE, ASCII.SQUOTE))
                            {
                                var value0 = read + 1;
                                read = MatchIndex(ref data0, value0, AddB(ref data0, read));
                                var valuel = read - value0;
                                var valuefl = (AddB(ref data0, value0) << 8) | AddB(ref data0, read - 1);

                                if (attrcnt == _attrbuk.Length) Resize(ref _attrbuk);
                                _attrbuk[attrcnt++] = new(name0, namel, namefl, value0, valuel, valuefl);
                                goto R2;
                            }
                        }
                    }
                    _attrcnt = attrcnt;
                    _attridx = 0;

                    _read = read;
                    return true;
                }
            }
            _eoffset = 0;
            _elength = 0;
            _eflbyte = 0;
            _attrcnt = 0;
            _attridx = 0;

            _read = read;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ElementNext(int depthTarget, out int flbyte)
        {
            var flag = ElementNext(depthTarget);
            flbyte = _eflbyte;
            return flag;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool ElementNext(int depthTarget)
        {
            ref var data0 = ref _stream.Stream0;

            var read = _read;
            var depth = _depth;
        R1: if ((read = MatchIndex(ref data0, read, ASCII.LTHAN)) < _end && depth + 1 >= depthTarget)
            {
                while (AddB(ref data0, ++read) <= ASCII.SPACE) ;
                if (AddB(ref data0, read) == ASCII.SLASH) { depth--; goto R1; }
                if (ESeq(ref data0, read, ASCII.EXMARK, ASCII.MINUS, ASCII.MINUS))
                {
                    do read = MatchIndex(ref data0, read + 3, ASCII.GTHAN);
                    while (ESeq(ref data0, read - 2, ASCII.MINUS, ASCII.MINUS) == false);
                    goto R1;
                }

                var offset = read;
                do read = LessIndex(ref data0, read + 1, ASCII.A);
                while (EAny(ref data0, read, ASCII.GTHAN, ASCII.SPACE, ASCII.SLASH) == false);

                if (depth + 1 > depthTarget)
                {
                    read = MatchIndex(ref data0, read, ASCII.GTHAN);
                    if (AddB(ref data0, read - 1) != ASCII.SLASH) depth++;
                    goto R1;
                }
                else
                {
                    _eoffset = offset;
                    _elength = read - offset;
                    _eflbyte = (AddB(ref data0, offset) << 8) | AddB(ref data0, read - 1);

                    var attrcnt = 0;
                    if (AddB(ref data0, read) == ASCII.SPACE)
                    {
                    R2: while (AddB(ref data0, ++read) <= ASCII.SPACE) ;
                        if (EAny(ref data0, read, ASCII.GTHAN, ASCII.SLASH) == false)
                        {
                            var name0 = read;
                            read = MatchIndex(ref data0, name0, ASCII.EQUAL);
                            var namel = read - name0;
                            var namefl = (AddB(ref data0, name0) << 8) | AddB(ref data0, read - 1);

                            while (AddB(ref data0, ++read) <= ASCII.SPACE) ;
                            if (EAny(ref data0, read, ASCII.DQUOTE, ASCII.SQUOTE))
                            {
                                var value0 = read + 1;
                                read = MatchIndex(ref data0, value0, AddB(ref data0, read));
                                var valuel = read - value0;
                                var valuefl = (AddB(ref data0, value0) << 8) | AddB(ref data0, read - 1);

                                if (attrcnt == _attrbuk.Length) Resize(ref _attrbuk);
                                _attrbuk[attrcnt++] = new(name0, namel, namefl, value0, valuel, valuefl);
                                goto R2;
                            }
                        }
                    }
                    _attrcnt = attrcnt;
                    _attridx = 0;

                    if (AddB(ref data0, read) == ASCII.GTHAN)
                    {
                    R3: while (AddB(ref data0, ++read) <= ASCII.SPACE) ;
                        offset = read;
                        read = MatchIndex(ref data0, read, ASCII.LTHAN);
                        if (ESeq(ref data0, read + 1, ASCII.EXMARK, ASCII.MINUS, ASCII.MINUS))
                        {
                            do read = MatchIndex(ref data0, read + 3, ASCII.GTHAN);
                            while (ESeq(ref data0, read - 2, ASCII.MINUS, ASCII.MINUS) == false);
                            goto R3;
                        }
                        _coffset = offset;
                        _clength = read - offset;
                        _cflbyte = (AddB(ref data0, offset) << 8) | AddB(ref data0, read - 1);

                        _read = read;
                        _depth = depth + 1;
                        return true;
                    }
                    else
                    {
                        _coffset = 0;
                        _clength = 0;
                        _cflbyte = 0;

                        _read = read;
                        _depth = depth;
                        return true;
                    }
                }
            }

            _eoffset = 0;
            _elength = 0;
            _eflbyte = 0;

            _coffset = 0;
            _clength = 0;
            _cflbyte = 0;

            _attrcnt = 0;
            _attridx = 0;

            _read = read;
            _depth = depth;
            return false;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Resize(ref Attr[] attrbuk)
        {
            var attrcap = attrbuk.Length;
            attrbuk = AC.CopyNew(attrbuk, attrcap * 2, attrcap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string RangeToASCII(PoolStream stream, int offset, int length) =>
            ByteParser.ToASCII(ref stream.Ref(offset), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string RangeToUTF8(PoolStream stream, int offset, int length) =>
            ByteParser.ToUTF8(ref stream.Ref(offset), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool RangeToBool(PoolStream stream, int offset, int length) =>
            ByteParser.ToBool(ref stream.Ref(offset), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RangeToInt(PoolStream stream, int offset, int length) =>
            ByteParser.ToInt(ref stream.Ref(offset), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RangeToUInt(PoolStream stream, int offset, int length) =>
            ByteParser.ToUInt(ref stream.Ref(offset), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float RangeToFloat(PoolStream stream, int offset, int length) =>
            ByteParser.ToFloat(ref stream.Ref(offset), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double RangeToDouble(PoolStream stream, int offset, int length) =>
            ByteParser.ToDouble(ref stream.Ref(offset), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] RangeToBytes(PoolStream stream, int offset, int length) =>
            AC.CopyNew(ref stream.Ref(offset), length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float[] RangeToFloatArray(PoolStream stream, int offset, int length, byte seperator)
        {
            if (length > 0)
            {
                ref var ref0 = ref stream.Ref(offset);
                var read = 0;
                var count = 0;
                var result = new float[4];
                do
                {
                    var start = read; read = ByteParser.MatchIndex(ref ref0, read, length, seperator);
                    if (start < read)
                    {
                        if (count == result.Length) result = AC.CopyNew(result, count * 2, count);
                        result[count++] = ByteParser.ToFloat(ref AddB(ref ref0, start), read - start);
                    }
                }
                while (++read < length);

                if (count != 0)
                    return count != result.Length ? AC.CopyNew(result, count) : result;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool RangeCompare(ReadOnlySpan<byte> bytes, PoolStream stream, int offset, int length) =>
            bytes.Length == length && AC.IsEqualUnsafe(ref stream.Ref(offset), ref MemoryMarshal.GetReference(bytes), length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool RangeCompare(string ascii, PoolStream stream, int offset, int length) =>
            ascii.Length == length && ASCIICompareInternal(ref stream.Ref(offset), ref RefT(ascii), length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ASCIICompareInternal(ref byte ref0, ref char char0, int l)
        {
            ref var b = ref ref0;
            ref var c = ref AsB(ref char0);
            if (l > 4)
            {
                do
                {
                    if (b != c ||
                        AddB(ref b, 1) != AddB(ref c, 2) ||
                        AddB(ref b, 2) != AddB(ref c, 4) ||
                        AddB(ref b, 3) != AddB(ref c, 6)) goto FALSE;
                    b = ref AddB(ref b, 4);
                    c = ref AddB(ref c, 8);
                }
                while ((l -= 4) > 4);
            }

            if (b == c && AddB(ref b, l - 1) == AddB(ref c, (l - 1) * 2))
            {
                if (l <= 2 || (AddB(ref b, 1) == AddB(ref c, 2) && AddB(ref b, 2) == AddB(ref c, 4)))
                    return true;
            }
        FALSE:
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MatchIndex(ref byte ref0, int read, byte b)
        {
            var r = read;
            while (true)
            {
                ref var t = ref AddB(ref ref0, r);
                if (t != b)
                    if (AddB(ref t, 1) != b)
                        if (AddB(ref t, 2) != b)
                            if (AddB(ref t, 3) != b) r += 4;
                            else return r + 3;
                        else return r + 2;
                    else return r + 1;
                else return r;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LessIndex(ref byte ref0, int read, byte b)
        {
            var r = read;
            while (true)
            {
                ref var t = ref AddB(ref ref0, r);
                if (t >= b)
                    if (AddB(ref t, 1) >= b)
                        if (AddB(ref t, 2) >= b)
                            if (AddB(ref t, 3) >= b) r += 4;
                            else return r + 3;
                        else return r + 2;
                    else return r + 1;
                else return r;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ESeq(ref byte ref0, int read, byte b1, byte b2, byte b3)
        {
            ref var t = ref AddB(ref ref0, read);
            return t == b1 && AddB(ref t, 1) == b2 && AddB(ref t, 2) == b3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ESeq(ref byte ref0, int read, byte b1, byte b2)
        {
            ref var t = ref AddB(ref ref0, read);
            return t == b1 && AddB(ref t, 1) == b2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EAny(ref byte ref0, int read, byte b1, byte b2, byte b3)
        {
            ref var t = ref AddB(ref ref0, read);
            return t == b1 || t == b2 || t == b3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EAny(ref byte ref0, int read, byte b1, byte b2)
        {
            ref var t = ref AddB(ref ref0, read);
            return t == b1 || t == b2;
        }
    }
}