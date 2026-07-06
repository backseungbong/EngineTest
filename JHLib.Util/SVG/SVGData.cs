using JHLib.Util.Struct;

namespace JHLib.Util.SVG
{
    public class SVGData
    {
        public string Title;
        public string Desc;
        public float[] ViewBox;
        public SVGGeometry[] Geometrys;
    }

    public abstract class SVGGeometry(SVGGeometryType type)
    {
        public readonly SVGGeometryType GeometryType = type;
        public SVGStyle[] Styles;
    }

    public class SVGRect : SVGGeometry
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public SVGRect() : base(SVGGeometryType.Rect) { }
    }

    public class SVGPath : SVGGeometry
    {
        public Float2D[][] Paths;
        public SVGPath() : base(SVGGeometryType.Path) { }
    }

    public class SVGCircle : SVGGeometry
    {
        public float X;
        public float Y;
        public float Radius;
        public SVGCircle() : base(SVGGeometryType.Circle) { }
    }
}