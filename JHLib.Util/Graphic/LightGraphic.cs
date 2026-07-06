using JHLib.Util.Geometry;
using JHLib.Util.Graphic.Data;
using JHLib.Util.Graphic.Helper;
using JHLib.Util.List;
using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Graphic
{
    /// <summary>
    /// 자제 제작 그래픽 라이브러리 <para/>
    /// 단순 그리기 및 2D 관련 처리를 일반 엔진보다 빠르기 그리기 위해 구현됨 <para/>
    /// 단독으로 사용하기엔 현재 기능이 부족하고, 메인 그래픽 엔진과 하이브리드 형식으로 사용
    /// </summary>
    public unsafe partial class LightGraphic
    {
        private readonly PathsManager _pathManager;
        private readonly EdgeManager _edgeManager;

        internal void* Bitmap0;
        internal int Width;
        internal int Height;

        internal IntColor FillColorInternal;
        internal IntColor StrokeColorInternal;
        internal float StrokeThicknessInternal;

        /// <summary>
        /// 기본적으로 ARGB 타입이지만 특정 OS(예, 리눅스)의 경우 ABGR 타입일 수 있음 <para/>
        /// 이 타입기반으로 픽셀을 쓰기전 R과 B값을 스위칭할지 여부를 결정
        /// </summary>
        public bool IsAGBRType { get; set; }

        /// <summary> 채우기 색상 설정 </summary>
        public IntColor FillColor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FillColorInternal;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => FillColorInternal = IsAGBRType ? IntColor.SwitchRB(value) : value;
        }

        /// <summary> 선 색상 설정 </summary>
        public IntColor StrokeColor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StrokeColorInternal;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => StrokeColorInternal = IsAGBRType ? IntColor.SwitchRB(value) : value;
        }

        /// <summary> 선 두께 설정 </summary>
        public float StrokeThickness
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StrokeThicknessInternal;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => StrokeThicknessInternal = value;
        }

        public PathsManager PathManager => _pathManager;
        public LightGraphic()
        {
            _pathManager = new PathsManager();
            _edgeManager = Sse2.IsSupported ? new EdgeManager() : null;

            FillColorInternal = IntColors.Black;
            StrokeColorInternal = IntColors.Black;
            StrokeThicknessInternal = 1.0f;

            IsAGBRType = false;
        }

        /// <summary> 그려질 캔버스의 메모리 공간포인터와 너비, 높이를 설정한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCanvasInfo(void* bitmap0, int width, int height)
        {
            Bitmap0 = bitmap0;
            Width = width;
            Height = height;
        }

        /// <summary> 입력된 데이타를 초기화 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (Sse2.IsSupported)
                _edgeManager.Clear();
            else
                NativeGraphic.path_clear();
        }

        /// <summary> 사각 영역 데이타를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRect(in FloatRect rect) => AddRect(rect.X1, rect.Y1, rect.X2, rect.Y2);

        /// <summary> 사각 영역 데이타를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRect(float x1, float y1, float x2, float y2)
        {
            if (Sse2.IsSupported)
                _edgeManager.AddRect(x1, y1, x2, y2);
            else
                NativeGraphic.path_add_rect(x1, y1, x2, y2);
        }

        /// <summary> 포인트 데이타를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPath(in Span<Float2D> path) => AddPath(ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 포인트 데이타를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPath(ref Float2D path0, int pathn) { fixed (Float2D* p0 = &path0) { AddPath(p0, pathn); return; } }

        /// <summary> 포인트 데이타를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPath(Float2D* p0, int pn)
        {
            if (pn < 2) return;
            if (Sse2.IsSupported)
                _edgeManager.AddPoints(p0, pn, 0, Height);
            else
                NativeGraphic.path_add(p0, pn, 0, Height);
        }

        /// <summary> 전체영역 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillAll()
        {
            if (Sse2.IsSupported)
                PolygonX8664.AllFill(Bitmap0, Width, Height, FillColorInternal);
            else
                NativeGraphic.all_fill(Bitmap0, Width, Height, FillColorInternal);
        }

        /// <summary> 사각 영역 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(in FloatRect rect) => Fill(rect.X1, rect.Y1, rect.X2, rect.Y2);

        /// <summary> 사각 영역 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Fill(float x1, float y1, float x2, float y2)
        {
            var w = Width;
            var h = Height;
            var wf = (float)w;
            var hf = (float)h;

            if (x1 < x2 && y1 < y2 && x2 > 0f && y2 > 0f && x1 < wf && y1 < hf)
            {
                var b0 = Bitmap0;
                var c32 = FillColorInternal;

                if (Sse2.IsSupported)
                {
                    var xs = SIMD.ToIntRound(SIMD.Max(0f, x1));
                    var xe = SIMD.ToIntRound(SIMD.Min(wf, x2));
                    var ys = SIMD.ToIntRound(SIMD.Max(0f, y1));
                    var ye = SIMD.ToIntRound(SIMD.Min(hf, y2));

                    if (xs < xe && ys < ye)
                    {
                        var dy = ye - ys;
                        var dx = xe - xs;
                        if (dx == w)
                            PolygonX8664.AllFill((uint*)b0 + (ys * w), w, dy, c32);
                        else
                            PolygonX8664.RectFill(b0, w, h, c32, xs, dx, ys, dy);
                    }
                }
                else
                {
                    var xs = x1 < 0f ? 0 : (int)MathF.Round(x1);
                    var xe = x2 > wf ? w : (int)MathF.Round(x2);
                    var ys = y1 < 0f ? 0 : (int)MathF.Round(y1);
                    var ye = y2 > hf ? h : (int)MathF.Round(y2);

                    if (xs < xe && ys < ye)
                    {
                        var dy = ye - ys;
                        var dx = xe - xs;
                        if (dx == w)
                            NativeGraphic.all_fill((uint*)b0 + (ys * w), w, dy, c32);
                        else
                            NativeGraphic.rect_fill(b0, w, h, c32, xs, dx, ys, dy);
                    }
                }
            }
        }

        /// <summary> 지정 포인트 데이타로 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(Float2D[] path) => Fill(ref MemoryMarshal.GetArrayDataReference(path), path.Length);

        /// <summary> 지정 포인트 데이타로 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(in Span<Float2D> path) => Fill(ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 지정 포인트 데이타로 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(ref Float2D path0, int pathn) { fixed (Float2D* p = &path0) { Fill(p, pathn); return; } }

        /// <summary> 지정 포인트 데이타로 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Fill(Float2D* p0, int pn)
        {
            if (pn < 2) return;
            if (Sse2.IsSupported)
            {
                var em = _edgeManager;
                em.AddPoints(p0, pn, 0, Height);
                PolygonX8664.PathFill(em, Bitmap0, Width, Height, FillColorInternal);
            }
            else
            {
                NativeGraphic.path_fill_simple(p0, pn, Bitmap0, Width, Height, FillColorInternal);
            }
        }

        /// <summary> 이전에 입력한 데이타로 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Fill()
        {
            if (Sse2.IsSupported)
                PolygonX8664.PathFill(_edgeManager, Bitmap0, Width, Height, FillColorInternal);
            else
                NativeGraphic.path_fill(Bitmap0, Width, Height, FillColorInternal);
        }

        /// <summary> PathsManager 데이타로 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Fill(PathsManager pm)
        {
            var h = Height;
            if (Sse2.IsSupported)
            {
                var em = _edgeManager; em.Clear();
                foreach (var path in pm)
                {
                    fixed (Float2D* p0 = &MemoryMarshal.GetReference(path))
                    {
                        if (path.Length < 2) continue;
                        em.AddPoints(p0, path.Length, 0, h);
                    }
                }
                PolygonX8664.PathFill(em, Bitmap0, Width, h, FillColorInternal);
            }
            else
            {
                NativeGraphic.path_clear();
                foreach (var path in pm)
                {
                    fixed (Float2D* p0 = &MemoryMarshal.GetReference(path))
                    {
                        if (path.Length < 2) continue;
                        NativeGraphic.path_add(p0, path.Length, 0, h);
                    }
                }
                NativeGraphic.path_fill(Bitmap0, Width, h, FillColorInternal);
            }
        }

        /// <summary> 지정 포인트 데이타로 선그리기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(float x1, float y1, float x2, float y2) => Draw(new Float2D(x1, y1), new Float2D(x2, y2));

        /// <summary> 지정 포인트 데이타로 선그리기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(in Float2D p1, in Float2D p2) => StrokeInternal.DrawLine(p1, p2, this);


        /// <summary> 지정 포인트 데이타로 선그리기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(Float2D[] points) => Draw(ref MemoryMarshal.GetArrayDataReference(points), points.Length);

        /// <summary> 지정 포인트 데이타로 선그리기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(in Span<Float2D> path) => Draw(ref MemoryMarshal.GetReference(path), path.Length);

        /// <summary> 지정 포인트 데이타로 선그리기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(ref Float2D path0, int pathn) { fixed (Float2D* p = &path0) { Draw(p, pathn); return; } }

        /// <summary> 지정 포인트 데이타로 선그리기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(Float2D* p0, int pn)
        {
            if (pn >= 2)
                StrokeInternal.DrawPathFlatJoin(p0, pn, p0[0], p0[(uint)pn - 1], this);
        }

        /// <summary> 지정 포인트 데이타로 선그리기를 한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Draw(ref Float2D path0, int pathn, SList<ClippedPath> cpaths)
        {
            fixed (Float2D* p0 = &path0)
            {
                if (cpaths != null && cpaths.Count != 0)
                {
                    foreach (ref var cp in cpaths)
                        Draw(p0, cp);
                }
                else
                {
                    Draw(p0, pathn);
                }
            }
        }

        /// <summary> 지정 포인트 데이타로 선그리기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(Float2D* p0, in ClippedPath cp)
        {
            if (cp.Length <= 0)
                StrokeInternal.DrawLine(cp.Head, cp.Tail, this);
            else
                StrokeInternal.DrawPathFlatJoin(p0 + cp.Offset, cp.Length, cp.Head, cp.Tail, this);
        }

        /// <summary> ScreenPathManager 데이타로 채우기를 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(PathsManager pm)
        {
            foreach (var path in pm)
                Draw(path);
        }
    }
}