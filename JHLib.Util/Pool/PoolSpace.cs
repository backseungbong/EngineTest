using JHLib.Util.ArrayControl;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Pool
{
    using static JHLib.Util.Helper.RefCommand;
    public class PoolSpace : IDisposable
    {
        private const int MIN_SPACE = 4096;
        private static readonly byte[] Empty = [];

        private byte[] _space;
        private int _capac;
        public int Capacity => _capac;

        internal byte[] Space => _space;
        public ref byte Space0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetArrayDataReference(_space);
        }       

        public void Dispose()
        {
            var space = _space; _space = null; _capac = 0;
            if (space != Empty && space != null)
                ArrayPool<byte>.Shared.Return(space);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PoolSpace() => _space = Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PoolSpace(int size)
        {
            var space = ArrayPool<byte>.Shared.Rent(size > MIN_SPACE ? size : MIN_SPACE);
            _space = space;
            _capac = space.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeInternal(int size, int copy)
        {
            var spaceOld = _space;
            var spaceNew = ArrayPool<byte>.Shared.Rent(size > MIN_SPACE ? size : MIN_SPACE);

            if (copy > 0) AC.Copy(spaceOld, spaceNew, copy);

            _space = spaceNew;
            _capac = spaceNew.Length;

            if (spaceOld != null)
                ArrayPool<byte>.Shared.Return(spaceOld);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte EnsureSpace0(int ensureSpace, int position) =>
            ref AddBU(ref Resize0(position + ensureSpace, position), position);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int resize) { if (resize > _capac) ResizeInternal(resize, 0); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int resize, int copy) { if (resize > _capac) ResizeInternal(resize, copy); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte Resize0(int resize) => ref Resize0(resize, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte Resize0(int resize, int copy)
        {
            if (resize > _capac) ResizeInternal(resize, copy);
            return ref MemoryMarshal.GetArrayDataReference(_space);
        }
    }
}