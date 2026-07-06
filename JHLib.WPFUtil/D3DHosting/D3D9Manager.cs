using System.Windows.Interop;
using Vortice.Direct3D9;

namespace JHLib.WPFUtil.D3DHosting
{
    internal class D3D9Manager
    {
        private static readonly IDirect3D9Ex D3D9Context;
        private static readonly IDirect3DDevice9Ex D3D9Device;
        static D3D9Manager()
        {
            D3D9Context = D3D9.Direct3DCreate9Ex();

            if (D3D9Context == null)
                throw new InvalidOperationException("Failed to create Direct3D9Ex context.");

            var presentParams = new PresentParameters
            {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                DeviceWindowHandle = HiddenWindow.Handle,
                PresentationInterval = PresentInterval.Immediate,
                BackBufferFormat = Format.A8R8G8B8, // D3DImage 호환 형식
                BackBufferWidth = 1, // 최소 크기
                BackBufferHeight = 1, // 최소 크기
                BackBufferCount = 1,
                EnableAutoDepthStencil = false // 깊이/스텐실 버퍼 불필요
            };

            D3D9Device = D3D9Context.CreateDeviceEx(
                0, // Adapter.Default(기본 어댑터)
                DeviceType.Hardware, // 하드웨어 가속
                HiddenWindow.Handle,
                CreateFlags.HardwareVertexProcessing,
                presentParams);

            if (D3D9Device == null)
                throw new InvalidOperationException("Failed to create Direct3D9Ex device.");
        }

        private IDirect3DTexture9 _sharedD3D9Texture;
        public void UnsetSharedResource(D3DImage d3dImage)
        {
            d3dImage.Dispatcher.Invoke(() =>
            {
                d3dImage.Lock();
                d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, 0);
                d3dImage.Unlock();

                _sharedD3D9Texture?.Dispose();
            });
        }

        public void SetSharedResource(uint width, uint height, nint sharedHandle, D3DImage d3dImage)
        {
            d3dImage.Dispatcher.Invoke(() =>
            {
                _sharedD3D9Texture = D3D9Device.CreateTexture(
                    width,
                    height,
                    1,
                    Usage.RenderTarget,
                    Format.A8R8G8B8,
                    Pool.Default,
                    ref sharedHandle);

                using var sharedD3D9Texture0 = _sharedD3D9Texture.GetSurfaceLevel(0);

                d3dImage.Lock();
                d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, sharedD3D9Texture0.NativePointer);
                d3dImage.Unlock();
            });
        }
    }
}