using System.Text.RegularExpressions;

namespace Legacy.ECM_Core.Table
{
    public static class LookupTable
    {
        public static bool Get_Lookup(byte type, int index, bool chart1, out ENC.Lookup Lookup)
        {
            switch (type)
            {
                case 0:
                    if (index < PPTable.Table.Count)
                    {
                        Lookup = PPTable.Table[index];
                        return true;
                    }
                    break;
                case 1:
                    if (index < PSTable.Table.Count)
                    {
                        Lookup = PSTable.Table[index];
                        return true;
                    }
                    break;
                case 2:
                    if (index < LTable.Table.Count)
                    {
                        Lookup = LTable.Table[index];
                        return true;
                    }
                    break;
                case 3:
                    if (!chart1)
                    {
                        if (index < APTable.Table.Count)
                        {
                            Lookup = APTable.Table[index];
                            return true;
                        }
                    }
                    else
                    {
                        if (index < ATable.Table.Count)
                        {
                            Lookup = ATable.Table[index];
                            return true;
                        }
                    }
                    break;
                case 4:
                    if (!chart1)
                    {
                        if (index < ASTable.Table.Count)
                        {
                            Lookup = ASTable.Table[index];
                            return true;
                        }
                    }
                    break;
            }

            Lookup = new ENC.Lookup();

            return false;
        }

        public static bool Get_IndexSize(byte type, string acronym, bool chart1, out (int Index, int Size) index_size)
        {
            bool Result = false;

            List<ENC.Lookup>? Lookup = null;

            switch (type)
            {
                case 0: { Lookup = PPTable.Table; } break;
                case 1: { Lookup = PSTable.Table; } break;
                case 2: { Lookup = LTable.Table; } break;
                case 3: { Lookup = chart1 ? ATable.Table : APTable.Table; } break;
                case 4: { Lookup = chart1 ? null : ASTable.Table; } break;
            }

            if (Lookup == null)
            {
                index_size = (Index: -1, Size: -1);
            }
            else
            {
                index_size = (Index: -1, Size: 0);
                bool Find = false;

                for (int i = 0; i < Lookup.Count; i++)
                {
                    if (acronym == Lookup[i].Acronym)
                    {
                        if (!Find)
                        {
                            index_size.Index = i;
                            Find = true;
                        }

                        index_size.Size++;
                    }
                    else
                    {
                        if (Find) { break; }
                    }
                }

                Result = true;
            }

            return Result;
        }

        public static void Extract_LookupAttribute(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                string[] Data_Segment = data.Split(';');

                lookup.Attribute = new List<(string Acronym, string[] Element)>();

                foreach (string Attribute_Data in Data_Segment)
                {
                    if (Attribute_Data.Length > 6)
                    {
                        lookup.Attribute.Add((
                            Acronym: Attribute_Data.Substring(0, 6),
                            Element: Attribute_Data.Substring(6).Split(',')
                        ));
                    }
                    else
                    {
                        lookup.Attribute.Add((
                            Acronym: Attribute_Data.Substring(0, 6),
                            Element: Array.Empty<string>()
                        ));
                    }
                }
            }
        }

