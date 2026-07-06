using JHLib.Util.Hash;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Graphic.Image
{
    public enum RasterFontFace { Normal = 0, Bold = 1, Italic = 2, BoldItalic = 3 }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public readonly struct RangeData(int offset, int y, int count)
    {
        public readonly bool IsValid => Unsafe.As<RangeData, uint>(ref Unsafe.AsRef(in this)) != 0;

        public readonly ushort Offset = (ushort)offset;
        public readonly byte Count = (byte)count;
        public readonly byte Y = (byte)y;
    }

    public class FontRasterData(int fontSize, int fontType, RangeData[] range, ulong[] stream, int width, int height)
    {
        public readonly int FontSize = fontSize;
        public readonly int FontType = fontType;
        public readonly RangeData[] Range = range;
        public readonly ulong[] Stream = stream;
        public readonly int Width = width;
        public readonly int Height = height;
    }

    public unsafe static class RasterSmallTextAVX2
    {
        private static readonly KeyTo<uint, FontRasterData> _streams = new();
        private static FontRasterData _targetStream;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadyRasterData(int fontSize, int fontType)
        {
            var key = ((uint)fontType << 16) | (uint)fontSize;
            if (_streams.Get(key, out var data))
            {
                _targetStream = data;
                return true;
            }
            _targetStream = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetRasterData(FontRasterData data)
        {
            var key = ((uint)data.FontType << 16) | (uint)data.FontSize;
            _streams.Update(key, data);
            _targetStream = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawFont(void* bitmap0, int w, int h, float drawx, float drawy, char* text0, int textl, uint color)
        {
            var fd = _targetStream;
            if (fd != null && textl > 0)
            {
                var y = (int)drawy;
                if (y >= 0 && y <= h - fd.Height)
                {
                    var x = (int)drawx;
                    if (x + fd.Width * textl >= fd.Width && x <= w - fd.Width)
                        DrawFontInternal(bitmap0, w, h, x, y, fd, text0, textl, color);
                }
            }
        }

        /// <summary> 반드시 내부 레스터 스트림이 설정되어 있고, 텍스트 길이가 0보다 큰 상황이 보장될때 사용 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawFontUnsafe(void* bitmap0, int w, int h, int drawx, int drawy, char* text0, int textl, uint color)
        {
            var fd = _targetStream;
            if (drawy >= 0 && drawy <= h - fd.Height &&
                drawx + fd.Width * textl >= fd.Width && drawx <= w - fd.Width)
                DrawFontInternal(bitmap0, w, h, drawx, drawy, fd, text0, textl, color);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DrawFontInternal(void* bitmap0, int w, int h, int x, int y, FontRasterData fd, char* t0, int tl, uint color)
        {
            var add = Vector256.Create(0x0001000100010001ul);
            var sub = Vector256.Create(0x0100010001000100ul);
            var shf = Vector256.Create((byte)
                00, 01, 00, 01, 04, 05, 04, 05, 08, 09, 08, 09, 12, 13, 12, 13,
                00, 01, 00, 01, 04, 05, 04, 05, 08, 09, 08, 09, 12, 13, 12, 13);

            var sv = Vector256.Create(color).AsUInt16();
            ref var r0 = ref MemoryMarshal.GetArrayDataReference(fd.Range);
            ref var s0 = ref MemoryMarshal.GetArrayDataReference(fd.Stream);
            var fw = fd.Width;

            var t = t0;
            var e = t0 + tl;

            var r = x + fw * tl - w;
            if (r > 0) { var n = (fw - 1 + r) / fw; e -= (uint)n; }
            if (x < 0) { var n = (fw - 1 - x) / fw; t += (uint)n; x += fw * n; }

            var st = (uint)w * 4;
            var ft = (uint)fw * 4;
            var d0 = (byte*)bitmap0 + ((uint)y * st + (uint)x * 4);
            do
            {
                ref var data = ref Unsafe.Add(ref r0, (byte)*t);
                if (data.IsValid)
                {
                    ref var s = ref Unsafe.Add(ref s0, (uint)data.Offset);
                    ref var se = ref Unsafe.Add(ref s, (uint)data.Count);
                    var d = d0 + data.Y * st;
                    do
                    {
                        var a8 = Avx2.ConvertToVector256Int32(Vector128.CreateScalarUnsafe(s).AsByte());
                        var sc = Avx2.Shuffle(a8.AsByte(), shf).AsUInt64();

                        var s1 = Avx2.Add(sc, add).AsUInt16();
                        var v1 = Avx2.MultiplyHigh(Avx2.ShiftLeftLogical128BitLane(sv, 1), s1);
                        var v2 = Avx2.ShiftLeftLogical128BitLane(Avx2.MultiplyHigh(sv, s1), 1);
                        var c1 = Avx2.Or(v1, v2);

                        var dv = Avx.LoadVector256((ulong*)d).AsUInt16();
                        var s2 = Avx2.Subtract(sub, sc).AsUInt16();
                        var d1 = Avx2.MultiplyHigh(Avx2.ShiftLeftLogical128BitLane(dv, 1), s2);
                        var d2 = Avx2.ShiftLeftLogical128BitLane(Avx2.MultiplyHigh(dv, s2), 1);
                        Avx.Store((ushort*)d, Avx2.Add(c1, Avx2.Or(d1, d2)));
                        d += st;
                    }
                    while (Unsafe.IsAddressLessThan(ref s = ref Unsafe.Add(ref s, 1), ref se));
                }
                d0 += ft;
            }
            while (++t < e);
        }
    }
}