using JHLib.ChartManager.Catalogue;
using JHLib.ChartManager.Configuration;
using JHLib.ChartManager.Record;
using JHLib.ChartManager.Report;
using Newtonsoft.Json;
using System.IO;

namespace JHLib.ChartManager.API
{
    public static class ChartManagerApi
    {




        public static FeatureAttributeRecord? ReadFeatureAttribute(string chartName, uint rcid)
        {
            Dictionary<uint, FeatureAttributeRecord> faCollection = ChartManagerApi.ReadFeatureAttributes(chartName);

            if (faCollection.TryGetValue(rcid, out FeatureAttributeRecord? faRecord))
            {
                return faRecord;
            }
            else
            {
                return null;
            }
        }

        public static Dictionary<uint, FeatureAttributeRecord> ReadFeatureAttributes(string chartName)
        {
            Dictionary<uint, FeatureAttributeRecord> faCollection = new Dictionary<uint, FeatureAttributeRecord>();
            FileInfo faFile = new FileInfo(Path.Combine(DirectoryConfiguration.encAttribute, chartName, $"{chartName}{FileConfiguration.encAttributeExtension}"));

            using (StreamReader reader = new StreamReader(faFile.OpenRead()))
            {
                string? readLine = null;

                while ((readLine = reader.ReadLine()) != null)
                {
                    FeatureAttributeRecord? featureAttribute;

                    try
                    {
                        featureAttribute = JsonConvert.DeserializeObject<FeatureAttributeRecord>(readLine);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }

                    if (featureAttribute != null)
                    {
                        faCollection.TryAdd(featureAttribute.frid.rcid, featureAttribute);
                    }
                }
            }

            return faCollection;
        }


        public static Dictionary<string, ChartDownloadReport> GetDownloadInformation()
        {
            Dictionary<string, ChartDownloadReport> downloadReportCollection = new Dictionary<string, ChartDownloadReport>();
            DirectoryInfo downloadDirectory = new DirectoryInfo(DirectoryConfiguration.encDownload);

            if (downloadDirectory.Exists)
            {
                foreach (DirectoryInfo downloadChartDirectory in downloadDirectory.GetDirectories())
                {
                    ChartDownloadReport? downloadReport = ChartManagerApi.GetDownloadInformation(downloadChartDirectory.Name);

                    if (downloadReport != null)
                    {
                        downloadReportCollection.Add(downloadChartDirectory.Name, downloadReport);
                    }
                }
            }

            return downloadReportCollection;
        }

        public static ChartDownloadReport? GetDownloadInformation(string chartName)
        {
            DirectoryInfo downloadDirectory = new DirectoryInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName));

            if (downloadDirectory.Exists)
            {
                ChartDownloadReport downloadReport = new ChartDownloadReport(chartName);
                List<FileInfo> downloadFileCollection = downloadDirectory.GetFiles()
                                                                         .Where(file => (!file.Name.Contains("CATALOG") && int.TryParse(file.Extension.Replace(".", ""), out int updateNumber)))
                                                                         .ToList();

                foreach (FileInfo downloadFile in downloadFileCollection)
                {
                    downloadReport.cellReport.Add(new CellDownloadReport(downloadFile.Name, true, string.Empty));
                }

                return downloadReport;
            }
            else
            {
                return null;
            }
        }
        
        public static void DeleteChart(string chartName, bool deleteDownload = false)
        {
            ChartCatalogue.Delete(chartName);

            FileInfo sencFile = new FileInfo(Path.Combine(DirectoryConfiguration.encSenc, $"{chartName}{FileConfiguration.encSencExtension}"));
            DirectoryInfo attributeDirectory = new DirectoryInfo(Path.Combine(DirectoryConfiguration.encAttribute, chartName));
            FileInfo coverageFile = new FileInfo(Path.Combine(DirectoryConfiguration.encCoverage, $"{chartName}{FileConfiguration.encCoverageExtension}"));
            FileInfo detectFile = new FileInfo(Path.Combine(DirectoryConfiguration.encDetect, $"{chartName}{FileConfiguration.encDetectExtension}"));
            FileInfo updateFile = new FileInfo(Path.Combine(DirectoryConfiguration.encUpdate, $"{chartName}{FileConfiguration.encUpdateExtension}"));

            if (sencFile.Exists) { sencFile.Delete(); }
            if (attributeDirectory.Exists) { attributeDirectory.Delete(true); }
            if (coverageFile.Exists) { coverageFile.Delete(); }
            if (detectFile.Exists) { detectFile.Delete(); }
            if (updateFile.Exists) { updateFile.Delete(); }
            if (deleteDownload) { DeleteDownload(chartName); }
        }

        public static void DeleteDownload(string chartName)
        {
            DirectoryInfo downloadDirectory = new DirectoryInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName));

            if (downloadDirectory.Exists) { downloadDirectory.Delete(true); }
        }

        public static void DeleteDownload(string chartName, string cellName)
        {
            DirectoryInfo downloadDirectory = new DirectoryInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName));

            if (downloadDirectory.Exists)
            {
                downloadDirectory.GetFiles().Where(file => file.Name == cellName).ToList().ForEach(file => {
                    file.Delete();
                });

                int cellCount = downloadDirectory.GetFiles().Count(file => (!file.Name.Contains("CATALOG") && int.TryParse(file.Extension.Replace(".", ""), out int updateNumber)));

                if (cellCount < 1)
                {
                    downloadDirectory.Delete(true);
                }
            }
        }
    }
}