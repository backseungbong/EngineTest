using System.IO;

namespace Legacy.ECM_Core.Catalogue
{
    public static class AttributeCatalogue
    {
        public static Dictionary<int, ENC.Attribute> Catalogue { get; private set; } = new Dictionary<int, ENC.Attribute>();

        public static bool Loaded { get; private set; } = false;



        public static void Read(string file_path)
        {
            Loaded = false;

            Catalogue.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_path))
            {
                AttributeCatalogue.Read(Catalogue_Reader);
            }

            Loaded = true;
        }

        public static void Read(Stream file_stream)
        {
            Loaded = false;

            Catalogue.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_stream))
            {
                AttributeCatalogue.Read(Catalogue_Reader);
            }

            Loaded = true;
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
                        ENC.Attribute Attribute = new ENC.Attribute();
                        string[] Data_Segment = Data_Sentence.Split(':');

                        if (Data_Segment.Length > 0) { if (!string.IsNullOrEmpty(Data_Segment[0])) { Attribute.Acronym = Data_Segment[0]; } }
                        if (Data_Segment.Length > 1) { Attribute.Code = int.TryParse(Data_Segment[1], out int Code) ? Code : -1; }
                        if (Data_Segment.Length > 2) { if (!string.IsNullOrEmpty(Data_Segment[2])) { Attribute.Object_Type = Data_Segment[2]; } }
                        if (Data_Segment.Length > 3) { if (!string.IsNullOrEmpty(Data_Segment[3])) { Attribute.Attribute_Type = Data_Segment[3]; } }
                        if (Data_Segment.Length > 4) { if (!string.IsNullOrEmpty(Data_Segment[4])) { Attribute.Attribute_Name = Data_Segment[4]; } }
                        if (Data_Segment.Length > 5) {
                            switch (Attribute.Attribute_Type)
                            {
                                case "E":
                                case "L":
                                    if (!string.IsNullOrEmpty(Data_Segment[5]))
                                    {
                                        Attribute.Attribute_Element = Data_Segment[5].Split('|');
                                    }
                                    break;
                            }
                        }
                        if (Data_Segment.Length > 6) { if (!string.IsNullOrEmpty(Data_Segment[6])) { Attribute.Attribute_Format = Data_Segment[6]; } }

                        if (Attribute.Code > -1)
                        {
                            Catalogue.TryAdd(Attribute.Code, Attribute);
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