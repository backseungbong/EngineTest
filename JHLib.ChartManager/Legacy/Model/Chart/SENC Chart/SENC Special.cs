namespace Legacy.ECM_Core.Chart
{
    public struct SencSpecial
    {
        public DCC.FRID FRID;
        public byte RESARE;

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;
    }
}