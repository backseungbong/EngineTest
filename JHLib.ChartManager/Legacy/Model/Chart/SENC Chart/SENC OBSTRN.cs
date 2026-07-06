namespace Legacy.ECM_Core.Chart
{
    public struct SencObstrn
    {
        public uint RCID;
        public byte PRIM;
        public ushort OBJL;

        public (int X, int Y) Pivot;

        public int Minimum_Scale;
        public byte Display_Group;
        public byte Radar_Overlay;
        public bool Information;
        public byte Viewing_Group;

        public byte Update_Type;

        #region [[ Danger Attribute ]]
        public float Danger_DEPTH;
        public float DRVAL1;
        public float VALSOU;
        public bool Danger_Accuracy;
        public bool Danger_WATLEV_1_2;
        public bool Sounding;
        public string Sounding_Symbol;
        #endregion

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;

        public List<DCC.DrawCommand> Command;
    }
}