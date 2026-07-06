using JHLib.Util.Hash;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.CustomStream
{
    public class SimpleReader(byte[] bytes, bool hashCodeCheck = false)
    {
        private readonly byte[] _stream = bytes;
        private readonly int _length = bytes.Length;
        private int _position = hashCodeCheck ? 8 : 0;
        public SimpleReader(string path, bool existHashSpace = false) :
            this(File.ReadAllBytes(path), existHashSpace)
        { }
        public bool CheckXXHash64()
        {
            var len = _length - 8;
            if (len >= 0)
            {
                ref var ref0 = ref MemoryMarshal.GetArrayDataReference(_stream);
                return Unsafe.As<byte, long>(ref ref0) == XXHash.H64(ref Unsafe.AddByteOffset(ref ref0, 8), len);
            }
            return false;
        }

        public bool Read<T>(out T data) where T : unmanaged
        {
            var pos = _position;
            if (pos + Unsafe.SizeOf<T>() <= _length)
            {
                _position = pos + Unsafe.SizeOf<T>();
                data = AsItem0<T>(pos);
                return true;
            }
            data = default;
            return false;
        }
        public bool Read(out string data)
        {
            if (Read(out ReadOnlySpan<char> span))
            {
                data = new string(span);
                return true;
            }
            data = default;
            return false;
        }
        public bool Read<T>(out T[] data) where T : unmanaged
        {
            if (Read(out ReadOnlySpan<T> span))
            {
                data = span.ToArray();
                return true;
            }
            data = default;
            return false;
        }
        public bool Read<T>(out ReadOnlySpan<T> span) where T : unmanaged
        {
            var pos = _position;
            if (pos + sizeof(int) <= _length)
            {
                ref var len0 = ref AsLen0(pos);
                var add = sizeof(int) + Unsafe.SizeOf<T>() * len0;
                if (len0 >= 0 && pos + add <= _length)
                {
                    _position = pos + add;
                    span = AsArray0<T>(ref len0);
                    return true;
                }
            }
            span = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T AsItem0<T>(int pos) =>
           ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(_stream), pos));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int AsLen0(int pos) =>
           ref Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(_stream), pos));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<T> AsArray0<T>(ref int len0) =>
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<int, T>(ref Unsafe.AddByteOffset(ref len0, sizeof(int))), len0);
    }

    public ref struct SimpleRefReader(byte[] bytes, bool existHashSpace = false)
    {
        private readonly ref byte _stream0 = ref MemoryMarshal.GetArrayDataReference(bytes);
        private readonly int _length = bytes.Length;
        private int _position = existHashSpace ? 8 : 0;
        public SimpleRefReader(string path, bool existHashSpace = false) :
            this(File.ReadAllBytes(path), existHashSpace)
        { }
        public bool CheckXXHash64()
        {
            var len = _length - 8;
            if (len >= 0)
            {
                ref var ref0 = ref _stream0;
                return Unsafe.As<byte, long>(ref ref0) == XXHash.H64(ref Unsafe.AddByteOffset(ref ref0, 8), len);
            }
            return false;
        }

        public bool Read<T>(out T data) where T : unmanaged
        {
            var pos = _position;
            if (pos + Unsafe.SizeOf<T>() <= _length)
            {
                _position = pos + Unsafe.SizeOf<T>();
                data = AsItem0<T>(pos);
                return true;
            }
            data = default;
            return false;
        }
        public bool Read(out string data)
        {
            if (Read(out ReadOnlySpan<char> span))
            {
                data = new string(span);
                return true;
            }
            data = default;
            return false;
        }
        public bool Read<T>(out T[] data) where T : unmanaged
        {
            if (Read(out ReadOnlySpan<T> span))
            {
                data = span.ToArray();
                return true;
            }
            data = default;
            return false;
        }
        public bool Read<T>(out ReadOnlySpan<T> span) where T : unmanaged
        {
            var pos = _position;
            if (pos + sizeof(int) <= _length)
            {
                ref var len0 = ref AsLen0(pos);
                var add = sizeof(int) + Unsafe.SizeOf<T>() * len0;
                if (len0 >= 0 && pos + add <= _length)
                {
                    _position = pos + add;
                    span = AsArray0<T>(ref len0);
                    return true;
                }
            }
            span = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUnsafe<T>(out T data) where T : unmanaged
        {
            var pos = _position; _position = pos + Unsafe.SizeOf<T>();
            data = AsItem0<T>(pos);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUnsafe(out string data)
        {
            ReadUnsafe(out ReadOnlySpan<char> span);
            data = new string(span);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUnsafe<T>(out T[] data) where T : unmanaged
        {
            ReadUnsafe(out ReadOnlySpan<T> span);
            data = span.ToArray();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ReadUnsafe<T>(out ReadOnlySpan<T> span) where T : unmanaged
        {
            var pos = _position;
            ref var len0 = ref AsLen0(pos);
            _position = pos + sizeof(int) + Unsafe.SizeOf<T>() * len0;
            span = AsArray0<T>(ref len0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref T AsItem0<T>(int pos) =>
            ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref _stream0, pos));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref int AsLen0(int pos) =>
            ref Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref _stream0, pos));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<T> AsArray0<T>(ref int len0) =>
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<int, T>(ref Unsafe.AddByteOffset(ref len0, sizeof(int))), len0);
    }
}