using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Util.XML
{
    public partial class FXReader
    {
        public int AttrCount => _attrcnt;

        public int AttrNameFLByte => _attrbuk[_attrtar].NFLByte;
        public int AttrNameLength => _attrbuk[_attrtar].NLength;
        public int AttrNameByte(int index) => _stream.Ref(_attrbuk[_attrtar].NOffset + index);
        public string AttrNameASCII => _attrbuk[_attrtar].NameASCII(_stream);
        public string AttrNameUTF8 => _attrbuk[_attrtar].NameUTF8(_stream);

        public int AttrValueFLByte => _attrbuk[_attrtar].VFLByte;
        public int AttrValueLength => _attrbuk[_attrtar].VLength;
        public byte AttrValueByte(int index) => _stream.Ref(_attrbuk[_attrtar].VOffset + index);
        public string AttrValueASCII => _attrbuk[_attrtar].ValueASCII(_stream);
        public string AttrValueUTF8 => _attrbuk[_attrtar].ValueUTF8(_stream);

        public bool AttrValueEqual(int flByte) => _attrbuk[_attrtar].ValueEqual(flByte);
        public bool AttrValueEqual(byte[] bytes) => _attrbuk[_attrtar].ValueEqual(bytes, _stream);
        public bool AttrValueEqual(string ascii) => _attrbuk[_attrtar].ValueEqual(ascii, _stream);
        public bool AttrValueRange(out DataRange range) => _attrbuk[_attrtar].ValueRange(_stream, out range);

        public bool AttrValueBool => _attrbuk[_attrtar].ValueBool(_stream);
        public int AttrValueInt => _attrbuk[_attrtar].ValueInt(_stream);
        public uint AttrValueUInt => _attrbuk[_attrtar].ValueUInt(_stream);
        public float AttrValueFloat => _attrbuk[_attrtar].ValueFloat(_stream);
        public double AttrValueDouble => _attrbuk[_attrtar].ValueDouble(_stream);
        public byte[] AttrValueBytes => _attrbuk[_attrtar].ValueBytes(_stream);
        public float[] AttrValueFloatArray(byte seperator) =>
            _attrbuk[_attrtar].ValueFloatArray(_stream, seperator);


        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AttrTarget(string ascii)
        {
            var c = _attrcnt;
            if (c != 0)
            {
                var i = _attridx;
                do
                {
                    if (_attrbuk[i].NameEqual(ascii, _stream))
                    {
                        _attrtar = i;
                        _attridx = (i + 1) % c;
                        return true;
                    }
                    i = (i + 1) % c;
                }
                while (i != _attridx);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AttrTarget(byte[] bytes)
        {
            var c = _attrcnt;
            if (c != 0)
            {
                var i = _attridx;
                do
                {
                    if (_attrbuk[i].NameEqual(bytes, _stream))
                    {
                        _attrtar = i;
                        _attridx = (i + 1) % c;
                        return true;
                    }
                    i = (i + 1) % c;
                }
                while (i != _attridx);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool AttrTarget(int flByte)
        {
            var c = _attrcnt;
            if (c != 0)
            {
                var i = _attridx;
                do
                {
                    if (_attrbuk[i].NameEqual(flByte))
                    {
                        _attrtar = i;
                        _attridx = (i + 1) % c;
                        return true;
                    }
                    i = (i + 1) % c;
                }
                while (i != _attridx);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AttrNext(out int flByte)
        {
            var i = _attridx;
            if (i < _attrcnt)
            {
                _attrtar = i;
                _attridx = i + 1;
                flByte = _attrbuk[i].NFLByte;
                return true;
            }
            flByte = 0;
            return false;
        }

        public bool AttrGetAsBool(string asciiName, bool defaultValue = false) =>
            AttrTarget(asciiName) ? AttrValueBool : defaultValue;
        public byte AttrGetAsByte(string asciiName, int byteIndex, byte defaultValue = 0) =>
            AttrTarget(asciiName) ? AttrValueByte(byteIndex) : defaultValue;
        public int AttrGetAsInt(string asciiName, int defaultValue = 0) =>
            AttrTarget(asciiName) ? AttrValueInt : defaultValue;
        public uint AttrGetAsUInt(string asciiName, uint defaultValue = 0) =>
            AttrTarget(asciiName) ? AttrValueUInt : defaultValue;
        public float AttrGetAsFloat(string asciiName, float defaultValue = 0) =>
            AttrTarget(asciiName) ? AttrValueFloat : defaultValue;
        public double AttrGetAsDouble(string asciiName, double defaultValue = 0) =>
            AttrTarget(asciiName) ? AttrValueDouble : defaultValue;
        public byte[] AttrGetAsBytes(string asciiName, byte[] defaultValue = null) =>
            AttrTarget(asciiName) ? AttrValueBytes : defaultValue;
        public float[] AttrGetAsFloatArray(string asciiName, byte seperator, float[] defaultValue = null) =>
            AttrTarget(asciiName) ? AttrValueFloatArray(seperator) : defaultValue;
        public string AttrGetAsASCII(string asciiName, string defaultValue = null) =>
            AttrTarget(asciiName) ? AttrValueASCII : defaultValue;
        public string AttrGetAsUTF8(string asciiName, string defaultValue = null) =>
            AttrTarget(asciiName) ? AttrValueUTF8 : defaultValue;


        public bool AttrGetAsBool(byte[] bytesName, bool defaultValue = false) =>
            AttrTarget(bytesName) ? AttrValueBool : defaultValue;
        public byte AttrGetAsByte(byte[] bytesName, int byteIndex, byte defaultValue = 0) =>
            AttrTarget(bytesName) ? AttrValueByte(byteIndex) : defaultValue;
        public int AttrGetAsInt(byte[] bytesName, int defaultValue = 0) =>
            AttrTarget(bytesName) ? AttrValueInt : defaultValue;
        public uint AttrGetAsUInt(byte[] bytesName, uint defaultValue = 0) =>
            AttrTarget(bytesName) ? AttrValueUInt : defaultValue;
        public float AttrGetAsFloat(byte[] bytesName, float defaultValue = 0) =>
            AttrTarget(bytesName) ? AttrValueFloat : defaultValue;
        public double AttrGetAsDouble(byte[] bytesName, double defaultValue = 0) =>
            AttrTarget(bytesName) ? AttrValueDouble : defaultValue;
        public byte[] AttrGetAsBytes(byte[] bytesName, byte[] defaultValue = null) =>
            AttrTarget(bytesName) ? AttrValueBytes : defaultValue;
        public float[] AttrGetAsFloatArray(byte[] bytesName, byte seperator, float[] defaultValue = null) =>
            AttrTarget(bytesName) ? AttrValueFloatArray(seperator) : defaultValue;
        public string AttrGetAsASCII(byte[] bytesName, string defaultValue = null) =>
            AttrTarget(bytesName) ? AttrValueASCII : defaultValue;
        public string AttrGetAsUTF8(byte[] bytesName, string defaultValue = null) =>
            AttrTarget(bytesName) ? AttrValueUTF8 : defaultValue;


        public bool AttrGetAsBool(int flByte, bool defaultValue = false) =>
            AttrTarget(flByte) ? AttrValueBool : defaultValue;
        public byte AttrGetAsByte(int flByte, int byteIndex, byte defaultValue = 0) =>
            AttrTarget(flByte) ? AttrValueByte(byteIndex) : defaultValue;
        public int AttrGetAsInt(int flByte, int defaultValue = 0) =>
            AttrTarget(flByte) ? AttrValueInt : defaultValue;
        public uint AttrGetAsUInt(int flByte, uint defaultValue = 0) =>
            AttrTarget(flByte) ? AttrValueUInt : defaultValue;
        public float AttrGetAsFloat(int flByte, float defaultValue = 0) =>
            AttrTarget(flByte) ? AttrValueFloat : defaultValue;
        public double AttrGetAsDouble(int flByte, double defaultValue = 0) =>
            AttrTarget(flByte) ? AttrValueDouble : defaultValue;
        public byte[] AttrGetAsBytes(int flByte, byte[] defaultValue = null) =>
            AttrTarget(flByte) ? AttrValueBytes : defaultValue;
        public float[] AttrGetAsFloatArray(int flByte, byte seperator, float[] defaultValue = null) =>
            AttrTarget(flByte) ? AttrValueFloatArray(seperator) : defaultValue;
        public string AttrGetAsASCII(int flByte, string defaultValue = null) =>
            AttrTarget(flByte) ? AttrValueASCII : defaultValue;
        public string AttrGetAsUTF8(int flByte, string defaultValue = null) =>
            AttrTarget(flByte) ? AttrValueUTF8 : defaultValue;
    }
}