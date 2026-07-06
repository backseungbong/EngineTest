namespace JHLib.ChartManager.ENC
{
    public class Permit
    {
        public string name;

        public Validation? error = null;
        public int? type = null;  // 0 ENC, 1 ECS
        public string? checksum = null;

        public string? expirationDate = null;
        public (string X, string Y)? key = null;

        public string? comment = null;

        public string? DSID = null;
        public int? EDTN = null;
        public int? serviceLevel = null; // 서비스 레벨 지시자 (0 또는 1)



        public Permit(string name)
        {
            this.name = name;
        }



        public enum Validation
        {
            Available = 0,
            Warning = 1,    // (사용은 가능)
            FatalError = 2, // (사용불가)
            OldVersion = 3,
            Expired = 4,
            NewVersion = 5,
        }
    }
}