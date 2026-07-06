using JHLib.Graphics;
using JHLib.S57.Chart;
using JHLib.Util.Geometry;
using JHLib.Util.Geometry.Clipper2;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Weather;
using SkiaSharp;

namespace JHLib.S57.ChartObject
{
    public class ObjBase
    {
        public ObjBase(S57ChartRenderer chartRenderer)
        {
            ChartRenderer = chartRenderer;
        }

        public const float ScaleFactor = 10000000.0f;

        // Main정보를 저장할 인스턴스 
        public S57ChartRenderer ChartRenderer = null;

        // 현재 그려질 차트의 Usage를 가지고 있을 변수 
        public byte Usage = 255;

        public ST_POINTS Points;

        // 월드 좌표로 저장할 어레이
        public Float2D[] PathWorld = null;

        // Geometry를 구성할 World영역의 사각 영역을 구성한 어레이[화면영역과 비교하여 만들지 말지를 결정]
        public FloatRect[] WorldRect = null;

        // Area SKPath 정보를 저장할 인스턴스
        public ChartSKPath AreaSKPath = new();

        // Query시에 선택 여부
        public bool IsQuerySelect = false;

        // Query정보를 Reset하는 함수 
        public void ResetQuery()
        {
            IsQuerySelect = false;
        }

