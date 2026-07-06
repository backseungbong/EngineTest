namespace Legacy.ECM_Core.DCC
{
    public class DrawCommand
    {
        public List<SY>? SY;
        public List<LS>? LS;
        public List<LC>? LC;
        public List<AC>? AC;
        public List<AP>? AP;
        public List<TX>? TX;

        public bool Commanding { get => (SY != null) || (LS != null) || (LC != null) || (AC != null) || (AP != null) || (TX != null); }
    }



    #region [[ Draw Command ]]
    public struct SY
    {
        public int Index;
        public float Angle;
    }

    public struct LS
    {
        public int Pen_Type;
        public int Pen_Width;
        public int Pen_ColorIndex;
    }

    public struct LC
    {
        public int Index;
    }

    public struct AC
    {
        public int Index;
        public int Trans;
    }

    public struct AP
    {
        public int Index;
    }

    public struct TX
    {
        public string Text;
        public string NationalText;

        public byte Align;
        public (int X, int Y) Offset;

        public byte Text_Group;
        public byte Text_ColorIndex;
    }
    #endregion
}