using JHLib.Util.Struct;

namespace JHLib.S57ManualUpdate.ManualUpdate
{
    public class SymbolInfo
    {
        public SymbolInfo(int symbolIndex, float angle = 0f, float strength = 0)
        {
            SymbolIndex = symbolIndex;
            Angle = angle;
            Strength = strength;
        }

        public int SymbolIndex { get; set; }
        public float Angle { get; set; }

        // Actual Tidal or Predicted Tidal을 표시하기 위해서 추가함
        public float Strength { get; set; }

        public SymbolInfo Clone()
        {
            return new SymbolInfo(this.SymbolIndex, this.Angle, this.Strength);
        }
    }

    public class MUpoint
    {
        public List<SymbolInfo> SymbolInfos { get; set; } = new();
        public Float2D Pivot { get; set; } = new Float2D(float.MaxValue, float.MaxValue);

        public MUpoint Clone()
        {
            return new MUpoint
            {
                SymbolInfos = this.SymbolInfos.Select(s => s.Clone()).ToList(),
                Pivot = new Float2D(this.Pivot.X, this.Pivot.Y)
            };
        }
    }
}
