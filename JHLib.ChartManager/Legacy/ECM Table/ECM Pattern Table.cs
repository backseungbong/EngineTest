using System.IO;

namespace Legacy.ECM_Core.Table
{
    public static class PatternTable
    {
        public static Dictionary<string, int> Table { get; private set; } = new Dictionary<string, int>();

        public static bool Loaded { get; private set; } = false;



        public static void Read(string file_path)
        {
            Loaded = false;

            Table.Clear();

            using (StreamReader Table_Reader = new StreamReader(file_path))
            {
                PatternTable.Read(Table_Reader);
            }

            Loaded = true;
        }

        public static void Read(Stream file_stream)
        {
            Loaded = false;

            Table.Clear();

            using (StreamReader Table_Reader = new StreamReader(file_stream))
            {
                PatternTable.Read(Table_Reader);
            }

            Loaded = true;
        }

        private static void Read(StreamReader table_reader)
        {
            int Index = 0;

            while (true)
            {
                string? Data_Sentence = table_reader.ReadLine();

                if (Data_Sentence != null)
                {
                    if (!string.IsNullOrEmpty(Data_Sentence))
                    {
                        string[] Data_Segment = Data_Sentence.Split(',');

                        if ((Data_Segment.Length > 0) && (!string.IsNullOrEmpty(Data_Segment[0])))
                        {
                            Table.TryAdd(Data_Segment[0], Index++);
                        }
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