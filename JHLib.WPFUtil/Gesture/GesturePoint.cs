using System.Runtime.CompilerServices;
using System.Windows;

namespace JHLib.WPFUtil.Gesture
{
    public sealed class GesturePoint
    {
        public Point Origin { get; private set; }
        public Point Previous { get; private set; }
        public Point Position { get; private set; }
        public double Distance { get; private set; }
        public Vector Vector => Position - Previous;
        internal bool IsPan => Distance > GesturePanel.TapLimitLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GesturePoint(Point point) => Reset(point);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Reset(Point point)
        {
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                point = default;

            Origin = point;
            Previous = point;
            Position = point;
            Distance = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void MovePosition(Point point)
        {
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                return;

            var prev = Position;
            Previous = IsPan ? prev : Origin;
            Position = point;
            Distance += (point - prev).Length;
        }
    }
}