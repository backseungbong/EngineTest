using System.Windows;
using System.Windows.Input;

namespace JHLib.WPFUtil.Gesture
{
    public partial class GesturePanel : UIElement
    {
        private static bool CheckTouch(TouchEventArgs e)
        {
            if (e.TouchDevice != null)
            {
                e.Handled = true;
                return true;
            }
            return false;
        }

        protected override void OnTouchDown(TouchEventArgs e)
        {
            if (_activeSource != Source.None && _activeSource != Source.Touch)
                return;

            if (CheckTouch(e))
            {
                if (_touchManager.Start(e.TouchDevice, ResetGesture, out var result))
                {
                    if (result.State == TouchStartState.SingleTouchStart)
                        StartGesture(result.Position, Source.Touch, true);
                    else if (result.State == TouchStartState.MultiTouchStart)
                        ReleaseGesture();
                }
            }
        }

        protected override void OnTouchMove(TouchEventArgs e)
        {
            if (CheckTouch(e))
            {
                if (_touchManager.Move(e.TouchDevice, out var result))
                {
                    if (result.SingleTouch)
                        MoveGesture(result.Position);
                    else
                        ZoomEvent(result.Position, result.Vector, result.Scale, ZoomType.Pinch);
                }
            }
        }

        protected override void OnTouchUp(TouchEventArgs e)
        {
            if (CheckTouch(e))
            {
                if (_touchManager.End(e.TouchDevice, out var result))
                {
                    if (result.SingleAction)
                        EndGesture(result.Position);
                    else
                        _activeSource = Source.None;
                }
            }
        }

        protected override void OnTouchLeave(TouchEventArgs e)
        {
            if (CheckTouch(e))
                _touchManager.InvalidEnd(e.TouchDevice);
        }
        protected override void OnLostTouchCapture(TouchEventArgs e)
        {
            if (CheckTouch(e))
                _touchManager.InvalidEnd(e.TouchDevice);
        }
    }
}