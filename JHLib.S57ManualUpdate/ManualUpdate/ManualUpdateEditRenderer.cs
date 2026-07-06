using JHLib.Graphics;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using SkiaSharp;

namespace JHLib.S57ManualUpdate.ManualUpdate
{
    public class ManualUpdateEditRenderer
    {
        public void ReadyToDrawing(GraphicsContext context, SKColor editColor)
        {
            _editColor = editColor;
        }

        public void Drawing(GraphicsContext context)
        {
            context.Clear();

            DrawManualUpdateEdit(context);
        }

        private SKColor _editColor;

        private GraphicsLayer _layer = null;

        private Float2D _cursorPos = new Float2D(float.MaxValue, float.MaxValue);

        // Hit Index를 저장할 변수 
        public int HitIndex = -1;

        private string _chartName = "";

        // Edit시에 정보를 저장할 변수
        public MUmain ActiveEdit = null;
        private readonly object _lockMU = new object();

        public void SetLayer(GraphicsLayer layer) => _layer = layer;

        public void PendingDrawing()
        {
            if (_layer != null)
            {
                _layer.PendingDrawing(true);
            }
        }

        public void SetActiveEdit(EnumGeoType geoType, string name, string objClass, string comment = "", int startDate = 0, int endDate = 0, bool isDelete = false, int id = 0)
        {
            ClearCursorPos();
            HitIndex = -1;
            _chartName = string.Empty;
            ActiveEdit = new MUmain(geoType, name, objClass, comment, startDate, endDate, isDelete, id);
        }

        public void SetActiveEdit(MUmain mu)
        {
            ClearCursorPos();
            HitIndex = -1;
            _chartName = string.Empty;
            ActiveEdit = mu;
        }

        public MUmain GetActiveEdit()
        {
            return ActiveEdit?.Clone();
        }

        public void SetChartName(string chartName)
        {
            if(string.IsNullOrEmpty(_chartName) && _chartName != chartName) _chartName = chartName;
        }

        public string GetChartName()
        {
            return _chartName;
        }

        public void AddPointInfo(int symbolIndex, float angle = 0f, float strength = 0f)
        {
            if (ActiveEdit == null) return;

            ActiveEdit.PointObj.SymbolInfos.Add(new SymbolInfo(symbolIndex, angle, strength));
        }

        public void AddLineInfo(EnumLineType lineType, int index, int width = 1, EnumPlainLineType plainType = EnumPlainLineType.Solid)
        {
            if (ActiveEdit == null || ActiveEdit.GeoType != EnumGeoType.Line) return;

            ActiveEdit.LineObj = new MUline(lineType, index, width, plainType);
        }

        public void AddAreaInfo(EnumAreaType areaType, int index, byte arpha, EnumLineType lineType, int lineIndex, int lineWidth = 1, EnumPlainLineType plainType = EnumPlainLineType.Solid)
        {
            if (ActiveEdit == null || ActiveEdit.GeoType != EnumGeoType.Area) return;

            ActiveEdit.AreaObj = new MUarea(areaType, index, arpha, lineType, lineIndex, lineWidth, plainType);
        }


        public void ClearActiveEdit()
        {
            lock (_lockMU)
            {
                if(ActiveEdit != null)
                {
                    ActiveEdit = null;
                    ClearCursorPos();
                    HitIndex = -1;
                    _chartName = string.Empty;
                }
            }
        }

        public bool CheckPossibleCommit()
        {
            if (ActiveEdit == null) return false;
            if (string.IsNullOrEmpty(_chartName) == true) return false;

            lock (_lockMU)
            {
                switch (ActiveEdit.GeoType)
                {
                    case EnumGeoType.Point:
                        if (ActiveEdit.PointObj.Pivot.X == float.MaxValue || ActiveEdit.PointObj.Pivot.Y == float.MaxValue) return false;
                        break;
                    case EnumGeoType.Line:
                        if (ActiveEdit.LineObj.Points.Count < 2) return false;
                        break;
                    case EnumGeoType.Area:
                        if (ActiveEdit.AreaObj.Points.Count < 3) return false;
                        break;
                }
            }

            return true;
        }

