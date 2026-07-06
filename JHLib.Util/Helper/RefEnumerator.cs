using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Helper
{
    /// <summary>
    /// 연속적인 배열이나 리스트에 대해서 반복기를 생성한다 <para/>
    /// 포인터를 통해 가능한한 가장 빠르게 아이템을 반복한다 (레퍼런스 반복기보다 성능과 단순함을 위해 작성) <para/>
    /// 반복기는 스택 메모리의 사용을 강제하기 위해 ref struct 형태로 생성된다
    /// </summary>
    public struct Etor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefEnumerator<T> New<T>(T[] array) =>
            array != null ? new(ref MemoryMarshal.GetArrayDataReference(array), (uint)array.Length) : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefEnumerator<T> New<T>(T[] array, nint count) =>
            array != null ? new(ref MemoryMarshal.GetArrayDataReference(array), count) : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefEnumerator<T> NewUnsafe<T>(T[] array) =>
            new(ref MemoryMarshal.GetArrayDataReference(array), (uint)array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefEnumerator<T> NewUnsafe<T>(T[] array, nint count) =>
            new(ref MemoryMarshal.GetArrayDataReference(array), count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefEnumerator<T> New<T>(List<T> list) => new(CollectionsMarshal.AsSpan(list));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefEnumerator<T> New<T>(ReadOnlySpan<T> span) => new(span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefEnumerator<T> New<T>(ref T ptr, int count) => new(ref ptr, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefEnumerator<T> New<T>(in T item) => new(ref Unsafe.AsRef(in item), 1);
    }

    public ref struct RefEnumerator<T>
    {
        private ref T p;
        private readonly ref T e;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator(ref T p0, uint count) { p = ref p0; e = ref Unsafe.Add(ref p0, count); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator(ref T p0, nint count) { p = ref p0; e = ref Unsafe.Add(ref p0, count); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator(ReadOnlySpan<T> span)
        {
            p = ref MemoryMarshal.GetReference(span);
            e = ref Unsafe.Add(ref p, span.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly RefEnumerator<T> GetEnumerator() => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => Unsafe.IsAddressLessThan(ref p, ref e);
        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Subtract(ref p = ref Unsafe.Add(ref p, 1), 1);
        }
    }
}