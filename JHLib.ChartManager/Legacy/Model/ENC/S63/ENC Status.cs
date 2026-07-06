namespace Legacy.ECM_Core.ENC
{
    public struct Status
    {
        public int Base_Number;
        public string Provider;
        public int Week;             // 년주차를 이어서 계산 WK19_07 => 0719로 저장
        public string Message;
        public string Issue_Date;

        public byte Week_Validation; // 0 = 없다, 1 = 같다. 2 = 크다, 3 = 작다
        public bool Base_Load;       // true = Base Load상태, false = Base Unload상태
    }
}