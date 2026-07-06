using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace JHLib.WPFUtil.D3DHosting
{
    internal class D3D11Manager
    {
        public static readonly ID3D11Device D3D11Device;
        public static readonly ID3D11DeviceContext D3D11DeviceContext;
        public static readonly ID3D11DeviceContext3 D3D11DeviceContext3;
        static D3D11Manager()
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            factory.EnumAdapters1(0, out var adapter);

            var result = D3D11.D3D11CreateDevice(
                adapter, // 기본 어댑터 사용
                DriverType.Unknown, // 하드웨어 가속 우선
                DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Singlethreaded,
                null, // 사용할 기능 수준 배열 (null이면 기본값 사용)
                out D3D11Device,
                out D3D11DeviceContext);

            if (result.Failure)
            {
                // 하드웨어 장치 생성 실패 시 WARP (소프트웨어 렌더러)로 대체 시도
                Trace.WriteLine("Hardware device creation failed, trying WARP device.");

                result = D3D11.D3D11CreateDevice(
                    null,
                    DriverType.Warp, // WARP 장치 사용
                    DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Singlethreaded,
                    null,
                    out D3D11Device,
                    out D3D11DeviceContext);

                // WARP 생성도 실패하면 예외 발생
                result.CheckError();
            }

            D3D11DeviceContext3 = D3D11DeviceContext.QueryInterface<ID3D11DeviceContext3>();
        }

        private readonly AutoResetEvent _flushWaitHandle;
        private ID3D11Texture2D _textureForHosting;
        private ID3D11Texture2D _textureForTarget;
        private ID3D11Texture2D _textureForBitmap;
        public D3D11Manager() => _flushWaitHandle = new AutoResetEvent(false);
        public void Initialize(uint w, uint h, D3D9Manager d3d9Manager, D2D1Manager d2d1Manager, D3DImage d3dImage)
        {
            d3d9Manager.UnsetSharedResource(d3dImage);
            d2d1Manager.UnsetSharedSurface();

            _textureForHosting?.Dispose();
            _textureForTarget?.Dispose();
            _textureForBitmap?.Dispose();

            var textureForHosting = D3D11Device.CreateTexture2D(new Texture2DDescription
            {
                Width = w,
                Height = h,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.Shared
            });

            var textureForTarget = D3D11Device.CreateTexture2D(new Texture2DDescription
            {
                Width = w,
                Height = h,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            });

            var textureForBitmap = D3D11Device.CreateTexture2D(new Texture2DDescription
            {
                Width = w,
                Height = h,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.Write,
                MiscFlags = ResourceOptionFlags.None
            });

            using var resourceHost = textureForHosting.QueryInterface<IDXGIResource>();
            using var surfaceTarget = textureForTarget.QueryInterface<IDXGISurface>();
            using var surfaceBitmap = textureForBitmap.QueryInterface<IDXGISurface>();

            d3d9Manager.SetSharedResource(w, h, resourceHost.SharedHandle, d3dImage);
            d2d1Manager.SetSharedSurface(surfaceTarget, surfaceBitmap);

            _textureForHosting = textureForHosting;
            _textureForTarget = textureForTarget;
            _textureForBitmap = textureForBitmap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateSurface(bool directBitmapCopy)
        {
            D3D11DeviceContext3.CopyResource(_textureForHosting, directBitmapCopy ? _textureForBitmap : _textureForTarget);
            D3D11DeviceContext3.Flush1(0, _flushWaitHandle);
            _flushWaitHandle.WaitOne();
        }
    }
}