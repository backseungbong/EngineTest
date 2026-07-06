using JHLib.Util.ArrayControl;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.CustomStream
{
    public unsafe class FastStream : Stream
    {
        private volatile byte[] _buk;
        private volatile int _cap;
        private volatile int _len;
        private volatile int _idx;
        private volatile int _lockr;
        private volatile int _lockw;
        private volatile bool _onNewWrite;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _len;
        public override long Position { get => _idx; set => throw new NotSupportedException(); }

        public int RemainLength => _len - _idx;
        public bool OnNewWrite { get { var r = _onNewWrite; _onNewWrite = false; return r; } }

        public FastStream() : this(32) { }
        public FastStream(int capacity)
        {
            _buk = new byte[capacity];
            _cap = capacity;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadLockRelease() => _lockr = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadLock()
        {
            while (true)
            {
                if (Interlocked.CompareExchange(ref _lockr, 1, 0) == 0) return;
                Thread.Yield();
            }
        }

        public bool Peek<T>(out T result) where T : unmanaged
        {
            while (true)
            {
                if (Interlocked.CompareExchange(ref _lockr, 1, 0) == 0)
                {
                    var i = _idx;
                    if (i + sizeof(T) <= _len)
                    {
                        fixed (byte* s = &_buk[i])
                        {
                            result = *(T*)s;
                            _lockr = 0;
                            return true;
                        }
                    }
                    else
                    {
                        _lockr = 0;
                        result = default;
                        return false;
                    }
                }
                Thread.Yield();
            }
        }

        public bool Read<T>(out T result) where T : unmanaged
        {
            while (true)
            {
                if (Interlocked.CompareExchange(ref _lockr, 1, 0) == 0)
                {
                    var i = _idx;
                    if (i + sizeof(T) <= _len)
                    {
                        fixed (byte* s = &_buk[i])
                        {
                            result = *(T*)s;
                            _idx = i + sizeof(T);
                            _lockr = 0;
                            return true;
                        }
                    }
                    else
                    {
                        _lockr = 0;
                        result = default;
                        return false;
                    }
                }
                Thread.Yield();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset >= 0 && count > 0 && offset + count <= buffer.Length)
            {
                fixed (byte* d = &buffer[offset])
                {
                    while (true)
                    {
                        if (Interlocked.CompareExchange(ref _lockr, 1, 0) == 0)
                        {
                            var l = _len;
                            var i = _idx;
                            if (i + count <= l)
                            {
                                fixed (byte* s = &_buk[i])
                                {
                                    AC.Copy(s, d, count);
                                    _idx = i + count;
                                    _lockr = 0;
                                    return count;
                                }
                            }
                            else if (i < l)
                            {
                                fixed (byte* s = &_buk[i])
                                {
                                    AC.Copy(s, d, l - i);
                                    _idx = l;
                                    _lockr = 0;
                                    return l - i;
                                }
                            }
                            else
                            {
                                _lockr = 0;
                                return 0;
                            }
                        }
                        Thread.Yield();
                    }
                }
            }
            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset >= 0 && count > 0 && offset + count <= buffer.Length)
            {
                fixed (byte* s = &buffer[offset])
                {
                    while (true)
                    {
                        if (Interlocked.CompareExchange(ref _lockw, 1, 0) == 0)
                        {
                            var l = _len;
                            if (l > _idx)
                            {
                                if (l + count <= _cap)
                                {
                                    fixed (byte* d = &_buk[l])
                                    {
                                        AC.Copy(s, d, count);
                                        _len = l + count;
                                        _lockw = 0;
                                        _onNewWrite = true;
                                        return;
                                    }
                                }
                                else
                                {
                                    var p = (int)BitOperations.RoundUpToPowerOf2((uint)(l + count));
                                    var b = AC.CopyNew(_buk, p, l);
                                    fixed (byte* d = &b[l])
                                    {
                                        AC.Copy(s, d, count);
                                        _buk = b;
                                        _cap = p;
                                        _len = l + count;
                                        _lockw = 0;
                                        _onNewWrite = true;
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                _len = 0;
                                _idx = 0;

                                if (count <= _cap)
                                {
                                    fixed (byte* d = &_buk[0])
                                    {
                                        AC.Copy(s, d, count);
                                        _len = count;
                                        _lockw = 0;
                                        _onNewWrite = true;
                                        return;
                                    }
                                }
                                else
                                {
                                    var p = (int)BitOperations.RoundUpToPowerOf2((uint)count);
                                    var b = new byte[p];
                                    fixed (byte* d = &b[0])
                                    {
                                        AC.Copy(s, d, count);
                                        _buk = b;
                                        _cap = p;
                                        _len = count;
                                        _lockw = 0;
                                        _onNewWrite = true;
                                        return;
                                    }
                                }
                            }
                        }
                        Thread.Yield();
                    }
                }
            }
        }

        public void Clear()
        {
            while (Interlocked.CompareExchange(ref _lockr, 1, 0) != 0)
                Thread.Yield();

            while (Interlocked.CompareExchange(ref _lockw, 1, 0) != 0)
                Thread.Yield();

            _len = 0;
            _idx = 0;
            _lockw = 0;
            _lockr = 0;
        }
    }
}