namespace JHLib.ChartManager.Chart.Search
{
    public class SearchChart
    {
        public string name;

        public (int EDTN, int UPDN)? baseVersion = null;
        public int? updateVersion = null;
        public string? issueDate = null;
        public ENC.SerialEnc? serialEnc = null;

        public (double north, double south, double east, double west)? boundary = null;
        public (ENC.Permit.Validation? permit, bool necessary, bool product) validation = (permit: null, necessary: false, product: false);

        public List<SearchCell> cell = new List<SearchCell>();
        public List<ENC.SubContent> subContent = new List<ENC.SubContent>();



        public SearchChart(string name)
        {
            this.name = name;
        }
    }
}