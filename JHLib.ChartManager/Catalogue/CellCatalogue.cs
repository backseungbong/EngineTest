using System.IO;

namespace JHLib.ChartManager.Catalogue
{
    public class CellCatalogue
    {
        public Dictionary<string, List<ENC.Cell>> catalogue { get; private set; } = new Dictionary<string, List<ENC.Cell>>();
        public List<ENC.SubContent> subContent { get; private set; } = new List<ENC.SubContent>();

        public bool loaded { get; private set; } = false;



        public void Load(string filePath, bool append = false)
        {
            this.loaded = false;

            if (!append)
            {
                this.catalogue.Clear();
                this.subContent.Clear();
            }

            string directoryName = Path.GetDirectoryName(filePath) ?? string.Empty;

            FileInfo cellSerialFile = new FileInfo(Path.Combine(directoryName, "..", "SERIAL.ENC"));
            ENC.Cell.Serial? cellSerial = null;

            if (cellSerialFile.Exists)
            {
                using (StreamReader reader = new StreamReader(cellSerialFile.OpenRead()))
                {
                    cellSerial = ReadCellSerial(reader);
                }
            }

            using (StreamReader reader = new StreamReader(filePath))
            {
                List<ENC.Cell> cellCollection = this.Read(reader);

                foreach (ENC.Cell cell in cellCollection)
                {
                    string chartName = Path.GetFileNameWithoutExtension(cell.FILE) ?? string.Empty;

                    if (!string.IsNullOrEmpty(chartName))
                    {
                        switch (cell.IMPL)
                        {
                            case "BIN":
                                {
                                    if (cellSerial != null)
                                    {
                                        cell.provider = cellSerial.provider;
                                        cell.provideType = cellSerial.type;
                                        cell.week = cellSerial.week;
                                    }

                                    cell.catalogueFile = filePath;

                                    if (!this.catalogue.ContainsKey(chartName)) { this.catalogue.Add(chartName, new List<ENC.Cell>()); }

                                    this.catalogue[chartName].Add(cell);
                                }
                                break;
                            case "TXT":
                            case "TIF":
                                {
                                    if (Path.GetExtension(cell.FILE)?.ToUpper() == $".{cell.IMPL}")
                                    {
                                        this.subContent.Add(new ENC.SubContent(cell.RCNM, cell.RCID) {
                                            IMPL = cell.IMPL,
                                            contentFile = Path.Combine(directoryName, cell.FILE!),
                                        });
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            this.loaded = true;
        }

        public void Load(string filePath, ENC.Media media, bool append = false)
        {
            this.loaded = false;

            if (!append)
            {
                this.catalogue.Clear();
                this.subContent.Clear();
            }

            string directoryName = Path.GetDirectoryName(filePath) ?? string.Empty;

            using (StreamReader reader = new StreamReader(filePath))
            {
                List<ENC.Cell> cellCollection = this.Read(reader);

                foreach (ENC.Cell cell in cellCollection)
                {
                    string chartName = Path.GetFileNameWithoutExtension(cell.FILE) ?? string.Empty;

                    if (!string.IsNullOrEmpty(chartName))
                    {
                        switch (cell.IMPL)
                        {
                            case "BIN":
                                {
                                    cell.provider = media.header.dataServerID;
                                    cell.provideType = media.header.mediaType;
                                    cell.week = media.header.week;
                                    cell.catalogueFile = filePath;

                                    if (!this.catalogue.ContainsKey(chartName)) { this.catalogue.Add(chartName, new List<ENC.Cell>()); }

                                    this.catalogue[chartName].Add(cell);
                                }
                                break;
                            case "TXT":
                            case "TIF":
                                {
                                    this.subContent.Add(new ENC.SubContent(cell.RCNM, cell.RCID) {
                                        IMPL = cell.IMPL,
                                        contentFile = Path.Combine(directoryName, cell.FILE!),
                                    });
                                }
                                break;
                        }
                    }
                }
            }

            this.loaded = true;
        }

        private List<ENC.Cell> Read(StreamReader reader)
        {
            List<ENC.Cell> cellCollection = new List<ENC.Cell>();

            string catalogueData = reader.ReadToEnd();

            if ((catalogueData.Length > 6) && (catalogueData[6] == 'L') && int.TryParse(catalogueData.AsSpan(0, 5), out int dataStartIndex))
            {
                string[] readLine = catalogueData[dataStartIndex..].Split('\u001E');

                for (int i = 0; i < readLine.Length - 2; i += 3)
                {
                    if (readLine[i].Contains("CATD") && (readLine.Length > (i + 2)))
                    {
                        ENC.Cell cell;
                        string[] dataSegment = readLine[i + 2].Split('\u001F');

                        if ((dataSegment.Length > 0) &&
                            (dataSegment[0].Length > 12) &&
                            int.TryParse(dataSegment[0].AsSpan(2, 10), out int RCID))
                        {
                            cell = new ENC.Cell(dataSegment[0][0].ToString(), RCID) {
                                FILE = dataSegment[0][12..],
                            };

                            if (dataSegment.Length > 1) { cell.LFILE = dataSegment[1]; }
                            if (dataSegment.Length > 2) { cell.VOLM = dataSegment[2]; }
                            if (dataSegment.Length > 3) {
                                if (dataSegment[3].Length > 2) { cell.IMPL = dataSegment[3][..3]; }
                            }
                            if (dataSegment.Length > 7) { cell.CRC = dataSegment[7]; }
                            if (dataSegment.Length > 8) { // 63은 cell이 암호화되어 있어서 사전에 DSID를 못 보니까 이렇게 볼 수 있게 만들었다고 함
                                cell.comment = dataSegment[8];

                                string[] commentSegment = dataSegment[8].Split(',');

                                if ((commentSegment.Length > 0) &&
                                    (commentSegment[0].Length > 8) &&
                                    commentSegment[0].ToUpper().StartsWith("VERSION=")) {
                                    cell.version = commentSegment[0][8..];
                                }
                                if ((commentSegment.Length > 1) &&
                                    (commentSegment[1].Length > 5) &&
                                    commentSegment[1].ToUpper().StartsWith("EDTN=") &&
                                    int.TryParse(commentSegment[1].AsSpan(5), out int EDTN)) {
                                    cell.EDTN = EDTN;
                                }
                                if ((commentSegment.Length > 2) &&
                                    (commentSegment[2].Length > 5) &&
                                    commentSegment[2].ToUpper().StartsWith("UPDN=") &&
                                    int.TryParse(commentSegment[2].AsSpan(5), out int UPDN)) {
                                    cell.UPDN = UPDN;
                                }
                            }

                            switch (cell.IMPL)
                            {
                                case "BIN":
                                    {
                                        if ((dataSegment[3].Length > 3) &&
                                            (dataSegment.Length > 6) &&
                                            double.TryParse(dataSegment[3].AsSpan(3), out double south) &&
                                            double.TryParse(dataSegment[4], out double west) &&
                                            double.TryParse(dataSegment[5], out double north) &&
                                            double.TryParse(dataSegment[6], out double east))
                                        {
                                            cell.boundary = (north, south, east, west);
                                        }

                                        cellCollection.Add(cell);
                                    }
                                    break;
                                case "TXT":
                                case "TIF":
                                    {
                                        cellCollection.Add(cell);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            return cellCollection;
        }

        private ENC.Cell.Serial? ReadCellSerial(StreamReader reader)
        {
            ENC.Cell.Serial? cellSerial = null;

            string? readLine = null;

            while ((readLine = reader.ReadLine()) != null)
            {
                if (readLine.Length > 29)
                {
                    cellSerial = new ENC.Cell.Serial() {
                        provider = readLine[0..2],
                        issueDate = readLine[12..20].Trim(),
                        type = readLine[20..30].ToUpper(),
                    };

                    if (int.TryParse(readLine[7..9] + readLine[4..6], out int week))
                    {
                        cellSerial.week = week;
                    }
                }

                if (readLine.Length > 35)
                {
                    string number = readLine[35..].Trim();

                    if (number.Length > 5)
                    {
                        if (int.TryParse(number[1..3], out int currentNumber))
                        {
                            cellSerial!.currentNumber = currentNumber;
                        }

                        if (int.TryParse(number[4..6], out int totalNumber))
                        {
                            cellSerial!.totalNumber = totalNumber;
                        }
                    }
                }
            }

            return cellSerial;
        }
    }
}