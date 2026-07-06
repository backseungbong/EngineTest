using JHLib.Util.ByteControl;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.XML
{
    public partial class FXReader
    {
        public int ElementFLByte => _eflbyte;
        public int ElementLength => _elength;
        public byte ElementByte(int index) => _stream.Ref(_eoffset + index);
        public bool ElementEqual(int flByte) => _eflbyte == flByte;
        public bool ElementEqual(byte[] bytes) => RangeCompare(bytes, _stream, _eoffset, _elength);
        public bool ElementEqual(string ascii) => RangeCompare(ascii, _stream, _eoffset, _elength);
        public bool ElementRange(out DataRange range) =>
            (range = _stream.ToDataRange(_eoffset, _elength)).Count > 0;

        public string ElementASCII => RangeToASCII(_stream, _eoffset, _elength);
        public string ElementUTF8 => RangeToUTF8(_stream, _eoffset, _elength);

        public Span<byte> ElementSpan(bool withoutNamespace = false)
        {
            ref var s0 = ref _stream.Ref(_eoffset);
            var l = _elength;
            if (l > 0 && withoutNamespace)
            {
                var c = ByteParser.MatchIndex(ref s0, 0, l, ASCII.COLON) + 1;
                if (c <= l)
                {
                    s0 = ref Unsafe.Add(ref s0, (uint)c);
                    l -= c;
                }
            }
            return MemoryMarshal.CreateSpan(ref s0, l);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool ElementFind(int depthTarget, int flByte)
        {
            while (ElementNext(depthTarget))
            {
                if (_eflbyte == flByte)
                    return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool ElementFind(int depthTarget, string ascii)
        {
            while (ElementNext(depthTarget))
            {
                if (RangeCompare(ascii, _stream, _eoffset, _elength))
                    return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ElementFind(int depthTarget, byte[] bytes) => ElementFind(depthTarget, bytes, false);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool ElementFind(int depthTarget, ReadOnlySpan<byte> bytes, bool withoutNamespace = false)
        {
            while (ElementNext(depthTarget))
            {
                var span = ElementSpan(withoutNamespace);
                if (span.SequenceEqual(bytes))
                    return true;
            }
            return false;
        }
    }
}