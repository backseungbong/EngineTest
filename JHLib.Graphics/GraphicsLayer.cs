using JHLib.Util.Projection.ScreenTransform;
using System.Runtime.CompilerServices;

namespace JHLib.Graphics
{
    public abstract class GraphicsLayer
    {
        private TransformSetter _setter;
        private GraphicsLayerManager _manager;

        internal readonly BitmapManager BitmapManager;
        internal volatile bool Enable;
        internal volatile bool Redraw;
        internal volatile int ZIndex;

        internal TransformSetter Setter
        {
            get => _setter;
            set
            {
                _setter = value;
                _manager = value as GraphicsLayerManager;
            }
        }

        public GraphicsLayer()
        {
            BitmapManager = new();
            Enable = true;
            Redraw = false;
            ZIndex = 0;
        }

        /// <summary> 레이어 활성화 상태, 현재 레이어가 포함된 레이어 메니저에서 렌더링 참여 유무를 정한다 </summary>  
        public bool LayerEnable
        {
            get => Enable;
            set
            {
                if (Enable != value)
                {
                    Enable = value;
                    _manager?.RaiseReindex();
                }
            }
        }

        /// <summary> 
        /// 레이어 깊이 인덱스, 현재 레이어가 포함된 레이어 메니저에서 렌더링 우선순위를 정한다 <para/>
        /// 인덱스의 값이 작을수록 먼저 그려지고, 클수록 나중에 그려진다       
        /// </summary>      
        public int LayerZIndex
        {
            get => ZIndex;
            set
            {
                if (ZIndex != value)
                {
                    ZIndex = value;
                    if (Enable)
                        _manager?.RaiseReindex();
                }
            }
        }

        /// <summary>
        /// 현재 레이어의 Drawing 호출을 요청 <br/>
        /// 'true' = '재 렌더링 이벤트'를 발동시켜 Drawing 호출을 최대한 빠르게 받는다 <br/>
        /// 'false' = '재 렌더링 이벤트'를 발동시키지 않고 Drawing 호출을 무기한 기다린다 <br/>
        /// 'false'의 주 목적은 화면 갱신의 부하를 줄이기 위하여 다른 레이어의 '재 렌더링 이벤트'를 기다렸다가 같이 Drawing 호출을 받는 방법 <br/>
        /// [ 아래의 조건을 모두 만족하는 상황이라면 false를 권장 ]<br/>
        /// - 레이어 메니저에 추가된 레이어의 갯수가 2개 이상<br/>
        /// - 레이어 메니저에 포함된 다른 레이어에서 '재 렌더링 이벤트' 발생이 보장됨<br/>
        /// - 화면이 즉시 갱신되어야 하는 그리기 작업이 아님
        /// </summary>
        /// <param name="raiseRerendering">'재 렌더링 이벤트' 발동 유무</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PendingDrawing(bool raiseRerendering = false)
        {
            Redraw = true;
            if (raiseRerendering)
                _manager?.RaiseChanged();
        }

        /// <summary> 현재 Transform 반환 </summary>        
        public Transform Transform => _setter?.Transform;

        /// <summary>
        /// 화면의 특정 위치(상대값)를 피봇으로 설정한다 <para/>
        /// 피봇은 화면의 이동 및 회전시 중심점의 역할을한다 (기본값 : 0.5, 범위 : 0.0 ~ 1.0)
        /// </summary>
        public Transform SetPivot(double pivotX, double pivotY) =>
            _setter?.SetPivot(pivotX, pivotY);

        /// <summary> 
        /// 화면의 특정 위치(픽셀값)를 피봇위치로 설정한다 <para/>
        /// 피봇은 화면의 이동 및 회전시 중심점의 역할을한다
        /// </summary>
        public Transform SetPivotScreen(double sx, double sy) =>
            _setter?.SetPivotScreen(sx, sy);

        /// <summary> 화면 Rotation을 설정한다 </summary>
        public Transform SetRotation(double rotation, bool addRotation = false) =>
            _setter?.SetRotation(rotation, addRotation);

        /// <summary> 화면 Scale을 설정한다 </summary>
        public Transform SetScale(double scale) =>
            _setter?.SetScale(scale);

        /// <summary> 화면을 줌인 한다 (대축척 방향) </summary>
        public Transform ZoomIn() =>
            _setter?.ZoomIn();

        /// <summary> 화면을 줌아웃 한다 (소축척 방향) </summary>
        public Transform ZoomOut() =>
            _setter?.ZoomOut();

        /// <summary> 화면을 지정한 화면좌표로 이동시킨다 </summary>
        public Transform MoveToScreen(double sx, double sy,
            bool isRelative = false, bool immediateTransformApply = true) =>
            _setter?.MoveToScreen(sx, sy, isRelative, immediateTransformApply);

        /// <summary> 화면을 지정한 월드좌표로 이동시킨다 </summary>
        public Transform MoveToWorld(double wx, double wy,
            bool isRelative = false, bool immediateTransformApply = true) =>
            _setter?.MoveToWorld(wx, wy, isRelative, immediateTransformApply);

        /// <summary> 화면을 지정한 WGS84좌표로 이동시킨다 </summary>
        public Transform MoveToWGS84(double lon, double lat,
            bool isRelative = false, bool immediateTransformApply = true) =>
            _setter?.MoveToWGS84(lon, lat, isRelative, immediateTransformApply);

        /// <summary> 화면을 지정한 월드좌표로 이동 및 Scale을 설정한다 </summary>
        public Transform MoveToWorldOrSetScale(double wx, double wy, double scale, bool immediateTransformApply = true) =>
            _setter?.MoveToWorldOrSetScale(wx, wy, scale, immediateTransformApply);

        /// <summary> 화면을 지정한 월드좌표로 이동 및 Rotation을 설정한다 </summary>
        public Transform MoveToWorldOrSetRotation(double wx, double wy, double rotation, bool immediateTransformApply = true) =>
            _setter?.MoveToWorldOrSetRotation(wx, wy, rotation, immediateTransformApply);

        internal void FreeLayer()
        {
            Redraw = false;
            BitmapManager.Dispose();
        }
        internal void InitLayer(int width, int height)
        {
            BitmapManager.InitBitmap(width, height);
            Redraw = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool PopRedraw()
        {
            if (Redraw) { Redraw = false; return true; }
            return false;
        }

        protected virtual void ReadyToDrawing(GraphicsContext tc) { }
        protected abstract void Drawing(GraphicsContext tc);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReadyToDrawingInternal(GraphicsContext tc)
        {
            BitmapManager.ApplyToContext(tc);
            ReadyToDrawing(tc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DrawingInternal(GraphicsContext tc)
        {
            BitmapManager.ApplyToContext(tc);
            Drawing(tc);
        }
    }
}