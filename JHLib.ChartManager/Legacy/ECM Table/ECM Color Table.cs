using System.IO;

namespace Legacy.ECM_Core.Table
{
    public static class ColorTable
    {
        public static Dictionary<string, int> Table { get; private set; } = new Dictionary<string, int>();

        public static bool Loaded { get; private set; } = false;



        public static void Read(string file_path)
        {
            Loaded = false;

            Table.Clear();

            using (StreamReader Table_Reader = new StreamReader(file_path))
            {
                ColorTable.Read(Table_Reader);
            }

            Loaded = true;
        }

        public static void Read(Stream file_stream)
        {
            Loaded = false;

            Table.Clear();

            using (StreamReader Table_Reader = new StreamReader(file_stream))
            {
                ColorTable.Read(Table_Reader);
            }

            Loaded = true;
        }

        private static void Read(StreamReader table_reader)
        {
            int Index = 0;

            while (true)
            {
                string? Acronym = table_reader.ReadLine();

                if (Acronym != null)
                {
                    if (!string.IsNullOrEmpty(Acronym))
                    {
                        Table.TryAdd(Acronym, Index++);
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