namespace JHLib.ChartManager.ENC
{
    public class SubContent
    {
        public string RCNM;
        public int RCID;

        public string? IMPL = null;

        public string? contentFile;



        public SubContent(string RCNM, int RCID)
        {
            this.RCNM = RCNM;
            this.RCID = RCID;
        }
    }
}