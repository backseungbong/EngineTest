using JHLib.Util.ByteControl;
using JHLib.Util.Helper;
using System.Runtime.CompilerServices;

namespace JHLib.Util.AIS
{
    using static System.Runtime.CompilerServices.Unsafe;
    using static System.Runtime.InteropServices.MemoryMarshal;

    /// <summary> AIS Message Decoder </summary> 
    public unsafe readonly struct AISDecoder
    {
        // 기본 바이트 할당 크기    
        private const int DEFAULT_BUFFER_SIZE = 64;

        // 최소 보장 AIS 용량
        // ulong(64bit)의 여유공간을 남겨 마지막 처리시 메모리 접근 오류 방지
        // 여유공간을 제외한 최종 비트수를 AIS 문자당 비트수(6bit)로 나누어 계산
        private const int ENSURE_AIS_CAPACITY = (DEFAULT_BUFFER_SIZE - sizeof(ulong)) * 8 / 6;

        // 최소 보장 bit 용량
        private const int ENSURE_BIT_CAPACITY = ENSURE_AIS_CAPACITY * 6;

        private readonly byte[] _bitData;
        private readonly int _bitLength;
        public readonly int BitLength => _bitLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AISDecoder(byte[] ais, byte[] buf = null)
        {
            var l = DEFAULT_BUFFER_SIZE;
            var c = ais.Length; _bitLength = c * 6;
            if (c > ENSURE_AIS_CAPACITY) l = (c * 6 + 127 & ~63) >> 3;

            var buk = buf != null && buf.Length >= l ? buf : new byte[l];
            Initialize(ais, _bitData = buk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AISDecoder(string ais, byte[] buf = null)
        {
            var l = DEFAULT_BUFFER_SIZE;
            var c = ais.Length; _bitLength = c * 6;
            if (c > ENSURE_AIS_CAPACITY) l = (c * 6 + 127 & ~63) >> 3;

            var buk = buf != null && buf.Length >= l ? buf : new byte[l];
            Initialize(ais, _bitData = buk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Initialize(byte[] ais, byte[] buk)
        {
            var c = ais.Length;
            fixed (byte* s0 = &GetArrayDataReference(ais))
            fixed (byte* d0 = &GetArrayDataReference(buk))
            fixed (byte* m0 = &GetArrayDataReference(AISDec.ASCII_TO_RBIT6))
            {
                var s = s0;
                var d = d0;

                if (c >= 8)
                {
                    var e = s0 + ((uint)c - 8);
                    do
                    {
                        *(uint*)(d + 0) =
                            (uint)m0[s[0]] << 00 |
                            (uint)m0[s[1]] << 06 |
                            (uint)m0[s[2]] << 12 |
                            (uint)m0[s[3]] << 18;
                        *(uint*)(d + 3) =
                            (uint)m0[s[4]] << 00 |
                            (uint)m0[s[5]] << 06 |
                            (uint)m0[s[6]] << 12 |
                            (uint)m0[s[7]] << 18;
                        s += 8;
                        d += 6;
                    }
                    while (s <= e);
                }

                if ((c & 4) != 0)
                {
                    *(uint*)d =
                        (uint)m0[s[0]] << 00 |
                        (uint)m0[s[1]] << 06 |
                        (uint)m0[s[2]] << 12 |
                        (uint)m0[s[3]] << 18;
                    s += 4;
                    d += 3;
                }

                if ((c & 3) != 0)
                {
                    var b = (uint)m0[s[0]] << 00;
                    if ((c & 2) != 0)
                    {
                        b |= (uint)m0[s[1]] << 06;
                        if ((c & 1) != 0)
                        {
                            b |= (uint)m0[s[2]] << 12;
                        }
                    }
                    *(uint*)d = b;
                }
                return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Initialize(string ais, byte[] buk)
        {
            var c = ais.Length;
            fixed (char* s0 = &GetReference<char>(ais))
            fixed (byte* d0 = &GetArrayDataReference(buk))
            fixed (byte* m0 = &GetArrayDataReference(AISDec.ASCII_TO_RBIT6))
            {
                var s = s0;
                var d = d0;

                if (c >= 8)
                {
                    var e = s0 + ((uint)c - 8);
                    do
                    {
                        *(uint*)(d + 0) =
                            (uint)m0[s[0]] << 00 |
                            (uint)m0[s[1]] << 06 |
                            (uint)m0[s[2]] << 12 |
                            (uint)m0[s[3]] << 18;
                        *(uint*)(d + 3) =
                            (uint)m0[s[4]] << 00 |
                            (uint)m0[s[5]] << 06 |
                            (uint)m0[s[6]] << 12 |
                            (uint)m0[s[7]] << 18;
                        s += 8;
                        d += 6;
                    }
                    while (s <= e);
                }

                if ((c & 4) != 0)
                {
                    *(uint*)d =
                        (uint)m0[s[0]] << 00 |
                        (uint)m0[s[1]] << 06 |
                        (uint)m0[s[2]] << 12 |
                        (uint)m0[s[3]] << 18;
                    s += 4;
                    d += 3;
                }

                if ((c & 3) != 0)
                {
                    var b = (uint)m0[s[0]] << 00;
                    if ((c & 2) != 0)
                    {
                        b |= (uint)m0[s[1]] << 06;
                        if ((c & 1) != 0)
                        {
                            b |= (uint)m0[s[2]] << 12;
                        }
                    }
                    *(uint*)d = b;
                }
                return;
            }
        }

        /// <summary>
        /// 비트오프셋 위치에서 uint(32bit) 형식으로 데이타를 로드 <br/>
        /// 오프셋 보정을 위해 최대 7bit가 삭제될수 있으므로 보장되는 데이타는 25bit(32bit - 7bit) <br/>
        /// 보장비트가 25bit이므로 25bit이하인 경우에 사용
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint LoadBit25(int bitOffset)
        {
            ref var buk = ref AddByteOffset(ref GetArrayDataReference(_bitData), (uint)bitOffset >> 3);
            var bit25 = As<byte, uint>(ref buk) >> (bitOffset & 7);
            return bit25;
        }
        /// <summary>
        /// 비트오프셋 위치에서 ulong(64bit) 형식으로 데이타를 로드 <br/>
        /// 오프셋 보정을 위해 최대 7비트가 삭제될수 있으므로 보장되는 데이타는 57bit(64bit - 7bit) <br/>
        /// 57bit에서 32bit만 캐스팅하여 가져온다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint LoadBit32(int bitOffset)
        {
            ref var buk = ref AddByteOffset(ref GetArrayDataReference(_bitData), (uint)bitOffset >> 3);
            var bit57 = As<byte, ulong>(ref buk) >> (bitOffset & 7);
            return (uint)bit57;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValidOffset(int bitOffset) =>
            (uint)bitOffset < ENSURE_BIT_CAPACITY || (uint)bitOffset < (uint)_bitLength;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private uint GetBit8(int bitOffset) => BitReverse.Bit8Unsafe(LoadBit25(bitOffset));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private uint GetBit32(int bitOffset) => BitReverse.Bit32(LoadBit32(bitOffset));


        /// <summary> 지정된 비트오프셋 비트를 bool(0=false, 1=true) 형식으로 가져온다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetBool(int bitOffset) => UnsafeEx.AsT<bool>(GetBit(bitOffset));

        /// <summary> 지정된 비트오프셋 비트를 byte(0 or 1) 형식으로 가져온다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetBit(int bitOffset)
        {
            if (IsValidOffset(bitOffset))
                return (byte)(LoadBit25(bitOffset) & 1);
            else
                return default;
        }

        /// <summary> 지정된 비트오프셋에서 비트길이만큼 byte형식으로 가져온다 <br/>
        /// 비트길이는 1~8값으로 제한된다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        /// <param name="bitLength">비트길이 (1 ~ 8)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(int bitOffset, int bitLength)
        {
            if (IsValidOffset(bitOffset))
            {
                if (bitLength == 1)
                    return (byte)(LoadBit25(bitOffset) & 1);
                else
                    return (byte)(GetBit8(bitOffset) >> (8 - bitLength));
            }
            return default;
        }

        /// <summary> 지정된 비트오프셋에서 비트길이만큼 ushort형식으로 가져온다 <br/>
        /// 비트길이는 1~16값으로 제한된다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        /// <param name="bitLength">비트길이 (1 ~ 16)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetUShort(int bitOffset, int bitLength)
        {
            if (IsValidOffset(bitOffset))
                return (ushort)(GetBit32(bitOffset) >> (32 - bitLength));
            else
                return default;
        }

        /// <summary> 지정된 비트오프셋에서 비트길이만큼 uint형식으로 가져온다 <br/>
        /// 비트길이는 1~32값으로 제한된다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        /// <param name="bitLength">비트길이 (1 ~ 32)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetUInt(int bitOffset, int bitLength)
        {
            if (IsValidOffset(bitOffset))
                return GetBit32(bitOffset) >> (32 - bitLength);
            else
                return default;
        }

        /// <summary> 지정된 비트오프셋에서 비트길이만큼 int형식으로 가져온다 <br/>
        /// 비트길이는 부호비트가 포함되므로 2~32값으로 제한된다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        /// <param name="bitLength">비트길이 (2 ~ 32)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInt(int bitOffset, int bitLength)
        {
            if (IsValidOffset(bitOffset))
                return (int)GetBit32(bitOffset) >> (32 - bitLength);
            else
                return default;
        }


        /// <summary> 지정된 비트오프셋에서 끝부분까지 ASCII(6bit base)형식으로 가져온다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetASCII(int bitOffset) => GetASCIICore(bitOffset, _bitLength - bitOffset);

        /// <summary> 지정된 비트오프셋에서 문자수만큼 ASCII(6bit base)형식으로 가져온다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        /// <param name="count">ASCII 문자수 (1 ~ )</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetASCII(int bitOffset, int count) => GetASCIICore(bitOffset, count * 6);


        //---- [AIS 표준은 6bit base이지만, 7bit base로 추후 확장시 사용할수 있도록 7bit base 샘플을 남겨둠]
        ///// <summary> 지정된 비트오프셋에서 끝부분까지 ASCII(7bit base)형식으로 가져온다 </summary>
        ///// <param name="bitOffset">비트오프셋</param>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public string GetASCII7(int bitOffset) => GetASCIICore(bitOffset, _len - bitOffset);

        ///// <summary> 지정된 비트오프셋에서 문자수만큼 ASCII(7bit base)형식으로 가져온다 </summary>
        ///// <param name="bitOffset">비트오프셋</param>
        ///// <param name="count">ASCII 문자수 (1 ~ )</param>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public string GetASCII7(int bitOffset, int count) => GetASCIICore(bitOffset, count * 7);


        /// <summary> 지정된 비트오프셋부터 끝부분까지 UTF16(16bit base)형식으로 가져온다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetUTF16(int bitOffset) => GetUTF16Core(bitOffset, _bitLength - bitOffset);

        /// <summary> 지정된 비트오프셋부터 문자수만큼 UTF16(16bit base)형식으로 가져온다 </summary>
        /// <param name="bitOffset">비트오프셋</param>
        /// <param name="count">UTF16 문자수 (1 ~ )</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetUTF16(int bitOffset, int count) => GetUTF16Core(bitOffset, count * 16);


        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetASCIICore(int bitOffset, int bitLength)
        {
            if (bitLength < 6 || (ulong)(uint)bitOffset + (uint)bitLength > (uint)_bitLength)
                return string.Empty;

            var i = bitOffset;
            var c = bitLength / 6;
            var r = new string(default, c);

            fixed (byte* s0 = &GetArrayDataReference(_bitData))
            fixed (char* d0 = &GetReference<char>(r))
            fixed (byte* m0 = &GetArrayDataReference(AISDec.RBIT6_TO_ASCII))
            {
                var s = s0 + ((uint)i >> 3);
                var d = d0;

                if (c >= 8)
                {
                    var e = d0 + ((uint)c - 8);
                    do
                    {
                        var b = *(ulong*)s >> (i & 7);
                        *(byte*)(d + 0) = m0[(uint)(b >> 00) & 63];
                        *(byte*)(d + 1) = m0[(uint)(b >> 06) & 63];
                        *(byte*)(d + 2) = m0[(uint)(b >> 12) & 63];
                        *(byte*)(d + 3) = m0[(uint)(b >> 18) & 63];
                        *(byte*)(d + 4) = m0[(uint)(b >> 24) & 63];
                        *(byte*)(d + 5) = m0[(uint)(b >> 30) & 63];
                        *(byte*)(d + 6) = m0[(uint)(b >> 36) & 63];
                        *(byte*)(d + 7) = m0[(uint)(b >> 42) & 63];
                        d += 8;
                        s += 6;
                    }
                    while (d <= e);
                }

                if ((c & 4) != 0)
                {
                    var b = *(uint*)s >> (i & 7);
                    *(byte*)(d + 0) = m0[(b >> 00) & 63];
                    *(byte*)(d + 1) = m0[(b >> 06) & 63];
                    *(byte*)(d + 2) = m0[(b >> 12) & 63];
                    *(byte*)(d + 3) = m0[(b >> 18) & 63];
                    d += 4;
                    s += 3;
                }

                if ((c & 3) != 0)
                {
                    var b = *(uint*)s >> (i & 7);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetUTF16Core(int bitOffset, int bitLength)
        {
            if (bitLength < 16 || (ulong)(uint)bitOffset + (uint)bitLength > (uint)_bitLength)
                return string.Empty;

            var i = bitOffset;
            var c = bitLength / 16;
            var r = new string(default, c);

            fixed (byte* s0 = &GetArrayDataReference(_bitData))
            fixed (char* d0 = &GetReference<char>(r))
            {
                var s = s0;
                var d = d0;

                if (c >= 2)
                {
                    var e = d0 + ((uint)c - 2);
                    do
                    {
                        var b = (uint)(*(ulong*)(s + ((uint)i >> 3)) >> (i & 7));
                        *(uint*)d = BitReverse.Bit16x2(b);
                        d += 2;
                        i += 32;
                    }
                    while (d <= e);
                }

                if ((c & 1) != 0)
                {
                    var b = *(uint*)(s + ((uint)i >> 3)) >> (i & 7);
                    *(ushort*)d = (ushort)BitReverse.Bit16(b);
                }
                return r;
            }
        }
    }
}