        public void SetCursorPos(Transform tf, Float2D pos)
        {
            if (ActiveEdit == null) return;

            var pt = tf.ScreenToWGS84(pos);

            lock(_lockMU)
            {
                switch (ActiveEdit.GeoType)
                {
                    case EnumGeoType.Point:
                        ActiveEdit.PointObj.Pivot = pt;
                        break;
                    case EnumGeoType.Line:
                        {
                            HitIndex = CheckHit(tf, pos, ActiveEdit.LineObj.Points);
                            if (HitIndex == -1) ActiveEdit.LineObj.Points.Add(pt);
                        }
                        break;
                    case EnumGeoType.Area:
                        {
                            HitIndex = CheckHit(tf, pos, ActiveEdit.AreaObj.Points);
                            if (HitIndex == -1) ActiveEdit.AreaObj.Points.Add(pt);
                        }
                        break;
                }
            }

            if (HitIndex == -1)  _cursorPos = pt;
            else _cursorPos = new Float2D(float.MaxValue, float.MaxValue);

            PendingDrawing();
        }

        public void SetHoverPos(Transform tf, Float2D pos)
        {
            if (ActiveEdit == null || ActiveEdit.GeoType == EnumGeoType.Point) return;

            var pt = tf.ScreenToWGS84(pos);

            lock (_lockMU)
            {
                switch (ActiveEdit.GeoType)
                {
                    case EnumGeoType.Line:
                        {
                            HitIndex = CheckHit(tf, pos, ActiveEdit.LineObj.Points);
                        }
                        break;
                    case EnumGeoType.Area:
                        {
                            HitIndex = CheckHit(tf, pos, ActiveEdit.AreaObj.Points);
                        }
                        break;
                }
            }
        }

        public void ClearCursorPos() => _cursorPos = new Float2D(float.MaxValue, float.MaxValue);

        public bool ChangeHitPos(Transform tf, Float2D pos)
        {
            if (ActiveEdit == null || HitIndex == -1 || ActiveEdit.GeoType == EnumGeoType.Point) return false;

            var pt = tf.ScreenToWGS84(pos);
            lock (_lockMU)
            {
                switch (ActiveEdit.GeoType)
                {
                    case EnumGeoType.Line:
                        ActiveEdit.LineObj.Points[HitIndex] = pt;
                        break;
                    case EnumGeoType.Area:
                        ActiveEdit.AreaObj.Points[HitIndex] = pt;
                        break;
                }
            }

            ClearCursorPos();

            return true;
        }

        public bool ReleaseHitPos(out int hitIndex, out Float2D point)
        {
            hitIndex = -1;
            point = new();
            if (ActiveEdit == null || HitIndex == -1 || ActiveEdit.GeoType == EnumGeoType.Point) return false;

            hitIndex = HitIndex;
            lock (_lockMU)
            {
                switch (ActiveEdit.GeoType)
                {
                    case EnumGeoType.Line:
                        point = ActiveEdit.LineObj.Points[HitIndex];
                        break;
                    case EnumGeoType.Area:
                        point = ActiveEdit.AreaObj.Points[HitIndex];
                        break;
                }
            }

            HitIndex = -1;
            return true;
        }


        private void DrawManualUpdateEdit(GraphicsContext context)
        {
            if (ActiveEdit == null) return;

            switch(ActiveEdit.GeoType)
            {
                case EnumGeoType.Point:
                    DrawMUeditPoint(context);
                    break;
                case EnumGeoType.Line:
                    DrawMUeditLine(context);
                    break;
                case EnumGeoType.Area:
                    DrawMUeditArea(context);
                    break;
            }

            DrawMUeditClickPoint(context);
        }

        private void DrawMUeditPoint(GraphicsContext context)
        {
            if (ActiveEdit == null) return;
            if (ActiveEdit.PointObj.Pivot.X == float.MaxValue || ActiveEdit.PointObj.Pivot.Y == float.MaxValue) return;

            context.SetStrokeColor(_editColor);
            context.SetStrokeWidth(1);
            context.SetFillColor(_editColor);

            lock (_lockMU)
            {
                var sxy = context.Transform.WGS84ToScreen(ActiveEdit.PointObj.Pivot);
                context.FillEllipse(3f, sxy);
                context.DrawEllipse(8f, sxy);
            }
        }