        public static void Extract_LookupCommand(string data, Regex regex, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                MatchCollection Lookup_MatchCollection = regex.Matches(data);

                foreach (Match Lookup_Command in Lookup_MatchCollection)
                {
                    switch (Lookup_Command.Groups[1].Value)
                    {
                        case "SY":
                            {
                                LookupTable.Extract_SY(Lookup_Command.Groups[2].Value, ref lookup);
                            }
                            break;
                        case "TE":
                            {
                                LookupTable.Extract_TE(Lookup_Command.Groups[2].Value, ref lookup);
                            }
                            break;
                        case "TX":
                            {
                                LookupTable.Extract_TX(Lookup_Command.Groups[2].Value, ref lookup);
                            }
                            break;
                        case "LS":
                            {
                                LookupTable.Extract_LS(Lookup_Command.Groups[2].Value, ref lookup);
                            }
                            break;
                        case "LC":
                            {
                                LookupTable.Extract_LC(Lookup_Command.Groups[2].Value, ref lookup);
                            }
                            break;
                        case "AC":
                            {
                                LookupTable.Extract_AC(Lookup_Command.Groups[2].Value, ref lookup);
                            }
                            break;
                        case "AP":
                            {
                                LookupTable.Extract_AP(Lookup_Command.Groups[2].Value, ref lookup);
                            }
                            break;
                        case "CS":
                            {
                                LookupTable.Extract_CS(Lookup_Command.Groups[2].Value, ref lookup);
                            }
                            break;
                    }
                }
            }
        }

        private static void Extract_SY(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (lookup.SY == null) { lookup.SY = new List<ENC.SY>(); }

                string[] Data_Segment = data.Split(',');
                ENC.SY ENC_SY = new ENC.SY();

                if (Data_Segment.Length > 0) { ENC_SY.Acronym = Data_Segment[0]; }
                if (Data_Segment.Length > 1) { ENC_SY.Degree = Data_Segment[1]; }

                lookup.SY.Add(ENC_SY);
            }
        }

        private static void Extract_TE(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (lookup.TE == null) { lookup.TE = new List<ENC.TE>(); }

                var matches = Regex.Matches(data, @"'[^']*'|[^,]+");
                var Data_Segment = matches.Select(m => m.Value.Trim('\'')).ToList();
                //string[] Data_Segment = data.Replace("\'", "").Replace("‘", "").Replace("’", "").Split(',');
                ENC.TE ENC_TE = new ENC.TE();

                if (Data_Segment.Count > 0) { ENC_TE.Format = Data_Segment[0]; }
                if (Data_Segment.Count > 1) { ENC_TE.Element = Data_Segment[1]; }
                if (Data_Segment.Count > 2) { ENC_TE.Font_HJUST = int.TryParse(Data_Segment[2], out int HJUST) ? HJUST : -1; }
                if (Data_Segment.Count > 3) { ENC_TE.Font_VJUST = int.TryParse(Data_Segment[3], out int VJUST) ? VJUST : -1; }
                if (Data_Segment.Count > 4) { ENC_TE.Font_SPACE = int.TryParse(Data_Segment[4], out int SPACE) ? SPACE : -1; }
                if (Data_Segment.Count > 5) { ENC_TE.Font_CHARS = Data_Segment[5]; }
                if (Data_Segment.Count > 6) { ENC_TE.Font_Offset.X = int.TryParse(Data_Segment[6], out int X) ? X : 0; }
                if (Data_Segment.Count > 7) { ENC_TE.Font_Offset.Y = int.TryParse(Data_Segment[7], out int Y) ? Y : 0; }
                if (Data_Segment.Count > 8) { ENC_TE.Font_ColorAcronym = Data_Segment[8]; }
                if (Data_Segment.Count > 9) { ENC_TE.Font_Group = int.TryParse(Data_Segment[9], out int Group) ? Group : -1; }

                lookup.TE.Add(ENC_TE);
            }
        }

        private static void Extract_TX(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (lookup.TX == null) { lookup.TX = new List<ENC.TX>(); }

                var matches = Regex.Matches(data, @"'[^']*'|[^,]+");
                var Data_Segment = matches.Select(m => m.Value).ToList();
                //string[] Data_Segment = data.Split(',');
                ENC.TX ENC_TX = new ENC.TX();

                if (Data_Segment.Count > 0) {
                    if (Data_Segment[0].Contains('\'') || Data_Segment[0].Contains('‘') || Data_Segment[0].Contains('’'))
                    {
                        ENC_TX.Text = Data_Segment[0].Replace("\'", "").Replace("‘", "").Replace("’", "");
                    }
                    else
                    {
                        ENC_TX.Element = Data_Segment[0];
                    }
                }
                if (Data_Segment.Count > 1) { ENC_TX.Font_HJUST = int.TryParse(Data_Segment[1], out int HJUST) ? HJUST : -1; }
                if (Data_Segment.Count > 2) { ENC_TX.Font_VJUST = int.TryParse(Data_Segment[2], out int VJUST) ? VJUST : -1; }
                if (Data_Segment.Count > 3) { ENC_TX.Font_SPACE = int.TryParse(Data_Segment[3], out int SPACE) ? SPACE : -1; }
                if (Data_Segment.Count > 4) { ENC_TX.Font_CHARS = Data_Segment[4].Replace("\'", "").Replace("‘", "").Replace("’", ""); }
                if (Data_Segment.Count > 5) { ENC_TX.Font_Offset.X = int.TryParse(Data_Segment[5], out int X) ? X : 0; }
                if (Data_Segment.Count > 6) { ENC_TX.Font_Offset.Y = int.TryParse(Data_Segment[6], out int Y) ? Y : 0; }
                if (Data_Segment.Count > 7) { ENC_TX.Font_ColorAcronym = Data_Segment[7]; }
                if (Data_Segment.Count > 8) { ENC_TX.Font_Group = int.TryParse(Data_Segment[8], out int Group) ? Group : -1; }

                lookup.TX.Add(ENC_TX);
            }
        }

        private static void Extract_LS(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (lookup.LS == null) { lookup.LS = new List<ENC.LS>(); }

                string[] Data_Segment = data.Split(',');
                ENC.LS ENC_LS = new ENC.LS();

                if (Data_Segment.Length > 0) {
                    ENC_LS.Pen_Type = Data_Segment[0] switch {
                        "SOLD" => 0,
                        "DASH" => 1,
                        "DOTT" => 2,
                        _ => -1,
                    };
                }
                if (Data_Segment.Length > 1) { ENC_LS.Pen_Width = int.TryParse(Data_Segment[1], out int Pen_Width) ? Pen_Width : -1; }
                if (Data_Segment.Length > 2) { ENC_LS.Pen_ColorAcronym = Data_Segment[2]; }

                lookup.LS.Add(ENC_LS);
            }
        }

        private static void Extract_LC(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (lookup.LC == null) { lookup.LC = new List<ENC.LC>(); }

                ENC.LC ENC_LC = new ENC.LC()
                {
                    Acronym = data,
                };

                lookup.LC.Add(ENC_LC);
            }
        }

        private static void Extract_AC(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (lookup.AC == null) { lookup.AC = new List<ENC.AC>(); }

                string[] Data_Segment = data.Split(',');
                ENC.AC ENC_AC = new ENC.AC();

                if (Data_Segment.Length > 0) { ENC_AC.Acronym = Data_Segment[0]; }
                if (Data_Segment.Length > 1) { ENC_AC.Trans = int.TryParse(Data_Segment[1], out int Trans) ? Trans : -1; }

                lookup.AC.Add(ENC_AC);
            }
        }

        private static void Extract_AP(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (lookup.AP == null) { lookup.AP = new List<ENC.AP>(); }

                ENC.AP ENC_AP = new ENC.AP()
                {
                    Acronym = data,
                };

                lookup.AP.Add(ENC_AP);
            }
        }

        private static void Extract_CS(string data, ref ENC.Lookup lookup)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (lookup.CS == null) { lookup.CS = new List<ENC.CS>(); }

                ENC.CS ENC_CS = new ENC.CS()
                {
                    Acronym = data,
                };

                lookup.CS.Add(ENC_CS);
            }
        }
    }
}