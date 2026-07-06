using JHLib.Util.ArrayControl;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.ByteControl
{
    using static JHLib.Util.Helper.RefCommand;
    public class ByteSpace
    {
        private byte[] _buk;
        private int _cap;
        public int Capacity => _cap;
        public ref byte Ref0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetArrayDataReference(_buk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T RefT<T>(int byteOffset) =>
            ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(_buk), byteOffset));

        public Span<byte> ToSpan() => AsSpan(_buk);
        public ReadOnlySpan<byte> ToReadOnlySpan() => AsReadOnlySpan(_buk);
        public Memory<byte> ToMemory() => new(_buk);

        public ByteSpace() { }
        public ByteSpace(int size)
        {
            byte[] buk;
            if (size > 2048)
                buk = GC.AllocateUninitializedArray<byte>(ByteSize.Align64(size));
            else
                buk = new byte[size > 64 ? ByteSize.Align64(size) : 64];

            _buk = buk;
            _cap = buk.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int resize, int copy = 0)
        {
            if (resize > _cap)
                ResizeInternal(resize, copy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte Resize0(int resize, int copy = 0)
        {
            if (resize > _cap) ResizeInternal(resize, copy);
            return ref MemoryMarshal.GetArrayDataReference(_buk);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeInternal(int size, int copy)
        {
            var spaceOld = _buk;
            var spaceNew = GC.AllocateUninitializedArray<byte>(ByteSize.Align64(size));

            if (copy > 0 && spaceOld != null)
            {
                if (copy > spaceOld.Length)
                    copy = spaceOld.Length;

                AC.Copy(spaceOld, spaceNew, copy);
            }

            _buk = spaceNew;
            _cap = spaceNew.Length;
        }
    }
}