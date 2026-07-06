using JHLib.Util.Hash;
using JHLib.Util.Helper;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.WPFUtil.Gesture;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace JHLib.NavigationLayer.InteractionMode
{
    /// <summary> InteractionMode 변경 델리게이트 </summary>
    public delegate void InteractionModeHandler(InteractionModeType mode);

    /// <summary> 화면고정 모드 변경 델리게이트 </summary>
    public delegate void FixedStatusHandler(bool isFixed);

    /// <summary> GestureEvent 처리 델리게이트 (이벤트 소비 여부(Handled) 및 다시그리기 여부(Redraw) 반환) </summary>
    public delegate GestureResultType GestureLayerEventHandler(GestureType type, float screenX, float screenY, Transform transform);

    /// <summary> Interaction 상태 전환관리 및 제스쳐 중계 관리자 </summary>
    public sealed class InteractionModeManager
    {
        // Voyage 모드로 자동 복귀하기까지의 기본 시간 (ms)
        private const int VOYAGE_RETURN_TIMER = 10000;

        // 기본 모드(제스처 활성화 모드)
        private const InteractionModeType DEFAULT_MODE = InteractionModeType.GestureActive;

        private readonly DispatcherTimer _returnTimer;
        private SimdKeyValueMap<GestureLayerEventHandler> _handlerMap;

        private volatile InteractionModeType _currentMode;
        private volatile bool _fixedStatus;
        private volatile int _modeVersion;

        private GesturePanel _gesturePanel;
        private bool _isUpdatingMode;
        private int _lastResetReturnTimer;

        /// <summary> 제스처 진행 중 실시간으로 화면을 갱신할지 여부 </summary>
        public bool UpdateDuringGesture { get; set; }

        /// <summary> 
        /// GestureActive 모드에서 입력이 없을때 자동으로 Voyage 모드로 복귀하기까지의 시간 (ms)<br/>
        /// 기본값 : 10000(10초), 1000(1초) ~ 300000(300초) 사이로 설정 
        /// </summary>
        public int VoyageReturnTimer
        {
            get;
            set
            {
                if (value < 1000) value = 1000;
                if (value > 300000) value = 300000;
                if (field == value) return; field = value;
                _returnTimer.Interval = TimeSpan.FromMilliseconds(value);
            }
        }

        /// <summary> 화면 이동/줌 변환을 적용할 TransformSetter </summary>
        public TransformSetter TransformSetter { get; set; }

        /// <summary> GesturePanel 설정 </summary>
        public GesturePanel GesturePanel { get => _gesturePanel; set => InvokeSetGesturePanel(value); }


        /// <summary> 현재 활성 Mode </summary>
        public InteractionModeType CurrentMode => _currentMode;

        /// <summary> 화면 고정 상태 </summary>
        public bool FixedStatus => _fixedStatus;

        /// <summary> InteractionMode 변경 시마다 증가하는 버전, 모드 변경 감지에 활용 </summary>
        public int ModeVersion => _modeVersion;


        /// <summary> Mode 변경 발생 이벤트 </summary>
        public event InteractionModeHandler OnModeChanged;

        /// <summary> 화면고정 모드 변경 발생 이벤트 </summary>
        public event FixedStatusHandler OnFixedStatusChanged;

        /// <summary> 다시그리기 발생 이벤트 </summary> 
        public event Action OnRedraw;

        public InteractionModeManager()
        {
            _returnTimer = new();
            _returnTimer.Tick += ReturnTimerTick;
            _handlerMap = new SimdKeyValueMap<GestureLayerEventHandler>(16);

            _currentMode = InteractionModeType.Voyage;
            _fixedStatus = false;
            _modeVersion = 0;

            _gesturePanel = null;
            _isUpdatingMode = false;

            UpdateDuringGesture = true;
            VoyageReturnTimer = VOYAGE_RETURN_TIMER;
        }

        /// <summary> 특정 모드 활성화 시 동작할 제스처 핸들러 등록 </summary>
        public void Register(InteractionModeType mode, GestureLayerEventHandler handler = null)
        {
            lock (this)
            {
                if (_handlerMap.TryUpdateOrUpsize((int)mode, handler, ref _handlerMap)) return;
                if (_handlerMap.TryAdd((int)mode, handler) == false)
                    throw new InvalidOperationException($"Failed to register InteractionModeType {mode}.");
            }
        }

        /// <summary> 지정한 InteractionMode로 전환  </summary>
        /// <param name="isForce">
        /// true = 어떤 모드에서든 강제로 모드 전환 <br/>
        /// false = 내부 전환 조건에 따라 모드 전환이 실패할수 있음
        /// </param>
        /// <returns> 
        /// 모드 전환 성공 : null 반환 <br/>
        /// 모드 전환 실패 : 실패상태 메시지 반환 <br/>
        /// </returns>
        public string SetMode(InteractionModeType mode, bool isForce = false) => InvokeSetMode(mode, isForce);

        /// <summary> 
        /// 현재 InteractionMode 취소하고 기본모드(GestureActive) 복귀<br/>
        /// checkMode를 지정한다면 checkMode가 현재모드와 일치할 때만 기본모드로 복귀          
        /// </summary>
        /// <returns>기본모드(GestureActive) 복귀 성공 여부</returns>
        public bool CancelMode(InteractionModeType? checkMode = null) => InvokeCancelMode(checkMode);

        /// <summary>
        /// 화면 고정모드 설정 (상태에 대한 결과는 OnFixedStatusChanged 이벤트로 전달)
        /// </summary>
        /// <param name="fixedStatus">null지정시엔 true->false, false->true 형태로 토글</param>
        /// <returns> 화면 고정 상태 결과 </returns>
        public bool SetFixedStatus(bool? fixedStatus = null) => InvokeSetFixedStatus(fixedStatus);

        /// <summary>
        /// GestureActive -> Voyage 모드로 자동 복귀 타이머 재시작
        /// </summary>
        public void RestartReturnTimer() => InvokeRestartReturnTimer();

        private void GesturePanel_OnGesture(GestureType gestureType, GesturePoint point)
        {
            if (gestureType == GestureType.Hover)
            {
                var currentMode = _currentMode;
                if (currentMode == InteractionModeType.Voyage)
                    currentMode = InteractionModeType.GestureActive;

                if (_handlerMap.TryGet((int)currentMode, out var handler))
                    EventGesture(handler, gestureType, point);
            }
            else
            {
                CheckGestureStatus();

                if (_handlerMap.TryGet((int)_currentMode, out var handler))
                    if (EventGesture(handler, gestureType, point))
                        return;
            }

            var setter = TransformSetter;
            if (setter == null) return;

            if (gestureType == GestureType.PanMoved)
            {
                if (UpdateDuringGesture)
                    setter.MoveToScreen(-point.Vector.X, -point.Vector.Y, true);
                else
                    setter.MoveToScreenChanging(-point.Vector.X, -point.Vector.Y);
            }
        }

        private void GesturePanel_OnZoom(Point origin, Vector vector, double scale, ZoomType type)
        {
            CheckGestureStatus();

            var setter = TransformSetter;
            if (setter == null) return;

            switch (type)
            {
                case ZoomType.Wheel:
                    if (scale >= 0)
                        setter.ZoomIn(origin.X, origin.Y);
                    else
                        setter.ZoomOut(origin.X, origin.Y);
                    return;

                case ZoomType.Pinch:
                    if (UpdateDuringGesture)
                        setter.MoveOrSetScale(-vector.X, -vector.Y, scale, origin.X, origin.Y);
                    else
                        setter.MoveOrSetScaleChanging(-vector.X, -vector.Y, scale, origin.X, origin.Y);
                    break;
            }
        }

        private void CheckGestureStatus()
        {
            var currentMode = _currentMode;
            if (currentMode == InteractionModeType.Voyage)
            {
                currentMode = InteractionModeType.GestureActive;
                SetInteractionModeCore(currentMode);
            }
            else if (currentMode == InteractionModeType.GestureActive)
            {
                RestartReturnTimerCore();
            }
        }

        private void SetGesturePanel(GesturePanel newPanel)
        {
            var oldPanel = _gesturePanel;
            if (oldPanel != newPanel)
            {
                if (oldPanel != null)
                {
                    oldPanel.OnGesture -= GesturePanel_OnGesture;
                    oldPanel.OnZoom -= GesturePanel_OnZoom;
                }

                _gesturePanel = newPanel;
                if (newPanel != null)
                {
                    newPanel.OnGesture += GesturePanel_OnGesture;
                    newPanel.OnZoom += GesturePanel_OnZoom;
                }
            }
        }

        private string SetInteractionModeCore(InteractionModeType newMode, bool isForce = false)
        {
            var oldMode = _currentMode;
            if (oldMode == newMode)
            {
                if (newMode == InteractionModeType.GestureActive)
                    RestartReturnTimerCore();
                return null;
            }

            if (_isUpdatingMode)
                return "Mode change is already in progress.";

            if (isForce == false)
            {
                if (oldMode != InteractionModeType.Voyage &&
                    oldMode != InteractionModeType.GestureActive)
                    return $"Exit the active [{EnumHelper.GetDescription(oldMode)}] to switch modes.";
            }

            _isUpdatingMode = true;

            if (oldMode != InteractionModeType.Voyage)
            {
                _gesturePanel?.ClearGesture();

                if (_handlerMap.TryGet((int)oldMode, out var handler))
                    EventGesture(handler, GestureType.Exit);
            }

            if (newMode == InteractionModeType.Voyage)
                SetFixedStatusCore(false);

            _currentMode = newMode;
            _modeVersion++;

            try { OnModeChanged?.Invoke(newMode); }
            catch { }

            if (newMode != InteractionModeType.Voyage)
            {
                if (_handlerMap.TryGet((int)newMode, out var handler))
                    EventGesture(handler, GestureType.Enter);
            }

            if (newMode == InteractionModeType.GestureActive)
                _returnTimer.Start();
            else
                _returnTimer.Stop();

            _isUpdatingMode = false;
            return null;
        }

        private bool CancelModeCore(InteractionModeType? checkMode)
        {
            if (_currentMode == InteractionModeType.GestureActive)
                return true;

            if (checkMode.HasValue && _currentMode != checkMode.Value)
                return false;

            SetInteractionModeCore(DEFAULT_MODE, true);
            return true;
        }

        private bool SetFixedStatusCore(bool? setStatus)
        {
            var status = _fixedStatus;
            var newValue = setStatus ?? !status;
            if (newValue != status)
            {
                if (newValue)
                {
                    if (_currentMode == InteractionModeType.Voyage)
                    {
                        SetInteractionModeCore(InteractionModeType.GestureActive, true);
                    }
                }
                else
                {
                    if (_currentMode == InteractionModeType.GestureActive)
                    {
                        _returnTimer.Stop();
                        _returnTimer.Start();
                    }
                }

                _fixedStatus = newValue;
                try { OnFixedStatusChanged?.Invoke(newValue); }
                catch { }
            }
            return newValue;
        }

        public void RestartReturnTimerCore()
        {
            var tick = Environment.TickCount;
            if ((uint)tick - (uint)_lastResetReturnTimer > 100)
            {
                _lastResetReturnTimer = tick;
                _returnTimer.Stop();
                _returnTimer.Start();
            }
        }

        private void ReturnTimerTick(object sender, EventArgs e)
        {
            _returnTimer.Stop();

            if (_fixedStatus) return;
            if (_currentMode == InteractionModeType.GestureActive)
                SetInteractionModeCore(InteractionModeType.Voyage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EventGesture(GestureLayerEventHandler handler, GestureType gestureType) =>
            handler != null && EventGesture(handler, gestureType, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EventGesture(GestureLayerEventHandler handler, GestureType gestureType, GesturePoint point) =>
            handler != null && EventGesture(handler, gestureType, (float)point.Position.X, (float)point.Position.Y);


        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool EventGesture(GestureLayerEventHandler handler, GestureType gestureType, float x, float y)
        {
            var result = GestureResultType.None;
            try { result = handler.Invoke(gestureType, x, y, TransformSetter?.Transform); }
            catch { }

            if ((result & GestureResultType.Redraw) != 0)
                OnRedraw?.Invoke();

            return (result & GestureResultType.Handled) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InvokeSetGesturePanel(GesturePanel panel)
        {
            if (GetDispatcher(out var dispatcher))
            {
                if (dispatcher.CheckAccess())
                    SetGesturePanel(panel);
                else
                    dispatcher.Invoke(() => SetGesturePanel(panel));
            }
        }

        private string InvokeSetMode(InteractionModeType mode, bool isForce)
        {
            var result = default(string);
            if (GetDispatcher(out var dispatcher))
            {
                if (dispatcher.CheckAccess())
                    result = SetInteractionModeCore(mode, isForce);
                else
                    result = dispatcher.Invoke(() => SetInteractionModeCore(mode, isForce));
            }
            return result;
        }

        private bool InvokeCancelMode(InteractionModeType? checkMode)
        {
            var result = true;
            if (GetDispatcher(out var dispatcher))
            {
                if (dispatcher.CheckAccess())
                    result = CancelModeCore(checkMode);
                else
                    result = dispatcher.Invoke(() => CancelModeCore(checkMode));
            }
            return result;
        }

        private bool InvokeSetFixedStatus(bool? fixedStatus)
        {
            var result = false;
            if (GetDispatcher(out var dispatcher))
            {
                if (dispatcher.CheckAccess())
                    result = SetFixedStatusCore(fixedStatus);
                else
                    result = dispatcher.Invoke(() => SetFixedStatusCore(fixedStatus));
            }
            return result;
        }

        private void InvokeRestartReturnTimer()
        {
            if (GetDispatcher(out var dispatcher))
            {
                if (dispatcher.CheckAccess())
                    RestartReturnTimerCore();
                else
                    dispatcher.Invoke(RestartReturnTimerCore);
            }
        }

        private static bool GetDispatcher(out Dispatcher dispatcher)
        {
            dispatcher = Application.Current?.Dispatcher;
            return dispatcher != null;
        }
    }
}