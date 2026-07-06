using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace JHLib.WPFUtil.Gesture
{
    public enum TouchStartState { None, SingleTouchStart, MultiTouchStart }
    public readonly record struct TouchStartResult(TouchStartState State, Point Position);
    public readonly record struct TouchMoveResult(bool SingleTouch, Point Position, Vector Vector, double Scale);
    public readonly record struct TouchEndResult(bool SingleAction, Point Position);

    public class TouchManager
    {
        private readonly UIElement _panel;
        private readonly Dictionary<TouchDevice, LinkedListNode<GesturePoint>> _touchMap;
        private readonly LinkedList<GesturePoint> _touchLink;

        private PresentationSource _touchSource;
        private Action _invalidEndAction;

        private bool _singleTouch;
        private bool _resetPinch;

        private Point _singlePoint;
        private double _centerX;
        private double _centerY;
        private double _radius;

        private Point PrimaryPoint => _touchLink.First.Value.Position;
        private Point SecondaryPoint => _touchLink.First.Next.Value.Position;
        public bool SingleTouchMode => _singleTouch;

        public TouchManager(UIElement panel)
        {
            _panel = panel;
            _touchMap = new Dictionary<TouchDevice, LinkedListNode<GesturePoint>>(8);
            _touchLink = new LinkedList<GesturePoint>();
            _touchSource = null;
            _singleTouch = false;
            _resetPinch = true;
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            if (sender is TouchDevice device)
            {
                device.Deactivated -= OnDeactivated;
                InvalidEnd(device);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Start(TouchDevice device, Action invalidEndAction, out TouchStartResult result)
        {
            Unsafe.SkipInit(out result);

            if (_touchSource == null)
                _touchSource = device.ActiveSource;
            else if (_touchSource != device.ActiveSource)
                return false;

            var point = device.GetTouchPoint(_panel).Position;
            var touchMap = _touchMap;
            if (touchMap.TryGetValue(device, out var node))
            {
                node.Value.MovePosition(point);
                return false;
            }
            else if (_panel.CaptureTouch(device))
            {
                touchMap.Add(device, _touchLink.AddLast(new GesturePoint(point)));
                device.Deactivated += OnDeactivated;

                var state = TouchStartState.None;
                if (touchMap.Count == 1)
                {
                    _singleTouch = true;
                    _singlePoint = point;
                    _invalidEndAction = invalidEndAction;
                    state = TouchStartState.SingleTouchStart;
                }
                else
                {
                    if (_singleTouch)
                    {
                        _singleTouch = false;
                        state = TouchStartState.MultiTouchStart;
                    }
                    if (_resetPinch)
                    {
                        var p1 = PrimaryPoint;
                        var p2 = SecondaryPoint;
                        _resetPinch = false;
                        _centerX = (p2.X + p1.X) * 0.5f;
                        _centerY = (p2.Y + p1.Y) * 0.5f;
                        _radius = (p2 - p1).Length;
                    }
                }

                result = new(state, point);
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Move(TouchDevice device, out TouchMoveResult result)
        {
            Unsafe.SkipInit(out result);

            var touchMap = _touchMap;
            if (touchMap.TryGetValue(device, out var node))
            {
                node.Value.MovePosition(device.GetTouchPoint(_panel).Position);

                if (touchMap.Count == 1)
                {
                    _singlePoint += node.Value.Vector;
                    result = new(true, _singlePoint, default, 1);
                }
                else if (touchMap.Count >= 2)
                {
                    var p1 = PrimaryPoint;
                    var p2 = SecondaryPoint;
                    var cx = (p2.X + p1.X) * 0.5f;
                    var cy = (p2.Y + p1.Y) * 0.5f;
                    var rad = (p2 - p1).Length;

                    if (_resetPinch)
                    {
                        _resetPinch = false;
                        _centerX = cx;
                        _centerY = cy;
                        _radius = rad;
                        result = new(false, new(cx, cy), new(0, 0), 1);
                    }
                    else
                    {
                        var movex = cx - _centerX;
                        var movey = cy - _centerY;
                        var amount = rad / _radius;
                        _centerX = cx;
                        _centerY = cy;
                        _radius = rad;
                        result = new(false, new(cx, cy), new(movex, movey), amount);
                    }
                }
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool End(TouchDevice device, out TouchEndResult result)
        {
            Unsafe.SkipInit(out result);

            var touchMap = _touchMap;
            if (touchMap.Remove(device, out var node))
            {
                node.Value.MovePosition(device.GetTouchPoint(_panel).Position);
                result = new TouchEndResult(_singleTouch, node.Value.Position);

                _panel.ReleaseTouchCapture(device);
                _touchLink.Remove(node);
                _resetPinch = true;

                if (touchMap.Count == 0)
                {
                    touchMap.Clear();
                    _touchLink.Clear();
                    _touchSource = null;
                    _singleTouch = false;
                    return true;
                }
            }
            return false;
        }

        public void InvalidEnd(TouchDevice device)
        {
            if (End(device, out _))
            {
                _invalidEndAction?.Invoke();
                _invalidEndAction = null;
            }
        }

        public void Reset()
        {
            var touchMap = _touchMap;
            if (touchMap.Count != 0)
            {
                touchMap.Clear();
                _touchLink.Clear();
                _touchSource = null;
                _singleTouch = false;
                _resetPinch = true;
                _panel.ReleaseAllTouchCaptures();
            }
        }
    }
}