using System.IO;

namespace JHLib.ChartManager.Catalogue
{
    public class MediaCatalogue
    {
        public List<ENC.Media> catalogue { get; private set; } = new List<ENC.Media>();
        public List<ProductCatalogue> productCatalogue { get; private set; } = new List<ProductCatalogue>();

        public bool loaded { get; private set; } = false;



        public void Load(string filePath, bool append = false)
        {
            this.loaded = false;

            if (!append)
            {
                this.catalogue.Clear();
                this.productCatalogue.Clear();
            }

            using (StreamReader reader = new StreamReader(filePath))
            {
                string? mediaHeader = reader.ReadLine();

                if (mediaHeader != null)
                {
                    ENC.Media media = new ENC.Media(filePath);

                    if (mediaHeader.Length > 1) { media.header.dataServerID = mediaHeader[..2].Trim(); }
                    if (mediaHeader.Length > 11) {
                        media.header.weekOfIssue = mediaHeader[2..12].Trim();

                        if (media.header.weekOfIssue.Length > 6)
                        {
                            string year = media.header.weekOfIssue[5..7];
                            string week = media.header.weekOfIssue[2..4];

                            if (int.TryParse(year + week, out int weekValue))
                            {
                                media.header.week = weekValue;
                            }
                        }
                    }
                    if (mediaHeader.Length > 19) { media.header.dateOfIssue = mediaHeader[12..20].Trim(); }
                    if (mediaHeader.Length > 29) { media.header.mediaType = mediaHeader[20..30].Trim(); }
                    if (mediaHeader.Length > 35) { media.header.MLI = mediaHeader[30..36].Trim(); }

                    mediaHeader = reader.ReadLine();

                    if (mediaHeader != null)
                    {
                        string[] headerSegment = mediaHeader.Split(',');

                        if (headerSegment.Length > 0) { media.header.mediaID = headerSegment[0]; }
                        if (headerSegment.Length > 1) { media.header.MRMN = headerSegment[1]; }

                        string? readLine = null;

                        while ((readLine = reader.ReadLine()) != null)
                        {
                            if (!string.IsNullOrEmpty(readLine))
                            {
                                ENC.Media.Record record = new ENC.Media.Record();
                                string[] recordSegment = readLine.Split(',');

                                if (recordSegment.Length > 0) {
                                    string[] dataSegment = recordSegment[0].Split(';');

                                    if (dataSegment.Length > 0) { record.location = dataSegment[0]; }
                                    if (dataSegment.Length > 1) { record.folder = dataSegment[1]; }
                                }
                                if (recordSegment.Length > 1) { record.date = recordSegment[1]; }
                                if (recordSegment.Length > 2) { record.mediaNumber = recordSegment[2].Replace("'", ""); }

                                media.record.Add(record);
                            }
                        }
                    }

                    FileInfo productFile = new FileInfo(Path.Combine(Path.GetDirectoryName(filePath) ?? "", "INFO", "PRODUCTS.TXT"));

                    if (productFile.Exists)
                    {
                        ProductCatalogue productCatalogue = new ProductCatalogue();
                        productCatalogue.Load(Path.Combine(Path.GetDirectoryName(filePath) ?? "", "INFO", "PRODUCTS.TXT"));

                        this.productCatalogue.Add(productCatalogue);

                        if (media.header.mediaType == "UPDATE")
                        {
                            //
                        }
                    }

                    this.catalogue.Add(media);
                }
            }

            this.loaded = true;
        }
    }
}