using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JHLib.Util.CustomStream
{
    public unsafe class SocketStream
    {
        private const int SIZE_MIN = 4096; // 4KB
        private const int SIZE_MAX = 1048576; // 1MB

        private byte[] _buk;
        private int _cap;

        private volatile int _wPos;
        private volatile int _rPos;
        private volatile bool _isClosed;

        public bool IsClosed => _isClosed;
        public int NextByteLength => _wPos - _rPos;
        public SocketStream()
        {
            _buk = new byte[SIZE_MIN];
            _cap = SIZE_MIN;
            _wPos = 0;
            _rPos = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string NextASCII(int length)
        {
            if (length > 0)
            {
                var w = _wPos;
                var r = _rPos;
                if (r + length <= w)
                {
                    var v = new string(default, length);
                    fixed (char* d0 = v)
                    fixed (byte* s0 = &_buk[r])
                    {
                        var d = (byte*)d0;
                        if (length <= 4)
                        {
                            d[0] = s0[0];
                            if (length > 2)
                            {
                                d[2] = s0[1];
                                d[4] = s0[2];
                            }
                            d[length * 2 - 2] = s0[length - 1];
                        }
                        else
                        {
                            var s = s0;
                            var e = s + (length - 4);
                            do
                            {
                                d[0] = s[0];
                                d[2] = s[1];
                                d[4] = s[2];
                                d[6] = s[3]; d += 8;
                            }
                            while ((s += 4) < e);

                            d = (byte*)d0 + (length * 2 - 8);
                            d[0] = e[0];
                            d[2] = e[1];
                            d[4] = e[2];
                            d[6] = e[3];
                        }
                        _rPos = r + length;
                        return v;
                    }
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string PeekASCII(uint offset, int length)
        {
            if (length > 0)
            {
                var w = _wPos;
                var r = _rPos + (int)offset;
                if (r + length <= w)
                {
                    var v = new string(default, length);
                    fixed (char* d0 = v)
                    fixed (byte* s0 = &_buk[r])
                    {
                        var d = (byte*)d0;
                        if (length <= 4)
                        {
                            d[0] = s0[0];
                            if (length > 2)
                            {
                                d[2] = s0[1];
                                d[4] = s0[2];
                            }
                            d[length * 2 - 2] = s0[length - 1];
                        }
                        else
                        {
                            var s = s0;
                            var e = s + (length - 4);
                            do
                            {
                                d[0] = s[0];
                                d[2] = s[1];
                                d[4] = s[2];
                                d[6] = s[3]; d += 8;
                            }
                            while ((s += 4) < e);

                            d = (byte*)d0 + (length * 2 - 8);
                            d[0] = e[0];
                            d[2] = e[1];
                            d[4] = e[2];
                            d[6] = e[3];
                        }
                        return v;
                    }
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string NextUTF8(int length)
        {
            if (length > 0)
            {
                var w = _wPos;
                var r = _rPos;
                var l = r + length;
                if (l <= w)
                {
                    var v = Encoding.UTF8.GetString(_buk, r, length);
                    _rPos = l;
                    return v;
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string PeekUTF8(uint offset, int length)
        {
            if (length > 0)
            {
                var w = _wPos;
                var r = _rPos + (int)offset;
                var l = r + length;
                if (l <= w)
                {
                    var v = Encoding.UTF8.GetString(_buk, r, length);
                    return v;
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public byte[] NextBytes(int length)
        {
            if (length > 0)
            {
                var w = _wPos;
                var r = _rPos;
                var l = r + length;
                if (l <= w)
                {
                    var result = new byte[length];
                    fixed (byte* d = &result[0])
                    fixed (byte* s = &_buk[r])
                    {
                        MemCopy(s, d, length);
                        _rPos = l;
                        return result;
                    }
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public byte[] PeekBytes(uint offset, int length)
        {
            if (length > 0)
            {
                var w = _wPos;
                var r = _rPos + (int)offset;
                var l = r + length;
                if (l <= w)
                {
                    var result = new byte[length];
                    fixed (byte* d = &result[0])
                    fixed (byte* s = &_buk[r])
                    {
                        MemCopy(s, d, length);
                        return result;
                    }
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool NextT<T>(out T result) where T : unmanaged
        {
            var w = _wPos;
            var r = _rPos;
            if (r + sizeof(T) <= w)
            {
                fixed (byte* p = &_buk[r])
                    result = *(T*)p;
                _rPos = r + sizeof(T);
                return true;
            }
            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool PeekT<T>(uint offset, out T result) where T : unmanaged
        {
            var w = _wPos;
            var r = _rPos + (int)offset;
            if (r + sizeof(T) <= w)
            {
                fixed (byte* p = &_buk[r])
                    result = *(T*)p;
                return true;
            }
            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool NextASCIIToInt(int length, out int result)
        {
            if (length > 0)
            {
                var w = _wPos;
                var r = _rPos;
                var l = r + length;
                if (l <= w)
                {
                    var b = _buk;
                    var v = 0;
                    do
                    {
                        var c = b[r];
                        if ('0' <= c && c <= '9')
                        {
                            v = c - '0' + v * 10;
                        }
                        else
                        {
                            result = 0;
                            return false;
                        }
                    }
                    while (++r < l);
                    result = v;
                    _rPos = l;
                    return true;
                }
            }
            result = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool PeekASCIIToInt(uint offset, int length, out int result)
        {
            if (length > 0)
            {
                var w = _wPos;
                var r = _rPos + (int)offset;
                var l = r + length;
                if (l <= w)
                {
                    var b = _buk;
                    var v = 0;
                    do
                    {
                        var c = b[r];
                        if ('0' <= c && c <= '9')
                        {
                            v = c - '0' + v * 10;
                        }
                        else
                        {
                            result = 0;
                            return false;
                        }
                    }
                    while (++r < l);
                    result = v;
                    return true;
                }
            }
            result = 0;
            return false;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool SearchByteAndMovePosition(byte searchByte, int moveAmount, bool ifFailsSetToLastPos = true)
        {
            var w = _wPos;
            var r = _rPos;
            if (r < w)
            {
                var t = r;
                var b = _buk;
                do
                {
                    if (searchByte == b[t])
                    {
                        var m = t + moveAmount;
                        if (r <= m && m <= w)
                        {
                            _rPos = m;
                            return true;
                        }
                    }
                }
                while (++t < w);
            }

            if (ifFailsSetToLastPos)
                _rPos = w;

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool SearchByteAndSetPosition(byte searchByte, bool ifFailsSetToLastPos = true)
        {
            var w = _wPos;
            var r = _rPos;
            if (r < w)
            {
                var b = _buk;
                do
                {
                    if (b[r] == searchByte)
                    {
                        _rPos = r;
                        return true;
                    }
                }
                while (++r < w);
            }

            if (ifFailsSetToLastPos)
                _rPos = w;

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool SearchByteAndSetPosition(byte searchByteMin, byte searchByteMax, bool ifFailsSetToLastPos = true)
        {
            var w = _wPos;
            var r = _rPos;
            if (r < w)
            {
                var b = _buk;
                do
                {
                    var t = b[r];
                    if (searchByteMin <= t && t <= searchByteMax)
                    {
                        _rPos = r;
                        return true;
                    }
                }
                while (++r < w);
            }

            if (ifFailsSetToLastPos)
                _rPos = w;

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool NextPosition(uint moveAmount)
        {
            var w = _wPos;
            var r = _rPos + (int)moveAmount;
            if (r <= w)
            {
                _rPos = r;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Push(byte[] buffer, int length)
        {
            if (length > 0 && length <= SIZE_MAX)
            {
                var w = _wPos;
                if (w <= _rPos)
                {
                    _wPos = 0;
                    _rPos = 0;
                    w = 0;
                }

                if (w + length <= _cap)
                {
                    fixed (byte* s = &buffer[0])
                    fixed (byte* d = &_buk[0])
                    {
                        MemCopy(s, d + w, length);
                        _wPos = w + length;
                        return;
                    }
                }
                else
                {
                    var n = w + length - 1;
                    n |= n >> 1;
                    n |= n >> 2;
                    n |= n >> 4;
                    n |= n >> 8;
                    n |= n >> 16;
                    n += 1;

                    if (n > SIZE_MAX)
                    {
                        _wPos = 0;
                        _rPos = 0;

                        if (_cap < SIZE_MAX)
                        {
                            _buk = new byte[SIZE_MAX];
                            _cap = SIZE_MAX;
                        }

                        fixed (byte* b = &buffer[0])
                        fixed (byte* d = &_buk[0])
                        {
                            MemCopy(b, d, length);
                            _wPos = length;
                            return;
                        }
                    }
                    else
                    {
                        var buk = new byte[n];
                        fixed (byte* s = &_buk[0])
                        fixed (byte* b = &buffer[0])
                        fixed (byte* d = &buk[0])
                        {
                            if (w > 0) MemCopy(s, d, w);
                            MemCopy(b, d + w, length);
                            _buk = buk;
                            _cap = n;
                            _wPos = w + length;
                            return;
                        }
                    }
                }
            }
        }

        public void Close() => _isClosed = true;


        [StructLayout(LayoutKind.Sequential, Size = 16)] public readonly struct B16 { }
        [StructLayout(LayoutKind.Sequential, Size = 32)] public readonly struct B32 { }
        [StructLayout(LayoutKind.Sequential, Size = 64)] public readonly struct B64 { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MemCopy(byte* s, byte* d, int l)
        {
            if (l <= 64)
                if (l > 2)
                    if (l > 4)
                        if (l > 8)
                            if (l > 16)
                                if (l > 32)
                                {
                                    *(B32*)(d + 0) = *(B32*)(s + 0);
                                    *(B32*)(d + (l - 32)) = *(B32*)(s + (l - 32));
                                }
                                else
                                {
                                    *(B16*)(d + 0) = *(B16*)(s + 0);
                                    *(B16*)(d + (l - 16)) = *(B16*)(s + (l - 16));
                                }
                            else
                            {
                                *(long*)(d + 0) = *(long*)(s + 0);
                                *(long*)(d + (l - 8)) = *(long*)(s + (l - 8));
                            }
                        else
                        {
                            *(int*)(d + 0) = *(int*)(s + 0);
                            *(int*)(d + (l - 4)) = *(int*)(s + (l - 4));
                        }
                    else
                    {
                        *(short*)(d + 0) = *(short*)(s + 0);
                        *(short*)(d + (l - 2)) = *(short*)(s + (l - 2));
                    }
                else
                {
                    *(d + 0) = *(s + 0);
                    *(d + (l - 1)) = *(s + (l - 1));
                }
            else if (l <= 2048)
            {
                var a = s;
                var b = d; d += l - 64;
                do { *(B64*)b = *(B64*)a; a += 64; }
                while ((b += 64) < d);
                *(B64*)d = *(B64*)(s + (l - 64));
            }
            else
            {
                NativeMemory.Copy(s, d, (uint)l);
            }
        }
    }
}