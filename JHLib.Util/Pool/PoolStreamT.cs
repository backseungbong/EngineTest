using JHLib.Util.ArrayControl;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Pool
{
    using static JHLib.Util.Helper.RefCommand;
    public unsafe class PoolStream<T>(int capacity) : IDisposable where T : unmanaged
    {
        private readonly PoolSpace _pool = new(capacity * sizeof(T));
        private int _pos;
        public int Count => _pos / sizeof(T);
        public ref T Space0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref AsT<T>(ref _pool.Space0);
        }
        public T First
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsT<T>(ref _pool.Space0);
        }
        public T Last
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsT<T>(ref _pool.Space0, _pos - sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _pool.Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Occupy0(int count)
        {
            var pos = _pos;
            var space = count * sizeof(T);
            _pos = pos + space;

            return ref AsT<T>(ref _pool.EnsureSpace0(space, pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddRef()
        {
            var pos = _pos; _pos = pos + sizeof(T);
            return ref AsT<T>(ref _pool.EnsureSpace0(sizeof(T), pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            var pos = _pos; _pos = pos + sizeof(T);
            AsT<T>(ref _pool.EnsureSpace0(sizeof(T), pos)) = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _pos = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            var count = _pos / Unsafe.SizeOf<T>();
            var array = AC.UninitializedArray<T>(count);
            AC.Copy(ref _pool.Space0, ref RefB(array), count * Unsafe.SizeOf<T>());
            return array;
        }
    }
}