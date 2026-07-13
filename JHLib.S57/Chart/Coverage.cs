using JHLib.Util.ByteControl;
using JHLib.Util.Projection;
using JHLib.Util.Struct;
using System.IO;
using JHLib.Util.Projection.ScreenTransform;

namespace JHLib.S57.Chart
{
    public class Coverage
    {
        public Coverage(string exePath) 
        {
            _covFilePath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.encDir, S57PathName.coverageDir);
        }

        private const float ScaleFactor = 10000000.0f;

        public string ChartName = "";
        private string _covFilePath = "";
        public Dictionary<string, Float2D[][]> DicCov = new();

        public bool ParseCoverage(Transform tf, string chartName, out Float2D[][] pathsCov)
        {
            try
            {
                if (FindCoverage(chartName, out pathsCov) == true) return true;

                var filePath = Path.Combine(_covFilePath, chartName + S57PathName.coverageExt);
                if (File.Exists(filePath) == false) return false;

                var data = File.ReadAllBytes(filePath);

                uint count = 0;
                var size = ByteParser.AsType<uint>(data, ref count);
                if (size <= 0) return false;
                var covSize = ByteParser.AsTypeArray<uint>(data, (int)size, ref count);

                pathsCov = new Float2D[size][];
                for (int i = 0; i < (int)size; i++)
                {
                    var start = covSize[i] + count;
                    var nPT = ByteParser.AsType<int>(data, ref start);
                    var btCover2 = ByteParser.AsType<byte>(data, ref start);
                    if (btCover2 > 0)
                    {
                        pathsCov[i] = new Float2D[0];
                        continue;
                    }

                    var vecPT = ByteParser.AsTypeArray<int>(data, nPT * 2, ref start);
                    int index = 0;
                    Float2D[] pathCover = new Float2D[nPT];
                    for (int k = 0; k < nPT; k++)
                    {
                        pathCover[k].X = vecPT[index++] / ScaleFactor;
                        pathCover[k].Y = vecPT[index++] / ScaleFactor;
                    }

                    // 월드 좌표로 변환
                    // [bsb]
                    //EPSG3857.ToWorld(pathCover);
                    tf.WGS84ToWorld(pathCover);
                    pathsCov[i] = pathCover;
                }

                DicCov.TryAdd(chartName, pathsCov);

                return true;
            }
            catch (Exception ex)
            {
                pathsCov = null;
                var msg = ex.Message;
                return false;
            }
        }

        public bool ParseCoverage(string chartName, out Float2D[][] pathsCov)
        {
            try
            {
                if (FindCoverage(chartName, out pathsCov) == true) return true;

                var filePath = Path.Combine(_covFilePath, chartName + S57PathName.coverageExt);
                if (File.Exists(filePath) == false) return false;

                var data = File.ReadAllBytes(filePath);

                uint count = 0;
                var size = ByteParser.AsType<uint>(data, ref count);
                if (size <= 0) return false;
                var covSize = ByteParser.AsTypeArray<uint>(data, (int)size, ref count);

                pathsCov = new Float2D[size][];
                for (int i = 0; i < (int)size; i++)
                {
                    var start = covSize[i] + count;
                    var nPT = ByteParser.AsType<int>(data, ref start);
                    var btCover2 = ByteParser.AsType<byte>(data, ref start);
                    if (btCover2 > 0)
                    {
                        pathsCov[i] = new Float2D[0];
                        continue;
                    }

                    var vecPT = ByteParser.AsTypeArray<int>(data, nPT * 2, ref start);
                    int index = 0;
                    Float2D[] pathCover = new Float2D[nPT];
                    for (int k = 0; k < nPT; k++)
                    {
                        pathCover[k].X = vecPT[index++] / ScaleFactor;
                        pathCover[k].Y = vecPT[index++] / ScaleFactor;
                    }

                    // 월드 좌표로 변환
                    EPSG3857.ToWorld(pathCover);
                    pathsCov[i] = pathCover;
                }

                DicCov.TryAdd(chartName, pathsCov);

                return true;
            }
            catch (Exception ex)
            {
                pathsCov = null;
                var msg = ex.Message;
                return false;
            }
        }

        public bool FindCoverage(string chartName, out Float2D[][] pathsCov)
        {
            return DicCov.TryGetValue(chartName, out pathsCov);
        }

        public void RemoveCoverage(List<string> listChartName)
        {
            // DicCov의 키 중 listChartName에 포함되지 않은 키만 골라냅니다.
            var keysToRemove = DicCov.Keys.Where(key => !listChartName.Contains(key)).ToList();

            // 골라낸 키들을 삭제합니다.
            keysToRemove.ForEach(key => DicCov.Remove(key));
        }
    }
}
