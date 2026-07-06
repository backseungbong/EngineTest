namespace JHLib.ChartManager.ENC
{
    public class Attribute
    {
        public string acronym;
        public int code;

        public string? objectType;

        public string? type;
        public string? name;
        public List<string> element = new List<string>();
        public string? format;



        public Attribute(string acronym, int code)
        {
            this.acronym = acronym;
            this.code = code;
        }
    }
}