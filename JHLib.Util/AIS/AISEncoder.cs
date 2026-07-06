using JHLib.Util.ArrayControl;
using JHLib.Util.ByteControl;
using JHLib.Util.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.AIS
{
    using static System.Runtime.CompilerServices.Unsafe;
    using static System.Runtime.InteropServices.MemoryMarshal;

    /// <summary> AIS Message Encoder </summary> 
    public unsafe sealed class AISEncoder
    {
        // 기본 바이트 할당 크기      
        private const int DEFAULT_BUFFER_SIZE = 64;

        // 최소 보장 bit 용량
        private const int DEFAULT_BIT_CAPACITY = (DEFAULT_BUFFER_SIZE - sizeof(ulong)) * 8;

        private byte[] _buk = new byte[DEFAULT_BUFFER_SIZE];
        private int _cap = DEFAULT_BIT_CAPACITY;
        private int _pos = 0;
        public int BitLength => _pos;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref byte Resize()
        {
            int cap;
            var size = _buk.Length;
            do { size *= 2; cap = (size - sizeof(ulong)) * 8; }
            while (cap < _pos);

            var buk = UnsafeEx.Resize(_buk, size);
            _buk = buk;
            _cap = cap;

            return ref GetArrayDataReference(buk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref uint ResizeRef32(int bitOffset) =>
            ref As<byte, uint>(ref Add(ref Resize(), (uint)bitOffset >> 3));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref ulong ResizeRef64(int bitOffset) =>
            ref As<byte, ulong>(ref Add(ref Resize(), (uint)bitOffset >> 3));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref uint StoreRef32(int bitOffset)
        {
            ref var buk = ref Add(ref GetArrayDataReference(_buk), (uint)bitOffset >> 3);
            return ref As<byte, uint>(ref buk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref ulong StoreRef64(int bitOffset)
        {
            ref var buk = ref Add(ref GetArrayDataReference(_buk), (uint)bitOffset >> 3);
            return ref As<byte, ulong>(ref buk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddBit1(byte value)
        {
            var i = _pos; _pos = i + 1;
            var b = (uint)(value & 1) << (i & 7);
            (i + 1 > _cap ? ref ResizeRef32(i) : ref StoreRef32(i)) |= b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddBit8(byte value, int bitLength)
        {
            var i = _pos; _pos = i + bitLength;
            var b = BitReverse.Bit8(value << (8 - bitLength)) << (i & 7);
            (i + bitLength > _cap ? ref ResizeRef32(i) : ref StoreRef32(i)) |= b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddBit16(ushort value, int bitLength)
        {
            var i = _pos; _pos = i + bitLength;
            var b = BitReverse.Bit32((uint)value << (32 - bitLength)) << (i & 7);
            (i + bitLength > _cap ? ref ResizeRef32(i) : ref StoreRef32(i)) |= b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddBit32(uint value, int bitLength)
        {
            var i = _pos; _pos = i + bitLength;
            var b = (ulong)BitReverse.Bit32(value << (32 - bitLength)) << (i & 7);
            (i + bitLength > _cap ? ref ResizeRef64(i) : ref StoreRef64(i)) |= b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidLength() =>
            throw new ArgumentOutOfRangeException($"bitLength is invalid");


        /// <summary> bool(false = 0, true = 1)값을 추가한다 </summary>
        /// <param name="value">bool 값 (false = 0, true = 1)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBool(bool value) => AddBit1(As<bool, byte>(ref value));

        /// <summary> bit(0 or 1)값을 추가한다 </summary>
        /// <param name="value">bit 값 (0 or 1)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBit(byte value) => AddBit1(value);

        /// <summary> byte 값을 특정 비트길이로 추가한다 <br/>
        /// byte는 1~8비트 길이를 가질 수 있다 </summary>
        /// <param name="value">byte 값</param>
        /// <param name="bitLength">비트길이 (1 ~ 8)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddByte(byte value, int bitLength)
        {
            if (bitLength == 1)
                AddBit1(value);
            else if ((uint)(bitLength - 1) <= 7)
                AddBit8(value, bitLength);
            else
                ThrowInvalidLength();
        }

        /// <summary> ushort 값을 특정 비트길이로 추가한다<br/>
        /// ushort는 1~16비트 길이를 가질 수 있다 </summary>
        /// <param name="value">ushort 값</param>
        /// <param name="bitLength">비트길이 (1 ~ 16)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUshort(ushort value, int bitLength)
        {
            if ((uint)(bitLength - 1) <= 15)
                AddBit16(value, bitLength);
            else
                ThrowInvalidLength();
        }

        /// <summary> uint 값을 특정 비트길이로 추가한다<br/>
        /// uint는 1~32비트 길이를 가질 수 있다 </summary>
        /// <param name="value">uint 값</param>
        /// <param name="bitLength">비트길이 (1 ~ 32)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUInt(uint value, int bitLength)
        {
            if ((uint)(bitLength - 1) <= 31)
                AddBit32(value, bitLength);
            else
                ThrowInvalidLength();
        }

        /// <summary> int 값을 특정 비트길이로 추가한다<br/>
        /// int는 부호비트가 포함되므로 2~32비트 길이를 가질 수 있다 </summary>
        /// <param name="value">int 값</param>
        /// <param name="bitLength">비트길이 (2 ~ 32)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddInt(int value, int bitLength)
        {
            if ((uint)(bitLength - 2) <= 30)
                AddBit32((uint)value, bitLength);
            else
                ThrowInvalidLength();
        }


        /// <summary> AIS(6bit base) 문자열을 추가한다 (추가되는 비트길이 : 문자길이 * 6bit)</summary>
        /// <param name="ais">AIS(6bit base) 문자열</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddASCII(string ais) { if (ais != null) AddASCII(ais, ais.Length); }

        /// <summary> AIS(6bit base) 문자열을 추가한다 (추가되는 비트길이 : count * 6bit)<br/>
        /// 문자열은 count만큼만 추가하고, count가 더 크다면 남은 부분은 빈값으로 채운다 </summary>
        /// <param name="ais">AIS(6bit base) 문자열</param>
        /// <param name="count">문자길이</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddASCII(string ais, int count)
        {
            if (count <= 0)
                return;

            var i = _pos; _pos = i + count * 6;
            if (i + count * 6 > _cap) Resize();

            if (ais == null || ais.Length == 0)
                return;

            fixed (char* s0 = &GetReference<char>(ais))
            fixed (byte* d0 = &GetArrayDataReference(_buk))
            fixed (byte* m0 = &GetArrayDataReference(AISEnc.ASCII_TO_RBIT6))
            {
                var s = s0;
                var d = d0 + ((uint)i >> 3);
                var r = *(uint*)d;

                var c = ais.Length < count ? ais.Length : count;
                if (c >= 8)
                {
                    var e = s0 + ((uint)c - 8);
                    do
                    {
                        var b =
                            (uint)m0[*(byte*)(s + 0)] << 00 |
                            (uint)m0[*(byte*)(s + 1)] << 06 |
                            (uint)m0[*(byte*)(s + 2)] << 12 |
                            (uint)m0[*(byte*)(s + 3)] << 18 |
                            (uint)m0[*(byte*)(s + 4)] << 24 |
                            (ulong)m0[*(byte*)(s + 5)] << 30 |
                            (ulong)m0[*(byte*)(s + 6)] << 36 |
                            (ulong)m0[*(byte*)(s + 7)] << 42; b <<= i & 7;
                        *(ulong*)d = b | r; r = (uint)(b >> 48);
                        s += 8;
                        d += 6;
                    }
                    while (s <= e);
                }

                if ((c & 4) != 0)
                {
                    var b =
                        (uint)m0[*(byte*)(s + 0)] << 00 |
                        (uint)m0[*(byte*)(s + 1)] << 06 |
                        (uint)m0[*(byte*)(s + 2)] << 12 |
                        (uint)m0[*(byte*)(s + 3)] << 18; b <<= i & 7;
                    *(uint*)d = b | r; r = b >> 24;
                    s += 4;
                    d += 3;
                }

                if ((c & 3) != 0)
                {
                    var b = (uint)m0[*(byte*)(s + 0)] << 00;
                    if ((c & 2) != 0)
                    {
                        b |= (uint)m0[*(byte*)(s + 1)] << 06;
                        if ((c & 1) != 0)
                        {
                            b |= (uint)m0[*(byte*)(s + 2)] << 12;
                        }
                    }
                    *(uint*)d = (b << (i & 7)) | r;
                }
                return;
            }
        }


        /// <summary> UTF16(16bit base) 문자열을 추가한다 (추가되는 비트길이 : 문자길이 * 16bit)</summary>
        /// <param name="utf16">UTF16(16bit base) 문자열</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUTF16(string utf16) { if (utf16 != null) AddUTF16(utf16, utf16.Length); }

        /// <summary> UTF16(16bit base) 문자열을 추가한다 (추가되는 비트길이 : count * 16bit) <br/>
        /// 문자열은 count만큼만 추가하고, count가 더 크다면 남은 부분은 빈값으로 채운다 </summary>
        /// <param name="utf16">UTF16(16bit base) 문자열</param>
        /// <param name="count">문자길이</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddUTF16(string utf16, int count)
        {
            if (count <= 0)
                return;

            var i = _pos; _pos = i + count * 16;
            if (i + count * 16 > _cap) Resize();

            if (utf16 == null || utf16.Length == 0)
                return;

            fixed (char* s0 = &GetReference<char>(utf16))
            fixed (byte* d0 = &GetArrayDataReference(_buk))
            {
                var s = s0;
                var d = d0;

                var c = utf16.Length < count ? utf16.Length : count;
                if (c >= 2)
                {
                    var e = s0 + ((uint)c - 2);
                    do
                    {
                        var b = BitReverse.Bit16x2(*(uint*)s);
                        *(ulong*)(d + ((uint)i >> 3)) |= (ulong)b << (i & 7);
                        s += 2;
                        i += 32;
                    }
                    while (s <= e);
                }

                if ((c & 1) != 0)
                {
                    var b = BitReverse.Bit16(*s);
                    *(uint*)(d + ((uint)i >> 3)) |= b << (i & 7);
                }
                return;
            }
        }

        /// <summary> byte배열 타입으로 인코딩하여 출력 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public byte[] ToEncodedBytes()
        {
            var c = (_pos + 5) / 6;
            if (c == 0) { return []; }

            var r = new byte[c];
            fixed (byte* s0 = &GetArrayDataReference(_buk))
            fixed (byte* d0 = &GetArrayDataReference(r))
            fixed (byte* m0 = &GetArrayDataReference(AISEnc.RBIT6_TO_ASCII))
            {
                var s = s0;
                var d = d0;

                if (c >= 8)
                {
                    var e = d0 + ((uint)c - 8);
                    do
                    {
                        var b = *(uint*)(s + 0);
                        d[0] = m0[(b >> 00) & 63];
                        d[1] = m0[(b >> 06) & 63];
                        d[2] = m0[(b >> 12) & 63];
                        d[3] = m0[(b >> 18) & 63];
                        b = *(uint*)(s + 3);
                        d[4] = m0[(b >> 00) & 63];
                        d[5] = m0[(b >> 06) & 63];
                        d[6] = m0[(b >> 12) & 63];
                        d[7] = m0[(b >> 18) & 63];
                        d += 8;
                        s += 6;
                    }
                    while (d <= e);
                }

                if ((c & 4) != 0)
                {
                    var b = *(uint*)s;
                    d[0] = m0[(b >> 00) & 63];
                    d[1] = m0[(b >> 06) & 63];
                    d[2] = m0[(b >> 12) & 63];
                    d[3] = m0[(b >> 18) & 63];
                    d += 4;
                    s += 3;
                }

                if ((c & 3) != 0)
                {
                    var b = *(uint*)s;
                    d[0] = m0[(b >> 00) & 63];
                    if ((c & 2) != 0)
                    {
                        d[1] = m0[(b >> 06) & 63];
                        if ((c & 1) != 0)
                        {
                            d[2] = m0[(b >> 12) & 63];
                        }
                    }
                }
                return r;
            }
        }

        /// <summary> string 타입으로 인코딩하여 출력 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string ToEncodedString()
        {
            var c = (_pos + 5) / 6;
            if (c == 0) { return string.Empty; }

            var r = new string(default, c);
            fixed (byte* s0 = &GetArrayDataReference(_buk))
            fixed (char* d0 = &GetReference<char>(r))
            fixed (byte* m0 = &GetArrayDataReference(AISEnc.RBIT6_TO_ASCII))
            {
                var s = s0;
                var d = d0;

                if (c >= 8)
                {
                    var e = d0 + ((uint)c - 8);
                    do
                    {
                        var b = *(uint*)(s + 0);
                        *(byte*)(d + 0) = m0[(b >> 00) & 63];
                        *(byte*)(d + 1) = m0[(b >> 06) & 63];
                        *(byte*)(d + 2) = m0[(b >> 12) & 63];
                        *(byte*)(d + 3) = m0[(b >> 18) & 63];
                        b = *(uint*)(s + 3);
                        *(byte*)(d + 4) = m0[(b >> 00) & 63];
                        *(byte*)(d + 5) = m0[(b >> 06) & 63];
                        *(byte*)(d + 6) = m0[(b >> 12) & 63];
                        *(byte*)(d + 7) = m0[(b >> 18) & 63];
                        d += 8;
                        s += 6;
                    }
                    while (d <= e);
                }

                if ((c & 4) != 0)
                {
                    var b = *(uint*)s;
                    *(byte*)(d + 0) = m0[(b >> 00) & 63];
                    *(byte*)(d + 1) = m0[(b >> 06) & 63];
                    *(byte*)(d + 2) = m0[(b >> 12) & 63];
                    *(byte*)(d + 3) = m0[(b >> 18) & 63];
                    d += 4;
                    s += 3;
                }

                if ((c & 3) != 0)
                {
                    var b = *(uint*)s;
                    *(byte*)(d + 0) = m0[(b >> 00) & 63];
                    if ((c & 2) != 0)
                    {
                        *(byte*)(d + 1) = m0[(b >> 06) & 63];
                        if ((c & 1) != 0)
                        {
                            *(byte*)(d + 2) = m0[(b >> 12) & 63];
                        }
                    }
                }
                return r;
            }
        }

        /// <summary> 누적된 AIS 데이타를 모두 삭제하고 초기화한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Clear(bool zeroFill = true)
        {
            var pos = _pos; _pos = 0;
            if (pos != 0 && zeroFill)
                AC.ZeroFill(_buk, pos + 7 >> 3);
        }
    }
}