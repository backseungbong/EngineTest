namespace Legacy.ECM_Core.Chart
{
    public struct SencMeta
    {
        public DCC.FRID FRID;

        public byte Information;
        public bool Low_Accuracy;
        public byte Highlight;
        public byte Viewing_Group;
        public int CSCALE;

        public byte Update_Type;
        public byte Display_Group;
        public byte Radar_Overlay;

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;

        public List<DCC.DrawCommand> Command;
    }
}