using System.Runtime.CompilerServices;

namespace JHLib.Util.Time
{
    /// <summary>
    /// UNIX 타임의 특성상 32비트로 표현할 수 있는 값이 제한적이므로 <para/>
    /// 기본적으로 64비트의 변환을 하고, 추가적으로 부호없는 32비트 변환을 제공한다
    /// </summary>
    public static class UnixTime
    {
        private readonly static long TICKS_MIN; // UNIX 타임의 최소 Ticks 값
        private readonly static long TICKS_MAX; // UNIX 타임의 최대 Ticks 값 (5000년 이후의 시간은 에러 값으로 간주)
        private readonly static long TICKS_MAX32; // 부호 없는 32비트 기반 UNIX 타임 최대값 (약 2106년 2월 7일)
        private readonly static long UNIX_MAX; // UNIX 타임의 최대 Seconds 값

        static UnixTime()
        {
            TICKS_MIN = new DateTime(1970, 1, 1).Ticks;
            TICKS_MAX = new DateTime(5000, 1, 1).Ticks;
            TICKS_MAX32 = TICKS_MIN + uint.MaxValue * TimeSpan.TicksPerSecond;
            UNIX_MAX = (TICKS_MAX - TICKS_MIN) / TimeSpan.TicksPerSecond;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ToDate(uint unixTime) => new(ToTicksInternal(unixTime));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ToDate(long unixTime) => new(ToTicksInternal(unixTime));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToTicks(uint unixTime) => ToTicksInternal(unixTime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToTicks(long unixTime) => ToTicksInternal(unixTime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ToTicksInternal(long unix)
        {
            if (0 < unix)
                if (unix < UNIX_MAX)
                    return TICKS_MIN + unix * TimeSpan.TicksPerSecond;
                else return TICKS_MAX;
            else return TICKS_MIN;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToUnix(DateTime utc) => ToUnix(utc.Ticks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToUnix(long ticks)
        {
            if (TICKS_MIN < ticks)
                if (ticks < TICKS_MAX)
                    return (ticks - TICKS_MIN) / TimeSpan.TicksPerSecond;
                else return UNIX_MAX;
            else return 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUnix32(DateTime utc) => ToUnix32(utc.Ticks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUnix32(long ticks)
        {
            if (TICKS_MIN < ticks)
                if (ticks < TICKS_MAX32)
                    return (uint)((ticks - TICKS_MIN) / TimeSpan.TicksPerSecond);
                else return uint.MaxValue;
            else return 0;
        }
    }
}