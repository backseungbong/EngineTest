using JHLib.Graphics;
using JHLib.Graphics.SkiaExtention;
using JHLib.S57.Catalogue;
using JHLib.S57.ChartObject;
using JHLib.S57ManualUpdate.ManualUpdate;
using JHLib.Util.Geometry;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using JHLib.Util.Time;
using JHLib.Weather;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Data;

namespace JHLib.S57.Chart
{
    public class S57ChartInfo
    {
        public S57ChartInfo(string chartName, byte usage, bool overScale, int agency, double scale, Float2D[][] pathsChart)
        {
            ChartName = chartName;
            Usage = usage;
            IsOverScale = overScale;
            PathsChart = pathsChart;
            Agency = agency;
            Scale = scale;
        }

        public string ChartName = null;
        public int Agency = -1;
        public byte Usage = 255;
        public bool IsOverScale = false;
        public Float2D[][] PathsChart = null;
        public double Scale = 0;
    }

    public partial class S57ChartRenderer
    {
        public S57ChartRenderer(string exePath)
        {
            // Manual Update 초기화
            ManualUpdate = new(exePath);

            // 기본 설정값을 읽어옴
            ChartSetupManager.Init(exePath);
            DaiPL4.Init(exePath);
            WeatherColor.Init(exePath);
            ChartCat.Init(exePath);
            AttributeCat.Init(exePath);
            ObjectCat.Init(exePath);
            ChartFinder.Init(exePath);

            CreateDaiToSymbol();
            CreateDaiToLine();
            CreateDaiToPattern();

            ChangeChartCategory(true);

            // 초기화 완료
            _initEnd = true;
        }

        private GraphicsLayer _layer = null;

        private bool _initEnd = false;

        // Lat, Lon Max값 정의
        private const double LatLonMax = 999.9;

        public float ScaleFactor = 0.0333f;

        // 심볼, 라인, 패턴의 어레이
        public Dictionary<string, DrawSymbol> DicSymbol = new();
        public Dictionary<string, DrawLine> DicLine = new();
        public Dictionary<string, DrawPattern> DicPattern = new();

        // Chart 정보를 가지고 있을 어레이 
        public Dictionary<string, ChartMain> DicChart = new();

        // 검색된 Chart정보를 저장할 어레이
        private readonly object _chartLock = new object();
        public List<S57ChartInfo> ListChartInfo = new();

        // Chart Scale Boundary를 Usage별로 구분하기 위한 변수
        public Dictionary<int, Float2D[][]> DicScaleBoundary = new();

        // VDR로 Chart정보를 보내주기 위해서 정보를 저장할 어레이
        public ConcurrentDictionary<string, string> DicSendVdr = new();
        private bool _usedVDR = false;

        // Base = 0, 1 = Standard + Customize, 2 = Standard, 3 = Other
        public byte DisplayLevel = 3;
        // Overscale Factor값을 저장할 변수
        public float OverscaleFactor = float.MaxValue;
        // None Official Chart가 있음을 확인하는 변수 
        public bool NonOfficialChart = false;
        // Not Up to Date가 있음을 확인하는 변수 
        public bool NotUpToDate = false;
        // Overlap Chart 확인 변수
        public bool OverlapChart = false;

        // 자선의 위치정보 저장변수
        public double OwnshipLat = 0;
        public double OwnshipLon = 0;

        // Under / Over Bitmap
        public SKCanvasEx CanvasUnder = null;
        public SKCanvasEx CanvasOver = null;
        // Radar Overlay를 그릴 Bitmap
        private SKCanvasEx CanvasRadar = null;

        // 자선의 위치와 Overlab된 Object를 이동시키는 기능을 사용할 것인지 설정할 변수 
        public readonly object posLock = new object();
        public Float2D OwnshipPosition = new Float2D();

        // Manual Update를 관리하는 변수
        public ManualUpdateManager ManualUpdate = null;
        private bool _clearManualUpdate = false;

        // Permanent Indication을 위한 이벤트
        public delegate void S57PermanentIndication(float overscaleFactor, bool nonEncData, bool notUptoDate, bool overlapChart);
        public event S57PermanentIndication? OnS57PermanentIndication;

        // VDR로 현재 화면에 걸려 있는 차트 정보를 보내주기 위한 이벤트
        public delegate void S57ChartInfoToVDR(ConcurrentDictionary<string, string> dicSendVDR);
        public event S57ChartInfoToVDR? OnS57ChartInfoToVDR;

        public void SetLayer(GraphicsLayer layer)
        {
            _layer = layer;
        }

        public void PendingDrawing()
        {
            if (_layer != null)
            {
                _layer.PendingDrawing(true);
            }
        }

        public Float2D ToWorld(double lon, double lat)
        {
            if (_layer != null)
            {
                var tf = _layer.Transform;
                return tf.WGS84ToWorld(lon, lat);
            }

            return new Float2D();
        }

        public void ResetProjection()
        {
            foreach (var chart in DicChart)
            {
                chart.Value.Dispose();
            }
            DicChart.Clear();

            PendingDrawing();
        }


        public void Dispose()
        {
            lock (_chartLock)
            {
                ListChartInfo.Clear();

                foreach (var chart in DicChart)
                {
                    chart.Value.Dispose();
                }
                DicChart.Clear();

                foreach (var sym in DicSymbol)
                {
                    sym.Value.Dispose();
                }
                DicSymbol.Clear();

                foreach (var line in DicLine)
                {
                    line.Value.Dispose();
                }
                DicLine.Clear();

                foreach (var pattern in DicPattern)
                {
                    pattern.Value.Dispose();
                }
                DicPattern.Clear();

                //if (_timerRADAR != null)
                //{
                //    _timerRADAR.Change(Timeout.Infinite, Timeout.Infinite);
                //    _timerRADAR.Dispose();
                //}

                //if (_bitmapRADAR != null) _bitmapRADAR.Dispose();

                if (CanvasUnder != null) CanvasUnder.Dispose();
                if (CanvasOver != null) CanvasOver.Dispose();
                if(CanvasRadar != null) CanvasRadar.Dispose();
            }
        }

