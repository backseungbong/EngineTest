using JHLib.S57ManualUpdate.ManualUpdate;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;

namespace JHLib.S57.Chart
{
    public class MUsymbolInfo
    {
        public MUsymbolInfo(int symbolIndex, float angle = 0f, float strength = 0)
        {
            SymbolIndex = symbolIndex;
            Angle = angle;
            Strength = strength;
        }

        public int SymbolIndex = -1;
        public float Angle = 0;
        // Actual Tidal or Predicted Tidal을 표시하기 위해서 추가함
        public float Strength = 0;

        public MUsymbolInfo Clone()
        {
            return new MUsymbolInfo(this.SymbolIndex, this.Angle, this.Strength);
        }
    }

    public class MUqueryPoint()
    {
        public List<MUsymbolInfo> SymbolInfos = new();
        public Float2D Pivot = new Float2D(float.MaxValue, float.MaxValue);

        public MUqueryPoint Clone()
        {
            return new MUqueryPoint
            {
                SymbolInfos = this.SymbolInfos.Select(s => s.Clone()).ToList(),
                Pivot = new Float2D(this.Pivot.X, this.Pivot.Y)
            };
        }
    }

    public class ManualUpdateQueryInfo
    {
        public ManualUpdateQueryInfo() { }

        public ManualUpdateQueryInfo(string chartName, string objName, string objACNM)
        {
            ChartName = chartName;
            ObjectName = objName;
            ObjectACNM = objACNM;
        }

        public string ChartName = "";
        public string ObjectName = "";
        public string ObjectACNM = "";
        public MUqueryPoint PointObj = new();

        public ManualUpdateQueryInfo Clone()
        {
            return new ManualUpdateQueryInfo
            {
                ChartName = this.ChartName,
                ObjectName = this.ObjectName,
                ObjectACNM = this.ObjectACNM,
                PointObj = this.PointObj.Clone()
            };
        }
    }

    public partial class S57ChartRenderer
    {
        public delegate void ResultChartManualUpdateQueryInfo(List<ManualUpdateQueryInfo> listPoint);
        public event ResultChartManualUpdateQueryInfo? OnResultChartManualUpdateQueryInfo;

        private List<ManualUpdateQueryInfo> _listMUpoint = new();

        // 클릭 화면 위치 저장 변수
        public Float2D CursorMUqueryPos = new Float2D(float.MinValue, float.MinValue);

        public void SetManualUpdateQuery(Float2D pos)
        {
            CursorMUqueryPos = pos;
            PendingDrawing();
        }

        public void ResetManualUpdateQuery()
        {
            CursorMUqueryPos = new Float2D(float.MinValue, float.MinValue);
        }

        public void ManualUpdateQuery(Transform projection)
        {
            if (CursorMUqueryPos.X == float.MinValue || CursorMUqueryPos.Y == float.MinValue) return;

            lock (_chartLock)
            {
                _listMUpoint.Clear();

                var point = CursorMUqueryPos;
                foreach (var chart in ListChartInfo)
                {
                    var name = chart.ChartName;
                    if (DicChart.ContainsKey(name) == true && CheckIndexMap(name) == false)
                    {
                        DicChart[name].MUquery(projection, name, point, _listMUpoint);
                    }
                }

                OnResultChartManualUpdateQueryInfo?.Invoke(_listMUpoint.ToList());
                CursorMUqueryPos = new Float2D(float.MinValue, float.MinValue);
            }
        }

    }
}
