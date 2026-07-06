#pragma warning disable CS0660 // 형식은 == 연산자 또는 != 연산자를 정의하지만 Object.Equals(object o)를 재정의하지 않습니다.
#pragma warning disable CS0661 // 형식은 == 연산자 또는 != 연산자를 정의하지만 Object.GetHashCode()를 재정의하지 않습니다.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public readonly struct DateRange
    {
        private readonly ulong _date;

        public readonly int Year1 => (ushort)(_date >> 48);
        public readonly int Month1 => (byte)(_date >> 40);
        public readonly int Day1 => (byte)(_date >> 32);

        public readonly int Year2 => (ushort)(_date >> 16);
        public readonly int Month2 => (byte)(_date >> 8);
        public readonly int Day2 => (byte)(_date >> 0);

        public DateRange(uint s, uint e) => _date = (ulong)s << 32 | e;
        public DateRange(DateTime start, DateTime end)
        {
            if (end < start) { var t = start; start = end; end = t; }
            var s = (uint)(start.Year << 16) | (uint)(start.Month << 8) | (uint)start.Day;
            var e = (uint)(end.Year << 16) | (uint)(end.Month << 8) | (uint)end.Day;
            _date = (ulong)s << 32 | e;
        }
        public DateRange(DateTime date)
        {
            var n = (uint)(date.Year << 16) | (uint)(date.Month << 8) | (uint)date.Day;
            _date = (ulong)n << 32 | n;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetData(
            out int year1, out int month1, out int day1,
            out int year2, out int month2, out int day2)
        {
            var date = _date;
            year1 = (ushort)(date >> 48);
            month1 = (byte)(date >> 40);
            day1 = (byte)(date >> 32);
            year2 = (ushort)(date >> 16);
            month2 = (byte)(date >> 8);
            day2 = (byte)(date >> 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetData1(out int year, out int month, out int day)
        {
            var date = _date;
            year = (ushort)(date >> 48);
            month = (byte)(date >> 40);
            day = (byte)(date >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetData2(out int year, out int month, out int day)
        {
            var date = _date;
            year = (ushort)(date >> 16);
            month = (byte)(date >> 8);
            day = (byte)(date >> 0);
        }

        public readonly DateTimeRange ToDateTimeRange()
        {
            var date = _date;
            DateTime date1;
            DateTime date2;

            try { date1 = new DateTime((ushort)(date >> 48), (byte)(date >> 40), (byte)(date >> 32)); }
            catch (Exception e)
            {
                date1 = new DateTime(1900, 1, 1);
                Trace.WriteLine(e.Message);
            }

            try { date2 = new DateTime((ushort)(date >> 16), (byte)(date >> 08), (byte)(date >> 00)); }
            catch (Exception e)
            {
                date2 = new DateTime(3000, 1, 1);
                Trace.WriteLine(e.Message);
            }
            return new DateTimeRange(date1, date2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(DateRange a, DateRange b) => a._date != b._date;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(DateRange a, DateRange b) => a._date == b._date;
    }
}

#pragma warning restore CS0660 // 형식은 == 연산자 또는 != 연산자를 정의하지만 Object.Equals(object o)를 재정의하지 않습니다.
#pragma warning restore CS0661 // 형식은 == 연산자 또는 != 연산자를 정의하지만 Object.GetHashCode()를 재정의하지 않습니다.