namespace JHLib.ChartManager.ENC
{
    public class Cell
    {
        public string RCNM;
        public int RCID;

        public string? FILE = null;
        public string? LFILE = null;
        public string? VOLM = null;
        public string? IMPL = null;

        public (double north, double south, double east, double west)? boundary = null;

        public string? CRC = null;
        public string? comment = null;

        public string? version = null;
        public int? EDTN = null;
        public int? UPDN = null;

        public string? catalogueFile = null;
        public string? provider = null;       // 공급자명
        public string? provideType = null;    // Base, Update
        public int? week = null;              // Week정보



        public Cell(string RCNM, int RCID)
        {
            this.RCNM = RCNM;
            this.RCID = RCID;
        }



        public class Serial
        {
            public string? provider = null;    // 공급자명
            public int? week = null;           // Week 정보
            public string? issueDate = null;   // 제작 날짜
            public string? type = null;        // Base, Update
            public int? totalNumber = null;    // Total 갯수
            public int? currentNumber = null;  // 현재 번호
        }
    }
}