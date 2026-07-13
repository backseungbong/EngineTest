using JHLib.ChartManager.Catalogue;
using JHLib.S57.Catalogue;
using JHLib.S57ManualUpdate.ManualUpdate;
using JHLib.Util.Geometry;
using JHLib.Util.Geometry.Clipper2;
using JHLib.Util.Projection;
using JHLib.Util.Projection.ScreenTransform;
using JHLib.Util.Struct;
using System.Collections.Concurrent;

namespace JHLib.S57.Chart
{
    public class Worldmap
    {
        public string Name = null;
        public Float2D[] PathArea = new Float2D[4];
    }

    public static class ChartFinder
    {
        public static string ExePath = "";

        public const float ScaleFactor = 10000000.0f;

        // Coverage 사용
        public static Coverage FindCoverage = null;
        public static Coverage OwnshipCoverage = null;
        public static Coverage RouteCoverage = null;

        // Detection 사용
        public static Detection OwnshipDetection = null;
        public static Detection RouteDetection = null;

        // World Map정보를 저장할 어레이
        public static List<Worldmap> ListWorldMap = new();

        // Manual Update Detection을 위한 변수
        public static ManualUpdateManager ManualUpdate = null;

        public static void Init(string exePath)
        {
            ExePath = exePath;

            ManualUpdate = new(exePath);

            AgencyCat.Init(exePath);

            FindCoverage = new Coverage(exePath);
            OwnshipCoverage = new Coverage(exePath);   
            RouteCoverage = new Coverage(exePath);

            OwnshipDetection= new Detection(exePath);
            RouteDetection = new Detection(exePath); 
        }

        // S-98 5.2판에 맞춰서 Usage 적용해 봄
        //private const float Usage1_OptimumScale = 10000000.0f;
        //private const float Usage2_OptimumScale = 3500000.0f;
        //private const float Usage3_OptimumScale = 1500000.0f;
        //private const float Usage4_OptimumScale = 700000.0f;
        //private const float Usage5_OptimumScale = 350000.0f;
        //private const float Usage6_OptimumScale = 180000.0f;
        //private const float Usage7_OptimumScale = 90000.0f;
        //private const float Usage8_OptimumScale = 45000.0f;
        //private const float Usage9_OptimumScale = 22000.0f;
        //private const float Usage10_OptimumScale = 12000.0f;
        //private const float Usage11_OptimumScale = 8000.0f;
        //private const float Usage12_OptimumScale = 4000.0f;
        //private const float Usage13_OptimumScale = 3000.0f;
        //private const float Usage14_OptimumScale = 2000.0f;
        //private const float Usage15_OptimumScale = 1000.0f;

        //public static int SearchStartUsage(float curScale)
        //{
        //    if (curScale >= Usage5_OptimumScale && curScale < Usage1_OptimumScale) return 0;
        //    else if (curScale >= Usage6_OptimumScale && curScale < Usage5_OptimumScale) return 1;
        //    else if (curScale >= Usage8_OptimumScale && curScale < Usage6_OptimumScale) return 2;
        //    else if (curScale >= Usage9_OptimumScale && curScale < Usage8_OptimumScale) return 3;
        //    else if (curScale >= Usage13_OptimumScale && curScale < Usage9_OptimumScale) return 4;
        //    else if (curScale >= Usage15_OptimumScale && curScale < Usage13_OptimumScale) return 5;

        //    return -1;
        //}

        private const int USAGE_SCALEMIN_6 = 0;
        private const int USAGE_SCALEMAX_6 = 3999;
        private const int USAGE_SCALEMIN_5 = 4000;
        private const int USAGE_SCALEMAX_5 = 21999;
        private const int USAGE_SCALEMIN_4 = 22000;
        private const int USAGE_SCALEMAX_4 = 89999;
        private const int USAGE_SCALEMIN_3 = 90000;
        private const int USAGE_SCALEMAX_3 = 349999;
        private const int USAGE_SCALEMIN_2 = 350000;
        private const int USAGE_SCALEMAX_2 = 1499999;
        private const int USAGE_SCALEMIN_1 = 1500000;
        private const int USAGE_SCALEMAX_1 = 20000000;

        public static int SearchStartUsage(double dCurScale)
        {
            int nUsage = -1;

            if (USAGE_SCALEMIN_6 <= dCurScale && dCurScale <= USAGE_SCALEMAX_6) nUsage = 5;
            else if (USAGE_SCALEMIN_5 <= dCurScale && dCurScale <= USAGE_SCALEMAX_5) nUsage = 4;
            else if (USAGE_SCALEMIN_4 <= dCurScale && dCurScale <= USAGE_SCALEMAX_4) nUsage = 3;
            else if (USAGE_SCALEMIN_3 <= dCurScale && dCurScale <= USAGE_SCALEMAX_3) nUsage = 2;
            else if (USAGE_SCALEMIN_2 <= dCurScale && dCurScale <= USAGE_SCALEMAX_2) nUsage = 1;
            else if (USAGE_SCALEMIN_1 <= dCurScale && dCurScale <= USAGE_SCALEMAX_1) nUsage = 0;

            return nUsage;
        }

        // None Official Data에 대해서 No ENC로 표시가 나와야 한다는 문건(P8)에 의해서 적용함
        public static bool IsNonOfficialData(string chartName)
        {
            return ChartCat.IsNonOfficialData(chartName);

            //if(ChartCat.DicChartCat.TryGetValue(chartName, out var chart) == true)
            //{
            //    return string.IsNullOrEmpty(AgencyCat.GetName(chart.Agency));
            //}

            //return false;
        }

