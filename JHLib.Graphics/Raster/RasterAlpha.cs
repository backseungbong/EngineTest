using JHLib.Graphics.SkiaExtention;
using JHLib.Util.Graphic;
using JHLib.Util.Pool;
using JHLib.Util.Simd;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Graphics.Raster
{
    /// <summary>
    /// 이미지를 레스터 형태로 그리는 클래스 <para/>
    /// 이미지의 알파값들을 종합하여 최적화한뒤 <para/>
    /// 원하는 색상의 이미지로 다시 그릴수 있다 <para/>
    /// 렌더링 엔진을통해 다시 드로잉하는것보다 최대 수십배 이상의 성능을 보인다 <para/>
    /// 사이즈변경이나 회전되지 않는 단일색 고정 심볼이나 텍스트를 빠르게 다시 그리기위해 구현되었다 <para/>
    /// 최대 이미지 크기는 255 x 255 사이즈로 제한된다
    /// </summary>
    public unsafe class RasterAlpha
    {
        private const int MAX_SIZE = 255;
        private const int SIZE_HEADER = 2;
        private const int SIZE_BODY = 3;

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE_HEADER)]
        private readonly struct Header(int y, int count)
        {
            public readonly byte Y = (byte)y;
            public readonly byte Count = (byte)count;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = SIZE_BODY)]
        private readonly struct Body(int xs, int xe, uint alpha)
        {
            public readonly byte XS = (byte)xs;
            public readonly byte XE = (byte)xe;
            public readonly byte Alpha = (byte)alpha;
        }

        public static readonly RasterAlpha Empty = new(null, float.NaN, float.NaN);

        private static readonly bool IsABGR =
            SkiaSharp.SKImageInfo.PlatformColorType == SkiaSharp.SKColorType.Rgba8888;

        private readonly byte[] _stream;
        private readonly int _width;
        private readonly int _height;
        private float _pivotX;
        private float _pivotY;

        public int Width => _width;
        public int Height => _height;
        public float PivotX { get => _pivotX; set { if (_width != 0) _pivotX = value; } }
        public float PivotY { get => _pivotY; set { if (_height != 0) _pivotY = value; } }
        public RasterAlpha(SKCanvasEx cv, float pivotX, float pivotY)
        {
            if (cv != null)
            {
                var w = cv.Width;
                var h = cv.Height;
                if (w > 0 && h > 0)
                {
                    if (w > MAX_SIZE) w = MAX_SIZE;
                    if (h > MAX_SIZE) h = MAX_SIZE;

                    var d0 = (uint*)cv.Bitmap0;
                    var stream = new PoolStream(8192);

                    var xMin = 0;
                    var xMax = 0;
                    var yMin = 0;
                    var yMax = 0;

                    var y = 0;
                    do
                    {
                        var p0 = stream.Occupy(SIZE_HEADER);
                        var r0 = d0 + y * cv.Width;
                        var a0 = r0[0] & 0xF0000000;
                        var x0 = 0;
                        var x = 1;
                        do
                        {
                            var a = r0[x] & 0xF0000000; // 256단계 투명도를 16단계로 축소 (성능을 위해)
                            if (a != a0)
                            {
                                if (a0 != 0)
                                    stream.AddRef<Body>() = new(x0, x, a0 >> 24 | 0x0F);
                                a0 = a;
                                x0 = x;
                            }
                        }
                        while (++x < w);

                        if (a0 != 0)
                        {
                            stream.AddRef<Body>() = new(x0, w, a0 >> 24 | 0x0F);
                            x0 = w;
                        }

                        var l = stream.Position - (p0 + SIZE_HEADER);
                        if (l == 0) stream.Position = p0;
                        else
                        {
                            stream.Ref<Header>(p0) = new(y, l / SIZE_BODY);

                            var xs = stream.Ref<Body>(p0 + SIZE_HEADER).XS;
                            if (p0 == 0)
                            {
                                yMin = y;
                                yMax = y + 1;
                                xMin = xs;
                                xMax = x0;
                            }
                            else
                            {
                                yMax = y + 1;
                                if (xs < xMin) xMin = xs;
                                if (x0 > xMax) xMax = x0;
                            }
                        }
                    }
                    while (++y < h);

                    if (xMax > xMin && yMax > yMin)
                    {
                        if (xMin > 0 || yMin > 0)
                        {
                            pivotX -= xMin;
                            pivotY -= yMin;

                            var xMinMin = (ushort)(xMin << 8 | xMin);
                            ref var t = ref stream.Stream0;
                            ref var f = ref t;
                            ref var e = ref Unsafe.AddByteOffset(ref t, stream.Position);
                            do
                            {
                                GetY(ref t) -= (byte)yMin;
                                t = ref Unsafe.AddByteOffset(ref t, SIZE_HEADER);
                                f = ref Unsafe.AddByteOffset(ref t, GetLen(ref f));
                                do
                                {
                                    GetXSE(ref t) -= xMinMin;
                                    t = ref Unsafe.AddByteOffset(ref t, SIZE_BODY);
                                }
                                while (Unsafe.IsAddressLessThan(ref t, ref f));
                            }
                            while (Unsafe.IsAddressLessThan(ref f, ref e));
                        }

                        _stream = stream.ToArray();
                        _width = xMax - xMin;
                        _height = yMax - yMin;
                        _pivotX = pivotX;
                        _pivotY = pivotY;
                        stream.Dispose();
                        return;
                    }
                    else
                    {
                        stream.Dispose();
                    }
                }
            }
            _pivotX = float.NaN;
            _pivotY = float.NaN;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTo(SKCanvasEx cv, Float2D pos, IntColor color) =>
            DrawTo(cv, pos.X, pos.Y, color, color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTo(SKCanvasEx cv, float drawX, float drawY, IntColor color) =>
            DrawTo(cv, drawX, drawY, color, color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTo(SKCanvasEx cv, float drawX, float drawY, IntColor opaqueColor, IntColor translucentColor)
        {
            if (Sse2.IsSupported) { DrawToSse2(cv, drawX, drawY, opaqueColor, translucentColor); }
            else
            {
                var x1 = (int)(drawX - _pivotX + 0.5f);
                var x2 = x1 + _width;
                if (x2 > 0 && x1 < cv.Width)
                {
                    var y1 = (int)(drawY - _pivotY + 0.5f);
                    var y2 = y1 + _height;
                    if (y2 > 0 && y1 < cv.Height)
                        DrawToInternal(_stream, cv, x1, y1, x2, y2, opaqueColor, translucentColor);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawToSse2(SKCanvasEx cv, float drawX, float drawY, IntColor opaqueColor, IntColor translucentColor)
        {
            var x1 = SIMD.ToIntRound(drawX - _pivotX);
            var x2 = x1 + _width;
            if (x2 > 0 && x1 < cv.Width)
            {
                var y1 = SIMD.ToIntRound(drawY - _pivotY);
                var y2 = y1 + _height;
                if (y2 > 0 && y1 < cv.Height)
                    DrawToInternal(_stream, cv, x1, y1, x2, y2, opaqueColor, translucentColor);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DrawToInternal(byte[] stream, SKCanvasEx cv, int x1, int y1, int x2, int y2, uint c32_1, uint c32_2)
        {
            if (IsABGR)
            {
                c32_1 = IntColor.SwitchRB(c32_1);
                c32_2 = IntColor.SwitchRB(c32_2);
            }

            var len = stream.Length;
            fixed (byte* s0 = &MemoryMarshal.GetArrayDataReference(stream))
            {
                LightGraphic.DrawRasterAlpha(s0, len, cv.Bitmap0, cv.Width, cv.Height, x1, y1, x2, y2, c32_1, c32_2);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte GetY(ref byte p) => ref p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref ushort GetXSE(ref byte p) => ref Unsafe.As<byte, ushort>(ref p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetLen(ref byte p) => Unsafe.AddByteOffset(ref p, 1) * SIZE_BODY;
    }
}