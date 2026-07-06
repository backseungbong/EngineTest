using JHLib.ChartManager.Chart.Detection;
using JHLib.ChartManager.Chart.Search;
using JHLib.ChartManager.Configuration;
using JHLib.ChartManager.Record;
using Newtonsoft.Json;
using System.IO;

namespace JHLib.ChartManager.Catalogue
{
    public static class ChartCatalogue
    {
        public static Dictionary<string, ChartRecord> catalogue { get; private set; } = new Dictionary<string, ChartRecord>();
        public static Dictionary<int, List<ChartCoverageRecord>> coverageTable { get; private set; } = new Dictionary<int, List<ChartCoverageRecord>>();

        public static bool loaded { get; private set; } = false;
        public static bool overlapped { get; private set; } = false;

        public static string fileName = "chart.cat";

        public static void Read()
        {
            FileInfo catalogueFile = new FileInfo(Path.Combine(DirectoryConfiguration.catalogue, ChartCatalogue.fileName));

            if (catalogueFile.Exists)
            {
                ChartCatalogue.Read(catalogueFile.FullName);
            }
            else
            {
                ChartCatalogue.loaded = false;
                ChartCatalogue.overlapped = false;

                ChartCatalogue.catalogue.Clear();
                ChartCatalogue.coverageTable.Clear();

                ChartCatalogue.loaded = true;
            }
        }

        public static void Read(string filePath)
        {
            ChartCatalogue.loaded = false;
            ChartCatalogue.overlapped = false;

            ChartCatalogue.catalogue.Clear();
            ChartCatalogue.coverageTable.Clear();

            using (StreamReader reader = new StreamReader(filePath))
            {
                ChartCatalogue.Read(reader);
            }

            ChartCatalogue.loaded = true;
        }

        public static void Read(Stream fileStream)
        {
            ChartCatalogue.loaded = false;
            ChartCatalogue.overlapped = false;

            ChartCatalogue.catalogue.Clear();
            ChartCatalogue.coverageTable.Clear();

            using (StreamReader reader = new StreamReader(fileStream))
            {
                ChartCatalogue.Read(reader);
            }

            ChartCatalogue.loaded = true;
        }

        private static void Read(StreamReader reader)
        {
            string? readLine = null;

            while ((readLine = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(readLine))
                {
                    ChartRecord? chartRecord;

                    try
                    {
                        chartRecord = JsonConvert.DeserializeObject<ChartRecord>(readLine);

                        //if ((chartRecord?.lifecycle == ChartRecord.Lifecycle.UpToDate) &&
                        //    DateTime.TryParseExact(
                        //        chartRecord.issueDate,
                        //        "yyyyMMdd",
                        //        CultureInfo.InvariantCulture,
                        //        DateTimeStyles.AssumeUniversal,
                        //        out DateTime issueDate))
                        //{
                        //    DateTime reference = issueDate.ToUniversalTime();
                        //    DateTime now = DateTime.UtcNow;

                        //    if ((now - reference) > TimeSpan.FromDays(28))
                        //    {
                        //        chartRecord.lifecycle = ChartRecord.Lifecycle.Outdated;
                        //    }
                        //}
                    }
                    catch (Exception e)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(chartRecord?.name))
                    {
                        ChartCatalogue.catalogue.TryAdd(chartRecord.name, chartRecord);

                        if ((0 < chartRecord.usage) && (chartRecord.usage < 7))
                        {
                            ChartCoverageRecord chartCoverageRecord = new ChartCoverageRecord(chartRecord.name) {
                                usage = chartRecord.usage,
                                scale = chartRecord.scale,
                                baseVersion = chartRecord.baseVersion,
                                updateVersion = chartRecord.updateVersion,
                                issueDate = chartRecord.issueDate,
                                IsChart1 = chartRecord.IsChart1
                            };

                            if (chartRecord.boundary != null)
                            {
                                chartCoverageRecord.coveragePath = new ChartRecord.Position[] {
                                    new ChartRecord.Position(
                                        chartRecord.boundary.west,
                                        chartRecord.boundary.north
                                    ),
                                    new ChartRecord.Position(
                                        chartRecord.boundary.east,
                                        chartRecord.boundary.north
                                    ),
                                    new ChartRecord.Position(
                                        chartRecord.boundary.east,
                                        chartRecord.boundary.south
                                    ),
                                    new ChartRecord.Position(
                                        chartRecord.boundary.west,
                                        chartRecord.boundary.south
                                    ),
                                };

                                chartCoverageRecord.CoverageConverter();

                                if (!ChartCatalogue.coverageTable.ContainsKey(chartCoverageRecord.usage.Value)) { ChartCatalogue.coverageTable.Add(chartCoverageRecord.usage.Value, new List<ChartCoverageRecord>()); }

                                ChartCatalogue.coverageTable[chartCoverageRecord.usage.Value].Add(chartCoverageRecord);
                            }
                        }

                        if (chartRecord.overlapped) { ChartCatalogue.overlapped = true; }
                    }
                }
            }
        }

