using JHLib.Graphics;
using JHLib.NavigationLayer.InteractionMode;
using JHLib.NavigationLayer.Interface;
using JHLib.Util.Hash;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.WPFUtil.Gesture;

namespace JHLib.NavigationLayer
{
    /// <summary> InteractionMode 관리 및 모드별 렌더링을 담당하는 GraphicsLayer </summary>
    public class InteractionModeLayer : GraphicsLayer
    {
        private readonly InteractionModeManager _modeManager;
        private SimdKeyValueMap<IModeRenderer> _rendererMap;

        private InteractionModeType _cachedMode;
        private int _cachedVersion;

        /// <summary> 화면 이동/줌 변환을 적용할 TransformSetter </summary>
        public TransformSetter TransformSetter
        {
            get => _modeManager.TransformSetter;
            set => _modeManager.TransformSetter = value;
        }

        /// <summary> GesturePanel 설정 </summary>
        public GesturePanel GesturePanel
        {
            get => _modeManager.GesturePanel;
            set => _modeManager.GesturePanel = value;
        }

        /// <summary> 
        /// GestureActive 모드에서 입력이 없을때 자동으로 Voyage 모드로 복귀하기까지의 시간 (ms)<br/>
        /// 기본값 : 10000(10초), 1000(1초) ~ 300000(300초) 사이로 설정 
        /// </summary>
        public int VoyageReturnTimer
        {
            get => _modeManager.VoyageReturnTimer;
            set => _modeManager.VoyageReturnTimer = value;
        }

        /// <summary> 현재 활성 Mode </summary>
        public InteractionModeType CurentMode => _modeManager.CurrentMode;

        /// <summary> 화면 고정 상태 </summary>
        public bool FixedStatus => _modeManager.FixedStatus;


        /// <summary> Mode 변경 발생 이벤트 </summary>
        public event InteractionModeHandler OnModeChanged;

        /// <summary> 화면고정 모드 변경 발생 이벤트 </summary>
        public event FixedStatusHandler OnFixedStatusChanged
        {
            add => _modeManager.OnFixedStatusChanged += value;
            remove => _modeManager.OnFixedStatusChanged -= value;
        }

        public InteractionModeLayer()
        {
            _modeManager = new InteractionModeManager();
            _rendererMap = new SimdKeyValueMap<IModeRenderer>(16);

            _modeManager.OnRedraw += RedrawLayer;
            _modeManager.OnModeChanged += ModeChanged;
        }

        private void RedrawLayer() => PendingDrawing(true);
        private void ModeChanged(InteractionModeType mode)
        {
            LayerEnable = mode != InteractionModeType.Voyage && mode != InteractionModeType.GestureActive;
            OnModeChanged?.Invoke(mode);
        }

        /// <summary> 지정한 InteractionMode로 전환  </summary>
        /// <param name="isForce">
        /// true = 어떤 모드에서든 강제로 모드 전환 <br/>
        /// false = 현재 상태에 따라 모드 전환이 실패할수 있음
        /// </param>
        /// <returns> 
        /// 모드 전환 성공 : null 반환 <br/>
        /// 모드 전환 실패 : 실패상태 메시지 반환 <br/>
        /// </returns>
        public string SetMode(InteractionModeType mode, bool isForce = false) => _modeManager.SetMode(mode, isForce);

        /// <summary> 
        /// 현재 InteractionMode 취소하고 기본모드(GestureActive) 복귀<br/>
        /// checkMode를 지정한다면 checkMode가 현재모드와 일치할 때만 기본모드로 복귀  
        /// </summary>
        /// <returns>기본모드(GestureActive) 복귀 성공 여부</returns>
        public bool CancelMode(InteractionModeType? checkMode = null) => _modeManager.CancelMode(checkMode);

        /// <summary>
        /// 화면 고정모드 설정 (상태에 대한 결과는 OnFixedStatusChanged 이벤트로 전달)
        /// </summary>
        /// <param name="fixedStatus">null지정시엔 true->false, false->true 형태로 토글</param>
        /// <returns> 화면 고정 상태 결과 </returns>
        public bool SetFixedStatus(bool? fixedStatus = null) => _modeManager.SetFixedStatus(fixedStatus);

        /// <summary>
        /// GestureActive -> Voyage 모드로 자동 복귀 타이머 재시작
        /// </summary>
        public void RestartReturnTimer() => _modeManager.RestartReturnTimer();


        /// <summary> 특정 모드 활성화 시 동작할 IModeRenderer 등록 </summary>
        public void Register(InteractionModeType mode, IModeRenderer renderer)
        {
            if (mode == 0)
                return;

            lock (_modeManager)
            {
                renderer.ModeOnLayerSet(this);
                _modeManager.Register(mode, renderer.ModeGesture);

                if (_rendererMap.TryUpdateOrUpsize((int)mode, renderer, ref _rendererMap)) return;
                if (_rendererMap.TryAdd((int)mode, renderer) == false)
                    throw new InvalidOperationException($"Failed to register InteractionModeType {mode}.");
            }
        }

        /// <summary> 특정 모드 활성화 시 동작할 제스처 핸들러 등록 </summary>
        public void Register(InteractionModeType mode, GestureLayerEventHandler handler)
        {
            if (mode == 0)
                return;

            lock (_modeManager)
            {
                _modeManager.Register(mode, handler);

                if (_rendererMap.TryGet((int)mode, out var renderer))
                {
                    _rendererMap.TryUpdate((int)mode, null);
                    renderer?.ModeOnLayerSet(null);
                }
            }
        }

        protected override void ReadyToDrawing(GraphicsContext tc)
        {
            _cachedMode = _modeManager.CurrentMode;
            _cachedVersion = _modeManager.ModeVersion;

            if (_cachedMode == 0)
                return;

            if (_rendererMap.TryGet((int)_cachedMode, out var handlers))
                handlers?.ModeReadyToDrawing(tc);
        }

        protected override void Drawing(GraphicsContext tc)
        {
            tc.Clear();

            if (_cachedMode == 0 || _cachedVersion != _modeManager.ModeVersion)
                return;

            if (_rendererMap.TryGet((int)_cachedMode, out var handlers))
                handlers?.ModeDrawing(tc);
        }
    }
}