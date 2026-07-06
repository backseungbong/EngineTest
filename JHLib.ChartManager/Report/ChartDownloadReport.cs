namespace JHLib.ChartManager.Report
{
    public class ChartDownloadReport
    {
        public string name { get; private set; }
        public bool result { get => this.cellReport.All(report => report.result); }

        public List<CellDownloadReport> cellReport { get; private set; } = new List<CellDownloadReport>();



        public ChartDownloadReport(string name)
        {
            this.name = name;
        }
    }
}