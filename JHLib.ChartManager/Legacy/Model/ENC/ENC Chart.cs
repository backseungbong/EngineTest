namespace Legacy.ECM_Core.ENC
{
    public struct Chart
    {
        public string Name;

        public int Usage;
        public int Scale;
        public int Comf;

        public (int EDTN, int UPDN) Base;
        public int Update;
        public string Issue_Date;
        public string Update_Date;
        public string S57_Version;
        public int Agency;

        public (int X, int Y) Center_Point;

        public int HDAT;
        public int VDAT;
        public int SDAT;
        public int DUNI;
        public int HUNI;
        public int PUNI;

        public (double North, double South, double East, double West) Boundary;

        public string Reference_Date;
        public bool Overlap;
    }



    public struct ChartCoverage
    {
        public string Name;

        public int Usage;
        public int Scale;

        public (int EDTN, int UPDN) Base;
        public string Issue_Date;

        public JHLib.Util.Struct.Float2D[] Path;
    }
}