namespace Legacy.ECM_Core.Chart
{
    public struct SencDepcnt
    {
        public uint RCID;
        public float VALDCO;

        public int Minimum_Scale;
        public byte Display_Group;
        public byte Radar_Overlay;

        public byte Update_Type;

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;
    }
}