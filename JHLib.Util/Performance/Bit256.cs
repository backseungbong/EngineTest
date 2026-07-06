using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Performance
{
    [StructLayout(LayoutKind.Sequential, Size = BYTESIZE)]
    public struct Bit256()
    {
        private const int BYTESIZE = 32;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref uint Ref32(int idx)
        {
            return ref Unsafe.Add(
                ref Unsafe.As<Bit256, uint>(ref Unsafe.AsRef(in this)), (uint)idx);
        }

        public bool this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)i >= BYTESIZE * 8)
                    return false;

                var val = Ref32(i >> 5) >> (i & 31);
                return (val & 1) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)i >= BYTESIZE * 8)
                    return;

                var bit = 1u << (i & 31);
                if (value)
                    Ref32(i >> 5) |= bit;
                else
                    Ref32(i >> 5) &= ~bit;
            }
        }
    }
}
