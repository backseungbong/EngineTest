using System.IO;

namespace Legacy.ECM_Core.Catalogue
{
    public static class CellCatalogue
    {
        public static Dictionary<string, List<ENC.Cell>> Catalogue { get; private set; } = new Dictionary<string, List<ENC.Cell>>();
        public static List<ENC.Content> Sub_Content { get; private set; } = new List<ENC.Content>();



        public static void Load(string file_path)
        {
            using (StreamReader Catalogue_Reader = new StreamReader(file_path))
            {
                List<ENC.Cell> Cell_Collection = CellCatalogue.Read(Catalogue_Reader);

                foreach (ENC.Cell Cell in Cell_Collection)
                {
                    string Chart_Name = Path.GetFileNameWithoutExtension(Cell.File) ?? string.Empty;

                    if (!string.IsNullOrEmpty(Chart_Name))
                    {
                        switch (Cell.IMPL)
                        {
                            case "BIN":
                                {
                                    ENC.Cell Cell_Value = Cell;
                                    ENC.Serial? Serial = CellCatalogue.Read_CellSerial(file_path);

                                    if (Serial != null)
                                    {
                                        Cell_Value.Provider = Serial.Value.Provider;
                                        Cell_Value.Provide_Type = Serial.Value.Type;
                                        Cell_Value.Week = Serial.Value.Week;
                                    }

                                    if (!Catalogue.ContainsKey(Chart_Name)) { Catalogue.Add(Chart_Name, new List<ENC.Cell>()); }

                                    Cell_Value.Catalogue_File = file_path;

                                    Catalogue[Chart_Name].Add(Cell_Value);
                                }
                                break;
                            case "TXT":
                            case "TIF":
                                {
                                    ENC.Content Content = new ENC.Content()
                                    {
                                        RCNM = Cell.RCNM,
                                        RCID = Cell.RCID,
                                        IMPL = Cell.IMPL,
                                        Full_Name = Path.Combine(Path.GetDirectoryName(file_path) ?? "", Cell.File),
                                    };

                                    Sub_Content.Add(Content);
                                }
                                break;
                        }
                    }
                }
            }
        }

