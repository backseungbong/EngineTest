using System.IO;
using System.Text.RegularExpressions;

namespace Legacy.ECM_Core.Table
{
    public static class ASTable
    {
        public static List<ENC.Lookup> Table { get; private set; } = new List<ENC.Lookup>();

        public static bool Loaded { get; private set; } = false;



        public static void Read(string file_path)
        {
            Loaded = false;

            Table.Clear();

            using (StreamReader Table_Reader = new StreamReader(file_path))
            {
                ASTable.Read(Table_Reader);
            }

            Loaded = true;
        }

        public static void Read(Stream file_stream)
        {
            Loaded = false;

            Table.Clear();

            using (StreamReader Table_Reader = new StreamReader(file_stream))
            {
                ASTable.Read(Table_Reader);
            }

            Loaded = true;
        }

        private static void Read(StreamReader table_reader)
        {
            Regex Command_Regex = new Regex(@"(\b[A-Z]{2}\b)\(([^\(\)]+)\)");

            while (true)
            {
                string? Data_Sentence = table_reader.ReadLine();

                if (Data_Sentence != null)
                {
                    if (!string.IsNullOrEmpty(Data_Sentence))
                    {
                        ENC.Lookup Lookup = new ENC.Lookup();
                        string[] Data_Segment = Data_Sentence.Replace("\"", "").Replace("“", "").Replace("”", "").Split('|');

                        if (Data_Segment.Length > 0) { if (!string.IsNullOrEmpty(Data_Segment[0])) { Lookup.Acronym = Data_Segment[0]; } }
                        if (Data_Segment.Length > 1) { LookupTable.Extract_LookupAttribute(Data_Segment[1], ref Lookup); }
                        if (Data_Segment.Length > 2) { LookupTable.Extract_LookupCommand(Data_Segment[2], Command_Regex, ref Lookup); }
                        if (Data_Segment.Length > 3) { Lookup.Display_Group = byte.TryParse(Data_Segment[3], out byte Display_Group) ? Display_Group : (byte)0; }
                        if (Data_Segment.Length > 4) { Lookup.Radar_Overlay = (Data_Segment[4] == "O") ? (byte)1 : (byte)0; }
                        if (Data_Segment.Length > 5) {
                            Lookup.Display_Category = Data_Segment[5] switch {
                                "DISPLAYBASE" => 0,
                                "STANDARD" => 1,
                                _ => 2,
                            };
                        }
                        if (Data_Segment.Length > 6) { Lookup.Group_Layer = int.TryParse(Data_Segment[6], out int Group_Layer) ? Group_Layer : -1; }

                        Table.Add(Lookup);
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}