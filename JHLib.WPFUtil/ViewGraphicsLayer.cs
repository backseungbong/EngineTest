using JHLib.Graphics;
using JHLib.Util.Graphic;
using JHLib.WPFUtil.D3DHosting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JHLib.WPFUtil
{
    public partial class ViewGraphicsLayer : ContentControl
    {
        private readonly Image _image;
        private readonly D3DHost _d3dHost;
        private DoubleBufferedBitmap _dbBitmap;

        private GraphicsLayerManager _manager;
        public GraphicsLayerManager Manager
        {
            get => _manager;
            set
            {
                var manager = _manager;
                if (manager != null)
                {
                    manager.OnTransformChanging -= _d3dHost.UpdateTransform;
                    manager.OnTransformChanged -= _d3dHost.UpdateTransform;
                    manager.OnBackBufferChanged -= _d3dHost.UpdateBackBuffer;
                    manager.SetRenderView(null);
                }

                manager = value;
                if (manager != null)
                {
                    manager.OnTransformChanging += _d3dHost.UpdateTransform;
                    manager.OnTransformChanged += _d3dHost.UpdateTransform;
                    manager.OnBackBufferChanged += _d3dHost.UpdateBackBuffer;
                    manager.SetRenderView(_dbBitmap);
                }
                _manager = manager!;
            }
        }

        public ViewGraphicsLayer()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            var image = new Image
            {
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.None,
            };

            _d3dHost = new D3DHost();
            _image = image;
            _image.Source = _d3dHost.HostImage;

            Content = _image;
            CompositionTarget.Rendering += Rendering;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            var w = (int)sizeInfo.NewSize.Width;
            var h = (int)sizeInfo.NewSize.Height;
            if (w > 0 && h > 0 && w <= 7680 && h <= 7680) // 최대 8k 해상도 제한
            {
                if (_dbBitmap == null || _dbBitmap.SameSize(w, h) == false)
                {
                    _dbBitmap = new(w, h);
                    _manager?.SetRenderView(_dbBitmap);
                }
            }
        }

        private void Rendering(object sender, EventArgs e)
        {
            _d3dHost.UpdateDirtyRect(_dbBitmap);
        }
    }
}