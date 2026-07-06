namespace JHLib.ChartManager.Record
{
    public class ProvideRecord(string provider, int baseNumber, int week)
    {
        public string provider = provider;
        public int baseNumber = baseNumber;
        public int week = week;
        public DateTime? referenceDate = null;
    }
}