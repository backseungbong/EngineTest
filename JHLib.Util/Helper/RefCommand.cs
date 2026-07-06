using JHLib.Util.ArrayControl;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Helper
{
    using static System.Runtime.CompilerServices.Unsafe;
    using static System.Runtime.InteropServices.MemoryMarshal;

    /// <summary>C#에서 관리형 포인터를 고성능으로 직접 다루기 위한 클래스</summary>
    public static class RefCommand
    {
        /// <summary>0번째 인덱스 참조 주소를 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefB(byte[] array) => ref GetArrayDataReference(array);

        /// <summary>지정된 바이트오프셋 참조 주소를 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefB(byte[] array, int byteOffset) => ref AddByteOffset(ref GetArrayDataReference(array), byteOffset);

        /// <summary>지정된 바이트오프셋 참조 주소를 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefB(byte[] array, uint byteOffset) => ref AddByteOffset(ref GetArrayDataReference(array), byteOffset);

        /// <summary>지정된 바이트오프셋 참조 주소를 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefBU(byte[] array, int byteOffset) => ref AddByteOffset(ref GetArrayDataReference(array), (uint)byteOffset);



        /// <summary>0번째 인덱스 참조 주소를 T 타입으로 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RefT<T>(byte[] array) => ref As<byte, T>(ref GetArrayDataReference(array));

        /// <summary>지정된 바이트오프셋 참조 주소를 T 타입으로 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RefT<T>(byte[] array, int byteOffset) => ref As<byte, T>(ref AddByteOffset(ref GetArrayDataReference(array), byteOffset));

        /// <summary>지정된 바이트오프셋 참조 주소를 T 타입으로 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RefT<T>(byte[] array, uint byteOffset) => ref As<byte, T>(ref AddByteOffset(ref GetArrayDataReference(array), byteOffset));

        /// <summary>지정된 바이트오프셋 참조 주소를 T 타입으로 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RefTU<T>(byte[] array, int byteOffset) => ref As<byte, T>(ref AddByteOffset(ref GetArrayDataReference(array), (uint)byteOffset));



        /// <summary>0번째 인덱스 참조 주소를 byte 타입으로 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefB(string str) => ref As<char, byte>(ref GetReference<char>(str));

        /// <summary>0번째 인덱스 참조 주소를 byte 타입으로 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefB<T>(T[] array) => ref As<T, byte>(ref GetArrayDataReference(array));

        /// <summary>지정된 인덱스 참조 주소를 byte 타입으로 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefB<T>(T[] array, int index) => ref As<T, byte>(ref Add(ref GetArrayDataReference(array), index));

        /// <summary>지정된 인덱스 참조 주소를 byte 타입으로 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefB<T>(T[] array, uint index) => ref As<T, byte>(ref Add(ref GetArrayDataReference(array), index));

        /// <summary>지정된 인덱스 참조 주소를 byte 타입으로 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte RefBU<T>(T[] array, int index) => ref As<T, byte>(ref Add(ref GetArrayDataReference(array), (uint)index));



        /// <summary>0번째 인덱스 참조 주소를 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref char RefT(string str) => ref GetReference<char>(str);

        /// <summary>0번째 인덱스 참조 주소를 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RefT<T>(T[] array) => ref GetArrayDataReference(array);

        /// <summary>지정된 인덱스 참조 주소를 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RefT<T>(T[] array, int index) => ref Add(ref GetArrayDataReference(array), index);

        /// <summary>지정된 인덱스 참조 주소를 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RefT<T>(T[] array, uint index) => ref Add(ref GetArrayDataReference(array), index);

        /// <summary>지정된 인덱스 참조 주소를 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T RefTU<T>(T[] array, int index) => ref Add(ref GetArrayDataReference(array), (uint)index);



        /// <summary>참조 주소에 지정된 바이트오프셋을 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddB<T>(ref T ptr, int byteOffset) => ref AddByteOffset(ref ptr, byteOffset);

        /// <summary>참조 주소에 지정된 바이트오프셋을 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddB<T>(ref T ptr, uint byteOffset) => ref AddByteOffset(ref ptr, byteOffset);

        /// <summary>참조 주소에 지정된 바이트오프셋을 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddB<T>(ref T ptr, nint byteOffset) => ref AddByteOffset(ref ptr, byteOffset);

        /// <summary>참조 주소에 지정된 바이트오프셋을 더해 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddBU<T>(ref T ptr, int byteOffset) => ref AddByteOffset(ref ptr, (uint)byteOffset);



        /// <summary>참조 주소에 지정된 인덱스를 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddT<T>(ref T ptr, int index) => ref Add(ref ptr, index);

        /// <summary>참조 주소에 지정된 인덱스를 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddT<T>(ref T ptr, uint index) => ref Add(ref ptr, index);

        /// <summary>참조 주소에 지정된 인덱스를 더해 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddTU<T>(ref T ptr, int index) => ref Add(ref ptr, (uint)index);



        /// <summary>참조 주소에 지정된 바이트오프셋을 빼서 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubB<T>(ref T ptr, int byteOffset) => ref SubtractByteOffset(ref ptr, byteOffset);

        /// <summary>참조 주소에 지정된 바이트오프셋을 빼서 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubB<T>(ref T ptr, uint byteOffset) => ref SubtractByteOffset(ref ptr, byteOffset);

        /// <summary>참조 주소에 지정된 바이트오프셋을 빼서 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubBU<T>(ref T ptr, int byteOffset) => ref SubtractByteOffset(ref ptr, (uint)byteOffset);



        /// <summary>참조 주소에 지정된 인덱스를 빼서 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubT<T>(ref T ptr, int index) => ref Subtract(ref ptr, index);

        /// <summary>참조 주소에 지정된 인덱스를 빼서 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubT<T>(ref T ptr, uint index) => ref Subtract(ref ptr, index);

        /// <summary>참조 주소에 지정된 인덱스를 빼서 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubTU<T>(ref T ptr, int index) => ref Subtract(ref ptr, (uint)index);



        /// <summary>참조 주소에 1바이트오프셋 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte Add1(ref byte ptr) => ref AddByteOffset(ref ptr, 1);

        /// <summary>참조 주소에 1바이트오프셋 빼서 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte Sub1(ref byte ptr) => ref SubtractByteOffset(ref ptr, 1);

        /// <summary>참조 주소에 1인덱스를 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add1<T>(ref T ptr) => ref Add(ref ptr, 1);

        /// <summary>참조 주소에 1인덱스를 빼서 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Sub1<T>(ref T ptr) => ref Subtract(ref ptr, 1);



        /// <summary>참조 주소를 byte 타입으로 변환하여 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsB<T>(ref T ptr) => ref As<T, byte>(ref ptr);

        /// <summary>참조 주소를 byte 타입으로 변환하고 지정된 바이트오프셋을 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsB<T>(ref T ptr, int byteOffset) => ref AddByteOffset(ref As<T, byte>(ref ptr), byteOffset);

        /// <summary>참조 주소를 byte 타입으로 변환하고 지정된 바이트오프셋을 더해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsB<T>(ref T ptr, uint byteOffset) => ref AddByteOffset(ref As<T, byte>(ref ptr), byteOffset);

        /// <summary>참조 주소를 byte 타입으로 변환하고 지정된 바이트오프셋을 더해 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsBU<T>(ref T ptr, int byteOffset) => ref AddByteOffset(ref As<T, byte>(ref ptr), (uint)byteOffset);



        /// <summary>참조 주소를 T타입으로 변환하여 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(ref byte ptr) => ref As<byte, T>(ref ptr);

        /// <summary>참조 주소에 지정된 바이트오프셋을 더한 뒤 T타입으로 변환하여 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(ref byte ptr, int byteOffset) => ref As<byte, T>(ref AddByteOffset(ref ptr, byteOffset));

        /// <summary>참조 주소에 지정된 바이트오프셋을 더한 뒤 T타입으로 변환하여 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(ref byte ptr, uint byteOffset) => ref As<byte, T>(ref AddByteOffset(ref ptr, byteOffset));

        /// <summary>참조 주소에 지정된 바이트오프셋을 더한 뒤 T타입으로 변환하여 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(ref byte ptr, nint byteOffset) => ref As<byte, T>(ref AddByteOffset(ref ptr, byteOffset));

        /// <summary>참조 주소에 지정된 바이트오프셋을 더한 뒤 T타입으로 변환하여 반환한다 (양수 오프셋이 보장되는 경우 사용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsTU<T>(ref byte ptr, int byteOffset) => ref As<byte, T>(ref AddByteOffset(ref ptr, (uint)byteOffset));



        /// <summary>두 byte 참조 주소 사이의 바이트오프셋을 계산한다 (target - origin)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SubRef(ref byte origin, ref byte target) => (int)ByteOffset(ref origin, ref target);

        /// <summary>두 참조 주소 사이의 바이트오프셋을 계산한다 (서로 다른 타입, target - origin)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SubRef<T, U>(ref T origin, ref U target) => (int)ByteOffset(ref origin, ref As<U, T>(ref target));


        /// <summary>두 참조 주소를 비교해 left가 더 작은지 검사한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThan<T>(ref T left, ref T right) => IsAddressLessThan(ref left, ref right);

        /// <summary>두 참조 주소를 비교해 left가 더 큰지 검사한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThan<T>(ref T left, ref T right) => IsAddressGreaterThan(ref left, ref right);


        /// <summary>두 byte 참조 주소가 동일한지 검사한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SameRef(ref byte left, ref byte right) => AreSame(ref left, ref right);

        /// <summary>두 참조 주소가 동일한지 검사한다 (서로 다른 타입)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SameRef<T, U>(ref T left, ref U right) => AreSame(ref left, ref As<U, T>(ref right));


        /// <summary>조건 검사를 생략해 빠르게 Span을 생성한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(T[] array) =>
            CreateSpan(ref GetArrayDataReference(array), array.Length);

        /// <summary>조건 검사를 생략해 빠르게 ReadOnlySpan을 생성한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(T[] array) =>
            CreateReadOnlySpan(ref GetArrayDataReference(array), array.Length);


        /// <summary>조건 검사를 생략해 빠르게 Span을 생성한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(T[] array, int length) =>
            CreateSpan(ref GetArrayDataReference(array), length);

        /// <summary>조건 검사를 생략해 빠르게 ReadOnlySpan을 생성한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(T[] array, int length) =>
            CreateReadOnlySpan(ref GetArrayDataReference(array), length);


        /// <summary>조건 검사를 생략해 빠르게 Span을 생성한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(T[] array, int index, int length) =>
            CreateSpan(ref AddT(ref GetArrayDataReference(array), index), length);

        /// <summary>조건 검사를 생략해 빠르게 ReadOnlySpan을 생성한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(T[] array, int index, int length) =>
            CreateReadOnlySpan(ref AddT(ref GetArrayDataReference(array), index), length);


        /// <summary>조건 검사를 생략해 빠르게 Span을 생성한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(ref T ptr, int count) => CreateSpan(ref ptr, count);

        /// <summary>조건 검사를 생략해 빠르게 ReadOnlySpan을 생성한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(ref T ptr, int count) => CreateReadOnlySpan(ref ptr, count);


        /// <summary>Null 참조를 생성해 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NullRef<T>() => ref Unsafe.NullRef<T>();


        /// <summary>배열을 초기화한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RefClear<T>(T[] array)
        {
            ref var ref0 = ref GetArrayDataReference(array);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                CreateSpan(ref ref0, array.Length).Clear();
            else
                AC.ZeroFill(ref AsB(ref ref0), array.Length * SizeOf<T>());
        }

        /// <summary>배열을 부분 초기화한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RefClear<T>(T[] array, int count)
        {
            ref var ref0 = ref GetArrayDataReference(array);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                CreateSpan(ref ref0, count).Clear();
            else
                AC.ZeroFill(ref AsB(ref ref0), count * SizeOf<T>());
        }

        /// <summary>배열의 지정된 범위를 초기화한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RefClear<T>(T[] array, int index, int count)
        {
            ref var ref0 = ref Add(ref GetArrayDataReference(array), index);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                CreateSpan(ref ref0, count).Clear();
            else
                AC.ZeroFill(ref AsB(ref ref0), count * SizeOf<T>());
        }

        /// <summary>원본 배열을 다른 배열로 복사한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RefCopy<T>(T[] source, T[] dest, int count)
        {
            if (count > 0)
            {
                ref var src0 = ref GetArrayDataReference(source);
                ref var dst0 = ref GetArrayDataReference(dest);

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    RefCopyInternal(ref src0, ref dst0, count);
                else
                    AC.Copy(ref AsB(ref src0), ref AsB(ref dst0), count * SizeOf<T>());
            }
        }

        /// <summary>원본 배열의 지정된 구간을 다른 배열로 복사한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RefCopy<T>(T[] source, int sindex, T[] dest, int dindex, int count)
        {
            if (count > 0)
            {
                ref var src0 = ref Add(ref GetArrayDataReference(source), sindex);
                ref var dst0 = ref Add(ref GetArrayDataReference(dest), dindex);

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    RefCopyInternal(ref src0, ref dst0, count);
                else
                    AC.Copy(ref AsB(ref src0), ref AsB(ref dst0), count * SizeOf<T>());
            }
        }

        /// <summary>원본 참조 주소가 가리키는 데이터를 다른 배열로 복사한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RefCopy<T>(ref T source0, T[] dest, int dindex, int count)
        {
            if (count > 0)
            {
                ref var dst0 = ref Add(ref GetArrayDataReference(dest), dindex);

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    RefCopyInternal(ref source0, ref dst0, count);
                else
                    AC.Copy(ref AsB(ref source0), ref AsB(ref dst0), count * SizeOf<T>());
            }
        }

        /// <summary>원본 참조 주소가 가리키는 데이터를 다른 참조 주소로 복사한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RefCopy<T>(ref T source0, ref T dest0, int count)
        {
            if (count > 0)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    RefCopyInternal(ref source0, ref dest0, count);
                else
                    AC.Copy(ref AsB(ref source0), ref AsB(ref dest0), count * SizeOf<T>());
            }
        }

        /// <summary>원본 배열을 복사해 새 배열을 반환한다</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] RefCopyNew<T>(T[] source, int copysize) => RefCopyNew(source, copysize, copysize);

        /// <summary>원본 배열을 복사해 지정된 크기의 새 배열을 반환한다</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T[] RefCopyNew<T>(T[] source, int newsize, int copysize)
        {
            var dest = AC.UninitializedArray<T>(newsize);
            if (copysize > 0)
            {
                ref var src0 = ref GetArrayDataReference(source);
                ref var dst0 = ref GetArrayDataReference(dest);

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    RefCopyInternal(ref src0, ref dst0, copysize);
                else
                    AC.Copy(ref AsB(ref src0), ref AsB(ref dst0), copysize * SizeOf<T>());
            }
            return dest;
        }

        /// <summary>원본 배열을 복사해 새 배열을 반환하고, 원본 배열을 초기화한다</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T[] RefCopyNewClear<T>(T[] source, int copy)
        {
            var dest = AC.UninitializedArray<T>(copy);
            if (copy > 0)
            {
                ref var src0 = ref GetArrayDataReference(source);
                ref var dst0 = ref GetArrayDataReference(dest);

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    RefCopyInternal(ref src0, ref dst0, copy);
                    CreateSpan(ref src0, copy).Clear();
                }
                else
                {
                    AC.Copy(ref AsB(ref src0), ref AsB(ref dst0), copy * SizeOf<T>());
                    AC.ZeroFill(ref AsB(ref src0), copy * SizeOf<T>());
                }
            }
            return dest;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RefCopyInternal<T>(ref T src0, ref T dst0, int cnt)
        {
            if (cnt < 8)
            {
                dst0 = src0;
                if (cnt > 2)
                {
                    if (cnt > 4)
                    {
                        Add(ref dst0, 1) = Add(ref src0, 1);
                        Add(ref dst0, 2) = Add(ref src0, 2);
                        Add(ref dst0, 3) = Add(ref src0, 3);
                    }
                    Add(ref dst0, (uint)cnt - 3) = Add(ref src0, (uint)cnt - 3);
                    Add(ref dst0, (uint)cnt - 2) = Add(ref src0, (uint)cnt - 2);
                }
                Add(ref dst0, (uint)cnt - 1) = Add(ref src0, (uint)cnt - 1);
            }
            else
            {
                CreateSpan(ref src0, cnt).CopyTo(CreateSpan(ref dst0, cnt));
            }
        }
    }
}