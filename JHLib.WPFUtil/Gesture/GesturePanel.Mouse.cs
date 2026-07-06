using System.Windows;
using System.Windows.Input;

namespace JHLib.WPFUtil.Gesture
{
    public partial class GesturePanel : UIElement
    {
        private static bool CheckMouse(MouseEventArgs e)
        {
            if (e.StylusDevice == null)
            {
                e.Handled = true;
                return true;
            }
            return false;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (_activeSource != Source.None)
                return;

            if (CheckMouse(e))
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (CaptureMouse())
                        StartGesture(e.GetPosition(this), Source.MouseLeft);
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    _activeSource = Source.MouseRight;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (CheckMouse(e))
            {
                if (IsLeftMouseActive)
                {
                    MoveGesture(e.GetPosition(this));
                }
                else if (IsRightMouseActive)
                {
                    // none
                }
                else
                {
                    HoverPosition(e.GetPosition(this));
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (CheckMouse(e))
            {
                if (IsLeftMouseActive)
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        EndGesture(e.GetPosition(this));
                        ReleaseMouseCapture();
                    }
                }
                else if (IsRightMouseActive)
                {
                    if (e.ChangedButton == MouseButton.Right)
                    {
                        EndSubTap(e.GetPosition(this));
                    }
                }
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (CheckMouse(e))
            {
                if (IsMouseActive)
                    ResetGesture();
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (CheckMouse(e))
            {
                if (IsMouseActive)
                    ResetGesture();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            ZoomEvent(e.GetPosition(this), default, e.Delta, ZoomType.Wheel);
        }
    }
}