using JHLib.ChartManager.Catalogue;
using JHLib.ChartManager.Record;
using JHLib.Util.Time;
using System.IO;
using static JHLib.ChartManager.Configuration.TableConfiguration.LookupTable;
using static JHLib.ChartManager.Record.ChartRecord;

namespace JHLib.S57.Catalogue
{
    public static class ChartCat
    {
        static ChartCat()
        {
        }

        public static List<string> ListCat = new();

        private const float ScaleFactor = 10000000.0f;

        public static void Init(string exePath)
        {
            var catPath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.catalogueDir, "chart_new.cat");
            ChartCatalogue.Read(catPath);
            MakeListCat();
        }

        private static void MakeListCat()
        {
            foreach(var cat in ChartCatalogue.catalogue)
            {
                var chart = cat.Value as ChartRecord;
                if (chart.name.Contains("WORLDMAP")) continue;

                if (chart.IsChart1 == false)
                {
                    string status = "";
                    switch(chart.lifecycle)
                    {
                        case Lifecycle.Unknown: status = "Unknown"; break;
                        case Lifecycle.UpToDate: status = "Up to Date"; break;
                        case Lifecycle.Outdated: status = "Not Up to Date"; break;
                        case Lifecycle.Canceled: status = "With drawn"; break;
                    }

                    ListCat.Add($"{chart.name},{chart.baseVersion.EDTN},{chart.updateVersion},{chart.issueDate},{chart.updateDate},{status}");
                }
            }
        }

        public static int GetAgency(string chartName)
        {
            //return DicChartCat.TryGetValue(chartName, out var value) ? value.Agency : -1;
            return ChartCatalogue.catalogue.TryGetValue(chartName, out var value) ? value.agency ?? -1 : -1;
        }

        public static bool GetEditionAndUpdate(string chartName, out int edition, out int update)
        {
            edition = update = 0;
            if(ChartCatalogue.catalogue.TryGetValue(chartName, out var value) == true)
            {
                edition = value.baseVersion?.EDTN ?? 1;
                update = value.updateVersion ?? 0;
                return true;
            }

            return false;
        }

        public static bool IsOverlapChart(string chartName)
        {
            if (ChartCatalogue.catalogue.TryGetValue(chartName, out var value) == true)
            {
                return value.overlapped;
            }

            return false;
        }

        public static bool IsNotUpToDate(string chartName)
        {
            if (ChartCatalogue.catalogue.TryGetValue(chartName, out var value) == true)
            {
                return value.lifecycle == Lifecycle.Outdated ? true : false;
            }

            return false;
        }

        public static void GetChart1Position(out double lat, out double lon)
        {
            lat = lon = 0;
            if (ChartCatalogue.catalogue.TryGetValue("AA4C1XMS", out var value) == true)
            {
                lon = value.boundary.west / ScaleFactor;
                lat = value.boundary.north / ScaleFactor;
            }
        }

        public static bool GetChartPosition(string name, out double lat, out double lon)
        {
            lat = lon = 0;
            if (ChartCatalogue.catalogue.TryGetValue(name, out var value) == true)
            {
                lon = value.boundary.west / ScaleFactor;
                lat = value.boundary.north / ScaleFactor;
                return true;
            }

            return false;
        }

        public static int GetChartCount()
        {
            return ListCat.Count;
        }
        public static void GetChartStatusCout(ref int upToDate, ref int notUpToDate, ref int widthDrawn, ref int unknown)
        {
            // 카운트 초기화
            upToDate = 0;
            notUpToDate = 0;
            widthDrawn = 0;
            unknown = 0;

            string thresholdDate = AppTime.Utc.AddDays(-28).ToString("yyyyMMdd");

            foreach (var item in ChartCatalogue.catalogue)
            {
                var cat = item.Value as ChartRecord;
                if (cat.IsChart1 || cat.name.Contains("WORLDMAP")) continue;

                if (cat.lifecycle == Lifecycle.UpToDate) upToDate++;
                else if (cat.lifecycle == Lifecycle.Canceled) widthDrawn++;
                else if (cat.lifecycle == Lifecycle.Unknown) unknown++;
                else if (cat.lifecycle == Lifecycle.Unknown || (!string.IsNullOrEmpty(cat.referenceDate) && string.Compare(cat.referenceDate, thresholdDate) < 0)) notUpToDate++;
            }
        }

        public static bool IsNonOfficialData(string chartName)
        {
            if (ChartCatalogue.catalogue.TryGetValue(chartName, out var value) == true)
            {
                return string.IsNullOrEmpty(AgencyCat.GetName(value.agency ?? -1));
            }

            return false;
        }
    }
}
