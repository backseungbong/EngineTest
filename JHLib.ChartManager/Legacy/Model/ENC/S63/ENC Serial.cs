namespace Legacy.ECM_Core.ENC
{
    public struct Serial
    {
        public string Provider;    // 공급자명
        public int Week;           // Week정보
        public string Issue_Date;  // 제작 날짜
        public string Type;        // Base, Update
        public int Total_Number;   // Total 갯수
        public int Current_Number; // 현재 번호
    }
}