using JHLib.ChartManager.Catalogue;
using JHLib.ChartManager.Chart.Detection;
using JHLib.ChartManager.Chart.Search;
using JHLib.ChartManager.Configuration;
using JHLib.ChartManager.Record;
using JHLib.ChartManager.Report;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace JHLib.ChartManager
{
    public class ChartOrganizer(ChartManagerLogger logger)
    {
        private ChartManagerLogger logger = logger;



        public (Dictionary<string, SearchChart> searchChart, List<ProvideCatalogue> searchProvide) SearchChart(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                FileInfo? serialFile = new DirectoryInfo(directoryPath).GetFiles("*", new EnumerationOptions() {
                                                                            RecurseSubdirectories = true,
                                                                            IgnoreInaccessible = true,
                                                                        })
                                                                       .FirstOrDefault(file => file.Name.Contains("SERIAL.ENC", StringComparison.CurrentCultureIgnoreCase));

                if (serialFile != null)
                {
                    return SearchS63(directoryPath);
                }
                else
                {
                    return (SearchS57(directoryPath), new List<ProvideCatalogue>());
                }
            }
            else
            {
                return (new Dictionary<string, SearchChart>(), new List<ProvideCatalogue>());
            }
        }

        public Dictionary<string, SearchChart> SearchS57(string directoryPath, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Dictionary<string, SearchChart> lootingCollection = new Dictionary<string, SearchChart>();

            if (Directory.Exists(directoryPath))
            {
                FileInfo? catalogueFile = new DirectoryInfo(directoryPath).GetFiles("*", searchOption)
                                                                          .FirstOrDefault(file => file.Name.Contains("CATALOG"));

                if (catalogueFile != null)
                {
                    CellCatalogue cellCatalogue = new CellCatalogue();
                    cellCatalogue.Load(catalogueFile.FullName);

                    if (cellCatalogue.loaded)
                    {
                        List<FileInfo> cellFileCollection = (catalogueFile.Directory ?? new DirectoryInfo("")).GetFiles()
                                                                                                              .Where(file => (!file.Name.Contains("CATALOG") && int.TryParse(file.Extension.Replace(".", ""), out int updateNumber)))
                                                                                                              .ToList();

                        foreach (FileInfo cellFile in cellFileCollection)
                        {
                            string chartName = Path.GetFileNameWithoutExtension(cellFile.Name);

                            if (cellCatalogue.catalogue.ContainsKey(chartName))
                            {
                                ENC.Cell? cell = cellCatalogue.catalogue[chartName].FirstOrDefault(cell => (Path.GetFileName(cell.FILE) == cellFile.Name));

                                if (cell != null)
                                {
                                    if ((cell.EDTN == null) || (cell.UPDN == null))
                                    {
                                        DetectionCell detectionCell = new DetectionCell();
                                        detectionCell.Read(cellFile.OpenRead());

                                        cell.EDTN = detectionCell.editionNumber;
                                        cell.UPDN = detectionCell.updateNumber;
                                    }
                                }
                            }
                        }

                        foreach (FileInfo cellFile in cellFileCollection)
                        {
                            string chartName = Path.GetFileNameWithoutExtension(cellFile.Name);

                            if (cellCatalogue.catalogue.ContainsKey(chartName))
                            {
                                ENC.Cell? cell = cellCatalogue.catalogue[chartName].FirstOrDefault(cell => (Path.GetFileName(cell.FILE) == cellFile.Name));

                                if (cell != null)
                                {
                                    SearchChart searchChart;

                                    if (lootingCollection.ContainsKey(chartName))
                                    {
                                        searchChart = lootingCollection[chartName];
                                    }
                                    else
                                    {
                                        searchChart = new SearchChart(chartName) {
                                            boundary = cell.boundary, // legacy s63 방식에서 boundary를 기록하지 않으면 안 되는 상황이 있어서 추가된 것인데, 없앨 수 있는지 다시 확인해야? (처음에 base cell의 boundary를 가지고 시작하는?)
                                            subContent = cellCatalogue.subContent.Where(content => (Path.GetDirectoryName(content.contentFile) == Path.GetDirectoryName(cellFile.FullName))).ToList(),
                                        };
                                        searchChart.validation.permit = 0;
                                        searchChart.validation.necessary = true;

                                        if (cellFile.Extension == ".000")
                                        {
                                            if ((cell.EDTN != null) && (cell.UPDN != null))
                                            {
                                                searchChart.baseVersion = (cell.EDTN.Value, cell.UPDN.Value);
                                            }
                                        }
                                        else
                                        {
                                            ENC.Cell? baseCell = cellCatalogue.catalogue[chartName].FirstOrDefault(cell => (Path.GetExtension(cell.FILE) == ".000"));

                                            if (baseCell != null)
                                            {
                                                if ((baseCell.EDTN != null) && (baseCell.UPDN != null))
                                                {
                                                    searchChart.baseVersion = (baseCell.EDTN.Value, baseCell.UPDN.Value);
                                                }
                                            }
                                            else
                                            {
                                                ENC.Cell minimumCell = cellCatalogue.catalogue[chartName].MinBy(cell => cell.UPDN)!;
                                                ENC.Cell maximumCell = cellCatalogue.catalogue[chartName].Where(cell => (cell.EDTN == minimumCell.EDTN)).MaxBy(cell => cell.UPDN)!;

                                                if ((minimumCell.EDTN != null) && (minimumCell.UPDN != null))
                                                {
                                                    searchChart.baseVersion = (minimumCell.EDTN.Value, minimumCell.UPDN.Value - 1);
                                                }

                                                if ((maximumCell.EDTN != null) && (maximumCell.UPDN != null))
                                                {
                                                    searchChart.updateVersion = maximumCell.UPDN;
                                                }
                                            }
                                        }

                                        lootingCollection.Add(chartName, searchChart);
                                    }

                                    SearchCell searchCell = new SearchCell(cellFile) {
                                        EDTN = cell.EDTN,
                                        UPDN = cell.UPDN,
                                        CRC = cell.CRC,
                                        provider = cell.provider,
                                    };
                                    searchCell.validation.signature = true;
                                    searchCell.validation.necessary = ValidateCellNecessary(searchChart, searchCell);

                                    searchChart.cell.Add(searchCell);
                                }
                            }
                        }

                        ValidateChartProduct(lootingCollection);
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }

            return lootingCollection;
        }

        public (Dictionary<string, SearchChart> searchChart, List<ProvideCatalogue> searchProvide) SearchS63(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                MediaCatalogue mediaCatalogue = new MediaCatalogue();
                string singleMedia = Path.Combine(directoryPath, "MEDIA.TXT");

                if (File.Exists(singleMedia))
                {
                    mediaCatalogue.Load(singleMedia);
                }
                else
                {
                    foreach (string searchDirectory in Directory.GetDirectories(directoryPath))
                    {
                        string media = Path.Combine(searchDirectory, "MEDIA.TXT");

                        if (File.Exists(media))
                        {
                            mediaCatalogue.Load(media, true);
                        }
                    }
                }

                if (mediaCatalogue.catalogue.Count > 0)
                {
                    return SearchMediaChart(mediaCatalogue);
                }
                else
                {
                    return SearchGenericChart(directoryPath);
                }
            }
            else
            {
                return (new Dictionary<string, SearchChart>(), new List<ProvideCatalogue>());
            }
        }

        private (Dictionary<string, SearchChart> searchChart, List<ProvideCatalogue> searchProvide) SearchMediaChart(MediaCatalogue mediaCatalogue)
        {
            Dictionary<string, SearchChart> lootingCollection = new Dictionary<string, SearchChart>();
            List<ProvideCatalogue> provideCollection = new List<ProvideCatalogue>();

            foreach (ENC.Media media in mediaCatalogue.catalogue)
            {
                FileInfo mediaFile = new FileInfo(media.filePath);

                if (mediaFile.Directory?.Exists == true)
                {
                    foreach (ENC.Media.Record mediaRecord in media.record)
                    {
                        FileInfo serialFile = new FileInfo(Path.Combine(mediaFile.Directory.FullName, mediaRecord.folder ?? "", "SERIAL.ENC"));
                        ENC.SerialEnc? serialEnc = null;

                        if (serialFile.Exists)
                        {
                            serialEnc = new ENC.SerialEnc(serialFile);

                            if (serialEnc.type == "BASE")
                            {
                                if ((serialEnc.provider != null) &&
                                    (serialEnc.week != null) &&
                                    (serialEnc.current != null))
                                {
                                    ProvideCatalogue provideCatalogue = new ProvideCatalogue();
                                    provideCatalogue.Load(serialEnc.provider);

                                    if (provideCatalogue.catalogue.ContainsKey(serialEnc.current.Value))
                                    {
                                        provideCatalogue.catalogue[serialEnc.current.Value].week = serialEnc.week.Value;
                                        provideCatalogue.catalogue[serialEnc.current.Value].referenceDate = serialEnc.issueDate;
                                    }
                                    else
                                    {
                                        ProvideRecord provideRecord = new ProvideRecord(
                                            serialEnc.provider,
                                            serialEnc.current.Value,
                                            serialEnc.week.Value
                                        ) {
                                            referenceDate = serialEnc.issueDate,
                                        };

                                        provideCatalogue.catalogue.Add(
                                            serialEnc.current.Value,
                                            provideRecord
                                        );
                                    }

                                    provideCollection.Add(provideCatalogue);
                                }
                            }
                            else if (serialEnc.type == "UPDATE")
                            {
                                FileInfo? statusFile = new FileInfo(Path.Combine(mediaFile.Directory.FullName, "INFO", "STATUS.LST"));

                                if (statusFile.Exists)
                                {
                                    List<ENC.StatusLst> statusCollection = new List<ENC.StatusLst>();

                                    using (StreamReader statusReader = new StreamReader(statusFile.OpenRead()))
                                    {
                                        string? readLine = statusReader.ReadLine();

                                        if (readLine?.Length > 1)
                                        {
                                            ProvideCatalogue provideCatalogue = new ProvideCatalogue();
                                            provideCatalogue.Load(readLine[..2]);

                                            int? updateWeek = int.TryParse(readLine[8..10] + readLine[6..8], out int statusWeek) ? statusWeek : null;

                                            while ((readLine = statusReader.ReadLine()) != null)
                                            {
                                                if (!string.IsNullOrEmpty(readLine))
                                                {
                                                    string[] dataSegment = readLine.Split(',');

                                                    if ((dataSegment.Length > 0) &&
                                                        (dataSegment[0].Length > 1) &&
                                                        int.TryParse(dataSegment[0][1..], out int baseNumber))
                                                    {
                                                        ENC.StatusLst statusLst = new ENC.StatusLst(baseNumber);

                                                        if (dataSegment.Length > 1) { statusLst.provider = dataSegment[1]; }
                                                        if ((dataSegment.Length > 2) &&
                                                            (dataSegment[2].Length > 6) &&
                                                            int.TryParse(dataSegment[2][5..] + dataSegment[2][2..4], out int week)) { statusLst.week = week; }
                                                        if (dataSegment.Length > 3) { statusLst.message = dataSegment[3]; }
                                                        if ((dataSegment.Length > 4) &&
                                                            DateTime.TryParseExact(
                                                                dataSegment[4],
                                                                "yyyyMMdd",
                                                                CultureInfo.InvariantCulture,
                                                                DateTimeStyles.AssumeUniversal,
                                                                out DateTime issueDate
                                                            ))
                                                        {
                                                            statusLst.issueDate = issueDate.ToUniversalTime();
                                                        }

                                                        statusCollection.Add(statusLst);
                                                    }
                                                }
                                            }

                                            if (provideCatalogue.loaded)
                                            {
                                                statusCollection.ForEach(statusLst => {
                                                    if (provideCatalogue.catalogue.TryGetValue(statusLst.baseNumber, out ProvideRecord? provideRecord))
                                                    {
                                                        if (provideRecord.week >= statusLst.week)
                                                        {
                                                            if (updateWeek != null)
                                                            {
                                                                provideRecord.week = updateWeek.Value;
                                                            }
                                                            else if (serialEnc.week != null)
                                                            {
                                                                provideRecord.week = serialEnc.week.Value;
                                                            }

                                                            provideRecord.referenceDate = serialEnc.issueDate;
                                                        }
                                                        else
                                                        {
                                                            SSE.InvokeGenericError($"This `Update Media` is not compatible with the actual installed `Base Media`. Please install the following `Base Media` first and then continue with the `Update Media`: `{statusLst.message}`");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        SSE.InvokeGenericError($"This `Update Media` is not compatible with the actual installed `Base Media`. Please install the following `Base Media` first and then continue with the `Update Media`: `{statusLst.message}`");
                                                    }
                                                });
                                            }
                                            else
                                            {
                                                statusCollection.ForEach(statusLst => {
                                                    SSE.InvokeGenericError($"This `Update Media` is not compatible with the actual installed `Base Media`. Please install the following `Base Media` first and then continue with the `Update Media`: `{statusLst.message}`");
                                                });
                                            }

                                            provideCollection.Add(provideCatalogue);
                                        }
                                    }
                                }
                            }
                        }

                        string cellCatalogueFile = Path.Combine(mediaFile.Directory.FullName, mediaRecord.folder ?? "", "ENC_ROOT", "CATALOG.031");

                        if (File.Exists(cellCatalogueFile))
                        {
                            CellCatalogue mediaCellCatalogue = new CellCatalogue();
                            mediaCellCatalogue.Load(cellCatalogueFile, media);

                            foreach (KeyValuePair<string, List<ENC.Cell>> cellCatalogue in mediaCellCatalogue.catalogue)
                            {
                                foreach (ENC.Cell cell in cellCatalogue.Value)
                                {
                                    FileInfo cellFile = new FileInfo(Path.Combine(
                                        Path.GetDirectoryName(cell.catalogueFile) ?? "",
                                        cell.FILE ?? ""
                                    ));

                                    if (cellFile.Exists)
                                    {
                                        SearchChart searchChart;

                                        if (lootingCollection.ContainsKey(cellCatalogue.Key))
                                        {
                                            searchChart = lootingCollection[cellCatalogue.Key];
                                        }
                                        else
                                        {
                                            searchChart = new SearchChart(cellCatalogue.Key) {
                                                boundary = cell.boundary,
                                                subContent = mediaCellCatalogue.subContent.Where(content => (Path.GetDirectoryName(content.contentFile) == Path.GetDirectoryName(cellFile.FullName))).ToList(),
                                                serialEnc = serialEnc,
                                            };

                                            if (PermitCatalogue.catalogue.TryGetValue(cellCatalogue.Key, out List<ENC.Permit>? chartPermit))
                                            {
                                                ENC.Permit? cellPermit = chartPermit.FirstOrDefault(permit => (permit.DSID == cell.provider));

                                                if (cellPermit != null)
                                                {
                                                    searchChart.validation.permit = cellPermit.error;

                                                    switch (searchChart.validation.permit)
                                                    {
                                                        case ENC.Permit.Validation.Warning:
                                                            {
                                                                SSE.InvokeStandardError(SSE.StandardError.ERROR_20, searchChart.name);
                                                                this.logger.Info($"SSE20|{searchChart.name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_20, searchChart.name)}");
                                                            }
                                                            break;
                                                        case ENC.Permit.Validation.Expired:
                                                            {
                                                                SSE.InvokeStandardError(SSE.StandardError.ERROR_25, searchChart.name);
                                                                this.logger.Info($"SSE25|{searchChart.name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_25, searchChart.name)}");
                                                            }
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    searchChart.validation.permit = ENC.Permit.Validation.FatalError;

                                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_10, cellCatalogue.Key);
                                                    this.logger.Info($"SSE10|{cellCatalogue.Key}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_10, cellCatalogue.Key)}");
                                                }
                                            }
                                            else
                                            {
                                                searchChart.validation.permit = ENC.Permit.Validation.FatalError;

                                                SSE.InvokeStandardError(SSE.StandardError.ERROR_10, cellCatalogue.Key);
                                                this.logger.Info($"SSE10|{cellCatalogue.Key}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_10, cellCatalogue.Key)}");
                                            }

                                            ValidateProductNecessary(mediaCatalogue.productCatalogue.SelectMany(p => p.catalogue).ToList(), searchChart);

                                            lootingCollection.Add(cellCatalogue.Key, searchChart);
                                        }

                                        SearchCell searchCell = new SearchCell(cellFile) {
                                            EDTN = cell.EDTN,
                                            UPDN = cell.UPDN,
                                            CRC = cell.CRC,
                                            provider = cell.provider,
                                        };
                                        searchCell.validation.signature = ValidateCellSigniture(cellFile);
                                        searchCell.validation.necessary = ValidateCellNecessary(searchChart, searchCell);

                                        ENC.Product.Item? cellProductItem = mediaCatalogue.productCatalogue.SelectMany(p => p.catalogue)
                                                                                                           .ToList()
                                                                                                           .Select(product => product.item.TryGetValue(searchChart.name, out ENC.Product.Item? productItem) ? productItem : null)
                                                                                                           .FirstOrDefault(productItem => (productItem != null));

                                        if (cellProductItem != null)
                                        {
                                            searchCell.compression = (cellProductItem.compression > 0);
                                            searchCell.encryption = (cellProductItem.encryption > 0);
                                        }

                                        searchChart.cell.Add(searchCell);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, SearchChart> searchRecord in lootingCollection)
            {
                SearchChart searchChart = searchRecord.Value;
                int? update = searchChart.cell.Max(cell => cell.UPDN);

                if ((update == null) || (update < searchChart.updateVersion))
                {
                    SSE.InvokeStandardError(SSE.StandardError.ERROR_27, searchRecord.Key);
                    this.logger.Info($"SSE27|{searchRecord.Key}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_27, searchRecord.Key)}");
                }
            }

            ValidateChartProduct(lootingCollection);

            return (lootingCollection, provideCollection);
        }

        private (Dictionary<string, SearchChart> searchChart, List<ProvideCatalogue> searchProvide) SearchGenericChart(string directoryPath)
        {
            Dictionary<string, SearchChart> lootingCollection = new Dictionary<string, SearchChart>();
            List<ProvideCatalogue> provideCollection = new List<ProvideCatalogue>();

            if (Directory.Exists(directoryPath))
            {
                FileInfo[] searchFile = new DirectoryInfo(directoryPath).GetFiles("*", new EnumerationOptions() {
                                                                             RecurseSubdirectories = true,
                                                                             IgnoreInaccessible = true,
                                                                         });
                FileInfo? serialFile = searchFile.FirstOrDefault(file => file.Name.Equals("SERIAL.ENC", StringComparison.CurrentCultureIgnoreCase));

                if (serialFile != null)
                {
                    ENC.SerialEnc serialEnc = new ENC.SerialEnc(serialFile);
                    
                    if (serialEnc.type == "BASE")
                    {
                        if ((serialEnc.provider != null) &&
                            (serialEnc.week != null) &&
                            (serialEnc.current != null))
                        {
                            ProvideCatalogue provideCatalogue = new ProvideCatalogue();
                            provideCatalogue.Load(serialEnc.provider);

                            if (provideCatalogue.catalogue.ContainsKey(serialEnc.current.Value))
                            {
                                provideCatalogue.catalogue[serialEnc.current.Value].week = serialEnc.week.Value;
                                provideCatalogue.catalogue[serialEnc.current.Value].referenceDate = serialEnc.issueDate;
                            }
                            else
                            {
                                ProvideRecord provideRecord = new ProvideRecord(
                                    serialEnc.provider,
                                    serialEnc.current.Value,
                                    serialEnc.week.Value
                                ) {
                                    referenceDate = serialEnc.issueDate,
                                };

                                provideCatalogue.catalogue.Add(
                                    serialEnc.current.Value,
                                    provideRecord
                                );
                            }
                            
                            provideCollection.Add(provideCatalogue);
                        }
                    }
                    else if (serialEnc.type == "UPDATE") // 이전 media에서 일부분 없을 수 있는 케이스는 전혀 고려하지 않는 듯 (B2 안에 하나만 있는 게 아닌데)
                    {
                        FileInfo? statusFile = new FileInfo(Path.Combine(serialFile.Directory?.FullName ?? "", "INFO", "STATUS.LST"));

                        if (statusFile.Exists)
                        {
                            List<ENC.StatusLst> statusCollection = new List<ENC.StatusLst>();

                            using (StreamReader statusReader = new StreamReader(statusFile.OpenRead()))
                            {
                                string? readLine = statusReader.ReadLine();

                                if (readLine?.Length > 1)
                                {
                                    ProvideCatalogue provideCatalogue = new ProvideCatalogue();
                                    provideCatalogue.Load(readLine[..2]);

                                    int? updateWeek = int.TryParse(readLine[8..10] + readLine[6..8], out int statusWeek) ? statusWeek : null;

                                    while ((readLine = statusReader.ReadLine()) != null)
                                    {
                                        if (!string.IsNullOrEmpty(readLine))
                                        {
                                            string[] dataSegment = readLine.Split(',');

                                            if ((dataSegment.Length > 0) &&
                                                (dataSegment[0].Length > 1) &&
                                                int.TryParse(dataSegment[0][1..], out int baseNumber))
                                            {
                                                ENC.StatusLst statusLst = new ENC.StatusLst(baseNumber);

                                                if (dataSegment.Length > 1) { statusLst.provider = dataSegment[1]; }
                                                if ((dataSegment.Length > 2) &&
                                                    (dataSegment[2].Length > 6) &&
                                                    int.TryParse(dataSegment[2][5..] + dataSegment[2][2..4], out int week)) { statusLst.week = week; }
                                                if (dataSegment.Length > 3) { statusLst.message = dataSegment[3]; }
                                                if ((dataSegment.Length > 4) &&
                                                    DateTime.TryParseExact(
                                                        dataSegment[4],
                                                        "yyyyMMdd",
                                                        CultureInfo.InvariantCulture,
                                                        DateTimeStyles.AssumeUniversal,
                                                        out DateTime issueDate
                                                    ))
                                                {
                                                    statusLst.issueDate = issueDate.ToUniversalTime();
                                                }

                                                statusCollection.Add(statusLst);
                                            }
                                        }
                                    }

                                    if (provideCatalogue.loaded)
                                    {
                                        statusCollection.ForEach(statusLst => {
                                            if (provideCatalogue.catalogue.TryGetValue(statusLst.baseNumber, out ProvideRecord? provideRecord))
                                            {
                                                if (provideRecord.week >= statusLst.week)
                                                {
                                                    if (updateWeek != null)
                                                    {
                                                        provideRecord.week = updateWeek.Value;
                                                    }
                                                    else if (serialEnc.week != null)
                                                    {
                                                        provideRecord.week = serialEnc.week.Value;
                                                    }

                                                    provideRecord.referenceDate = serialEnc.issueDate;
                                                }
                                                else
                                                {
                                                    SSE.InvokeGenericError($"This `Update Media` is not compatible with the actual installed `Base Media`. Please install the following `Base Media` first and then continue with the `Update Media`: `{statusLst.message}`");
                                                }
                                            }
                                            else
                                            {
                                                SSE.InvokeGenericError($"This `Update Media` is not compatible with the actual installed `Base Media`. Please install the following `Base Media` first and then continue with the `Update Media`: `{statusLst.message}`");
                                            }
                                        });
                                    }
                                    else
                                    {
                                        statusCollection.ForEach(statusLst => {
                                            SSE.InvokeGenericError($"This `Update Media` is not compatible with the actual installed `Base Media`. Please install the following `Base Media` first and then continue with the `Update Media`: `{statusLst.message}`");
                                        });
                                    }

                                    provideCollection.Add(provideCatalogue);
                                }
                            }
                        }
                    }

                    ProductCatalogue productCatalogue = new ProductCatalogue();
                    productCatalogue.Load(Path.Combine(serialFile.Directory?.FullName ?? "", "INFO", "PRODUCTS.TXT"));

                    CellCatalogue cellCatalogue = new CellCatalogue();
                    cellCatalogue.Load(Path.Combine(serialFile.Directory?.FullName ?? "", "ENC_ROOT", "CATALOG.031"));

                    if (productCatalogue.loaded && cellCatalogue.loaded)
                    {
                        List<FileInfo> cellFileCollection = searchFile.Where(file => (!file.Name.Contains("CATALOG") && int.TryParse(file.Extension.Replace(".", ""), out int updateNumber))).ToList();

                        foreach (FileInfo cellFile in cellFileCollection)
                        {
                            string chartName = Path.GetFileNameWithoutExtension(cellFile.Name);

                            if (cellCatalogue.catalogue.ContainsKey(chartName))
                            {
                                ENC.Cell? cell = cellCatalogue.catalogue[chartName].FirstOrDefault(cell => (Path.GetFileName(cell.FILE) == cellFile.Name));

                                if (cell != null)
                                {
                                    SearchChart searchChart;

                                    if (lootingCollection.ContainsKey(chartName))
                                    {
                                        searchChart = lootingCollection[chartName];
                                    }
                                    else
                                    {
                                        searchChart = new SearchChart(chartName) {
                                            boundary = cell.boundary,
                                            subContent = cellCatalogue.subContent.Where(content => (Path.GetDirectoryName(content.contentFile) == Path.GetDirectoryName(cellFile.FullName))).ToList(),
                                            serialEnc = serialEnc,
                                        };

                                        if (PermitCatalogue.catalogue.TryGetValue(chartName, out List<ENC.Permit>? chartPermit))
                                        {
                                            ENC.Permit? cellPermit = chartPermit.FirstOrDefault(permit => (permit.DSID == cell.provider));

                                            if (cellPermit != null)
                                            {
                                                searchChart.validation.permit = cellPermit.error;

                                                switch (searchChart.validation.permit)
                                                {
                                                    case ENC.Permit.Validation.Warning:
                                                        {
                                                            SSE.InvokeStandardError(SSE.StandardError.ERROR_20, chartName);
                                                            this.logger.Info($"SSE20|{chartName}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_20, chartName)}");
                                                        }
                                                        break;
                                                    case ENC.Permit.Validation.Expired:
                                                        {
                                                            SSE.InvokeStandardError(SSE.StandardError.ERROR_25, chartName);
                                                            this.logger.Info($"SSE25|{chartName}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_25, chartName)}");
                                                        }
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                searchChart.validation.permit = ENC.Permit.Validation.FatalError;

                                                SSE.InvokeStandardError(SSE.StandardError.ERROR_10, chartName);
                                                this.logger.Info($"SSE10|{chartName}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_10, chartName)}");
                                            }
                                        }
                                        else
                                        {
                                            searchChart.validation.permit = ENC.Permit.Validation.FatalError;

                                            SSE.InvokeStandardError(SSE.StandardError.ERROR_10, chartName);
                                            this.logger.Info($"SSE10|{chartName}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_10, chartName)}");
                                        }

                                        ValidateProductNecessary(productCatalogue.catalogue, searchChart);

                                        lootingCollection.Add(chartName, searchChart);
                                    }

                                    SearchCell searchCell = new SearchCell(cellFile) {
                                        EDTN = cell.EDTN,
                                        UPDN = cell.UPDN,
                                        CRC = cell.CRC,
                                        provider = cell.provider,
                                    };
                                    searchCell.validation.signature = ValidateCellSigniture(cellFile);
                                    searchCell.validation.necessary = ValidateCellNecessary(searchChart, searchCell);

                                    ENC.Product.Item? cellProductItem = productCatalogue.catalogue.Select(product => product.item.TryGetValue(searchChart.name, out ENC.Product.Item? productItem) ? productItem : null)
                                                                                                  .FirstOrDefault(productItem => (productItem != null));

                                    if (cellProductItem != null)
                                    {
                                        searchCell.compression = (cellProductItem.compression > 0);
                                        searchCell.encryption = (cellProductItem.encryption > 0);
                                    }

                                    searchChart.cell.Add(searchCell);
                                }
                            }
                        }

                        foreach (KeyValuePair<string, SearchChart> searchRecord in lootingCollection)
                        {
                            SearchChart searchChart = searchRecord.Value;
                            int? update = searchChart.cell.Max(cell => cell.UPDN);

                            if ((update == null) || (update < searchChart.updateVersion))
                            {
                                SSE.InvokeStandardError(SSE.StandardError.ERROR_27, searchRecord.Key);
                                this.logger.Info($"SSE27|{searchRecord.Key}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_27, searchRecord.Key)}");
                            }
                        }

                        ValidateChartProduct(lootingCollection);
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }

            return (lootingCollection, provideCollection);
        }

        private void ValidateProductNecessary(List<ENC.Product> productCatalogue, SearchChart searchChart)
        {
            ENC.Product.Item? chartProductItem = productCatalogue.Select(product => product.item.TryGetValue(searchChart.name, out ENC.Product.Item? productItem) ? productItem : null)
                                                                 .FirstOrDefault(productItem => (productItem != null));

            if (chartProductItem != null)
            {
                if (int.TryParse(chartProductItem.editionNumber, out int EDTN) &&
                    int.TryParse(chartProductItem.baseUpdateNumber, out int UPDN))
                {
                    searchChart.baseVersion = (EDTN, UPDN);
                }

                if (int.TryParse(chartProductItem.updateNumber, out int updateNumber))
                {
                    searchChart.updateVersion = updateNumber;
                }

                searchChart.issueDate = chartProductItem.issueDate;

                if (ChartCatalogue.catalogue.TryGetValue(searchChart.name, out ChartRecord? chartRecord))
                {
                    if (searchChart.baseVersion?.EDTN > chartRecord.baseVersion?.EDTN)
                    {
                        searchChart.validation.necessary = true;
                    }
                    else if (searchChart.baseVersion?.EDTN == chartRecord.baseVersion?.EDTN)
                    {
                        bool productSuitable = DateTime.TryParseExact(chartProductItem.issueDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime productIssueDate);
                        bool recordSuitable = DateTime.TryParseExact(chartRecord.issueDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime recordIssueDate);

                        if (productSuitable &&
                            recordSuitable &&
                            (productIssueDate - recordIssueDate).Days > 0)
                        {
                            searchChart.validation.necessary = true;
                        }
                        else if (searchChart.updateVersion > chartRecord.updateVersion)
                        {
                            searchChart.validation.necessary = true;
                        }
                        else
                        {
                            searchChart.validation.necessary = false;
                        }
                    }
                    else
                    {
                        searchChart.validation.necessary = false;

                        if ((searchChart.baseVersion?.EDTN == 0) && !string.IsNullOrEmpty(chartProductItem.comment))
                        {
                            SSE.InvokeGenericError($"{chartRecord.name} has been cancelled and has been replaced by cell(s), {chartProductItem.comment.Replace(";", " & ")}. Please contact your data supplier to obtain the additional ENC permits.");
                        }
                    }
                }
                else
                {
                    searchChart.validation.necessary = true;
                }
            }
            else
            {
                searchChart.validation.necessary = false;
            }
        }

        private bool ValidateCellSigniture(FileInfo cellFile)
        {
            bool result = false;

            if (cellFile.Name.Length > 11)
            {
                string provider = cellFile.Name[..2];
                string NA = cellFile.Name[3..12];

                if (int.TryParse(cellFile.Name[2..3], out int INTU))
                {
                    FileInfo cellSignature = new FileInfo(Path.Combine(cellFile.DirectoryName ?? "", $"{provider}{(char)(72 + INTU)}{NA}"));

                    if (cellSignature.Exists)
                    {
                        using (StreamReader reader = new StreamReader(cellSignature.OpenRead()))
                        {
                            ENC.Signature signature = new ENC.Signature(cellFile.Name);
                            signature.Read(reader);

                            if ((signature.digest != null) && (SaCatalogue.catalogue.Count > 0))
                            {
                                List<ENC.Certificate> certificateCollection = SaCatalogue.catalogue.Values.Where(certificate => (certificate.status != "Expired")).ToList();

                                if (certificateCollection.Count > 0)
                                {
                                    bool certificateValidation = false;

                                    foreach (ENC.Certificate certificate in certificateCollection)
                                    {
                                        if (certificate.Authenticate(signature))
                                        {
                                            certificateValidation = true;
                                            result = signature.Authenticate(cellFile);

                                            if (result)
                                            {
                                                if (!(certificate.O?.Contains("International Hydrographic Organization (IHO)") == true))
                                                {
                                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_26, cellFile.Name);
                                                    this.logger.Info($"SSE26|{cellFile.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_26, cellFile.Name)}");
                                                }
                                            }
                                            else
                                            {
                                                SSE.InvokeStandardError(SSE.StandardError.ERROR_09, cellFile.Name);
                                                this.logger.Info($"SSE09|{cellFile.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_09, cellFile.Name)}");
                                            }

                                            break; // signature 유효성이 한 번 확인되면 굳이 다른 certificate로 반복할 필요가 없음
                                        }
                                    }

                                    if (!certificateValidation)
                                    {
                                        SSE.InvokeStandardError(SSE.StandardError.ERROR_06, cellFile.Name);
                                        this.logger.Info($"SSE06|{cellFile.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_06, cellFile.Name)}");
                                    }
                                }
                                else
                                {
                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_05, cellFile.Name);
                                    this.logger.Info($"SSE05|{cellFile.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_05, cellFile.Name)}");
                                }
                            }
                            else
                            {
                                if (SaCatalogue.catalogue.Count < 1)
                                {
                                    SSE.InvokeStandardError(SSE.StandardError.ERROR_05, cellFile.Name);
                                    this.logger.Info($"SSE05|{cellFile.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_05, cellFile.Name)}");
                                }
                            }
                        }
                    }
                    else
                    {
                        SSE.InvokeStandardError(SSE.StandardError.ERROR_24, cellFile.Name);
                        this.logger.Info($"SSE24|{cellFile.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_24, cellFile.Name)}");
                    }
                }
            }

            return result;
        }

        private bool ValidateCellNecessary(SearchChart searchChart, SearchCell searchCell)
        {
            if (searchChart.validation.necessary && (searchChart.baseVersion?.EDTN == searchCell.EDTN))
            {
                if (ChartCatalogue.catalogue.TryGetValue(searchChart.name, out ChartRecord? chartRecord))
                {
                    if (searchCell.EDTN > chartRecord.baseVersion?.EDTN)
                    {
                        if (searchCell.file.Extension == ".000")
                        {
                            return (searchChart.baseVersion?.UPDN == searchCell.UPDN) &&
                                   (searchCell.UPDN <= searchChart.updateVersion);
                        }
                        else
                        {
                            return (searchChart.baseVersion?.UPDN < searchCell.UPDN) &&
                                   (searchCell.UPDN <= searchChart.updateVersion);
                        }
                    }
                    else if (searchCell.EDTN == chartRecord.baseVersion?.EDTN)
                    {
                        if (searchCell.file.Extension == ".000")
                        {
                            if (DateTime.TryParseExact(searchChart.issueDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime incoming) &&
                                DateTime.TryParseExact(chartRecord.issueDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime holding) &&
                                ((incoming - holding).Days > 0))
                            {
                                return (chartRecord.baseVersion?.UPDN <= searchCell.UPDN) &&
                                       (searchChart.baseVersion?.UPDN == searchCell.UPDN) &&
                                       (searchCell.UPDN <= searchChart.updateVersion);
                            }
                            else
                            {
                                return (chartRecord.baseVersion?.UPDN < searchCell.UPDN) &&
                                       (searchChart.baseVersion?.UPDN == searchCell.UPDN) &&
                                       (searchCell.UPDN <= searchChart.updateVersion);
                            }
                        }
                        else
                        {
                            return (chartRecord.baseVersion?.UPDN < searchCell.UPDN) &&
                                   (searchChart.baseVersion?.UPDN < searchCell.UPDN) &&
                                   (searchCell.UPDN <= searchChart.updateVersion);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (searchCell.file.Extension == ".000")
                    {
                        return (searchChart.baseVersion?.UPDN == searchCell.UPDN) &&
                               (searchCell.UPDN <= searchChart.updateVersion);
                    }
                    else
                    {
                        return (searchChart.baseVersion?.UPDN < searchCell.UPDN) &&
                               (searchCell.UPDN <= searchChart.updateVersion);
                    }
                }
            }
            else
            {
                return false;
            }
        }

        private void ValidateChartProduct(Dictionary<string, SearchChart> lootingCollection)
        {
            foreach (SearchChart searchChart in lootingCollection.Values)
            {
                if (searchChart.baseVersion?.EDTN == 0)
                {
                    searchChart.validation.product = true;
                }
                else
                {
                    List<SearchCell> validCellCollection = searchChart.cell;//.Where(searchCell => searchCell.validation.signature).ToList();

                    if (validCellCollection.Count > 0)
                    {
                        SearchCell? baseCell = validCellCollection.FirstOrDefault(validCell => validCell.file.Extension == ".000");

                        if (baseCell != null)
                        {
                            int reference = ((2 * (baseCell.UPDN ?? 0)) + validCellCollection.Count - 1) * validCellCollection.Count / 2;
                            int sum = validCellCollection.Sum(validCell => validCell.UPDN) ?? 0;

                            searchChart.validation.product = (reference == sum);
                        }
                        else
                        {
                            if (ChartCatalogue.catalogue.TryGetValue(searchChart.name, out ChartRecord? chartRecord))
                            {
                                int? updateInitialNumber = validCellCollection.Min(validCell => validCell.UPDN);

                                if ((updateInitialNumber != null) &&
                                    (chartRecord.baseVersion?.EDTN == searchChart.baseVersion?.EDTN) &&
                                    ((chartRecord.updateVersion ?? 0) >= (updateInitialNumber - 1)))
                                {
                                    int reference = ((2 * updateInitialNumber.Value) + validCellCollection.Count - 1) * validCellCollection.Count / 2;
                                    int sum = validCellCollection.Sum(validCell => validCell.UPDN) ?? 0;

                                    searchChart.validation.product = (reference == sum);
                                }
                                else
                                {
                                    searchChart.validation.product = false;
                                }
                            }
                            else
                            {
                                searchChart.validation.product = false;
                            }
                        }
                    }
                    else
                    {
                        searchChart.validation.product = false;
                    }
                }

                if (!searchChart.validation.product)
                {
                    SSE.InvokeStandardError(SSE.StandardError.ERROR_23, searchChart.name);
                    this.logger.Info($"SSE23|{searchChart.name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_23, searchChart.name)}");
                }
            }
        }


        public Dictionary<string, ChartDownloadReport> DownloadChart(Dictionary<string, SearchChart> searchCollection, bool append = false)
        {
            Dictionary<string, ChartDownloadReport> downloadReport = new Dictionary<string, ChartDownloadReport>();

            foreach (KeyValuePair<string, SearchChart> searchItem in searchCollection)
            {
                downloadReport.Add(searchItem.Key, DownloadChart(searchItem.Key, searchItem.Value, append));
            }

            return downloadReport;
        }

        public ChartDownloadReport DownloadChart(string chartName, SearchChart searchChart, bool append = false)
        {
            DirectoryInfo downloadDirectory = new DirectoryInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName));

            if (downloadDirectory.Exists)
            {
                if (append)
                {
                    foreach (FileInfo oldFile in downloadDirectory.GetFiles())
                    {
                        int exist = searchChart.cell.Count(cell => cell.file.Name == oldFile.Name);

                        if (exist > 0)
                        {
                            try
                            {
                                oldFile.Delete();
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }

                    SearchCell? searchBaseCell = searchChart.cell.FirstOrDefault(cell => cell.file.Extension == ".000");

                    if (searchBaseCell != null)
                    {
                        foreach (FileInfo oldFile in downloadDirectory.GetFiles())
                        {
                            if (int.TryParse(oldFile.Extension.Replace(".", ""), out int UPND) &&
                                (0 < UPND) &&
                                (UPND <= searchChart.baseVersion?.UPDN))
                            {
                                try
                                {
                                    oldFile.Delete();
                                }
                                catch (Exception e)
                                {

                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (FileInfo oldFile in downloadDirectory.GetFiles())
                    {
                        try
                        {
                            oldFile.Delete();
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }

            if (!downloadDirectory.Exists) { downloadDirectory.Create(); }

            ChartDownloadReport downloadReport = new ChartDownloadReport(chartName);

            foreach (SearchCell searchCell in searchChart.cell)
            {
                if (searchCell.validation.signature)
                {
                    downloadReport.cellReport.Add(DownloadCell(searchCell, downloadDirectory));
                }
            }

            foreach (ENC.SubContent subContent in searchChart.subContent)
            {
                if (DownloadSubContent(subContent, downloadDirectory))
                {

                }
                else
                {

                }
            }

            return downloadReport;
        }

        public CellDownloadReport DownloadCell(SearchCell searchCell, DirectoryInfo downloadDirectory)
        {
            if (searchCell.file.Exists)
            {
                FileInfo downloadFile;

                if (searchCell.encryption || searchCell.compression)
                {
                    if (ExtractCell(searchCell, downloadDirectory))
                    {
                        downloadFile = new FileInfo(Path.Combine(downloadDirectory.FullName, searchCell.file.Name));
                    }
                    else
                    {
                        return new CellDownloadReport(searchCell.file.Name, false, "TEST");
                    }
                }
                else
                {
                    downloadFile = new FileInfo(Path.Combine(downloadDirectory.FullName, searchCell.file.Name));

                    File.Copy(searchCell.file.FullName, downloadFile.FullName, true);
                }

                if (downloadFile.Exists &&
                    Cipher.CRC.ValidateCRC(File.ReadAllBytes(downloadFile.FullName), searchCell.CRC))
                {
                    return new CellDownloadReport(searchCell.file.Name, true, "TEST");
                }
                else
                {
                    SSE.InvokeStandardError(SSE.StandardError.ERROR_16, searchCell.file.Name);
                    this.logger.Info($"SSE16|{searchCell.file.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_16, searchCell.file.Name)}");

                    downloadFile.Delete();

                    return new CellDownloadReport(searchCell.file.Name, false, "TEST");
                }
            }
            else
            {
                return new CellDownloadReport(searchCell.file.Name, false, "TEST");
            }
        }

        private bool ExtractCell(SearchCell searchCell, DirectoryInfo downloadDirectory)
        {
            byte[]? cellData = null;

            try
            {
                if (searchCell.encryption)
                {
                    cellData = DecryptCell(searchCell);
                }
                else
                {
                    cellData = File.ReadAllBytes(searchCell.file.FullName);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
            
            if (cellData != null)
            {
                FileInfo? downloadFile = null;

                if (searchCell.compression)
                {
                    if (UncompressCell(cellData, downloadDirectory))
                    {
                        downloadFile = new FileInfo(Path.Combine(downloadDirectory.FullName, searchCell.file.Name));
                    }
                    else
                    {
                        SSE.InvokeStandardError(SSE.StandardError.ERROR_21, searchCell.file.Name);
                        this.logger.Info($"SSE21|{searchCell.file.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_21, searchCell.file.Name)}");
                    }
                }
                else
                {
                    FileInfo writingFile = new FileInfo(Path.Combine(downloadDirectory.FullName, searchCell.file.Name));

                    try
                    {
                        if (writingFile.Exists) { writingFile.Delete(); }

                        using (StreamWriter writer = new StreamWriter(writingFile.OpenWrite()))
                        {
                            writer.Write(cellData);
                            writer.Flush();
                        }

                        downloadFile = writingFile;
                    }
                    catch (Exception e)
                    {
                        SSE.InvokeStandardError(SSE.StandardError.ERROR_21, searchCell.file.Name);
                        this.logger.Info($"SSE21|{searchCell.file.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_21, searchCell.file.Name)}");
                    }
                }

                if (downloadFile != null)
                {
                    if (downloadFile.Exists)
                    {
                        //try
                        //{
                        //    cellData = File.ReadAllBytes(downloadFile.FullName);
                        //}
                        //catch (Exception e)
                        //{
                        //    SSE.InvokeStandardError(SSE.StandardError.ERROR_21, searchCell.file.Name);
                        //    this.logger.Info($"SSE21|{searchCell.file.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_21, searchCell.file.Name)}");

                        //    return false;
                        //}

                        //if (Cipher.CRC.ValidateCRC(cellData, searchCell.CRC))
                        //{
                        //    return true;
                        //}
                        //else
                        //{
                        //    SSE.InvokeStandardError(SSE.StandardError.ERROR_16, searchCell.file.Name);
                        //    this.logger.Info($"SSE16|{searchCell.file.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_16, searchCell.file.Name)}");

                        //    downloadFile.Delete();

                        //    return false;
                        //}
                        return true; // 밖에서 CRC 체크하고 있어서 중복인듯
                    }
                    else
                    {
                        SSE.InvokeStandardError(SSE.StandardError.ERROR_21, searchCell.file.Name);
                        this.logger.Info($"SSE21|{searchCell.file.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_21, searchCell.file.Name)}");

                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                SSE.InvokeStandardError(SSE.StandardError.ERROR_21, searchCell.file.Name);
                this.logger.Info($"SSE21|{searchCell.file.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_21, searchCell.file.Name)}");

                return false;
            }
        }

        private byte[]? DecryptCell(SearchCell searchCell)
        {
            string chartName = Path.GetFileNameWithoutExtension(searchCell.file.Name);

            if (PermitCatalogue.catalogue.TryGetValue(chartName, out List<ENC.Permit>? chartPermit))
            {
                ENC.Permit? cellPermit = chartPermit.FirstOrDefault(permit => (permit.DSID == searchCell.provider));

                if (cellPermit != null)
                {
                    string? Key = (searchCell.EDTN > cellPermit.EDTN) ? cellPermit.key?.X : cellPermit.key?.Y;

                    if (!string.IsNullOrEmpty(Key))
                    {
                        Cipher.BlowFish blowFish = new Cipher.BlowFish(Convert.FromHexString(Key));

                        using (FileStream encryptedStream = searchCell.file.OpenRead())
                        using (MemoryStream decryptedStream = new MemoryStream())
                        {
                            while (true)
                            {
                                byte[] readBuffer = new byte[8];
                                int readLength = encryptedStream.Read(readBuffer, 0, 8);

                                if (readLength > 0)
                                {
                                    decryptedStream.Write(blowFish.Decrypt(readBuffer, 0, 0), 0, 8);
                                }
                                else
                                {
                                    break;
                                }
                            }

                            return decryptedStream.ToArray();
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private bool UncompressCell(byte[] archiveData, DirectoryInfo downloadDirectory)
        {
            try
            {
                bool result = true;

                using (MemoryStream archiveStream = new MemoryStream(archiveData))
                using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        try
                        {
                            entry.ExtractToFile(Path.Combine(downloadDirectory.FullName, entry.FullName), true);
                        }
                        catch (Exception e)
                        {
                            Trace.WriteLine(e.Message);

                            result = false;
                        }
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);

                return false;
            }
        }

        public bool DownloadSubContent(ENC.SubContent subContent, DirectoryInfo downloadDirectory)
        {
            if (subContent.contentFile == null) { return false; }

            try
            {
                File.Copy(subContent.contentFile, Path.Combine(downloadDirectory.FullName, Path.GetFileName(subContent.contentFile)), true);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}