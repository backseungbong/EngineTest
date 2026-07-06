using JHLib.Util.Struct;
using System.Runtime.CompilerServices;

namespace JHLib.Graphics.SkiaExtention
{
    public enum SKTextHorizental : byte { Center, Left, Right }
    public enum SKTextVertical : byte { Center, Up, Down }
    public enum SKTextFace : byte { Normal = 0, Bold = 1, Italic = 2, BoldItalic = 3, NULL = 255 }

    public static class SKTextPosition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Align(SKTextHorizental hor, in FloatRect bound)
        {
            if (hor != SKTextHorizental.Left)
                if (hor != SKTextHorizental.Right) return bound.CenterX;
                else return bound.X1;
            else return bound.X2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Align(SKTextVertical ver, in FloatRect bound)
        {
            if (ver != SKTextVertical.Up)
                if (ver != SKTextVertical.Down) return bound.CenterY;
                else return bound.Y1;
            else return bound.Y2;
        }
    }

    public sealed class SKReadyText : IDisposable
    {
        private readonly nint _hBlob;
        private readonly FloatRect _bound;
        private float _offsetX;
        private float _offsetY;
        public float Width => _bound.DX;
        public float Height => _bound.DY;

        public void Dispose() => SKApiEx.TextBlobUnref(_hBlob);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe SKReadyText(ReadOnlySpan<char> text, nint hFont, nint hPaint)
        {
            fixed (char* text0 = text)
            {
                _hBlob = SKApiEx.CreateBlob(hFont, text0, text.Length);
                _bound = SKApiEx.MeasureTextBound(hFont, hPaint, text0, text.Length);
                _offsetX = _bound.CenterX;
                _offsetY = _bound.CenterY;
                return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetHorizental(SKTextHorizental hor) => _offsetX = SKTextPosition.Align(hor, _bound);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertical(SKTextVertical ver) => _offsetY = SKTextPosition.Align(ver, _bound);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Draw(nint hCanvas, nint hPaint, float x, float y) =>
            SKApiEx.DrawBlob(hCanvas, hPaint, _hBlob, x - _offsetX, y - _offsetY);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Draw(nint hCanvas, nint hPaint, float x, float y, IntColor fill, IntColor stroke, float width) =>
            SKApiEx.DrawBlob(hCanvas, hPaint, _hBlob, x - _offsetX, y - _offsetY, fill, stroke, width);
    }
}