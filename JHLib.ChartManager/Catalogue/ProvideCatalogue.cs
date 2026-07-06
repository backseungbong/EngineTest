using JHLib.ChartManager.Configuration;
using JHLib.ChartManager.Record;
using System.Globalization;
using System.IO;

namespace JHLib.ChartManager.Catalogue
{
    public class ProvideCatalogue
    {
        public Dictionary<int, ProvideRecord> catalogue { get; private set; } = new Dictionary<int, ProvideRecord>();

        public bool loaded { get; private set; } = false;

        public string provider = string.Empty;



        public void Load(string provider, bool append = false)
        {
            this.loaded = false;

            if (!append)
            {
                this.catalogue.Clear();
            }

            string filePath = Path.Combine(DirectoryConfiguration.catalogue, "Provide", $"{provider}.cat");

            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string? readLine = null;

                    while ((readLine = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(readLine))
                        {
                            string[] dataSegment = readLine.Split('@');

                            if ((dataSegment.Length > 1) &&
                                int.TryParse(dataSegment[0], out int baseNumber) &&
                                int.TryParse(dataSegment[1], out int week))
                            {
                                this.catalogue[baseNumber] = new ProvideRecord(provider, baseNumber, week);

                                if ((dataSegment.Length > 2) &&
                                    DateTime.TryParseExact(
                                        dataSegment[2],
                                        "yyyyMMdd",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.AssumeUniversal,
                                        out DateTime issueDate
                                    ))
                                {
                                    this.catalogue[baseNumber].referenceDate = issueDate.ToUniversalTime();
                                }
                            }
                        }
                    }
                }
            }

            this.provider = provider;
            this.loaded = true;
        }
    }
}