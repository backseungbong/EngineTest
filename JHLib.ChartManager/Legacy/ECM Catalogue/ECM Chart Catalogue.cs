using Legacy.ECM_Core.Chart;
using System.IO;
using System.Text;

namespace Legacy.ECM_Core.Catalogue
{
    public static class ChartCatalogue
    {
        public static Dictionary<string, ENC.Chart> Catalogue { get; private set; } = new Dictionary<string, ENC.Chart>();
        public static Dictionary<int, List<ENC.ChartCoverage>> Coverage_Table { get; private set; } = new Dictionary<int, List<ENC.ChartCoverage>>();

        public static bool Loaded { get; private set; } = false;
        public static bool Overlapped { get; private set; } = false;



        public static void Read(string file_path)
        {
            Loaded = false;
            Overlapped = false;

            Catalogue.Clear();
            Coverage_Table.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_path))
            {
                ChartCatalogue.Read(Catalogue_Reader);
            }

            Loaded = true;
        }

        public static void Read(Stream file_stream)
        {
            Loaded = false;
            Overlapped = false;

            Catalogue.Clear();
            Coverage_Table.Clear();

            using (StreamReader Catalogue_Reader = new StreamReader(file_stream))
            {
                ChartCatalogue.Read(Catalogue_Reader);
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
                        ENC.Chart Chart = new ENC.Chart();
                        string[] Data_Segment = Data_Sentence.Split(',');

                        if (Data_Segment.Length > 0) { if (!string.IsNullOrEmpty(Data_Segment[0])) { Chart.Name = Data_Segment[0]; } }
                        if (Data_Segment.Length > 1) { Chart.Usage = int.TryParse(Data_Segment[1], out int Usage) ? Usage : -1; }
                        if (Data_Segment.Length > 2) { Chart.Scale = int.TryParse(Data_Segment[2], out int Scale) ? Scale : -1; }
                        if (Data_Segment.Length > 3) { Chart.Comf = int.TryParse(Data_Segment[3], out int Comf) ? Comf : -1; }
                        if (Data_Segment.Length > 4) { Chart.Base.EDTN = int.TryParse(Data_Segment[4], out int EDTN) ? EDTN : -1; }
                        if (Data_Segment.Length > 4) { Chart.Base.UPDN = 0; }
                        if (Data_Segment.Length > 5) { Chart.Update = int.TryParse(Data_Segment[5], out int Update) ? Update : -1; }
                        if (Data_Segment.Length > 6) { if (!string.IsNullOrEmpty(Data_Segment[6])) { Chart.Issue_Date = Data_Segment[6]; } }
                        if (Data_Segment.Length > 7) { if (!string.IsNullOrEmpty(Data_Segment[7])) { Chart.Update_Date = Data_Segment[7]; } }
                        if (Data_Segment.Length > 8) { if (!string.IsNullOrEmpty(Data_Segment[8])) { Chart.S57_Version = Data_Segment[8]; } }
                        if (Data_Segment.Length > 9) { Chart.Agency = int.TryParse(Data_Segment[9], out int Agency) ? Agency : -1; }
                        if (Data_Segment.Length > 10) { Chart.Center_Point.X = int.TryParse(Data_Segment[10], out int X) ? X : 0; }
                        if (Data_Segment.Length > 11) { Chart.Center_Point.Y = int.TryParse(Data_Segment[11], out int Y) ? Y : 0; }
                        if (Data_Segment.Length > 12) { Chart.HDAT = int.TryParse(Data_Segment[12], out int HDAT) ? HDAT : 0; }
                        if (Data_Segment.Length > 13) { Chart.VDAT = int.TryParse(Data_Segment[13], out int VDAT) ? VDAT : 0; }
                        if (Data_Segment.Length > 14) { Chart.SDAT = int.TryParse(Data_Segment[14], out int SDAT) ? SDAT : 0; }
                        if (Data_Segment.Length > 15) { Chart.DUNI = int.TryParse(Data_Segment[15], out int DUNI) ? DUNI : 0; }
                        if (Data_Segment.Length > 16) { Chart.HUNI = int.TryParse(Data_Segment[16], out int HUNI) ? HUNI : 0; }
                        if (Data_Segment.Length > 17) { Chart.PUNI = int.TryParse(Data_Segment[17], out int PUNI) ? PUNI : 0; }
                        if (Data_Segment.Length > 18) { Chart.Boundary.West = double.TryParse(Data_Segment[18], out double West) ? West : 0.0; }
                        if (Data_Segment.Length > 19) { Chart.Boundary.North = double.TryParse(Data_Segment[19], out double North) ? North : 0.0; }
                        if (Data_Segment.Length > 20) { Chart.Boundary.East = double.TryParse(Data_Segment[20], out double East) ? East : 0.0; }
                        if (Data_Segment.Length > 21) { Chart.Boundary.South = double.TryParse(Data_Segment[21], out double South) ? South : 0.0; }
                        if (Data_Segment.Length > 22) { if (!string.IsNullOrEmpty(Data_Segment[22])) { Chart.Reference_Date = Data_Segment[22]; } }
                        if (Data_Segment.Length > 23) { Chart.Overlap = int.TryParse(Data_Segment[23], out int Overlap) && (Overlap > 0); }

                        if (Chart.Name != null)
                        {
                            Catalogue.TryAdd(Chart.Name, Chart);

                            if ((0 < Chart.Usage) && (Chart.Usage < 7))
                            {
                                ENC.ChartCoverage Chart_Coverage = new ENC.ChartCoverage();

                                Chart_Coverage.Name = Chart.Name;
                                Chart_Coverage.Usage = Chart.Usage;
                                Chart_Coverage.Scale = Chart.Scale;
                                Chart_Coverage.Base = Chart.Base;

                                if (!string.IsNullOrEmpty(Chart.Issue_Date)) { Chart_Coverage.Issue_Date = Chart.Issue_Date; }

                                Chart_Coverage.Path = new JHLib.Util.Struct.Float2D[4];
                                Chart_Coverage.Path[0].X = (float)(Chart.Boundary.West / 10000000.0);
                                Chart_Coverage.Path[0].Y = (float)(Chart.Boundary.North / 10000000.0);
                                Chart_Coverage.Path[1].X = (float)(Chart.Boundary.East / 10000000.0);
                                Chart_Coverage.Path[1].Y = (float)(Chart.Boundary.North / 10000000.0);
                                Chart_Coverage.Path[2].X = (float)(Chart.Boundary.East / 10000000.0);
                                Chart_Coverage.Path[2].Y = (float)(Chart.Boundary.South / 10000000.0);
                                Chart_Coverage.Path[3].X = (float)(Chart.Boundary.West / 10000000.0);
                                Chart_Coverage.Path[3].Y = (float)(Chart.Boundary.South / 10000000.0);

                                if (!Coverage_Table.ContainsKey(Chart_Coverage.Usage)) { Coverage_Table.Add(Chart_Coverage.Usage, new List<ENC.ChartCoverage>()); }

                                Coverage_Table[Chart_Coverage.Usage].Add(Chart_Coverage);
                            }
                        }

                        if (Chart.Overlap) { Overlapped = true; }
                    }
                }
                else
                {
                    break;
                }
            }
        }


        public static void Set_Catalogue(DetectionChart chart, (int X, int Y) pivot)
        {
            ENC.Chart Chart = new ENC.Chart()
            {
                Name = chart.Name,
                Usage = chart.DSID.INTU,
                Scale = (int)chart.DSPM.CSCL,
                Comf = (int)chart.DSPM.COMF,
                Base = chart.Base,
                Update = chart.Update,
                Issue_Date = chart.DSID.ISDT,
                Update_Date = chart.Update_Date,
                S57_Version = chart.DSID.STED,
                Agency = chart.DSID.AGEN,
                Center_Point = pivot,
                HDAT = chart.DSPM.HDAT,
                VDAT = chart.DSPM.VDAT,
                SDAT = chart.DSPM.SDAT,
                DUNI = chart.DSPM.DUNI,
                HUNI = chart.DSPM.HUNI,
                PUNI = chart.DSPM.PUNI,
                Boundary = chart.Boundary,
                Reference_Date = "", // 쓰이는 곳이 없는데?
            };

            if (!Overlapped && (Chart.Usage > 0))
            {
                if (Coverage_Table.TryGetValue(Chart.Usage, out List<ENC.ChartCoverage>? Chart_Coverage) && (Chart_Coverage != null))
                {
                    JHLib.Util.Geometry.Clipper2.Clipper2 Clipper = new JHLib.Util.Geometry.Clipper2.Clipper2();

                    JHLib.Util.Struct.Float2D[] Area = new JHLib.Util.Struct.Float2D[4];
                    Area[0].X = (float)Chart.Boundary.West / 10000000.0f;
                    Area[0].Y = (float)Chart.Boundary.North / 10000000.0f;
                    Area[1].X = (float)Chart.Boundary.East / 10000000.0f;
                    Area[1].Y = (float)Chart.Boundary.North / 10000000.0f;
                    Area[2].X = (float)Chart.Boundary.East / 10000000.0f;
                    Area[2].Y = (float)Chart.Boundary.South / 10000000.0f;
                    Area[3].X = (float)Chart.Boundary.West / 10000000.0f;
                    Area[3].Y = (float)Chart.Boundary.South / 10000000.0f;

                    foreach (ENC.ChartCoverage Coverage in Chart_Coverage)
                    {
                        Clipper.Clear();

                        Clipper.AddSubject(Area);
                        Clipper.AddClip(Coverage.Path);

                        if (Clipper.Execute(0, 0))
                        {
                            Chart.Overlap = true;
                            break;
                        }
                    }
                }
                else
                {
                    Chart.Overlap = false;
                }
            }
            else
            {
                Chart.Overlap = false;
            }

            if (Catalogue.ContainsKey(Chart.Name))
            {
                Catalogue.Remove(Chart.Name);
            }

            Catalogue.TryAdd(Chart.Name, Chart);
        }


        public static void Save_Catalogue(string file_path)
        {
            FileInfo Catalogue_FileInfo = new FileInfo(file_path);

            if (Catalogue_FileInfo.Directory?.Exists == false) { Catalogue_FileInfo.Directory.Create(); }

            using (StreamWriter Writer = new StreamWriter(Catalogue_FileInfo.FullName, false))
            {
                IEnumerable<KeyValuePair<string, ENC.Chart>> KR_Enumeration = Catalogue.Where(Catalogue => Catalogue.Key.StartsWith("KR")).Select(Catalogue => Catalogue);
                IEnumerable<KeyValuePair<string, ENC.Chart>> Generic_Enumeration = Catalogue.Where(Catalogue => !Catalogue.Key.StartsWith("KR")).Select(Catalogue => Catalogue);

                foreach (KeyValuePair<string, ENC.Chart> KR_Chart in KR_Enumeration)
                {
                    Write_Catalogue(Writer, KR_Chart);
                }

                foreach (KeyValuePair<string, ENC.Chart> Generic_Chart in Generic_Enumeration)
                {
                    Write_Catalogue(Writer, Generic_Chart);
                }
            }
        }

        private static void Write_Catalogue(StreamWriter writer, KeyValuePair<string, ENC.Chart> chart)
        {
            StringBuilder Data_Builder = new StringBuilder();

            Data_Builder.Append(chart.Key); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Usage); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Scale); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Comf); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Base.EDTN); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Update); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Issue_Date); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Update_Date); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.S57_Version); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Agency); Data_Builder.Append(',');
            Data_Builder.Append($"{chart.Value.Center_Point.X:0.#}"); Data_Builder.Append(',');
            Data_Builder.Append($"{chart.Value.Center_Point.Y:0.#}"); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.HDAT); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.VDAT); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.SDAT); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.DUNI); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.HUNI); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.PUNI); Data_Builder.Append(',');
            Data_Builder.Append($"{chart.Value.Boundary.West:0.#}"); Data_Builder.Append(',');
            Data_Builder.Append($"{chart.Value.Boundary.North:0.#}"); Data_Builder.Append(',');
            Data_Builder.Append($"{chart.Value.Boundary.East:0.#}"); Data_Builder.Append(',');
            Data_Builder.Append($"{chart.Value.Boundary.South:0.#}"); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Reference_Date); Data_Builder.Append(',');
            Data_Builder.Append(chart.Value.Overlap ? '1' : '0');

            writer.WriteLine(Data_Builder);
            writer.Flush();
        }
    }
}