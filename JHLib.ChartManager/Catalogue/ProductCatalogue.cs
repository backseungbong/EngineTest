using System.IO;

namespace JHLib.ChartManager.Catalogue
{
    public class ProductCatalogue
    {
        public List<ENC.Product> catalogue { get; private set; } = new List<ENC.Product>();

        public bool loaded { get; private set; } = false;



        public void Load(string filePath, bool append = false)
        {
            this.loaded = false;

            if (!append)
            {
                this.catalogue.Clear();
            }

            using (StreamReader reader = new StreamReader(filePath))
            {
                string? productDateTime = reader.ReadLine();
                string? productVersion = reader.ReadLine();
                string? productContentType = reader.ReadLine();
                string? productChartType = reader.ReadLine();

                ENC.Product product = new ENC.Product() {
                    chartType = productChartType switch {
                        _ when (productChartType?.StartsWith(":ENC") ?? false) => 0,
                        _ when (productChartType?.StartsWith(":ECS") ?? false) => 1,
                        _ => null,
                    },
                };

                if (productDateTime?.StartsWith(":DATE") == true)
                {
                    if (productDateTime.Length > 13) { product.date = productDateTime[6..14]; }
                    if (productDateTime.Length > 19) { product.time = productDateTime[15..20]; }
                }

                if ((productVersion?.StartsWith(":VERSION") == true) &&
                    (productVersion.Length > 9) &&
                    int.TryParse(productVersion[9..], out int version))
                {
                    product.version = version;
                }

                if ((productContentType?.StartsWith(":CONTENT") == true) &&
                    (productContentType.Length > 9))
                {
                    product.contentType = productContentType[9..] switch {
                        "FULL" => 0,
                        "PARTIAL" => 1,
                        _ => null,
                    };
                }

                string? readLine = null;

                while ((readLine = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(readLine))
                    {
                        if (readLine.StartsWith(':'))
                        {
                            break;
                        }
                        else
                        {
                            ENC.Product.Item productItem = new ENC.Product.Item();
                            string[] dataSegment = readLine.Split(',');

                            if (dataSegment.Length > 0) { productItem.name = dataSegment[0]; }
                            if (dataSegment.Length > 1) { productItem.issueDate = dataSegment[1]; }
                            if (dataSegment.Length > 2) { productItem.editionNumber = dataSegment[2]; }
                            if (dataSegment.Length > 3) { productItem.updateDate = dataSegment[3]; }
                            if (dataSegment.Length > 4) { productItem.updateNumber = string.IsNullOrEmpty(dataSegment[4]) ? "0" : dataSegment[4]; }
                            if (dataSegment.Length > 5) { productItem.fileSize = dataSegment[5]; }
                            if ((dataSegment.Length > 9) &&
                                double.TryParse(dataSegment[6], out double south) &&
                                double.TryParse(dataSegment[7], out double west) &&
                                double.TryParse(dataSegment[8], out double north) &&
                                double.TryParse(dataSegment[9], out double east))
                            {
                                productItem.boundary = (north, south, east, west);
                            }
                            if ((dataSegment.Length > 30) &&
                                int.TryParse(dataSegment[30], out int compression))
                            {
                                productItem.compression = compression;
                            }
                            if ((dataSegment.Length > 31) &&
                                int.TryParse(dataSegment[31], out int encryption))
                            {
                                productItem.encryption = encryption;
                            }
                            if (dataSegment.Length > 32) { productItem.baseUpdateNumber = dataSegment[32]; }
                            if (dataSegment.Length > 33) { productItem.preEditionUpdateNumber = dataSegment[33]; }
                            if (dataSegment.Length > 34) { productItem.reserve = dataSegment[34]; }
                            if (dataSegment.Length > 35) { productItem.comment = dataSegment[35]; }

                            if (!string.IsNullOrEmpty(productItem.name))
                            {
                                string chartName = Path.GetFileNameWithoutExtension(productItem.name);

                                if (chartName.Length > 7)
                                {
                                    string[]? reserve = productItem.reserve?.Split(';');
                                    string productDirectory = Path.Combine(
                                        Path.GetDirectoryName(filePath) ?? "",
                                        "..",
                                        (reserve?.Length > 1) ? reserve[1] : "",
                                        "ENC_ROOT",
                                        chartName[..2],
                                        chartName,
                                        productItem.editionNumber ?? "",
                                        productItem.updateNumber ?? ""
                                    );

                                    if (!Directory.Exists(productDirectory))
                                    {

                                    }
                                }
                                else
                                {

                                }

                                product.item.TryAdd(chartName, productItem);
                            }
                            else
                            {

                            }
                        }
                    }
                }

                this.catalogue.Add(product);
            }

            this.loaded = true;
        }
    }
}