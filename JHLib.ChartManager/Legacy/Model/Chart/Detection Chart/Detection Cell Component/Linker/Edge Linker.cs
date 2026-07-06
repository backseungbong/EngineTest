namespace Legacy.ECM_Core.DCC
{
    public class EdgeLinker
    {
        public byte RCNM = 0;
        public uint RCID = 0;

        public uint ATVL = 1;

        public byte ORNT = 0;
        public byte USAG = 0;
        public byte MASK = 0;

        public EdgeAttribute Edge_Attribute;
        public EdgeCommand Edge_Command;

        public List<SG2D>? SG2D;



        public EdgeLinker()
        {
            Edge_Attribute = new EdgeAttribute() {
                UNSAFE = false,
                VALDCO = float.MaxValue,
                DRVAL1 = float.MaxValue,
            };
            Edge_Command = new EdgeCommand() {
                SY = -1,
                LS = new LS() {
                    Pen_Type = -1,
                    Pen_Width = -1,
                    Pen_ColorIndex = -1,
                },
                LC = -1,
            };
        }
    }



    public struct EdgeAttribute
    {
        public bool UNSAFE;
        public float VALDCO;
        public float DRVAL1;
    }
}