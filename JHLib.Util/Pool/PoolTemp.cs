using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Pool
{
    public struct PoolTemp<T> : IDisposable
    {
        private const int MIN_SPACE = 32;

        private T[] _space;
        private int _capac;
        public readonly ref T Ref0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetArrayDataReference(_space);
        }

        public void Dispose()
        {
            var space = _space; _space = null; _capac = 0;
            if (space != null)
                ArrayPool<T>.Shared.Return(space);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PoolTemp() { _space = null; _capac = 0; }
        public PoolTemp(int size)
        {
            var space = ArrayPool<T>.Shared.Rent(size > MIN_SPACE ? size : MIN_SPACE);
            _space = space;
            _capac = space.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureSize(int size)
        {
            if (size > _capac)
                ResizeInternal(size);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeInternal(int size)
        {
            var spaceOld = _space;
            if (spaceOld != null)
                ArrayPool<T>.Shared.Return(spaceOld);

            var spaceNew = ArrayPool<T>.Shared.Rent(size > MIN_SPACE ? size : MIN_SPACE);
            _space = spaceNew;
            _capac = spaceNew.Length;
        }
    }
}