        // 심볼을 생성하는 함수
        public void CreateDaiToSymbol()
        {
            DicSymbol.Clear();

            try
            {
                foreach (var sym in DaiPL4.dicSymbol)
                {
                    var drawSym = new DrawSymbol();
                    if (drawSym.CreateSymbol(sym.Value) == true)
                    {
                        drawSym.SymbolIndex = sym.Key;
                        DicSymbol.TryAdd(sym.Value.synm, drawSym);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] CreateSymbol = " + ex.Message);
            }
        }

        // Line을 생성하는 함수
        public void CreateDaiToLine()
        {
            try
            {
                DicLine.Clear();
                foreach (var line in DaiPL4.dicLine)
                {
                    var drawLine = new DrawLine();
                    if (drawLine.CreateLine(line.Value, DicSymbol) == true)
                    {
                        // Bitmap을 쓸일이 없어서 막음
                        //drawLine.CreateBitmap(ScaleFactor, _clsColor);
                        drawLine.LineIndex = line.Key;
                        DicLine.TryAdd(line.Value.linm, drawLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] CreateLine = " + ex.Message);
            }
        }

        // Pattern을 생성하는 함수
        public void CreateDaiToPattern()
        {
            DicPattern.Clear();
            foreach (var pattern in DaiPL4.dicPattern)
            {
                var drawPattern = new DrawPattern();
                if (drawPattern.CreatePattern(pattern.Value) == true)
                {
                    drawPattern.Patterindex = (byte)pattern.Key;
                    drawPattern.CreatePatternBitmap(ScaleFactor);
                    DicPattern.TryAdd(pattern.Value.panm, drawPattern);
                }
            }
        }

        // 심볼명에 대한 심볼인덱스
        public int GetSymbolIndex(string name)
        {
            if (DicSymbol.TryGetValue(name, out var symbol) == true)
            {
                return symbol.SymbolIndex;
            }

            return -1;
        }

        // 심볼의 사각 사이즈와 피봇 위치를 가져오는 함수
        public bool GetSymbolBoundAndPivot(int symbolIndex, ref float boxWidth, ref float boxHeight, ref float pivotX, ref float pivotY)
        {
            if (DaiPL4.dicSymbol.TryGetValue(symbolIndex, out var symbol) == true)
            {
                if (DicSymbol.TryGetValue(symbol.synm, out var sym) == true)
                {
                    boxWidth = sym.BoxWidth;
                    boxHeight = sym.BoxHeight;
                    pivotX = sym.PivotX;
                    pivotY = sym.PivotY;
                    return true;
                }
            }

            return false;
        }

        // 라인명에 대한 라인인덱스
        public int GetLineIndex(string name)
        {
            if (DicLine.TryGetValue(name, out var line) == true)
            {
                return line.LineIndex;
            }

            return -1;
        }

        public double PTtoDistance(Float2D p1, Float2D p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public float PTtoAngle(Float2D p1, Float2D p2)
        {
            return (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
        }

        public Float2D CalcBrgDistToPoint(Float2D start, float angle, float distance)
        {
            var result = new Float2D(0, 0);

            result.X = (float)(start.X + Math.Cos(angle) * distance);
            result.Y = (float)(start.Y + Math.Sin(angle) * distance);

            return result;
        }

        // 패턴명에 대한 패턴인덱스를 가져오는 함수 
        public byte GetPatterindex(string name)
        {
            if (DicPattern.TryGetValue(name, out var pattern) == true)
            {
                return pattern.Patterindex;
            }

            return 255;
        }

        // 새로 그려져야할 차트명을 가져오는 함수 
        public void FindNewChart(List<S57ChartInfo> listChart, List<S57ChartInfo> listNewChart)
        {
            foreach (var chart in listChart)
            {
                if (DicChart.ContainsKey(chart.ChartName) == false) listNewChart.Add(chart);
            }
        }

        // 현재 검색된 List에 기존에 존재하던 Chart가 없으면 삭제하는 함수 
        public void FindDeleteChart(List<S57ChartInfo> listChart)
        {
            // 비교 속도를 높이기 위해 차트 이름들을 집합(Set)으로 만듭니다.
            var chartNames = new HashSet<string>(listChart.Select(c => c.ChartName));

            // 기존 딕셔너리의 키를 순회하며 삭제 대상을 찾습니다.
            foreach (var key in DicChart.Keys.ToList())
            {
                if (!chartNames.Contains(key) && !CheckIndexMap(key))
                {
                    DicChart[key].Dispose();
                    DicChart.Remove(key);
                }
            }
        }

        // Index Chart인지 찾는 함수
        public bool CheckIndexMap(string chartName)
        {
            if (chartName == "WORLDMAP1" || chartName == "WORLDMAP2" || chartName == "WORLDMAP3"
                || chartName == "WORLDMAP4" || chartName == "WORLDMAP5" || chartName == "WORLDMAP6")
            {
                return true;
            }

            return false;
        }

        // 현재 그려지고 있는 차트의 Usage위에 그려질 차트 영역안에 점이 포함되는지 확인하는 함수 
        public bool CheckOverUsageChartInPivot(Transform projection, byte usage, Float2D pivot)
        {
            // 마지막 Usage차트이면 빠져나간다.
            if (usage == 5) return false;

            var wxy = projection.ScreenToWorld(pivot);
            lock (_chartLock)
            {
                foreach (var chart in ListChartInfo)
                {
                    if (chart.Usage > usage)
                    {
                        foreach (var path in chart.PathsChart)
                        {
                            if (path != null && GeometryHelper.PointInPolygonWinding(wxy, path) == true)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public void ClearChartArea(GraphicsContext context, Float2D[][] pathsChart)
        {
            if (pathsChart == null || pathsChart.Length < 0) return;

            context.SetFillColor(SKColors.White);
            context.SetFillBlend(SKBlendMode.Clear);
            foreach (var path in pathsChart) context.FillPathWorld(path);
            context.SetFillBlend();
        }

        public void RadarOverlayOnOff(bool radarOvarlayOn)
        {
            S57ChartOption.Option.RadarOverlayOn = radarOvarlayOn;
            ChartSetupManager.SaveS57ChartOption();
            PendingDrawing();
        }

        public void SetOwnshipPosition(double dLat, double dLon)
        {
            if (dLat == LatLonMax || dLon == LatLonMax) return;

            OwnshipLat = dLat;
            OwnshipLon = dLon;
        }


        public void LoadCharts(Transform projection)
        {
            // 초기화가 끝나지 않았으면 빠져나감
            if (_initEnd == false) return;

            lock (_chartLock)
            {
                // 화면 영역에 걸치는 전자해도 찾기 
                ChartFinder.FindIntersectionScreenToCharts(projection, ListChartInfo, DicScaleBoundary, DicSendVdr, ref OverscaleFactor, ref NonOfficialChart, ref NotUpToDate, ref OverlapChart);

                // 기존에 찾은 차트가 현재 찾을 차트중에 없으면 지우는 행위를 함
                FindDeleteChart(ListChartInfo);

                // 새롭게 그려야할 차트를 찾음
                List<S57ChartInfo> listNewChart = new();
                FindNewChart(ListChartInfo, listNewChart);

                int size = listNewChart.Count;
                if (size > 0)
                {
                    ChartMain[] arrChart = new ChartMain[size];
                    Parallel.For(0, size, index =>
                    {
                        string chartName = listNewChart[index].ChartName;
                        bool bIndexMap = CheckIndexMap(chartName);
                        arrChart[index] = new ChartMain(ChartFinder.ExePath, listNewChart[index], this);
                        arrChart[index].LoadChart(chartName, bIndexMap);
                    });

                    for (int i = 0; i < size; i++)
                    {
                        DicChart.TryAdd(listNewChart[i].ChartName, arrChart[i]);
                    }
                }

                listNewChart.Clear();
            }
        }

        public void SetClearManualUpdate()
        {
            _clearManualUpdate = true;
            PendingDrawing();
        }

        private void ClearManualUpdate()
        {
            if (_clearManualUpdate == false) return;

            lock (_chartLock)
            {
                foreach(var chart in DicChart)  chart.Value.ListManualUpdate.Clear();
            }
        }

        // CS 관련 처리 함수들
        public void CS_SNDFRM04(float depthValue, List<int> listSoundIndex)
        {
            listSoundIndex.Clear();

            string symName = "SOUND";
            if (depthValue <= S57ChartSafetyValue.SafetyDepth)
                symName += "S";
            else
                symName += "G";

            string sym;
            if (depthValue < 0.0)
            {
                sym = symName;
                sym += "A1";
                int index = GetSymbolIndex(sym);
                listSoundIndex.Add(index);
                sym = "";
                depthValue *= -1.0f;
                depthValue += 0.01f;
            }

            bool fraction = false;
            int depth = (int)depthValue;
            if (depth < depthValue) fraction = true;

            if (depthValue < 10.0)
            {
                int value = 10 + depth;
                sym = symName + value.ToString();
                int index = GetSymbolIndex(sym);
                listSoundIndex.Add(index);
                if (fraction == true)
                {
                    int nF = (int)(((float)(depthValue + 0.05) - depth) * 10.0);
                    value = 50 + nF;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);
                }
            }
            else if (depthValue < 31.0 && fraction == true)
            {
                string strDigit = depth.ToString();
                int nLen = strDigit.Length;
                int n10, n1;
                n10 = n1 = 0;
                if (nLen >= 2)
                {
                    n10 = int.Parse(strDigit.Substring(0, 1));
                    n1 = int.Parse(strDigit.Substring(nLen - 1, 1));

                    int value = 20 + n10;
                    sym = symName + value.ToString();
                    int index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = 10 + n1;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    int nF = (int)(((float)(depthValue + 0.05) - depth) * 10.0);
                    value = 50 + nF;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);
                }
            }
            else if (depthValue < 100.0)
            {
                string strDigit = depth.ToString();
                int nLen = strDigit.Length;
                int n10, n1;
                n10 = n1 = 0;
                if (nLen >= 2)
                {
                    n10 = int.Parse(strDigit.Substring(0, 1));
                    n1 = int.Parse(strDigit.Substring(nLen - 1, 1));

                    int value = 10 + n10;
                    sym = symName + value.ToString();
                    int index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = n1;
                    sym = symName + value.ToString("00");
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);
                }
            }
            else if (depthValue < 1000.0)
            {
                string strDigit = depth.ToString();
                int nLen = strDigit.Length;
                int n100, n10, n1;
                n100 = n10 = n1 = 0;
                if (nLen >= 3)
                {
                    n100 = int.Parse(strDigit.Substring(0, 1));
                    n10 = int.Parse(strDigit.Substring(1, 1));
                    n1 = int.Parse(strDigit.Substring(nLen - 1, 1));

                    int value = 20 + n100;
                    sym = symName + value.ToString();
                    int index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = 10 + n10;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = n1;
                    sym = symName + value.ToString("00");
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);
                }
            }
            else if (depthValue < 10000.0)
            {
                string strDigit = depth.ToString();
                int nLen = strDigit.Length;
                int n1000, n100, n10, n1;
                n1000 = n100 = n10 = n1 = 0;
                if (nLen >= 4)
                {
                    n1000 = int.Parse(strDigit.Substring(0, 1));
                    n100 = int.Parse(strDigit.Substring(1, 1));
                    n10 = int.Parse(strDigit.Substring(2, 1));
                    n1 = int.Parse(strDigit.Substring(nLen - 1, 1));

                    int value = 20 + n1000;
                    sym = symName + value.ToString();
                    int index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = 10 + n100;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = n10;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = 40 + n1;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);
                }
            }
            else
            {
                string strDigit = depth.ToString();
                int nLen = strDigit.Length;
                int n10000, n1000, n100, n10, n1;
                n10000 = n1000 = n100 = n10 = n1 = 0;
                if (nLen >= 5)
                {
                    n10000 = int.Parse(strDigit.Substring(0, 1));
                    n1000 = int.Parse(strDigit.Substring(1, 1));
                    n100 = int.Parse(strDigit.Substring(2, 1));
                    n10 = int.Parse(strDigit.Substring(3, 1));
                    n1 = int.Parse(strDigit.Substring(nLen - 1, 1));

                    int value = 30 + n10000;
                    sym = symName + value.ToString();
                    int index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = 20 + n1000;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = 10 + n100;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = n10;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);

                    value = 40 + n1;
                    sym = symName + value.ToString();
                    index = GetSymbolIndex(sym);
                    listSoundIndex.Add(index);
                }
            }
        }

        public void CS_SAFCON01(float depthValue, List<short> listSY)
        {
            listSY.Clear();

            string symbol, sSound;
            string sPrefix = "SAFCON";
            int depth = (int)depthValue;
            bool bFrac = false;

            if (depthValue > depth)
            {
                int nFrac = (int)(((depthValue + 0.05) - (float)depth) * 10.0);
                if (nFrac > 0)
                {
                    sSound = depth.ToString() + nFrac.ToString("0");
                    bFrac = true;
                }
                else
                {
                    sSound = depth.ToString();
                }
            }
            else
            {
                sSound = depth.ToString();
            }

            if (depthValue < 0.0f || depthValue > 99999.0f)
            {
                return;
            }

            if (depthValue < 10.0f)
            {
                symbol = $"{sPrefix}0{sSound.Substring(0, 1)}";
                var sIndex = (short)GetSymbolIndex(symbol);
                listSY.Add(sIndex);

                if (bFrac == true && sSound.Length >= 2)
                {
                    symbol = $"{sPrefix}6{sSound.Substring(1, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);
                }

                return;
            }
            else if (depthValue < 31.0f && bFrac == true)
            {
                if (sSound.Length >= 3)
                {
                    symbol = $"{sPrefix}2{sSound.Substring(0, 1)}";
                    var sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}1{sSound.Substring(1, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}5{sSound.Substring(2, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);
                }

                return;
            }
            else if (depthValue < 100.0f)
            {
                if (sSound.Length >= 2)
                {
                    symbol = $"{sPrefix}2{sSound.Substring(0, 1)}";
                    var sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}1{sSound.Substring(1, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);
                }

                return;
            }
            else if (depthValue < 1000.0f)
            {
                if (sSound.Length >= 3)
                {
                    symbol = $"{sPrefix}8{sSound.Substring(0, 1)}";
                    var sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}0{sSound.Substring(1, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}9{sSound.Substring(2, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);
                }

                return;
            }
            else if (depthValue < 10000.0f)
            {
                if (sSound.Length >= 4)
                {
                    symbol = $"{sPrefix}3{sSound.Substring(0, 1)}";
                    var sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}2{sSound.Substring(1, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}1{sSound.Substring(2, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}7{sSound.Substring(3, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);
                }

                return;
            }
            else
            {
                if (sSound.Length >= 5)
                {
                    symbol = $"{sPrefix}4{sSound.Substring(0, 1)}";
                    var sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}3{sSound.Substring(1, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}2{sSound.Substring(2, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}1{sSound.Substring(3, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);

                    symbol = $"{sPrefix}7{sSound.Substring(4, 1)}";
                    sIndex = (short)GetSymbolIndex(symbol);
                    listSY.Add(sIndex);
                }

                return;
            }
        }

        // Text의 Align을 계산하는 함수
        public Float2D CalcTextAlign(int textAlign, int len, int height, Float2D pivot)
        {
            Float2D rtn = new Float2D(0, 0);
            rtn = pivot;

            int ten = textAlign / 10;
            int rem = textAlign % 10;

            int width = 8;

            switch (ten)
            {
                case 1://TA_CENTER;
                    rtn.X -= len * width / 2.0f;
                    break;
                case 2://TA_RIGHT;
                    rtn.X -= len * width;
                    break;
                case 3://TA_LEFT;
                    break;
            }

            switch (rem)
            {
                case 1://TA_BOTTOM;
                    rtn.Y -= height;
                    break;
                case 2://TA_BASELINE;
                    rtn.Y -= height / 2;
                    break;
                case 3://TA_TOP;
                    break;
            }

            //switch (ten)
            //{
            //    case 1://TA_CENTER;
            //        rtn.X -= len * width / 2.0f;
            //        break;
            //    case 2://TA_RIGHT;
            //        rtn.X -= len * width;
            //        break;
            //    case 3://TA_LEFT;
            //        break;
            //}

            //switch (rem)
            //{
            //    case 1://TA_BOTTOM;
            //        rtn.Y -= height;
            //        break;
            //    case 2://TA_BASELINE;
            //        rtn.Y -= height / 2;
            //        break;
            //    case 3://TA_TOP;
            //        break;
            //}

            return rtn;
        }

        // 안전 수심값 변경 함수
        public void ChangeSafetyValue(float shallowContour, float safetyContour, float deepContour, float safetyDpeth)
        {
            S57ChartSafetyValue.Safetyvalue.SafetyDepth = safetyDpeth;
            S57ChartSafetyValue.Safetyvalue.ShallowContour = shallowContour;
            S57ChartSafetyValue.Safetyvalue.SafetyContour = safetyContour;
            S57ChartSafetyValue.Safetyvalue.DeepContour = deepContour;

            ChangeSafetyValueAndDangerObject(0);
        }

        public void ApplySafetyValue()
        {
            ChangeSafetyValueAndDangerObject(0);
        }

        // 전자해도 옵션을 변경하는 함수 
        public void ChangeChartOption(bool changeFourDepthShade = false, bool changeShallowWaterDangers = false)
        {
            if (changeFourDepthShade)
            {
                ChangeSafetyValueAndDangerObject(1);
            }

            if (changeShallowWaterDangers)
            {
                ChangeSafetyValueAndDangerObject(2);
            }

            ChartSetupManager.SaveS57ChartOption();

            PendingDrawing();
        }

        public void ChangeChartCategory(bool init = false)
        {
            var standardList = new List<bool>
            {
                S57ChartCategory.DryingLine, S57ChartCategory.AllBuoyBeacons,
                S57ChartCategory.BuoysBeacons, S57ChartCategory.Lights,
                S57ChartCategory.BoundariesLimits, S57ChartCategory.ProhitbitedRestricted,
                S57ChartCategory.CautionaryNotes, S57ChartCategory.TrafficRoute,
                S57ChartCategory.ArchipelagicSeaLanes, S57ChartCategory.StandardMiscellaneous,
                S57ChartCategory.ChartScaleBoundaries
                // ChartScaleBoundaries 특수 로직 포함
                //S57ChartCategory.ChartScaleBoundaries || !S57ChartOption.OverScalePattern
            };

            var otherList = new List<bool>
            {
                S57ChartCategory.SpotSoundings, S57ChartCategory.CablesPipelines,
                S57ChartCategory.AllIsolatedDangers, S57ChartCategory.MagnaticVariation,
                S57ChartCategory.DepthContours, S57ChartCategory.Seabed,
                S57ChartCategory.Tidal, S57ChartCategory.OthersMiscellaneous,
                S57TextGroup.ImportantText, S57TextGroup.OtherText,
                S57TextGroup.LightDescription, S57TextGroup.Names, S57TextGroup.AllOther
            };

            bool anyStandard = standardList.Any(x => x);
            bool allStandard = standardList.All(x => x);
            bool anyOther = otherList.Any(x => x);
            bool allOther = otherList.All(x => x);

            byte btRtn = 0;

            if (!anyStandard && !anyOther)
            {
                btRtn = 0; // 1. 순수 Base 상태 (Standard/Other가 하나도 없음)
            }
            else if (allStandard && !anyOther)
            {
                btRtn = 2; // 2. 순수 Standard 상태 (Standard는 다 켜졌고, Other는 하나도 없음)
            }
            else if (allStandard && allOther)
            {
                btRtn = 3; // 3. All 정보 상태 (Standard와 Other가 모두 다 켜짐)
            }
            else
            {
                btRtn = 1; // 4. 그 외 모든 수정된 상태 (Standard 일부 누락, 혹은 Standard+Other 일부)
            }

            // Base = 0, 1 = Custom, 2 = Standard, 3 = Other
            DisplayLevel = btRtn;

            if(init == false)
            {
                ChartSetupManager.SaveS57ChartCategory();
                ChartSetupManager.SaveS57ChartTextGroup();
            }

            PendingDrawing();
        }

        public byte GetDisplayLevel() => DisplayLevel;

        //public string GetDisplayLevel()
        //{
        //    if (DisplayLevel == 0) return "Base";
        //    else if (DisplayLevel == 1) return "Standard(Customize)";
        //    else if (DisplayLevel == 2) return "Standard";
        //    else if (DisplayLevel == 3) return "Other";

        //    return string.Empty;
        //}

        public void ChangeWeather(byte weatherIndex)
        {
            WeatherColor.WeatherIndex = weatherIndex;
            PendingDrawing();
        }

        // 옵션의 변경에 따른 Contour Value의 적용 및 위험물 변경 함수 (type : 0 = All, 1 = Contour, 2 = Danger)
        public void ChangeSafetyValueAndDangerObject(byte type)
        {
            lock (_chartLock)
            {
                foreach (var chart in DicChart)
                {
                    var enc = chart.Value as ChartMain;
                    if (CheckIndexMap(enc.ChartInfo.ChartName) == true) continue;

                    if (type == 0)
                    {
                        enc.ListDepareCS.Clear();
                        enc.ListDrgareCS.Clear();
                        enc.ListObstrnCS.Clear();
                        enc.ListWrecksCS.Clear();

                        for (int i = 1; i < 8; i++)
                        {
                            var layer = enc.Layer[i] as ChartLayer;
                            var count = layer.ListDepare.Count;
                            for (int k = 0; k < count; k++)
                            {
                                byte csPriority = 0;
                                enc.DepareChangeContourValue(layer.ListDepare[k], ref csPriority);
                                if (csPriority != 0)
                                {
                                    var cs = new ST_CS();
                                    cs.LayerNum = (byte)i;
                                    cs.Index = k;
                                    cs.RCID = layer.ListDepare[k].Header.RCID;
                                    enc.ListDepareCS.Add(cs);
                                }
                            }

                            count = layer.ListDrgare.Count;
                            for (int k = 0; k < count; k++)
                            {
                                byte btCSPriority = 0;
                                enc.DrgareChangeContourValue(layer.ListDrgare[k], ref btCSPriority);
                                if (btCSPriority != 0)
                                {
                                    var cs = new ST_CS();
                                    cs.LayerNum = (byte)i;
                                    cs.Index = k;
                                    cs.RCID = layer.ListDrgare[k].Header.RCID;
                                    enc.ListDrgareCS.Add(cs);
                                }
                            }

                            count = layer.ListObstrn.Count;
                            for (int k = 0; k < count; k++)
                            {
                                var obstrn = layer.ListObstrn[k];
                                obstrn.IsDanger = enc.UDWHAZ05(obstrn, S57ChartSafetyValue.SafetyContour, S57ChartOption.ShallowWaterDangers);
                                if (obstrn.IsChangePriority == true)
                                {
                                    var cs = new ST_CS();
                                    cs.LayerNum = (byte)i;
                                    cs.Index = k;
                                    cs.RCID = obstrn.Header.RCID;
                                    enc.ListObstrnCS.Add(cs);
                                }
                                else obstrn.Header.ViewingGroup = obstrn.OriViewingGroup;
                            }

                            count = layer.ListWrecks.Count;
                            for (int k = 0; k < count; k++)
                            {
                                var wreck = layer.ListWrecks[k];
                                wreck.IsDanger = enc.UDWHAZ05(wreck, S57ChartSafetyValue.SafetyContour, S57ChartOption.ShallowWaterDangers);
                                if (wreck.IsChangePriority == true)
                                {
                                    var cs = new ST_CS();
                                    cs.LayerNum = (byte)i;
                                    cs.Index = k;
                                    cs.RCID = wreck.Header.RCID;
                                    enc.ListWrecksCS.Add(cs);
                                }
                                else wreck.Header.ViewingGroup = wreck.OriViewingGroup;
                            }
                        }
                    }
                    else if (type == 1)
                    {
                        enc.ListDepareCS.Clear();
                        enc.ListDrgareCS.Clear();

                        for (int i = 1; i < 8; i++)
                        {
                            var layer = enc.Layer[i] as ChartLayer;
                            var count = layer.ListDepare.Count;
                            for (int k = 0; k < count; k++)
                            {
                                byte btCSPriority = 0;
                                enc.DepareChangeContourValue(layer.ListDepare[k], ref btCSPriority);
                                if (btCSPriority != 0)
                                {
                                    var cs = new ST_CS();
                                    cs.LayerNum = (byte)i;
                                    cs.Index = k;
                                    cs.RCID = layer.ListDepare[k].Header.RCID;
                                    enc.ListDepareCS.Add(cs);
                                }
                            }

                            count = layer.ListDrgare.Count;
                            for (int k = 0; k < count; k++)
                            {
                                byte csPriority = 0;
                                enc.DrgareChangeContourValue(layer.ListDrgare[k], ref csPriority);
                                if (csPriority != 0)
                                {
                                    var cs = new ST_CS();
                                    cs.LayerNum = (byte)i;
                                    cs.Index = k;
                                    cs.RCID = layer.ListDrgare[k].Header.RCID;
                                    enc.ListDrgareCS.Add(cs);
                                }
                            }
                        }
                    }
                    else if (type == 2)
                    {
                        enc.ListObstrnCS.Clear();
                        enc.ListWrecksCS.Clear();

                        for (int i = 1; i < 8; i++)
                        {
                            var layer = enc.Layer[i] as ChartLayer;
                            var count = layer.ListObstrn.Count;
                            for (int k = 0; k < count; k++)
                            {
                                var obstrn = layer.ListObstrn[k];
                                obstrn.IsDanger = enc.UDWHAZ05(obstrn, S57ChartSafetyValue.SafetyContour, S57ChartOption.ShallowWaterDangers);
                                if (obstrn.IsChangePriority == true)
                                {
                                    var cs = new ST_CS();
                                    cs.LayerNum = (byte)i;
                                    cs.Index = k;
                                    cs.RCID = obstrn.Header.RCID;
                                    enc.ListObstrnCS.Add(cs);
                                }
                                else obstrn.Header.ViewingGroup = obstrn.OriViewingGroup;
                            }

                            count = layer.ListWrecks.Count;
                            for (int k = 0; k < count; k++)
                            {
                                var wreck = layer.ListWrecks[k];
                                wreck.IsDanger = enc.UDWHAZ05(wreck, S57ChartSafetyValue.SafetyContour, S57ChartOption.ShallowWaterDangers);
                                if (wreck.IsChangePriority == true)
                                {
                                    var cs = new ST_CS();
                                    cs.LayerNum = (byte)i;
                                    cs.Index = k;
                                    cs.RCID = wreck.Header.RCID;
                                    enc.ListWrecksCS.Add(cs);
                                }
                                else wreck.Header.ViewingGroup = wreck.OriViewingGroup;
                            }
                        }
                    }
                }
            }

            PendingDrawing();
        }

        // Transparent를 byte으로 변환하는 함수 
        public byte TransparentToByte(byte trans)
        {
            byte result = 255;
            switch (trans)
            {
                case 1:
                    result = 191;
                    break;
                case 2:
                    result = 127;
                    break;
                case 3:
                    result = 64;
                    break;
                case 4:
                    result = 0;
                    break;
            }

            return result;
        }

        // Text Group을 확인하는 함수 
        public bool FindTextGroup(byte textGroup)
        {
            bool bRtn = false;

            switch (textGroup)
            {
                case 11:
                    if (S57TextGroup.ImportantText == true) bRtn = true;
                    break;
                case 21:
                case 26:
                case 29:
                    if (S57TextGroup.OtherText == true) bRtn = true;
                    if (S57TextGroup.Names == true) bRtn = true;
                    break;
                case 23:
                    if (S57TextGroup.OtherText == true) bRtn = true;
                    if (S57TextGroup.LightDescription == true) bRtn = true;
                    break;
                case 25:
                case 27:
                case 28:
                case 30:
                    if (S57TextGroup.OtherText == true) bRtn = true;
                    if (S57TextGroup.AllOther == true) bRtn = true;
                    break;
                case 31:
                    bRtn = true;
                    break;
            }

            return bRtn;
        }
        // Viewing Group 존재 여부 확인 함수
        public bool FindViewingGroup(byte group)
        {
            // Other상태이면 OK
            if (DisplayLevel == 3) return true;

            bool bRtn = false;

            switch (group)
            {
                case 1:     // Base
                    bRtn = true;
                    break;
                case 2:     // Standard / Drying line
                    if (S57ChartCategory.DryingLine == true) bRtn = true;
                    break;
                case 3:     // Standard / Unknown Object
                    if (S57ChartCategory.AllBuoyBeacons == true) bRtn = true;
                    if (S57ChartCategory.BuoysBeacons == true) bRtn = true;
                    // Standard가 꺼지면 의미가 없으며, 만약 Standard가 켜졌을때 옵션에 의해서 On/Off가 되도록 수정함
                    if (bRtn == true)
                    {
                        bRtn = S57ChartOption.UnknownObject;
                    }
                    break;
                case 4:
                    if (S57ChartCategory.AllBuoyBeacons == true) bRtn = true;
                    if (S57ChartCategory.BuoysBeacons == true) bRtn = true;
                    break;
                case 5:
                    if (S57ChartCategory.AllBuoyBeacons == true) bRtn = true;
                    if (S57ChartCategory.Lights == true) bRtn = true;
                    break;
                case 6:
                    if (S57ChartCategory.BoundariesLimits == true) bRtn = true;
                    break;
                case 7:
                    if (S57ChartCategory.ProhitbitedRestricted == true) bRtn = true;
                    break;
                case 8:
                    if (S57ChartCategory.ChartScaleBoundaries == true) bRtn = true;
                    break;
                case 9:
                    if (S57ChartCategory.CautionaryNotes == true) bRtn = true;
                    break;
                case 10:
                    if (S57ChartCategory.TrafficRoute == true) bRtn = true;
                    break;
                case 11:
                    if (S57ChartCategory.ArchipelagicSeaLanes == true) bRtn = true;
                    break;
                case 12:
                    if (S57ChartCategory.StandardMiscellaneous == true) bRtn = true;
                    break;
                case 13:
                    if (S57ChartCategory.SpotSoundings == true) bRtn = true;
                    break;
                case 14:
                    if (S57ChartCategory.CablesPipelines == true) bRtn = true;
                    break;
                case 15:
                    if (S57ChartCategory.AllIsolatedDangers == true) bRtn = true;
                    break;
                case 16:
                    if (S57ChartCategory.MagnaticVariation == true) bRtn = true;
                    break;
                case 17:
                    if (S57ChartCategory.DepthContours == true) bRtn = true;
                    break;
                case 18:
                    if (S57ChartCategory.Seabed == true) bRtn = true;
                    break;
                case 19:
                    if (S57ChartCategory.Tidal == true) bRtn = true;
                    break;
                case 20:
                    if (S57ChartCategory.OthersMiscellaneous == true) bRtn = true;
                    break;
            }

            return bRtn;
        }
        // Viewing Group에 따른 Category번호를 받는 함수
        public byte GetViewingGroupToCategoryNum(byte group)
        {
            byte categoryNum = 0;

            if (group >= 2 && group <= 12) categoryNum = 1;
            else if (group >= 13 && group <= 20) categoryNum = 4;

            return categoryNum;
        }

        // Scale Min Check함수
        public bool CheckScaleMin(int scaleMin, Transform tf)
        {
            if (S57ChartOption.ScaleMin == true)
            {
                if (scaleMin != int.MaxValue)
                {
                    if (tf.Scale >= (double)scaleMin)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Date Dependent한 Object의 Check함수
        public bool CheckDateDependent(int startDate, int endDate, ref bool isDateDependent, bool isCurrentDate = true , int startRangeDate = 0, int endRangeDate = 0)
        {
            isDateDependent = false;

            int startDateResult = 0;
            int endDateResult = 0;
            string ymdStart = "YMD";
            string ymdEnd = "YMD";

            // 시작 시간만 있는 경우
            if (startDate > 0 && endDate == 0)
            {
                if (IsUnknownDate(startDate, true, out ymdStart, out startDateResult) == false)
                {
                    var year = startDateResult.ToString().Substring(0, 4);
                    int.TryParse($"{year}1231", out endDateResult);
                }
                else return false;
            }
            // 종료 시간만 있는 경우
            else if (startDate == 0 && endDate > 0)
            {
                if (IsUnknownDate(endDate, false, out ymdEnd, out endDateResult) == false)
                {
                    var year = endDateResult.ToString().Substring(0, 4);
                    int.TryParse($"{year}0101", out startDateResult);
                }
                else return false;
            }
            // 시간 Data가 없는 경우
            else if (startDate <= 0 && endDate <= 0) return true;
            // 정상인 경우
            else
            {
                IsUnknownDate(startDate, true, out ymdStart, out startDateResult);
                IsUnknownDate(endDate, false, out ymdEnd, out endDateResult);
            }

            var now = AppTime.Utc;

            // 2. 비교 대상 시간 범위(Target Range) 설정
            int targetStart, targetEnd;
            if(isCurrentDate)
            {
                if (ymdStart == "EMD") targetStart = int.Parse(now.ToString("MMdd"));
                else if (ymdStart == "EED") targetStart = int.Parse(now.ToString("dd"));
                else targetStart = int.Parse(now.ToString("yyyyMMdd"));

                if (ymdEnd == "EMD") targetEnd = int.Parse(now.ToString("MMdd"));
                else if (ymdEnd == "EED") targetEnd = int.Parse(now.ToString("dd"));
                else targetEnd = int.Parse(now.ToString("yyyyMMdd"));
            }
            else
            {
                targetStart = startRangeDate;
                var start = startRangeDate.ToString();
                if (ymdStart == "EMD") targetStart = int.Parse(start.Substring(4, 4));
                else if (ymdStart == "EED") targetStart = int.Parse(start.Substring(6, 2));

                targetEnd = endRangeDate;
                var end = endRangeDate.ToString();
                if (ymdEnd == "EMD") targetEnd = int.Parse(end.Substring(4, 4));
                else if (ymdEnd == "EED") targetEnd = int.Parse(end.Substring(6, 2));
            }

            // 시작 조건 검사 (객체의 시작일이 비교 범위의 끝보다 작거나 같아야 함)
            bool startCondition = startDateResult <= targetEnd;

            // 종료 조건 검사 (객체의 종료일이 비교 범위의 시작보다 크거나 같아야 함)
            bool endCondition = endDateResult >= targetStart;

            isDateDependent = startCondition && endCondition;

            return isDateDependent;
        }

        private bool IsUnknownDate(int date, bool isStart, out string ymd, out int dateResult)
        {
            ymd = "YMD";
            dateResult = 0;
            if (date <= 0 || date == 99999999) return true;

            string dateStr = date.ToString();
            if (dateStr.Length < 8)  return true;

            var year = dateStr.Substring(0, 4);
            var month = dateStr.Substring(4, 2);
            var day = dateStr.Substring(6, 2);

            bool goodYear = year == "9999" ? false : true;
            bool goodMonth = month == "99" ? false : true;
            bool goodDay = day == "99" ? false : true;

            if (goodYear && goodMonth && goodDay)
            {
                dateResult = date;
                ymd = "YMD";
            }
            else if (!goodYear && goodMonth && goodDay)
            {
                int.TryParse($"{month}{day}", out dateResult);
                ymd = "EMD";
            }
            else if (!goodYear && !goodMonth && goodDay)
            {
                int.TryParse($"{day}", out dateResult);
                ymd = "EED";
            }
            else if (!goodYear && goodMonth && !goodDay)
            {
                if (isStart) int.TryParse($"{month}01", out dateResult);
                else int.TryParse($"{month}31", out dateResult);
                ymd = "EMD";
            }
            else if (goodYear && goodMonth && !goodDay)
            {
                if (isStart) int.TryParse($"{year}{month}01", out dateResult);
                else int.TryParse($"{year}{month}31", out dateResult);
                ymd = "YMD";
            }
            else if (goodYear && !goodMonth && !goodDay)
            {
                if (isStart) int.TryParse($"{year}0101", out dateResult);
                else int.TryParse($"{year}1231", out dateResult);
                ymd = "YMD";
            }

            return false;
        }

        public void SetOwnshipPosition(Float2D position)
        {
            lock(posLock)
            {
                OwnshipPosition = position;
            }
        }

        public Float2D[] GetOwnshipArea()
        {
            lock(posLock)
            {
                var offset = 50;
                var tf = _layer?.Transform;
                var sxy = tf.WGS84ToScreen(OwnshipPosition);
                var PathOwnship = new Float2D[4];
                PathOwnship[0] = new Float2D(sxy.X - offset, sxy.Y - offset);
                PathOwnship[1] = new Float2D(sxy.X + offset, sxy.Y - offset);
                PathOwnship[2] = new Float2D(sxy.X + offset, sxy.Y + offset);
                PathOwnship[3] = new Float2D(sxy.X - offset, sxy.Y + offset);

                return PathOwnship;
            }
        }

        public void SetUsedVDR(bool usedVDR) => _usedVDR = usedVDR;

        public void LoadNewManualUpdateInfo(string chartName)
        {
            lock (_chartLock)
            {
                if(DicChart.TryGetValue(chartName, out var chart) == true)
                {
                    ManualUpdate.LoadNewManualUpdate(chartName, out chart.ListManualUpdate);
                    PendingDrawing();
                }
            }
        }
        public void LoadManualUpdateInfo(string chartName)
        {
            lock (_chartLock)
            {
                if (DicChart.TryGetValue(chartName, out var chart) == true)
                {
                    ManualUpdate.LoadManualUpdate(chartName, out chart.ListManualUpdate);
                    PendingDrawing();
                }
            }
        }
    }
}
