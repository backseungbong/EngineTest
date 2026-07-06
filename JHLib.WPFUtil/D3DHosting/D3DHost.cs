using JHLib.Util.Graphic;
using JHLib.Util.Projection.ScreenTransform;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;

namespace JHLib.WPFUtil.D3DHosting
{
    public class D3DHost
    {
        private readonly D3DImage _d3dImage;
        private readonly D3D9Manager _d3d9Manager;
        private readonly D3D11Manager _d3d11Manager;
        private readonly D2D1Manager _d2d1Manager;

        private readonly AutoResetEvent _updateBitmapEvent;
        private readonly Thread _updateBitmapThread;
        private int _onUpdateEvent;

        private volatile DoubleBufferedBitmap _dbBitmapNew;
        private volatile DoubleBufferedBitmap _dbBitmap;
        private volatile int _frontVersionNew;
        private volatile int _frontVersion;

        private readonly AutoResetEvent _dirtyRectEvent;
        private volatile Transform _lastTransform;
        private volatile int _onDirtyRect;
        public D3DImage HostImage => _d3dImage;
        public D3DHost()
        {
            _d3dImage = new D3DImage();
            _d3d9Manager = new D3D9Manager();
            _d3d11Manager = new D3D11Manager();
            _d2d1Manager = new D2D1Manager();

            _updateBitmapEvent = new AutoResetEvent(false);
            _updateBitmapThread = new Thread(WorkerUpdateSurface) { IsBackground = true };
            _updateBitmapThread.Start();

            //_dirtyRectEvent = new AutoResetEvent(true);
            _d3dImage.IsFrontBufferAvailableChanged += FrontBufferAvailableChanged;
            _frontVersionNew = _d3dImage.IsFrontBufferAvailable ? 1 : 2;
        }

        private void FrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_d3dImage.IsFrontBufferAvailable)
            {
                // Front buffer 사용가능: 이전버전에서 증가된 홀수
                _frontVersionNew = _frontVersionNew + 2 | 1;
                _updateBitmapEvent.Set();
            }
            else
            {
                // Front buffer 사용불가: 이전버전에서 증가된 짝수
                _frontVersionNew = _frontVersionNew + 2 & ~1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Initialize()
        {
            var dbBitmap = _dbBitmapNew;
            if (dbBitmap == null) { return false; }

            var frontVersion = _frontVersionNew;
            if ((frontVersion & 1) == 0) { return false; }

            _d3d11Manager.Initialize((uint)dbBitmap.Width, (uint)dbBitmap.Height, _d3d9Manager, _d2d1Manager, _d3dImage);
            return true;
        }

        private void WorkerUpdateSurface()
        {
            while (true)
            {
                _updateBitmapEvent.WaitOne();
                Interlocked.Exchange(ref _onUpdateEvent, 0);

                var dbBitmap = _dbBitmapNew;
                var frontVersion = _frontVersionNew;
                if ((dbBitmap == _dbBitmap && frontVersion == _frontVersion) || Initialize())
                {
                    //if (_dirtyRectEvent.WaitOne())
                    {
                        var transform = dbBitmap.GetFrontbuffer(out var buffer);
                        if (transform != null)
                        {
                            if (_d2d1Manager.DrawToTarget(buffer, transform, _lastTransform, out var directCopy))
                            {
                                _d3d11Manager.UpdateSurface(directCopy);
                                _dbBitmap = dbBitmap;
                                _frontVersion = frontVersion;
                                _onDirtyRect = 1;
                            }
                        }
                    }
                }
            }
        }

        public void UpdateBackBuffer(DoubleBufferedBitmap dbBitmap)
        {
            _dbBitmapNew = dbBitmap;
            if (Interlocked.Exchange(ref _onUpdateEvent, 1) == 0)
                _updateBitmapEvent.Set();
        }

        public void UpdateTransform(Transform tranform)
        {
            _lastTransform = tranform;
            if (Interlocked.Exchange(ref _onUpdateEvent, 1) == 0)
                _updateBitmapEvent.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateDirtyRect(DoubleBufferedBitmap dbBitmap)
        {
            if (_onDirtyRect != 0)
                UpdateDirtyRectInternal(dbBitmap);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdateDirtyRectInternal(DoubleBufferedBitmap dbBitmap)
        {
            if (dbBitmap == _dbBitmap && _frontVersionNew == _frontVersion)
            {
                var d3dImage = _d3dImage;
                d3dImage.Lock();
                d3dImage.AddDirtyRect(new(0, 0, dbBitmap.Width, dbBitmap.Height));
                d3dImage.Unlock();
            }
            //_dirtyRectEvent.Set();
            _onDirtyRect = 0;
        }
    }
}