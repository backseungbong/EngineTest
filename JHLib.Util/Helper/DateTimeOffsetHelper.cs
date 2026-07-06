namespace JHLib.Util.Helper
{
    public static class DateTimeOffsetHelper
    {
        public static DateTimeOffset ParseOrDefault(string timeOffsetText, DateTimeOffset defaultTimeOffset = default)
        {
            if (timeOffsetText != null && timeOffsetText.Length > 0)
            {
                var span = timeOffsetText.AsSpan().Trim();
                if (DateTimeOffset.TryParse(span, out var result))
                    return result;

                var r = timeOffsetText.Trim();
                r = r.Replace(" ", "");
                r = r.Replace(".", ":");
                r = r.Replace("-", ":");
                r = r.Replace("_", ":");
                r = r.Replace("..", ":");
                r = r.Replace("::", ":");
                r = r.Replace(":000", ".000");

                if (DateTimeOffset.TryParse(r, out result))
                    return result;
            }
            return defaultTimeOffset;
        }

        public static bool TryParse(string timeOffsetText, out DateTimeOffset result)
        {
            if (timeOffsetText != null && timeOffsetText.Length > 0)
            {
                if (DateTimeOffset.TryParse(timeOffsetText, out result))
                    return true;

                var r = timeOffsetText.Trim();
                r = r.Replace(" ", "");
                r = r.Replace(",", ":");
                r = r.Replace(".", ":");
                r = r.Replace("-", ":");
                r = r.Replace("_", ":");
                r = r.Replace("..", ":");
                r = r.Replace("::", ":");
                r = r.Replace(":000", ".000");

                if (DateTimeOffset.TryParse(r, out result))
                    return true;
            }
            result = default;
            return false;
        }
    }
}