namespace Legacy.ECM_Core.Chart
{
    public struct SencUnsare
    {
        public uint RCID;

        public int Minimum_Scale;
        public byte Display_Group;
        public byte Radar_Overlay;

        public byte Update_Type;

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;

        public List<DCC.DrawCommand> Command;
    }
}