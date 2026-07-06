using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace JHLib.WPFUtil.Gesture
{
    public enum ZoomType { Wheel, Pinch }

    public delegate void GestureEventHandler(
        GestureType inputType, GesturePoint point);

    public delegate void ZoomEventHandler(
        Point origin, Vector vector, double scale, ZoomType type);

    public partial class GesturePanel : UIElement
    {
        internal const int LongPressDelay = 700; // LongPress로 인식되는 시간 (밀리초 단위)
        internal const int TapLimitLength = 12; // Tap으로 인식되는 최대 이동 거리 (픽셀 단위), 이 값 이상 이동하면 Pan제스쳐로 변경

        private enum Source
        {
            None,
            MouseLeft = 1 << 0,
            MouseMiddle = 1 << 1,
            MouseRight = 1 << 2,
            Mouse = MouseLeft | MouseMiddle | MouseRight,

            Touch = 1 << 3,
            Stylus = 1 << 4,
        }

        private Source _activeSource;
        private GesturePoint _activePoint;
        private readonly TouchManager _touchManager;

        private bool _releaseEvent;
        private readonly DispatcherTimer _longPressTimer;

        private bool IsMouseActive => (_activeSource & Source.Mouse) != 0;
        private bool IsLeftMouseActive => _activeSource == Source.MouseLeft;
        private bool IsRightMouseActive => _activeSource == Source.MouseRight;
        private bool IsTouchActive => _activeSource == Source.Touch;
        private bool IsStylusActive => _activeSource == Source.Stylus;

        public event GestureEventHandler OnGesture;
        public event ZoomEventHandler OnZoom;
        public GesturePanel()
        {
            // WPF 기본 PressAndHold(원형 표시 후 우클릭 전환) 및 Flick 비활성화
            Stylus.SetIsPressAndHoldEnabled(this, false);
            Stylus.SetIsFlicksEnabled(this, false);

            _touchManager = new TouchManager(this);
            _activePoint = new GesturePoint(new Point());

            _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(LongPressDelay) };
            _longPressTimer.Tick += LongPressTimerTick;
        }

        // UIElement의 기본 HitTestCore는 포인터 입력을 처리하지 않으므로
        // 이 패널이 포인터 입력을 수신하도록 하려면 HitTestCore를 재정의
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) =>
            new PointHitTestResult(this, hitTestParameters.HitPoint);

        private void LongPressTimerTick(object sender, EventArgs e)
        {
            _longPressTimer.Stop();

            var ap = _activePoint;
            if (ap.IsPan)
                return;

            if (IsTouchActive)
            {
                if (_touchManager.SingleTouchMode)
                {
                    EndSubTap(ap.Position);
                    _touchManager.Reset();
                }
            }
            else if (IsStylusActive)
            {
                EndSubTap(ap.Position);
                ReleaseStylusCapture();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GestureEvent(GestureType inputType, GesturePoint point)
        {
            try { OnGesture?.Invoke(inputType, point); }
            catch { }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ZoomEvent(Point origin, Vector vector, double scale, ZoomType type)
        {
            try { OnZoom?.Invoke(origin, vector, scale, type); }
            catch { }
        }

        public void ClearGesture()
        {
            if (Dispatcher.CheckAccess())
                ResetGesture();
            else
                Dispatcher.Invoke(ResetGesture);
        }

        private void HoverPosition(Point point)
        {
            if (_activeSource == Source.None)
            {
                _activePoint.MovePosition(point);
                GestureEvent(GestureType.Hover, _activePoint);
            }
        }

        private void StartGesture(Point point, Source active, bool pressTimer = false)
        {
            _activeSource = active;
            _activePoint = new GesturePoint(point);
            GestureEvent(GestureType.Pressed, _activePoint);

            _releaseEvent = true;
            if (pressTimer) _longPressTimer.Start();
        }

        private void MoveGesture(Point point)
        {
            var ap = _activePoint;
            ap.MovePosition(point);

            if (ap.IsPan)
                GestureEvent(GestureType.PanMoved, ap);
        }

        private void ReleaseGesture(bool handled = false)
        {
            if (!handled && _releaseEvent)
                GestureEvent(GestureType.Released, _activePoint);

            _releaseEvent = false;
            _longPressTimer.Stop();
        }

        private void EndGesture(Point point)
        {
            var ap = _activePoint;
            ap.MovePosition(point);

            if (ap.IsPan)
                GestureEvent(GestureType.Released, ap);
            else
                GestureEvent(GestureType.Tap, ap);

            ReleaseGesture(true);
            _activeSource = Source.None;
        }

        private void EndSubTap(Point point)
        {
            _activePoint.MovePosition(point);
            GestureEvent(GestureType.SubTap, _activePoint);

            ReleaseGesture(true);
            _activeSource = Source.None;
        }

        private void ResetGesture()
        {
            if (_activeSource == Source.None)
                return;

            ReleaseGesture();
            _activeSource = Source.None;

            ReleaseMouseCapture();
            ReleaseStylusCapture();
            _touchManager.Reset();
        }
    }
}