        public static bool Add(DetectionChart chart, bool saving = true)
        {
            return false;
        }

        public static bool Add(ChartRecord chartRecord, bool saving = true)
        {
            if (!ChartCatalogue.overlapped &&
                (chartRecord.usage > 0) &&
                (chartRecord.boundary != null) &&
                ChartCatalogue.coverageTable.TryGetValue(chartRecord.usage.Value, out List<ChartCoverageRecord>? chartCoverageTable))
            {
                Util.Geometry.Clipper2.Clipper2 clipper = new Util.Geometry.Clipper2.Clipper2();

                Util.Struct.Float2D[] chartBoundaryArea = new Util.Struct.Float2D[4];
                Util.Struct.Float2D[] chartCoverageArea = new Util.Struct.Float2D[4];

                chartBoundaryArea[0].X = chartRecord.boundary.west;
                chartBoundaryArea[0].Y = chartRecord.boundary.north;
                chartBoundaryArea[1].X = chartRecord.boundary.east;
                chartBoundaryArea[1].Y = chartRecord.boundary.north;
                chartBoundaryArea[2].X = chartRecord.boundary.east;
                chartBoundaryArea[2].Y = chartRecord.boundary.south;
                chartBoundaryArea[3].X = chartRecord.boundary.west;
                chartBoundaryArea[3].Y = chartRecord.boundary.south;

                foreach (ChartCoverageRecord chartCoverageRecord in chartCoverageTable)
                {
                    if (chartCoverageRecord.IsChart1) { continue; }
                    if (chartCoverageRecord.name == chartRecord.name) { continue; }

                    if (chartCoverageRecord.coveragePath != null)
                    {
                        chartCoverageArea[0].X = chartCoverageRecord.coveragePath[0].x;
                        chartCoverageArea[0].Y = chartCoverageRecord.coveragePath[0].y;
                        chartCoverageArea[1].X = chartCoverageRecord.coveragePath[1].x;
                        chartCoverageArea[1].Y = chartCoverageRecord.coveragePath[1].y;
                        chartCoverageArea[2].X = chartCoverageRecord.coveragePath[2].x;
                        chartCoverageArea[2].Y = chartCoverageRecord.coveragePath[2].y;
                        chartCoverageArea[3].X = chartCoverageRecord.coveragePath[3].x;
                        chartCoverageArea[3].Y = chartCoverageRecord.coveragePath[3].y;

                        clipper.Clear();
                        clipper.AddSubject(chartBoundaryArea);
                        clipper.AddClip(chartCoverageArea);

                        if (clipper.Execute(0, 0))
                        {
                            chartRecord.overlapped = true;

                            if (ChartCatalogue.catalogue.TryGetValue(chartCoverageRecord.name, out ChartRecord? referenceRecord))
                            {
                                referenceRecord.overlapped = true;
                            }
                        }
                    }
                }
            }

            if (ChartCatalogue.catalogue.ContainsKey(chartRecord.name))
            {
                ChartCatalogue.catalogue.Remove(chartRecord.name);
            }

            ChartCatalogue.catalogue.Add(chartRecord.name, chartRecord);

            if (saving) { ChartCatalogue.Save(); }

            return true;
        }

