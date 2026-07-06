using JHLib.Util.Graphic;
using JHLib.Util.Projection.ScreenTransform;
using SkiaSharp;
using System.Runtime.CompilerServices;

namespace JHLib.Graphics
{
    public class GraphicsLayerManager : TransformSetter, IDisposable
    {
        private const int MAX_LAYERCOUNT = 32;

        private readonly Lock _locker;
        private int _isDisposed;

        private readonly HashSet<GraphicsLayer> _pendingAddLayers;
        private readonly HashSet<GraphicsLayer> _pendingRmvLayers;
        private readonly HashSet<GraphicsLayer> _pendingChkLayers;

        private readonly GraphicsLayer[] _bucketLayer;
        private int _bucketLayerCount;

        private readonly GraphicsLayer[] _sortedLayer;
        private int _sortedLayerCount;

        private bool[] _redrawcheck;

        private volatile bool _isChanged;
        private volatile bool _isRelayer;
        private volatile bool _isReindex;
        private int _drawingStep;
        private int _drawingStepNext;

        private DoubleBufferedBitmap _dbBitmapNew;
        private DoubleBufferedBitmap _dbBitmap;
        private GraphicsContext _transformContext;

        internal bool IsChanged => _isChanged;
        internal bool IsDisposed => _isDisposed == 1;


        public event UpdatedTransform OnTransformChanged;
        public event UpdatedBackBuffer OnBackBufferChanged;
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                var bucketLayer = _bucketLayer;
                var bucketLayerCount = _bucketLayerCount;

                for (var i = 0; i < bucketLayerCount; i++)
                    bucketLayer[i].FreeLayer();

