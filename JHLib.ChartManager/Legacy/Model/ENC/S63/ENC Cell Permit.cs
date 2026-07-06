namespace Legacy.ECM_Core.ENC
{
    public struct CellPermit
    {
        public string Name;

        public int Error; // 0 에러 없음, 1 주의 (사용은 가능), 2 치명적 에러 (사용불가), 3 Old Version, 4 Permit 만료, 9 New Version
        public int Type; // 0 ENC, 1 ECS
        public string Checksum;

        public string Expiration_Date;
        public (string X, string Y) Key;

        public string Comment;

        public string DSID;
        public int EDTN;
        public int Service_Level; // 서비스 레벨 지시자 (0 또는 1)
    }
}