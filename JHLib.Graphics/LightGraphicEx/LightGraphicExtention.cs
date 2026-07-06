using JHLib.Util.Graphic.Helper;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Graphics
{
    public static unsafe class LightGraphicExtention
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddWGS84Paths(this PathsManager pm, Float2D[][] paths, Transform transform)
        {
            if (paths != null && paths.Length != 0)
            {
                ref var p = ref MemoryMarshal.GetArrayDataReference(paths);
                ref var e = ref Unsafe.Add(ref p, paths.Length);
                do
                {
                    if (p != null && p.Length >= 2)
                        AddWGS84PathInternal(pm, ref MemoryMarshal.GetArrayDataReference(p), p.Length, transform);
                }
                while (Unsafe.IsAddressLessThan(ref p = ref Unsafe.Add(ref p, 1), ref e));
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddWGS84Path(this PathsManager pm, Float2D[] path, Transform transform)
        {
            if (path != null && path.Length >= 2)
                AddWGS84PathInternal(pm, ref MemoryMarshal.GetArrayDataReference(path), path.Length, transform);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddWorldPaths(this PathsManager pm, Float2D[][] paths, Transform transform)
        {
            if (paths != null && paths.Length != 0)
            {
                ref var p = ref MemoryMarshal.GetArrayDataReference(paths);
                ref var e = ref Unsafe.Add(ref p, paths.Length);
                do
                {
                    if (p != null && p.Length >= 2)
                        AddWorldPathInternal(pm, ref MemoryMarshal.GetArrayDataReference(p), p.Length, transform);
                }
                while (Unsafe.IsAddressLessThan(ref p = ref Unsafe.Add(ref p, 1), ref e));
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddWorldPath(this PathsManager pm, Float2D[] path, Transform transform)
        {
            if (path != null && path.Length >= 2)
                AddWorldPathInternal(pm, ref MemoryMarshal.GetArrayDataReference(path), path.Length, transform);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddWGS84PathInternal(this PathsManager pm, ref Float2D p0, int count, Transform transform)
        {
            ref var d = ref pm.AddPath0Unsafe(count);
            transform.WGS84ToScreen(ref p0, ref d, count);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddWorldPathInternal(this PathsManager pm, ref Float2D p0, int count, Transform transform)
        {
            ref var d = ref pm.AddPath0Unsafe(count);
            transform.WorldToScreen(ref p0, ref d, count);
        }
    }
}