using JHLib.Util.ByteControl;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Helper
{
    public static class DateTimeHelper
    {
        private static readonly long MinTicks = DateTime.MinValue.Ticks;
        private static readonly long MaxTicks = DateTime.MaxValue.Ticks;

        private readonly static char[] DayToWeekKorean;
        private readonly static string[] DayToWeekFullKorean;
        private readonly static string[] Formats;

        static DateTimeHelper()
        {
            DayToWeekKorean = ['일', '월', '화', '수', '목', '금', '토'];
            DayToWeekFullKorean = ["일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일"];
            Formats =
            [
                "yyyyMMddHHmmss",
                "yyyyMMddTHHmmss",
                "yyyyMMddHHmmssfff",
                "yyyyMMddTHHmmssfff",

                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-dd HH:mm:ssfff",
                "yyyy-MM-ddTHH:mm:ssfff",

                "yyyy/MM/dd HH:mm:ss",
                "yyyy/MM/ddTHH:mm:ss",
                "yyyy/MM/dd HH:mm:ssfff",
                "yyyy/MM/ddTHH:mm:ssfff",

                "yyyyMMddHHmmss.fff",
                "yyyyMMddTHHmmss.fff",
                "yyyy-MM-dd HH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy/MM/dd HH:mm:ss.fff",
                "yyyy/MM/ddTHH:mm:ss.fff",
            ];
        }

        public static DateTime ParseOrDefault(string strDateTime, DateTime defaultDateTime = default)
        {
            if (strDateTime != null && strDateTime.Length > 0)
            {
                var span = strDateTime.AsSpan().Trim();
                if (DateTime.TryParse(span, out var result))
                    return result;

                if (DateTime.TryParseExact(span, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    return result;
            }
            return defaultDateTime;
        }

        public static bool TryParse(string strDateTime, out DateTime result)
        {
            if (strDateTime != null && strDateTime.Length > 0)
            {
                var span = strDateTime.AsSpan().Trim();
                if (DateTime.TryParse(span, out result))
                    return true;

                if (DateTime.TryParseExact(span, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    return true;
            }
            result = default;
            return false;
        }

        public static char WeekToKoreanName(DayOfWeek week) => DayToWeekKorean[(int)week];
        public static string WeekToKoreanFullName(DayOfWeek week) => DayToWeekFullKorean[(int)week];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToKoreanTextFromUTC(DateTime utcTime) => ToKoreanTextFromUTC(utcTime.Ticks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToKoreanTextFromUTC(long utcTicks) => ToKoreanText(utcTicks + TimeSpan.TicksPerHour * 9);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToKoreanText(long ticks) =>
            ToKoreanText((ulong)ticks <= (ulong)MaxTicks ? Unsafe.As<long, DateTime>(ref ticks) : DateTime.MinValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ToKoreanText(DateTime time)
        {
            var text = new string(default, 24);
            ref var t0 = ref MemoryMarshal.GetReference<char>(text);
            ref var c99 = ref MemoryMarshal.GetReference(ASCII.NumToChar99);

            var y = (uint)time.Year;
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 0)) = Unsafe.Add(ref c99, y / 100);
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 2)) = Unsafe.Add(ref c99, y % 100);
            Unsafe.Add(ref t0, 4) = '/';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 5)) = Unsafe.Add(ref c99, (uint)time.Month);
            Unsafe.Add(ref t0, 7) = '/';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 8)) = Unsafe.Add(ref c99, (uint)time.Day);
            Unsafe.Add(ref t0, 10) = '(';
            Unsafe.Add(ref t0, 11) = DayToWeekKorean[(int)time.DayOfWeek];
            Unsafe.Add(ref t0, 12) = ')';
            Unsafe.Add(ref t0, 13) = ' ';
            Unsafe.Add(ref t0, 14) = '오';

            var hour = (uint)time.Hour;
            var ampm = hour < 12 ? '전' : '후';
            var hh = hour > 12 ? hour - 12 : hour;
            var mm = (uint)time.Minute;
            var ss = (uint)time.Second;

            Unsafe.Add(ref t0, 15) = ampm;
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 16)) = Unsafe.Add(ref c99, hh);
            Unsafe.Add(ref t0, 18) = ':';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 19)) = Unsafe.Add(ref c99, mm);
            Unsafe.Add(ref t0, 21) = ':';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 22)) = Unsafe.Add(ref c99, ss);

            return text;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToKoreanShortTextFromUTC(DateTime utcTime) => ToKoreanShortTextFromUTC(utcTime.Ticks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToKoreanShortTextFromUTC(long utcTicks) => ToShortText(utcTicks + TimeSpan.TicksPerHour * 9);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToShortText(long ticks) =>
            ToShortText((ulong)ticks <= (ulong)MaxTicks ? Unsafe.As<long, DateTime>(ref ticks) : DateTime.MinValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ToShortText(DateTime time)
        {
            var text = new string(default, 17);
            ref var t0 = ref MemoryMarshal.GetReference<char>(text);
            ref var c99 = ref MemoryMarshal.GetReference(ASCII.NumToChar99);

            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 0)) = Unsafe.Add(ref c99, (uint)time.Year % 100);
            Unsafe.Add(ref t0, 2) = '/';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 3)) = Unsafe.Add(ref c99, (uint)time.Month);
            Unsafe.Add(ref t0, 5) = '/';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 6)) = Unsafe.Add(ref c99, (uint)time.Day);

            var hh = (uint)time.Hour;
            var mm = (uint)time.Minute;
            var ss = (uint)time.Second;

            Unsafe.Add(ref t0, 8) = ' ';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 9)) = Unsafe.Add(ref c99, hh);
            Unsafe.Add(ref t0, 11) = ':';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 12)) = Unsafe.Add(ref c99, mm);
            Unsafe.Add(ref t0, 14) = ':';
            Unsafe.As<char, uint>(ref Unsafe.Add(ref t0, 15)) = Unsafe.Add(ref c99, ss);

            return text;
        }
    }
}