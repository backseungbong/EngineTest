namespace Legacy.ECM_Core.Chart
{
    public struct SencSafety
    {
        public DCC.FRID FRID;
        public float DRVAL1;

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;
    }
}