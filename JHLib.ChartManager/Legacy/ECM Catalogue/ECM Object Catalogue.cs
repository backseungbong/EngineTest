using System.IO;

namespace Legacy.ECM_Core.Catalogue
{
    public static class ObjectCatalogue
    {
        public static Dictionary<int, ENC.Object> Catalogue { get; private set; } = new Dictionary<int, ENC.Object>();

        public static bool Loaded { get; private set; } = false;



        public static void Read(string file_path)
        {
            Loaded = false;

            Catalogue.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_path))
            {
                ObjectCatalogue.Read(Catalogue_Reader);
            }

            Loaded = true;
        }

        public static void Read(Stream file_stream)
        {
            Loaded = false;

            Catalogue.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_stream))
            {
                ObjectCatalogue.Read(Catalogue_Reader);
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
                        ENC.Object Object = new ENC.Object();
                        string[] Data_Segment = Data_Sentence.Split(':');

                        if (Data_Segment.Length > 0) { if (!string.IsNullOrEmpty(Data_Segment[0])) { Object.Acronym = Data_Segment[0]; } }
                        if (Data_Segment.Length > 1) { Object.Code = int.TryParse(Data_Segment[1], out int Code) ? Code : -1; }
                        if (Data_Segment.Length > 2) { if (!string.IsNullOrEmpty(Data_Segment[2])) { Object.Object_Type = Data_Segment[2]; } }
                        if (Data_Segment.Length > 3) { }
                        if (Data_Segment.Length > 4) { if (!string.IsNullOrEmpty(Data_Segment[4])) { Object.Object_Name = Data_Segment[4]; } }
                        if (Data_Segment.Length > 5) {
                            if (!string.IsNullOrEmpty(Data_Segment[5]))
                            {
                                if (Object.Object_Element == null) { Object.Object_Element = new List<string[]> { null, null, null }; }

                                Object.Object_Element[0] = Data_Segment[5].Split(';');
                            }
                        }
                        if (Data_Segment.Length > 6) {
                            if (!string.IsNullOrEmpty(Data_Segment[6]))
                            {
                                if (Object.Object_Element == null) { Object.Object_Element = new List<string[]> { null, null, null }; }

                                Object.Object_Element[1] = Data_Segment[6].Split(';');
                            }
                        }
                        if (Data_Segment.Length > 7) {
                            if (!string.IsNullOrEmpty(Data_Segment[7]))
                            {
                                if (Object.Object_Element == null) { Object.Object_Element = new List<string[]> { null, null, null }; }

                                Object.Object_Element[2] = Data_Segment[7].Split(';');
                            }
                        }
                        if (Data_Segment.Length > 8) {
                            if (!string.IsNullOrEmpty(Data_Segment[8]))
                            {
                                Object.Object_ShapeType = Data_Segment[8].Split(';');
                            }
                        }

                        if (Object.Code > -1)
                        {
                            Catalogue.TryAdd(Object.Code, Object);
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