        public static void Load(string file_path, ENC.Media media)
        {
            using (StreamReader Catalogue_Reader = new StreamReader(file_path))
            {
                List<ENC.Cell> Cell_Collection = CellCatalogue.Read(Catalogue_Reader);

                foreach (ENC.Cell Cell in Cell_Collection)
                {
                    string Chart_Name = Path.GetFileNameWithoutExtension(Cell.File) ?? string.Empty;

                    if (!string.IsNullOrEmpty(Chart_Name))
                    {
                        switch (Cell.IMPL)
                        {
                            case "BIN":
                                {
                                    ENC.Cell Cell_Value = Cell;
                                    Cell_Value.Provider = media.Header.Server_ID;
                                    Cell_Value.Provide_Type = media.Header.Media_Type;
                                    Cell_Value.Week = media.Header.Week;
                                    Cell_Value.Catalogue_File = file_path;

                                    if (!Catalogue.ContainsKey(Chart_Name)) { Catalogue.Add(Chart_Name, new List<ENC.Cell>()); }

                                    Catalogue[Chart_Name].Add(Cell_Value);
                                }
                                break;
                            case "TXT":
                            case "TIF":
                                {
                                    ENC.Content Content = new ENC.Content()
                                    {
                                        RCNM = Cell.RCNM,
                                        RCID = Cell.RCID,
                                        IMPL = Cell.IMPL,
                                        Full_Name = Path.Combine(Path.GetDirectoryName(file_path) ?? "", Cell.File),
                                    };

                                    Sub_Content.Add(Content);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private static List<ENC.Cell> Read(StreamReader catalogue_reader)
        {
            List<ENC.Cell> Extract = new List<ENC.Cell>();

            string Read_Data = catalogue_reader.ReadToEnd();

            if ((Read_Data.Length > 6) && (Read_Data[6] == 'L') && int.TryParse(Read_Data.Substring(0, 5), out int Data_Start_Index))
            {
                string[] Data_Sentence = Read_Data.Remove(0, Data_Start_Index).Split('\u001E');

                for (int i = 0; i < Data_Sentence.Length - 2; i += 3)
                {
                    if (Data_Sentence[i].Contains("CATD") && (Data_Sentence.Length > (i + 2)))
                    {
                        ENC.Cell Cell = new ENC.Cell();
                        string[] Data_Segment = Data_Sentence[i + 2].Split('\u001F');

                        if (Data_Segment.Length > 0) {
                            if (Data_Segment[0].Length > 0) { Cell.RCNM = Data_Segment[0][0].ToString(); }
                            if (Data_Segment[0].Length > 12) { Cell.File = Data_Segment[0][12..]; }
                        }
                        if (Data_Segment.Length > 1) { Cell.LFile = Data_Segment[1]; }
                        if (Data_Segment.Length > 2) { Cell.VOLM = Data_Segment[2]; }
                        if (Data_Segment.Length > 3) {
                            if (Data_Segment[3].Length > 2) { Cell.IMPL = Data_Segment[3][..3]; }
                        }
                        if (Data_Segment.Length > 7) { Cell.CRC = Data_Segment[7]; }
                        if (Data_Segment.Length > 8) { // 63은 cell이 암호화되어 있어서 사전에 DSID를 못 보니까 이렇게 볼 수 있게 만들었다고 함. 57 031에는 없는 이유는 암호화가 안 되어 있으니 직접 볼 수 있어서 그런 것인듯
                            Cell.Comment = Data_Segment[8];

                            string[] Comment_Segment = Cell.Comment.Split(',');

                            if (Comment_Segment.Length > 0) {
                                if (Comment_Segment[0].ToUpper().StartsWith("VERSION=") && (Comment_Segment[0].Length > 8)) { Cell.Version = Comment_Segment[0][8..]; }
                            }
                            if (Comment_Segment.Length > 1) {
                                if (Comment_Segment[1].ToUpper().StartsWith("EDTN=") && (Comment_Segment[1].Length > 5)) { Cell.EDTN = int.TryParse(Comment_Segment[1][5..], out int EDTN) ? EDTN : -1; }
                            }
                            if (Comment_Segment.Length > 2) {
                                if (Comment_Segment[2].ToUpper().StartsWith("UPDN=") && (Comment_Segment[2].Length > 5)) { Cell.UPDN = int.TryParse(Comment_Segment[2][5..], out int UPDN) ? UPDN : -1; }
                            }
                        }

                        if (Cell.IMPL?.Contains("BIN") == true)
                        {
                            Cell.RCID = int.TryParse(Data_Segment[0].Substring(2, 10), out int RCID) ? RCID : -1;

                            if (Data_Segment[3].Length > 3) { Cell.Boundary.South = double.TryParse(Data_Segment[3][3..], out double South) ? South : 0.0; }
                            if (Data_Segment.Length > 4) { Cell.Boundary.West = double.TryParse(Data_Segment[4], out double West) ? West : 0.0; }
                            if (Data_Segment.Length > 5) { Cell.Boundary.North = double.TryParse(Data_Segment[5], out double North) ? North : 0.0; }
                            if (Data_Segment.Length > 6) { Cell.Boundary.East = double.TryParse(Data_Segment[6], out double East) ? East : 0.0; }

                            Extract.Add(Cell);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(Cell.CRC))
                            {
                                switch (Cell.IMPL)
                                {
                                    case "TXT":
                                    case "TIF":
                                        {
                                            Cell.RCID = int.TryParse(Data_Segment[0].Substring(2, 10), out int RCID) ? RCID : -1;

                                            Extract.Add(Cell);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return Extract;
        }

        private static ENC.Serial? Read_CellSerial(string catalogue_file)
        {
            ENC.Serial? Extract = null;

            FileInfo Catalogue_FileInfo = new FileInfo(catalogue_file);
            FileInfo Serial_FileInfo = new FileInfo(Path.Combine(Catalogue_FileInfo.DirectoryName ?? "", "..", "SERIAL.ENC"));

            if (Serial_FileInfo.Exists)
            {
                using (StreamReader Serial_Reader = new StreamReader(Serial_FileInfo.OpenRead()))
                {
                    ENC.Serial Serial = new ENC.Serial();

                    while (true)
                    {
                        string? Data_Sentence = Serial_Reader.ReadLine();

                        if (Data_Sentence != null)
                        {
                            if (Data_Sentence.Length > 29)
                            {
                                Serial.Provider = Data_Sentence[0..2];
                                Serial.Issue_Date = Data_Sentence[12..20].Trim();
                                Serial.Type = Data_Sentence[20..30].Trim().ToUpper();

                                string Year = Data_Sentence[7..9];
                                string Week = Data_Sentence[4..6];

                                Serial.Week = int.TryParse(Year + Week, out int Week_Value) ? Week_Value : -1;
                            }

                            if (Data_Sentence.Length > 35)
                            {
                                string Number = Data_Sentence[35..].Trim();

                                if (Number.Length > 5)
                                {
                                    string Current = Number[1..3];
                                    string Total = Number[4..6];

                                    Serial.Current_Number = int.TryParse(Current, out int Current_Number) ? Current_Number : -1;
                                    Serial.Total_Number = int.TryParse(Total, out int Total_Number) ? Total_Number : -1;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    Extract = Serial;
                }
            }

            return Extract;
        }
    }
}