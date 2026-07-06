namespace Legacy.ECM_Core.Chart
{
    public struct SencSlcons
    {
        public uint RCID;
        public byte PRIM;

        public (int X, int Y) Pivot;

        public int Minimum_Scale;
        public byte Display_Group;
        public byte Radar_Overlay;
        public bool Information;

        public byte Update_Type;

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;

        public List<DCC.EdgeCommand> Edge_Command;

        public List<DCC.DrawCommand> Command;
    }
}