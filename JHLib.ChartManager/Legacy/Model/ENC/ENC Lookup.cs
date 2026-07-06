namespace Legacy.ECM_Core.ENC
{
    public struct Lookup
    {
        public string Acronym;
        public List<(string Acronym, string[] Element)> Attribute;

        public List<SY> SY;
        public List<TE> TE;
        public List<TX> TX;
        public List<LS> LS;
        public List<LC> LC;
        public List<AC> AC;
        public List<AP> AP;
        public List<CS> CS;

        public byte Display_Group;
        public byte Display_Category;
        public int Group_Layer;
        public byte Radar_Overlay;
    }



    #region [[ Lookup Command ]]
    public struct SY
    {
        public string Acronym;
        public string Degree;
    }

    public struct TE
    {
        public string Format;
        public string Element;

        public int Font_HJUST;
        public int Font_VJUST;
        public int Font_SPACE;
        public string Font_CHARS;
        public (int X, int Y) Font_Offset;
        public string Font_ColorAcronym;
        public int Font_Group;
    }

    public struct TX
    {
        public string Text;
        public string Element;

        public int Font_HJUST;
        public int Font_VJUST;
        public int Font_SPACE;
        public string Font_CHARS;
        public (int X, int Y) Font_Offset;
        public string Font_ColorAcronym;
        public int Font_Group;
    }

    public struct LS
    {
        public int Pen_Type;
        public int Pen_Width;
        public string Pen_ColorAcronym;
    }

    public struct LC
    {
        public string Acronym;
    }

    public struct AC
    {
        public string Acronym;
        public int Trans;
    }

    public struct AP
    {
        public string Acronym;
    }

    public struct CS
    {
        public string Acronym;
    }
    #endregion
}