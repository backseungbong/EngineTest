using JHLib.Graphics.Raster;
using JHLib.Graphics.SkiaExtention;
using JHLib.Util.ArrayControl;
using JHLib.Util.DataStream;
using JHLib.Util.Geometry;
using JHLib.Util.Graphic;
using JHLib.Util.List;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Graphics
{
    using static System.Runtime.InteropServices.MemoryMarshal;
    public unsafe partial class GraphicsContext
    {
        private readonly LightGraphic _lightGraphic;
        private readonly SList<Float2D> _pathBuffer;
        private readonly SList<ClippedPath> _cpathBuffer;

        private Float2D[] _pathBufferInternal;
        private int _pathBufferInternalCapacity;
        private Float2D[] _pathBufferExternal;
        private int _pathBufferExternalCapacity;

        private Transform _transform;
        private SKCanvasEx _lcanvas;
        private SKCanvasEx _tcanvas;
        private SKCanvasEx _prevCanvas;
        private nint _tHandle;

        private readonly SKCanvasEx _cv128x128;
        private readonly SKCanvasEx _cv256x256;
        private readonly SKCanvasEx _cv512x064;

        private readonly SKPath _skpathDrawing;
        private readonly nint _skpathDrawingHandle;
        private readonly SKPath _skpathInternal;
        private readonly nint _skpathInternalHandle;

        private readonly SKTypeface[] _faceDefault;
        private readonly SKFont _fontDefault;
        private readonly nint _fontHandle;

        private readonly SKPaint _pStroke;
        private readonly nint _pStrokeHandle;
        private readonly SKPaint _pFill;
        private readonly nint _pFillHandle;
        private readonly SKPaint _pImage;
        private readonly nint _pImageHandle;
        private readonly SKPaint _pText;
        private readonly nint _pTextHandle;
        private nint _hDashEffect;

        private bool _lastSetMatrix;

        private float _lastTextSize;
        private uint _lastTextColor;
        private SKBlendMode _lastTextBlend;
        private SKTextFace _lastTextFace;

        private uint _lastFillColor;
        private bool _lastFillIsAntialias;
        private SKBlendMode _lastFillBlend;
        private SKShader _lastFillShader;

        private uint _lastStrokeColor;
        private float _lastStrokeWidth;
        private bool _lastStrokeIsAntialias;
        private SKStrokeCap _lastStrokeCap;
        private SKStrokeJoin _lastStrokeJoin;
        private SKBlendMode _lastStrokeBlend;


        /// <summary> 그리기 단계 </summary>
        public DrawingStep DrawStep { get; internal set; }

        /// <summary> 현재 콘텍스트 트랜스폼 </summary>
        public Transform Transform { get => _transform; internal set => _transform = value; }

        /// <summary> 
        /// '레이어 캔버스', 레이어별로 각자 가지는 기본 캔버스 <para/>
        /// 교체가 불가능한 기본캔버스
        /// </summary>
        public SKCanvasEx LayerCanvas
        {
            get => _lcanvas;
            internal set
            {
                _transform.SetTransform1();

                var handle = value.Canvas.Handle;
                SKApiEx.ResetMatrix(handle);
                _lastSetMatrix = false;

                _prevCanvas = value;
                _lcanvas = value;
                _tcanvas = value;
                _tHandle = handle;
                _lightGraphic.SetCanvasInfo(value.Bitmap0, value.Width, value.Height);
            }
        }

        /// <summary> 
        /// 그리기작업이 진행될 캔버스 <para/>
        /// Drawing 호출시 '레이어 캔버스'로 기본 할당됨. 타겟 캔버스는 교체하여 사용가능
        /// </summary>
        public SKCanvasEx TargetCanvas
        {
            get => _tcanvas;
            set
            {
                var handle = value.Canvas.Handle;
                SKApiEx.ResetMatrix(handle);
                _lastSetMatrix = false;

                _prevCanvas = _tcanvas;
                _tcanvas = value;
                _tHandle = handle;
                _lightGraphic.SetCanvasInfo(value.Bitmap0, value.Width, value.Height);
            }
        }

        public LightGraphic LightGraphic => _lightGraphic;
        public GraphicsContext()
        {
            _lightGraphic = new LightGraphic() { IsAGBRType = SKImageInfo.PlatformColorType == SKColorType.Rgba8888 };

            _cv128x128 = new SKCanvasEx(128, 128);
            _cv256x256 = new SKCanvasEx(256, 256);
            _cv512x064 = new SKCanvasEx(512, 064);
            _pathBuffer = new SList<Float2D>(64);
            _cpathBuffer = new SList<ClippedPath>(8);

            _pathBufferExternal = GC.AllocateUninitializedArray<Float2D>(4096);
            _pathBufferExternalCapacity = 4096;
            _pathBufferInternal = GC.AllocateUninitializedArray<Float2D>(4096);
            _pathBufferInternalCapacity = 4096;

            _faceDefault = new SKTypeface[4];
            _fontDefault = new SKFont(default, 18);
            _fontHandle = _fontDefault.Handle;

            _skpathDrawing = new SKPath() { FillType = SKPathFillType.EvenOdd };
            _skpathDrawingHandle = _skpathDrawing.Handle;
            _skpathInternal = new SKPath() { FillType = SKPathFillType.EvenOdd };
            _skpathInternalHandle = _skpathInternal.Handle;

            _lastStrokeColor = IntColors.Black;
            _lastStrokeWidth = 1f;
            _lastStrokeIsAntialias = true;
            _lastStrokeCap = SKStrokeCap.Butt;
            _lastStrokeJoin = SKStrokeJoin.Bevel;
            _lastStrokeBlend = SKBlendMode.SrcOver;
            _pStroke = new SKPaint()
            {
                IsAntialias = _lastStrokeIsAntialias,
                IsStroke = true,
                Color = _lastStrokeColor,
                StrokeWidth = _lastStrokeWidth,
                Style = SKPaintStyle.Stroke,
                StrokeCap = _lastStrokeCap,
                StrokeJoin = _lastStrokeJoin,
                BlendMode = _lastStrokeBlend
            };
            _pStrokeHandle = _pStroke.Handle;

            _lastFillColor = IntColors.Black;
            _lastFillIsAntialias = false;
            _lastFillBlend = SKBlendMode.SrcOver;
            _pFill = new SKPaint()
            {
                IsAntialias = _lastFillIsAntialias,
                IsStroke = false,
                Color = _lastFillColor,
                StrokeWidth = 0,
                Style = SKPaintStyle.Fill,
                BlendMode = _lastFillBlend
            };
            _pFillHandle = _pFill.Handle;

            _lastTextSize = 0;
            _lastTextColor = IntColors.Black;
            _lastTextBlend = SKBlendMode.SrcOver;
            _lastTextFace = SKTextFace.NULL;
            _pText = new SKPaint()
            {
                Color = _lastTextColor,
                BlendMode = _lastTextBlend,
                Style = SKPaintStyle.Fill,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                TextEncoding = SKTextEncoding.Utf16,
            };
            _pTextHandle = _pText.Handle;

            _pImage = new SKPaint()
            {
                BlendMode = SKBlendMode.SrcOver,
                FilterQuality = SKFilterQuality.Medium,
            };
            _pImageHandle = _pImage.Handle;
        }

        public void SetDrawingFontNormal(SKTypeface normal)
        {
            _faceDefault[(int)SKTextFace.Normal]?.Dispose();
            _faceDefault[(int)SKTextFace.Normal] = normal;
        }
        public void SetDrawingFontBold(SKTypeface bold)
        {
            _faceDefault[(int)SKTextFace.Bold]?.Dispose();
            _faceDefault[(int)SKTextFace.Bold] = bold;
        }
        public void SetDrawingFontItalic(SKTypeface italic)
        {
            _faceDefault[(int)SKTextFace.Italic]?.Dispose();
            _faceDefault[(int)SKTextFace.Italic] = italic;
        }
        public void SetDrawingFontBoldItalic(SKTypeface boldItalic)
        {
            _faceDefault[(int)SKTextFace.BoldItalic]?.Dispose();
            _faceDefault[(int)SKTextFace.BoldItalic] = boldItalic;
        }


        // ================ PAINT SET ===============

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeColor(SKColor color) => SetStrokeColor((uint)color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeColor(uint color)
        {
            if (_lastStrokeColor != color)
            {
                _lastStrokeColor = color;
                SKApiEx.PaintSetColor(_pStrokeHandle, color);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeWidth(float width)
        {
            if (_lastStrokeWidth != width)
            {
                _lastStrokeWidth = width;
                SKApiEx.PaintSetStrokeWidth(_pStrokeHandle, width);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeCap(SKStrokeCap cap = SKStrokeCap.Butt)
        {
            if (_lastStrokeCap != cap)
            {
                _lastStrokeCap = cap;
                SKApiEx.PaintSetStrokeCap(_pStrokeHandle, cap);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeJoin(SKStrokeJoin join = SKStrokeJoin.Bevel)
        {
            if (_lastStrokeJoin != join)
            {
                _lastStrokeJoin = join;
                SKApiEx.PaintSetStrokeJoin(_pStrokeHandle, join);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeBlend(SKBlendMode blend = SKBlendMode.SrcOver)
        {
            if (_lastStrokeBlend != blend)
            {
                _lastStrokeBlend = blend;
                SKApiEx.PaintSetBlendMode(_pStrokeHandle, blend);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeAntialias(bool isAntialias = true)
        {
            if (_lastStrokeIsAntialias != isAntialias)
            {
                _lastStrokeIsAntialias = isAntialias;
                SKApiEx.PaintSetAntialias(_pStrokeHandle, Unsafe.As<bool, byte>(ref isAntialias));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeCapJoinRoundForce()
        {
            var handle = _pStrokeHandle;
            SKApiEx.PaintSetStrokeCap(handle, SKStrokeCap.Round);
            SKApiEx.PaintSetStrokeJoin(handle, SKStrokeJoin.Round);
            _lastStrokeCap = SKStrokeCap.Round;
            _lastStrokeJoin = SKStrokeJoin.Round;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeCapJoinSquareForce()
        {
            var handle = _pStrokeHandle;
            SKApiEx.PaintSetStrokeCap(handle, SKStrokeCap.Square);
            SKApiEx.PaintSetStrokeJoin(handle, SKStrokeJoin.Miter);
            _lastStrokeCap = SKStrokeCap.Square;
            _lastStrokeJoin = SKStrokeJoin.Miter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeCapJoinFlatForce()
        {
            var handle = _pStrokeHandle;
            SKApiEx.PaintSetStrokeCap(handle, SKStrokeCap.Butt);
            SKApiEx.PaintSetStrokeJoin(handle, SKStrokeJoin.Bevel);
            _lastStrokeCap = SKStrokeCap.Butt;
            _lastStrokeJoin = SKStrokeJoin.Bevel;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeDash(ReadOnlySpan<float> dash, float phase = 0f)
        {
            if (dash.Length == 2) SetStrokeDash(dash[0], dash[1], phase);
            else if (dash.Length == 4) SetStrokeDash(dash[0], dash[1], dash[2], dash[3], phase);
            else
            {
                fixed (float* dash0 = &GetReference(dash))
                {
                    SetStrokeDashInternal(dash0, dash.Length, phase);
                    return;
                }
            }
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeDash(float dash, float gap, float phase = 0f)
        {
            var buffer = new Float2D(dash, gap);
            SetStrokeDashInternal((float*)&buffer, 2, phase);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStrokeDash(float dash1, float gap1, float dash2, float gap2, float phase = 0f)
        {
            var buffer = new FloatRect(dash1, gap1, dash2, gap2);
            SetStrokeDashInternal((float*)&buffer, 4, phase);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetStrokeDashInternal(float* dash0, int count, float phase)
        {
            var stroke = _pStrokeHandle;
            var effect = _hDashEffect;
            if (effect != 0)
            {
                SKApiEx.PaintSetPathEffect(stroke, 0);
                SKApiEx.RefCntSafeUnref(effect); effect = 0;
            }
            if (count != 0)
            {
                effect = SKApiEx.PathEffectCreateDash((nint)dash0, count, phase);
                SKApiEx.PaintSetPathEffect(stroke, effect);
            }
            _hDashEffect = effect;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClearStrokeDash()
        {
            var effect = _hDashEffect;
            if (effect != 0)
            {
                _hDashEffect = 0;
                SKApiEx.PaintSetPathEffect(_pStrokeHandle, 0);
                SKApiEx.RefCntSafeUnref(effect);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFillColor(SKColor color) => SetFillColor((uint)color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFillColor(uint color)
        {
            if (_lastFillColor != color)
            {
                _lastFillColor = color;
                SKApiEx.PaintSetColor(_pFillHandle, color);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFillOpaque()
        {
            if (_lastFillColor < 0xFF000000)
            {
                _lastFillColor = 0xFF000000;
                SKApiEx.PaintSetColor(_pFillHandle, 0xFF000000);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFillBlend(SKBlendMode blend = SKBlendMode.SrcOver)
        {
            if (_lastFillBlend != blend)
            {
                _lastFillBlend = blend;
                SKApiEx.PaintSetBlendMode(_pFillHandle, blend);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFillShader(SKShader shader = null)
        {
            if (_lastFillShader != shader)
            {
                _lastFillShader = shader;
                SKApiEx.PaintSetShader(_pFillHandle, shader?.Handle ?? IntPtr.Zero);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFillAntialias(bool isAntialias = true)
        {
            if (_lastFillIsAntialias != isAntialias)
            {
                _lastFillIsAntialias = isAntialias;
                SKApiEx.PaintSetAntialias(_pFillHandle, Unsafe.As<bool, byte>(ref isAntialias));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTextColor(SKColor color) => SetTextColor((uint)color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTextColor(uint color)
        {
            if (_lastTextColor != color)
            {
                _lastTextColor = color;
                SKApiEx.PaintSetColor(_pTextHandle, color);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTextSize(float size)
        {
            if (_lastTextSize != size)
            {
                _lastTextSize = size;
                SKApiEx.FontSetSize(_fontHandle, size);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTextFace(SKTextFace face)
        {
            if (_lastTextFace != face)
            {
                _lastTextFace = face;
                if (_faceDefault[(int)face] is SKTypeface tf)
                    SKApiEx.FontSetTypeface(_fontHandle, tf.Handle);
                else
                    SKApiEx.FontSetTypeface(_fontHandle, 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTextBlend(SKBlendMode blend = SKBlendMode.SrcOver)
        {
            if (_lastTextBlend != blend)
            {
                _lastTextBlend = blend;
                SKApiEx.PaintSetBlendMode(_pTextHandle, blend);
            }
        }

        // ================ MATRIX ================

        /// <summary> Canvas의 Matirx를 리셋한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetMatrix()
        {
            if (_lastSetMatrix)
            {
                _lastSetMatrix = false;
                SKApiEx.ResetMatrix(_tHandle);
            }
        }

        /// <summary> Canvas의 Matirx를 이전에 설정된 메트릭스가 있는지 상관없이 리셋한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetMatrixForce()
        {
            _lastSetMatrix = false;
            SKApiEx.ResetMatrix(_tHandle);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct F16
        {
            public float M11, M12, M13, M14;
            public float M21, M22, M23, M24;
            public float M31, M32, M33, M34;
            public float M41, M42, M43, M44;
        }

        /// <summary> Canvas의 Matirx를 설정한다 </summary>
        /// <param name="pivotX">그려질 대상의 PivotX</param>
        /// <param name="pivotY">그려질 대상의 PivotY</param>
        /// <param name="rotation">Pivot에서의 회전값</param>
        /// <param name="scale">Pivot에서의 스케일값</param>
        /// <param name="drawPositionX">그려질 화면 위치 ScreenX</param>
        /// <param name="drawPositionY">그려질 화면 위치 ScreenY</param>
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SetMatrix(double pivotX, double pivotY, double rotation, double scale = 1, double drawPositionX = 0, double drawPositionY = 0)
        {
            const ulong F00 = 0x0000000000000000; // 0f, 0f
            const ulong F10 = 0x000000003F800000; // 1f, 0f
            const ulong F01 = 0x3F80000000000000; // 0f, 1f

            Unsafe.SkipInit(out F16 f16);
            var mx0 = (float*)Unsafe.AsPointer(ref f16);

            *(ulong*)(mx0 + 02) = F00;
            *(ulong*)(mx0 + 06) = F00;
            *(ulong*)(mx0 + 08) = F00;
            *(ulong*)(mx0 + 10) = F10;
            *(ulong*)(mx0 + 14) = F01;

            if (Unsafe.BitCast<double, ulong>(rotation) == 0)
            {
                *(float*)(mx0 + 00) = (float)scale;
                *(float*)(mx0 + 01) = 0;
                *(float*)(mx0 + 04) = 0;
                *(float*)(mx0 + 05) = (float)scale;
                *(float*)(mx0 + 12) = (float)(drawPositionX - pivotX * scale);
                *(float*)(mx0 + 13) = (float)(drawPositionY - pivotY * scale);
            }
            else
            {
                var (ss, cs) = Math.SinCos(rotation * (Math.PI / 180));
                ss *= scale;
                cs *= scale;

                *(float*)(mx0 + 00) = (float)cs;
                *(float*)(mx0 + 01) = (float)ss;
                *(float*)(mx0 + 04) = (float)-ss;
                *(float*)(mx0 + 05) = (float)cs;
                *(float*)(mx0 + 12) = (float)(drawPositionX - pivotX * cs + pivotY * ss);
                *(float*)(mx0 + 13) = (float)(drawPositionY - pivotX * ss - pivotY * cs);
            }

            _lastSetMatrix = true;
            SKApiEx.SetMatrix(_tHandle, (nint)mx0);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SetMatrix(float pivotX, float pivotY, float rotation, float scale = 1, float drawPositionX = 0, float drawPositionY = 0)
        {
            const ulong F00 = 0x0000000000000000; // 0f, 0f
            const ulong F10 = 0x000000003F800000; // 1f, 0f
            const ulong F01 = 0x3F80000000000000; // 0f, 1f

            Unsafe.SkipInit(out F16 f16);
            var mx0 = (float*)Unsafe.AsPointer(ref f16);

            *(ulong*)(mx0 + 02) = F00;
            *(ulong*)(mx0 + 06) = F00;
            *(ulong*)(mx0 + 08) = F00;
            *(ulong*)(mx0 + 10) = F10;
            *(ulong*)(mx0 + 14) = F01;

            if (Unsafe.BitCast<float, uint>(rotation) == 0)
            {
                *(float*)(mx0 + 00) = scale;
                *(float*)(mx0 + 01) = 0;
                *(float*)(mx0 + 04) = 0;
                *(float*)(mx0 + 05) = scale;
                *(float*)(mx0 + 12) = drawPositionX - pivotX * scale;
                *(float*)(mx0 + 13) = drawPositionY - pivotY * scale;
            }
            else
            {
                var (ss, cs) = MathF.SinCos(rotation * (MathF.PI / 180));
                ss *= scale;
                cs *= scale;

                *(float*)(mx0 + 00) = cs;
                *(float*)(mx0 + 01) = ss;
                *(float*)(mx0 + 04) = -ss;
                *(float*)(mx0 + 05) = cs;
                *(float*)(mx0 + 12) = drawPositionX - pivotX * cs + pivotY * ss;
                *(float*)(mx0 + 13) = drawPositionY - pivotX * ss - pivotY * cs;
            }

            _lastSetMatrix = true;
            SKApiEx.SetMatrix(_tHandle, (nint)mx0);
        }

        /// <summary> Canvas의 Matirx를 설정한다 </summary>
        /// <param name="drawPositionX">그려질 화면 위치 ScreenX</param>
        /// <param name="drawPositionY">그려질 화면 위치 ScreenY</param>
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SetMatrix(double drawPositionX, double drawPositionY)
        {
            const ulong F00 = 0x0000000000000000; // 0f, 0f
            const ulong F10 = 0x000000003F800000; // 1f, 0f
            const ulong F01 = 0x3F80000000000000; // 0f, 1f

            Unsafe.SkipInit(out F16 f16);
            var mx0 = (float*)Unsafe.AsPointer(ref f16);

            *(ulong*)(mx0 + 00) = F10;
            *(ulong*)(mx0 + 02) = F00;
            *(ulong*)(mx0 + 04) = F01;
            *(ulong*)(mx0 + 06) = F00;
            *(ulong*)(mx0 + 08) = F00;
            *(ulong*)(mx0 + 10) = F10;
            *(float*)(mx0 + 12) = (float)drawPositionX;
            *(float*)(mx0 + 13) = (float)drawPositionY;
            *(ulong*)(mx0 + 14) = F01;

            _lastSetMatrix = true;
            SKApiEx.SetMatrix(_tHandle, (nint)mx0);
        }

        // ================ CANVAS ================

        /// <summary> Canvas를 Clear한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _tcanvas.Clear();

        /// <summary> Canvas를 특정색으로 채운다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(SKColor color) => Clear((uint)color);

        /// <summary> Canvas를 특정색으로 채운다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(uint color)
        {
            if ((color & 0xFF000000) != 0)
                SKApiEx.Clear(_tHandle, color);
            else
                _tcanvas.Clear();
        }

        /// <summary> 이전 레이어 캔버스로 되돌린다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RestorePreviousCanvas() => TargetCanvas = _prevCanvas;

        /// <summary> TargetCanvas를 128x128Canvas로 설정한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set128x128Canvas() => TargetCanvas = _cv128x128;

        /// <summary> TargetCanvas를 256x256Canvas로 설정한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set256x256Canvas() => TargetCanvas = _cv256x256;

        /// <summary> TargetCanvas를 512x064Canvas로 설정한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set512x064Canvas() => TargetCanvas = _cv512x064;

        /// <summary> TargetCanvas를 Clear된 128x128Canvas로 설정한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetClear128x128Canvas() => TargetCanvas = _cv128x128.ClearGet();

        /// <summary> TargetCanvas를 Clear된 256x256Canvas로 설정한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetClear256x256Canvas() => TargetCanvas = _cv256x256.ClearGet();

        /// <summary> TargetCanvas를 Clear된 256x256Canvas로 설정한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetClear512x064Canvas() => TargetCanvas = _cv512x064.ClearGet();

        /// <summary> TargetCanvas에 그려진 이미지를 RasterAlpha 형태로 생성한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RasterAlpha CreateRasterAlpha(float pivotx, float pivoty, bool restorePreviousCanvas = false)
        {
            var canvas = _tcanvas;
            if (restorePreviousCanvas) TargetCanvas = _prevCanvas;
            return new(canvas, pivotx, pivoty);
        }

        /// <summary> TargetCanvas에 그려진 이미지를 RasterImage 형태로 생성한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RasterImage CreateRasterImage(float pivotx, float pivoty, bool restorePreviousCanvas = false)
        {
            var canvas = _tcanvas;
            if (restorePreviousCanvas) TargetCanvas = _prevCanvas;
            return new(canvas, pivotx, pivoty);
        }

        /// <summary> Canvas와 동일한 색상정보의 Bitmap을 생성한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKBitmap CreateBitmap(int width, int height, SKAlphaType alphaType = SKAlphaType.Premul) =>
            new(new(width, height, SKImageInfo.PlatformColorType, alphaType));

        // ================ DRAW LINE ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(Float2D p1, Float2D p2, SKPaint paint = null) => DrawLine(p1.X, p1.Y, p2.X, p2.Y, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(float x1, float y1, float x2, float y2, SKPaint paint = null)
        {
            SKApiEx.DrawLine(_tHandle, x1, y1, x2, y2, (paint ?? _pStroke).Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLineWorld(Float2D p1, Float2D p2, SKPaint paint = null) => DrawLineWorld(p1.X, p1.Y, p2.X, p2.Y, paint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawLineWorld(float x1, float y1, float x2, float y2, SKPaint paint = null)
        {
            var t = Transform;
            var p1 = t.WorldToScreen(x1, y1);
            var p2 = t.WorldToScreen(x2, y2);
            if (t.LineClipScreen(ref p1, ref p2))
                SKApiEx.DrawLine(_tHandle, p1.X, p1.Y, p2.X, p2.Y, (paint ?? _pStroke).Handle);
        }


        // ================ DRAW RECT ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRect(float x1, float y1, float x2, float y2, SKPaint paint = null) =>
            DrawRect(new(x1, y1, x2, y2), paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRect(FloatRect rect, SKPaint paint = null) =>
            SKApiEx.DrawRect(_tHandle, (nint)(&rect), (paint ?? _pStroke).Handle);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawRectWorld(in FloatRect rect, SKPaint paint = null)
        {
            Transform.WorldToScreen(rect, out var path4);
            DrawPathInternal(ref path4.P1, 4, true, paint);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawRectWGS84(in FloatRect rect, SKPaint paint = null)
        {
            Transform.WGS84ToScreen(rect, out var path4);
            DrawPathInternal(ref path4.P1, 4, true, paint);
        }


        // ================ FILL RECT ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillRect(float x1, float y1, float x2, float y2, SKPaint paint = null) =>
            FillRect(new(x1, y1, x2, y2), paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillRect(FloatRect rect, SKPaint paint = null) =>
            SKApiEx.DrawRect(_tHandle, (nint)(&rect), (paint ?? _pFill).Handle);

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FillRectWorld(in FloatRect rect, SKPaint paint = null)
        {
            Transform.WorldToScreen(rect, out var path4);
            FillPathInternal(ref path4.P1, 4, paint);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FillRectWGS84(in FloatRect rect, SKPaint paint = null)
        {
            Transform.WGS84ToScreen(rect, out var path4);
            FillPathInternal(ref path4.P1, 4, paint);
        }


        // ================ DRAW Ellipse ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawEllipse(FloatEllipse ellipse, SKPaint paint = null) =>
            DrawEllipse(ellipse.Radius, ellipse.CenterX, ellipse.CenterY, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawEllipse(float radius, Float2D drawPosition, SKPaint paint = null) =>
            DrawEllipse(radius, drawPosition.X, drawPosition.Y, paint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawEllipse(float radius, float drawPositionX = 0, float drawPositionY = 0, SKPaint paint = null)
        {
            var r = GeometryHelper.CircleRelation(drawPositionX, drawPositionY, radius, _transform.ScreenBound);
            if ((r & GeoRelation.PathIntersect) != 0)
            {
                // 원의 경우 반경이 너무 크면 skia그리기 처리과정에서 성능상의 문제가 발생하므로 제한을 둠 (현재 65536이하만 가능)                
                if (radius < 65536)
                    SKApiEx.DrawCircle(_tHandle, drawPositionX, drawPositionY, radius, (paint ?? _pStroke).Handle);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillEllipse(FloatEllipse ellipse, SKPaint paint = null) =>
            FillEllipse(ellipse.Radius, ellipse.CenterX, ellipse.CenterY, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillEllipse(float radius, Float2D drawPosition, SKPaint paint = null) =>
            FillEllipse(radius, drawPosition.X, drawPosition.Y, paint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FillEllipse(float radius, float drawPositionX = 0, float drawPositionY = 0, SKPaint paint = null)
        {
            var r = GeometryHelper.CircleRelation(drawPositionX, drawPositionY, radius, _transform.ScreenBound);
            if (r != 0)
            {
                if (r == GeoRelation.ContainedBy) { FillRect(_transform.ScreenBound); return; }
                // 원의 경우 반경이 너무 크면 skia그리기 처리과정에서 성능상의 문제가 발생하므로 제한을 둠 (현재 65536이하만 가능)
                if (radius < 65536)
                    SKApiEx.DrawCircle(_tHandle, drawPositionX, drawPositionY, radius, (paint ?? _pFill).Handle);
            }
        }

        // ================ PATH DETAIL================

        /// <summary> 내부 SKPath를 초기화(FillType을 포함) 한다 <br/> (영역그리기용) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathClear()
        {
            var hPath = _skpathInternalHandle;
            SKApiEx.PathRewind(hPath);
            SKApiEx.PathSetFillType(hPath, SKPathFillType.EvenOdd);
        }

        /// <summary> 내부 SKPath를 초기화(FillType을 제외) 한다 <br/> (라인그리기용) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathClearWithoutFillType() => SKApiEx.PathRewind(_skpathInternalHandle);

        /// <summary> 내부 SKPath에 시작점을 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathMoveTo(Float2D point) => SKApiEx.PathMoveTo(_skpathInternalHandle, point.X, point.Y);

        /// <summary> 내부 SKPath에 시작점을 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathMoveTo(float x, float y) => SKApiEx.PathMoveTo(_skpathInternalHandle, x, y);

        /// <summary> 내부 SKPath에 연결점을 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathLineTo(Float2D point) => SKApiEx.PathLineTo(_skpathInternalHandle, point.X, point.Y);

        /// <summary> 내부 SKPath에 연결점을 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathLineTo(float x, float y) => SKApiEx.PathLineTo(_skpathInternalHandle, x, y);


        /// <summary> 내부 SKPath에 라인을 추가한다 </summary>
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(Float2D p1, Float2D p2)
        {
            var line = new FloatLine(p1, p2);
            SKApiEx.PathAddPoly(_skpathInternalHandle, (nint)(&line), 2, false);
        }

        /// <summary> 내부 SKPath에 라인을 추가한다 </summary>
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(float x1, float y1, float x2, float y2)
        {
            var line = new FloatLine(new(x1, y1), new(x2, y2));
            SKApiEx.PathAddPoly(_skpathInternalHandle, (nint)(&line), 2, false);
        }

        /// <summary> 내부 SKPath에 사각형을 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(FloatRect rect, SKPathDirection direction = SKPathDirection.Clockwise) =>
            SKApiEx.PathAddRect(_skpathInternalHandle, (nint)(&rect), direction);

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(List<Float2D> path, bool close = false) =>
            PathAdd(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, close);

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(SList<Float2D> path, bool close = false) =>
            PathAdd(ref path.Ref0, path.Count, close);

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(Span<Float2D> path, bool close = false) =>
            PathAdd(ref GetReference(path), path.Length, close);

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathsAdd(Float2D[][] paths, bool close = false)
        {
            var i = 0;
            do PathAdd(ref GetArrayDataReference(paths[i]), paths[i].Length, close);
            while (++i < paths.Length);
        }

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(DataRange<Float2D> range, bool close = false) =>
            PathAdd(ref range.Data0, range.Count, close);

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(DataHeaderReader<Float2D> reader, bool close = false) =>
            PathAdd(ref reader.Data0, reader.Count, close);

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAdd(ref Float2D path0, int length, bool close = false)
        {
            if (length < 2) return;
            fixed (Float2D* pPath = &path0)
            {
                SKApiEx.PathAddPoly(_skpathInternalHandle, (nint)pPath, length, close);
                return;
            }
        }

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAddWorld(Span<Float2D> path, bool close = false) =>
            PathAddWorld(ref GetReference(path), path.Length, close);

        /// <summary> 내부 SKPath에 패스를 추가한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PathAddWorld(ref Float2D path0, int length, bool close = false)
        {
            if (length < 2) return;
            ref var buffer0 = ref GetPathBufferInternal0(length);
            var dedupeCount = Transform.WorldToScreenDedupe(ref path0, ref buffer0, length);
            fixed (Float2D* pPath = &buffer0)
            {
                SKApiEx.PathAddPoly(_skpathInternalHandle, (nint)pPath, dedupeCount, close);
                return;
            }
        }

        /// <summary> 내부 SKPath로 Fill 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LastPathFill()
        {
            SKApiEx.DrawPath(_tHandle, _skpathInternalHandle, _pFillHandle);
        }

        /// <summary> 내부 SKPath로 Draw 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LastPathDraw()
        {
            SKApiEx.DrawPath(_tHandle, _skpathInternalHandle, _pStrokeHandle);
        }

        /// <summary> 내부 SKPath로 Fill 및 Draw 한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LastPathFillDraw()
        {
            SKApiEx.DrawPath(_tHandle, _skpathInternalHandle, _pFillHandle);
            SKApiEx.DrawPath(_tHandle, _skpathInternalHandle, _pStrokeHandle);
        }

        // ================ DRAW PATH ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPath(SKPath path, SKPaint paint = null) =>
            SKApiEx.DrawPath(_tHandle, path.Handle, (paint ?? _pStroke).Handle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPath(List<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPath(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, close, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPath(SList<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPath(ref path.Ref0, path.Count, close, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPath(Span<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPath(ref GetReference(path), path.Length, close, paint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawPath(ref Float2D path0, int length, bool close = false, SKPaint paint = null)
        {
            if (length < 2) return;
            DrawPathInternal(ref path0, length, close, paint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPath(ref Float2D path0, int length, SList<ClippedPath> cpaths, SKPaint paint = null)
        {
            if (cpaths != null && cpaths.Count != 0)
            {
                var i = 0;
                ref var cpath0 = ref cpaths.Ref0;
                do DrawPath(ref path0, Unsafe.Add(ref cpath0, i), paint);
                while (++i < cpaths.Count);
            }
            else
            {
                DrawPath(ref path0, length);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawPath(ref Float2D path0, in ClippedPath cpath, SKPaint paint = null)
        {
            var hCanvas = _tHandle;
            var hStroke = (paint ?? _pStroke).Handle;
            if (cpath.Length == 0)
            {
                SKApiEx.DrawLine(
                    hCanvas, cpath.Head.X, cpath.Head.Y, cpath.Tail.X, cpath.Tail.Y, hStroke);
            }
            else
            {
                var hPath = _skpathDrawingHandle;
                SKApiEx.PathRewind(hPath);
                fixed (Float2D* pSpace = &path0)
                {
                    var point0 = pSpace + cpath.Offset;
                    SKApiEx.PathMoveTo(hPath, cpath.Head.X, cpath.Head.Y);
                    SKApiEx.PathLineTo(hPath, point0->X, point0->Y);
                    SKApiEx.PathAddPoly(hPath, (nint)(pSpace + cpath.Offset), cpath.Length, false);
                    SKApiEx.PathLineTo(hPath, cpath.Tail.X, cpath.Tail.Y);
                }
                SKApiEx.DrawPath(hCanvas, hPath, hStroke);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPathWorld(List<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPathWorld(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, close, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPathWorld(SList<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPathWorld(ref path.Ref0, path.Count, close, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPathWorld(Span<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPathWorld(ref GetReference(path), path.Length, close, paint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawPathWorld(ref Float2D path0, int length, bool close = false, SKPaint paint = null)
        {
            if (length < 2) return;
            ref var buffer0 = ref GetPathBufferInternal0(length);
            var dedupeCount = Transform.WorldToScreenDedupe(ref path0, ref buffer0, length);
            DrawPathInternal(ref buffer0, dedupeCount, close, paint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPathWGS84(List<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPathWGS84(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, close, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPathWGS84(SList<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPathWGS84(ref path.Ref0, path.Count, close, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPathWGS84(Span<Float2D> path, bool close = false, SKPaint paint = null) =>
            DrawPathWGS84(ref GetReference(path), path.Length, close, paint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawPathWGS84(ref Float2D path0, int length, bool close = false, SKPaint paint = null)
        {
            if (length < 2) return;
            ref var buffer0 = ref GetPathBufferInternal0(length);
            Transform.WGS84ToScreen(ref path0, ref buffer0, length);
            DrawPathInternal(ref buffer0, length, close, paint);
        }



        // ================ FILL PATH ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillPaint(SKPaint paint = null) =>
            SKApiEx.DrawPaint(_tHandle, (paint ?? _pFill).Handle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillPath(SKPath path, SKPaint paint = null) =>
            SKApiEx.DrawPath(_tHandle, path.Handle, (paint ?? _pFill).Handle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillPath(List<Float2D> path, SKPaint paint = null) =>
            FillPath(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillPath(SList<Float2D> path, SKPaint paint = null) =>
            FillPath(ref path.Ref0, path.Count, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillPath(Span<Float2D> path, SKPaint paint = null) =>
            FillPath(ref GetReference(path), path.Length, paint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FillPath(ref Float2D path0, int length, SKPaint paint = null)
        {
            if (length < 3) return;
            FillPathInternal(ref path0, length, paint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillPathWorld(List<Float2D> path, SKPaint paint = null) =>
            FillPathWorld(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillPathWorld(SList<Float2D> path, SKPaint paint = null) =>
            FillPathWorld(ref path.Ref0, path.Count, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillPathWorld(Span<Float2D> path, SKPaint paint = null) =>
            FillPathWorld(ref GetReference(path), path.Length, paint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FillPathWorld(ref Float2D path0, int length, SKPaint paint = null)
        {
            if (length < 3) return;
            ref var buffer0 = ref GetPathBufferInternal0(length);
            Transform.WorldToScreen(ref path0, ref buffer0, length);
            FillPathInternal(ref buffer0, length, paint);
        }

        // ================ FILL AND DRAW PATH ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPath(SKPath path, SKPaint fillPaint = null, SKPaint strokePaint = null)
        {
            var hCanvas = _tHandle;
            SKApiEx.DrawPath(hCanvas, path.Handle, (fillPaint ?? _pFill).Handle);
            SKApiEx.DrawPath(hCanvas, path.Handle, (strokePaint ?? _pStroke).Handle);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPath(List<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPath(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPath(SList<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPath(ref path.Ref0, path.Count, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPath(Span<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPath(ref GetReference(path), path.Length, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FillDrawPath(ref Float2D path0, int length, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null)
        {
            if (length < 2) return;
            FillDrawPathInternal(ref path0, length, close, fillPaint, strokePaint);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPathWorld(List<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPathWorld(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPathWorld(SList<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPathWorld(ref path.Ref0, path.Count, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPathWorld(Span<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPathWorld(ref GetReference(path), path.Length, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FillDrawPathWorld(ref Float2D path0, int length, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null)
        {
            if (length < 2) return;
            ref var buffer0 = ref GetPathBufferInternal0(length);
            var dedupeCount = Transform.WorldToScreenDedupe(ref path0, ref buffer0, length);
            FillDrawPathInternal(ref buffer0, dedupeCount, close, fillPaint, strokePaint);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPathWGS84(List<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPathWGS84(ref GetReference(CollectionsMarshal.AsSpan(path)), path.Count, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPathWGS84(SList<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPathWGS84(ref path.Ref0, path.Count, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillDrawPathWGS84(Span<Float2D> path, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null) =>
            FillDrawPathWGS84(ref GetReference(path), path.Length, close, fillPaint, strokePaint);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FillDrawPathWGS84(ref Float2D path0, int length, bool close = false, SKPaint fillPaint = null, SKPaint strokePaint = null)
        {
            if (length < 2) return;
            ref var buffer0 = ref GetPathBufferInternal0(length);
            Transform.WGS84ToScreen(ref path0, ref buffer0, length);
            FillDrawPathInternal(ref buffer0, length, close, fillPaint, strokePaint);
        }

        // ================ DRAW PICTURE ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPicture(SKPicture picture, SKPaint paint = null) =>
            SKApiEx.DrawPicture(_tHandle, picture.Handle, 0, (paint ?? _pFill).Handle);


        // ================ DRAW IMAGE ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawImage(SKImage image, float drawPositionX = 0, float drawPositionY = 0, SKPaint paint = null) =>
            DrawImage(image.Handle, drawPositionX, drawPositionY, paint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawBitmap(SKBitmap bitmap, float drawPositionX = 0, float drawPositionY = 0, SKPaint paint = null)
        {
            var hImage = SKApiEx.ImageNewFromBitmap(bitmap.Handle);
            DrawImage(hImage, drawPositionX, drawPositionY, paint);
            SKApiEx.RefCntSafeUnref(hImage);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DrawImage(nint hImage, float drawPositionX = 0, float drawPositionY = 0, SKPaint paint = null)
        {
            var sampling = SKSamplingOptions.Default;
            SKApiEx.DrawImage(_tHandle, hImage, drawPositionX, drawPositionY, (nint)(&sampling), (paint ?? _pImage).Handle);
        }

        // ================ DRAW ARC ================

        /// <summary> Arc모양으로 선을 그린다 (각도는 시계의 12시를(0도) 기준으로 시계방향) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawArc(float radius, float startAngle, float sweepAngle, float posx = 0, float posy = 0) =>
            PaintArcInternal(radius, startAngle, sweepAngle, _pStrokeHandle, posx, posy);

        /// <summary> Arc모양으로 색을 채운다 (각도는 시계의 12시를(0도) 기준으로 시계방향) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillArc(float radius, float startAngle, float sweepAngle, float posx = 0, float posy = 0) =>
            PaintArcInternal(radius, startAngle, sweepAngle, _pFillHandle, posx, posy);

        /// <summary> Arc모양으로 패인트를 채운다 (각도는 시계의 12시를(0도) 기준으로 시계방향) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PaintArc(float radius, float startAngle, float sweepAngle, SKPaint paint, float posx = 0, float posy = 0) =>
            PaintArcInternal(radius, startAngle, sweepAngle, paint.Handle, posx, posy);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PaintArcInternal(float radius, float startAngle, float sweepAngle, nint paintHandle, float drawPositionX = 0, float drawPositionY = 0)
        {
            var oval = new FloatRect(drawPositionX - radius, drawPositionY - radius, drawPositionX + radius, drawPositionY + radius);
            SKApiEx.DrawArc(_tHandle, (nint)(&oval), startAngle - 90, sweepAngle, 0, paintHandle);
        }

        // ================ TEXT ================

        /// <summary> 
        /// 텍스트에 대한 SKReadyText를 생성한다. SKReadyText를 재사용하여 퍼포먼스를 향상 시킬수 있다 (사용 후 반드시 Dispose필요) <para/>
        /// SKReadyText는 텍스트의 가로세로 중앙정렬을 기본값으로 설정된다
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public SKReadyText CreateReadyText(ReadOnlySpan<char> text, float size = 12, SKTextFace face = SKTextFace.Normal)
        {
            if (text.Length == 0) return null;
            return new(text, SetText(size, face), _pTextHandle);
        }

        /// <summary> SKReadyText를 사용하여 특정위치에 텍스트를 그린다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawText(SKReadyText readyText, float x, float y) =>
            readyText.Draw(_tHandle, _pTextHandle, x, y);

        /// <summary> SKReadyText를 사용하여 특정위치에 경계선이 있는 텍스트를 그린다 / </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawText(SKReadyText readyText, float x, float y, IntColor stroke, float strokeWidth) =>
            readyText.Draw(_tHandle, _pTextHandle, x, y, _lastTextColor, stroke, strokeWidth);


        /// <summary> 중앙정렬하여 특정위치에 텍스트를 그린다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawText(ReadOnlySpan<char> text, float size, float x, float y, SKTextFace face = SKTextFace.Normal) =>
            DrawText(text, size, x, y, 0, 0, face, 0, 0);

        /// <summary> 중앙정렬하여 특정위치에 텍스트를 그린다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawText(ReadOnlySpan<char> text, float size, float x, float y, SKTextFace face, IntColor stroke, float strokeWidth) =>
            DrawText(text, size, x, y, 0, 0, face, stroke, strokeWidth);

        /// <summary> 정렬정보를 반영하여 특정위치에 텍스트를 그린다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawText(ReadOnlySpan<char> text, float size, float x, float y,
            SKTextHorizental hor, SKTextVertical ver, SKTextFace face = SKTextFace.Normal) =>
            DrawText(text, size, x, y, hor, ver, face, 0, 0);

        /// <summary> 정렬정보를 반영하여 특정위치에 텍스트를 그린다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawText(ReadOnlySpan<char> text, float size, float x, float y,
            SKTextHorizental hor, SKTextVertical ver, SKTextFace face, IntColor stroke, float strokeWidth)
        {
            if (text.Length == 0)
                return;

            var hFont = SetText(size, face);
            var hPaint = _pTextHandle;

            fixed (char* text0 = text)
            {
                if (strokeWidth > 0)
                {
                    var b = SKApiEx.MeasureTextBound(hFont, hPaint, text0, text.Length, strokeWidth);
                    x -= SKTextPosition.Align(hor, b);
                    y -= SKTextPosition.Align(ver, b);
                    SKApiEx.DrawText(_tHandle, hFont, hPaint, text0, text.Length, x, y, _lastTextColor, stroke, strokeWidth);
                    return;
                }
                else
                {
                    var b = SKApiEx.MeasureTextBound(hFont, hPaint, text0, text.Length);
                    x -= SKTextPosition.Align(hor, b);
                    y -= SKTextPosition.Align(ver, b);
                    SKApiEx.DrawText(_tHandle, hFont, hPaint, text0, text.Length, x, y);
                    return;
                }
            }
        }

        /// <summary> 
        /// 정렬하지 않고 특정위치에 텍스트를 그린다 <para/>
        /// 위치값에 이미 정렬정보가 반영되었을경우 더 빠르게 그리기 위해 사용한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextUnaligned(ReadOnlySpan<char> text, float x, float y)
        {
            if (text.Length == 0)
                return;

            fixed (char* text0 = text)
            {
                DrawTextUnaligned(text0, text.Length, x, y);
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextUnaligned(char* text0, int textLength, float x, float y) =>
            SKApiEx.DrawText(_tHandle, _fontHandle, _pTextHandle, text0, textLength, x, y);

        /// <summary> 
        /// 정렬하지 않고 특정위치에 텍스트를 그린다 <para/>
        /// 위치값에 이미 정렬정보가 반영되었을경우 더 빠르게 그리기 위해 사용한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextUnaligned(ReadOnlySpan<char> text, float size, float x, float y, SKTextFace face = SKTextFace.Normal) =>
            DrawTextUnaligned(text, size, x, y, face, 0, 0);

        /// <summary> 
        /// 정렬하지 않고 특정위치에 텍스트를 그린다 <para/>
        /// 위치값에 이미 정렬정보가 반영되었을경우 더 빠르게 그리기 위해 사용한다
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DrawTextUnaligned(ReadOnlySpan<char> text, float size, float x, float y, SKTextFace face, IntColor stroke, float width)
        {
            if (text.Length == 0)
                return;

            var hFont = SetText(size, face);
            fixed (char* text0 = text)
            {
                if (width > 0)
                {
                    SKApiEx.DrawText(_tHandle, hFont, _pTextHandle, text0, text.Length, x, y,
                        _lastTextColor, stroke, width);
                    return;
                }
                else
                {
                    SKApiEx.DrawText(_tHandle, hFont, _pTextHandle, text0, text.Length, x, y);
                    return;
                }
            }
        }

        /// <summary> 텍스트의 렌더링 사이즈를 계산한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public FloatRect GetTextBound(ReadOnlySpan<char> text, float size, SKTextFace face = SKTextFace.Normal, float outlineWidth = 0)
        {
            if (text.Length == 0)
                return default;

            var hFont = SetText(size, face);
            fixed (char* text0 = text)
            {
                if (outlineWidth > 0)
                    return SKApiEx.MeasureTextBound(hFont, _pTextHandle, text0, text.Length, outlineWidth);
                else
                    return SKApiEx.MeasureTextBound(hFont, _pTextHandle, text0, text.Length);
            }
        }

        /// <summary> 문자의 렌더링 사이즈를 계산한다 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public FloatRect GetTextBound(char utf16, float size, SKTextFace face = SKTextFace.Normal)
        {
            var hFont = SetText(size, face);
            return SKApiEx.MeasureTextBound(hFont, _pTextHandle, &utf16, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private nint SetText(float size, SKTextFace face)
        {
            if (_lastTextSize != size || _lastTextFace != face)
                SetTextInternal(size, face);
            return _fontHandle;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetTextInternal(float size, SKTextFace face)
        {
            if (_lastTextFace != face)
            {
                _lastTextFace = face;

                if (_faceDefault[(int)face] is SKTypeface tf)
                    SKApiEx.FontSetTypeface(_fontHandle, tf.Handle);
                else
                    SKApiEx.FontSetTypeface(_fontHandle, 0);
            }

            if (_lastTextSize != size)
            {
                _lastTextSize = size;

                SKApiEx.FontSetSize(_fontHandle, size);
            }
        }


        // ================ Buffer ================
        /// <summary> 
        /// 일시적으로 저장소로 사용될 Float2D 리스트를 반환한다 <para/>
        /// 반환된 리스트는 TransformContext의 공용 자원이고, 재사용 목적으로 제공되므로 Drawing 호출 스택에서만 사용한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SList<Float2D> GetClearPathBuffer() => _pathBuffer.ClearGet();

        /// <summary> 
        /// 일시적으로 저장소로 사용될 ClippedPath 리스트를 반환한다 <para/>
        /// 반환된 리스트는 TransformContext의 공용 자원이고, 재사용 목적으로 제공되므로 Drawing 호출 스택에서만 사용한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SList<ClippedPath> GetClearCPathBuffer() => _cpathBuffer.ClearGet();


        /// <summary> 
        /// 일시적으로 저장소로 사용될 Float2D 공간을 반환한다 <para/>
        /// 반환된 공간은 TransformContext의 공용 자원이고, 재사용 목적으로 제공되므로 Drawing 호출 스택에서만 사용한다
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Float2D GetPathBuffer0(int size, int copy = 0)
        {
            if (size > _pathBufferExternalCapacity) return ref GetNewBuffer(size, copy);
            return ref GetArrayDataReference(_pathBufferExternal);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref Float2D GetNewBuffer(int newSize, int copy)
        {
            var cap = newSize + 4095 & ~4095; // 4096 씩 증가
            var buf = GC.AllocateUninitializedArray<Float2D>(cap);
            if (copy > 0) AC.Copy(_pathBufferExternal, buf, copy);
            _pathBufferExternal = buf;
            _pathBufferExternalCapacity = cap;
            return ref GetArrayDataReference(buf);
        }


        // ================ PRIVATE FUNCTION ================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Float2D GetPathBufferInternal0(int size)
        {
            if (size > _pathBufferInternalCapacity) return ref GetNewBufferInternal(size);
            return ref GetArrayDataReference(_pathBufferInternal);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref Float2D GetNewBufferInternal(int length)
        {
            var cap = length + 4095 & ~4095; // 4096 씩 증가
            var buf = GC.AllocateUninitializedArray<Float2D>(cap);
            _pathBufferInternal = buf;
            _pathBufferInternalCapacity = cap;
            return ref GetArrayDataReference(buf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawPathInternal(ref Float2D path0, int length, bool close, SKPaint paint)
        {
            var hPath = _skpathDrawingHandle;
            SKApiEx.PathRewind(hPath);
            fixed (Float2D* pSpace = &path0)
                SKApiEx.PathAddPoly(hPath, (nint)pSpace, length, close);
            SKApiEx.DrawPath(_tHandle, hPath, (paint ?? _pStroke).Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FillPathInternal(ref Float2D path0, int length, SKPaint paint)
        {
            var hPath = _skpathDrawingHandle;
            SKApiEx.PathRewind(hPath);
            SKApiEx.PathSetFillType(hPath, SKPathFillType.EvenOdd);
            fixed (Float2D* pSpace = &path0)
                SKApiEx.PathAddPoly(hPath, (nint)pSpace, length, true);
            SKApiEx.DrawPath(_tHandle, hPath, (paint ?? _pFill).Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FillDrawPathInternal(ref Float2D path0, int length, bool close, SKPaint fillPaint, SKPaint strokePaint)
        {
            var hPath = _skpathDrawingHandle;
            SKApiEx.PathRewind(hPath);
            SKApiEx.PathSetFillType(hPath, SKPathFillType.EvenOdd);
            fixed (Float2D* pSpace = &path0)
                SKApiEx.PathAddPoly(hPath, (nint)pSpace, length, close);
            SKApiEx.DrawPath(_tHandle, hPath, (fillPaint ?? _pFill).Handle);
            SKApiEx.DrawPath(_tHandle, hPath, (strokePaint ?? _pStroke).Handle);
        }
    }
}