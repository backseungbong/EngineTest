namespace Legacy.ECM_Core.Chart
{
    public struct SencObject
    {
        public uint RCID;
        public byte PRIM;
        public ushort OBJL;

        public (int X, int Y) Pivot;

        public int Minimum_Scale;
        public byte Display_Group;
        public byte Radar_Overlay;
        public byte Information;
        public byte Group_Layer;
        public bool Reverse;

        public (int Start, int End) Valid_Date;
        public byte Update_Type;

        #region [[ Light Attribute ]]
        public float ORIENT;
        public float VALNMR;
        public float SECTR1;
        public float SECTR2;

        public bool CATLIT_8_11;
        public bool CATLIT_9;
        public bool CATLIT_1_16;
        public bool LITVIS_3_7_8;

        public byte COLOUR;

        public bool Flare_At_45_Degrees;
        public bool All_Round_Light;
        public string LITDSN;
        public bool Extended_Arc_Radius;
        public bool Radius_26mm;
        #endregion

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;

        public List<DCC.EdgeMask> Edge_Mask;

        public List<DCC.EdgeCommand> Edge_Command;

        public List<DCC.DrawCommand> Command;
    }
}