using JHLib.Util.ByteControl;
using JHLib.Util.Projection;
using JHLib.Util.Struct;
using System.IO;

namespace JHLib.S57.Chart
{
    public class DetectionOutput
    {
        public DetectionOutput() { }

        public DetectionOutput(byte type, string chartName, int rcid)
        {
            Type = type;
            ChartName = chartName;
            RCID = rcid;
            Draw = true;
        }

        public string ChartName = null;
        // 0 = Safety, 1 = Special, 2 = Hazard, 3 = Tool, 4 = Manual Update
        public byte Type = 255;

        public int RCID = -1;
        public short OBJL = -1;
        public byte PRIM = 255;
        // RESARE Object의 Type 0 = NULL, 1 = RESTRN != 14 && CATREA != 28, 2 = RESTRN == 14, 3 = CATREA == 28
        public byte RESARE = 255;

        public Float2D Point = new Float2D(float.MaxValue, float.MaxValue);
        public Float2D[][] PathsInfo = null;

        public bool Draw = true;

        public DetectionOutput Clone()
        {
            var clone = new DetectionOutput
            {
                ChartName = this.ChartName,
                Type = this.Type,
                RCID = this.RCID,
                OBJL = this.OBJL,
                PRIM = this.PRIM,
                RESARE = this.RESARE,
                Draw = this.Draw,
                // Float2D가 struct(구조체)라면 값 복사가 일어납니다. 
                // 만약 class라면 clone.Point = new Float2D(this.Point.X, this.Point.Y); 처럼 처리해야 합니다.
                Point = this.Point
            };

            // PathsInfo (Jagged Array) 깊은 복사 처리
            if (this.PathsInfo != null)
            {
                clone.PathsInfo = new Float2D[this.PathsInfo.Length][];
                for (int i = 0; i < this.PathsInfo.Length; i++)
                {
                    if (this.PathsInfo[i] != null)
                    {
                        clone.PathsInfo[i] = new Float2D[this.PathsInfo[i].Length];
                        // Float2D가 struct라는 가정하에 Array.Copy로 빠르게 복사
                        Array.Copy(this.PathsInfo[i], clone.PathsInfo[i], this.PathsInfo[i].Length);
                    }
                }
            }

            return clone;
        }
    }

    public class DetectionInfo
    {
        ~DetectionInfo()
        {
            ListSafety.Clear();
            ListSafetyDepth.Clear();
            ListSpecial.Clear();
            ListHazardDepth.Clear();
            ListHazardSound.Clear();
            ListHazard.Clear();
        }

        public ST_DETECT_SIZE[] ListDetectSize = null;
        public List<ST_DETECT_SAFETY> ListSafety = new();
        public List<ST_DETECT_SAFETY> ListSafetyDepth = new();
        public List<ST_DETECT_SPECIAL> ListSpecial = new();
        public List<ST_DETECT_HAZARD> ListHazardDepth = new();
        public List<ST_DETECT_HAZARD> ListHazardSound = new();
        public List<ST_DETECT_HAZARD> ListHazard = new();
    }


    public class Detection
    {
        public Detection(string exePath)
        {
            _detectFilePath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.encDir, S57PathName.detectDir);
        }

        public const float ScaleFactor = 10000000.0f;

        private string _detectFilePath = "";
        public Dictionary<string, DetectionInfo> DicDetect = new();

        public bool ParseDetect(string chartName, out DetectionInfo detectInfo)
        {
            try
            {
                if (FindDetect(chartName, out detectInfo) == true) return true;

                var filePath = Path.Combine(_detectFilePath, chartName + S57PathName.detectExt);
                if (File.Exists(filePath) == false) return false;

                var data = File.ReadAllBytes(filePath);

                detectInfo = new();

                return ParsingDetection(chartName, data, detectInfo);
            }
            catch(Exception ex)
            {
                detectInfo = null;
                var msg = ex.Message;
                return false;
            }
        }

        private bool ParsingDetection(string chartName, byte[] data, DetectionInfo detectInfo)
        {
            try
            {
                uint count = 0;
                var size = ByteParser.AsType<uint>(data, ref count);
                if (size <= 0) return false;
                detectInfo.ListDetectSize = ByteParser.AsTypeArray<ST_DETECT_SIZE>(data, (int)size, ref count);

                foreach (var det in detectInfo.ListDetectSize)
                {
                    var nStart = det.Start + count;
                    switch (det.Type)
                    {
                        case 0:     // Safety
                            ParsingSafety(data, nStart, detectInfo);
                            break;
                        case 1:     // Safety Depth
                            ParsingSafetyDepth(data, nStart, detectInfo);
                            break;
                        case 2:     // Special
                            ParsingSpecial(data, nStart, detectInfo);
                            break;
                        case 3:     // Hazard
                            ParsingHazard(data, nStart, detectInfo);
                            break;
                        case 4:     // Hazard Depth
                            ParsingHazardDepth(data, nStart, detectInfo);
                            break;
                        case 5:     // Hazard Sound
                            ParsingHazardSound(data, nStart, detectInfo);
                            break;
                    }
                }

                // 어레이에 추가
                DicDetect.TryAdd(chartName, detectInfo);

                return true;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return false; 
            }
        }

