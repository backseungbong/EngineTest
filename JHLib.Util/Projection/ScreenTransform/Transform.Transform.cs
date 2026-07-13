using JHLib.Util.ArrayControl;
using JHLib.Util.Geometry;
using JHLib.Util.Struct;
using JHLib.Util.ThreadSafe;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    public partial class Transform
    {
        /// <summary> World -> Screen 변환 </summary>        
        public Float2D WorldToScreen(in Float2D wp) => _toLocal.TransformPreFlipY(wp);
        public Float2D WorldToScreen(in Double2D wp) => _toLocal.TransformPreFlipY(wp);
        public Float2D WorldToScreen(float wx, float wy) => _toLocal.TransformPreFlipY(wx, wy);
        public Float2D WorldToScreen(double wx, double wy) => _toLocal.TransformPreFlipY(wx, wy);
        public Double2D WorldToScreenD(in Float2D wp) => _toLocal.Transform64PreFlipY(wp);
        public Double2D WorldToScreenD(in Double2D wp) => _toLocal.Transform64PreFlipY(wp);
        public Double2D WorldToScreenD(float wx, float wy) => _toLocal.Transform64PreFlipY(wx, wy);
        public Double2D WorldToScreenD(double wx, double wy) => _toLocal.Transform64PreFlipY(wx, wy);

        /// <summary> World -> Screen 변환 (자체 Array에 변환된 좌표 저장) </summary>    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WorldToScreen(Span<Float2D> wxy)
        {
            if (wxy.Length == 0) return;
            ref var s = ref MemoryMarshal.GetReference(wxy);
            WorldToScreen(ref s, ref s, wxy.Length);
        }

        /// <summary> World -> Screen 변환 (다른 Array에 변환된 좌표 저장) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WorldToScreen(Span<Float2D> wxy, Span<Float2D> sxy)
        {
            if (wxy.Length == 0) return;
            if (wxy.Length <= sxy.Length)
            {
                ref var s = ref MemoryMarshal.GetReference(wxy);
                ref var d = ref MemoryMarshal.GetReference(sxy);
                WorldToScreen(ref s, ref d, wxy.Length);
            }
            else ThrowOverRange();
        }

        /// <summary> World -> Screen 변환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WorldToScreen(ref Float2D src, ref Float2D dst, int len) =>
            _toLocal.TransformPreFlipY(ref src, ref dst, len);

        /// <summary> World -> Screen 변환 (결과는 Float2Dx4 형태로 반환) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WorldToScreen(in FloatRect wrect, out Float2Dx4 path4) =>
            _toLocal.TransformPreFlipY(wrect, out path4);

        /// <summary> World -> Screen 변환 후 회전된 영역을 포함하는 Bound 반환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatRect WorldToScreenBound(in FloatRect wrect)
        {
            WorldToScreen(wrect, out var spath);
            return GeometryHelper.GetBound(spath.AsSpan());
        }


        /// <summary>  World -> Screen 변환 및 중복좌표를 제거. 유효한 좌표들은 앞쪽으로 밀어 압축 </summary>
        /// <param name="dedupeSize">중복 좌표로 판단할 기준 크기 (최소값 1, 만약 2인 경우 2x2 영역 내 1개 포인트만 유지)</param> 
        /// <returns>중복좌표가 제거된 좌표 갯수</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int WorldToScreenDedupe(ref Float2D wxy, ref Float2D sxy, int len, float dedupeSize = 1f)
        {
            WorldToScreen(ref wxy, ref sxy, len);
            return GeometryHelper.DedupePoints(ref sxy, len, dedupeSize);
        }

        /// <summary>  World -> Screen 변환 및 중복좌표를 제거한 새로운 배열을 반환 </summary>
        /// <param name="dedupeSize">중복 좌표로 판단할 기준 크기 (최소값 1, 만약 2인 경우 2x2 영역 내 1개 포인트만 유지)</param> 
        /// <returns>중복좌표가 제거된 화면좌표 배열</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float2D[] WorldToScreenDedupe(List<Float2D> wxy, float dedupeSize = 1f) =>
            WorldToScreenDedupe(CollectionsMarshal.AsSpan(wxy), dedupeSize);

        /// <summary>  World -> Screen 변환 및 중복좌표를 제거한 새로운 배열을 반환 </summary>
        /// <param name="dedupeSize">중복 좌표로 판단할 기준 크기 (최소값 1, 만약 2인 경우 2x2 영역 내 1개 포인트만 유지)</param> 
        /// <returns>중복좌표가 제거된 화면좌표 배열</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Float2D[] WorldToScreenDedupe(Span<Float2D> wxy, float dedupeSize = 1f)
        {
            if (wxy.Length <= 0)
                return [];

            ref var dst = ref ThreadResource.GetBuffer0<Float2D>(wxy.Length);
            var rcount = WorldToScreenDedupe(ref MemoryMarshal.GetReference(wxy), ref dst, wxy.Length, dedupeSize);
            var result = GC.AllocateUninitializedArray<Float2D>(rcount);
            AC.Copy(ref dst, ref MemoryMarshal.GetArrayDataReference(result), rcount);
            return result;
        }



        /// <summary> Screen -> World 변환 </summary>
        public Float2D ScreenToWorld(in Float2D sp) => _toWorld.TransformPostFlipY(sp);
        public Float2D ScreenToWorld(in Double2D sp) => _toWorld.TransformPostFlipY(sp);
        public Float2D ScreenToWorld(float sx, float sy) => _toWorld.TransformPostFlipY(sx, sy);
        public Float2D ScreenToWorld(double sx, double sy) => _toWorld.TransformPostFlipY(sx, sy);
        public Double2D ScreenToWorldD(in Float2D sp) => _toWorld.Transform64PostFlipY(sp);
        public Double2D ScreenToWorldD(in Double2D sp) => _toWorld.Transform64PostFlipY(sp);
        public Double2D ScreenToWorldD(float sx, float sy) => _toWorld.Transform64PostFlipY(sx, sy);
        public Double2D ScreenToWorldD(double sx, double sy) => _toWorld.Transform64PostFlipY(sx, sy);

        /// <summary> Screen -> World 변환 (자체 Array에 변환된 좌표 저장) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScreenToWorld(Span<Float2D> sxy)
        {
            if (sxy.Length == 0) return;
            ref var s = ref MemoryMarshal.GetReference(sxy);
            ScreenToWorld(ref s, ref s, sxy.Length);
        }

        /// <summary> Screen -> World 변환 (다른 Array에 변환된 좌표 저장) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScreenToWorld(Span<Float2D> sxy, Span<Float2D> wxy)
        {
            if (sxy.Length == 0) return;
            if (sxy.Length <= wxy.Length)
            {
                ref var s = ref MemoryMarshal.GetReference(sxy);
                ref var d = ref MemoryMarshal.GetReference(wxy);
                ScreenToWorld(ref s, ref d, sxy.Length);
            }
            else ThrowOverRange();
        }

        /// <summary> Screen -> World 변환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScreenToWorld(ref Float2D src, ref Float2D dst, int len) =>
            _toWorld.TransformPostFlipY(ref src, ref dst, len);

        /// <summary> Screen -> World 변환 (결과는 Float2Dx4 형태로 반환) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScreenToWorld(in FloatRect srect, out Float2Dx4 path4) =>
            _toWorld.TransformPostFlipY(srect, out path4);

        /// <summary> Screen -> World 변환 후 회전된 영역을 포함하는 Bound 반환 </summary>     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FloatRect ScreenToWorldBound(in FloatRect srect)
        {
            ScreenToWorld(srect, out var wpath);
            return GeometryHelper.GetBound(wpath.AsSpan());
        }


        // [bsb 막음]
        /// <summary> WGS84 -> World 변환 </summary>
        //public Float2D WGS84ToWorld(in Float2D w84) => EPSG3857.ToWorld(w84);
        public Float2D WGS84ToWorld(in Float2D w84) => Projection.ToWorld(w84);
        //public Float2D WGS84ToWorld(in Double2D w84) => EPSG3857.ToWorld(w84);
        public Float2D WGS84ToWorld(in Double2D w84) => Projection.ToWorld(new Float2D((float)w84.X, (float)w84.Y));
        //public Float2D WGS84ToWorld(float lon, float lat) => EPSG3857.ToWorld(lon, lat);
        public Float2D WGS84ToWorld(float lon, float lat) => Projection.ToWorld(lon, lat);
        //public Float2D WGS84ToWorld(double lon, double lat) => EPSG3857.ToWorld(lon, lat);
        public Float2D WGS84ToWorld(double lon, double lat) => Projection.ToWorld((float)lon, (float)lat);
        //public Double2D WGS84ToWorldD(in Float2D w84) => EPSG3857.ToWorldD(w84);
        public Double2D WGS84ToWorldD(in Float2D w84) => Projection.ToWorldD(new Double2D(w84.X, w84.Y));
        //public Double2D WGS84ToWorldD(in Double2D w84) => EPSG3857.ToWorldD(w84);
        public Double2D WGS84ToWorldD(in Double2D w84) => Projection.ToWorldD(w84);
        //public Double2D WGS84ToWorldD(float lon, float lat) => EPSG3857.ToWorldD(lon, lat);
        public Double2D WGS84ToWorldD(float lon, float lat) => Projection.ToWorldD(lon, lat);
        //public Double2D WGS84ToWorldD(double lon, double lat) => EPSG3857.ToWorldD(lon, lat);
        public Double2D WGS84ToWorldD(double lon, double lat) => Projection.ToWorldD(lon, lat);

        /// <summary> WGS84 -> World 변환 (자체 Array에 변환된 좌표 저장) </summary>     
        //public void WGS84ToWorld(Span<Float2D> w84) => EPSG3857.ToWorld(w84);
        public void WGS84ToWorld(Span<Float2D> w84)
        {
            for (int i = 0; i < w84.Length; i++) w84[i] = Projection.ToWorld(w84[i]);
        }

        /// <summary> WGS84 -> World 변환 (다른 Array에 변환좌표 저장) </summary>
        //public void WGS84ToWorld(Span<Float2D> w84, Span<Float2D> wxy) => EPSG3857.ToWorld(w84, wxy);
        public void WGS84ToWorld(Span<Float2D> w84, Span<Float2D> wxy)
        {
            for (int i = 0; i < w84.Length; i++) wxy[i] = Projection.ToWorld(w84[i]);
        }



        /// <summary> World -> WGS84 변환 </summary> 
        //public Float2D WorldToWGS84(in Float2D wp) => EPSG3857.ToWGS84(wp);
        public Float2D WorldToWGS84(in Float2D wp) => Projection.ToWGS84(wp);
        //public Float2D WorldToWGS84(in Double2D wp) => EPSG3857.ToWGS84(wp);
        public Float2D WorldToWGS84(in Double2D wp) => Projection.ToWGS84(new Float2D((float)wp.X, (float)wp.Y));
        //public Float2D WorldToWGS84(float wx, float wy) => EPSG3857.ToWGS84(wx, wy);
        public Float2D WorldToWGS84(float wx, float wy) => Projection.ToWGS84(wx, wy);
        //public Float2D WorldToWGS84(double wx, double wy) => EPSG3857.ToWGS84(wx, wy);
        public Float2D WorldToWGS84(double wx, double wy) => Projection.ToWGS84((float)wx, (float)wy);
        //public Double2D WorldToWGS84D(in Float2D wp) => EPSG3857.ToWGS84D(wp);
        public Double2D WorldToWGS84D(in Float2D wp) => Projection.ToWGS84D(new Double2D(wp.X, wp.Y));
        //public Double2D WorldToWGS84D(in Double2D wp) => EPSG3857.ToWGS84D(wp);
        public Double2D WorldToWGS84D(in Double2D wp) => Projection.ToWGS84D(wp);
        //public Double2D WorldToWGS84D(float wx, float wy) => EPSG3857.ToWGS84D(wx, wy);
        public Double2D WorldToWGS84D(float wx, float wy) => Projection.ToWGS84D(wx, wy);
        //public Double2D WorldToWGS84D(double wx, double wy) => EPSG3857.ToWGS84D(wx, wy);
        public Double2D WorldToWGS84D(double wx, double wy) => Projection.ToWGS84D(wx, wy);

        /// <summary> World -> WGS84 변환 (자체 Array에 변환된 좌표 저장) </summary>     
        //public void WorldToWGS84(Span<Float2D> wxy) => EPSG3857.ToWGS84(wxy);
        public void WorldToWGS84(Span<Float2D> wxy)
        {
            for (int i = 0; i < wxy.Length; i++) wxy[i] = Projection.ToWGS84(wxy[i]);
        }

        /// <summary> World -> WGS84 변환 (다른 Array에 변환좌표 저장) </summary>
        //public void WorldToWGS84(Span<Float2D> wxy, Span<Float2D> w84) => EPSG3857.ToWGS84(wxy, w84);
        public void WorldToWGS84(Span<Float2D> wxy, Span<Float2D> w84)
        {
            for (int i = 0; i < wxy.Length; i++) w84[i] = Projection.ToWGS84(wxy[i]);
        }


        /// <summary> WGS84 -> Screen 변환 </summary>  
        //public Float2D WGS84ToScreen(in Float2D w84) => EPSG3857.WGS84ToScreen(_toLocal, w84);
        public Float2D WGS84ToScreen(in Float2D w84) => WorldToScreen(Projection.ToWorld(w84));
        //public Float2D WGS84ToScreen(in Double2D w84) => EPSG3857.WGS84ToScreen(_toLocal, w84);
        public Float2D WGS84ToScreen(in Double2D w84) => WorldToScreen(Projection.ToWorld(new Float2D((float)w84.X, (float)w84.Y)));
        //public Float2D WGS84ToScreen(float lon, float lat) => EPSG3857.WGS84ToScreen(_toLocal, lon, lat);
        public Float2D WGS84ToScreen(float lon, float lat) => WorldToScreen(Projection.ToWorld(lon, lat));
        //public Float2D WGS84ToScreen(double lon, double lat) => EPSG3857.WGS84ToScreen(_toLocal, lon, lat);
        public Float2D WGS84ToScreen(double lon, double lat) => WorldToScreen(Projection.ToWorld((float)lon, (float)lat));
        //public Double2D WGS84ToScreenD(in Float2D w84) => EPSG3857.WGS84ToScreenD(_toLocal, w84);
        public Double2D WGS84ToScreenD(in Float2D w84) => WorldToScreenD(Projection.ToWorld(w84));
        //public Double2D WGS84ToScreenD(in Double2D w84) => EPSG3857.WGS84ToScreenD(_toLocal, w84);
        public Double2D WGS84ToScreenD(in Double2D w84) => WorldToScreenD(Projection.ToWorldD(w84));
        //public Double2D WGS84ToScreenD(float lon, float lat) => EPSG3857.WGS84ToScreenD(_toLocal, lon, lat);
        public Double2D WGS84ToScreenD(float lon, float lat) => WorldToScreenD(Projection.ToWorldD(lon, lat));
        //public Double2D WGS84ToScreenD(double lon, double lat) => EPSG3857.WGS84ToScreenD(_toLocal, lon, lat);
        public Double2D WGS84ToScreenD(double lon, double lat) => WorldToScreenD(Projection.ToWorldD(lon, lat));

        /// <summary> WGS84 -> Screen 변환 (자체 Array에 변환된 좌표 저장) </summary>  
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void WGS84ToScreen(Span<Float2D> w84) =>
        //    EPSG3857.WGS84ToScreen(_toLocal, w84);
        public void WGS84ToScreen(Span<Float2D> w84)
        {
            for (int i = 0; i < w84.Length; i++)
                w84[i] = _toLocal.TransformPreFlipY(Projection.ToWorld(w84[i]));
        }

        /// <summary> WGS84 -> Screen 변환 (다른 Array에 변환된 좌표 저장) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void WGS84ToScreen(Span<Float2D> w84, Span<Float2D> sxy) =>
        //    EPSG3857.WGS84ToScreen(_toLocal, w84, sxy);
        public void WGS84ToScreen(Span<Float2D> w84, Span<Float2D> sxy)
        {
            for (int i = 0; i < w84.Length; i++)
                sxy[i] = _toLocal.TransformPreFlipY(Projection.ToWorld(w84[i]));
        }

        /// <summary> WGS84 -> Screen 변환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void WGS84ToScreen(ref Float2D w84, ref Float2D sxy, int count) =>
        //    EPSG3857.WGS84ToScreen(_toLocal, ref w84, ref sxy, count);
        public void WGS84ToScreen(ref Float2D w84, ref Float2D sxy, int count)
        {
            var srcSpan = MemoryMarshal.CreateSpan(ref w84, count);
            var dstSpan = MemoryMarshal.CreateSpan(ref sxy, count);
            for (int i = 0; i < count; i++)
                dstSpan[i] = _toLocal.TransformPreFlipY(Projection.ToWorld(srcSpan[i]));
        }

        /// <summary> WGS84 -> Screen 변환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void WGS84ToScreen(in FloatRect w84rect, out Float2Dx4 path4) =>
        //    EPSG3857.WGS84ToScreen(_toLocal, w84rect, out path4);
        public void WGS84ToScreen(in FloatRect w84rect, out Float2Dx4 path4)
        {
            Unsafe.SkipInit(out path4);
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<Float2Dx4, Float2D>(ref path4), 4);
            // 극방위 등 직사각형이 유지되지 않는 도법을 위해 4꼭지점을 각각 투영하여 스크린으로 변환합니다.
            span[0] = _toLocal.TransformPreFlipY(Projection.ToWorld(w84rect.X1, w84rect.Y1));
            span[1] = _toLocal.TransformPreFlipY(Projection.ToWorld(w84rect.X2, w84rect.Y1));
            span[2] = _toLocal.TransformPreFlipY(Projection.ToWorld(w84rect.X2, w84rect.Y2));
            span[3] = _toLocal.TransformPreFlipY(Projection.ToWorld(w84rect.X1, w84rect.Y2));
        }



        /// <summary> Screen -> WGS84 변환 </summary>
        //public Float2D ScreenToWGS84(in Float2D sp) => EPSG3857.ScreenToWGS84(_toWorld, sp);
        //public Float2D ScreenToWGS84(in Double2D sp) => EPSG3857.ScreenToWGS84(_toWorld, sp);
        //public Float2D ScreenToWGS84(float sx, float sy) => EPSG3857.ScreenToWGS84(_toWorld, sx, sy);
        //public Float2D ScreenToWGS84(double sx, double sy) => EPSG3857.ScreenToWGS84(_toWorld, sx, sy);
        //public Double2D ScreenToWGS84D(in Float2D sp) => EPSG3857.ScreenToWGS84D(_toWorld, sp);
        //public Double2D ScreenToWGS84D(in Double2D sp) => EPSG3857.ScreenToWGS84D(_toWorld, sp);
        //public Double2D ScreenToWGS84D(float sx, float sy) => EPSG3857.ScreenToWGS84D(_toWorld, sx, sy);
        //public Double2D ScreenToWGS84D(double sx, double sy) => EPSG3857.ScreenToWGS84D(_toWorld, sx, sy);

        public Float2D ScreenToWGS84(in Float2D sp) => Projection.ToWGS84(ScreenToWorld(sp));
        public Float2D ScreenToWGS84(in Double2D sp) => Projection.ToWGS84(ScreenToWorld(new Float2D((float)sp.X, (float)sp.Y)));
        public Float2D ScreenToWGS84(float sx, float sy) => Projection.ToWGS84(ScreenToWorld(sx, sy));
        public Float2D ScreenToWGS84(double sx, double sy) => Projection.ToWGS84(ScreenToWorld((float)sx, (float)sy));
        public Double2D ScreenToWGS84D(in Float2D sp) => Projection.ToWGS84D(ScreenToWorldD(sp));
        public Double2D ScreenToWGS84D(in Double2D sp) => Projection.ToWGS84D(ScreenToWorldD(sp));
        public Double2D ScreenToWGS84D(float sx, float sy) => Projection.ToWGS84D(ScreenToWorldD(sx, sy));
        public Double2D ScreenToWGS84D(double sx, double sy) => Projection.ToWGS84D(ScreenToWorldD(sx, sy));

        /// <summary> Screen -> WGS84 변환 (자체 Array에 변환된 좌표 저장) </summary>    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void ScreenToWGS84(Span<Float2D> sxy) =>
        //    EPSG3857.ScreenToWGS84(_toWorld, sxy);
        public void ScreenToWGS84(Span<Float2D> sxy)
        {
            for (int i = 0; i < sxy.Length; i++)
                sxy[i] = Projection.ToWGS84(_toWorld.TransformPostFlipY(sxy[i]));
        }

        /// <summary> Screen -> WGS84 변환 (다른 Array에 변환된 좌표 저장) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void ScreenToWGS84(Span<Float2D> sxy, Span<Float2D> w84) =>
        //    EPSG3857.ScreenToWGS84(_toWorld, sxy, w84);
        public void ScreenToWGS84(Span<Float2D> sxy, Span<Float2D> w84)
        {
            for (int i = 0; i < sxy.Length; i++)
                w84[i] = Projection.ToWGS84(_toWorld.TransformPostFlipY(sxy[i]));
        }

        /// <summary> Screen -> WGS84 변환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void ScreenToWGS84(ref Float2D sxy, ref Float2D w84, int count) =>
        //    EPSG3857.ScreenToWGS84(_toWorld, ref sxy, ref w84, count);
        public void ScreenToWGS84(ref Float2D sxy, ref Float2D w84, int count)
        {
            var srcSpan = MemoryMarshal.CreateSpan(ref sxy, count);
            var dstSpan = MemoryMarshal.CreateSpan(ref w84, count);
            for (int i = 0; i < count; i++)
                dstSpan[i] = Projection.ToWGS84(_toWorld.TransformPostFlipY(srcSpan[i]));
        }

        /// <summary> Screen -> WGS84 변환 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void ScreenToWGS84(in FloatRect srect, out Float2Dx4 path4) =>
        //    EPSG3857.ScreenToWGS84(_toWorld, srect, out path4);
        public void ScreenToWGS84(in FloatRect srect, out Float2Dx4 path4)
        {
            ScreenToWorld(srect, out path4); // 스크린 사각형을 월드 4꼭지점으로 변환 (기존 로직 활용)
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<Float2Dx4, Float2D>(ref path4), 4);
            for (int i = 0; i < 4; i++)
                span[i] = Projection.ToWGS84(span[i]); // 월드 4꼭지점을 각각 WGS84로 역산
        }



        /// <summary> [화면좌표 기반] Great Circle 방식, 두 지점사이의 거리 계산 </summary> 
        /// <param name="sp1">위치1 (화면좌표)</param>
        /// <param name="sp2">위치2 (화면좌표)</param>
        /// <returns>두 지점사이의 거리 (미터, meter)</returns>
        public double GCDistanceFromScreen(in Float2D sp1, in Float2D sp2) =>
            GCDistanceFromScreen(sp1.X, sp1.Y, sp2.X, sp2.Y);

        /// <summary> [화면좌표 기반] Great Circle 방식, 두 지점사이의 거리 계산 </summary> 
        /// <param name="sx1">위치1 X(화면좌표)</param>
        /// <param name="sy1">위치1 Y(화면좌표)</param>
        /// <param name="sx2">위치2 X(화면좌표)</param>
        /// <param name="sy2">위치2 Y(화면좌표)</param>
        /// <returns>두 지점사이의 거리 (미터, meter)</returns>
        public double GCDistanceFromScreen(double sx1, double sy1, double sx2, double sy2)
        {
            var p1 = ScreenToWGS84D(sx1, sy1);
            var p2 = ScreenToWGS84D(sx2, sy2);
            return (float)GreatCircle.Distance(p1.X, p1.Y, p2.X, p2.Y);
        }


        /// <summary> [월드좌표 기반] Great Circle 방식, 두 지점사이의 거리 계산 </summary> 
        /// <param name="wp1">위치1 (월드좌표)</param>
        /// <param name="wp2">위치2 (월드좌표)</param>
        /// <returns>두 지점사이의 거리 (미터, meter)</returns>
        public double GCDistanceFromWorld(in Float2D wp1, in Float2D wp2) =>
            GCDistanceFromWorld(wp1.X, wp1.Y, wp2.X, wp2.Y);

        /// <summary> [월드좌표 기반] Great Circle 방식, 두 지점사이의 거리 계산 </summary> 
        /// <param name="wx1">위치1 X(월드좌표)</param>
        /// <param name="wy1">위치1 Y(월드좌표)</param>
        /// <param name="wx2">위치2 X(월드좌표)</param>
        /// <param name="wy2">위치2 Y(월드좌표)</param>
        /// <returns>두 지점사이의 거리 (미터, meter)</returns>
        public double GCDistanceFromWorld(double wx1, double wy1, double wx2, double wy2)
        {
            var p1 = WorldToWGS84D(wx1, wy1);
            var p2 = WorldToWGS84D(wx2, wy2);
            return (float)GreatCircle.Distance(p1.X, p1.Y, p2.X, p2.Y);
        }


        /// <summary> [WGS84 기반] Great Circle 방식, 두 지점사이의 거리 계산 </summary> 
        /// <param name="lonlat1">위치1 (WGS84좌표)</param>
        /// <param name="lonlat2">위치2 (WGS84좌표)</param>
        /// <returns>두 지점사이의 거리 (미터, meter)</returns>
        public double GCDistanceFromWGS84(in Float2D lonlat1, in Float2D lonlat2) =>
            GCDistanceFromWGS84(lonlat1.X, lonlat1.Y, lonlat2.X, lonlat2.Y);

        /// <summary> [WGS84 기반] Great Circle 방식, 두 지점사이의 거리 계산 </summary> 
        /// <param name="lon1">위치1 lon(WGS84좌표)</param>
        /// <param name="lat1">위치1 lat(WGS84좌표)</param>
        /// <param name="lon2">위치2 lon(WGS84좌표)</param>
        /// <param name="lat2">위치2 lat(WGS84좌표)</param>
        /// <returns>두 지점사이의 거리 (미터, meter)</returns>
        public double GCDistanceFromWGS84(double lon1, double lat1, double lon2, double lat2) =>
            (float)GreatCircle.Distance(lon1, lat1, lon2, lat2);



        /// <summary> [화면좌표 기반] Great Circle 방식, 시작위치에서부터 특정 각도 및 거리만큼 떨어진 위치 계산 </summary> 
        /// <param name="sp">시작위치 (화면좌표)</param>
        /// <param name="direction">각도 (북쪽을 기준으로 시계방향)</param>
        /// <param name="distance">거리 (미터, meter)</param>
        /// <returns> 떨어진 화면 좌표 </returns>
        public Float2D GCDestinationFromScreen(in Float2D sp, double direction, double distance) =>
            GCDestinationFromScreen(sp.X, sp.Y, direction, distance);

        /// <summary> [화면좌표 기반] Great Circle 방식, 시작위치에서부터 특정 각도 및 거리만큼 떨어진 위치 계산 </summary> 
        /// <param name="sx">시작위치 X(화면좌표)</param>
        /// <param name="sy">시작위치 Y(화면좌표)</param>
        /// <param name="direction">각도 (북쪽을 기준으로 시계방향)</param>
        /// <param name="distance">거리 (미터, meter)</param>
        /// <returns> 떨어진 화면 좌표 </returns>
        public Float2D GCDestinationFromScreen(double sx, double sy, double direction, double distance)
        {
            var w84 = ScreenToWGS84D(sx, sy);
            GreatCircle.Destination(w84.X, w84.Y, direction, distance, out var newLon, out var newLat);
            return WGS84ToScreen(newLon, newLat);
        }


        /// <summary> [월드좌표 기반] Great Circle 방식, 시작위치에서부터 특정 각도 및 거리만큼 떨어진 위치 계산 </summary> 
        /// <param name="wp">시작위치 (월드좌표)</param>
        /// <param name="direction">각도 (북쪽을 기준으로 시계방향)</param>
        /// <param name="distance">거리 (미터, meter)</param>
        /// <returns> 떨어진 월드 좌표 </returns>
        public Float2D GCDestinationFromWorld(in Float2D wp, double direction, double distance) =>
            GCDestinationFromWorld(wp.X, wp.Y, direction, distance);

        /// <summary> [월드좌표 기반] Great Circle 방식, 시작위치에서부터 특정 각도 및 거리만큼 떨어진 위치 계산 </summary> 
        /// <param name="wx">시작위치 X(월드좌표)</param>
        /// <param name="wy">시작위치 Y(월드좌표)</param>
        /// <param name="direction">각도 (북쪽을 기준으로 시계방향)</param>
        /// <param name="distance">거리 (미터, meter)</param>
        /// <returns> 떨어진 월드 좌표 </returns>
        public Float2D GCDestinationFromWorld(double wx, double wy, double direction, double distance)
        {
            var w84 = WorldToWGS84D(wx, wy);
            GreatCircle.Destination(w84.X, w84.Y, direction, distance, out var newLon, out var newLat);
            return WGS84ToWorld(newLon, newLat);
        }


        /// <summary> [WGS84 기반] Great Circle 방식, 시작위치에서부터 특정 각도 및 거리만큼 떨어진 위치 계산 </summary> 
        /// <param name="lonlat">시작위치 (WGS84)</param>
        /// <param name="direction">각도 (북쪽을 기준으로 시계방향)</param>
        /// <param name="distance">거리 (미터, meter)</param>
        /// <returns> 떨어진 WGS84 좌표 </returns>
        public Float2D GCDestinationFromWGS84(in Float2D lonlat, double direction, double distance) =>
            GCDestinationFromWGS84(lonlat.X, lonlat.Y, direction, distance);

        /// <summary> [WGS84 기반] Great Circle 방식, 시작위치에서부터 특정 각도 및 거리만큼 떨어진 위치 계산 </summary> 
        /// <param name="lon">시작위치 lon(WGS84)</param>
        /// <param name="lat">시작위치 lat(WGS84)</param>
        /// <param name="direction">각도 (북쪽을 기준으로 시계방향)</param>
        /// <param name="distance">거리 (미터, meter)</param>
        /// <returns> 떨어진 WGS84 좌표 </returns>
        public Float2D GCDestinationFromWGS84(double lon, double lat, double direction, double distance)
        {
            GreatCircle.Destination(lon, lat, direction, distance, out var lon2, out var lat2);
            return new(lon2, lat2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOverRange() =>
            throw new Exception("변환좌표가 저장될 배열의 길이는 원본 배열의 길이보다 크거나 같아야 합니다");
    }
}