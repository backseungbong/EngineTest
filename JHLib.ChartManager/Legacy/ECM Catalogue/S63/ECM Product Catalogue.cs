using Legacy.ECM_Core.Enumeration;
using System.IO;

namespace Legacy.ECM_Core.Catalogue
{
    public static class ProductCatalogue
    {
        public static List<ENC.Product> Catalogue { get; private set; } = new List<ENC.Product>();



        public static void Load(string file_path)
        {
            if (File.Exists(file_path))
            {
                using (StreamReader Catalogue_Reader = new StreamReader(file_path))
                {
                    string? Date_Sentence = Catalogue_Reader.ReadLine();
                    string? Version_Sentence = Catalogue_Reader.ReadLine();
                    string? ContentType_Sentence = Catalogue_Reader.ReadLine();
                    string? Enc_Sentence = Catalogue_Reader.ReadLine();

                    if ((Enc_Sentence != null) && Enc_Sentence.StartsWith(":ENC"))
                    {
                        ENC.Product Product = new ENC.Product();

                        if (Date_Sentence?.StartsWith(":DATE") == true)
                        {
                            if (Date_Sentence.Length > 13) { Product.Date = Date_Sentence[6..14]; }
                            if (Date_Sentence.Length > 19) { Product.Time = Date_Sentence[15..20]; }
                        }
                        if ((Version_Sentence?.StartsWith(":VERSION") == true) && (Version_Sentence.Length > 9)) { Product.Version = int.TryParse(Version_Sentence[9..], out int Version) ? Version : -1; }
                        if ((ContentType_Sentence?.StartsWith(":CONTENT") == true) && (ContentType_Sentence.Length > 9))
                        {
                            Product.Content_Type = ContentType_Sentence[9..] switch {
                                "FULL" => 0,
                                "PARTIAL" => 1,
                                _ => -1,
                            };
                        }

                        while (true)
                        {
                            string? Data_Sentence = Catalogue_Reader.ReadLine();

                            if (Data_Sentence != null)
                            {
                                if (Data_Sentence.StartsWith(':'))
                                {
                                    break;
                                }
                                else
                                {
                                    ENC.ProductRecord Product_Record = new ENC.ProductRecord();
                                    string[] Data_Segment = Data_Sentence.Split(',');

                                    if (Data_Segment.Length > 0) { Product_Record.Name = Data_Segment[0]; }
                                    if (Data_Segment.Length > 1) { Product_Record.Issue_Date = Data_Segment[1]; }
                                    if (Data_Segment.Length > 2) { Product_Record.Edition_Number = Data_Segment[2]; }
                                    if (Data_Segment.Length > 3) { Product_Record.Update_Date = Data_Segment[3]; }
                                    if (Data_Segment.Length > 4) { Product_Record.Update_Number = string.IsNullOrEmpty(Data_Segment[4]) ? "0" : Data_Segment[4]; }
                                    if (Data_Segment.Length > 5) { Product_Record.File_Size = Data_Segment[5]; }
                                    if (Data_Segment.Length > 6) { Product_Record.Boundary.South = double.TryParse(Data_Segment[6], out double South) ? South : 0.0; }
                                    if (Data_Segment.Length > 7) { Product_Record.Boundary.West = double.TryParse(Data_Segment[7], out double West) ? West : 0.0; }
                                    if (Data_Segment.Length > 8) { Product_Record.Boundary.North = double.TryParse(Data_Segment[8], out double North) ? North : 0.0; }
                                    if (Data_Segment.Length > 9) { Product_Record.Boundary.East = double.TryParse(Data_Segment[9], out double East) ? East : 0.0; }
                                    if (Data_Segment.Length > 30) { Product_Record.Compression = int.TryParse(Data_Segment[30], out int Compression) ? Compression : -1; }
                                    if (Data_Segment.Length > 31) { Product_Record.Encryption = int.TryParse(Data_Segment[31], out int Encryption) ? Encryption : -1; }
                                    if (Data_Segment.Length > 32) { Product_Record.Base_UpdateNumber = Data_Segment[32]; }
                                    if (Data_Segment.Length > 33) { Product_Record.PreEdition_UpdateNumber = Data_Segment[33]; }
                                    if (Data_Segment.Length > 34) { Product_Record.Reserve = Data_Segment[34]; }
                                    if (Data_Segment.Length > 35) { Product_Record.Comment = Data_Segment[35]; }

                                    if (Product.Record == null) { Product.Record = new Dictionary<string, ENC.ProductRecord>(); }

                                    if (!string.IsNullOrEmpty(Product_Record.Name))
                                    {
                                        string Chart_Name = Path.GetFileNameWithoutExtension(Product_Record.Name);

                                        if (Chart_Name.Length > 7)
                                        {
                                            string[]? Reserve = Product_Record.Reserve?.Split(';');
                                            string Product_Path = Path.Combine(Path.GetDirectoryName(file_path) ?? "", "..", (Reserve?.Length > 1) ? Reserve[1] : "", "ENC_ROOT", Chart_Name[..2], Chart_Name, Product_Record.Edition_Number ?? "", Product_Record.Update_Number ?? "");

                                            if (!Directory.Exists(Product_Path))
                                            {
                                                Product_Record.SSE27 = StandardError.Get_Message(SSE.ERROR_27, Chart_Name);
                                            }
                                        }
                                        else
                                        {
                                            Product_Record.SSE27 = StandardError.Get_Message(SSE.ERROR_27, Chart_Name); // 이건 굳이 안 알려도 된다는 건가?
                                        }

                                        Product.Record.TryAdd(Product_Record.Name.Split('.')[0], Product_Record);
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        Catalogue.Add(Product);
                    }
                }
            }
        }
    }
}