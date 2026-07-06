using JHLib.Util.Hash;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.CustomStream
{
    public class SimpleWriter(bool writeHashCode = false)
    {
        private const int DEFAULT_SIZE = 4096;

        private byte[] _stream = GC.AllocateUninitializedArray<byte>(DEFAULT_SIZE);
        private int _capacity = DEFAULT_SIZE;
        private int _position = writeHashCode ? 8 : 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int addSize)
        {
            var capacity = (int)BitOperations.RoundUpToPowerOf2((uint)(_position + addSize));
            var stream = GC.AllocateUninitializedArray<byte>(capacity);
            _stream.AsSpan().CopyTo(stream.AsSpan());
            _stream = stream;
            _capacity = capacity;
        }

        public void WriteXXHash64()
        {
            var len = _position - 8;
            if (len >= 0)
            {
                ref var ref0 = ref MemoryMarshal.GetArrayDataReference(_stream);
                Unsafe.As<byte, long>(ref ref0) = XXHash.H64(ref Unsafe.AddByteOffset(ref ref0, 8), len);
            }
        }

        public void Write<T>(T data) where T : unmanaged
        {
            var pos = _position;
            if (pos + Unsafe.SizeOf<T>() > _capacity) Resize(Unsafe.SizeOf<T>());
            AsItem0<T>(pos) = data;
            _position = pos + Unsafe.SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string data) => Write(data.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T[] data) where T : unmanaged => Write(new ReadOnlySpan<T>(data));

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Write<T>(ReadOnlySpan<T> data) where T : unmanaged
        {
            var pos = _position;
            var len = data.Length;
            var add = sizeof(int) + Unsafe.SizeOf<T>() * len;
            if (pos + add > _capacity) Resize(add);

            ref var len0 = ref AsLen0(pos); len0 = len;
            if (len != 0) data.CopyTo(AsArray0<T>(ref len0));
            _position = pos + add;
        }

        public void WriteFile(string path)
        {
            var pos = _position;
            if (pos > 0)
            {
                File.WriteAllBytes(path, _stream.AsSpan(0, pos));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T AsItem0<T>(int pos) =>
           ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(_stream), pos));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int AsLen0(int pos) =>
           ref Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(_stream), pos));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<T> AsArray0<T>(ref int len0) =>
            MemoryMarshal.CreateSpan(ref Unsafe.As<int, T>(ref Unsafe.AddByteOffset(ref len0, sizeof(int))), len0);
    }
}