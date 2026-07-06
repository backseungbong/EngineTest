using System.Windows;
using System.Windows.Input;

namespace JHLib.WPFUtil.Gesture
{
    public partial class GesturePanel : UIElement
    {
        private static bool CheckStylus(StylusEventArgs e)
        {
            if (e.StylusDevice?.TabletDevice?.Type == TabletDeviceType.Stylus)
            {
                e.Handled = true;
                return true;
            }
            return false;
        }

        private static bool HasSideButtonDown(StylusDevice stylus)
        {
            for (int i = 0; i < stylus.StylusButtons.Count; i++)
            {
                var btn = stylus.StylusButtons[i];

                if (btn.StylusDevice != null &&
                    btn.StylusDevice.Inverted &&
                    btn.Guid == StylusPointProperties.BarrelButton.Id)
                    return true;
            }
            return false;
        }

        protected override void OnStylusInAirMove(StylusEventArgs e)
        {
            if (CheckStylus(e))
            {
                if (IsStylusActive == false)
                    HoverPosition(e.GetPosition(this));
            }
        }

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            if (_activeSource != Source.None)
                return;

            if (CheckStylus(e))
            {
                if (HasSideButtonDown(e.StylusDevice))
                {
                    EndSubTap(e.GetPosition(this));
                }
                else
                {
                    if (CaptureStylus())
                        StartGesture(e.GetPosition(this), Source.Stylus, true);
                }
            }
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            if (CheckStylus(e))
            {
                if (IsStylusActive)
                    MoveGesture(e.GetPosition(this));
                else
                    HoverPosition(e.GetPosition(this));
            }
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            if (CheckStylus(e))
            {
                if (IsStylusActive)
                {
                    EndGesture(e.GetPosition(this));
                    ReleaseStylusCapture();
                }
            }
        }

        protected override void OnStylusLeave(StylusEventArgs e)
        {
            if (CheckStylus(e) && IsStylusActive)
                ResetGesture();
        }
        protected override void OnLostStylusCapture(StylusEventArgs e)
        {
            if (CheckStylus(e) && IsStylusActive)
                ResetGesture();
        }
    }
}