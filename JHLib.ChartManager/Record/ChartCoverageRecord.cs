using JHLib.Util.Projection;
using JHLib.Util.Struct;

namespace JHLib.ChartManager.Record
{
    public class ChartCoverageRecord
    {
        public string name { get; set; }

        public int? usage { get; set; } = null;
        public int? scale { get; set; } = null;

        public ChartRecord.BaseVersion? baseVersion { get; set; } = null;
        public int? updateVersion { get; set; } = null;
        public string? issueDate { get; set; } = null;

        public ChartRecord.Position[]? coveragePath { get; set; } = null;
        public Float2D[] Cov { get; set; } = null;

        public bool IsChart1 = false;

        public ChartCoverageRecord(string name)
        {
            this.name = name;
        }

        private const float ScaleFactor = 10000000.0f;
        public void CoverageConverter()
        {
            Cov = new Float2D[coveragePath.Length];
            Cov[0] = new Float2D(coveragePath[0].x / ScaleFactor, coveragePath[0].y / ScaleFactor);
            Cov[1] = new Float2D(coveragePath[1].x / ScaleFactor, coveragePath[1].y / ScaleFactor);
            Cov[2] = new Float2D(coveragePath[2].x / ScaleFactor, coveragePath[2].y / ScaleFactor);
            Cov[3] = new Float2D(coveragePath[3].x / ScaleFactor, coveragePath[3].y / ScaleFactor);
            CalcBound();
        }

        // Bound 영역 저장
        public float minX, maxX, minY, maxY;

        public void CalcBound()
        {
            float x1, x2, y1, y2;
            if (Cov[0].X < Cov[2].X)
            {
                x1 = Cov[0].X;
                x2 = Cov[2].X;
            }
            else
            {
                x2 = Cov[0].X;
                x1 = Cov[2].X;
            }
            if (Cov[0].Y < Cov[2].Y)
            {
                y1 = Cov[0].Y;
                y2 = Cov[2].Y;
            }
            else
            {
                y2 = Cov[0].Y;
                y1 = Cov[2].Y;
            }
            var xy1 = EPSG3857.ToWorld(x1, y1);
            var xy2 = EPSG3857.ToWorld(x2, y2);

            // Bounding Box의 경계값 확정 (Min/Max 정규화)
            minX = Math.Min(xy1.X, xy2.X);
            maxX = Math.Max(xy1.X, xy2.X);
            minY = Math.Min(xy1.Y, xy2.Y);
            maxY = Math.Max(xy1.Y, xy2.Y);
        }

        public bool IsWorldBoundInPosition(Float2D posWorld)
        {
            if (posWorld.X < minX || posWorld.X > maxX || posWorld.Y < minY || posWorld.Y > maxY) return false;

            return true;
        }

        public bool CheckHideDisplay(double Scale)
        {
            // Compilation Scale의 0.5(min) ~ 2(max)배안의 차트만 표시한다.
            var max = scale * 2;
            return Scale > max ? true : false;
        }
    }
}