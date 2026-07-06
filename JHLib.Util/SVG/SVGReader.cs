using JHLib.Util.ByteControl;
using JHLib.Util.List;
using JHLib.Util.Struct;
using JHLib.Util.XML;

namespace JHLib.Util.SVG
{
    public unsafe class SVGReader
    {
        public static SVGData Read(string filePath, SVGCSSStyleSheets cssManager)
        {
            using var xr = new FXReader(filePath, false);

            var cssTable = default(SVGCSSTable);
            if (cssManager != null)
            {
                while (true)
                {
                    if (xr.PrologNext() == false) break;
                    if (xr.ElementEqual("xml-stylesheet") && xr.AttrTarget("href"))
                        cssTable = cssManager.Get(xr.AttrValueASCII);
                }
            }

            if (xr.ElementFind(1, "svg"))
                return ReadXmlElement_SVG(xr, 1, cssTable);

            return null;
        }

        private static SVGData ReadXmlElement_SVG(FXReader xr, int depth, SVGCSSTable cssTable)
        {
            depth++;

            var item = new SVGData();
            var geos = new TList<SVGGeometry>();

            var vb = xr.AttrGetAsFloatArray("viewBox", (byte)' ');
            if (vb != null && vb.Length == 4)
                item.ViewBox = vb;
            else
                item.ViewBox = [0, 0, 1, 1];

            while (xr.ElementNext(depth, out var flByte))
            {
                if (flByte == (ASCII.r << 8 | ASCII.t)) // rect
                {
                    geos.Add(ReadXmlElement_Rect(xr, cssTable));
                }
                else if (flByte == (ASCII.p << 8 | ASCII.h)) // path
                {
                    geos.Add(ReadXmlElement_Path(xr, cssTable));
                }
                else if (flByte == (ASCII.c << 8 | ASCII.e)) // circle
                {
                    geos.Add(ReadXmlElement_Circle(xr, cssTable));
                }
                else if (flByte == (ASCII.d << 8 | ASCII.c)) // desc
                {
                    item.Desc = xr.ContentUTF8;
                }
                else if (flByte == (ASCII.t << 8 | ASCII.e)) // title
                {
                    item.Title = xr.ContentASCII;
                }
                else if (flByte == (ASCII.m << 8 | ASCII.a)) // metadata
                {
                }
                else
                {
                    xr.InvalidElementName();
                }
            }
            item.Geometrys = geos.ToArrayEmpty();
            return item;
        }

        private static SVGRect ReadXmlElement_Rect(FXReader xr, SVGCSSTable cssTable)
        {
            var item = new SVGRect();
            var styles = new TList<SVGStyle>(2);
            while (xr.AttrNext(out var flByte))
            {
                if (flByte == (ASCII.x << 8 | ASCII.x)) // x
                {
                    item.X = xr.AttrValueFloat;
                }
                else if (flByte == (ASCII.y << 8 | ASCII.y)) // y
                {
                    item.Y = xr.AttrValueFloat;
                }
                else if (flByte == (ASCII.w << 8 | ASCII.h)) // width                        
                {
                    item.Width = xr.AttrValueFloat;
                }
                else if (flByte == (ASCII.h << 8 | ASCII.t)) // height                        
                {
                    item.Height = xr.AttrValueFloat;
                }
                else if (flByte == (ASCII.f << 8 | ASCII.l)) // fill
                {
                    styles.Add(new SVGFill(xr.AttrValueASCII, null));
                }
                else if (flByte == (ASCII.c << 8 | ASCII.s)) // class
                {
                    cssTable?.GetStyles("rect", xr.AttrValueASCII, styles);
                }
                else if (flByte == (ASCII.s << 8 | ASCII.e)) // style
                {
                    SVGStyle.Parse(xr.AttrValueASCII, null, styles);
                }
                else
                {
                    if (SVGStyle.TryParse(xr.AttrNameASCII, xr.AttrValueASCII, styles))
                        continue;

                    xr.InvalidAttributeName();
                }
            }
            item.Styles = styles.ToArrayEmpty();
            return item;
        }