        public static bool IsNonOfficialData(int agency)
        {
            return string.IsNullOrEmpty(AgencyCat.GetName(agency));
        }

        public static bool FindLargerChart(Transform projection, int startUsage)
        {
            Clipper2 clip = new();

            // 마지막 Usage = 5까지 확인하여 화면과 겹치는 차트가 있는지 확인한다.
            for (int usage = startUsage; usage < 6; usage++)
            {
                if (ChartCatalogue.coverageTable.ContainsKey(usage) == false) continue;
                //int count = ChartCat.ListCov[usage].Count;
                int count = ChartCatalogue.coverageTable[usage].Count;
                if (count <= 0) continue;

                for (int index = 0; index < count; index++)
                {
                    //var cov = ChartCat.ListCov[usage][index];
                    var cov = ChartCatalogue.coverageTable[usage][index];
                    if (cov.IsChart1) continue;
                    // Lat / Lon Error상태이면 빠진다.
                    if (IsLatLonError(cov.Cov) == true) continue;

                    // 현재 차트의 Cover가 화면영역과 교차하는지 확인한다.
                    var pathCov = new Float2D[cov.Cov.Length];
                    projection.WGS84ToScreen(cov.Cov, pathCov);
                    if (pathCov == null || pathCov.Length <= 0) continue;

                    clip.Clear();
                    clip.AddSubject(projection.ScreenBound.ToPath());
                    clip.AddClip(pathCov);
                    if (clip.Execute(0, 0) == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsLatLonError(Float2D[] pathCov)
        {
            bool bError = false;

            // 180 / -180 / 90 / -90을 가진 차트에 대한 예외처리를 위해 추가함
            if (pathCov.Length > 0)
            {
                foreach (var item in pathCov)
                {
                    if (item.Y >= EPSG3857.MAX_LAT || item.Y <= EPSG3857.MIN_LAT)
                    {
                        bError = true;
                        break;
                    }
                }
            }

            return bError;
        }

        public static void FindIntersectionScreenToCharts(Transform projection, List<S57ChartInfo> listChart, Dictionary<int, Float2D[][]> dicScaleBoundarys, ConcurrentDictionary<string, string> dicSendVdr, ref float overscaleFactor, ref bool nonEncData, ref bool notUpToDate, ref bool overlap)
        {
            Clipper2 clip = new();
            listChart.Clear();
            dicSendVdr.Clear();
            dicScaleBoundarys.Clear();

            // 화면 영역을 구해 놓기
            List<Float2D[]> pathsTotal = new();
            pathsTotal.Add(projection.WorldBound.ToPath());

            List<Float2D[]> pathsTotal2 = new();
            if (projection.IsMultiTransform == true)
            {
                projection.SetTransform2();
                pathsTotal2.Add(projection.WorldBound.ToPath());
                projection.SetTransform1();
            }

            nonEncData = false;
            notUpToDate = false;
            overlap = false;

            // 나중에 Coverige정보를 지우기 위해서 필요한 리스트
            List<string> listChartName = new List<string>();

            overscaleFactor = float.MaxValue;

            bool fullCover = false;
            //int startUsage = SearchStartUsage((float)projection.Scale);
            int startUsage = 5;
            for (int usage = startUsage; usage >= 0; usage--)
            {
                if (ChartCatalogue.coverageTable.ContainsKey(usage) == false) continue;

                dicScaleBoundarys.TryAdd(usage, null);

                int count = ChartCatalogue.coverageTable[usage].Count;
                if (count <= 0) continue;

                for (int index = 0; index < count; index++)
                {
                    var cov = ChartCatalogue.coverageTable[usage][index];
                    // Chart1을 보겠다고 하지 않았으면 
                    if (cov.IsChart1 && S57ChartOption.UsedChart1 == false) continue;
                    // Lat / Lon Error상태이면 빠진다.
                    if (IsLatLonError(cov.Cov) == true) continue;

                    // Compilation Scale의 0.5(min) ~ 2(max)배안의 차트만 표시한다.
                    if (cov.CheckHideDisplay(projection.Scale) == true) continue;

                    // 현재 차트의 Cover가 화면영역과 교차하는지 확인한다.
                    var pathCov = new Float2D[cov.Cov.Length];
                    projection.WGS84ToScreen(cov.Cov, pathCov);
                    if (pathCov == null || pathCov.Length <= 0) continue;

                    clip.Clear();
                    clip.AddSubject(projection.ScreenBound.ToPath());
                    clip.AddClip(pathCov);
                    if (clip.Execute(0, 0) == true)
                    {
                        // World좌표로 확인한다.
                        //if (FindCoverage.ParseCoverage(cov.name, out var pointsCover) == true)
                        if (FindCoverage.ParseCoverage(projection, cov.name, out var pointsCover) == true)
                        {
                            // Step 1. 화면 영역과 현재 Cover와의 교차정보를 가져온다.
                            clip.Clear();
                            clip.AddSubject(pathsTotal);
                            clip.AddClip(pointsCover);
                            if (clip.Execute(0, 0) == true)
                            {
                                // Overlap 차트 확인
                                if (overlap == false) overlap = ChartCat.IsOverlapChart(cov.name);
                                // Not Up To Date 확인
                                if (notUpToDate == false) notUpToDate = ChartCat.IsNotUpToDate(cov.name);

                                // PL4.0.1에서 10.1.10.2에보면 "grossly overscale"이란 표현이 있는데 DNV에서는 이 의미를 2 * compilation Scale을 의미한다고 한다.
                                // 따라서 기존에는 1.5배만 되면 보여줬는데 2배가 되어야 OverScale을 표시하기로 한다.
                                var overscale = (cov.scale / projection.Scale);
                                // Overscale Indication은 1배 초과이면 무조건 Indication을 표시
                                if(overscale > 1.0 && overscaleFactor == float.MaxValue) overscaleFactor = (float)overscale;
                                // Overscale Pattern은 2배 이상이면 표시
                                bool isOverScalePattern = overscale >= 2.0 ? true : false;
                                if (isOverScalePattern == true)
                                {
                                    // 현재 확대는 되었는데 가장 대축척의 Usage이면 Overscale Pattern을 그리지 않아야 한다.
                                    // 따라서 현재 Usage보다 대축척 전자해도가 현재 화면에 존재고, 표출되지 않아도 Overscale Pattern을 그리도록 해야 한다.
                                    // 현재 화면영역 밑에 더 대축척 
                                    bool isLagerChart = FindLargerChart(projection, usage + 1);
                                    if (isLagerChart == false) isOverScalePattern = false;
                                }
                                int agency = ChartCat.GetAgency(cov.name);
                                var chart = new S57ChartInfo(cov.name, (byte)usage, isOverScalePattern, agency, (double)cov.scale, pointsCover);
                                listChart.Add(chart);

                                // VDR로 차트 정보를 보내기 위한 정보 저장
                                var chartVersion = cov.baseVersion?.EDTN.ToString() + "," + cov.updateVersion.ToString();
                                dicSendVdr.TryAdd(cov.name, chartVersion);

                                // Coverige정보를 정리하기 위해서 저장
                                listChartName.Add(cov.name);

                                // Non Enc Data에 대해서 확인한다.(이것은 화면 중심이다)
                                if (nonEncData == false) nonEncData = IsNonOfficialData(cov.name);

                                // 전체 영역에서 교차된 영역을 제거하여 전체 영역을 다시 만든다.
                                clip.Clear();
                                clip.AddSubject(pathsTotal);
                                clip.AddClip(pointsCover);
                                List<Float2D[]> pathsDIFF = new();
                                if (clip.Execute(2, 0, pathsDIFF) == true)
                                {
                                    pathsTotal.Clear();
                                    pathsTotal = pathsDIFF;
                                }
                                else
                                {
                                    fullCover = true;
                                    break;
                                }

                                // Usage별 Scale Boundary를 만들기 위해서 추가
                                if (dicScaleBoundarys.TryGetValue(usage, out var value))
                                {
                                    if (value == null) dicScaleBoundarys[usage] = pointsCover;
                                    else
                                    {
                                        // 영역을 Sum한다.
                                        clip.Clear();
                                        clip.AddSubject(value);
                                        clip.AddClip(pointsCover);
                                        if(clip.Execute(1, 0, out value)) dicScaleBoundarys[usage] = value;
                                    }
                                }
                            }

                            if (projection.IsMultiTransform == true)
                            {
                                clip.Clear();
                                clip.AddSubject(pathsTotal2);
                                clip.AddClip(pointsCover);
                                if (clip.Execute(0, 0) == true)
                                {
                                    // PL4.0.1에서 10.1.10.2에보면 "grossly overscale"이란 표현이 있는데 DNV에서는 이 의미를 2 * compilation Scale을 의미한다고 한다.
                                    // 따라서 기존에는 1.5배만 되면 보여줬는데 2배가 되어야 OverScale을 표시하기로 한다.
                                    var overscale = (cov.scale / projection.Scale);
                                    // Overscale Indication은 1배 초과이면 무조건 Indication을 표시
                                    if (overscale > 1.0 && overscaleFactor == float.MaxValue) overscaleFactor = (float)overscale;
                                    // Overscale Pattern은 2배 이상이면 표시
                                    bool isOverScalePattern = overscale >= 2.0 ? true : false;
                                    if (isOverScalePattern == true)
                                    {
                                        // 현재 확대는 되었는데 가장 대축척의 Usage이면 Overscale Pattern을 그리지 않아야 한다.
                                        // 따라서 현재 Usage보다 대축척 전자해도가 현재 화면에 존재고, 표출되지 않아도 Overscale Pattern을 그리도록 해야 한다.
                                        // 현재 화면영역 밑에 더 대축척 
                                        bool isLagerChart = FindLargerChart(projection, usage + 1);
                                        if (isLagerChart == false) isOverScalePattern = false;
                                    }
                                    int agency = ChartCat.GetAgency(cov.name);
                                    var chart = new S57ChartInfo(cov.name, (byte)usage, isOverScalePattern, agency, (double)cov.scale, pointsCover);
                                    listChart.Add(chart);

                                    // VDR로 차트 정보를 보내기 위한 정보 저장
                                    var chartVersion = cov.baseVersion?.EDTN.ToString() + "," + cov.updateVersion.ToString();
                                    dicSendVdr.TryAdd(cov.name, chartVersion);

                                    // Coverige정보를 정리하기 위해서 저장
                                    listChartName.Add(cov.name);

                                    // 전체 영역에서 교차된 영역을 제거하여 전체 영역을 다시 만든다.
                                    clip.Clear();
                                    clip.AddSubject(pathsTotal2);
                                    clip.AddClip(pointsCover);
                                    List<Float2D[]> pathsDIFF = new();
                                    if (clip.Execute(2, 0, pathsDIFF) == true)
                                    {
                                        pathsTotal2.Clear();
                                        pathsTotal2 = pathsDIFF;
                                    }

                                    // Usage별 Scale Boundary를 만들기 위해서 추가
                                    if (dicScaleBoundarys.TryGetValue(usage, out var value))
                                    {
                                        if (value == null) dicScaleBoundarys[usage] = pointsCover;
                                        else
                                        {
                                            // 영역을 Sum한다.
                                            clip.Clear();
                                            clip.AddSubject(value);
                                            clip.AddClip(pointsCover);
                                            clip.Execute(1, 0, out value);
                                            dicScaleBoundarys[usage] = value;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (fullCover == true) break;
                }

                if (fullCover == true) break;
            }

            // World Map을 사용하고, FullCover가 되지 않은 상태이면 World Map을 찾는다.
            if (S57ChartOption.UsedWorldMap == true && fullCover == false)
            {
                var pathView = projection.WorldBound.ToPath();
                List<Float2D[]> pathView2 = new();
                if (projection.IsMultiTransform == true)
                {
                    projection.SetTransform2();
                    pathView2.Add(projection.WorldBound.ToPath());
                    projection.SetTransform1();
                }

                // 화면 영역을 구해 놓기
                for (int i = 0; i < 6; i++)
                {
                    string name = "WORLDMAP" + (i + 1).ToString();
                    //if (FindCoverage.ParseCoverage(name, out var pathsCover) == true)
                    if (FindCoverage.ParseCoverage(projection, name, out var pathsCover) == true)
                    {
                        // Step 1. 화면 영역과 현재 Cover와의 교차정보를 가져온다.
                        clip.Clear();
                        clip.AddSubject(pathView);
                        clip.AddClip(pathsCover);
                        if (clip.Execute(0, 0) == true)
                        {
                            var chart = new S57ChartInfo(name, 0, false, -1, 0, null);
                            listChart.Add(chart);
                        }

                        if (projection.IsMultiTransform == true)
                        {
                            clip.Clear();
                            clip.AddSubject(pathView2);
                            clip.AddClip(pathsCover);
                            if (clip.Execute(0, 0) == true)
                            {
                                var chart = new S57ChartInfo(name, 0, false, -1, 0, null);
                                listChart.Add(chart);
                            }
                        }
                    }
                }
            }

            // Coverige 정리
            FindCoverage.RemoveCoverage(listChartName);
            listChartName.Clear();
        }

        public static void FindDetectionOwnshipPosition(Transform tf, Float2D posOwnship, ref string chartName, ref double chartScale, ref bool largerChart, ref bool noENCavailable)
        {
            var coverage = OwnshipCoverage;

            largerChart = false;
            noENCavailable = true;
            for (int usage = 5; usage >= 0; usage--)
            {
                if (ChartCatalogue.coverageTable.ContainsKey(usage) == false) continue;

                int count = ChartCatalogue.coverageTable[usage].Count;
                if (count <= 0) continue;

                for (int index = 0; index < count; index++)
                {
                    var cov = ChartCatalogue.coverageTable[usage][index];
                    // Chart1은 배제 
                    if (cov.IsChart1) continue;
                    // Lat / Lon Error상태이면 빠진다.
                    if (IsLatLonError(cov.Cov) == true) continue;
                    // cov의 Bound체크를 해서 Bound 밖이면 건너뛴다.
                    if (cov.IsWorldBoundInPosition(posOwnship) == false) continue;

                    if (coverage.ParseCoverage(cov.name, out var pointsCover) == true)
                    {
                        // 대축척 차트가 있음을 확인
                        if (largerChart == false) largerChart = cov.CheckHideDisplay(tf.Scale);

                        // No ENC Available 검사 및 VoyageLog에 전달할 차트명, 차트스케일 저장
                        if (noENCavailable == true)
                        {
                            foreach (var pathCov in pointsCover)
                            {
                                if (GeometryHelper.PointInPolygonWinding(posOwnship, pathCov) == true)
                                {
                                    chartName = cov.name;
                                    chartScale = (double)cov.scale;
                                    noENCavailable = false;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        // 외부에서 넘어온 영역과 교차하는 차트의 Detection정보를 넘겨주는 함수 (btType : 0 = Ownship, 1 = Route)
        public static void FindDetectionInfo(Transform tf, byte type, Float2D[] pathSource, List<DetectionOutput> listDetectionOutput, Float2D posOwnship, ref string chartName, ref double chartScale, ref bool largerChart, ref bool noENCavailable)
        {
            Clipper2 clip = new();

            // 화면 영역을 구해 놓기
            List<Float2D[]> pathsTotal = new();
            pathsTotal.Add(pathSource);

            // 나중에 Coverige정보를 지우기 위해서 필요한 리스트
            List<string> listChartName = new List<string>();

            Detection detection = null;
            Coverage coverage = null;
            if (type == 0)
            {
                coverage = OwnshipCoverage;
                detection = OwnshipDetection;
            }
            else if (type == 1)
            {
                coverage = RouteCoverage;
                detection = RouteDetection;
            }
            else return;

            largerChart = false;
            noENCavailable = true;
            bool isFullCover = false;
            for (int usage = 5; usage >= 0; usage--)
            {
                if (ChartCatalogue.coverageTable.ContainsKey(usage) == false) continue;

                int count = ChartCatalogue.coverageTable[usage].Count;
                if (count <= 0) continue;

                for (int index = 0; index < count; index++)
                {
                    var cov = ChartCatalogue.coverageTable[usage][index];
                    // Chart1은 배제 
                    if (cov.IsChart1) continue;
                    // Lat / Lon Error상태이면 빠진다.
                    if (IsLatLonError(cov.Cov) == true) continue;

                    // 디텍션 영역과 현재 차트가 교차하는지 확인한다.
                    if (GeometryHelper.PathIntersect(pathSource, cov.minX, cov.minY, cov.maxX, cov.maxY) == false) continue;

                    if (coverage.ParseCoverage(cov.name, out var pointsCover) == true)
                    {
                        // 대축척 차트가 있음을 확인
                        if(largerChart == false) largerChart = cov.CheckHideDisplay(tf.Scale);

                        // No ENC Available 검사 및 VoyageLog에 전달할 차트명, 차트스케일 저장
                        if (noENCavailable == true)
                        {
                            foreach (var pathCov in pointsCover)
                            {
                                if (GeometryHelper.PointInPolygonWinding(posOwnship, pathCov) == true)
                                {
                                    chartName = cov.name;
                                    chartScale = (double)cov.scale;
                                    noENCavailable = false;
                                    break;
                                }
                            }
                        }

                        // Step 1. 외부 입력 영역과 현재 Cover와의 교차정보를 가져온다.
                        clip.Clear();
                        clip.AddSubject(pathsTotal);
                        clip.AddClip(pointsCover);
                        var pathsIn = new List<Float2D[]>();
                        if (clip.Execute(0, 0, pathsIn) == true)
                        {
                            // 차트명 저장
                            listChartName.Add(cov.name);

                            // 실제 검사 시작
                            GetDetectionObject(cov.name, pathsIn, detection, listDetectionOutput);

                            // Step 2. 전체 영역에서 교차된 영역을 제거하여 전체 영역을 다시 만든다.
                            clip.Clear();
                            clip.AddSubject(pathsTotal);
                            clip.AddClip(pointsCover);
                            var pathsDIFF = new List<Float2D[]>();
                            if (clip.Execute(2, 0, pathsDIFF) == true)
                            {
                                pathsTotal.Clear();
                                pathsTotal = pathsDIFF;
                            }
                            else
                            {
                                isFullCover = true;
                                break;
                            }
                        }
                    }
                }

                if (isFullCover == true) break;
            }

            detection.RemoveDetect(listChartName);
            coverage.RemoveCoverage(listChartName);
            listChartName.Clear();
        }

        public static void FindRouteDetectionInfo(Float2D[] pathSource, List<DetectionOutput> listDetectionOutput)
        {
            Clipper2 clip = new();

            // 화면 영역을 구해 놓기
            List<Float2D[]> pathsTotal = new();
            pathsTotal.Add(pathSource);

            // 나중에 Coverige정보를 지우기 위해서 필요한 리스트
            List<string> listChartName = new List<string>();

            Detection detection = RouteDetection;
            Coverage coverage = RouteCoverage;
            ManualUpdateManager manual = ManualUpdate;

            bool isFullCover = false;
            for (int usage = 5; usage >= 0; usage--)
            {
                if (ChartCatalogue.coverageTable.ContainsKey(usage) == false) continue;

                int count = ChartCatalogue.coverageTable[usage].Count;
                if (count <= 0) continue;

                for (int index = 0; index < count; index++)
                {
                    var cov = ChartCatalogue.coverageTable[usage][index];
                    // Chart1은 배제 
                    if (cov.IsChart1) continue;
                    // Lat / Lon Error상태이면 빠진다.
                    if (IsLatLonError(cov.Cov) == true) continue;

                    // 디텍션 영역과 현재 차트가 교차하는지 확인한다.
                    float x1, x2, y1, y2;
                    if (cov.Cov[0].X < cov.Cov[2].X)
                    {
                        x1 = cov.Cov[0].X;
                        x2 = cov.Cov[2].X;
                    }
                    else
                    {
                        x2 = cov.Cov[0].X;
                        x1 = cov.Cov[2].X;
                    }

                    if (cov.Cov[0].Y < cov.Cov[2].Y)
                    {
                        y1 = cov.Cov[0].Y;
                        y2 = cov.Cov[2].Y;
                    }
                    else
                    {
                        y2 = cov.Cov[0].Y;
                        y1 = cov.Cov[2].Y;
                    }

                    var xy1 = EPSG3857.ToWorld(x1, y1);
                    var xy2 = EPSG3857.ToWorld(x2, y2);
                    if (GeometryHelper.PathIntersect(pathSource, xy1.X, xy1.Y, xy2.X, xy2.Y) == false) continue;

                    if (coverage.ParseCoverage(cov.name, out var pointsCover) == true)
                    {
                        // Step 1. 외부 입력 영역과 현재 Cover와의 교차정보를 가져온다.
                        clip.Clear();
                        clip.AddSubject(pathsTotal);
                        clip.AddClip(pointsCover);
                        var pathsIn = new List<Float2D[]>();
                        if (clip.Execute(0, 0, pathsIn) == true)
                        {
                            // 차트명 저장
                            listChartName.Add(cov.name);

                            // 실제 검사 시작
                            GetDetectionObject(cov.name, pathsIn, detection, listDetectionOutput);
                            // Manual Update 검사
                            GetDetectionManualUpdateObject(cov.name, pathsIn, manual, listDetectionOutput);

                            // Step 2. 전체 영역에서 교차된 영역을 제거하여 전체 영역을 다시 만든다.
                            clip.Clear();
                            clip.AddSubject(pathsTotal);
                            clip.AddClip(pointsCover);
                            var pathsDIFF = new List<Float2D[]>();
                            if (clip.Execute(2, 0, pathsDIFF) == true)
                            {
                                pathsTotal.Clear();
                                pathsTotal = pathsDIFF;
                            }
                            else
                            {
                                isFullCover = true;
                                break;
                            }
                        }
                    }
                }

                if (isFullCover == true) break;
            }

            detection.RemoveDetect(listChartName);
            coverage.RemoveCoverage(listChartName);
            listChartName.Clear();
        }

        static void GetDetectionObject(string chartName, List<Float2D[]> pathsClip, Detection detection, List<DetectionOutput> listDetectionOutput)
        {
            if (detection.ParseDetect(chartName, out var detectionInfo) == false) return;

            // Safety Depth검색
            FindDetectionSafetyDepth(chartName, pathsClip, detectionInfo, listDetectionOutput);
            // Safety 검색
            FindDetectionSafety(chartName, pathsClip, detectionInfo, listDetectionOutput);
            // Special 검색
            FindDetectionSpecial(chartName, pathsClip, detectionInfo, listDetectionOutput);
            // Hazard Depth 검색
            FindDetectionHazardDepth(chartName, pathsClip, detectionInfo, listDetectionOutput);
            // Hazard Sound 검색
            FindDetectionHazardSound(chartName, pathsClip, detectionInfo, listDetectionOutput);
            // Hazard 검색
            FindDetectionHazard(chartName, pathsClip, detectionInfo, listDetectionOutput);
        }
        
        static void GetDetectionManualUpdateObject(string chartName, List<Float2D[]> pathsClip, ManualUpdateManager manual, List<DetectionOutput> listDetectionOutput)
        {
            if (manual.LoadNewManualUpdate(chartName, out var listManualUpdate) == false) return;

            Clipper2 clip = new();
            foreach (var mu in listManualUpdate)
            {
                if (mu.IsDelete == true) continue;

                short OBJL = ObjectCat.GetObjectID(mu.ObjectClass);
                if (OBJL == 0) continue;

                switch (mu.GeoType)
                {
                    case S57ManualUpdate.EnumGeoType.Point:
                        {
                            foreach (var path in pathsClip)
                            {
                                var wxy = EPSG3857.ToWorld(mu.PointObj.Pivot);
                                if (GeometryHelper.PointInPolygonWinding(wxy, path) == true)
                                {
                                    DetectionOutput output = new DetectionOutput();
                                    output.Type = 2;
                                    output.RCID = -1;
                                    output.OBJL = OBJL;
                                    output.PRIM = 1;
                                    output.Point = wxy;
                                    output.ChartName = chartName;
                                    listDetectionOutput.Add(output);
                                    break;
                                }
                            }
                        }
                        break;

                    case S57ManualUpdate.EnumGeoType.Area:
                        {
                            var points = new Float2D[mu.AreaObj.Points.Count];
                            for (int i = 0; i < points.Length; i++) points[i] = EPSG3857.ToWorld(mu.AreaObj.Points[i]);
                            
                            foreach (var path in pathsClip)
                            {
                                clip.Clear();
                                clip.AddSubject(pathsClip);
                                clip.AddClip(points);
                                var pathsINTER = new List<Float2D[]>();
                                if (clip.Execute(0, 0, pathsINTER) == true)
                                {
                                    DetectionOutput output = new DetectionOutput();
                                    output.Type = 1;
                                    output.RCID = -1;
                                    output.OBJL = OBJL;
                                    output.RESARE = 1;
                                    output.PRIM = 3;
                                    output.PathsInfo = pathsINTER.ToArray();
                                    output.ChartName = chartName;
                                    listDetectionOutput.Add(output);
                                }
                            }
                        }
                        break;
                }
            }
        }

        static bool FindDetectionSafetyDepth(string chartName, List<Float2D[]> pathsClip, DetectionInfo detectionInfo, List<DetectionOutput> listDetectionOutput)
        {
            bool bRtn = false;

            Clipper2 clip = new();

            var safetyContour = S57ChartSafetyValue.SafetyContour;
            foreach (var depth in detectionInfo.ListSafetyDepth)
            {
                // UNSAFE가 TRUE인 조건을 찾는다.
                if (depth.DRVAL1 >= safetyContour) continue;

                clip.Clear();
                clip.AddSubject(pathsClip);
                clip.AddClip(depth.Points.PathsShape);
                var pathsINTER = new List<Float2D[]>();
                if (clip.Execute(0, 0, pathsINTER) == true)
                {
                    DetectionOutput output = new DetectionOutput();
                    output.Type = 0;
                    output.RCID = depth.RCID;
                    output.OBJL = depth.OBJL;
                    output.PRIM = depth.PRIM;
                    output.PathsInfo = pathsINTER.ToArray();
                    output.ChartName = chartName;
                    listDetectionOutput.Add(output);

                    bRtn = true;
                }
            }

            return bRtn;
        }

        static bool FindDetectionSafety(string chartName, List<Float2D[]> pathsClip, DetectionInfo detectionInfo, List<DetectionOutput> listDetectionOutput)
        {
            bool bRtn = false;

            Clipper2 clip = new();

            foreach (var safety in detectionInfo.ListSafety)
            {
                if (safety.PRIM == 1)
                {
                    foreach (var pt in safety.Points.PathPT)
                    {
                        foreach (var path in pathsClip)
                        {
                            if (GeometryHelper.PointInPolygonWinding(pt, path) == true)
                            {
                                DetectionOutput output = new DetectionOutput();
                                output.Type = 0;
                                output.RCID = safety.RCID;
                                output.OBJL = safety.OBJL;
                                output.PRIM = safety.PRIM;
                                output.Point = pt;
                                output.ChartName = chartName;
                                listDetectionOutput.Add(output);
                                bRtn = true;
                                break;
                            }
                        }
                    }
                }
                else if (safety.PRIM == 2)
                {
                    // 라인 클리핑은 Clipper2에서는 지원을 안함
                    // 그래서 예전것을 사용하자
                    clip.Clear();
                    clip.AddSubject(safety.Points.PathPT, true);
                    clip.AddClip(pathsClip);
                    var pathsINTER = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINTER) == true)
                    {
                        DetectionOutput output = new DetectionOutput();
                        output.Type = 0;
                        output.RCID = safety.RCID;
                        output.OBJL = safety.OBJL;
                        output.PRIM = safety.PRIM;
                        output.PathsInfo = pathsINTER.ToArray();
                        output.ChartName = chartName;
                        listDetectionOutput.Add(output);
                        bRtn = true;
                    }
                }
                else if (safety.PRIM == 3)
                {
                    clip.Clear();
                    clip.AddSubject(pathsClip);
                    clip.AddClip(safety.Points.PathsShape);
                    var pathsINTER = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINTER) == true)
                    {
                        DetectionOutput output = new DetectionOutput();
                        output.Type = 0;
                        output.RCID = safety.RCID;
                        output.OBJL = safety.OBJL;
                        output.PRIM = safety.PRIM;
                        output.PathsInfo = pathsINTER.ToArray();
                        output.ChartName = chartName;
                        listDetectionOutput.Add(output);
                        bRtn = true;
                    }
                }
            }

            return bRtn;
        }

        static bool FindDetectionSpecial(string chartName, List<Float2D[]> pathsClip, DetectionInfo detectionInfo, List<DetectionOutput> listDetectionOutput)
        {
            bool bRtn = false;

            Clipper2 clip = new();

            foreach (var special in detectionInfo.ListSpecial)
            {
                if (special.PRIM == 1)
                {
                    foreach (var pt in special.Points.PathPT)
                    {
                        foreach (var path in pathsClip)
                        {
                            if (GeometryHelper.PointInPolygonWinding(pt, path) == true)
                            {
                                DetectionOutput output = new DetectionOutput();
                                output.Type = 1;
                                output.RCID = special.RCID;
                                output.OBJL = special.OBJL;
                                output.PRIM = special.PRIM;
                                output.RESARE = special.RESARE;
                                output.Point = pt;
                                output.ChartName = chartName;
                                listDetectionOutput.Add(output);
                                bRtn = true;
                                break;
                            }
                        }
                    }
                }
                else if (special.PRIM == 2)
                {
                    // 라인 클리핑은 Clipper2에서는 지원을 안함
                    // 그래서 예전것을 사용하자
                    clip.Clear();
                    clip.AddSubject(special.Points.PathPT, true);
                    clip.AddClip(pathsClip);
                    var pathsINTER = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINTER) == true)
                    {
                        DetectionOutput output = new DetectionOutput();
                        output.Type = 1;
                        output.RCID = special.RCID;
                        output.OBJL = special.OBJL;
                        output.PRIM = special.PRIM;
                        output.RESARE = special.RESARE;
                        output.PathsInfo = pathsINTER.ToArray();
                        output.ChartName = chartName;
                        listDetectionOutput.Add(output);
                        bRtn = true;
                    }
                }
                else if (special.PRIM == 3)
                {
                    clip.Clear();
                    clip.AddSubject(pathsClip);
                    clip.AddClip(special.Points.PathsShape);
                    var pathsINTER = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINTER) == true)
                    {
                        DetectionOutput output = new DetectionOutput();
                        output.Type = 1;
                        output.RCID = special.RCID;
                        output.OBJL = special.OBJL;
                        output.PRIM = special.PRIM;
                        output.RESARE = special.RESARE;
                        output.PathsInfo = pathsINTER.ToArray();
                        output.ChartName = chartName;
                        listDetectionOutput.Add(output);
                        bRtn = true;
                    }
                }
            }

            return bRtn;
        }

        static bool FindDetectionHazardDepth(string chartName, List<Float2D[]> pathsClip, DetectionInfo detectionInfo, List<DetectionOutput> listDetectionOutput)
        {
            bool bRtn = false;

            Clipper2 clip = new();

            var safetyContour = S57ChartSafetyValue.SafetyContour;

            foreach (var hazard in detectionInfo.ListHazardDepth)
            {
                if (hazard.DEPTH_VALUE > safetyContour) continue;

                if (hazard.PRIM == 1)
                {
                    foreach (var pt in hazard.Points.PathPT)
                    {
                        foreach (var path in pathsClip)
                        {
                            if (GeometryHelper.PointInPolygonWinding(pt, path) == true)
                            {
                                DetectionOutput output = new DetectionOutput();
                                output.Type = 2;
                                output.RCID = hazard.RCID;
                                output.OBJL = hazard.OBJL;
                                output.PRIM = hazard.PRIM;
                                output.Point = pt;
                                output.ChartName = chartName;
                                listDetectionOutput.Add(output);
                                bRtn = true;
                                break;
                            }
                        }
                    }
                }
                else if (hazard.PRIM == 2)
                {
                    // 라인 클리핑은 Clipper2에서는 지원을 안함
                    // 그래서 예전것을 사용하자
                    clip.Clear();
                    clip.AddSubject(hazard.Points.PathPT, true);
                    clip.AddClip(pathsClip);
                    var pathsINTER = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINTER) == true)
                    {
                        DetectionOutput output = new DetectionOutput();
                        output.Type = 2;
                        output.RCID = hazard.RCID;
                        output.OBJL = hazard.OBJL;
                        output.PRIM = hazard.PRIM;
                        output.PathsInfo = pathsINTER.ToArray();
                        output.ChartName = chartName;
                        listDetectionOutput.Add(output);
                        bRtn = true;
                    }
                }
                else if (hazard.PRIM == 3)
                {
                    clip.Clear();
                    clip.AddSubject(pathsClip);
                    clip.AddClip(hazard.Points.PathsShape);
                    var pathsINTER = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINTER) == true)
                    {
                        DetectionOutput output = new DetectionOutput();
                        output.Type = 2;
                        output.RCID = hazard.RCID;
                        output.OBJL = hazard.OBJL;
                        output.PRIM = hazard.PRIM;
                        output.PathsInfo = pathsINTER.ToArray();
                        output.ChartName = chartName;
                        listDetectionOutput.Add(output);
                        bRtn = true;
                    }
                }
            }

            return bRtn;
        }

        static bool FindDetectionHazardSound(string chartName, List<Float2D[]> pathsClip, DetectionInfo detectionInfo, List<DetectionOutput> listDetectionOutput)
        {
            bool bRtn = false;

            var safetyContour = S57ChartSafetyValue.SafetyContour;
            foreach (var hazard in detectionInfo.ListHazardSound)
            {
                foreach (var soundg in hazard.ArrSoundg)
                {
                    if (soundg.Sound > safetyContour) continue;

                    var pt = EPSG3857.ToWorld(soundg.X / ScaleFactor, soundg.Y / ScaleFactor);

                    foreach (var path in pathsClip)
                    {
                        if (GeometryHelper.PointInPolygonWinding(pt, path) == true)
                        {
                            DetectionOutput output = new DetectionOutput();
                            output.Type = 2;
                            output.RCID = hazard.RCID;
                            output.OBJL = hazard.OBJL;
                            output.PRIM = 1;
                            output.Point = pt;
                            output.ChartName = chartName;
                            listDetectionOutput.Add(output);
                            bRtn = true;
                            break;
                        }
                    }
                }
            }

            return bRtn;
        }

        static bool FindDetectionHazard(string chartName, List<Float2D[]> pathsClip, DetectionInfo detectionInfo, List<DetectionOutput> listDetectionOutput)
        {
            bool bRtn = false;

            Clipper2 clip = new();

            foreach (var hazard in detectionInfo.ListHazard)
            {
                if (hazard.PRIM == 1)
                {
                    foreach (var pt in hazard.Points.PathPT)
                    {
                        foreach (var path in pathsClip)
                        {
                            if (GeometryHelper.PointInPolygonWinding(pt, path) == true)
                            {
                                var output = new DetectionOutput();
                                output.Type = 2;
                                output.RCID = hazard.RCID;
                                output.OBJL = hazard.OBJL;
                                output.PRIM = hazard.PRIM;
                                output.Point = pt;
                                output.ChartName = chartName;
                                listDetectionOutput.Add(output);
                                bRtn = true;
                                break;
                            }
                        }
                    }
                }
                else if (hazard.PRIM == 2)
                {
                    // 라인 클리핑은 Clipper2에서는 지원을 안함
                    // 그래서 예전것을 사용하자
                    clip.Clear();
                    clip.AddSubject(hazard.Points.PathPT, true);
                    clip.AddClip(pathsClip);
                    var pathsINTER = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINTER) == true)
                    {
                        DetectionOutput output = new DetectionOutput();
                        output.Type = 2;
                        output.RCID = hazard.RCID;
                        output.OBJL = hazard.OBJL;
                        output.PRIM = hazard.PRIM;
                        output.PathsInfo = pathsINTER.ToArray();
                        output.ChartName = chartName;
                        listDetectionOutput.Add(output);
                        bRtn = true;
                    }
                }
                else if (hazard.PRIM == 3)
                {
                    clip.Clear();
                    clip.AddSubject(pathsClip);
                    clip.AddClip(hazard.Points.PathsShape);
                    var pathsINTER = new List<Float2D[]>();
                    if (clip.Execute(0, 0, pathsINTER) == true)
                    {
                        DetectionOutput output = new DetectionOutput();
                        output.Type = 2;
                        output.RCID = hazard.RCID;
                        output.OBJL = hazard.OBJL;
                        output.PRIM = hazard.PRIM;
                        output.PathsInfo = pathsINTER.ToArray();
                        output.ChartName = chartName;
                        listDetectionOutput.Add(output);
                        bRtn = true;
                    }
                }
            }

            return bRtn;
        }

    }
}
