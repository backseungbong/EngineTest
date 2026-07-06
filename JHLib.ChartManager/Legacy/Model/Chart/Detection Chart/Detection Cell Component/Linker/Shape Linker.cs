namespace Legacy.ECM_Core.DCC
{
    public class ShapeLinker
    {
        public Vector2D Vector_2D;
        public Vector3D Vector_3D;

        public List<EdgeLinker>? Edge;
        public List<SG2D>? Point;



        public ShapeLinker()
        {
            this.Vector_2D.ATVL = 1;
            this.Vector_3D.ATVL = 1;
        }
    }
}