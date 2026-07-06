namespace JHLib.ChartManager.ENC
{
    public class Object
    {
        public string acronym;
        public int code;

        public string? type;
        public string? name;
        public List<string[]> element = new List<string[]>();
        public List<string> shapeType = new List<string>();



        public Object(string acronym, int code)
        {
            this.acronym = acronym;
            this.code = code;
        }
    }
}