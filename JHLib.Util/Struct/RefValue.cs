using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    public readonly ref struct RefValue<T>
    {
        public readonly ref T Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefValue(ref T refValue) =>
            Value = ref refValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefValue(T[] buk, int index) =>
            Value = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buk), (uint)index);
    }
}