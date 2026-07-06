using Legacy.ECM_Core.Definition;
using System.IO;
using System.Text;

namespace Legacy.ECM_Core.Catalogue
{
    public static class MediaCatalogue
    {
        public static List<(string FilePath, ENC.Media Media)> Catalogue { get; private set; } = new List<(string FilePath, ENC.Media Media)>();



        public static void Read(params string[] file_path)
        {
            Catalogue.Clear();

            foreach (string File_Path in file_path)
            {
                using (StreamReader Catalogue_Reader = new StreamReader(File_Path))
                {
                    MediaCatalogue.Read(Catalogue_Reader, File_Path);
                }
            }
        }

        private static void Read(StreamReader catalogue_reader, string file_path)
        {
            string? Header_Sentence = catalogue_reader.ReadLine();

            if (Header_Sentence != null)
            {
                FileInfo Media_FileInfo = new FileInfo(file_path);
                DirectoryInfo? Media_DirectoryInfo = Media_FileInfo.Directory;

                ENC.Media Media = new ENC.Media();
                ENC.MediaHeader Header = new ENC.MediaHeader();

                if (Header_Sentence.Length > 1) { Header.Server_ID = Header_Sentence[..2].Trim(); }
                if (Header_Sentence.Length > 11) {
                    Header.Week_Of_Issue = Header_Sentence[2..12].Trim();

                    if (Header.Week_Of_Issue.Length > 6) {
                        string Year = Header.Week_Of_Issue[5..7];
                        string Week = Header.Week_Of_Issue[2..4];

                        Header.Week = int.TryParse(Year + Week, out int Week_Value) ? Week_Value : -1;
                    }
                }
                if (Header_Sentence.Length > 19) { Header.Date_Of_Issue = Header_Sentence[12..20].Trim(); }
                if (Header_Sentence.Length > 29) { Header.Media_Type = Header_Sentence[20..30].Trim(); }
                if (Header_Sentence.Length > 35) { Header.MLI = Header_Sentence[30..36].Trim(); }

                Header_Sentence = catalogue_reader.ReadLine();

                if (Header_Sentence != null)
                {
                    string[] Header_Segment = Header_Sentence.Split(',');

                    if (Header_Segment.Length > 0) { Header.Media_ID = Header_Segment[0]; }
                    if (Header_Segment.Length > 1) { Header.MRMN = Header_Segment[1]; }

                    while (true)
                    {
                        string? Data_Sentence = catalogue_reader.ReadLine();

                        if (Data_Sentence != null)
                        {
                            ENC.MediaRecord Record = new ENC.MediaRecord();
                            string[] Data_Segment = Data_Sentence.Split(',');

                            if (Data_Segment.Length > 0) {
                                string[] Sector_Segment = Data_Segment[0].Split(';');

                                if (Sector_Segment.Length > 0) { Record.Location = Sector_Segment[0]; }
                                if (Sector_Segment.Length > 1) { Record.Folder = Sector_Segment[1]; }
                            }
                            if (Data_Segment.Length > 1) { Record.Date = Data_Segment[1]; }
                            if (Data_Segment.Length > 2) { Record.Media_Number = Data_Segment[2].Replace("'", ""); }

                            if (Media.Record == null) { Media.Record = new List<ENC.MediaRecord>(); }

                            Media.Record.Add(Record);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                Media.Header = Header;

                if (Media_DirectoryInfo?.Exists == true)
                {
                    ProductCatalogue.Load(Path.Combine(Media_DirectoryInfo.FullName, "INFO", "PRODUCTS.TXT"));

                    if (Media.Header.Media_Type == "UPDATE")
                    {
                        List<ENC.Status> Media_Status = MediaCatalogue.Load_MediaStatus(Path.Combine(Media_DirectoryInfo.FullName, "INFO"), Media.Header.Server_ID ?? "", out bool Error);
                    }
                }

                Catalogue.Add((file_path, Media));
            }
        }

        private static List<ENC.Status> Load_MediaStatus(string status_directory, string media_server, out bool error)
        {
            error = false;

            List<ENC.Status> Media_Status = new List<ENC.Status>();
            FileInfo Status_FileInfo = new FileInfo(Path.Combine(status_directory, "STATUS.LST"));

            if (Status_FileInfo.Exists)
            {
                Dictionary<int, int> Media_Provide = Load_MediaProvide(media_server);

                using (StreamReader Status_Reader = new StreamReader(Status_FileInfo.OpenRead()))
                {
                    string? Header_Sentence = Status_Reader.ReadLine();

                    if (Header_Sentence != null)
                    {
                        while (true)
                        {
                            string? Data_Sentence = Status_Reader.ReadLine();

                            if (Data_Sentence != null)
                            {
                                ENC.Status Status = new ENC.Status();
                                string[] Data_Segment = Data_Sentence.Split(',');

                                if (Data_Segment.Length > 0) {
                                    if (Data_Segment[0].Length > 1) { Status.Base_Number = int.TryParse(Data_Segment[0][1..], out int Base_Number) ? Base_Number : -1; }
                                }
                                if (Data_Segment.Length > 1) { Status.Provider = Data_Segment[1]; }
                                if (Data_Segment.Length > 2) {
                                    if (Data_Segment[2].Length > 6) {
                                        string Year = Data_Segment[2][5..7];
                                        string Week = Data_Segment[2][2..4];

                                        Status.Week = int.TryParse(Year + Week, out int Week_Value) ? Week_Value : -1;
                                    }
                                }
                                if (Data_Segment.Length > 3) { Status.Message = Data_Segment[3]; }
                                if (Data_Segment.Length > 4) { Status.Issue_Date = Data_Segment[4]; }

                                Status.Week_Validation = 0;

                                if (Media_Provide.TryGetValue(Status.Base_Number, out int Provide_Week))
                                {
                                    if (Provide_Week < Status.Week)
                                    {
                                        Status.Base_Load = false;
                                        Status.Week_Validation = 2;
                                    }
                                    else if (Provide_Week > Status.Week)
                                    {
                                        Status.Base_Load = true;
                                        Status.Week_Validation = 3;
                                    }
                                    else if (Provide_Week == Status.Week)
                                    {
                                        Status.Base_Load = true;
                                        Status.Week_Validation = 1;
                                    }
                                }

                                if (!Status.Base_Load && (Status.Week_Validation == 0))
                                {
                                    error = true;
                                }

                                Media_Status.Add(Status);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return Media_Status;
        }

        public static Dictionary<int, int> Load_MediaProvide(string media_server)
        {
            Dictionary<int, int> Media_Provide = new Dictionary<int, int>();
            FileInfo Provide_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.SystemS63_Directory, $"{media_server}.ini"));

            if (Provide_FileInfo.Exists)
            {
                StringBuilder Profile_Builder = new StringBuilder();
                Kernel32.GetPrivateProfileString("BASE", "TOTAL", null, Profile_Builder, 32, Provide_FileInfo.FullName);

                if (int.TryParse(Profile_Builder.ToString(), out int Total))
                {
                    for (int i = 0; i < Total; i++)
                    {
                        Profile_Builder.Clear();
                        Kernel32.GetPrivateProfileString("BASE", $"{(i + 1):00}", null, Profile_Builder, 32, Provide_FileInfo.FullName);

                        if (int.TryParse(Profile_Builder.ToString(), out int Week))
                        {
                            Media_Provide.TryAdd(i + 1, Week);
                        }
                    }
                }
            }

            return Media_Provide;
        }
    }
}