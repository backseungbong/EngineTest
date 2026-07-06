using JHLib.Util.Struct;

namespace JHLib.Util.XML
{
    public partial class FXReader
    {
        public int ContentFLByte => _cflbyte;
        public int ContentLength => _clength;
        public byte ContentByte(int index) => _stream.Ref(_coffset + index);
        public bool ContentEqual(int flByte) => _cflbyte == flByte;
        public bool ContentEqual(byte[] bytes) => RangeCompare(bytes, _stream, _coffset, _clength);
        public bool ContentEqual(string ascii) => RangeCompare(ascii, _stream, _coffset, _clength);
        public bool ContentRange(out DataRange range) =>
            (range = _stream.ToDataRange(_coffset, _clength)).Count > 0;

        public string ContentASCII => RangeToASCII(_stream, _coffset, _clength);
        public string ContentUTF8 => RangeToUTF8(_stream, _coffset, _clength);

        public bool ContentBool => RangeToBool(_stream, _coffset, _clength);
        public int ContentInt => RangeToInt(_stream, _coffset, _clength);
        public uint ContentUInt => RangeToUInt(_stream, _coffset, _clength);
        public float ContentFloat => RangeToFloat(_stream, _coffset, _clength);
        public double ContentDouble => RangeToDouble(_stream, _coffset, _clength);
        public byte[] ContentBytes => RangeToBytes(_stream, _coffset, _clength);
        public float[] ContentFloatArray(byte seperator) =>
            RangeToFloatArray(_stream, _coffset, _clength, seperator);
    }
}