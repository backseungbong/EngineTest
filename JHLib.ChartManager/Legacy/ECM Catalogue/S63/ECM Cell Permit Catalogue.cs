using Legacy.ECM_Core.Enumeration;
using System.Globalization;
using System.IO;

namespace Legacy.ECM_Core.Catalogue
{
    public static class PermitCatalogue
    {
        public static Dictionary<string, List<ENC.CellPermit>> Catalogue { get; private set; } = new Dictionary<string, List<ENC.CellPermit>>();

        public static string HWID = "12345";



        public static void Read(string file_path)
        {
            Catalogue.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_path))
            {
                PermitCatalogue.Read(Catalogue_Reader);
            }
        }

        public static void Read(Stream file_stream)
        {
            Catalogue.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_stream))
            {
                PermitCatalogue.Read(Catalogue_Reader);
            }
        }

        private static void Read(StreamReader catalogue_reader)
        {
            while (true)
            {
                string? Data_Sentence = catalogue_reader.ReadLine();

                if (Data_Sentence != null)
                {
                    if (!string.IsNullOrEmpty(Data_Sentence))
                    {
                        ENC.CellPermit Permit = new ENC.CellPermit();
                        string[] Data_Segment = Data_Sentence.Split('@');

                        if (Data_Segment.Length > 0) { Permit.Name = Data_Segment[0]; }
                        if (Data_Segment.Length > 1) { Permit.Error = int.TryParse(Data_Segment[1], out int Error) ? Error : -1; }
                        if (Data_Segment.Length > 2) { Permit.Type = int.TryParse(Data_Segment[2], out int Type) ? Type : -1; }
                        if (Data_Segment.Length > 3) { Permit.Checksum = Data_Segment[3]; }
                        if (Data_Segment.Length > 4) { Permit.Expiration_Date = Data_Segment[4]; }
                        if (Data_Segment.Length > 5) { Permit.Key.X = Data_Segment[5]; }
                        if (Data_Segment.Length > 6) { Permit.Key.Y = Data_Segment[6]; }
                        if (Data_Segment.Length > 7) { Permit.Comment = Data_Segment[7]; }
                        if (Data_Segment.Length > 8) { Permit.DSID = Data_Segment[8]; }
                        if (Data_Segment.Length > 9) { Permit.EDTN = int.TryParse(Data_Segment[9], out int EDTN) ? EDTN : -1; }
                        if (Data_Segment.Length > 10) { Permit.Service_Level = int.TryParse(Data_Segment[10], out int Service_Level) ? Service_Level : -1; }

                        Permit.Error = Expiration(Permit);

                        if (!string.IsNullOrEmpty(Permit.Name))
                        {
                            if (!Catalogue.ContainsKey(Permit.Name))
                            {
                                Catalogue.Add(Permit.Name, new List<ENC.CellPermit>());
                            }

                            Catalogue[Permit.Name].Add(Permit);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            if (Catalogue.Count < 1)
            {
                StandardError.Invoke_Message(SSE.ERROR_11);
            }
        }


        public static void Add_Permit(string file_path)
        {
            if (File.Exists(file_path) && (Path.GetFileName(file_path).ToUpper() == "PERMIT.TXT"))
            {
                using (StreamReader Permit_Reader = new StreamReader(file_path))
                {
                    PermitCatalogue.Add_Permit(Permit_Reader);
                }
            }
            else
            {
                StandardError.Invoke_Message(SSE.ERROR_11);
            }
        }

        public static void Add_Permit(FileInfo file_info)
        {
            if (file_info.Exists && (file_info.Name.ToUpper() == "PERMIT.TXT"))
            {
                using (StreamReader Permit_Reader = new StreamReader(file_info.FullName))
                {
                    PermitCatalogue.Add_Permit(Permit_Reader);
                }
            }
            else
            {
                StandardError.Invoke_Message(SSE.ERROR_11);
            }
        }

        private static void Add_Permit(StreamReader permit_reader)
        {
            string? Date_Sentence = permit_reader.ReadLine();
            string? Version_Sentence = permit_reader.ReadLine();
            string? ENC_Header = permit_reader.ReadLine();

            bool Suitable_DateSentence = (!string.IsNullOrEmpty(Date_Sentence) && Date_Sentence.ToUpper().StartsWith(":DATE"));
            bool Suitable_VersionSentence = (!string.IsNullOrEmpty(Version_Sentence) && Version_Sentence.ToUpper().StartsWith(":VERSION"));
            bool Suitable_EncHeader = (!string.IsNullOrEmpty(ENC_Header) && ENC_Header.ToUpper().StartsWith(":ENC"));

            if (Suitable_DateSentence && Suitable_VersionSentence && Suitable_EncHeader)
            {
                Cipher.BlowFish.Initialize(HWID + HWID[0]);

                while (true)
                {
                    string? ENC_Sentence = permit_reader.ReadLine();

                    if (ENC_Sentence != null)
                    {
                        if (!string.IsNullOrEmpty(ENC_Sentence))
                        {
                            if (ENC_Sentence.ToUpper().StartsWith(":ECS")) { break; }

                            ENC.CellPermit Permit = new ENC.CellPermit();
                            string[] ENC_Segment = ENC_Sentence.Split(',');

                            Permit.Type = 0;

                            if (ENC_Segment.Length > 0) {
                                if (ENC_Segment[0].Length == 64) {
                                    string Data_Section = ENC_Segment[0][..48];

                                    Permit.Name = Data_Section[0..8];
                                    Permit.Expiration_Date = Data_Section[8..16];
                                    Permit.Key.X = Data_Section[16..32];
                                    Permit.Key.Y = Data_Section[32..];
                                    Permit.Checksum = ENC_Segment[0][48..];

                                    if (ENC_Segment.Length > 1) { Permit.Service_Level = int.TryParse(ENC_Segment[1], out int Service_Level) ? Service_Level : -1; }
                                    if (ENC_Segment.Length > 2) { Permit.EDTN = int.TryParse(ENC_Segment[2], out int EDTN) ? EDTN : -1; }
                                    if (ENC_Segment.Length > 3) { Permit.DSID = ENC_Segment[3]; }
                                    if (ENC_Segment.Length > 4) { Permit.Comment = ENC_Segment[4]; }

                                    if (Cipher.CRC_32.Validate_CRC(Data_Section, Permit.Checksum, true))
                                    {
                                        Permit.Key.X = Cipher.BlowFish.Decrypt_Key(Convert.FromHexString(Permit.Key.X));
                                        Permit.Key.Y = Cipher.BlowFish.Decrypt_Key(Convert.FromHexString(Permit.Key.Y));

                                        if (!string.IsNullOrEmpty(Permit.Key.X) && !string.IsNullOrEmpty(Permit.Key.Y))
                                        {
                                            Permit.Error = Expiration(Permit);

                                            switch (Permit.Error)
                                            {
                                                case 1:
                                                    {
                                                        StandardError.Invoke_Message(SSE.ERROR_20, Permit.Name);
                                                    }
                                                    break;
                                                case 4:
                                                    {
                                                        StandardError.Invoke_Message(SSE.ERROR_15, Permit.Name);
                                                        StandardError.Invoke_Message(SSE.ERROR_25, Permit.Name);
                                                    }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            Permit.Error = 2;
                                        }
                                    }
                                    else
                                    {
                                        Permit.Error = 2;

                                        StandardError.Invoke_Message(SSE.ERROR_13, Permit.Name);
                                    }
                                }
                                else {
                                    if (ENC_Segment[0].Length > 7) { Permit.Name = ENC_Segment[0][0..8]; }

                                    Permit.Error = 2;

                                    if (!string.IsNullOrEmpty(Permit.Name))
                                    {
                                        StandardError.Invoke_Message(SSE.ERROR_12, Permit.Name);
                                    }
                                    else
                                    {
                                        StandardError.Invoke_Message(SSE.ERROR_12);
                                    }
                                }
                            }
                            else {
                                Permit.Error = 2;

                                StandardError.Invoke_Message(SSE.ERROR_12);
                            }

                            if (!string.IsNullOrEmpty(Permit.Name))
                            {
                                if (!Catalogue.ContainsKey(Permit.Name))
                                {
                                    Catalogue.Add(Permit.Name, new List<ENC.CellPermit>());
                                    Catalogue[Permit.Name].Add(Permit);
                                }
                                else
                                {
                                    IEnumerable<ENC.CellPermit> CellPermit_Enumeration = Catalogue[Permit.Name].Where(Cell_Permit => Cell_Permit.DSID == Permit.DSID);

                                    if (CellPermit_Enumeration.Count() > 0)
                                    {
                                        ENC.CellPermit[] CellPermit_Collection = CellPermit_Enumeration.ToArray();
                                        ENC.CellPermit Selected_Permit = Permit;

                                        Catalogue[Permit.Name].RemoveAll(Cell_Permit => (Cell_Permit.DSID == Permit.DSID));

                                        foreach (ENC.CellPermit Cell_Permit in CellPermit_Collection)
                                        {
                                            if (Cell_Permit.Error != 2)
                                            {
                                                if (Selected_Permit.Error == 2)
                                                {
                                                    Selected_Permit = Cell_Permit;
                                                }
                                                else
                                                {
                                                    bool Suitable_SelectedExpirationDate = DateTime.TryParseExact(Selected_Permit.Expiration_Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Selected_ExpirationDate);
                                                    bool Suitable_ReferenceExpirationDate = DateTime.TryParseExact(Cell_Permit.Expiration_Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Reference_ExpirationDate);

                                                    if (Suitable_SelectedExpirationDate && Suitable_ReferenceExpirationDate)
                                                    {
                                                        if (Selected_ExpirationDate < Reference_ExpirationDate)
                                                        {
                                                            Selected_Permit = Cell_Permit;
                                                        }
                                                    }
                                                    else if (!Suitable_SelectedExpirationDate && Suitable_ReferenceExpirationDate)
                                                    {
                                                        Selected_Permit = Cell_Permit;
                                                    }
                                                }
                                            }
                                        }

                                        Catalogue[Permit.Name].Add(Selected_Permit);
                                    }
                                    else
                                    {
                                        Catalogue[Permit.Name].Add(Permit);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (Catalogue.Count < 1)
                {
                    StandardError.Invoke_Message(SSE.ERROR_11);
                }
            }
            else
            {
                StandardError.Invoke_Message(SSE.ERROR_12);
            }
        }


        private static int Expiration(ENC.CellPermit permit)
        {
            if (!string.IsNullOrEmpty(permit.Expiration_Date))
            {
                DateTime Now = DateTime.UtcNow;
                
                if (DateTime.TryParseExact(permit.Expiration_Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime Expiration_Date))
                {
                    DateTime reference = Expiration_Date.ToUniversalTime();

                    if (Now > reference)
                    {
                        return 4; // Permit이 만료되어도 사용은 가능하다 (S-63 10.5.5 Edition 3.0). 따라서 주의로 표시한다.
                    }
                    else if ((reference - Now).Days <= 30)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return 1;
            }
        }
    }
}