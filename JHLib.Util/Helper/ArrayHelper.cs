using System.Numerics;
using System.Runtime.CompilerServices;

namespace JHLib.Util.Helper
{
    public static class ArrayHelper
    {
        // .NET 배열 및 2의 거듭제곱 제약을 고려한 최대 안전 상한선 (2^30 = 1,073,741,824)
        private const int MaxValidBucketSize = 1 << 30;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Pow2ArrayLength(int needsize, int minsize = 4)
        {
            var size = minsize;
            if (needsize > minsize)
            {
                size = (int)BitOperations.RoundUpToPowerOf2((uint)needsize);
                if (needsize > MaxValidBucketSize) ThrowArgumentOutOfRangeException(needsize);
            }
            return size;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeException(int needsize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(needsize), $"The bucket size {needsize} is too large. The maximum allowed size is {Array.MaxLength}.");
        }
    }
}