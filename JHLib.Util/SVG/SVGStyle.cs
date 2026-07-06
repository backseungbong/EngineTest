using JHLib.Util.ByteControl;
using JHLib.Util.List;
using JHLib.Util.Struct;
using System.Diagnostics;

namespace JHLib.Util.SVG
{
    public enum SVGGeometryType : byte { None, Rect, Path, Circle }
    public enum SVGFillRuleType : byte { NonZero, EvenOdd }
    public enum SVGDisplayType : byte { Inline, None, Block }
    public enum SVGLineCapType : byte { Butt, Round, Square }
    public enum SVGLineJoinType : byte { Miter, Arcs, Bevel, MiterClip, Round }
    public enum SVGStyleType : byte { None, Display, Stroke, StrokeWidth, StrokeLineCap, StrokeLineJoin, StrokeOpacity, Fill, FillOpacity, FillRule }

    public abstract class SVGStyle(SVGStyleType type, string cssClass)
    {
        public readonly SVGStyleType StyleType = type;
        public readonly string CSSClass = cssClass;

        public static void Parse(string commands, string cssClass, TList<SVGStyle> result)
        {
            var styles = FastASCII.SplitTrim(commands, ';');
            for (var i = 0; i < styles.Length; i++)
            {
                var style = FastASCII.SplitTrim(styles[i], ':', 2);
                if (style.Length == 2)
                {
                    var s = style[0];
                    var l = s.Length;
                    if (l >= 2)
                    {
                        var h = (byte)s[0] << 24 | (byte)s[1] << 16 | (byte)s[l - 2] << 8 | (byte)s[l - 1];
                        if (h == ('d' << 24 | 'i' << 16 | 'a' << 8 | 'y')) /// display
                        {
                            result.Add(new SVGDisplay(style[1], cssClass));
                        }
                        else if (h == ('s' << 24 | 't' << 16 | 'k' << 8 | 'e')) /// stroke
                        {
                            result.Add(new SVGStroke(style[1], cssClass));
                        }
                        else if (h == ('s' << 24 | 't' << 16 | 't' << 8 | 'h')) /// stroke-width
                        {
                            result.Add(new SVGStrokeWidth(style[1], cssClass));
                        }
                        else if (h == ('s' << 24 | 't' << 16 | 'a' << 8 | 'p')) /// stroke-linecap
                        {
                            result.Add(new SVGStrokeLineCap(style[1], cssClass));
                        }
                        else if (h == ('s' << 24 | 't' << 16 | 'i' << 8 | 'n')) /// stroke-linejoin
                        {
                            result.Add(new SVGStrokeLineJoin(style[1], cssClass));
                        }
                        else if (h == ('s' << 24 | 't' << 16 | 't' << 8 | 'y')) /// stroke-opacity
                        {
                            result.Add(new SVGStrokeOpacity(style[1], cssClass));
                        }
                        else if (h == ('f' << 24 | 'i' << 16 | 'l' << 8 | 'l')) /// fill
                        {
                            result.Add(new SVGFill(style[1], cssClass));
                        }
                        else if (h == ('f' << 24 | 'i' << 16 | 't' << 8 | 'y')) /// fill-opacity
                        {
                            result.Add(new SVGFillOpacity(style[1], cssClass));
                        }
                        else
                        {
                            Trace.WriteLine($"not supported SVGStyle : {style[0]}");
                        }
                    }
                }
            }
        }

        public static bool TryParse(string style, string value, TList<SVGStyle> result)
        {
            var r = result.Count;
            var s = style.AsSpan();
            var l = s.Length;
            if (l >= 2)
            {
                var h = (byte)s[0] << 24 | (byte)s[1] << 16 | (byte)s[l - 2] << 8 | (byte)s[l - 1];
                if (h == ('d' << 24 | 'i' << 16 | 'a' << 8 | 'y')) /// display
                {
                    result.Add(new SVGDisplay(value));
                }
                else if (h == ('s' << 24 | 't' << 16 | 'k' << 8 | 'e')) /// stroke
                {
                    result.Add(new SVGStroke(value));
                }
                else if (h == ('s' << 24 | 't' << 16 | 't' << 8 | 'h')) /// stroke-width
                {
                    result.Add(new SVGStrokeWidth(value));
                }
                else if (h == ('s' << 24 | 't' << 16 | 'a' << 8 | 'p')) /// stroke-linecap
                {
                    result.Add(new SVGStrokeLineCap(value));
                }
                else if (h == ('s' << 24 | 't' << 16 | 'i' << 8 | 'n')) /// stroke-linejoin
                {
                    result.Add(new SVGStrokeLineJoin(value));
                }
                else if (h == ('s' << 24 | 't' << 16 | 't' << 8 | 'y')) /// stroke-opacity
                {
                    result.Add(new SVGStrokeOpacity(value));
                }
                else if (h == ('f' << 24 | 'i' << 16 | 'l' << 8 | 'l')) /// fill
                {
                    result.Add(new SVGFill(value));
                }
                else if (h == ('f' << 24 | 'i' << 16 | 't' << 8 | 'y')) /// fill-opacity
                {
                    result.Add(new SVGFillOpacity(value));
                }
            }
            return r != result.Count;
        }
    }

