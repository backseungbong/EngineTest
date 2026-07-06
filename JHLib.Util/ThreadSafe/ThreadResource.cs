using JHLib.Util.Helper;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.ThreadSafe
{
    /// <summary>
    /// 스레드별로 독립적인 재사용 가능한 버퍼를 제공<br/>
    /// 버퍼는 하나의 작업단위에서만 사용해야함
    /// </summary>
    public static class ThreadResource
    {
        private const uint MIN_BUFFER_SIZE = 1024; // 1kb
        private const uint MAX_BUFFER_SIZE = 1024 * 1024; // 1mb

        [ThreadStatic]
        private static BufferInfo _buffer;
        private struct BufferInfo
        {
            private byte[] _buffer;
            private uint _length;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public byte[] Get(int size) => _length <= (uint)(size - 1) ? Resize(size) : _buffer;

            [MethodImpl(MethodImplOptions.NoInlining)]
            private byte[] Resize(int size)
            {
                var newLength = (uint)ArrayHelper.Pow2ArrayLength(size, (int)MIN_BUFFER_SIZE);
                if (newLength > MAX_BUFFER_SIZE)
                    return GC.AllocateUninitializedArray<byte>((int)newLength);

                if (_length < newLength)
                {
                    _buffer = GC.AllocateUninitializedArray<byte>((int)newLength);
                    _length = newLength;
                }
                return _buffer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetBuffer0<T>(int count) => ref GetBuffer0<T>(count, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetBuffer0<T>(int count, out int capacity)
        {
            var len = count * Unsafe.SizeOf<T>();
            var buk = _buffer.Get(len);
            capacity = buk.Length / Unsafe.SizeOf<T>();
            return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(buk));
        }
    }

    public static class ThreadResource<T>
    {
        private const uint MIN_BUFFER_SIZE = 16;
        private const uint MAX_BUFFER_SIZE = 4096;

        [ThreadStatic]
        private static BufferInfo _buffer;
        private struct BufferInfo
        {
            private T[] _buffer;
            private uint _length;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[] Get(int size) => _length <= (uint)(size - 1) ? Resize(size) : _buffer;

            [MethodImpl(MethodImplOptions.NoInlining)]
            private T[] Resize(int size)
            {
                var newLength = (uint)ArrayHelper.Pow2ArrayLength(size, (int)MIN_BUFFER_SIZE);
                if (newLength > MAX_BUFFER_SIZE)
                    return GC.AllocateUninitializedArray<T>((int)newLength);

                if (_length < newLength)
                {
                    _buffer = GC.AllocateUninitializedArray<T>((int)newLength);
                    _length = newLength;
                }
                return _buffer;
            }
        }
        public static ThreadResourceList<T> CreateList(int size = 4) =>
            new ThreadResourceList<T>(size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetBuffer0(int count) => ref GetBuffer0(count, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetBuffer0(int count, out int capacity)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                var buffer = _buffer.Get(count);
                capacity = buffer.Length;
                return ref MemoryMarshal.GetArrayDataReference(buffer);
            }
            return ref ThreadResource.GetBuffer0<T>(count, out capacity);
        }
    }

    public ref struct ThreadResourceList<T>
    {
        private ref T _buk;
        private int _cap;
        private int _cnt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ThreadResourceList(int size)
        {
            _buk = ref ThreadResource<T>.GetBuffer0(size, out var cap);
            _cap = cap;
            _cnt = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref T Resize0(int size)
        {
            ref var newbuk = ref ThreadResource<T>.GetBuffer0(size, out var newcap);
            if (_cnt != 0)
            {
                var newSpan = UnsafeEx.CreateSpan(newbuk, newcap);
                var oldSpan = UnsafeEx.CreateSpan(_buk, _cnt);
                oldSpan.CopyTo(newSpan);
            }
            _buk = ref newbuk;
            _cap = newcap;
            return ref newbuk;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref T ResizeRef()
        {
            ref var newbuk = ref Resize0(_cap * 2);
            var cnt = _cnt; _cnt = cnt + 1;
            return ref Unsafe.Add(ref newbuk, cnt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddRef()
        {
            var cnt = _cnt;
            if (cnt < _cap)
            {
                _cnt = cnt + 1;
                return ref Unsafe.Add(ref _buk, cnt);
            }
            return ref ResizeRef();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsSpan() => UnsafeEx.CreateSpan(_buk, _cnt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray() => ToArray(RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray(bool clear)
        {
            if (_cnt != 0)
            {
                var array = new T[_cnt];
                var dstSpan = UnsafeEx.CreateSpan(array);
                var srcSpan = UnsafeEx.CreateSpan(_buk, _cnt);
                srcSpan.CopyTo(dstSpan);
                if (clear) srcSpan.Clear();
                return array;
            }
            return null;
        }
    }
}