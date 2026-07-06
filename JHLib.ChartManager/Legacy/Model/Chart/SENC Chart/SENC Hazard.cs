namespace Legacy.ECM_Core.Chart
{
    public struct SencHazard
    {
        public DCC.FRID FRID;
        public float DEPTH;

        public List<SCE.SencPoint> Point;
        public List<SCE.SencEdge> Edge;
        public List<SCE.SencShape> Shape;

        public List<SCE.SencHazardSound> SOUNDG;
    }
}