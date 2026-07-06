using JHLib.Util.Helper;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Performance
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct ArrayPoolItem(byte[] array, int length) : IDisposable
    {
        private readonly byte[] _array = array;
        private readonly int _length = length;
        public readonly byte[] Array => _array;
        public readonly int Length => _length;
        public ref byte Byte0 => ref UnsafeEx.Arr0(_array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_array != null)
                ArrayPool<byte>.Shared.Return(_array);
        }
    }
}