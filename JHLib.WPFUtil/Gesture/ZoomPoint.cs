using System.Windows;

namespace JHLib.WPFUtil.Gesture
{
    public sealed class ZoomPoint(Point origin, Point offset, double scale, ZoomType type = ZoomType.Wheel)
    {
        public readonly Point Origin = origin;
        public readonly Point Offset = offset;
        public readonly double Scale = scale;
        public readonly ZoomType Type = type;
    }
}