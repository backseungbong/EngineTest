using JHLib.Util.Hash;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JHLib.Util.Struct
{
    public readonly struct IntColor : IEquatable<IntColor>
    {
        private readonly uint _argb;
        public readonly byte A => (byte)(_argb >> 24);
        public readonly byte R => (byte)(_argb >> 16);
        public readonly byte G => (byte)(_argb >> 8);
        public readonly byte B => (byte)_argb;
        public readonly bool IsNull => _argb == 0;
        public readonly bool IsTransparent => _argb < 0x01000000;

        public IntColor(uint value) => _argb = value;

        /// <summary> 0 ~ 255의 byte 색상값으로 초기화 </summary>
        public IntColor(byte r, byte g, byte b) =>
            _argb = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;

        /// <summary> 0 ~ 255의 byte 색상값으로 초기화 </summary>
        public IntColor(byte r, byte g, byte b, byte a) =>
            _argb = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;


        /// <summary> 투명도가 없는 새 색상을 반환한다 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntColor Opaque() => _argb | 0xFF000000;

        /// <summary> 보색의 새 색상을 생성 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntColor Complementary() => Complementary(_argb);

        /// <summary> 밝기 값(luminance)을 기준으로 대비되는 색상 생성 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntColor Contrast() => Contrast(_argb);

        /// <summary> 알파값을 색상값에 미리 반영시킨 새 색상을 생성 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntColor Premul() => Premul(_argb);

        /// <summary> Premul 방식으로 블랜딩하여 새 색상을 생성 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntColor PremulBlend(IntColor blend) => PremulBlend(_argb, blend);

        /// <summary> Red과 Blue의 위치를 스위칭한 새 색상을 생성(ARGB => ABGR or ABGR => ARGB) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntColor SwitchRB() => SwitchRB(_argb);


        /// <summary> Alpha값을 0.0f ~ 1.0f 사이의 투명도로 변환한다 </summary>
        public float AsTransparency() => (255 - A) / 255f;

        /// <summary> 0 ~ 1f 범위의 투명도 값 </summary>
        public IntColor WithTransparency(double transparency) => WithTransparency((float)transparency);

        /// <summary> 0 ~ 1f 범위의 투명도 값 </summary>
        public IntColor WithTransparency(float transparency)
        {
            var alpha = To255(1 - transparency);
            return (alpha << 24) | (_argb & 0x00FFFFFF);
        }

        /// <summary> 0 ~ 255 범위의 byte 값 </summary>
        public IntColor WithAlpha(byte a) => (uint)a << 24 | (_argb & 0x00FFFFFF);

        /// <summary> 0 ~ 255 범위의 byte 값 </summary>
        public IntColor WithRed(byte r) => (uint)r << 16 | (_argb & 0xFF00FFFF);

        /// <summary> 0 ~ 255 범위의 byte 값 </summary>
        public IntColor WithGreen(byte g) => (uint)g << 8 | (_argb & 0xFFFF00FF);

        /// <summary> 0 ~ 255 범위의 byte 값 </summary>
        public IntColor WithBlue(byte b) => (uint)b | (_argb & 0xFFFFFF00);


        /// <summary> 0.0f ~ 1.0f 범위의 float 값 </summary>
        public IntColor WithAlphaF(float a) => To255(a) << 24 | (_argb & 0x00FFFFFF);

        /// <summary> 0.0f ~ 1.0f 범위의 float 값 </summary>
        public IntColor WithRedF(float r) => To255(r) << 16 | (_argb & 0xFF00FFFF);

        /// <summary> 0.0f ~ 1.0f 범위의 float 값 </summary>
        public IntColor WithGreenF(float g) => To255(g) << 8 | (_argb & 0xFFFF00FF);

        /// <summary> 0.0f ~ 1.0f 범위의 float 값 </summary>
        public IntColor WithBlueF(float b) => To255(b) | (_argb & 0xFFFFFF00);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint To255(float v)
        {
            if (Sse.IsSupported)
                return (uint)Sse.ConvertToInt32(Vector128.CreateScalarUnsafe(v * 255f));
            else
                return (uint)(int)(v * 255f + 0.5f);
        }

        /// <summary> 보색의 새 색상을 생성 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntColor Complementary(IntColor color)
        {
            var c = (uint)color;
            var r = 255u - (byte)(c >> 16);
            var g = 255u - (byte)(c >> 8);
            var b = 255u - (byte)(c);

            return (IntColor)((c & 0xFF000000) | (r << 16) | (g << 8) | b);
        }

        /// <summary> 밝기 값(luminance)을 기준으로 대비되는 색상 계산 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntColor Contrast(IntColor color)
        {
            var c = (uint)color;
            var r = (byte)(c >> 16);
            var g = (byte)(c >> 8);
            var b = (byte)(c);

            // 밝기 값(luminance)
            var luminance = 0.299f * r + 0.587f * g + 0.114f * b;

            // 기본은 128을 기준으로 어두운쪽 밝은쪽을 나누는데
            // 자체적인 기준으로 값을 200으로 변경하였음
            return luminance < 200 ? IntColors.WhiteSmoke : IntColors.DimGray;
        }

        /// <summary> 알파값을 색상값에 미리 반영시킨 새 색상을 생성 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntColor Premul(IntColor color)
        {
            var a = color >> 24;
            var c1 = (color & 0x00FF00FF) * (a + 1) & 0xFF00FF00;
            var c2 = (color & 0x0000FF00) * (a + 1) & 0x00FF0000;
            return (c1 | c2) >> 8 | a << 24;
        }

        /// <summary> Premul 방식으로 블랜딩하여 새 색상을 생성 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntColor PremulBlend(IntColor target, IntColor color)
        {
            var a = color >> 24;
            var c1 = (color & 0x00FF00FF) * (a + 1) & 0xFF00FF00;
            var c2 = (color & 0x0000FF00) * (a + 1) & 0x00FF0000;
            var p = (c1 | c2) >> 8 | a << 24;

            var s = 256 - a;
            var d1 = ((target & 0x00FF00FF) * s >> 8) & 0x00FF00FF;
            var d2 = ((target & 0xFF00FF00) >> 8) * s & 0xFF00FF00;
            return (d1 | d2) + p;
        }

        /// <summary> Red과 Blue의 위치를 스위칭한 새 색상을 생성(ARGB => ABGR or ABGR => ARGB) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntColor SwitchRB(IntColor color) => ((color >> 16) | (color << 16)) & 0x00FF00FF | color & 0xFF00FF00;

        /// <summary> 0.0f ~ 1.0f의 float 색상값으로 IntColor 생성 </summary>
        public static IntColor FromRGBFloat(float r, float g, float b)
        {
            if (Sse.IsSupported)
            {
                var rint = (uint)Sse.ConvertToInt32(Vector128.CreateScalarUnsafe(r * 255f));
                var gint = (uint)Sse.ConvertToInt32(Vector128.CreateScalarUnsafe(g * 255f));
                var bint = (uint)Sse.ConvertToInt32(Vector128.CreateScalarUnsafe(b * 255f));
                return 0xFF000000 | rint << 16 | gint << 8 | bint;
            }
            else
            {
                var rint = (uint)(int)(r * 255f + 0.5f);
                var gint = (uint)(int)(g * 255f + 0.5f);
                var bint = (uint)(int)(b * 255f + 0.5f);
                return 0xFF000000 | rint << 16 | gint << 8 | bint;
            }
        }

        /// <summary> 0.0f ~ 1.0f의 float 색상값으로 IntColor 생성 </summary>
        public static IntColor FromRGBAFloat(float r, float g, float b, float a)
        {
            if (Sse.IsSupported)
            {
                var rint = (uint)Sse.ConvertToInt32(Vector128.CreateScalarUnsafe(r * 255f));
                var gint = (uint)Sse.ConvertToInt32(Vector128.CreateScalarUnsafe(g * 255f));
                var bint = (uint)Sse.ConvertToInt32(Vector128.CreateScalarUnsafe(b * 255f));
                var aint = (uint)Sse.ConvertToInt32(Vector128.CreateScalarUnsafe(a * 255f));
                return aint << 24 | rint << 16 | gint << 8 | bint;
            }
            else
            {
                var rint = (uint)(int)(r * 255f + 0.5f);
                var gint = (uint)(int)(g * 255f + 0.5f);
                var bint = (uint)(int)(b * 255f + 0.5f);
                var aint = (uint)(int)(a * 255f + 0.5f);
                return aint << 24 | rint << 16 | gint << 8 | bint;
            }
        }

        /// <summary> ex) #FFFF00, #FFFF00FF, FFFF00  </summary>
        public static IntColor FromHexName(string hexName) => IntColors.FromHex(hexName);

        /// <summary> ex) #FFFF00, #FFFF00FF, FFFF00  </summary>
        public static IntColor FromHexName(ReadOnlySpan<byte> hexASCII) => IntColors.FromHex(hexASCII);

        /// <summary> ex) AliceBlue, darkGreen, Blue, red  </summary>        
        public static IntColor FromColorName(string colorName) => IntColors.FromName(colorName);

        /// <summary> ex) #FFFF00, AliceBlue, FFFF00, darkGreen, Blue, #FFFF00FF  </summary>
        public static IntColor FromUnknownName(string unknownName)
        {
            if (unknownName != null && unknownName.Length != 0)
            {
                if (unknownName[0] != '#' && IntColors.TryFromName(unknownName, out var color))
                    return color;

                return FromHexName(unknownName);
            }
            return 0;
        }

        private static ReadOnlySpan<byte> ToHexASCII => new byte[16]
        {
            (byte)'0', (byte)'1', (byte)'2', (byte)'3',
            (byte)'4', (byte)'5', (byte)'6', (byte)'7',
            (byte)'8', (byte)'9', (byte)'A', (byte)'B',
            (byte)'C', (byte)'D', (byte)'E', (byte)'F'
        };

        public string ToHexText()
        {
            var c = _argb;
            var r = new string(default, 8);
            ref var dest0 = ref MemoryMarshal.GetReference<char>(r);
            ref var tohex = ref MemoryMarshal.GetReference(ToHexASCII);
            Unsafe.Add(ref dest0, 0) = (char)Unsafe.Add(ref tohex, c >> 28);
            Unsafe.Add(ref dest0, 1) = (char)Unsafe.Add(ref tohex, c >> 24 & 15);
            Unsafe.Add(ref dest0, 2) = (char)Unsafe.Add(ref tohex, c >> 20 & 15);
            Unsafe.Add(ref dest0, 3) = (char)Unsafe.Add(ref tohex, c >> 16 & 15);
            Unsafe.Add(ref dest0, 4) = (char)Unsafe.Add(ref tohex, c >> 12 & 15);
            Unsafe.Add(ref dest0, 5) = (char)Unsafe.Add(ref tohex, c >> 08 & 15);
            Unsafe.Add(ref dest0, 6) = (char)Unsafe.Add(ref tohex, c >> 04 & 15);
            Unsafe.Add(ref dest0, 7) = (char)Unsafe.Add(ref tohex, c >> 00 & 15);
            return r;
        }

        public string ToHexTextWithoutAlpha()
        {
            var c = _argb;
            var r = new string(default, 6);
            ref var dest0 = ref MemoryMarshal.GetReference<char>(r);
            ref var tohex = ref MemoryMarshal.GetReference(ToHexASCII);
            Unsafe.Add(ref dest0, 0) = (char)Unsafe.Add(ref tohex, c >> 20 & 15);
            Unsafe.Add(ref dest0, 1) = (char)Unsafe.Add(ref tohex, c >> 16 & 15);
            Unsafe.Add(ref dest0, 2) = (char)Unsafe.Add(ref tohex, c >> 12 & 15);
            Unsafe.Add(ref dest0, 3) = (char)Unsafe.Add(ref tohex, c >> 08 & 15);
            Unsafe.Add(ref dest0, 4) = (char)Unsafe.Add(ref tohex, c >> 04 & 15);
            Unsafe.Add(ref dest0, 5) = (char)Unsafe.Add(ref tohex, c >> 00 & 15);
            return r;
        }

        /// <summary> 색상의 유클리드 거리 계산 </summary>
        public int Distance(IntColor other)
        {
            var argb1 = _argb;
            var argb2 = other._argb;
            var r = (byte)(argb1 >> 16) - (byte)(argb2 >> 16);
            var g = (byte)(argb1 >> 08) - (byte)(argb2 >> 08);
            var b = (byte)(argb1 >> 00) - (byte)(argb2 >> 00);
            return r * r + g * g + b * b;
        }

        /// <summary> 색상의 유클리드 거리 계산 (알파값 포함) </summary>
        public int DistanceWithAlpha(IntColor other)
        {
            var argb1 = _argb;
            var argb2 = other._argb;
            var a = (byte)(argb1 >> 24) - (byte)(argb2 >> 24);
            var r = (byte)(argb1 >> 16) - (byte)(argb2 >> 16);
            var g = (byte)(argb1 >> 08) - (byte)(argb2 >> 08);
            var b = (byte)(argb1 >> 00) - (byte)(argb2 >> 00);
            return a * a + r * r + g * g + b * b;
        }

        public static implicit operator uint(IntColor color) => color._argb;
        public static implicit operator IntColor(uint color) => new(color);

        public static bool operator ==(IntColor left, IntColor right) => left._argb == right._argb;
        public static bool operator !=(IntColor left, IntColor right) => left._argb != right._argb;

        public bool Equals(IntColor color) => color._argb == _argb;
        public override bool Equals(object other) => other is IntColor color && Equals(color);
        public override int GetHashCode() => (int)_argb;
        public override string ToString() => _argb.ToString();
    }

    public unsafe ref struct IntColors
    {
        public readonly static IntColor Null = 0x00000000;

        public readonly static IntColor AliceBlue = 0xFFF0F8FF;
        public readonly static IntColor AntiqueWhite = 0xFFFAEBD7;
        public readonly static IntColor Aqua = 0xFF00FFFF;
        public readonly static IntColor Aquamarine = 0xFF7FFFD4;
        public readonly static IntColor Azure = 0xFFF0FFFF;
        public readonly static IntColor Beige = 0xFFF5F5DC;
        public readonly static IntColor Bisque = 0xFFFFE4C4;
        public readonly static IntColor Black = 0xFF000000;
        public readonly static IntColor BlanchedAlmond = 0xFFFFEBCD;
        public readonly static IntColor Blue = 0xFF0000FF;
        public readonly static IntColor BlueViolet = 0xFF8A2BE2;
        public readonly static IntColor Brown = 0xFFA52A2A;
        public readonly static IntColor BurlyWood = 0xFFDEB887;
        public readonly static IntColor CadetBlue = 0xFF5F9EA0;
        public readonly static IntColor Chartreuse = 0xFF7FFF00;
        public readonly static IntColor Chocolate = 0xFFD2691E;
        public readonly static IntColor Coral = 0xFFFF7F50;
        public readonly static IntColor CornflowerBlue = 0xFF6495ED;
        public readonly static IntColor Cornsilk = 0xFFFFF8DC;
        public readonly static IntColor Crimson = 0xFFDC143C;
        public readonly static IntColor Cyan = 0xFF00FFFF;
        public readonly static IntColor DarkBlue = 0xFF00008B;
        public readonly static IntColor DarkCyan = 0xFF008B8B;
        public readonly static IntColor DarkGoldenrod = 0xFFB8860B;
        public readonly static IntColor DarkGray = 0xFFA9A9A9;
        public readonly static IntColor DarkGreen = 0xFF006400;
        public readonly static IntColor DarkKhaki = 0xFFBDB76B;
        public readonly static IntColor DarkMagenta = 0xFF8B008B;
        public readonly static IntColor DarkOliveGreen = 0xFF556B2F;
        public readonly static IntColor DarkOrange = 0xFFFF8C00;
        public readonly static IntColor DarkOrchid = 0xFF9932CC;
        public readonly static IntColor DarkRed = 0xFF8B0000;
        public readonly static IntColor DarkSalmon = 0xFFE9967A;
        public readonly static IntColor DarkSeaGreen = 0xFF8FBC8B;
        public readonly static IntColor DarkSlateBlue = 0xFF483D8B;
        public readonly static IntColor DarkSlateGray = 0xFF2F4F4F;
        public readonly static IntColor DarkTurquoise = 0xFF00CED1;
        public readonly static IntColor DarkViolet = 0xFF9400D3;
        public readonly static IntColor DeepPink = 0xFFFF1493;
        public readonly static IntColor DeepSkyBlue = 0xFF00BFFF;
        public readonly static IntColor DimGray = 0xFF696969;
        public readonly static IntColor DodgerBlue = 0xFF1E90FF;
        public readonly static IntColor Firebrick = 0xFFB22222;
        public readonly static IntColor FloralWhite = 0xFFFFFAF0;
        public readonly static IntColor ForestGreen = 0xFF228B22;
        public readonly static IntColor Fuchsia = 0xFFFF00FF;
        public readonly static IntColor Gainsboro = 0xFFDCDCDC;
        public readonly static IntColor GhostWhite = 0xFFF8F8FF;
        public readonly static IntColor Gold = 0xFFFFD700;
        public readonly static IntColor Goldenrod = 0xFFDAA520;
        public readonly static IntColor Gray = 0xFF808080;
        public readonly static IntColor Green = 0xFF008000;
        public readonly static IntColor GreenYellow = 0xFFADFF2F;
        public readonly static IntColor Honeydew = 0xFFF0FFF0;
        public readonly static IntColor HotPink = 0xFFFF69B4;
        public readonly static IntColor IndianRed = 0xFFCD5C5C;
        public readonly static IntColor Indigo = 0xFF4B0082;
        public readonly static IntColor Ivory = 0xFFFFFFF0;
        public readonly static IntColor Khaki = 0xFFF0E68C;
        public readonly static IntColor Lavender = 0xFFE6E6FA;
        public readonly static IntColor LavenderBlush = 0xFFFFF0F5;
        public readonly static IntColor LawnGreen = 0xFF7CFC00;
        public readonly static IntColor LemonChiffon = 0xFFFFFACD;
        public readonly static IntColor LightBlue = 0xFFADD8E6;
        public readonly static IntColor LightCoral = 0xFFF08080;
        public readonly static IntColor LightCyan = 0xFFE0FFFF;
        public readonly static IntColor LightGoldenrodYellow = 0xFFFAFAD2;
        public readonly static IntColor LightGray = 0xFFD3D3D3;
        public readonly static IntColor LightGreen = 0xFF90EE90;
        public readonly static IntColor LightPink = 0xFFFFB6C1;
        public readonly static IntColor LightSalmon = 0xFFFFA07A;
        public readonly static IntColor LightSeaGreen = 0xFF20B2AA;
        public readonly static IntColor LightSkyBlue = 0xFF87CEFA;
        public readonly static IntColor LightSlateGray = 0xFF778899;
        public readonly static IntColor LightSteelBlue = 0xFFB0C4DE;
        public readonly static IntColor LightYellow = 0xFFFFFFE0;
        public readonly static IntColor Lime = 0xFF00FF00;
        public readonly static IntColor LimeGreen = 0xFF32CD32;
        public readonly static IntColor Linen = 0xFFFAF0E6;
        public readonly static IntColor Magenta = 0xFFFF00FF;
        public readonly static IntColor Maroon = 0xFF800000;
        public readonly static IntColor MediumAquamarine = 0xFF66CDAA;
        public readonly static IntColor MediumBlue = 0xFF0000CD;
        public readonly static IntColor MediumOrchid = 0xFFBA55D3;
        public readonly static IntColor MediumPurple = 0xFF9370DB;
        public readonly static IntColor MediumSeaGreen = 0xFF3CB371;
        public readonly static IntColor MediumSlateBlue = 0xFF7B68EE;
        public readonly static IntColor MediumSpringGreen = 0xFF00FA9A;
        public readonly static IntColor MediumTurquoise = 0xFF48D1CC;
        public readonly static IntColor MediumVioletRed = 0xFFC71585;
        public readonly static IntColor MidnightBlue = 0xFF191970;
        public readonly static IntColor MintCream = 0xFFF5FFFA;
        public readonly static IntColor MistyRose = 0xFFFFE4E1;
        public readonly static IntColor Moccasin = 0xFFFFE4B5;
        public readonly static IntColor NavajoWhite = 0xFFFFDEAD;
        public readonly static IntColor Navy = 0xFF000080;
        public readonly static IntColor OldLace = 0xFFFDF5E6;
        public readonly static IntColor Olive = 0xFF808000;
        public readonly static IntColor OliveDrab = 0xFF6B8E23;
        public readonly static IntColor Orange = 0xFFFFA500;
        public readonly static IntColor OrangeRed = 0xFFFF4500;
        public readonly static IntColor Orchid = 0xFFDA70D6;
        public readonly static IntColor PaleGoldenrod = 0xFFEEE8AA;
        public readonly static IntColor PaleGreen = 0xFF98FB98;
        public readonly static IntColor PaleTurquoise = 0xFFAFEEEE;
        public readonly static IntColor PaleVioletRed = 0xFFDB7093;
        public readonly static IntColor PapayaWhip = 0xFFFFEFD5;
        public readonly static IntColor PeachPuff = 0xFFFFDAB9;
        public readonly static IntColor Peru = 0xFFCD853F;
        public readonly static IntColor Pink = 0xFFFFC0CB;
        public readonly static IntColor Plum = 0xFFDDA0DD;
        public readonly static IntColor PowderBlue = 0xFFB0E0E6;
        public readonly static IntColor Purple = 0xFF800080;
        public readonly static IntColor Red = 0xFFFF0000;
        public readonly static IntColor RosyBrown = 0xFFBC8F8F;
        public readonly static IntColor RoyalBlue = 0xFF4169E1;
        public readonly static IntColor SaddleBrown = 0xFF8B4513;
        public readonly static IntColor Salmon = 0xFFFA8072;
        public readonly static IntColor SandyBrown = 0xFFF4A460;
        public readonly static IntColor SeaGreen = 0xFF2E8B57;
        public readonly static IntColor SeaShell = 0xFFFFF5EE;
        public readonly static IntColor Sienna = 0xFFA0522D;
        public readonly static IntColor Silver = 0xFFC0C0C0;
        public readonly static IntColor SkyBlue = 0xFF87CEEB;
        public readonly static IntColor SlateBlue = 0xFF6A5ACD;
        public readonly static IntColor SlateGray = 0xFF708090;
        public readonly static IntColor Snow = 0xFFFFFAFA;
        public readonly static IntColor SpringGreen = 0xFF00FF7F;
        public readonly static IntColor SteelBlue = 0xFF4682B4;
        public readonly static IntColor Tan = 0xFFD2B48C;
        public readonly static IntColor Teal = 0xFF008080;
        public readonly static IntColor Thistle = 0xFFD8BFD8;
        public readonly static IntColor Tomato = 0xFFFF6347;
        public readonly static IntColor Transparent = 0x00FFFFFF;
        public readonly static IntColor Turquoise = 0xFF40E0D0;
        public readonly static IntColor Violet = 0xFFEE82EE;
        public readonly static IntColor Wheat = 0xFFF5DEB3;
        public readonly static IntColor White = 0xFFFFFFFF;
        public readonly static IntColor WhiteSmoke = 0xFFF5F5F5;
        public readonly static IntColor Yellow = 0xFFFFFF00;
        public readonly static IntColor YellowGreen = 0xFF9ACD32;

        private static readonly StrTo<IntColor> _colorMap;
        private static readonly byte[] _hexMap;
        private const byte ERR = 255;

        internal static IntColor FromName(string colorName) =>
            _colorMap[colorName.ToLowerInvariant()];
        internal static bool TryFromName(string colorName, out IntColor color) =>
            _colorMap.Get(colorName.ToLowerInvariant(), out color);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IntColor FromHex(ReadOnlySpan<byte> hexASCII)
        {
            var r = 0u;
            var l = hexASCII.Length;
            if (l > 0)
            {
                ref var t = ref MemoryMarshal.GetReference(hexASCII);
                if (t == '#') { t = ref Unsafe.Add(ref t, 1); l--; }

                if (l >= 6)
                {
                    ref var hm = ref MemoryMarshal.GetArrayDataReference(_hexMap);
                    var h1 = Unsafe.Add(ref hm, Unsafe.Add(ref t, 0));
                    var h2 = Unsafe.Add(ref hm, Unsafe.Add(ref t, 1));
                    var h3 = Unsafe.Add(ref hm, Unsafe.Add(ref t, 2));
                    var h4 = Unsafe.Add(ref hm, Unsafe.Add(ref t, 3));
                    var h5 = Unsafe.Add(ref hm, Unsafe.Add(ref t, 4));
                    var h6 = Unsafe.Add(ref hm, Unsafe.Add(ref t, 5));

                    if ((h1 | h2 | h3 | h4 | h5 | h6) < 16)
                    {
                        r = 0xFF000000 |
                            (uint)h1 << 20 | (uint)h2 << 16 |
                            (uint)h3 << 12 | (uint)h4 << 08 |
                            (uint)h5 << 04 | (uint)h6 << 00;

                        if (l >= 8)
                        {
                            var h7 = Unsafe.Add(ref hm, Unsafe.Add(ref t, 6));
                            var h8 = Unsafe.Add(ref hm, Unsafe.Add(ref t, 7));

                            if ((h7 | h8) < 16)
                                r = r << 8 | (uint)h7 << 4 | h8;
                        }
                    }
                }
            }
            return r;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IntColor FromHex(ReadOnlySpan<char> hexChars)
        {
            var r = 0u;
            var l = hexChars.Length;
            if (l > 0)
            {
                ref var t = ref MemoryMarshal.GetReference(hexChars);
                if (t == '#') { t = ref Unsafe.Add(ref t, 1); l--; }

                if (l >= 6)
                {
                    ref var hm = ref MemoryMarshal.GetArrayDataReference(_hexMap);
                    var h1 = Unsafe.Add(ref hm, (byte)Unsafe.Add(ref t, 0));
                    var h2 = Unsafe.Add(ref hm, (byte)Unsafe.Add(ref t, 1));
                    var h3 = Unsafe.Add(ref hm, (byte)Unsafe.Add(ref t, 2));
                    var h4 = Unsafe.Add(ref hm, (byte)Unsafe.Add(ref t, 3));
                    var h5 = Unsafe.Add(ref hm, (byte)Unsafe.Add(ref t, 4));
                    var h6 = Unsafe.Add(ref hm, (byte)Unsafe.Add(ref t, 5));

                    if ((h1 | h2 | h3 | h4 | h5 | h6) < 16)
                    {
                        r = 0xFF000000 |
                            (uint)h1 << 20 | (uint)h2 << 16 |
                            (uint)h3 << 12 | (uint)h4 << 08 |
                            (uint)h5 << 04 | (uint)h6 << 00;

                        if (l >= 8)
                        {
                            var h7 = Unsafe.Add(ref hm, (byte)Unsafe.Add(ref t, 6));
                            var h8 = Unsafe.Add(ref hm, (byte)Unsafe.Add(ref t, 7));

                            if ((h7 | h8) < 16)
                                r = r << 8 | (uint)h7 << 4 | h8;
                        }
                    }
                }
            }
            return r;
        }

        static IntColors()
        {
            var colorMap = new StrTo<IntColor>(256);
            colorMap.Add("aliceblue", AliceBlue);
            colorMap.Add("antiquewhite", AntiqueWhite);
            colorMap.Add("aqua", Aqua);
            colorMap.Add("aquamarine", Aquamarine);
            colorMap.Add("azure", Azure);
            colorMap.Add("beige", Beige);
            colorMap.Add("bisque", Bisque);
            colorMap.Add("black", Black);
            colorMap.Add("blanchedalmond", BlanchedAlmond);
            colorMap.Add("blue", Blue);
            colorMap.Add("blueviolet", BlueViolet);
            colorMap.Add("brown", Brown);
            colorMap.Add("burlywood", BurlyWood);
            colorMap.Add("cadetblue", CadetBlue);
            colorMap.Add("chartreuse", Chartreuse);
            colorMap.Add("chocolate", Chocolate);
            colorMap.Add("coral", Coral);
            colorMap.Add("cornflowerblue", CornflowerBlue);
            colorMap.Add("cornsilk", Cornsilk);
            colorMap.Add("crimson", Crimson);
            colorMap.Add("cyan", Cyan);
            colorMap.Add("darkblue", DarkBlue);
            colorMap.Add("darkcyan", DarkCyan);
            colorMap.Add("darkgoldenrod", DarkGoldenrod);
            colorMap.Add("darkgray", DarkGray);
            colorMap.Add("darkgreen", DarkGreen);
            colorMap.Add("darkkhaki", DarkKhaki);
            colorMap.Add("darkmagenta", DarkMagenta);
            colorMap.Add("darkolivegreen", DarkOliveGreen);
            colorMap.Add("darkorange", DarkOrange);
            colorMap.Add("darkorchid", DarkOrchid);
            colorMap.Add("darkred", DarkRed);
            colorMap.Add("darksalmon", DarkSalmon);
            colorMap.Add("darkseagreen", DarkSeaGreen);
            colorMap.Add("darkslateblue", DarkSlateBlue);
            colorMap.Add("darkslategray", DarkSlateGray);
            colorMap.Add("darkturquoise", DarkTurquoise);
            colorMap.Add("darkviolet", DarkViolet);
            colorMap.Add("deeppink", DeepPink);
            colorMap.Add("deepskyblue", DeepSkyBlue);
            colorMap.Add("dimgray", DimGray);
            colorMap.Add("dodgerblue", DodgerBlue);
            colorMap.Add("firebrick", Firebrick);
            colorMap.Add("floralwhite", FloralWhite);
            colorMap.Add("forestgreen", ForestGreen);
            colorMap.Add("fuchsia", Fuchsia);
            colorMap.Add("gainsboro", Gainsboro);
            colorMap.Add("ghostwhite", GhostWhite);
            colorMap.Add("gold", Gold);
            colorMap.Add("goldenrod", Goldenrod);
            colorMap.Add("gray", Gray);
            colorMap.Add("green", Green);
            colorMap.Add("greenyellow", GreenYellow);
            colorMap.Add("honeydew", Honeydew);
            colorMap.Add("hotpink", HotPink);
            colorMap.Add("indianred", IndianRed);
            colorMap.Add("indigo", Indigo);
            colorMap.Add("ivory", Ivory);
            colorMap.Add("khaki", Khaki);
            colorMap.Add("lavender", Lavender);
            colorMap.Add("lavenderblush", LavenderBlush);
            colorMap.Add("lawngreen", LawnGreen);
            colorMap.Add("lemonchiffon", LemonChiffon);
            colorMap.Add("lightblue", LightBlue);
            colorMap.Add("lightcoral", LightCoral);
            colorMap.Add("lightcyan", LightCyan);
            colorMap.Add("lightgoldenrodyellow", LightGoldenrodYellow);
            colorMap.Add("lightgray", LightGray);
            colorMap.Add("lightgreen", LightGreen);
            colorMap.Add("lightpink", LightPink);
            colorMap.Add("lightsalmon", LightSalmon);
            colorMap.Add("lightseagreen", LightSeaGreen);
            colorMap.Add("lightskyblue", LightSkyBlue);
            colorMap.Add("lightslategray", LightSlateGray);
            colorMap.Add("lightsteelblue", LightSteelBlue);
            colorMap.Add("lightyellow", LightYellow);
            colorMap.Add("lime", Lime);
            colorMap.Add("limegreen", LimeGreen);
            colorMap.Add("linen", Linen);
            colorMap.Add("magenta", Magenta);
            colorMap.Add("maroon", Maroon);
            colorMap.Add("mediumaquamarine", MediumAquamarine);
            colorMap.Add("mediumblue", MediumBlue);
            colorMap.Add("mediumorchid", MediumOrchid);
            colorMap.Add("mediumpurple", MediumPurple);
            colorMap.Add("mediumseagreen", MediumSeaGreen);
            colorMap.Add("mediumslateblue", MediumSlateBlue);
            colorMap.Add("mediumspringgreen", MediumSpringGreen);
            colorMap.Add("mediumturquoise", MediumTurquoise);
            colorMap.Add("mediumvioletred", MediumVioletRed);
            colorMap.Add("midnightblue", MidnightBlue);
            colorMap.Add("mintcream", MintCream);
            colorMap.Add("mistyrose", MistyRose);
            colorMap.Add("moccasin", Moccasin);
            colorMap.Add("navajowhite", NavajoWhite);
            colorMap.Add("navy", Navy);
            colorMap.Add("oldlace", OldLace);
            colorMap.Add("olive", Olive);
            colorMap.Add("olivedrab", OliveDrab);
            colorMap.Add("orange", Orange);
            colorMap.Add("orangered", OrangeRed);
            colorMap.Add("orchid", Orchid);
            colorMap.Add("palegoldenrod", PaleGoldenrod);
            colorMap.Add("palegreen", PaleGreen);
            colorMap.Add("paleturquoise", PaleTurquoise);
            colorMap.Add("palevioletred", PaleVioletRed);
            colorMap.Add("papayawhip", PapayaWhip);
            colorMap.Add("peachpuff", PeachPuff);
            colorMap.Add("peru", Peru);
            colorMap.Add("pink", Pink);
            colorMap.Add("plum", Plum);
            colorMap.Add("powderblue", PowderBlue);
            colorMap.Add("purple", Purple);
            colorMap.Add("red", Red);
            colorMap.Add("rosybrown", RosyBrown);
            colorMap.Add("royalblue", RoyalBlue);
            colorMap.Add("saddlebrown", SaddleBrown);
            colorMap.Add("salmon", Salmon);
            colorMap.Add("sandybrown", SandyBrown);
            colorMap.Add("seagreen", SeaGreen);
            colorMap.Add("seashell", SeaShell);
            colorMap.Add("sienna", Sienna);
            colorMap.Add("silver", Silver);
            colorMap.Add("skyblue", SkyBlue);
            colorMap.Add("slateblue", SlateBlue);
            colorMap.Add("slategray", SlateGray);
            colorMap.Add("snow", Snow);
            colorMap.Add("springgreen", SpringGreen);
            colorMap.Add("steelblue", SteelBlue);
            colorMap.Add("tan", Tan);
            colorMap.Add("teal", Teal);
            colorMap.Add("thistle", Thistle);
            colorMap.Add("tomato", Tomato);
            colorMap.Add("transparent", Transparent);
            colorMap.Add("turquoise", Turquoise);
            colorMap.Add("violet", Violet);
            colorMap.Add("wheat", Wheat);
            colorMap.Add("white", White);
            colorMap.Add("whitesmoke", WhiteSmoke);
            colorMap.Add("yellow", Yellow);
            colorMap.Add("yellowgreen", YellowGreen);
            _colorMap = colorMap;

            var hexMap = new byte[256];
            var l = hexMap.Length;
            do hexMap[--l] = ERR;
            while (l != 0);
            hexMap['0'] = 0x00;
            hexMap['1'] = 0x01;
            hexMap['2'] = 0x02;
            hexMap['3'] = 0x03;
            hexMap['4'] = 0x04;
            hexMap['5'] = 0x05;
            hexMap['6'] = 0x06;
            hexMap['7'] = 0x07;
            hexMap['8'] = 0x08;
            hexMap['9'] = 0x09;
            hexMap['A'] = 0x0A;
            hexMap['B'] = 0x0B;
            hexMap['C'] = 0x0C;
            hexMap['D'] = 0x0D;
            hexMap['E'] = 0x0E;
            hexMap['F'] = 0x0F;
            hexMap['a'] = 0x0A;
            hexMap['b'] = 0x0B;
            hexMap['c'] = 0x0C;
            hexMap['d'] = 0x0D;
            hexMap['e'] = 0x0E;
            hexMap['f'] = 0x0F;
            _hexMap = hexMap;
        }
    }
}