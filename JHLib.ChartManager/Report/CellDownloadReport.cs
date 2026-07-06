namespace JHLib.ChartManager.Report
{
    public class CellDownloadReport
    {
        public string name { get; private set; }
        public bool result { get; private set; }
        public string reason { get; private set; }



        public CellDownloadReport(string name, bool result, string reason)
        {
            this.name = name;
            this.result = result;
            this.reason = reason;
        }
    }
}