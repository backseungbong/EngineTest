using Legacy.ECM_Core.Catalogue;
using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Enumeration;
using System.Globalization;
using System.IO;

namespace Legacy.ECM_Core.Component
{
    public partial class ChartOrganizer
    {
        internal Dictionary<string, SearchChart> Search_S57(string searching_directory)
        {
            CellCatalogue.Catalogue.Clear();
            CellCatalogue.Sub_Content.Clear();
            MediaCatalogue.Catalogue.Clear();
            ProductCatalogue.Catalogue.Clear();

            Dictionary<string, SearchChart> Rooting_Collection = new Dictionary<string, SearchChart>();

            if (Directory.Exists(searching_directory))
            {
                FileInfo[] Search_File = new DirectoryInfo(searching_directory).GetFiles("*", SearchOption.AllDirectories);
                IEnumerable<FileInfo> SearchCatalogue_Enumeration = Search_File.Where(File => File.Name.Contains("CATALOG"));

                if (SearchCatalogue_Enumeration.Count() > 0)
                {
                    FileInfo Search_Catalogue = SearchCatalogue_Enumeration.First();
                    CellCatalogue.Load(Search_Catalogue.FullName);

                    IEnumerable<FileInfo> SearchCell_Enumeration = (Search_Catalogue.Directory ?? new DirectoryInfo("")).GetFiles().Where(File => !File.Name.Contains("CATALOG") && int.TryParse(File.Extension.Replace(".", ""), out int Update_Number));

                    foreach (FileInfo Search_Cell in SearchCell_Enumeration)
                    {
                        string Search_Chart = Path.GetFileNameWithoutExtension(Search_Cell.Name);

                        if (CellCatalogue.Catalogue.ContainsKey(Search_Chart))
                        {
                            IEnumerable<ENC.Cell> CatalogueCell_Enumeration = CellCatalogue.Catalogue[Search_Chart].Where(Catalogue => Path.GetFileName(Catalogue.File) == Search_Cell.Name);

                            if (CatalogueCell_Enumeration.Count() > 0)
                            {
                                ENC.Cell Catalogue_Cell = CatalogueCell_Enumeration.First();

                                if (!Rooting_Collection.ContainsKey(Search_Chart))
                                {
                                    SearchChart Chart = new SearchChart();
                                    Chart.Name = Search_Chart;
                                    Chart.Boundary = Catalogue_Cell.Boundary;
                                    Chart.Permit_Validation = 0;
                                    Chart.Necessary_Validation = true;
                                    Chart.Cell = new List<SearchCell>();

                                    Rooting_Collection.Add(Search_Chart, Chart);
                                }

                                SearchCell Cell = new SearchCell();
                                Cell.File = Search_Cell;
                                Cell.Signature_Validation = true;
                                Cell.Necessary_Validation = Validate_CellNecessary(Search_Chart, Cell);

                                Rooting_Collection[Search_Chart].Cell.Add(Cell);
                            }
                        }
                    }
                }
            }

            Validate_Product(Rooting_Collection);

            return Rooting_Collection;
        }

        internal Dictionary<string, SearchChart> Search_S63(string searching_directory)
        {
            CellCatalogue.Catalogue.Clear();
            CellCatalogue.Sub_Content.Clear();
            MediaCatalogue.Catalogue.Clear();
            ProductCatalogue.Catalogue.Clear();

            Dictionary<string, SearchChart> Rooting_Collection = new Dictionary<string, SearchChart>();

            if (Directory.Exists(searching_directory))
            {
                string Single_Media = Path.Combine(searching_directory, "MEDIA.TXT");

                if (File.Exists(Single_Media))
                {
                    MediaCatalogue.Read(Single_Media);
                }
                else
                {
                    List<string> Multi_Media = new List<string>();

                    foreach (string Searching_Directory in Directory.GetDirectories(searching_directory))
                    {
                        string Media = Path.Combine(Searching_Directory, "MEDIA.TXT");

                        if (File.Exists(Media))
                        {
                            Multi_Media.Add(Media);
                        }
                    }

                    MediaCatalogue.Read(Multi_Media.ToArray());
                }

                if (MediaCatalogue.Catalogue.Count > 0)
                {
                    Rooting_Collection = Search_MediaChart();
                }
                else
                {
                    Rooting_Collection = Search_GenericChart(searching_directory);
                }
            }

            return Rooting_Collection;
        }