        private void DrawMUeditLine(GraphicsContext context)
        {
            if (ActiveEdit == null) return;

            context.SetStrokeColor(_editColor);
            context.SetStrokeWidth(1);
            context.SetFillColor(_editColor);

            lock (_lockMU)
            {
                float offset = 10f;
                int index = 1;
                foreach (var pos in ActiveEdit.LineObj.Points)
                {
                    var sxy = context.Transform.WGS84ToScreen(pos);
                    context.FillEllipse(3f, sxy);
                    context.DrawEllipse(8f, sxy);

                    var text = $"{index}";
                    context.DrawText(text, 15, sxy.X, sxy.Y + offset);
                    index++;
                }

                if(ActiveEdit.LineObj.Points.Count >= 2)
                {
                    float[] intervals = null;
                    intervals = new[] { 3.0f, 3.0f };
                    context.SetStrokeDash(intervals, 0.0f);
                    context.DrawPathWGS84(ActiveEdit.LineObj.Points);
                    context.SetStrokeDash(null);
                }

                DrawMUeditHitPoint(context, ActiveEdit.LineObj.Points);
            }
        }

        private void DrawMUeditArea(GraphicsContext context)
        {
            if (ActiveEdit == null) return;

            context.SetStrokeColor(_editColor);
            context.SetStrokeWidth(1);
            context.SetFillColor(_editColor);

            lock (_lockMU)
            {
                context.SetTextColor(_editColor);

                float offset = 20f;
                int index = 1;
                foreach (var pos in ActiveEdit.AreaObj.Points)
                {
                    var sxy = context.Transform.WGS84ToScreen(pos);
                    context.FillEllipse(3f, sxy);
                    context.DrawEllipse(8f, sxy);

                    var text = $"{index}";
                    context.DrawText(text, 20, sxy.X, sxy.Y + offset);
                    index++;
                }

                float[] intervals = null;
                intervals = new[] { 3.0f, 3.0f };
                context.SetStrokeDash(intervals, 0.0f);

                if (ActiveEdit.AreaObj.Points.Count >= 3)  context.DrawPathWGS84(ActiveEdit.AreaObj.Points, true);
                else context.DrawPathWGS84(ActiveEdit.AreaObj.Points);

                context.SetStrokeDash(null);

                DrawMUeditHitPoint(context, ActiveEdit.AreaObj.Points);
            }
        }

