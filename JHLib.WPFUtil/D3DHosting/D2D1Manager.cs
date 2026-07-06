using JHLib.Util.Graphic;
using JHLib.Util.Matrix;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace JHLib.WPFUtil.D3DHosting
{
    internal class D2D1Manager
    {
        public static readonly ID2D1Factory1 D2DFactory;
        public static readonly IDWriteFactory DWriteFactory;
        public static readonly ID2D1Device D2DDevice;
        public static readonly ID2D1DeviceContext D2DDeviceContext;
        public static readonly ID2D1SolidColorBrush D2DColorBrush;

        static D2D1Manager()
        {
            using var dxgiDevice = D3D11Manager.D3D11Device.QueryInterface<IDXGIDevice>();

            D2D1.D2D1CreateFactory(out D2DFactory).CheckError();
            DWrite.DWriteCreateFactory(out DWriteFactory).CheckError();

            D2DDevice = D2DFactory.CreateDevice(dxgiDevice);
            D2DDeviceContext = D2DDevice.CreateDeviceContext();
            D2DColorBrush = D2DDeviceContext.CreateSolidColorBrush(new Color4(IntColors.Blue.SwitchRB()));
        }

        private ID2D1Bitmap1 _d2dTarget;
        private ID2D1Bitmap1 _d2dBitmap;
        private bool _emptyBitmap;

        public void UnsetSharedSurface()
        {
            D2DDeviceContext.Target = null;

            _d2dTarget?.Dispose();
            _d2dBitmap?.Dispose();
        }

        public void SetSharedSurface(IDXGISurface surfaceTarget, IDXGISurface surfaceBitmap)
        {
            var format = new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied);

            var d2dTarget = D2DDeviceContext.CreateBitmapFromDxgiSurface(surfaceTarget,
                new(format, 96.0f, 96.0f, BitmapOptions.Target | BitmapOptions.CannotDraw));

            var d2dBitmap = D2DDeviceContext.CreateBitmapFromDxgiSurface(surfaceBitmap,
                new(format, 96.0f, 96.0f, BitmapOptions.None));

            _d2dTarget = d2dTarget;
            _d2dBitmap = d2dBitmap;
            _emptyBitmap = true;

            D2DDeviceContext.Target = d2dTarget;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool DrawToTarget(DoubleBufferedBitmap.FrontBuffer buffer, Transform ft, Transform lt, out bool directCopy)
        {
            if (buffer.NewBuffer || _emptyBitmap)
            {
                var ms = _d2dBitmap.Map(MapOptions.Write | MapOptions.Discard);
                buffer.CopyTo(ms.Bits, ms.Pitch);
                _d2dBitmap.Unmap();
                _emptyBitmap = false;
            }
            buffer.Return();

            Matrix3x2 mtx;
            if (lt != null && lt != ft)
            {
                // 이전 화면 위치 차이가 너무 크지 않은 경우에만 갱신처리
                var t = lt.ToLocal1.Transform64PreFlipY(ft.WorldPosition);
                if (Math.Abs(t.X - ft.PivotPositionX) < 500 && Math.Abs(t.Y - ft.PivotPositionY) < 500)
                {
                    var s = ft.Scale / lt.Scale;
                    var r = lt.Rotation - ft.Rotation;
                    var m = Matrix22D.Create(ft.PivotPositionX, ft.PivotPositionY, r, s, t.X, t.Y);
                    Matrix22D.ToMatrix3x2(m, out mtx, true);
                }
                else
                {
                    directCopy = false;
                    return false;
                }
            }
            else
            {
                mtx = Matrix3x2.Identity;
            }

            if (mtx.IsIdentity)
            {
                directCopy = true;
            }
            else
            {
                var dc = D2DDeviceContext;
                dc.BeginDraw();
                dc.Clear(null);
                dc.Transform = mtx;
                dc.DrawBitmap(_d2dBitmap);
                dc.EndDraw();
                directCopy = false;
            }
            return true;
        }
    }
}