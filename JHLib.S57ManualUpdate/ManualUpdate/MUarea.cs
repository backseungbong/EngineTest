using JHLib.Util.Struct;
using System;
using System.Collections.Generic;
using System.Text;

namespace JHLib.S57ManualUpdate.ManualUpdate
{
    public enum EnumAreaType { None, Pattern, FillColor }

    public class MUarea
    {
        public MUarea() { }
        public MUarea(EnumAreaType areaType, int index, byte arpha, EnumLineType lineType, int lineIndex, int lineWidth, EnumPlainLineType plainType)
        {
            AreaType = areaType;
            Index = index;
            Arpha = arpha;
            LineType = lineType;
            LineIndex = lineIndex;
            LineWidth = lineWidth;
            PlainLineType = plainType;
        }

        public EnumAreaType AreaType { get; set; }
        public int Index { get; set; }       // Pattern이면 Pattern Index, FillColor이면 Color Index
        public byte Arpha { get; set; }     // FillColor일 때 투명도
        public EnumLineType LineType { get; set; }
        public int LineIndex { get; set; }
        public int LineWidth { get; set; }   // LineType = Symbolized이면 Symbol Line Index, Plain이면 Color Index
        public EnumPlainLineType PlainLineType { get; set; }
        public List<Float2D> Points { get; set; } = new();

        public MUarea Clone()
        {
            return new MUarea
            {
                AreaType = this.AreaType,
                Index = this.Index,
                Arpha = this.Arpha,
                LineType = this.LineType,
                LineIndex = this.LineIndex,
                LineWidth = this.LineWidth,
                PlainLineType = this.PlainLineType,
                Points = this.Points.ToList()
            };
        }
    }
}