        private void DrawMUeditHitPoint(GraphicsContext context, List<Float2D> points)
        {
            if (ActiveEdit == null) return;
            if (HitIndex >= points.Count || HitIndex < 0) return;

            context.SetStrokeColor(_editColor);
            context.SetStrokeWidth(3);

            var sxy = context.Transform.WGS84ToScreen(points[HitIndex]);
            var offset = 20f;
            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y - offset), new Float2D(sxy.X - offset + 5f, sxy.Y - offset));
            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y - offset), new Float2D(sxy.X - offset, sxy.Y - offset + 5f));

            context.DrawLine(new Float2D(sxy.X + offset, sxy.Y - offset), new Float2D(sxy.X + offset - 5f, sxy.Y - offset));
            context.DrawLine(new Float2D(sxy.X + offset, sxy.Y - offset), new Float2D(sxy.X + offset, sxy.Y - offset + 5f));

            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y + offset), new Float2D(sxy.X - offset + 5f, sxy.Y + offset));
            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y + offset), new Float2D(sxy.X - offset, sxy.Y + offset - 5f));

            context.DrawLine(new Float2D(sxy.X + offset, sxy.Y + offset), new Float2D(sxy.X + offset - 5f, sxy.Y + offset));
            context.DrawLine(new Float2D(sxy.X + offset, sxy.Y + offset), new Float2D(sxy.X + offset, sxy.Y + offset - 5f));
        }

        private void DrawMUeditClickPoint(GraphicsContext context)
        {
            if (ActiveEdit == null) return;
            if (_cursorPos.X == float.MaxValue || _cursorPos.Y == float.MaxValue) return;
            if (HitIndex != -1) return;

            context.SetStrokeColor(_editColor);
            context.SetStrokeWidth(2);

            var sxy = context.Transform.WGS84ToScreen(_cursorPos);

            float offset = 10f;
            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y), new Float2D(sxy.X + offset, sxy.Y));
            context.DrawLine(new Float2D(sxy.X, sxy.Y - offset), new Float2D(sxy.X, sxy.Y + offset));

            offset = 15f;
            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y - offset), new Float2D(sxy.X - offset + 5f, sxy.Y - offset));
            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y - offset), new Float2D(sxy.X - offset , sxy.Y - offset + 5f));

            context.DrawLine(new Float2D(sxy.X + offset, sxy.Y - offset), new Float2D(sxy.X + offset - 5f, sxy.Y - offset));
            context.DrawLine(new Float2D(sxy.X + offset, sxy.Y - offset), new Float2D(sxy.X + offset, sxy.Y - offset + 5f));

            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y + offset), new Float2D(sxy.X - offset + 5f, sxy.Y + offset));
            context.DrawLine(new Float2D(sxy.X - offset, sxy.Y + offset), new Float2D(sxy.X - offset, sxy.Y + offset - 5f));

            context.DrawLine(new Float2D(sxy.X + offset, sxy.Y + offset), new Float2D(sxy.X + offset - 5f, sxy.Y + offset));
            context.DrawLine(new Float2D(sxy.X + offset, sxy.Y + offset), new Float2D(sxy.X + offset, sxy.Y + offset - 5f));
        }

        private int CheckHit(Transform tf, Float2D clickPoint, List<Float2D> points)
        {
            if (ActiveEdit == null) return -1;

            double ClickRadius = 10.0;

            var count = points.Count;
            for(int index=0; index<count; index++)
            {
                var sxy = tf.WGS84ToScreen(points[index]);
                double dist = Math.Sqrt(Math.Pow(clickPoint.X - sxy.X, 2) + Math.Pow(clickPoint.Y - sxy.Y, 2));
                if (dist <= ClickRadius) return index;
            }

            return -1;
        }

        public bool GetActivePositions(out List<Float2D> positions)
        {
            positions = new();
            if (ActiveEdit == null) return false;

            lock(_lockMU)
            {
                switch (ActiveEdit.GeoType)
                {
                    case EnumGeoType.Point:
                        positions.Add(ActiveEdit.PointObj.Pivot);
                        break;
                    case EnumGeoType.Line:
                        positions = ActiveEdit.LineObj.Points.ToList();
                        break;
                    case EnumGeoType.Area:
                        positions = ActiveEdit.AreaObj.Points.ToList();
                        break;
                }
            }

            return true;
        }

        public void SetModifyPosition(int index, double lat, double lon)
        {
            if (ActiveEdit == null) return;

            lock (_lockMU)
            {
                switch (ActiveEdit.GeoType)
                {
                    case EnumGeoType.Point:
                        {
                            _cursorPos = new Float2D(lon, lat);
                            ActiveEdit.PointObj.Pivot = _cursorPos;
                        }
                        break;
                    case EnumGeoType.Line:
                        {
                            if (index < ActiveEdit.LineObj.Points.Count && index >= 0)
                            {
                                ActiveEdit.LineObj.Points[index] = new Float2D(lon, lat);
                            }
                        }
                        break;
                    case EnumGeoType.Area:
                        if (index < ActiveEdit.AreaObj.Points.Count && index >= 0)
                        {
                            ActiveEdit.AreaObj.Points[index] = new Float2D(lon, lat);
                        }
                        break;
                }
            }

            PendingDrawing();
        }

        public void DeletePosition(int index)
        {
            if (ActiveEdit == null || ActiveEdit.GeoType == EnumGeoType.Point) return;

            lock (_lockMU)
            {
                switch (ActiveEdit.GeoType)
                {
                    case EnumGeoType.Line:
                        {
                            if (index < ActiveEdit.LineObj.Points.Count && index >= 0)
                            {
                                ActiveEdit.LineObj.Points.RemoveAt(index);
                            }
                        }
                        break;
                    case EnumGeoType.Area:
                        if (index < ActiveEdit.AreaObj.Points.Count && index >= 0)
                        {
                            ActiveEdit.AreaObj.Points.RemoveAt(index);
                        }
                        break;
                }
            }

            PendingDrawing();
        }
    }
}