        private uint ParsingPoints(byte[] data, uint start, ref ST_DETECT_POINTS points, byte prim)
        {
            uint count = start;
            var nSize = ByteParser.AsType<uint>(data, ref count);
            var pathPT = ByteParser.AsTypeArray<Int2D>(data, (int)nSize, ref count);

            if (prim != 3)
            {
                points.PathPT = new Float2D[nSize];
                for (int i = 0; i < nSize; i++)
                {
                    points.PathPT[i].X = pathPT[i].X / ScaleFactor;
                    points.PathPT[i].Y = pathPT[i].Y / ScaleFactor;
                }

                // 월드 좌표로 변환
                EPSG3857.ToWorld(points.PathPT);
            }
            else if (prim == 3)
            {
                nSize = ByteParser.AsType<uint>(data, ref count);
                points.ArrShape = ByteParser.AsTypeArray<int>(data, (int)nSize, ref count);
                points.PathsShape = new Float2D[nSize][];

                int index = 0;
                for (int i = 0; i < nSize; i++)
                {
                    var nPT = points.ArrShape[i];
                    var path = new Float2D[nPT];
                    for (int k = 0; k < nPT; k++)
                    {
                        path[k].X = pathPT[index].X / ScaleFactor;
                        path[k].Y = pathPT[index].Y / ScaleFactor;

                        index++;
                    }

                    // 월드 좌표로 변환
                    EPSG3857.ToWorld(path);
                    points.PathsShape[i] = path;
                }
            }

            return count;
        }

        private void ParsingSafety(byte[] data, uint start, DetectionInfo detectInfo)
        {
            uint count = start;

            var safety = new ST_DETECT_SAFETY();
            safety.RCID = ByteParser.AsType<int>(data, ref count);
            safety.OBJL = ByteParser.AsType<short>(data, ref count);
            safety.PRIM = ByteParser.AsType<byte>(data, ref count);

            ParsingPoints(data, count, ref safety.Points, safety.PRIM);

            detectInfo.ListSafety.Add(safety);
        }

        private void ParsingSafetyDepth(byte[] data, uint start, DetectionInfo detectInfo)
        {
            uint count = start;

            var safety = new ST_DETECT_SAFETY();
            safety.RCID = ByteParser.AsType<int>(data, ref count);
            safety.OBJL = ByteParser.AsType<short>(data, ref count);
            safety.PRIM = ByteParser.AsType<byte>(data, ref count);
            safety.DRVAL1 = ByteParser.AsType<float>(data, ref count);

            ParsingPoints(data, count, ref safety.Points, safety.PRIM);

            detectInfo.ListSafetyDepth.Add(safety);
        }

        private void ParsingSpecial(byte[] data, uint start, DetectionInfo detectInfo)
        {
            uint count = start;

            var special = new ST_DETECT_SPECIAL();
            special.RCID = ByteParser.AsType<int>(data, ref count);
            special.OBJL = ByteParser.AsType<short>(data, ref count);
            special.PRIM = ByteParser.AsType<byte>(data, ref count);
            special.RESARE = ByteParser.AsType<byte>(data, ref count);

            ParsingPoints(data, count, ref special.Points, special.PRIM);

            detectInfo.ListSpecial.Add(special);
        }

        private void ParsingHazard(byte[] data, uint start, DetectionInfo detectInfo)
        {
            uint count = start;

            var hazard = new ST_DETECT_HAZARD();
            hazard.RCID = ByteParser.AsType<int>(data, ref count);
            hazard.OBJL = ByteParser.AsType<short>(data, ref count);
            hazard.PRIM = ByteParser.AsType<byte>(data, ref count);

            ParsingPoints(data, count, ref hazard.Points, hazard.PRIM);

            detectInfo.ListHazard.Add(hazard);
        }

        private void ParsingHazardDepth(byte[] data, uint start, DetectionInfo detectInfo)
        {
            uint count = start;

            var hazard = new ST_DETECT_HAZARD();
            hazard.RCID = ByteParser.AsType<int>(data, ref count);
            hazard.OBJL = ByteParser.AsType<short>(data, ref count);
            hazard.PRIM = ByteParser.AsType<byte>(data, ref count);
            hazard.DEPTH_VALUE = ByteParser.AsType<float>(data, ref count);

            ParsingPoints(data, count, ref hazard.Points, hazard.PRIM);

            detectInfo.ListHazardDepth.Add(hazard);
        }

        private void ParsingHazardSound(byte[] data, uint start, DetectionInfo detectInfo)
        {
            uint count = start;

            var hazard = new ST_DETECT_HAZARD();
            hazard.RCID = ByteParser.AsType<int>(data, ref count);
            hazard.OBJL = 129;
            hazard.PRIM = 1;

            var nSize = ByteParser.AsType<int>(data, ref count);
            hazard.ArrSoundg = ByteParser.AsTypeArray<ST_DETECT_HAZARD_SOUND>(data, (int)nSize, ref count);

            ParsingPoints(data, count, ref hazard.Points, hazard.PRIM);

            detectInfo.ListHazardSound.Add(hazard);
        }

        public bool FindDetect(string chartName, out DetectionInfo detectInfo)
        {
            return DicDetect.TryGetValue(chartName, out detectInfo);
        }

        public void RemoveDetect(List<string> listChartName)
        {
            // _dicCov의 키 중 listChartName에 포함되지 않은 키만 골라냅니다.
            var keysToRemove = DicDetect.Keys.Where(key => !listChartName.Contains(key)).ToList();

            // 골라낸 키들을 삭제합니다.
            keysToRemove.ForEach(key => DicDetect.Remove(key));
        }
    }
}
