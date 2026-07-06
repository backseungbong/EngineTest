using JHLib.Util.Matrix;
using JHLib.Util.Struct;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.Projection.ScreenTransform
{
    /// <summary>
    /// WGS84(경위도) - World(미터) - Screen(픽셀) 간의 좌표 변환 및 화면 상태 정의
    /// </summary>
    /// <remarks>
    /// <para><b>[좌표 정의 설명]</b></para>
    /// 1. <b>WGS84 (GPS):</b> 지구 경위도 좌표계 <br/>
    /// 2. <b>World (EPSG:3857):</b> WGS84를 2D 평면에 투영한 미터(Meter) 단위 좌표계 <br/>
    /// 3. <b>Screen (Pixel):</b> World 좌표에서 화면의 회전, 스케일, 피봇에 맞춰 변환된 픽셀 단위 좌표계 <br/> <br/>
    /// <para><b>[성능 최적화 팁]</b></para>
    /// 매 프레임 대량의 객체를 렌더링해야 한다면<br/>
    /// 비용이 큰 WGS84 → World 변환을 미리 캐싱하여 World → Screen 좌표 변환만 수행하는 방식을 권장한다
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public partial class Transform
    {
        private Matrix22D _toLocal;
        private Matrix22D _toWorld;
        private readonly Matrix22D _toLocal1;
        private readonly Matrix22D _toWorld1;
        private readonly Matrix22D _toLocal2;
        private readonly Matrix22D _toWorld2;

        private readonly FloatRect _localBound;
        private FloatRect _worldBound;
        private FloatRect _wgs84Bound;
        private readonly FloatRect _worldBound1;
        private readonly FloatRect _wgs84Bound1;
        private readonly FloatRect _worldBound2;
        private readonly FloatRect _wgs84Bound2;

        private FloatOBB _worldOBB;
        // 화면 4개 꼭지점의 월드좌표 (LeftTop, RightTop, RightBottom, LeftBottom)
        // 화면이 회전된 경우는 월드 좌표는 2개의 점으로 표시할수 없고 4개의 각각 다른 점으로 표현해야함
        //private readonly Float2D _wlb; // 화면 LeftBottom 위치의 월드좌표 
        //private readonly Float2D _wrb; // 화면 RightBottom 위치의 월드좌표 
        //private readonly Float2D _wrt; // 화면 RightTop 위치의 월드좌표 
        //private readonly Float2D _wlt; // 화면 LeftTop 위치의 월드좌표 

        private readonly ScreenInfo _sinfo;

        /// <summary> 화면 피봇의 WGS84 좌표 </summary>
        public readonly Double2D WGS84Position;

        /// <summary> 화면 피봇의 World 좌표 </summary>
        public readonly Double2D WorldPosition;

        /// <summary> 화면 회전 (정북에서 시계방향) </summary>
        public readonly double Rotation;

        /// <summary> 화면 스케일 </summary>
        public readonly double Scale;

        /// <summary> 현재 Scale을 적용한 1미터(Meter, 월드좌표 단위)의 화면픽셀(Pixel) </summary>
        public readonly double FactorWorldToScreen;

        /// <summary> 현재 Scale을 적용한 1화면픽셀(Pixel)의 미터(Meter, 월드좌표 단위) </summary>
        public readonly double FactorScreenToWorld;


        /// <summary> Transform 버전 </summary>
        public readonly uint Version;

        /// <summary> 화면 회전값이 0인지 여부 </summary>
        public readonly bool IsZeroRotation;

        /// <summary> 화면 변환팩터가 다중(2개) 존재하는지 여부 </summary>
        public readonly bool IsMultiTransform;

        /// <summary> 화면 Tranform 즉시 적용여부 </summary>
        public readonly bool ImmediateTransformApply;


        /// <summary> 화면 영역 Bound </summary>
        public FloatRect ScreenBound => _localBound;

        /// <summary> 화면 영역의 월드좌표 Bound (화면이 회전된경우 회전된 화면 영역을 포함하는 Bound 영역) https://i.stack.imgur.com/W5ady.png</summary>
        public FloatRect WorldBound => _worldBound;

        /// <summary> 화면 영역의 WGS84좌표 Bound (화면이 회전된경우 회전된 화면 영역을 포함하는 Bound 영역) https://i.stack.imgur.com/W5ady.png</summary>
        public FloatRect WGS84Bound => _wgs84Bound;


        /// <summary> 화면 렌더링 너비 </summary>
        public int RenderWidth => _sinfo.Width;

        /// <summary> 화면 렌더링 높이 </summary>
        public int RenderHeight => _sinfo.Height;

        /// <summary> 화면 피봇 X (이동 및 회전시 센터지점) </summary>
        public double PivotPositionX => _sinfo.PivotScreenX;

        /// <summary> 화면 피봇 Y (이동 및 회전시 센터지점) </summary>
        public double PivotPositionY => _sinfo.PivotScreenY;

        /// <summary> 화면 렌더링 너비, 높이중 짧은길이 </summary>
        public int ShorterLength => _sinfo.ShorterLength;

        /// <summary> 화면 렌더링 대각길이 </summary>
        public int DiagonalLength => _sinfo.DiagonalLength;


        /// <summary> 1미터(Meter)의 화면픽셀(Pixel) </summary>
        public double FactorMeToPixel => _sinfo.FactorMeToPixel;

        /// <summary> 1화면픽셀(Pixel)의 미터(Meter) </summary>>
        public double FactorPixelToMe => _sinfo.FactorPixelToMe;

        /// <summary> 1밀리미터(Millimeter)의 화면픽셀(Pixel) </summary>
        public double FactorMMToPixel => _sinfo.FactorMMToPixel;

        /// <summary> 1화면픽셀(Pixel)의 밀리미터(Millimeter) </summary>
        public double FactorPixelToMM => _sinfo.FactorPixelToMM;


        /// <summary> 월드 -> 화면 매트릭스1을 반환 </summary>
        public Matrix22D ToLocal1 => _toLocal1;

        /// <summary> 화면 -> 월드 매트릭스1을 반환 </summary>
        public Matrix22D ToWorld1 => _toWorld1;

        /// <summary> 월드 -> 화면 매트릭스2를 반환 </summary>
        public Matrix22D ToLocal2 => _toLocal2;

        /// <summary> 화면 -> 월드 매트릭스2를 반환 </summary>
        public Matrix22D ToWorld2 => _toWorld2;


        [SkipLocalsInit]
        internal Transform(ScreenInfo sinfo,
            uint version, double transX, double transY, double rotation, double scale, bool immediateTransformApply)
        {
            var lb = sinfo.FloatBound;
            var w2s = sinfo.FactorMeToPixel / scale;
            var toLocal = Matrix22D.Create(transX, -transY, rotation, w2s, sinfo.PivotScreenX, sinfo.PivotScreenY);
            var toWorld = toLocal.Invert();

            _localBound = lb;
            _toLocal = toLocal;
            _toWorld = toWorld;
            _toLocal1 = toLocal;
            _toWorld1 = toWorld;

            toWorld.Transform64PostFlipY(lb, out var wPath);

            var wb = wPath.GetBound();
            var eb = new DoubleRect(EPSG3857.ToWGS84D(wb.P1), EPSG3857.ToWGS84D(wb.P2));

            var WXMIN = EPSG3857.MIN_PJX;
            var WXMAX = EPSG3857.MAX_PJX;
            var EXMIN = EPSG3857.MIN_LON;
            var EXMAX = EPSG3857.MAX_LON;

            var multiTransform = false;
            if (wb.X1 < WXMIN)
            {
                _toLocal2 = Matrix22D.Create(WXMAX + (transX - WXMIN), -transY, rotation, w2s, sinfo.PivotScreenX, sinfo.PivotScreenY);
                _toWorld2 = _toLocal2.Invert();
                _worldBound1 = new((float)WXMIN, (float)wb.Y1, (float)wb.X2, (float)wb.Y2);
                _wgs84Bound1 = new((float)EXMIN, (float)eb.Y1, (float)eb.X2, (float)eb.Y2);
                _worldBound2 = new((float)(WXMAX - (WXMIN - wb.X1)), (float)wb.Y1, (float)WXMAX, (float)wb.Y2);
                _wgs84Bound2 = new((float)(EXMAX - (EXMIN - eb.X1)), (float)eb.Y1, (float)EXMAX, (float)eb.Y2);
                multiTransform = true;
            }
            else if (WXMAX < wb.X2)
            {
                _toLocal2 = Matrix22D.Create(WXMIN - (WXMAX - transX), -transY, rotation, w2s, sinfo.PivotScreenX, sinfo.PivotScreenY);
                _toWorld2 = _toLocal2.Invert();
                _worldBound1 = new((float)wb.X1, (float)wb.Y1, (float)WXMAX, (float)wb.Y2);
                _wgs84Bound1 = new((float)eb.X1, (float)eb.Y1, (float)EXMAX, (float)eb.Y2);
                _worldBound2 = new((float)WXMIN, (float)wb.Y1, (float)(WXMIN + (wb.X2 - WXMAX)), (float)wb.Y2);
                _wgs84Bound2 = new((float)EXMIN, (float)eb.Y1, (float)(EXMIN + (eb.X2 - EXMAX)), (float)eb.Y2);
                multiTransform = true;
            }
            else
            {
                _toLocal2 = toLocal;
                _toWorld2 = toWorld;
                _worldBound1 = wb.ToFloatRect();
                _wgs84Bound1 = eb.ToFloatRect();
                _worldBound2 = _worldBound1;
                _wgs84Bound2 = _wgs84Bound1;
            }

            _worldBound = _worldBound1;
            _wgs84Bound = _wgs84Bound1;
            _worldOBB = new FloatOBB(_worldBound1, (float)rotation);
            _sinfo = sinfo;

            WGS84Position = EPSG3857.ToWGS84D(transX, transY);
            WorldPosition = new Double2D(transX, transY);
            Rotation = rotation;
            Scale = scale;
            FactorScreenToWorld = sinfo.FactorPixelToMe * scale;
            FactorWorldToScreen = w2s;

            Version = version;
            IsZeroRotation = BitConverter.DoubleToUInt64Bits(rotation) == 0;
            IsMultiTransform = multiTransform;
            ImmediateTransformApply = immediateTransformApply;
        }

        /// <summary> 화면 변환 상태를 Transform1로 전환 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SetTransform1()
        {
            _toLocal = _toLocal1;
            _toWorld = _toWorld1;
            _wgs84Bound = _wgs84Bound1;
            (_worldBound = _worldBound1).ToExtents(out _worldOBB.Extents);
        }

        /// <summary> 화면 변환 상태를 Transform2로 전환 </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SetTransform2()
        {
            _toLocal = _toLocal2;
            _toWorld = _toWorld2;
            _wgs84Bound = _wgs84Bound2;
            (_worldBound = _worldBound2).ToExtents(out _worldOBB.Extents);
        }
    }
}