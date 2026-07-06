using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace JHLib.WPFUtil
{
    public static class MouseHelper
    {
        private static Cursor _lastCursor;
        public static Cursor OverrideCursor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _lastCursor;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_lastCursor != value)
                {
                    _lastCursor = value;
                    Mouse.OverrideCursor = value;
                }
            }
        }
    }
}
