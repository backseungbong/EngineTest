using JHLib.Util.Struct;
using System;
using System.Collections.Generic;
using System.Text;

namespace JHLib.S57ManualUpdate.ManualUpdate
{
    public enum EnumLineType { Symolized, Plain }
    public enum EnumPlainLineType { Solid, Dash, Dot }

    public class MUline
    {
        public MUline() { }

        public MUline(EnumLineType lineType, int index, int width, EnumPlainLineType plainType)
        {
            LineType = lineType;
            Index = index;
            Width = width;
            PlainLineType = plainType;
        }

        public EnumLineType LineType { get; set; }
        public int Index { get; set; }       // LineType = Symbolized이면 Symbol Line Index, Plain이면 Color Index
        public int Width { get; set; }
        public EnumPlainLineType PlainLineType { get; set; }
        public List<Float2D> Points { get; set; } = new();

        public MUline Clone()
        {
            return new MUline
            {
                LineType = this.LineType,
                Index = this.Index,
                Width = this.Width,
                PlainLineType = this.PlainLineType,
                Points = this.Points.ToList()
            };
        }
    }
}
