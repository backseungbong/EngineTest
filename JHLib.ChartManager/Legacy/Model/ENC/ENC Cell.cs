namespace Legacy.ECM_Core.ENC
{
    public struct Cell
    {
        public string RCNM;
        public int RCID;

        public string File;
        public string LFile;
        public string VOLM;
        public string IMPL;

        public (double North, double South, double East, double West) Boundary;

        public string CRC;
        public string Comment;

        public string Version;
        public int EDTN;
        public int UPDN;

        public string Catalogue_File;
        public string Provider;       // 공급자명
        public string Provide_Type;   // Base, Update
        public int Week;              // Week정보
    }
}