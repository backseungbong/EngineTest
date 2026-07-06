using JHLib.Util.Struct;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace JHLib.Util.SVG
{
//    internal enum PathOperation { Move, Line, Quad, Cubic, Arc, Close }
//    public class SVGComplexPath
//    {
//        private const float K_RATIO = 0.551784777779014f; // ideal ratio of cubic Bezier points for a quarter circle

//        private readonly List<float> _arcAngles;
//        private readonly List<bool> _arcClockwise;
//        private readonly List<Float2D> _points;
//        private readonly List<PathOperation> _operations;
//        private readonly List<bool> _subPathsClosed;
//        private int _subPathCount;

//        public SVGComplexPath()
//        {
//            _arcAngles = new List<float>();
//            _arcClockwise = new List<bool>();
//            _points = new List<Float2D>();
//            _operations = new List<PathOperation>();
//            _subPathsClosed = new List<bool>();
//            _subPathCount = 0;
//        }

//        public int SubPathCount => _subPathCount;
//        public bool Closed
//        {
//            get
//            {
//                if (_operations.Count > 0)
//                    return _operations[^1] == PathOperation.Close;
//                return false;
//            }
//        }

//        public List<Float2D> Points => _points;
//        public Float2D this[int index]
//        {
//            get
//            {
//                if (index < 0 || index >= _points.Count)
//                    return default;

//                return _points[index];
//            }
//        }

//        public void SetPoint(int index, float x, float y)
//        {
//            _points[index] = new Float2D(x, y);
//        }

//        public void SetPoint(int index, Float2D point)
//        {
//            _points[index] = point;
//        }

//        public int Count => _points.Count;
//        public int OperationCount => _operations.Count;

//        private float GetArcAngle(int aIndex)
//        {
//            if (_arcAngles.Count > aIndex)
//                return _arcAngles[aIndex];
//            return 0;
//        }

//        private bool GetArcClockwise(int aIndex)
//        {
//            if (_arcClockwise.Count > aIndex)
//                return _arcClockwise[aIndex];
//            return false;
//        }

//        public void Close()
//        {
//            if (!Closed)
//            {
//                _subPathsClosed.RemoveAt(_subPathCount - 1);
//                _subPathsClosed.Add(true);
//                _operations.Add(PathOperation.Close);
//            }
//        }

//        public SVGComplexPath MoveTo(float x, float y) => MoveTo(new Float2D(x, y));
//        public SVGComplexPath MoveTo(Float2D point)
//        {
//            _subPathCount++;
//            _subPathsClosed.Add(false);
//            _points.Add(point);
//            _operations.Add(PathOperation.Move);
//            return this;
//        }

//        public SVGComplexPath LineTo(float x, float y) => LineTo(new Float2D(x, y));
//        public SVGComplexPath LineTo(Float2D point)
//        {
//            if (_points.Count == 0)
//            {
//                _points.Add(point);
//                _subPathCount++;
//                _subPathsClosed.Add(false);
//                _operations.Add(PathOperation.Move);
//            }
//            else
//            {
//                _points.Add(point);
//                _operations.Add(PathOperation.Line);
//            }
//            return this;
//        }

//        public SVGComplexPath AddArc(float x1, float y1, float x2, float y2, float startAngle, float endAngle, bool clockwise) =>
//            AddArc(new Float2D(x1, y1), new Float2D(x2, y2), startAngle, endAngle, clockwise);
//        public SVGComplexPath AddArc(Float2D topLeft, Float2D bottomRight, float startAngle, float endAngle, bool clockwise)
//        {
//            if (_points.Count == 0 || _operations.Count == 0 || _operations[^1] == PathOperation.Close)
//            {
//                _subPathCount++;
//                _subPathsClosed.Add(false);
//            }

//            _points.Add(topLeft);
//            _points.Add(bottomRight);
//            _arcAngles.Add(startAngle);
//            _arcAngles.Add(endAngle);
//            _arcClockwise.Add(clockwise);
//            _operations.Add(PathOperation.Arc);
//            return this;
//        }

//        public SVGComplexPath QuadTo(float cx, float cy, float x, float y) =>
//            QuadTo(new Float2D(cx, cy), new Float2D(x, y));
//        public SVGComplexPath QuadTo(Float2D controlPoint, Float2D point)
//        {
//            _points.Add(controlPoint);
//            _points.Add(point);
//            _operations.Add(PathOperation.Quad);
//            return this;
//        }

//        public SVGComplexPath CurveTo(float c1X, float c1Y, float c2X, float c2Y, float x, float y) =>
//            CurveTo(new Float2D(c1X, c1Y), new Float2D(c2X, c2Y), new Float2D(x, y));
//        public SVGComplexPath CurveTo(Float2D controlPoint1, Float2D controlPoint2, Float2D point)
//        {
//            _points.Add(controlPoint1);
//            _points.Add(controlPoint2);
//            _points.Add(point);
//            _operations.Add(PathOperation.Cubic);
//            return this;
//        }

//        public void AppendEllipse(FloatRect rect) => AppendEllipse(rect.X1, rect.Y1, rect.DX, rect.DY);
//        public void AppendEllipse(float x, float y, float w, float h)
//        {
//            var minX = x;
//            var minY = y;
//            var maxX = minX + w;
//            var maxY = minY + h;
//            var midX = minX + w / 2;
//            var midY = minY + h / 2;
//            var offsetY = h / 2 * K_RATIO;
//            var offsetX = w / 2 * K_RATIO;

//            MoveTo(new Float2D(minX, midY));
//            CurveTo(new Float2D(minX, midY - offsetY), new Float2D(midX - offsetX, minY), new Float2D(midX, minY));
//            CurveTo(new Float2D(midX + offsetX, minY), new Float2D(maxX, midY - offsetY), new Float2D(maxX, midY));
//            CurveTo(new Float2D(maxX, midY + offsetY), new Float2D(midX + offsetX, maxY), new Float2D(midX, maxY));
//            CurveTo(new Float2D(midX - offsetX, maxY), new Float2D(minX, midY + offsetY), new Float2D(minX, midY));
//            Close();
//        }

//        public void AppendCircle(Float2D center, float r) => AppendCircle(center.X, center.Y, r);
//        public void AppendCircle(float cx, float cy, float r)
//        {
//            var minX = cx - r;
//            var minY = cy - r;
//            var maxX = cx + r;
//            var maxY = cy + r;
//            var midX = cx;
//            var midY = cy;
//            var offsetY = r * K_RATIO;
//            var offsetX = r * K_RATIO;

//            MoveTo(new Float2D(minX, midY));
//            CurveTo(new Float2D(minX, midY - offsetY), new Float2D(midX - offsetX, minY), new Float2D(midX, minY));
//            CurveTo(new Float2D(midX + offsetX, minY), new Float2D(maxX, midY - offsetY), new Float2D(maxX, midY));
//            CurveTo(new Float2D(maxX, midY + offsetY), new Float2D(midX + offsetX, maxY), new Float2D(midX, maxY));
//            CurveTo(new Float2D(midX - offsetX, maxY), new Float2D(minX, midY + offsetY), new Float2D(minX, midY));
//            Close();
//        }

//        public void AppendRectangle(FloatRect rect, bool includeLast = false) => AppendRectangle(rect.X1, rect.Y1, rect.DX, rect.DY, includeLast);
//        public void AppendRectangle(float x, float y, float w, float h, bool includeLast = false)
//        {
//            var minX = x;
//            var minY = y;
//            var maxX = minX + w;
//            var maxY = minY + h;

//            MoveTo(new Float2D(minX, minY));
//            LineTo(new Float2D(maxX, minY));
//            LineTo(new Float2D(maxX, maxY));
//            LineTo(new Float2D(minX, maxY));

//            if (includeLast)            
//                LineTo(new Float2D(minX, minY));            

//            Close();
//        }

//        public void AppendRoundedRectangle(FloatRect r, float cornerRadius, bool includeLast = false) =>
//            AppendRoundedRectangle(r.X1, r.Y1, r.DX, r.DY, cornerRadius, includeLast);
//        public void AppendRoundedRectangle(float x, float y, float w, float h, float cornerRadius, bool includeLast = false)
//        {
//            cornerRadius = ClampCornerRadius(cornerRadius, w, h);

//            var minX = x;
//            var minY = y;
//            var maxX = minX + w;
//            var maxY = minY + h;

//            var handleOffset = cornerRadius * K_RATIO;
//            var cornerOffset = cornerRadius - handleOffset;

//            MoveTo(new Float2D(minX, minY + cornerRadius));
//            CurveTo(new Float2D(minX, minY + cornerOffset), new Float2D(minX + cornerOffset, minY), new Float2D(minX + cornerRadius, minY));
//            LineTo(new Float2D(maxX - cornerRadius, minY));
//            CurveTo(new Float2D(maxX - cornerOffset, minY), new Float2D(maxX, minY + cornerOffset), new Float2D(maxX, minY + cornerRadius));
//            LineTo(new Float2D(maxX, maxY - cornerRadius));
//            CurveTo(new Float2D(maxX, maxY - cornerOffset), new Float2D(maxX - cornerOffset, maxY), new Float2D(maxX - cornerRadius, maxY));
//            LineTo(new Float2D(minX + cornerRadius, maxY));
//            CurveTo(new Float2D(minX + cornerOffset, maxY), new Float2D(minX, maxY - cornerOffset), new Float2D(minX, maxY - cornerRadius));

//            if (includeLast)            
//                LineTo(new Float2D(minX, minY + cornerRadius));            

//            Close();
//        }

//        public void AppendRoundedRectangle(FloatRect r, float topLeftCornerRadius, float topRightCornerRadius, float bottomLeftCornerRadius, float bottomRightCornerRadius, bool includeLast = false) =>
//            AppendRoundedRectangle(r.X1, r.Y1, r.DX, r.DY, topLeftCornerRadius, topRightCornerRadius, bottomLeftCornerRadius, bottomRightCornerRadius, includeLast);
//        public void AppendRoundedRectangle(FloatRect rect, float xCornerRadius, float yCornerRadius)
//        {
//            xCornerRadius = Math.Min(xCornerRadius, rect.DX / 2);
//            yCornerRadius = Math.Min(yCornerRadius, rect.DY / 2);

//            float minX = Math.Min(rect.X1, rect.X2);
//            float minY = Math.Min(rect.Y1, rect.Y2);
//            float maxX = Math.Max(rect.X1, rect.X2);
//            float maxY = Math.Max(rect.Y1, rect.Y2);

//            var xHandleOffset = xCornerRadius * K_RATIO;
//            var xCornerOffset = xCornerRadius - xHandleOffset;

//            var yHandleOffset = yCornerRadius * K_RATIO;
//            var yCornerOffset = yCornerRadius - yHandleOffset;

//            MoveTo(new Float2D(minX, minY + yCornerRadius));

//            CurveTo(
//                new Float2D(minX, minY + yCornerOffset),
//                new Float2D(minX + xCornerOffset, minY),
//                new Float2D(minX + xCornerRadius, minY));

//            LineTo(new Float2D(maxX - xCornerRadius, minY));

//            CurveTo(
//                new Float2D(maxX - xCornerOffset, minY),
//                new Float2D(maxX, minY + yCornerOffset),
//                new Float2D(maxX, minY + yCornerRadius));

//            LineTo(new Float2D(maxX, maxY - yCornerRadius));

//            CurveTo(
//                new Float2D(maxX, maxY - yCornerOffset),
//                new Float2D(maxX - xCornerOffset, maxY),
//                new Float2D(maxX - xCornerRadius, maxY));

//            LineTo(new Float2D(minX + xCornerRadius, maxY));

//            CurveTo(
//                new Float2D(minX + xCornerOffset, maxY),
//                new Float2D(minX, maxY - yCornerOffset),
//                new Float2D(minX, maxY - yCornerRadius));

//            LineTo(new Float2D(minX, minY + yCornerRadius));
//        }

//        public void AppendRoundedRectangle(float x, float y, float w, float h, float topLeftCornerRadius, float topRightCornerRadius, float bottomLeftCornerRadius, float bottomRightCornerRadius, bool includeLast = false)
//        {
//            topLeftCornerRadius = ClampCornerRadius(topLeftCornerRadius, w, h);
//            topRightCornerRadius = ClampCornerRadius(topRightCornerRadius, w, h);
//            bottomLeftCornerRadius = ClampCornerRadius(bottomLeftCornerRadius, w, h);
//            bottomRightCornerRadius = ClampCornerRadius(bottomRightCornerRadius, w, h);

//            var minX = x;
//            var minY = y;
//            var maxX = minX + w;
//            var maxY = minY + h;

//            var topLeftCornerOffset = topLeftCornerRadius - (topLeftCornerRadius * K_RATIO);
//            var topRightCornerOffset = topRightCornerRadius - (topRightCornerRadius * K_RATIO);
//            var bottomLeftCornerOffset = bottomLeftCornerRadius - (bottomLeftCornerRadius * K_RATIO);
//            var bottomRightCornerOffset = bottomRightCornerRadius - (bottomRightCornerRadius * K_RATIO);

//            MoveTo(new Float2D(minX, minY + topLeftCornerRadius));
//            CurveTo(new Float2D(minX, minY + topLeftCornerOffset), new Float2D(minX + topLeftCornerOffset, minY), new Float2D(minX + topLeftCornerRadius, minY));
//            LineTo(new Float2D(maxX - topRightCornerRadius, minY));
//            CurveTo(new Float2D(maxX - topRightCornerOffset, minY), new Float2D(maxX, minY + topRightCornerOffset), new Float2D(maxX, minY + topRightCornerRadius));
//            LineTo(new Float2D(maxX, maxY - bottomRightCornerRadius));
//            CurveTo(new Float2D(maxX, maxY - bottomRightCornerOffset), new Float2D(maxX - bottomRightCornerOffset, maxY), new Float2D(maxX - bottomRightCornerRadius, maxY));
//            LineTo(new Float2D(minX + bottomLeftCornerRadius, maxY));
//            CurveTo(new Float2D(minX + bottomLeftCornerOffset, maxY), new Float2D(minX, maxY - bottomLeftCornerOffset), new Float2D(minX, maxY - bottomLeftCornerRadius));

//            if (includeLast)
//                LineTo(new Float2D(minX, minY + topLeftCornerRadius));

//            Close();
//        }

//        private float ClampCornerRadius(float cornerRadius, float w, float h)
//        {
//            if (cornerRadius > h / 2)
//                cornerRadius = h / 2;
//            if (cornerRadius > w / 2)
//                cornerRadius = w / 2;
//            return cornerRadius;
//        }

//        public SVGComplexPath GetFlattenedPath(float flatness = .001f, bool includeSubPaths = false)
//        {
//            var flattenedPath = new SVGComplexPath();
//            List<Float2D> flattenedPoints = null;
//            List<Float2D> curvePoints = null;
//            bool foundClosed = false;
//            var pointIndex = 0;
//            int arcAngleIndex = 0;
//            int arcClockwiseIndex = 0;

//            for (var i = 0; i < _operations.Count && !foundClosed; i++)
//            {
//                var operation = _operations[i];
//                switch (operation)
//                {
//                    case PathOperation.Move:
//                        flattenedPath.MoveTo(_points[pointIndex++]);
//                        break;

//                    case PathOperation.Line:
//                        flattenedPath.LineTo(_points[pointIndex++]);
//                        break;

//                    case PathOperation.Quad:
//                        flattenedPoints ??= new List<Float2D>();
//                        flattenedPoints.Clear();
//                        curvePoints ??= new List<Float2D>();
//                        curvePoints.Clear();
//                        QuadToCubic(pointIndex, curvePoints);
//                        FlattenCubicSegment(0, flatness, curvePoints, flattenedPoints);
//                        foreach (var point in flattenedPoints)
//                            flattenedPath.LineTo(point);
//                        pointIndex += 2;
//                        break;

//                    case PathOperation.Cubic:
//                        flattenedPoints ??= new List<Float2D>();
//                        flattenedPoints.Clear();
//                        FlattenCubicSegment(pointIndex - 1, flatness, _points, flattenedPoints);
//                        foreach (var point in flattenedPoints)
//                            flattenedPath.LineTo(point);
//                        pointIndex += 3;
//                        break;

//                    case PathOperation.Arc:
//                        var topLeft = _points[pointIndex++];
//                        var bottomRight = _points[pointIndex++];
//                        float startAngle = GetArcAngle(arcAngleIndex++);
//                        float endAngle = GetArcAngle(arcAngleIndex++);
//                        var clockwise = GetArcClockwise(arcClockwiseIndex++);
//                        var flattenedArcPath = FlattenArc(topLeft, bottomRight, startAngle, endAngle, clockwise, flatness);
//                        foreach (var point in flattenedArcPath.Points)
//                            flattenedPath.LineTo(point);
//                        break;

//                    case PathOperation.Close:
//                        flattenedPath.Close();
//                        if (!includeSubPaths)
//                            foundClosed = true;
//                        break;
//                    default:
//                        throw new ArgumentOutOfRangeException();
//                }
//            }
//            return flattenedPath;
//        }

//        private SVGComplexPath FlattenArc(Float2D topLeft, Float2D bottomRight, float startAngle, float endAngle, bool clockwise, float flattness)
//        {
//            var arcFlattener = new ArcFlattener(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y, startAngle, endAngle, clockwise);
//            var flattenedPath = arcFlattener.CreateFlattenedPath(flattness);
//            return flattenedPath.GetFlattenedPath();
//        }

//        private void QuadToCubic(int pointIndex, List<Float2D> curvePoints)
//        {
//            var startPoint = _points[pointIndex - 1];
//            var quadControlPoint = _points[pointIndex];
//            var endPoint = _points[pointIndex + 1];

//            var controlPoint1 = new Float2D(startPoint.X + 2.0f * (quadControlPoint.X - startPoint.X) / 3.0f, startPoint.Y + 2.0f * (quadControlPoint.Y - startPoint.Y) / 3.0f);
//            var controlPoint2 = new Float2D(endPoint.X + 2.0f * (quadControlPoint.X - endPoint.X) / 3.0f, endPoint.Y + 2.0f * (quadControlPoint.Y - endPoint.Y) / 3.0f);

//            curvePoints.Add(startPoint);
//            curvePoints.Add(controlPoint1);
//            curvePoints.Add(controlPoint2);
//            curvePoints.Add(endPoint);
//        }

//        private void FlattenCubicSegment(int index, double flatness, List<Float2D> curvePoints, List<Float2D> flattenedPoints)
//        {
//            int i, k;
//            var numberOfPoints = 1;
//            var vectors = new Vector2[4];

//            double rCurve = 0;

//            for (i = index + 1; i <= index + 2; i++)
//            {
//                vectors[0] = (GetPointAsVector(curvePoints, i - 1) + GetPointAsVector(curvePoints, i + 1)) * 0.5f - GetPointAsVector(curvePoints, i);

//                double r = vectors[0].Length();

//                if (r > rCurve)
//                    rCurve = r;
//            }

//            if (rCurve <= 0.5 * flatness)
//            {
//                var vector = GetPointAsVector(curvePoints, index + 3);
//                flattenedPoints.Add(new Float2D(vector.X, vector.Y));
//                return;
//            }

//            numberOfPoints = (int)(Math.Sqrt(rCurve / flatness)) + 3;
//            if (numberOfPoints > 1000)
//                numberOfPoints = 1000;

//            var d = 1.0f / numberOfPoints;

//            vectors[0] = GetPointAsVector(curvePoints, index);
//            for (i = 1; i <= 3; i++)
//            {
//                vectors[i] = DeCasteljau(curvePoints, index, i * d);
//                flattenedPoints.Add(new Float2D(vectors[i].X, vectors[i].Y));
//            }

//            for (i = 1; i <= 3; i++)
//                for (k = 0; k <= (3 - i); k++)
//                    vectors[k] = vectors[k + 1] - vectors[k];

//            for (i = 4; i <= numberOfPoints; i++)
//            {
//                for (k = 1; k <= 3; k++)
//                    vectors[k] += vectors[k - 1];

//                flattenedPoints.Add(new Float2D(vectors[3].X, vectors[3].Y));
//            }
//        }

//        private Vector2 DeCasteljau(List<Float2D> curvePoints, int index, float t)
//        {
//            var s = 1.0f - t;

//            var vector0 = s * GetPointAsVector(curvePoints, index) + t * GetPointAsVector(curvePoints, index + 1);
//            var vector1 = s * GetPointAsVector(curvePoints, index + 1) + t * GetPointAsVector(curvePoints, index + 2);
//            var vector2 = s * GetPointAsVector(curvePoints, index + 2) + t * GetPointAsVector(curvePoints, index + 3);

//            vector0 = s * vector0 + t * vector1;
//            vector1 = s * vector1 + t * vector2;
//            return s * vector0 + t * vector1;
//        }

//        private Vector2 GetPointAsVector(List<Float2D> curvePoints, int index)
//        {
//            var point = curvePoints[index];
//            return new Vector2(point.X, point.Y);
//        }
//    }

//    internal class ArcFlattener
//    {
//        private float _cx;
//        private float _cy;
//        private float _diameter;
//        private float _radius;
//        private float _fx;
//        private float _fy;
//        private float _sweep;
//        private float _startAngle;
//        private Float2D _startPoint;

//        public ArcFlattener(
//            float x,
//            float y,
//            float width,
//            float height,
//            float startAngle,
//            float endAngle,
//            bool clockwise)
//        {
//            _startAngle = startAngle;

//            _cx = x + (width / 2);
//            _cy = y + (height / 2);

//            _diameter = Math.Max(width, height);
//            _radius = _diameter / 2;
//            _fx = width / _diameter;
//            _fy = height / _diameter;

//            _sweep = Math.Abs(endAngle - startAngle);
//            if (clockwise)
//                _sweep *= -1;

//            _startPoint = GetPointOnArc(0);
//        }

//        private Float2D GetPointOnArc(float percentage)
//        {
//            var angle = _startAngle + (_sweep * percentage);
//            while (angle >= 360)
//                angle -= 360;

//            angle *= -1;
//            var radians = MathF.PI * angle / 180;
//            var point = GetPointAtAngle(0, 0, _radius, radians);
//            point.X = _cx + (point.X * _fx);
//            point.Y = _cy + (point.Y * _fy);

//            return point;
//        }

//        private static Float2D GetPointAtAngle(float x, float y, float distance, float radians)
//        {
//            var (s, c) = MathF.SinCos(radians);
//            return new Float2D(x + c * distance, y + s * distance);
//        }

//        private static Float2D GetCenter(Float2D point1, Float2D point2)
//        {
//            var x = (point1.X + point2.X) * 0.5f;
//            var y = (point1.Y + point2.Y) * 0.5f;
//            return new Float2D(x, y);
//        }

//        public SVGComplexPath CreateFlattenedPath(float flatness = .5f)
//        {
//            var found = false;
//            var n = 1;

//            Float2D? endPoint = null;
//            while ((!found) && (n < 1024))
//            {
//                var candidate = 1f / n;
//                var midPointOnArc = GetPointOnArc(candidate / 2);

//                if (endPoint == null)
//                    endPoint = GetPointOnArc(candidate);

//                var midPointOnLine = GetCenter(_startPoint, (Float2D)endPoint);
//                var dx = midPointOnLine.X - midPointOnArc.X;
//                var dy = midPointOnLine.Y - midPointOnArc.Y;
//                if (MathF.Sqrt(dx * dx + dy * dy) <= flatness)
//                {
//                    found = true;
//                    n = n << 1;
//                }
//                else
//                {
//                    endPoint = midPointOnArc;
//                    n++;
//                }
//            }

//            var path = new SVGComplexPath();
//            path.MoveTo(_startPoint);

//            var step = 1f / n;
//            var percentage = 0f;

//            for (var i = 1; i < n; i++)
//            {
//                percentage += step;
//                path.LineTo(GetPointOnArc(percentage));
//            }

//            path.LineTo(GetPointOnArc(1));
//            return path;
//        }
//    }
//    public class PathBuilder
//    {
//        public static SVGComplexPath Build(string definition)
//        {
//            if (string.IsNullOrEmpty(definition))
//                return new SVGComplexPath();

//            var pathBuilder = new PathBuilder();
//            var path = pathBuilder.BuildPath(definition);
//            return path;
//        }

//        private readonly Stack<string> _commandStack = new Stack<string>();
//        private bool _closeWhenDone;
//        private char _lastCommand = '~';
//        private Float2D? _lastCurveControlPoint;
//        private Float2D? _lastMoveTo;

//        private SVGComplexPath _path;
//        private Float2D? _relativePoint;

//        private bool NextBoolValue
//        {
//            get
//            {
//                string vValueAsString = _commandStack.Pop();

//                if ("1".Equals(vValueAsString, StringComparison.Ordinal))
//                {
//                    return true;
//                }

//                return false;
//            }
//        }

//        private float NextValue
//        {
//            get
//            {
//                string vValueAsString = _commandStack.Pop();
//                try
//                {
//                    return ParseFloat(vValueAsString);
//                }
//                catch (Exception exc)
//                {
//                    throw new Exception("Error parsing a path value.", exc);
//                }
//            }
//        }

//        public static float ParseFloat(string value)
//        {
//            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
//            {
//                return number;
//            }

//            // Note: Illustrator will sometimes export numbers that look like "5.96.88", so we need to be able to handle them
//            var split = value.Split(new[] { '.' });
//            if (split.Length > 2)
//            {
//                if (float.TryParse($"{split[0]}.{split[1]}", NumberStyles.Any, CultureInfo.InvariantCulture, out number))
//                {
//                    return number;
//                }
//            }

//            string stringValue = GetNumbersOnly(value);
//            if (float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
//            {
//                return number;
//            }

//            throw new Exception($"Error parsing {value} as a float.");
//        }

//        private static string GetNumbersOnly(string value)
//        {
//            var builder = new StringBuilder(value.Length);
//            foreach (char c in value)
//            {
//                if (char.IsDigit(c) || c == '.' || c == '-')
//                {
//                    builder.Append(c);
//                }
//            }

//            return builder.ToString();
//        }

//        public SVGComplexPath BuildPath(string pathAsString)
//        {
//            try
//            {
//                _lastCommand = '~';
//                _lastCurveControlPoint = null;
//                _path = null;
//                _commandStack.Clear();
//                _relativePoint = new Float2D(0, 0);
//                _closeWhenDone = false;

//#if DEBUG_PATH
//				System.Diagnostics.Debug.WriteLine(aPathString);
//#endif
//#if NETSTANDARD2_0
//				pathAsString = pathAsString.Replace("Infinity", "0");
//#else
//                pathAsString = pathAsString.Replace("Infinity", "0", StringComparison.Ordinal);
//#endif
//                pathAsString = SeparateLetterCharsWithSpaces(pathAsString);
//#if NETSTANDARD2_0
//				pathAsString = pathAsString.Replace("-", " -");
//				pathAsString = pathAsString.Replace(" E  -", "E-");
//				pathAsString = pathAsString.Replace(" e  -", "e-");
//#else
//                pathAsString = pathAsString.Replace("-", " -", StringComparison.Ordinal);
//                pathAsString = pathAsString.Replace(" E  -", "E-", StringComparison.Ordinal);
//                pathAsString = pathAsString.Replace(" e  -", "e-", StringComparison.Ordinal);
//#endif
//#if DEBUG_PATH
//				System.Diagnostics.Debug.WriteLine(aPathString);
//#endif
//                string[] args = pathAsString.Split(new[] { ' ', '\r', '\n', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
//                for (int i = args.Length - 1; i >= 0; i--)
//                {
//                    string entry = args[i];
//                    char c = entry[0];
//                    if (char.IsLetter(c))
//                    {
//                        if (entry.Length > 1)
//                        {
//                            entry = entry.Substring(1);
//                            if (char.IsLetter(entry[0]))
//                            {
//                                if (entry.Length > 1)
//                                {
//                                    _commandStack.Push(entry.Substring(1));
//#if DEBUG_PATH
//										System.Diagnostics.Debug.WriteLine(vEntry.Substring(1));
//#endif
//                                }

//                                _commandStack.Push(entry[0].ToInvariantString());
//#if DEBUG_PATH
//								 System.Diagnostics.Debug.WriteLine(vEntry[0].ToString());
//#endif
//                            }
//                            else
//                            {
//                                _commandStack.Push(entry);
//#if DEBUG_PATH
//								System.Diagnostics.Debug.WriteLine(vEntry);
//#endif
//                            }
//                        }

//                        _commandStack.Push(c.ToInvariantString());
//#if DEBUG_PATH
//						System.Diagnostics.Debug.WriteLine(vChar.ToString());
//#endif
//                    }
//                    else
//                    {
//                        _commandStack.Push(entry);
//#if DEBUG_PATH
//						System.Diagnostics.Debug.WriteLine(vEntry);
//#endif
//                    }
//                }

//                while (_commandStack.Count > 0)
//                {
//                    if (_path == null)
//                    {
//                        _path = new SVGComplexPath();
//                    }

//                    string topCommand = _commandStack.Pop();
//                    var firstLetter = topCommand[0];

//                    if (IsCommand(firstLetter))
//                        HandleCommand(topCommand);
//                    else
//                    {
//                        _commandStack.Push(topCommand);
//                        HandleCommand(_lastCommand.ToString());
//                    }
//                }

//                if (_path != null && !_path.Closed)
//                {
//                    if (_closeWhenDone)
//                    {
//                        _path.Close();
//                    }
//                }
//            }
//            catch (Exception exc)
//            {
//                System.Diagnostics.Debug.WriteLine("=== An error occurred parsing the path. ===", exc);
//                System.Diagnostics.Debug.WriteLine(pathAsString);
//#if DEBUG
//                throw;
//#endif
//            }

//            static string SeparateLetterCharsWithSpaces(string input)
//            {
//                var sb = new StringBuilder(input.Length, maxCapacity: 3 * input.Length);
//                foreach (var character in input)
//                {
//                    if (char.IsLetter(character))
//                    {
//                        sb.Append(' ');
//                        sb.Append(character);
//                        sb.Append(' ');
//                    }
//                    else
//                    {
//                        sb.Append(character);
//                    }
//                }
//                return sb.ToString();
//            }

//            return _path;
//        }

//        private bool IsCommand(char firstLetter)
//        {
//            if (char.IsDigit(firstLetter))
//                return false;

//            if (firstLetter == '.')
//                return false;

//            if (firstLetter == '-')
//                return false;

//            if (firstLetter == 'e' || firstLetter == 'E')
//                return false;

//            return true;
//        }

//        private void HandleCommand(string command)
//        {
//            char c = command[0];

//            if (_lastCommand != '~' && (char.IsDigit(c) || c == '-'))
//            {
//                var previousCommand = _commandStack.Peek()?[0];

//                if (_lastCommand == 'M')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('L');
//                }
//                else if (_lastCommand == 'm')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('l');
//                }
//                else if (_lastCommand == 'L')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('L');
//                }
//                else if (_lastCommand == 'l')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('l');
//                }
//                else if (_lastCommand == 'H')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('H');
//                }
//                else if (_lastCommand == 'h')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('h');
//                }
//                else if (_lastCommand == 'V')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('V');
//                }
//                else if (_lastCommand == 'v')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('v');
//                }
//                else if (_lastCommand == 'C')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('C');
//                }
//                else if (_lastCommand == 'c')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('c');
//                }
//                else if (_lastCommand == 'S')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('S');
//                }
//                else if (_lastCommand == 's')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('s');
//                }
//                else if (_lastCommand == 'Q')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('Q');
//                }
//                else if (_lastCommand == 'q')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('q');
//                }
//                else if (_lastCommand == 'T')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('T', previousCommand);
//                }
//                else if (_lastCommand == 't')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('t', previousCommand);
//                }
//                else if (_lastCommand == 'A')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('A');
//                }
//                else if (_lastCommand == 'a')
//                {
//                    _commandStack.Push(command);
//                    HandleCommand('a');
//                }
//                else
//                {
//                    System.Diagnostics.Debug.WriteLine("Don't know how to handle the path command: " + command);
//                }
//            }
//            else
//            {
//                HandleCommand(c);
//            }
//        }

//        private void HandleCommand(char command, char? previousCommand = null)
//        {
//            if (command == 'M')
//            {
//                MoveTo(false);
//            }
//            else if (command == 'm')
//            {
//                MoveTo(true);
//                if (_lastCommand == '~')
//                {
//                    command = 'm';
//                }
//            }
//            else if (command == 'z' || command == 'Z')
//            {
//                ClosePath();
//            }
//            else if (command == 'L')
//            {
//                LineTo(false);
//            }
//            else if (command == 'l')
//            {
//                LineTo(true);
//            }
//            else if (command == 'Q')
//            {
//                QuadTo(false);
//            }
//            else if (command == 'q')
//            {
//                QuadTo(true);
//            }
//            else if (command == 'T')
//            {
//                ReflectiveQuadTo(false, previousCommand);
//            }
//            else if (command == 't')
//            {
//                ReflectiveQuadTo(true, previousCommand);
//            }
//            else if (command == 'C')
//            {
//                CurveTo(false);
//            }
//            else if (command == 'c')
//            {
//                CurveTo(true);
//            }
//            else if (command == 'S')
//            {
//                SmoothCurveTo(false);
//            }
//            else if (command == 's')
//            {
//                SmoothCurveTo(true);
//            }
//            else if (command == 'A')
//            {
//                ArcTo(false);
//            }
//            else if (command == 'a')
//            {
//                ArcTo(true);
//            }
//            else if (command == 'H')
//            {
//                HorizontalLineTo(false);
//            }
//            else if (command == 'h')
//            {
//                HorizontalLineTo(true);
//            }
//            else if (command == 'V')
//            {
//                VerticalLineTo(false);
//            }
//            else if (command == 'v')
//            {
//                VerticalLineTo(true);
//            }
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("Don't know how to handle the path command: " + command);
//            }

//            if (!(command == 'C' || command == 'c' || command == 's' || command == 'S'))
//            {
//                _lastCurveControlPoint = null;
//            }

//            _lastCommand = command;
//        }

//        private void ClosePath()
//        {
//            _path.Close();
//            _relativePoint = _lastMoveTo;
//        }

//        private void MoveTo(bool isRelative)
//        {
//            if (_path.SubPathCount == 1)
//            {
//                if (_path.FirstPoint.Equals(_path.LastPoint))
//                {
//                    _closeWhenDone = true;
//                }
//            }

//            var xOffset = NextValue;
//            var yOffset = NextValue;
//            var point = NewPoint(xOffset, yOffset, isRelative, true);
//            _path.MoveTo(point);
//            _lastMoveTo = point;
//        }

//        private void LineTo(bool isRelative)
//        {
//            var point = NewPoint(NextValue, NextValue, isRelative, true);
//            _path.LineTo(point);
//        }

//        private void HorizontalLineTo(bool isRelative)
//        {
//            var point = NewHorizontalPoint(NextValue, isRelative, true);
//            _path.LineTo(point);
//        }

//        private void VerticalLineTo(bool isRelative)
//        {
//            var point = NewVerticalPoint(NextValue, isRelative, true);
//            _path.LineTo(point);
//        }

//        private void CurveTo(bool isRelative)
//        {
//            var point1 = NewPoint(NextValue, NextValue, isRelative, false);
//            float x = NextValue;
//            float y = NextValue;

//            bool isQuad = char.IsLetter(_commandStack.Peek()[0]);
//            var point2 = NewPoint(x, y, isRelative, isQuad);

//            if (isQuad)
//            {
//                _path.QuadTo(point1, point2);
//                _lastCurveControlPoint = point1;
//            }
//            else
//            {
//                var point3 = NewPoint(NextValue, NextValue, isRelative, true);
//                _path.CurveTo(point1, point2, point3);
//                _lastCurveControlPoint = point2;
//                //System.Diagnostics.Debug.WriteLine($"CurveTo({point1.X},{point1.Y},{point2.X},{point2.Y},{point3.X},{point3.Y})");
//            }
//        }

//        private void QuadTo(bool isRelative)
//        {
//            var point1 = NewPoint(NextValue, NextValue, isRelative, false);
//            var point2 = NewPoint(NextValue, NextValue, isRelative, true);
//            _lastCurveControlPoint = point1;
//            _path.QuadTo(point1, point2);
//        }

//        private void ReflectiveQuadTo(bool isRelative, char? previousCommand)
//        {
//            var lastPoint = _path.LastPoint;
//            var point1 = lastPoint;
//            var lastCurveControlPoint = _lastCurveControlPoint ?? default;
//            switch (previousCommand)
//            {
//                case 'Q':
//                case 'q':
//                case 'T':
//                case 't':
//                    var dx = lastPoint.X - lastCurveControlPoint.X;
//                    var dy = lastPoint.Y - lastCurveControlPoint.Y;
//                    point1 = point1.Offset(dx, dy);
//                    break;
//            }
//            var point2 = NewPoint(NextValue, NextValue, isRelative, true);
//            _lastCurveControlPoint = point1;
//            _path.QuadTo(point1, point2);
//        }

//        private void SmoothCurveTo(bool isRelative)
//        {
//            Float2D? point1 = null;
//            var point2 = NewPoint(NextValue, NextValue, isRelative, false);

//            // ReSharper disable ConvertIfStatementToNullCoalescingExpression
//            if (_lastCurveControlPoint == null && _relativePoint != null)
//            {
//                // ReSharper restore ConvertIfStatementToNullCoalescingExpression
//                point1 = GeometryUtil.GetOppositePoint((Float2D)_relativePoint, point2);
//            }
//            else if (_relativePoint != null && _lastCurveControlPoint != null)
//            {
//                point1 = GeometryUtil.GetOppositePoint((Float2D)_relativePoint, (Float2D)_lastCurveControlPoint);
//            }

//            var point3 = NewPoint(NextValue, NextValue, isRelative, true);
//            if (point1 != null)
//                _path.CurveTo((Float2D)point1, point2, point3);
//            _lastCurveControlPoint = point2;
//        }

//        private void ArcTo(bool isRelative)
//        {
//            var startPoint = _relativePoint ?? default;

//            var rx = NextValue;
//            var ry = NextValue;

//            var r = NextValue;
//            var largeArcFlag = NextBoolValue;
//            var sweepFlag = NextBoolValue;
//            var endPoint = NewPoint(NextValue, NextValue, isRelative, false);

//            var arcPath = new SVGComplexPath(startPoint);
//            arcPath.SVGArcTo(rx, ry, r, largeArcFlag, sweepFlag, endPoint.X, endPoint.Y, startPoint.X, startPoint.Y);

//            for (int s = 0; s < arcPath.OperationCount; s++)
//            {
//                var segmentType = arcPath.GetSegmentType(s);
//                var pointsInSegment = arcPath.GetPointsForSegment(s);

//                if (segmentType == PathOperation.Move)
//                {
//                    // do nothing
//                }
//                else if (segmentType == PathOperation.Line)
//                {
//                    _path.LineTo(pointsInSegment[0]);
//                }
//                else if (segmentType == PathOperation.Cubic)
//                {
//                    _path.CurveTo(pointsInSegment[0], pointsInSegment[1], pointsInSegment[2]);
//                }
//                else if (segmentType == PathOperation.Quad)
//                {
//                    _path.QuadTo(pointsInSegment[0], pointsInSegment[1]);
//                }
//            }

//            _relativePoint = _path.LastPoint;
//        }

//        private Float2D NewPoint(float x, float y, bool isRelative, bool isReference)
//        {
//            Float2D point = default;

//            if (isRelative && _relativePoint != null)
//            {
//                point = new Float2D(((Float2D)_relativePoint).X + x, ((Float2D)_relativePoint).Y + y);
//            }
//            else
//            {
//                point = new Float2D(x, y);
//            }

//            // If this is the reference point, we want to store the location before
//            // we translate it into the final coordinates.  This way, future relative
//            // points will start from an un-translated position.
//            if (isReference)
//            {
//                _relativePoint = point;
//            }

//            return point;
//        }

//        private Float2D NewVerticalPoint(float y, bool isRelative, bool isReference)
//        {
//            Float2D point = default;

//            if (isRelative && _relativePoint != null)
//            {
//                point = new Float2D(((Float2D)_relativePoint).X, ((Float2D)_relativePoint).Y + y);
//            }
//            else if (_relativePoint != null)
//            {
//                point = new Float2D(((Float2D)_relativePoint).X, y);
//            }

//            // If this is the reference point, we want to store the location before
//            // we translate it into the final coordinates.  This way, future relative
//            // points will start from an un-translated position.
//            if (isReference)
//            {
//                _relativePoint = point;
//            }

//            return point;
//        }

//        private Float2D NewHorizontalPoint(float x, bool isRelative, bool isReference)
//        {
//            Float2D point = default;

//            if (isRelative && _relativePoint != null)
//            {
//                point = new Float2D(((Float2D)_relativePoint).X + x, ((Float2D)_relativePoint).Y);
//            }
//            else if (_relativePoint != null)
//            {
//                point = new Float2D(x, ((Float2D)_relativePoint).Y);
//            }

//            // If this is the reference point, we want to store the location before
//            // we translate it into the final coordinates.  This way, future relative
//            // points will start from an un-translated position.
//            if (isReference)
//            {
//                _relativePoint = point;
//            }

//            return point;
//        }
//    }
}