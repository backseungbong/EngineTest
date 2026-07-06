namespace JHLib.ChartManager.ENC
{
    public class Lookup
    {
        public string acronym;

        public List<(string acronym, string[] element)> attribute = new List<(string acronym, string[] element)>();

        public List<Lookup.SY> sy = new List<SY>();
        public List<Lookup.TE> te = new List<TE>();
        public List<Lookup.TX> tx = new List<TX>();
        public List<Lookup.LS> ls = new List<LS>();
        public List<Lookup.LC> lc = new List<LC>();
        public List<Lookup.AC> ac = new List<AC>();
        public List<Lookup.AP> ap = new List<AP>();
        public List<Lookup.CS> cs = new List<CS>();

        public byte? displayGroup;
        public byte? displayCategory;
        public int? groupLayer;
        public byte? radarOverlay;



        public Lookup(string acronym)
        {
            this.acronym = acronym;
        }



        public class SY
        {
            public string acronym;
            public string? degree;



            public SY(string acronym)
            {
                this.acronym = acronym;
            }
        }

        public class TE
        {
            public string? format;
            public string? element;

            public Font font = new Font();
        }

        public class TX
        {
            public string? text;
            public string? element;

            public Font font = new Font();
        }

        public class LS
        {
            public Pen pen = new Pen();
        }

        public class LC
        {
            public string acronym;



            public LC(string acronym)
            {
                this.acronym = acronym;
            }
        }

        public class AC
        {
            public string acronym;
            public int? trans;



            public AC(string acronym)
            {
                this.acronym = acronym;
            }
        }

        public class AP
        {
            public string acronym;



            public AP(string acronym)
            {
                this.acronym = acronym;
            }
        }

        public class CS
        {
            public string acronym;



            public CS(string acronym)
            {
                this.acronym = acronym;
            }
        }

        public class Font
        {
            public int? HJUST;
            public int? VJUST;
            public int? SPACE;
            public string? CHARS;
            public (int x, int y)? offset;
            public string? colorAcronym;
            public int? group;
        }

        public class Pen
        {
            public int? type;
            public int? width;
            public string? colorAcronym;
        }
    }
}