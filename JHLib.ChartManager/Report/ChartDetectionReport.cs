namespace JHLib.ChartManager.Report
{
    public class ChartDetectionReport
    {
        public string name { get; private set; }
        public bool result = false;
        public string reason = string.Empty;



        public ChartDetectionReport(string name)
        {
            this.name = name;
        }
    }
}