    public class SVGDisplay : SVGStyle
    {
        public readonly SVGDisplayType Type;
        public SVGDisplay(string value, string cssClass = null) :
            base(SVGStyleType.Display, cssClass)
        {
            switch (value)
            {
                case "none": Type = SVGDisplayType.None; break;
                case "inline": Type = SVGDisplayType.Inline; break;
                case "block": Type = SVGDisplayType.Block; break;
                default: Trace.WriteLine($"{GetType().Name} invalid {nameof(Type)} value : {value}"); break;
            }
        }
    }

    public class SVGStroke : SVGStyle
    {
        public readonly IntColor Color;
        public SVGStroke(string value, string cssClass = null) :
            base(SVGStyleType.Stroke, cssClass)
        {
            if (value != "none")
                Color = IntColor.FromUnknownName(value);
        }
    }

    public class SVGStrokeWidth : SVGStyle
    {
        public readonly float Width;
        public SVGStrokeWidth(string value, string cssClass = null) :
            base(SVGStyleType.StrokeWidth, cssClass)
        {
            if (float.TryParse(value, out Width) == false)
                Trace.WriteLine($"{GetType().Name} invalid {nameof(Width)} value : {value}");
        }
    }

    public class SVGStrokeOpacity : SVGStyle
    {
        public readonly float Opacity;
        public SVGStrokeOpacity(string value, string cssClass = null) :
            base(SVGStyleType.StrokeOpacity, cssClass)
        {
            if (float.TryParse(value, out Opacity) == false)
                Trace.WriteLine($"{GetType().Name} invalid {nameof(Opacity)} value : {value}");
        }
    }

    public class SVGStrokeLineCap : SVGStyle
    {
        public readonly SVGLineCapType Type;
        public SVGStrokeLineCap(string value, string cssClass = null) :
            base(SVGStyleType.StrokeLineCap, cssClass)
        {
            switch (value)
            {
                case "butt": Type = SVGLineCapType.Butt; break;
                case "round": Type = SVGLineCapType.Round; break;
                case "square": Type = SVGLineCapType.Square; break;
                default: Trace.WriteLine($"{GetType().Name} invalid {nameof(Type)} value : {value}"); break;
            }
        }
    }

    public class SVGStrokeLineJoin : SVGStyle
    {
        public readonly SVGLineJoinType Type;
        public SVGStrokeLineJoin(string value, string cssClass = null) :
            base(SVGStyleType.StrokeLineJoin, cssClass)
        {
            switch (value)
            {
                case "miter": Type = SVGLineJoinType.Miter; break;
                case "arcs": Type = SVGLineJoinType.Arcs; break;
                case "bevel": Type = SVGLineJoinType.Bevel; break;
                case "miter-clip": Type = SVGLineJoinType.MiterClip; break;
                case "round": Type = SVGLineJoinType.Round; break;
                default: Trace.WriteLine($"{GetType().Name} invalid {nameof(Type)} value : {value}"); break;
            }
        }
    }

    public class SVGFill : SVGStyle
    {
        public readonly IntColor Color;
        public SVGFill(string value, string cssClass = null) :
            base(SVGStyleType.Fill, cssClass)
        {
            if (value != "none")
                Color = IntColor.FromUnknownName(value);
        }
    }

    public class SVGFillOpacity : SVGStyle
    {
        public readonly float Opacity;
        public SVGFillOpacity(string value, string cssClass = null) :
            base(SVGStyleType.FillOpacity, cssClass)
        {
            if (float.TryParse(value, out Opacity) == false)
                Trace.WriteLine($"{GetType().Name} invalid {nameof(Opacity)} value : {value}");
        }
    }
}