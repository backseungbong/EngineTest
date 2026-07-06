using JHLib.Graphics;
using JHLib.Util.Geometry;
using JHLib.Util.Projection;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;

namespace JHLib.S57.Chart
{
    public class QueryInfo
    {
        public QueryInfo(uint rcid, string chartName, string objectName, string objectInfo, string textValue = "", bool isImage = false)
        {
            RCID = rcid;
            ChartName = chartName;
            ObjectName = objectName;
            ObjectInfo = objectInfo;
            TextValue = textValue;
            IsImage = isImage;
        }

        public uint RCID = 0;
        public string ChartName = "";
        public string ObjectName = "";
        public string ObjectInfo = "";
        public bool IsImage = false;
        public string TextValue = "";
    }

    public partial class S57ChartRenderer
    {
        public delegate void ResultChartQueryInfo(List<QueryInfo> listPoint, List<QueryInfo> listLine, List<QueryInfo> listArea);
        public event ResultChartQueryInfo? OnResultChartQueryInfo;

        // Query관련 변수
        public bool IsQuery = false;

        private List<QueryInfo> _listPoint = new();
        private List<QueryInfo> _listLine = new();
        private List<QueryInfo> _listArea = new();

        // 클릭 화면 위치 저장 변수
        public Float2D CursorPos = new Float2D(float.MinValue, float.MinValue);

        public void Query(Transform projection)
        {
            if (CursorPos.X == float.MinValue || CursorPos.Y == float.MinValue) return;

            lock (_chartLock)
            {
                // Query Reset
                foreach (var chart in DicChart) chart.Value.ResetQuery();

                var point = CursorPos;

                // Query 시작
                lock (_chartLock)
                {
                    _listPoint.Clear();
                    _listLine.Clear();
                    _listArea.Clear();

                    IsQuery = true;
                    foreach (var chart in ListChartInfo)
                    {
                        var name = chart.ChartName;
                        if (DicChart.ContainsKey(name) == true && CheckIndexMap(name) == false)
                        {
                            DicChart[name].Query(projection, point, _listPoint, _listLine, _listArea);
                        }
                    }

                    OnResultChartQueryInfo?.Invoke(_listPoint, _listLine, _listArea);
                }

                CursorPos = new Float2D(float.MinValue, float.MinValue);

            }
        }

        
        public void SetQuery(Float2D pos)
        {
            CursorPos = pos;
            PendingDrawing();
        }

        public void ResetQuery()
        {
            if (IsQuery == false) return;

            IsQuery = false;
            CursorPos = new Float2D(float.MinValue, float.MinValue);

            lock (_chartLock)  foreach (var chart in DicChart) chart.Value.ResetQuery();

            PendingDrawing();
        }

        public string QueryChartName(Float2D point)
        {
            lock (_chartLock)
            {
                var wxy = EPSG3857.ToWorld(point);
                var listChart = ListChartInfo.ToList();
                foreach(var chart in listChart)
                {
                    if (CheckIndexMap(chart.ChartName) == true) continue;
                    foreach(var cov in chart.PathsChart)
                    {
                        if (GeometryHelper.PointInPolygonWinding(wxy, cov) == true)
                        {
                            return chart.ChartName;
                        }
                    }
                }
            }

            return null;
        }
    }
}