        private static SVGCircle ReadXmlElement_Circle(FXReader xr, SVGCSSTable cssTable)
        {
            var item = new SVGCircle();
            var styles = new TList<SVGStyle>(2);
            while (xr.AttrNext(out var flByte))
            {
                if (flByte == (ASCII.r << 8 | ASCII.r)) // r
                {
                    item.Radius = xr.AttrValueFloat;
                }
                else if (flByte == (ASCII.c << 8 | ASCII.x)) // cx                        
                {
                    item.X = xr.AttrValueFloat;
                }
                else if (flByte == (ASCII.c << 8 | ASCII.y)) // cy                        
                {
                    item.Y = xr.AttrValueFloat;
                }
                else if (flByte == (ASCII.f << 8 | ASCII.l)) // fill
                {
                    styles.Add(new SVGFill(xr.AttrValueASCII, null));
                }
                else if (flByte == (ASCII.c << 8 | ASCII.s)) // class
                {
                    cssTable?.GetStyles("circle", xr.AttrValueASCII, styles);
                }
                else if (flByte == (ASCII.s << 8 | ASCII.e)) // style
                {
                    SVGStyle.Parse(xr.AttrValueASCII, null, styles);
                }
                else
                {
                    if (SVGStyle.TryParse(xr.AttrNameASCII, xr.AttrValueASCII, styles))
                        continue;

                    xr.InvalidAttributeName();
                }
            }
            item.Styles = styles.ToArrayEmpty();
            return item;
        }

        private static SVGPath ReadXmlElement_Path(FXReader xr, SVGCSSTable cssTable)
        {
            var item = new SVGPath();
            var styles = new TList<SVGStyle>(2);
            while (xr.AttrNext(out var flByte))
            {
                if (flByte == (ASCII.d << 8 | ASCII.d)) // d     
                {
                    if (xr.AttrValueRange(out var range))
                        item.Paths = ParseSVGPath_Tiny(ref range.Data0, range.Count);
                }
                else if (flByte == (ASCII.f << 8 | ASCII.l)) // fill
                {
                    styles.Add(new SVGFill(xr.AttrValueASCII, null));
                }
                else if (flByte == (ASCII.c << 8 | ASCII.s)) // class
                {
                    cssTable?.GetStyles("path", xr.AttrValueASCII, styles);
                }
                else if (flByte == (ASCII.s << 8 | ASCII.e)) // style
                {
                    SVGStyle.Parse(xr.AttrValueASCII, null, styles);
                }
                else
                {
                    if (SVGStyle.TryParse(xr.AttrNameASCII, xr.AttrValueASCII, styles))
                        continue;

                    xr.InvalidAttributeName();
                }
            }
            item.Styles = styles.ToArrayEmpty();
            return item;
        }

        private static Float2D[][] ParseSVGPath_Tiny(ref byte data0, int l)
        {
            fixed (byte* p = &data0)
                return ParseSVGPath_Tiny(p, l);
        }

        private static Float2D[][] ParseSVGPath_Tiny(byte* p0, int l)
        {
            var p = p0;
            var e = p0 + l;

            var result = new FList<Float2D[]>(2);
            var buffer = new SList<Float2D>(8);
            var point = new Float2D();
            do
            {
                var v = *p & 0xDF;
                if (v != ASCII.M)
                {
                    if (v != ASCII.L)
                    {
                        if (v != ASCII.H)
                        {
                            if (v != ASCII.V)
                            {
                                if (v == ASCII.Z && buffer.Count != 0)
                                    buffer.Add(buffer[0]);
                                p++;
                            }
                            else ParseSVGVerTo(ref p, e, ref point, buffer);
                        }
                        else ParseSVGHorTo(ref p, e, ref point, buffer);
                    }
                    else ParseSVGLineTo(ref p, e, ref point, buffer);
                }
                else
                {
                    if (buffer.Count != 0)
                        result.Add(buffer.ToArrayClear());

                    p++;
                    point.X = ParseFloat(ref p, e);
                    point.Y = ParseFloat(ref p, e);
                    buffer.Add(point);
                }
            }
            while (p < e);

            if (buffer.Count != 0)
                result.Add(buffer.ToArray());

            return result.ToArray();
        }