        private Dictionary<string, SearchChart> Search_MediaChart()
        {
            Dictionary<string, SearchChart> Rooting_Collection = new Dictionary<string, SearchChart>();

            foreach ((string FilePath, ENC.Media Media) Catalogue in MediaCatalogue.Catalogue)
            {
                FileInfo Media_FileInfo = new FileInfo(Catalogue.FilePath);
                DirectoryInfo? Media_DirectoryInfo = Media_FileInfo.Directory;

                if (Media_DirectoryInfo?.Exists == true)
                {
                    if (Catalogue.Media.Record != null)
                    {
                        foreach (ENC.MediaRecord Media_Record in Catalogue.Media.Record)
                        {
                            string CellCatalogue_Path = Path.Combine(Media_DirectoryInfo.FullName, Media_Record.Folder, "ENC_ROOT", "CATALOG.031");

                            if (File.Exists(CellCatalogue_Path))
                            {
                                CellCatalogue.Load(CellCatalogue_Path, Catalogue.Media);
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, List<ENC.Cell>> Catalogue in CellCatalogue.Catalogue)
            {
                foreach (ENC.Cell Catalogue_Cell in Catalogue.Value)
                {
                    FileInfo Catalogue_FileInfo = new FileInfo(Catalogue_Cell.Catalogue_File);
                    FileInfo Cell = new FileInfo(Path.Combine(Catalogue_FileInfo.DirectoryName ?? "", Catalogue_Cell.File));

                    if (Cell.Exists)
                    {
                        if (!Rooting_Collection.ContainsKey(Catalogue.Key))
                        {
                            SearchChart Chart = new SearchChart();
                            Chart.Name = Catalogue.Key;
                            Chart.Boundary = Catalogue_Cell.Boundary;
                            Chart.Cell = new List<SearchCell>();

                            if (PermitCatalogue.Catalogue.TryGetValue(Catalogue.Key, out List<ENC.CellPermit>? Cell_Permit) && (Cell_Permit != null))
                            {
                                IEnumerable<ENC.CellPermit> Permit_Enumeration = Cell_Permit.Where(Permit => Permit.DSID == Catalogue_Cell.Provider);

                                if (Permit_Enumeration.Count() > 0)
                                {
                                    Chart.Permit_Validation = Permit_Enumeration.First().Error;
                                }
                                else
                                {
                                    Chart.Permit_Validation = 2;
                                    StandardError.Invoke_Message(SSE.ERROR_10, Catalogue.Key);
                                }
                            }
                            else
                            {
                                Chart.Permit_Validation = 2;
                            }

                            Chart = Validate_ProductNecessary(Chart);

                            Rooting_Collection.Add(Catalogue.Key, Chart);
                        }

                        SearchCell Search_Cell = new SearchCell();
                        Search_Cell.File = Cell;
                        Search_Cell.EDTN = Catalogue_Cell.EDTN;
                        Search_Cell.UPDN = Catalogue_Cell.UPDN;
                        Search_Cell.Signature_Validation = Validate_CellSigniture(Cell);
                        Search_Cell.Necessary_Validation = Validate_CellNecessary(Rooting_Collection[Catalogue.Key], Search_Cell);

                        Rooting_Collection[Catalogue.Key].Cell.Add(Search_Cell);
                    }
                }
            }

            Validate_Product(Rooting_Collection);

            return Rooting_Collection;
        }

        private Dictionary<string, SearchChart> Search_GenericChart(string searching_directory)
        {
            Dictionary<string, SearchChart> Rooting_Collection = new Dictionary<string, SearchChart>();

            if (Directory.Exists(searching_directory))
            {
                FileInfo[] Search_File = new DirectoryInfo(searching_directory).GetFiles("*", SearchOption.AllDirectories);
                IEnumerable<FileInfo> SearchSerial_Enumeration = Search_File.Where(File => File.Name.Contains("SERIAL.ENC"));

                if (SearchSerial_Enumeration.Count() > 0)
                {
                    FileInfo Search_Serial = SearchSerial_Enumeration.First();
                    DirectoryInfo? SearchSerial_DirectoryInfo = Search_Serial.Directory;

                    if (SearchSerial_DirectoryInfo?.Exists == true)
                    {
                        ProductCatalogue.Load(Path.Combine(SearchSerial_DirectoryInfo.FullName, "INFO", "PRODUCTS.TXT"));
                        CellCatalogue.Load(Path.Combine(SearchSerial_DirectoryInfo.FullName, "ENC_ROOT", "CATALOG.031"));

                        IEnumerable<FileInfo> Cell_Enumeration = Search_File.Where(File => !File.Name.Contains("CATALOG") && int.TryParse(File.Extension.Replace(".", ""), out int Update_Number));

                        foreach (FileInfo Cell in Cell_Enumeration)
                        {
                            string Search_Chart = Path.GetFileNameWithoutExtension(Cell.Name);

                            if (CellCatalogue.Catalogue.ContainsKey(Search_Chart))
                            {
                                IEnumerable<ENC.Cell> CatalogueCell_Enumeration = CellCatalogue.Catalogue[Search_Chart].Where(Catalogue_Cell => Path.Combine(Path.GetDirectoryName(Catalogue_Cell.Catalogue_File) ?? "", Catalogue_Cell.File) == Cell.FullName);

                                if (CatalogueCell_Enumeration.Count() > 0)
                                {
                                    ENC.Cell Catalogue_Cell = CatalogueCell_Enumeration.First();

                                    if (!Rooting_Collection.ContainsKey(Search_Chart))
                                    {
                                        SearchChart Chart = new SearchChart();
                                        Chart.Name = Search_Chart;
                                        Chart.Boundary = Catalogue_Cell.Boundary;
                                        Chart.Cell = new List<SearchCell>();

                                        if (PermitCatalogue.Catalogue.TryGetValue(Search_Chart, out List<ENC.CellPermit>? Cell_Permit) && (Cell_Permit != null))
                                        {
                                            IEnumerable<ENC.CellPermit> Permit_Enumeration = Cell_Permit.Where(Permit => Permit.DSID == Catalogue_Cell.Provider);

                                            if (Permit_Enumeration.Count() > 0)
                                            {
                                                Chart.Permit_Validation = Permit_Enumeration.First().Error;
                                            }
                                            else
                                            {
                                                Chart.Permit_Validation = 2;
                                                StandardError.Invoke_Message(SSE.ERROR_10, Search_Chart);
                                            }
                                        }
                                        else
                                        {
                                            Chart.Permit_Validation = 2;
                                        }

                                        Chart = Validate_ProductNecessary(Chart);

                                        Rooting_Collection.Add(Search_Chart, Chart);
                                    }

                                    SearchCell Search_Cell = new SearchCell();
                                    Search_Cell.File = Cell;
                                    Search_Cell.EDTN = Catalogue_Cell.EDTN;
                                    Search_Cell.UPDN = Catalogue_Cell.UPDN;
                                    Search_Cell.Signature_Validation = Validate_CellSigniture(Cell);
                                    Search_Cell.Necessary_Validation = Validate_CellNecessary(Rooting_Collection[Search_Chart], Search_Cell);

                                    Rooting_Collection[Search_Chart].Cell.Add(Search_Cell);
                                }
                            }
                        }
                    }
                }
            }

            Validate_Product(Rooting_Collection);

            return Rooting_Collection;
        }

        private bool Validate_CellSigniture(FileInfo cell)
        {
            bool Result = false;

            if (cell.Name.Length > 11)
            {
                string Provider = cell.Name[..2];
                string NA = cell.Name[3..12];

                if (int.TryParse(cell.Name[2..3], out int INTU))
                {
                    FileInfo Cell_Signature = new FileInfo(Path.Combine(cell.DirectoryName ?? "", $"{Provider}{(char)(72 + INTU)}{NA}"));

                    if (Cell_Signature.Exists)
                    {
                        using (StreamReader Signature_Reader = new StreamReader(Cell_Signature.OpenRead()))//using (StreamReader Signature_Reader = new StreamReader(Cell_Signature.OpenRead(), TextEncoding.RCP))
                        {
                            ENC.CellSignature? Signature = Read_Signature(Signature_Reader, cell.Name);

                            if ((Signature != null) && (SaCatalogue.Catalogue.Count > 0))
                            {
                                IEnumerable<SA.Certificate> Certificate_Enumeration = SaCatalogue.Catalogue.Values.Where(Certificate => Certificate.Status != "Expired");

                                if (Certificate_Enumeration.Count() > 0)
                                {
                                    bool Certificate_Result = false;

                                    foreach (SA.Certificate Certificate in Certificate_Enumeration)
                                    {
                                        if (Authenticate_EncCertificate(Certificate, Signature.Value)) // 이거 확인되면 cell 검증이 안 된다고 다른 sa로 할 필요는 없음
                                        {
                                            Certificate_Result = true;
                                            Result = Authenticate_EncCell(cell, Signature.Value);

                                            if (!Result)
                                            {
                                                StandardError.Invoke_Message(SSE.ERROR_09, cell.Name);
                                            }

                                            break;
                                        }
                                    }

                                    if (!Certificate_Result)
                                    {
                                        StandardError.Invoke_Message(SSE.ERROR_06, cell.Name);
                                    }
                                }
                                else
                                {
                                    StandardError.Invoke_Message(SSE.ERROR_05, cell.Name);
                                }
                            }
                        }
                    }
                    else
                    {
                        StandardError.Invoke_Message(SSE.ERROR_24, cell.Name);
                    }
                }
            }

            return Result;
        }

        private SearchChart Validate_ProductNecessary(SearchChart chart)
        {
            SearchChart Search_Chart = chart;
            IEnumerable<ENC.ProductRecord> ProductRecord_Enumeration = ProductCatalogue.Catalogue.Select(Catalogue => Catalogue.Record).Where(Record => Record.ContainsKey(Search_Chart.Name)).Select(Record => Record[Search_Chart.Name]);

            if (ProductRecord_Enumeration.Count() > 0)
            {
                ENC.ProductRecord Product_Record = ProductRecord_Enumeration.First();

                Search_Chart.Base.EDTN = int.TryParse(Product_Record.Edition_Number, out int EDTN) ? EDTN : -1;
                Search_Chart.Base.UPDN = int.TryParse(Product_Record.Base_UpdateNumber, out int Base_UPDN) ? Base_UPDN : -1;
                Search_Chart.UPDN = int.TryParse(Product_Record.Update_Number, out int UPDN) ? UPDN : -1;
                Search_Chart.Issue_Date = Product_Record.Issue_Date;

                if (ChartCatalogue.Catalogue.TryGetValue(Search_Chart.Name, out ENC.Chart Chart_Catalogue))
                {
                    if (Search_Chart.Base.EDTN > Chart_Catalogue.Base.EDTN)
                    {
                        Search_Chart.Necessary_Validation = true;
                    }
                    else if (Search_Chart.Base.EDTN == Chart_Catalogue.Base.EDTN)
                    {
                        bool Suitable_Product = DateTime.TryParseExact(Product_Record.Issue_Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Product_IssueDate);
                        bool Suitable_Catalogue = DateTime.TryParseExact(Chart_Catalogue.Issue_Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Catalogue_IssueDate);

                        if (Suitable_Product && Suitable_Catalogue && ((Product_IssueDate - Catalogue_IssueDate).Days > 0)) // 이거 chart catalogue에 base의 update number를 기록하면 해결되는 문제 아닌가? 그렇긴 한데, 그러면 뒤에 어떤 cell이 필요한지 판별할 때, Reissue 에 대한 게 또 뭔가 해야함
                        {
                            Search_Chart.Necessary_Validation = true;
                        }
                        else if (Search_Chart.UPDN > Chart_Catalogue.Update)
                        {
                            Search_Chart.Necessary_Validation = true;
                        }
                        else
                        {
                            Search_Chart.Necessary_Validation = false;
                        }
                    }
                    else
                    {
                        Search_Chart.Necessary_Validation = false;
                    }
                }
                else
                {
                    Search_Chart.Necessary_Validation = true;
                }
            }
            else
            {
                Search_Chart.Base.EDTN = -1;
                Search_Chart.Base.UPDN = -1;
                Search_Chart.UPDN = -1;
                Search_Chart.Issue_Date = "00000101";
                Search_Chart.Necessary_Validation = false;
            }

            return Search_Chart;
        }

        private bool Validate_CellNecessary(SearchChart chart, SearchCell cell)
        {
            if (chart.Necessary_Validation)
            {
                if (cell.EDTN == chart.Base.EDTN)
                {
                    if (ChartCatalogue.Catalogue.TryGetValue(chart.Name, out ENC.Chart Chart_Catalogue))
                    {
                        if (cell.EDTN > Chart_Catalogue.Base.EDTN)
                        {
                            if (cell.File.Extension == ".000")
                            {
                                return (chart.Base.UPDN == cell.UPDN) && (cell.UPDN <= chart.UPDN);
                            }
                            else
                            {
                                return (chart.Base.UPDN < cell.UPDN) && (cell.UPDN <= chart.UPDN);
                            }
                        }
                        else if (cell.EDTN == Chart_Catalogue.Base.EDTN)
                        {
                            if (cell.File.Extension == ".000")
                            {
                                bool Suitable_Product = DateTime.TryParseExact(chart.Issue_Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Product_IssueDate);
                                bool Suitable_Catalogue = DateTime.TryParseExact(Chart_Catalogue.Issue_Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Catalogue_IssueDate);

                                if (Suitable_Product && Suitable_Catalogue && ((Product_IssueDate - Catalogue_IssueDate).Days > 0))
                                {
                                    return (Chart_Catalogue.Base.UPDN <= cell.UPDN) && (chart.Base.UPDN == cell.UPDN) && (cell.UPDN <= chart.UPDN);
                                }
                                else
                                {
                                    return (Chart_Catalogue.Base.UPDN < cell.UPDN) && (chart.Base.UPDN == cell.UPDN) && (cell.UPDN <= chart.UPDN);
                                }
                            }
                            else
                            {
                                return (Chart_Catalogue.Base.UPDN < cell.UPDN) && (chart.Base.UPDN < cell.UPDN) && (cell.UPDN <= chart.UPDN);
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (cell.File.Extension == ".000")
                        {
                            return (chart.Base.UPDN == cell.UPDN) && (cell.UPDN <= chart.UPDN);
                        }
                        else
                        {
                            return (chart.Base.UPDN < cell.UPDN) && (cell.UPDN <= chart.UPDN);
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool Validate_CellNecessary(string chart_name, SearchCell cell)
        {
            if (ChartCatalogue.Catalogue.TryGetValue(chart_name, out ENC.Chart Chart_Catalogue))
            {
                if (cell.EDTN > Chart_Catalogue.Base.EDTN)
                {
                    return true;
                }
                else if (cell.EDTN == Chart_Catalogue.Base.EDTN)
                {
                    if (cell.File.Extension == ".000")
                    {
                        return (cell.UPDN >= Chart_Catalogue.Base.UPDN);
                    }
                    else
                    {
                        return (cell.UPDN > Chart_Catalogue.Base.UPDN);
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private void Validate_Product(Dictionary<string, SearchChart> rooting_collection)
        {
            foreach (KeyValuePair<string, SearchChart> Rooting_Item in rooting_collection)
            {
                SearchChart Rooting_Chart = Rooting_Item.Value;

                if (Rooting_Chart.Necessary_Validation)
                {
                    IEnumerable<SearchCell> RootingCell_Enumeration = Rooting_Chart.Cell.Where(Cell => Cell.Signature_Validation && Cell.Necessary_Validation);
                    int RootingCell_Count = RootingCell_Enumeration.Count();

                    if (RootingCell_Count > 0)
                    {
                        IEnumerable<SearchCell> BaseCell_Enumeration = RootingCell_Enumeration.Where(Cell => Cell.File.Extension == ".000");

                        if (BaseCell_Enumeration.Count() > 0)
                        {
                            SearchCell BaseCell = BaseCell_Enumeration.First();

                            int Reference = ((2 * BaseCell.UPDN) + RootingCell_Count - 1) * RootingCell_Count / 2;
                            int Sum = RootingCell_Enumeration.Select(Cell => Cell.UPDN).Sum();

                            Rooting_Chart.Product_Validation = (Reference == Sum);
                        }
                        else
                        {
                            if (ChartCatalogue.Catalogue.TryGetValue(Rooting_Chart.Name, out ENC.Chart Chart_Catalogue))
                            {
                                int Reference = ((2 * Chart_Catalogue.Update) + RootingCell_Count + 1) * RootingCell_Count / 2;
                                int Sum = RootingCell_Enumeration.Select(Cell => Cell.UPDN).Sum();

                                Rooting_Chart.Product_Validation = (Reference == Sum);
                            }
                            else
                            {
                                Rooting_Chart.Product_Validation = false;
                            }
                        }
                    }
                    else
                    {
                        Rooting_Chart.Product_Validation = false;
                    }
                }
                else
                {
                    Rooting_Chart.Product_Validation = true;
                }

                if (!Rooting_Chart.Product_Validation)
                {
                    StandardError.Invoke_Message(SSE.ERROR_23, Rooting_Chart.Name);
                }

                rooting_collection[Rooting_Item.Key] = Rooting_Chart;
            }
        }
    }
}