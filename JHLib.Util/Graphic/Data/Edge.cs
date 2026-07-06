using System.Runtime.CompilerServices;

namespace JHLib.Util.Graphic.Data
{
    public struct Edge
    {
        public float XMin;
        public float YMin;
        public float XMax;
        public float YMax;
        public Edge(float xmin, float ymin, float xmax, float ymax)
        {
            XMin = xmin; YMin = ymin; XMax = xmax; YMax = ymax;
        }
        public Edge(float init)
        {
            XMin = init; YMin = init; XMax = init; YMax = init;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Slope() { var x = XMin; XMin = x + XMax; return x; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ready(float y)
        {
            var xm = XMin;
            var ym = YMin;
            var dx = XMax - xm;
            var dy = YMax - ym;
            XMin = (y - ym) / dy * dx + xm;
            XMax = dx / dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(in Edge a, in Edge b) => a.YMin <= b.YMin;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(in Edge a, in Edge b) => a.YMin >= b.YMin;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(in Edge a, in Edge b) => a.YMin < b.YMin;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(in Edge a, in Edge b) => a.YMin > b.YMin;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(in Edge a, float b) => a.YMin < b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(in Edge a, float b) => a.YMin > b;
    }
}