        private static void ParseSVGLineTo(ref byte* p, byte* e, ref Float2D point, SList<Float2D> l)
        {
            var t = p;
            do
            {
                var v = *t;
                if (v == ASCII.L) point = default;
                else if (v == ASCII.l || v <= ASCII.SPACE || v == ASCII.COMMA) continue;
                else if (ASCII.N0 <= v && v <= ASCII.N9 || v == ASCII.MINUS || v == ASCII.DOT)
                {
                    point.X += ParseFloat(ref t, e);
                    point.Y += ParseFloat(ref t, e);
                    l.Add(point);
                }
                else break;
            }
            while (++t < e);
            p = t;
        }

        private static void ParseSVGHorTo(ref byte* p, byte* e, ref Float2D point, SList<Float2D> l)
        {
            var t = p;
            do
            {
                var v = *t;
                if (v == ASCII.H) point.X = 0;
                else if (v == ASCII.h || v <= ASCII.SPACE || v == ASCII.COMMA) continue;
                else if (ASCII.N0 <= v && v <= ASCII.N9 || v == ASCII.MINUS || v == ASCII.DOT)
                {
                    point.X += ParseFloat(ref t, e);
                    l.Add(point);
                }
                else break;
            }
            while (++t < e);
            p = t;
        }

        private static void ParseSVGVerTo(ref byte* p, byte* e, ref Float2D point, SList<Float2D> l)
        {
            var t = p;
            do
            {
                var v = *t;
                if (v == ASCII.V) point.Y = 0;
                else if (v == ASCII.v || v <= ASCII.SPACE || v == ASCII.COMMA) continue;
                else if (ASCII.N0 <= v && v <= ASCII.N9 || v == ASCII.MINUS || v == ASCII.DOT)
                {
                    point.Y += ParseFloat(ref t, e);
                    l.Add(point);
                }
                else break;
            }
            while (++t < e);
            p = t;
        }

        private static float ParseFloat(ref byte* p, byte* e)
        {
            var t = p;
            var result = 0f;
            var digit = 18;
            var val = 0L;
            var div = 1L;
            do
            {
                if (*t > ASCII.SPACE && *t != ASCII.COMMA)
                {
                    if (*t != ASCII.MINUS)
                    {
                        do
                        {
                            if (*t < '0' || '9' < *t)
                            {
                                if (*t == ASCII.DOT && ++t < e)
                                {
                                    do { if (*t < '0' || '9' < *t) break; val = *t - '0' + val * 10; div *= 10; }
                                    while (++t < e && --digit != 0);
                                }
                                break;
                            }
                            val = *t - '0' + val * 10;
                        }
                        while (++t < e && --digit != 0);
                        result = val / (float)div;
                        break;
                    }
                    else if (++t < e)
                    {
                        do
                        {
                            if (*t < '0' || '9' < *t)
                            {
                                if (*t == ASCII.DOT && ++t < e)
                                {
                                    do { if (*t < '0' || '9' < *t) break; val = *t - '0' + val * 10; div *= 10; }
                                    while (++t < e && --digit != 0);
                                }
                                break;
                            }
                            val = *t - '0' + val * 10;
                        }
                        while (++t < e && --digit != 0);
                        result = -val / (float)div;
                        break;
                    }
                    else break;
                }
            }
            while (++t < e);
            while (digit == 0 && ++t < e && '0' <= *t && *t <= '9') ; p = t;
            return result;
        }
    }
}