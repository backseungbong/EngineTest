using JHLib.Graphics;
using JHLib.Graphics.SkiaExtention;
using JHLib.Util.Geometry;
using JHLib.Util.Geometry.Clipper2;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class ChartSKPath
    {
        // 중심점을 계산해야 하는지 저장할 변수 
        public bool IsCalcCenterPoint = false;

        // 기본 SKPath
        public SKPath MainSkPath = null;
        // SKPath Group을 만들기 위한 를 저장할 어레이
        public List<SKPath> ListSkPath = new();

        // 영역/라인의 중심 위치
        public Float2D Pivot = new Float2D(float.MinValue, float.MinValue);
        // 화면과 영역이 교차하는 영역
        public Float2D[][] PathsIntersect = null;
        // Line영역을 구성할 어레이
        public Float2D[] PathLine = null;

        public void Dispose()
        {
            if (ListSkPath != null)
            {
                foreach (var path in ListSkPath) path?.Dispose();
                ListSkPath.Clear();
            }

            MainSkPath?.Dispose();
            MainSkPath = null;

            PathLine = null;
            PathsIntersect = null;
        }

        public Float2D[] WorldToScreen(Transform projection, Float2D[] pathWorld)
        {
            if (pathWorld.Length >= 2)
            {
                var dedupedPath = projection.WorldToScreenDedupe(pathWorld);
                if (dedupedPath.Length >= 2)
                    return dedupedPath;
            }
            return null;


            // 기존 코드
            //const float TOLERANCE = 2f;
            //var pathScreen = new Float2D[pathWorld.Length];

            //int index = 0;
            //var prevPoint = projection.WorldToScreen(pathWorld[index]);
            //pathScreen[index++] = prevPoint;
            //for (var i = 1; i < pathWorld.Length; i++)
            //{
            //    var nextPoint = projection.WorldToScreen(pathWorld[i]);
            //    if ((int)(prevPoint.X * TOLERANCE) != (int)(nextPoint.X * TOLERANCE) ||
            //        (int)(prevPoint.Y * TOLERANCE) != (int)(nextPoint.Y * TOLERANCE))
            //    {
            //        pathScreen[index++] = nextPoint;
            //        prevPoint = nextPoint;
            //    }
            //}

            //unsafe
            //{
            //    if (index < 2) return null;

            //    var result = new Float2D[index];
            //    fixed (Float2D* pDest = &result[0])
            //    fixed (Float2D* pSource = &pathScreen[0])
            //        Buffer.MemoryCopy(pSource, pDest, 8 * index, 8 * index);
            //    return result;
            //}
        }

        public Float2D[] WorldToScreen(Transform projection, Float2D[] pathWorld, int nCount, int offset, bool bLine = false)
        {
            if (offset >= 0 && nCount >= 2 && offset + nCount <= pathWorld.Length)
            {
                var span = pathWorld.AsSpan(offset, nCount);
                var dedupedPath = projection.WorldToScreenDedupe(span);
                if (dedupedPath.Length >= 2)
                {
                    // Line인 경우 영역을 구해옮
                    if (bLine == true)                    
                        PathLine = dedupedPath;
                    return dedupedPath;
                }
            }
            return null;


            // 기존 코드
            //const float TOLERANCE = 2f;

            //var offsetAdd = offset;
            //var pathScreen = new Float2D[pathWorld.Length];

            //var prevPoint = projection.WorldToScreen(pathWorld[offset]);
            //pathScreen[offsetAdd++] = prevPoint;
            //for (var i = offset + 1; i < nCount + offset; i++)
            //{
            //    var nextPoint = projection.WorldToScreen(pathWorld[i]);
            //    if ((int)(prevPoint.X * TOLERANCE) != (int)(nextPoint.X * TOLERANCE) ||
            //        (int)(prevPoint.Y * TOLERANCE) != (int)(nextPoint.Y * TOLERANCE))
            //    {
            //        pathScreen[offsetAdd++] = nextPoint;
            //        prevPoint = nextPoint;
            //    }
            //}

            //unsafe
            //{
            //    var resultCount = offsetAdd - offset;
            //    if (resultCount < 2)
            //        return null;

            //    var result = new Float2D[resultCount];
            //    fixed (Float2D* pDest = &result[0])
            //    fixed (Float2D* pSource = &pathScreen[offset])
            //        Buffer.MemoryCopy(pSource, pDest, 8 * resultCount, 8 * resultCount);

            //    // Line인 경우 영역을 구해옮
            //    if (bLine == true)
            //    {
            //        PathLine = result;
            //    }

            //    return result;
            //}
        }

        // SKPath를 만드는 함수
        public bool CreateSKPath(Transform projection, Float2D[] pathWorld, int count, int offset, bool closed = true)
        {
            var data = WorldToScreen(projection, pathWorld, count, offset, true);
            if (data == null) return false;

            var path = new SKPath();
            path.FillType = SKPathFillType.EvenOdd;
            path.AddPoly(data, closed);

            if (MainSkPath == null)
                MainSkPath = path;

            // Pivot을 계산해 놓는다.
            PathLine = data;
            Pivot = GeometryHelper.PathLerp(PathLine, 0.5);

            return true;
        }

        // Circle SKPath 만드는 함수
        public bool CreateCircleSKPath(Float2D centerPos, float fRadius)
        {
            var path = new SKPath();
            path.FillType = SKPathFillType.EvenOdd;
            path.AddCircle(centerPos.X, centerPos.Y, fRadius);

            if (MainSkPath == null) MainSkPath = path;

            return true;
        }

        // SKPath Group를 만드는 함수
        public void CreateSKPathGroup(Transform projection, Float2D[] pathWorld, bool bEnd = false)
        {
            var data = WorldToScreen(projection, pathWorld);
            if (data == null)
            {
                if (bEnd && ListSkPath.Count > 0)
                {
                    var pathGroup = new SKPath();
                    pathGroup.FillType = SKPathFillType.EvenOdd;
                    foreach (var sk in ListSkPath) pathGroup.AddPath(sk);

                    MainSkPath = pathGroup;
                }
                return;
            }

            var path = new SKPath();
            path.FillType = SKPathFillType.EvenOdd;
            path.AddPoly(data);

            ListSkPath.Add(path);

            if (bEnd == true && MainSkPath == null && ListSkPath.Count != 0)
            {
                var pathGroup = new SKPath();
                pathGroup.FillType = SKPathFillType.EvenOdd;
                foreach (var sk in ListSkPath) pathGroup.AddPath(sk);

                MainSkPath = pathGroup;
            }
        }

        // SKPath Group를 만드는 함수
        public bool CreateSKPathGroup(Transform projection, int index, Float2D[] pathWorld, int nCount, int offset)
        {
            var data = WorldToScreen(projection, pathWorld, nCount, offset);
            if (data == null) return false;

            var path = new SKPath();
            path.FillType = SKPathFillType.EvenOdd;
            path.AddPoly(data);

            ListSkPath.Add(path);

            // 영역의 중심점을 구해야 하는 상황이면
            if (IsCalcCenterPoint == true && index == 0)
            {
                // 가장 바깥의 영역만 가지고 계산한다.
                // 화면영역과 Geometry의 영역이 교차하는 영역을 구한다.
                var pathView = projection.ScreenBound.ToPath();
                var pathObj = data;

                if (pathView != null && pathObj != null)
                {
                    Clipper2 clip = new();
                    clip.AddSubject(pathView);
                    clip.AddClip(pathObj);
                    var pathsINT = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINT) == true)
                    {
                        PathsIntersect = new Float2D[pathsINT.Count][];
                        for (int i = 0; i < pathsINT.Count; i++)
                        {
                            var temp = pathsINT[i] as Float2D[];
                            PathsIntersect[i] = new Float2D[temp.Length];
                            for (int k = 0; k < temp.Length; k++)
                            {
                                PathsIntersect[i][k].X = temp[k].X;
                                PathsIntersect[i][k].Y = temp[k].Y;
                            }
                        }

                        // AI의 도움을 받아서 중심점 구하는 함수를 변경함
                        //Pivot = GeometryHelper.GetCentroid(PathsIntersect[0]);
                        Pivot = GetBalancedInternalCenter(PathsIntersect[0]);
                    }
                    clip.Clear();
                }
            }

            return true;
        }

        public static Float2D GetBalancedInternalCenter(ReadOnlySpan<Float2D> path)
        {
            if (path.Length < 3) return path.Length > 0 ? path[0] : new Float2D(0, 0);

            // 1. 경계 박스 및 기본 정보 계산
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < path.Length; i++)
            {
                minX = Math.Min(minX, path[i].X); maxX = Math.Max(maxX, path[i].X);
                minY = Math.Min(minY, path[i].Y); maxY = Math.Max(maxY, path[i].Y);
            }

            Float2D bboxCenter = new Float2D((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            float maxScore = float.MinValue;
            Float2D bestPoint = path[0];

            // 2. 더 정밀한 그리드 스캔 (20x20 정도로 정밀도 향상)
            const int gridCount = 20;
            float stepX = (maxX - minX) / gridCount;
            float stepY = (maxY - minY) / gridCount;

            for (int ix = 0; ix <= gridCount; ix++)
            {
                for (int iy = 0; iy <= gridCount; iy++)
                {
                    Float2D p = new Float2D(minX + ix * stepX, minY + iy * stepY);

                    // 점이 내부에 있는지 확인
                    if (IsPointInPolygon(path, p))
                    {
                        float distToEdge = GetMinDistToEdge(path, p);

                        // [핵심] 가중치 계산 (Score 방식)
                        // 1. 테두리에서 멀수록 좋음 (distToEdge)
                        // 2. 전체 박스의 가로 중앙선에 가까울수록 가점 (Balance)
                        float distToCenterLine = Math.Abs(p.X - bboxCenter.X);
                        float score = distToEdge - (distToCenterLine * 0.2f); // 0.2는 밸런스 조정 강도

                        if (score > maxScore)
                        {
                            maxScore = score;
                            bestPoint = p;
                        }
                    }
                }
            }

            return bestPoint;
        }

        // 내부 판정 및 거리 계산 보조 함수 (이전과 동일)
        private static bool IsPointInPolygon(ReadOnlySpan<Float2D> poly, Float2D p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
                    (p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X))
                    inside = !inside;
            }
            return inside;
        }

        private static float GetMinDistToEdge(ReadOnlySpan<Float2D> poly, Float2D p)
        {
            float minDistSq = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                var v1 = poly[j]; var v2 = poly[i];
                float dx = v2.X - v1.X, dy = v2.Y - v1.Y;
                float t = Math.Clamp(((p.X - v1.X) * dx + (p.Y - v1.Y) * dy) / (dx * dx + dy * dy + 1e-10f), 0, 1);
                float dSq = (p.X - (v1.X + t * dx)) * (p.X - (v1.X + t * dx)) + (p.Y - (v1.Y + t * dy)) * (p.Y - (v1.Y + t * dy));
                if (dSq < minDistSq) minDistSq = dSq;
            }
            return (float)Math.Sqrt(minDistSq);
        }


        public void MakeSKPathGroup()
        {
            if (MainSkPath == null && ListSkPath.Count > 0)
            {
                var pathGroup = new SKPath();
                pathGroup.FillType = SKPathFillType.EvenOdd;
                foreach (var sk in ListSkPath) pathGroup.AddPath(sk);

                MainSkPath = pathGroup;
            }
        }

        // SKPath 영역을 그리는 함수 
        public void DrawSKPathArea(GraphicsContext context, SKColor color, bool bSelect = false)
        {
            if (MainSkPath == null) return;

            context.SetFillColor(color);
            context.FillPath(MainSkPath);

            if (bSelect == true)
            {
                context.SetFillColor(new SKColor(255, 0, 0, 76));
                context.FillPath(MainSkPath);
            }
        }

        // Geometry Line을 그리는 함수 
        public void DrawSKPathLine(GraphicsContext context, SKColor color, float[] intervals, float phase, float width, bool bSelect = false)
        {
            if (MainSkPath == null) return;

            context.SetStrokeWidth(width);
            if (bSelect == true)
            {
                context.SetStrokeColor(new SKColor(255, 0, 0, 76));
            }
            else
            {
                context.SetStrokeColor(color);
                context.SetStrokeDash(intervals, phase);
            }

            context.DrawPath(MainSkPath);
            context.SetStrokeDash(null);
        }

        // 한 점이 SKPath Group에 포함되어 있는지 확인하는 함수 
        public bool IsContainSKPathGroup(Float2D point)
        {
            if (MainSkPath == null) return false;

            return MainSkPath.Contains(point.X, point.Y);
        }

        // 한 점이 SKPath Line에 포함되어 있는지 확인하는 함수 
        public bool IsContainSKpathLine(Float2D point)
        {
            if (PathLine == null) return false;

            return GeometryHelper.PathIntersect(PathLine, point.X - 10, point.Y - 10, point.X + 10, point.Y + 10);
        }

    }
}