        public static bool Delete(ChartRecord chartRecord, bool saving = true)
        {
            return ChartCatalogue.Delete(chartRecord.name);
        }

        public static bool Delete(string chartName, bool saving = true)
        {
            if (ChartCatalogue.catalogue.ContainsKey(chartName))
            {
                int? usage = ChartCatalogue.catalogue[chartName].usage;

                if ((usage > 0) && ChartCatalogue.coverageTable.ContainsKey(usage.Value))
                {
                    List<ChartCoverageRecord> chartCoverageTable = ChartCatalogue.coverageTable[usage.Value];
                    List<int> removeList = new List<int>();

                    Util.Geometry.Clipper2.Clipper2 clipper = new Util.Geometry.Clipper2.Clipper2();
                    Util.Struct.Float2D[] chartBoundaryArea = new Util.Struct.Float2D[4];
                    Util.Struct.Float2D[] chartCoverageArea = new Util.Struct.Float2D[4];

                    for (int i = 0; i < (chartCoverageTable.Count - 1); i++)
                    {
                        ChartCoverageRecord chartCoverageRecord = chartCoverageTable[i];

                        if (chartCoverageRecord.name == chartName)
                        {
                            removeList.Add(i);
                        }
                        else
                        {
                            if ((chartCoverageRecord.coveragePath != null) &&
                                ChartCatalogue.catalogue.TryGetValue(chartCoverageRecord.name, out ChartRecord? chartRecord))
                            {
                                chartRecord.overlapped = false;

                                chartBoundaryArea[0].X = chartCoverageRecord.coveragePath[0].x;
                                chartBoundaryArea[0].Y = chartCoverageRecord.coveragePath[0].y;
                                chartBoundaryArea[1].X = chartCoverageRecord.coveragePath[1].x;
                                chartBoundaryArea[1].Y = chartCoverageRecord.coveragePath[1].y;
                                chartBoundaryArea[2].X = chartCoverageRecord.coveragePath[2].x;
                                chartBoundaryArea[2].Y = chartCoverageRecord.coveragePath[2].y;
                                chartBoundaryArea[3].X = chartCoverageRecord.coveragePath[3].x;
                                chartBoundaryArea[3].Y = chartCoverageRecord.coveragePath[3].y;

                                int checkOffset = i + 1;

                                for (int j = checkOffset; j < chartCoverageTable.Count; j++)
                                {
                                    ChartCoverageRecord referenceCoverageRecord = chartCoverageTable[j];

                                    if (referenceCoverageRecord.name == chartName) { continue; }
                                    if (referenceCoverageRecord.IsChart1) { continue; }

                                    if (referenceCoverageRecord.coveragePath != null)
                                    {
                                        chartCoverageArea[0].X = referenceCoverageRecord.coveragePath[0].x;
                                        chartCoverageArea[0].Y = referenceCoverageRecord.coveragePath[0].y;
                                        chartCoverageArea[1].X = referenceCoverageRecord.coveragePath[1].x;
                                        chartCoverageArea[1].Y = referenceCoverageRecord.coveragePath[1].y;
                                        chartCoverageArea[2].X = referenceCoverageRecord.coveragePath[2].x;
                                        chartCoverageArea[2].Y = referenceCoverageRecord.coveragePath[2].y;
                                        chartCoverageArea[3].X = referenceCoverageRecord.coveragePath[3].x;
                                        chartCoverageArea[3].Y = referenceCoverageRecord.coveragePath[3].y;

                                        clipper.Clear();
                                        clipper.AddSubject(chartBoundaryArea);
                                        clipper.AddClip(chartCoverageArea);

                                        if (clipper.Execute(0, 0))
                                        {
                                            chartRecord.overlapped = true;

                                            if (ChartCatalogue.catalogue.TryGetValue(referenceCoverageRecord.name, out ChartRecord? referenceRecord))
                                            {
                                                referenceRecord.overlapped = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    removeList.Reverse();
                    removeList.ForEach(chartCoverageTable.RemoveAt);
                }

                ChartCatalogue.catalogue.Remove(chartName);

                if (saving) { ChartCatalogue.Save(); }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void Save()
        {
            FileInfo catalogueFile = new FileInfo(Path.Combine(DirectoryConfiguration.catalogue, ChartCatalogue.fileName));

            if (catalogueFile.Directory?.Exists == false) { catalogueFile.Directory.Create(); }

            ChartCatalogue.Save(catalogueFile.FullName);
        }

        public static void Save(string filePath)
        {
            List<ChartRecord> krList = new List<ChartRecord>();
            List<ChartRecord> genericList = new List<ChartRecord>();

            foreach (KeyValuePair<string, ChartRecord> catalogueItem in ChartCatalogue.catalogue)
            {
                if (catalogueItem.Key.ToUpper().StartsWith("KR"))
                {
                    krList.Add(catalogueItem.Value);
                }
                else
                {
                    genericList.Add(catalogueItem.Value);
                }
            }

            ChartCatalogue.loaded = false;
            ChartCatalogue.overlapped = false;

            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                foreach (ChartRecord chartRecord in krList)
                {
                    ChartCatalogue.Write(writer, chartRecord);
                }

                foreach (ChartRecord chartRecord in genericList)
                {
                    ChartCatalogue.Write(writer, chartRecord);
                }
            }

            ChartCatalogue.loaded = true;
        }

        public static void Save(Stream fileStream)
        {
            List<ChartRecord> krList = new List<ChartRecord>();
            List<ChartRecord> genericList = new List<ChartRecord>();

            foreach (KeyValuePair<string, ChartRecord> chartRecord in ChartCatalogue.catalogue)
            {
                if (chartRecord.Key.ToUpper().StartsWith("KR"))
                {
                    krList.Add(chartRecord.Value);
                }
                else
                {
                    genericList.Add(chartRecord.Value);
                }
            }

            ChartCatalogue.loaded = false;
            ChartCatalogue.overlapped = false;

            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                foreach (ChartRecord chartRecord in krList)
                {
                    ChartCatalogue.Write(writer, chartRecord);
                }

                foreach (ChartRecord chartRecord in genericList)
                {
                    ChartCatalogue.Write(writer, chartRecord);
                }
            }

            ChartCatalogue.loaded = true;
        }

        private static void Write(StreamWriter writer, ChartRecord chartRecord)
        {
            writer.WriteLine(JsonConvert.SerializeObject(chartRecord));
            writer.Flush();
        }


        public static bool ValidateCellNecessary(string chartName, SearchCell searchCell)
        {
            bool necessary;

            if (!ChartCatalogue.loaded ||
                !ChartCatalogue.catalogue.TryGetValue(chartName, out ChartRecord? chartRecord) ||
                (chartRecord.baseVersion == null))
            {
                necessary = true;
            }
            else if (searchCell.EDTN != chartRecord.baseVersion.EDTN)
            {
                necessary = (searchCell.EDTN > chartRecord.baseVersion.EDTN);
            }
            else if (searchCell.file.Extension == ".000")
            {
                necessary = (searchCell.UPDN >= chartRecord.baseVersion.UPDN);
            }
            else
            {
                necessary = (searchCell.UPDN > chartRecord.baseVersion.UPDN);
            }

            searchCell.validation.necessary = necessary;

            return necessary;
        }
    }
}