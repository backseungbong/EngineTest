using JHLib.Util.Struct;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Graphics.SkiaExtention
{
    public static unsafe class SKPathEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddPoly(this SKPath path, Float2D[] points, bool close = true)
        {
            if (points == null || points.Length == 0) return;
            fixed (Float2D* src = &MemoryMarshal.GetArrayDataReference(points))
            {
                SKApiEx.PathAddPoly(path.Handle, (nint)src, points.Length, close);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddPoly(this SKPath path, ReadOnlySpan<Float2D> points, bool close = true)
        {
            if (points.Length == 0) return;
            fixed (Float2D* src = &MemoryMarshal.GetReference(points))
            {
                SKApiEx.PathAddPoly(path.Handle, (nint)src, points.Length, close);
                return;
            }
        }
    }
}