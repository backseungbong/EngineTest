using JHLib.Util.ByteControl;
using JHLib.Util.Hash;
using JHLib.Util.List;
using JHLib.Util.Pool;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.Util.SVG
{
    public class SVGCSSStyleSheets
    {
        private readonly StrTo<SVGCSSTable> _cssMap = new();
        public SVGCSSTable Get(string href) => href != null && href.Length > 0 ? _cssMap[href.ToLowerInvariant()] : null;
        public void AddCSSStyleSheet(string addDirectory)
        {
            try
            {
                if (Directory.Exists(addDirectory))
                {
                    foreach (var cssPath in Directory.EnumerateFiles(addDirectory, "*.css"))
                    {
                        var lowerName = Path.GetFileName(cssPath).Trim().ToLowerInvariant();
                        if (_cssMap.AddOrRefValue(lowerName, out var refValue))
                            refValue.Value = SVGCSS.Read(cssPath);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }
    }

    public class SVGCSSTable
    {
        private StrTo<SVGStyle[]> _typeStyle = new();
        private StrTo<SVGStyle[]> _classStyle = new();

        public void AddTypeStyle(string type, SVGStyle[] styles) => _typeStyle.Add(type, styles);
        public void AddClassStyle(string type, SVGStyle[] styles) => _classStyle.Add(type, styles);
        public void GetStyles(string cssType, string cssClass, TList<SVGStyle> result)
        {
            int i, j;
            var items = FastASCII.SplitTrim(cssClass, ' ');
            if (items.Length != 0)
            {
                if (_typeStyle.Get(cssType, out var styles))
                {
                    i = 0;
                    do
                    {
                        if (styles[i].CSSClass == null) result.Add(styles[i]);
                        else
                        {
                            j = 0;
                            do
                            {
                                if (styles[i].CSSClass == items[j])
                                {
                                    result.Add(styles[i]);
                                    items[j] = null;
                                    break;
                                }
                            }
                            while (++j < items.Length);
                        }
                    }
                    while (++i < styles.Length);
                }

                j = 0;
                do
                {
                    if (items[j] != null && _classStyle.Get(items[j], out styles))
                        result.AddRange(styles);
                }
                while (++j < items.Length);
            }
            else
            {
                if (_typeStyle.Get(cssType, out var styles))
                {
                    i = 0;
                    do if (styles[i].CSSClass == null) result.Add(styles[i]);
                    while (++i < styles.Length);
                }
            }
        }
    }

    public unsafe class SVGCSS
    {
        [StructLayout(LayoutKind.Sequential, Size = SIZE)]
        private struct EndSigns // 데이타 끝에 Ascii 코드를 추가하여 오버 플로우 대비
        {
            private const int SIZE = 64;
            private static readonly byte[] Signs =
            [
                ASCII.SPACE,
                ASCII.LBRACE,
                ASCII.SPACE,
                ASCII.RBRACE,
                ASCII.SLASH,
                ASCII.ASTERISK,
                ASCII.ASTERISK,
                ASCII.SLASH,
            ];

            public static readonly EndSigns Block;
            static EndSigns()
            {
                var t = new EndSigns();
                var b = (byte*)&t;
                for (var i = 0; i < SIZE; i++)
                    b[i] = Signs[i % Signs.Length];
                Block = t;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* PassCommentOrSpace(byte* p)
        {
            var t = p;
            while (true)
            {
                if (*t > ASCII.SPACE)
                {
                    if (*t != ASCII.SLASH) return t;
                    if (*++t == ASCII.ASTERISK)
                    {
                        do while (*++t != ASCII.SLASH) ;
                        while (t[-1] != ASCII.ASTERISK);
                    }
                }
                t++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* ToEqual(byte* p, byte b)
        {
            while (true)
            {
                if (p[0] != b)
                    if (p[1] != b)
                        if (p[2] != b)
                            if (p[3] != b) p += 4;
                            else return p + 3;
                        else return p + 2;
                    else return p + 1;
                else return p;
            }
        }

        public static SVGCSSTable Read(string path)
        {
            var table = new SVGCSSTable();

            using var ps = new PoolStream(path, sizeof(EndSigns));
            fixed (byte* p0 = &ps.Stream0)
            {
                var p = p0 - 1;
                var e = p0 + ps.Position;
                *(EndSigns*)e = EndSigns.Block;

                var stylelist = new TList<SVGStyle>(4);
                while (true)
                {
                    p = PassCommentOrSpace(p + 1);
                    if (p >= e) return table;
                    else
                    {
                        var t = p;
                        while (true)
                        {
                            if (*p <= ASCII.SPACE) break;
                            if (*p == ASCII.LBRACE) break;
                            if (*p == ASCII.SLASH) break;
                            p++;
                        }

                        if (t < p)
                        {
                            var d = t; while (*t != ASCII.DOT && ++t < p) ;
                            var tname = FastASCII.ToASCII(d, (int)(t - d));
                            var cname = FastASCII.ToASCII(t + 1, (int)(p - t - 1));

                            p = PassCommentOrSpace(p);
                            if (*p == ASCII.LBRACE)
                            {
                                t = p + 1;
                                p = ToEqual(t, ASCII.RBRACE);
                                SVGStyle.Parse(FastASCII.ToASCII(t, (int)(p - t)), cname, stylelist);

                                if (stylelist.Count != 0)
                                {
                                    var styles = stylelist.ToArrayClear();
                                    if (tname != null)
                                        table.AddTypeStyle(tname, styles);
                                    else if (cname != null)
                                        table.AddClassStyle(cname, styles);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}