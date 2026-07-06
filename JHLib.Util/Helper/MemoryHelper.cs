using JHLib.Util.ArrayControl;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Helper
{
    public static unsafe class MemoryHelper
    {
        // 정렬은 최소 16, 최대 256바이트 내로 제한
        private static ReadOnlySpan<uint> AlignMap => [16, 32, 64, 64, 128, 128, 128, 128];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint CheckAlign(int alignment)
        {
            if (alignment > 16)
                if (alignment < 256) return AlignMap[alignment >> 5];
                else return 256;
            else return 16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(nint ptr) => NativeMemory.AlignedFree((void*)ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(ref nint refptr)
        {
            var ptrFree = Interlocked.Exchange(ref refptr, 0);
            if (ptrFree != 0) NativeMemory.AlignedFree((void*)ptrFree);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static nint Alloc(int allocSize, int alignment = 64, bool fillZero = false)
        {
            var alloc = allocSize > 64 ? (uint)allocSize : 64;
            var align = CheckAlign(alignment);
            var amask = align - 1;
            alloc = alloc + amask & ~amask;

            var ptrAlloc = NativeMemory.AlignedAlloc(alloc, align);
            if (fillZero) AC.ZeroFill(ref *(byte*)ptrAlloc, (int)alloc);
            return (nint)ptrAlloc;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void FreeAlloc(ref nint refptr, int allocSize, int alignment = 64, bool fillZero = false)
        {
            var alloc = allocSize > 64 ? (uint)allocSize : 64;
            var align = CheckAlign(alignment);
            var amask = align - 1;
            alloc = alloc + amask & ~amask;

            var ptrAlloc = NativeMemory.AlignedAlloc(alloc, align);
            if (fillZero) AC.ZeroFill(ref *(byte*)ptrAlloc, (int)alloc);

            var ptrFree = Interlocked.Exchange(ref refptr, (nint)ptrAlloc);
            if (ptrFree != 0) NativeMemory.AlignedFree((void*)ptrFree);
        }
    }
}