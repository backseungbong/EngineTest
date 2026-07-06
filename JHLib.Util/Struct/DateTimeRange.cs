using System.Runtime.InteropServices;

namespace JHLib.Util.Struct
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public readonly struct DateTimeRange
    {
        public readonly DateTime Start;
        public readonly DateTime End;

        public DateTimeRange(DateTime start, DateTime end)
        {
            if (start > end) { Start = end; End = start; }
            else { Start = start; End = end; }
        }
        public DateTimeRange(DateTime date)
        {
            Start = date;
            End = date;
        }

        public readonly DateRange ToDayRange() =>
            new DateRange(Start, End);
    }
}