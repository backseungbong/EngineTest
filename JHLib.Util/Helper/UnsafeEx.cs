using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Helper
{
    public static unsafe class UnsafeEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(nint ptr, in T data) where T : unmanaged
        {
            *(T*)ptr = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(nint src, nint dst, int size)
        {
            if (size > 0) Copy(src, dst, (uint)size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(nint src, nint dst, uint size)
        {
            if (size != 0)
                Unsafe.CopyBlock((void*)dst, (void*)src, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Arr0<T>(T[] buk) =>
            ref MemoryMarshal.GetArrayDataReference(buk);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Arr0<T>(T[] buk, int idx) =>
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buk), (uint)idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Arr0<T>(T[] buk, uint idx) =>
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buk), idx);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddT<T>(in T ptr, nint idx) =>
            ref Unsafe.Add(ref Unsafe.AsRef(in ptr), idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddT<T>(in T ptr, uint idx) =>
            ref Unsafe.Add(ref Unsafe.AsRef(in ptr), idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddT<T>(in T ptr, int idx = 0) =>
            ref Unsafe.Add(ref Unsafe.AsRef(in ptr), (uint)idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubT<T>(in T ptr, nint idx) =>
            ref Unsafe.Subtract(ref Unsafe.AsRef(in ptr), idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubT<T>(in T ptr, uint idx) =>
            ref Unsafe.Subtract(ref Unsafe.AsRef(in ptr), idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T SubT<T>(in T ptr, int idx) =>
            ref Unsafe.Subtract(ref Unsafe.AsRef(in ptr), (uint)idx);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(in T ptr) =>
            ref Unsafe.AsRef(in ptr);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(in byte ptr) =>
            ref Unsafe.As<byte, T>(ref Unsafe.AsRef(in ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsT<T>(in byte ptr, uint byteOffset) =>
            ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in ptr), byteOffset));



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsByte<T>(in T ptr) =>
            ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref int AsInt<T>(in T ptr) =>
            ref Unsafe.As<T, int>(ref Unsafe.AsRef(in ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref uint AsUInt<T>(in T ptr) =>
            ref Unsafe.As<T, uint>(ref Unsafe.AsRef(in ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref long AsLong<T>(in T ptr) =>
            ref Unsafe.As<T, long>(ref Unsafe.AsRef(in ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref ulong AsULong<T>(in T ptr) =>
            ref Unsafe.As<T, ulong>(ref Unsafe.AsRef(in ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref float AsSingle<T>(in T ptr) =>
            ref Unsafe.As<T, float>(ref Unsafe.AsRef(in ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref double AsDouble<T>(in T ptr) =>
            ref Unsafe.As<T, double>(ref Unsafe.AsRef(in ptr));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsByte<T>(T[] buk) =>
            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(buk));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsByte<T>(T[] buk, int byteOffset) =>
            ref Unsafe.AddByteOffset(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(buk)), byteOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsByte<T>(in T ptr, int byteOffset) =>
            ref Unsafe.AddByteOffset(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in ptr)), byteOffset);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Less<T>(in T left, in T right) =>
            Unsafe.IsAddressLessThan(ref Unsafe.AsRef(in left), ref Unsafe.AsRef(in right));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessEqual<T>(in T left, in T right) =>
            Unsafe.IsAddressLessThanOrEqualTo(ref Unsafe.AsRef(in left), ref Unsafe.AsRef(in right));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Same<T>(in T ptr1, in T ptr2) =>
            Unsafe.AreSame(ref Unsafe.AsRef(in ptr1), ref Unsafe.AsRef(in ptr2));

        /// <summary> target - origin </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Diff<T>(in T target, in T origin) =>
            (int)((uint)Unsafe.ByteOffset(
                ref Unsafe.AsRef(in origin),
                ref Unsafe.AsRef(in target)) / (uint)Unsafe.SizeOf<T>());


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyBlock(in byte src0, in byte dst0, uint len) =>
            Unsafe.CopyBlock(ref Unsafe.AsRef(in dst0), ref Unsafe.AsRef(in src0), len);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyBlock(in byte src0, in byte dst0, int len) =>
            Unsafe.CopyBlock(ref Unsafe.AsRef(in dst0), ref Unsafe.AsRef(in src0), (uint)len);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> CreateSpan<T>(in T data0, int count) =>
            MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in data0), count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> CreateReadSpan<T>(in T data0, int count) =>
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in data0), count);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> CreateSpan<T>(T[] array) => CreateSpan(array, array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> CreateSpan<T>(T[] array, int length)
        {
            ref var array0 = ref MemoryMarshal.GetArrayDataReference(array);
            return MemoryMarshal.CreateSpan(ref array0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> CreateReadSpan<T>(T[] array) => CreateReadSpan(array, array.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> CreateReadSpan<T>(T[] array, int length)
        {
            ref var array0 = ref MemoryMarshal.GetArrayDataReference(array);
            return MemoryMarshal.CreateReadOnlySpan(ref array0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Resize<T>(T[] array, int resize)
        {
            var length = array.Length;
            var result = new T[resize];
            ref var dst = ref MemoryMarshal.GetArrayDataReference(result);
            ref var src = ref MemoryMarshal.GetArrayDataReference(array);
            CreateSpan(src, length).CopyTo(CreateSpan(dst, length));
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> Limit<T>(T[] array, int length, bool checkOutOfRange = false)
        {
            if (checkOutOfRange && (uint)array.Length < (uint)length)
                IndexException();

            ref var buffer0 = ref Arr0(array);
            return MemoryMarshal.CreateSpan(ref buffer0, length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> Slice<T>(T[] array, int offset, bool checkOutOfRange = false)
        {
            if (checkOutOfRange && (uint)array.Length < (uint)offset)
                IndexException();

            ref var buffer0 = ref Arr0(array, offset);
            return MemoryMarshal.CreateSpan(ref buffer0, array.Length - offset);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> Slice<T>(T[] array, int offset, int length, bool checkOutOfRange = false)
        {
            if (checkOutOfRange && (uint)array.Length < (uint)offset + (ulong)(uint)length)
                IndexException();

            ref var buffer0 = ref Arr0(array, offset);
            return MemoryMarshal.CreateSpan(ref buffer0, length);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void IndexException() => throw new IndexOutOfRangeException();
    }
}