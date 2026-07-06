namespace JHLib.ChartManager.ENC
{
    public class StatusLst(int baseNumber)
    {
        public int baseNumber = baseNumber;

        public string? provider = null;
        public int? week = null;
        public string? message = null;
        public DateTime? issueDate = null;
    }
}