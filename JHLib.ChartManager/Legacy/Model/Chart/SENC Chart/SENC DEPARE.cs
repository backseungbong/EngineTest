namespace Legacy.ECM_Core.Chart
{
    public struct SencDepare
    {
        public uint RCID;
        public float DRVAL1;
        public float DRVAL2;

        public int Minimum_Scale;
        public byte Display_Group;
        public byte Radar_Overlay;

        public byte Update_Type;

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;

        public List<DCC.EdgeAttribute> Edge_Attribute;

        public List<DCC.DrawCommand> Command;
    }
}