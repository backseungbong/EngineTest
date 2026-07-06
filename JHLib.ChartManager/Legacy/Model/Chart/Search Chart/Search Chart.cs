namespace Legacy.ECM_Core.Chart
{
    public struct SearchChart
    {
        public string Name;

        public (int EDTN, int UPDN) Base;
        public int UPDN;
        public string Issue_Date;

        public (double North, double South, double East, double West) Boundary;

        public int Permit_Validation;
        public bool Necessary_Validation;
        public bool Product_Validation;

        public List<SearchCell> Cell;
    }
}