                GC.SuppressFinalize(this);
            }
        }

        ~GraphicsLayerManager() => Dispose();
        public GraphicsLayerManager(DrawingStep startDrawingStep = DrawingStep.Step0)
        {
            _locker = new();
            _pendingAddLayers = [];
            _pendingRmvLayers = [];
            _pendingChkLayers = [];
            _bucketLayer = new GraphicsLayer[MAX_LAYERCOUNT];
            _sortedLayer = new GraphicsLayer[MAX_LAYERCOUNT];
            _redrawcheck = new bool[MAX_LAYERCOUNT];
            _transformContext = new();

            _drawingStep = (int)DrawingStep.Step0;
            _drawingStepNext = (int)startDrawingStep;

            RenderingThread.Register(this);
        }

        public void SetDrawingFontNormal(Stream stream) =>
            _transformContext.SetDrawingFontNormal(SKTypeface.FromStream(stream));

        public void SetDrawingFontBold(Stream stream) =>
            _transformContext.SetDrawingFontBold(SKTypeface.FromStream(stream));

        public void SetDrawingFontItalic(Stream stream) =>
            _transformContext.SetDrawingFontItalic(SKTypeface.FromStream(stream));

        public void SetDrawingFontBoldItalic(Stream stream) =>
            _transformContext.SetDrawingFontBoldItalic(SKTypeface.FromStream(stream));

        internal void UpdateLayer()
        {
            _isChanged = false;

            var dbBitmap = _dbBitmapNew;
            if (dbBitmap == null) { return; }
            if (dbBitmap != _dbBitmap)
                Recanvas(dbBitmap);

            if (_isRelayer) Relayer();
            if (_isReindex) Reindex();

            var context = _transformContext;
            var redrawcheck = _redrawcheck;
            var update = false;

            var layer = _sortedLayer;
            int count = _sortedLayerCount;
            if (count != 0)
            {
                var allredraw = false;
                var stepNext = _drawingStepNext;
                if (stepNext != _drawingStep && stepNext <= (int)DrawingStep.Step9)
                {
                    _drawingStep = stepNext;
                    context.DrawStep = (DrawingStep)stepNext;
                    allredraw = true;
                }

                if (context.Transform != _transform)
                {
                    context.Transform = _transform;
                    allredraw = true;
                }

                var i = 0;
                do if (redrawcheck[i] = (layer[i].PopRedraw() || allredraw)) update = true;
                while (++i < count);

                if (update)
                {
                    i = 0;
                    do if (redrawcheck[i]) layer[i].ReadyToDrawingInternal(context);
                    while (++i < count);

                    i = 0;
                    do if (redrawcheck[i]) layer[i].DrawingInternal(context);
                    while (++i < count);

                    if (dbBitmap.GetBackbuffer(out var buffer))
                    {
                        layer[0].BitmapManager.CopyTo(buffer);
                        if (count > 1)
                        {
                            var j = 1;
                            do layer[j].BitmapManager.BlendTo(buffer);
                            while (++j < count);
                        }
                        buffer.Return(context.Transform);
                        OnBackBufferChanged?.Invoke(dbBitmap);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Recanvas(DoubleBufferedBitmap dbBitmap)
        {
            _dbBitmap?.Dispose();
            _dbBitmap = dbBitmap;

            var bucketLayer = _bucketLayer;
            var bucketLayerCount = _bucketLayerCount;

            for (var i = 0; i < bucketLayerCount; i++)
                bucketLayer[i].FreeLayer();

            for (var i = 0; i < bucketLayerCount; i++)
                bucketLayer[i].InitLayer(dbBitmap.Width, dbBitmap.Height);

            SetScreen(dbBitmap.Width, dbBitmap.Height);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Relayer()
        {
            _isRelayer = false;

            var addLayersMap = _pendingAddLayers;
            var rmvLayersMap = _pendingRmvLayers;
            var chkLayersMap = _pendingChkLayers;

            int addCount, rmvCount;
            var addLayers = default(GraphicsLayer[]);
            var rmvLayers = default(GraphicsLayer[]);

            lock (_locker)
            {
                if ((addCount = addLayersMap.Count) != 0)
                {
                    addLayers = new GraphicsLayer[addCount];
                    addLayersMap.CopyTo(addLayers, 0, addCount);
                    addLayersMap.Clear();
                }
                if ((rmvCount = rmvLayersMap.Count) != 0)
                {
                    rmvLayers = new GraphicsLayer[rmvCount];
                    rmvLayersMap.CopyTo(rmvLayers, 0, rmvCount);
                    rmvLayersMap.Clear();
                }
            }

            for (var i = 0; i < rmvCount; i++)
            {
                if (chkLayersMap.Remove(rmvLayers[i]))
                {
                    rmvLayers[i].FreeLayer();
                    rmvLayers[i].Setter = null;
                }
            }

            for (var i = 0; i < addCount; i++)
            {
                if (chkLayersMap.Contains(addLayers[i])) continue;
                if (chkLayersMap.Count >= MAX_LAYERCOUNT)
                    throw new InvalidOperationException($"Layer Manager는 최대 '{MAX_LAYERCOUNT}'레이어로 제한됩니다");

                chkLayersMap.Add(addLayers[i]);
                addLayers[i].InitLayer(_dbBitmap.Width, _dbBitmap.Height);
            }

            var bucketLayer = _bucketLayer;
            var bucketLayerCount = _bucketLayerCount;
            _bucketLayerCount = chkLayersMap.Count;

            chkLayersMap.CopyTo(bucketLayer, 0, chkLayersMap.Count);

            for (var i = chkLayersMap.Count; i < bucketLayerCount; i++)
                bucketLayer[i] = null;

            Reindex();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Reindex()
        {
            _isReindex = false;

            var bucketLayer = _bucketLayer;
            var bucketLayerCount = _bucketLayerCount;
            var sortedLayer = _sortedLayer;
            var sortedLayerCount = _sortedLayerCount;

            var newLayerCount = 0;
            for (var i = 0; i < bucketLayerCount; i++)
            {
                if (bucketLayer[i].Enable)
                {
                    sortedLayer[newLayerCount] = bucketLayer[i];
                    newLayerCount++;
                }
            }
            _sortedLayerCount = newLayerCount;

            for (var i = newLayerCount; i < sortedLayerCount; i++)
                sortedLayer[i] = null;

            if (newLayerCount > 1)
            {
                var i = 0;
                var keys = new int[newLayerCount];
                do keys[i] = sortedLayer[i].ZIndex;
                while (++i < newLayerCount);

                Array.Sort(keys, sortedLayer, 0, newLayerCount, null);
            }
        }

        public void AddLayer(GraphicsLayer layer, int zIndex) { layer.ZIndex = zIndex; AddLayer(layer); }
        public void AddLayer(GraphicsLayer layer)
        {
            if (layer != null)
            {
                lock (layer)
                {
                    if (layer.Setter == null) layer.Setter = this;
                    else if (layer.Setter == this) return;
                    else throw new InvalidOperationException("이 레이어는 이미 소속된 Manager가 존재합니다");
                }

                lock (_locker)
                {
                    _pendingAddLayers.Add(layer);
                    if (_pendingRmvLayers.Count != 0)
                        _pendingRmvLayers.Remove(layer);
                    _isRelayer = true;
                    _isChanged = true;
                }
                RenderingThread.Rerendering();
            }
        }

        public void RemoveLayer(GraphicsLayer layer)
        {
            if (layer != null)
            {
                lock (layer)
                {
                    if (layer.Setter == null) return;
                    else if (layer.Setter != this)
                        throw new InvalidOperationException("이 레이어가 소속된 Manager가 아닙니다");
                }

                lock (_locker)
                {
                    _pendingRmvLayers.Add(layer);
                    if (_pendingAddLayers.Count != 0)
                        _pendingAddLayers.Remove(layer);
                    _isRelayer = true;
                    _isChanged = true;
                }
                RenderingThread.Rerendering();
            }
        }

        protected override void ChangedTransform(Transform transform)
        {
            OnTransformChanged?.Invoke(transform);
            _isChanged = true;
            RenderingThread.Rerendering();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RaiseChanged() { _isChanged = true; RenderingThread.Rerendering(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RaiseReindex() { _isReindex = true; _isChanged = true; RenderingThread.Rerendering(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRenderView(DoubleBufferedBitmap dbBitmap)
        {
            _dbBitmapNew = dbBitmap;
            _isChanged = true;
            RenderingThread.Rerendering();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DrawingStep NextDrawingStep()
        {
            var drawingStep = _drawingStep;
            if (Interlocked.CompareExchange(ref _drawingStepNext, drawingStep + 1, drawingStep) == drawingStep)
            {
                RaiseChanged();
                drawingStep++;
            }
            return (DrawingStep)drawingStep;
        }
    }
}