using JHLib.Util.ArrayControl;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.ByteControl
{
    public interface IResizableStream
    {
        ref byte Stream0 { get; }
        int Capacity { get; }
        int Position { get; set; }
        void EnsureFreeSpace(int freespace);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> WriteSpan()
        {
            var p = Position;
            return MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Stream0, p), Capacity - p);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> ReadSpan()
        {
            return MemoryMarshal.CreateSpan(ref Stream0, Position);
        }
    }

    public class ResizableArray : IResizableStream
    {
        private byte[] _buk;
        private int _cap;
        private int _pos;

        public ref byte Stream0 => ref MemoryMarshal.GetArrayDataReference(_buk);
        public int Capacity => _cap;
        public int Position { get => _pos; set => _pos = value; }
        public void EnsureFreeSpace(int freespace)
        {
            var resize = _pos + freespace;
            if (resize > _cap) Resize(resize);
        }

        public ResizableArray() => _buk = [];
        public ResizableArray(int initSize)
        {
            _buk = GC.AllocateUninitializedArray<byte>(initSize);
            _cap = initSize;
        }

        private void Resize(int resize)
        {
            var cap = (int)BitOperations.RoundUpToPowerOf2((uint)resize);
            var buk = GC.AllocateUninitializedArray<byte>(cap);
            var pos = _pos;
            if (pos != 0) AC.Copy(_buk, buk, pos);
            _buk = buk;
            _cap = cap;
        }
    }
}