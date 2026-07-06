using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace JHLib.Util.Helper
{
    public static class RandomHelper
    {
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long CreateInt64()
        {
            Unsafe.SkipInit(out long value);
            RandomNumberGenerator.Fill(MemoryMarshal.CreateSpan(ref *(byte*)&value, 8));
            return value;
        }
    }
}