        public void DrawSelectQueryPoint(GraphicsContext context, Float2D pivot)
        {
            if (IsQuerySelect == false) return;
            if (pivot.X == float.MaxValue || pivot.Y == float.MaxValue) return;

            var rgb = WeatherColor.GetColor("CURSR");
            context.SetStrokeColor(new SKColor(rgb.R, rgb.G, rgb.B));
            context.SetStrokeWidth(2);

            var offset = 20f;
            var space = 8f;
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y - offset), new Float2D(pivot.X - offset + space, pivot.Y - offset));
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y - offset), new Float2D(pivot.X - offset, pivot.Y - offset + space));

            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y - offset), new Float2D(pivot.X + offset - space, pivot.Y - offset));
            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y - offset), new Float2D(pivot.X + offset, pivot.Y - offset + space));

            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y + offset), new Float2D(pivot.X - offset + space, pivot.Y + offset));
            context.DrawLine(new Float2D(pivot.X - offset, pivot.Y + offset), new Float2D(pivot.X - offset, pivot.Y + offset - space));

            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y + offset), new Float2D(pivot.X + offset - space, pivot.Y + offset));
            context.DrawLine(new Float2D(pivot.X + offset, pivot.Y + offset), new Float2D(pivot.X + offset, pivot.Y + offset - space));
        }

        // Object를 그릴 조건이 되는지 확인하는 함수
        public bool IsDraw(byte updateType)
        {
            bool drawFlag = true;

            // Delete를 2->12로 만든 이유는 업데이트를 .001, .002 순차적으로 진행할 때 .001에서 삭제한 Object도 2 이고, .002에서 삭제한 Object도 2라서 
            // .001에서 삭제된 Object는 아예 Update Review를 하더라도 그리지 않게 해야 해서 마지막 Update의 경우에만 11,12,13처럼 10을 붙여서 보내주기로 함
            // Delete일때는 Review가 켜져있을 때만 그린다.
            if (updateType == 12) drawFlag = S57ChartOption.UpdateReview;
            else if (updateType == 2) drawFlag = false;

            return drawFlag;
        }


        // Shape SKPath를 만드는 함수 
        public void CreateSKPath_Shape(Transform projection, bool calcCenterPoint = false)
        {
            AreaSKPath.IsCalcCenterPoint = calcCenterPoint;

            int nShape = Points.Shape.ShapeArr.Length;
            int offset = 0;
            for (int i = 0; i < nShape; i++)
            {
                int nCount = Points.Shape.ShapeArr[i].PointCount;
                if (nCount > 0)
                {
                    if (projection.RectIntersectWorld(WorldRect[i]) == true)
                    {
                        AreaSKPath.CreateSKPathGroup(projection, i, PathWorld, nCount, offset);
                    }

                    offset += nCount;
                }
            }

            AreaSKPath.MakeSKPathGroup();
        }

        // Edeg SKPath를 만드는 함수
        public ChartSKPath CreateSKPath_EdgeGeo(Transform projection, int index)
        {
            int nStart = Points.Shape.EdgeArr[index].Start;
            int nCount = Points.Shape.EdgeArr[index].Count;

            var skPath = new ChartSKPath();
            if (skPath.CreateSKPath(projection, PathWorld, nCount, nStart, false) == true) return skPath;

            return null;
        }

        // Com Start/End를 검색할 함수 (btType : 0 = AC, 1 = AP, 2 = LS, 3 = LC, 4 = SY, 5 = TX)
        public void CheckComStartEnd(byte btComIndex, int nSize, byte btType, ST_COM_INFO[] arrComInfo, out int nComStart, out int nComEnd)
        {
            nComStart = 0;
            nComEnd = nSize;

            if (btComIndex == 0)
            {
                if (btType == 0) nComEnd = arrComInfo[0].AC;
                else if (btType == 1) nComEnd = arrComInfo[0].AP;
                else if (btType == 2) nComEnd = arrComInfo[0].LS;
                else if (btType == 3) nComEnd = arrComInfo[0].LC;
                else if (btType == 4) nComEnd = arrComInfo[0].SY;
                else if (btType == 5) nComEnd = arrComInfo[0].TX;
            }
            else
            {
                if (btComIndex >= arrComInfo.Length) nComStart = arrComInfo.Length;
                else
                {
                    int nComInfo = -1;
                    if (btType == 0) nComInfo = arrComInfo[btComIndex].AC;
                    else if (btType == 1) nComInfo = arrComInfo[btComIndex].AP;
                    else if (btType == 2) nComInfo = arrComInfo[btComIndex].LS;
                    else if (btType == 3) nComInfo = arrComInfo[btComIndex].LC;
                    else if (btType == 4) nComInfo = arrComInfo[btComIndex].SY;
                    else if (btType == 5) nComInfo = arrComInfo[btComIndex].TX;
                    nComStart = nSize - nComInfo;
                }
            }
        }

        public bool CheckOwnshipOverlabCenterSymbol(Float2D[] pathOwnship, int nSymbolIndex, Float2D pivot)
        {
            if (nSymbolIndex < 0) return false;
            if (pathOwnship == null) return false;

            float fW, fH, pX, pY;
            fW = fH = pX = pY = 0.0f;
            if (ChartRenderer.GetSymbolBoundAndPivot(nSymbolIndex, ref fW, ref fH, ref pX, ref pY) == true)
            {
                var harfW = (fW / 2.0f) * ChartRenderer.ScaleFactor;
                var harfH = (fH / 2.0f) * ChartRenderer.ScaleFactor;

                Float2D temp = new Float2D(0, 0);
                temp = pivot;
                if (pX < 0.0f)
                {
                    temp.X = (-pX + (fW / 2.0f)) * ChartRenderer.ScaleFactor;
                    temp.X += pivot.X;
                }

                if (pY < 0.0f)
                {
                    temp.Y = (-pY + (fH / 2.0f)) * ChartRenderer.ScaleFactor;
                    temp.Y += pivot.Y;
                }

                Float2D[] pathSymbol = new Float2D[4];
                pathSymbol[0] = new Float2D(temp.X - harfW, temp.Y - harfH);
                pathSymbol[1] = new Float2D(temp.X + harfW, temp.Y - harfH);
                pathSymbol[2] = new Float2D(temp.X + harfW, temp.Y + harfH);
                pathSymbol[3] = new Float2D(temp.X - harfW, temp.Y + harfH);

                Clipper2 clip = new();
                clip.AddSubject(pathOwnship);
                clip.AddClip(pathSymbol);
                return clip.Execute(0, 0);
            }

            return false;
        }

        // 교차 영역안에 Center심볼이 들어가는지 확인하는 함수
        public bool CheckAreaInCenterSymbol(Float2D[][] pathsIn, int nSymbolIndex, Float2D pivot)
        {
            if (nSymbolIndex < 0) return false;
            if (pathsIn == null) return false;

            // S-52에 Center Symbol은 화면과 교차되는 영역안에 심볼의 Bounding의 중심이 포함하지 않으면
            // 그리지 않도록 규정하고 있음
            bool bRtn = false;
            float fW, fH, pX, pY;
            fW = fH = pX = pY = 0.0f;
            if(ChartRenderer.GetSymbolBoundAndPivot(nSymbolIndex, ref fW, ref fH, ref pX, ref pY) == true)
            {
                Float2D temp = new Float2D(0,0);
                temp = pivot;
                if (pX < 0.0f)
                {
                    temp.X = (-pX + (fW / 2.0f)) * ChartRenderer.ScaleFactor;
                    temp.X += pivot.X;
                }

                if (pY < 0.0f)
                {
                    temp.Y = (-pY + (fH / 2.0f)) * ChartRenderer.ScaleFactor;
                    temp.Y += pivot.Y;
                }

                bRtn = GeometryHelper.PointInPolygonWinding(temp, pathsIn[0]);
            }

            return bRtn;
        }

        public virtual void Dispose()
        {
            AreaSKPath.Dispose();
        }
    }
}
