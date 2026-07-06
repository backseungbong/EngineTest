using JHLib.ChartManager.Configuration;
using JHLib.ChartManager.Record;
using Legacy.ECM_Core.Catalogue;
using Legacy.ECM_Core.Chart;
using Legacy.ECM_Core.Definition;
using Legacy.ECM_Core.Table;
using System.IO;
using System.Text;
using static JHLib.ChartManager.ChartComposer;

namespace Legacy.ECM_Core.Component
{
    public partial class ChartComposer
    {
        public bool Link_Chart(DetectionChart chart)
        {
            try
            {
                Link_FeatureAttribute(chart);
                Link_FeatureInformation(chart);
                Link_PL(chart);

                if (!chart.Name.Contains("KRINDEX"))
                {
                    Link_EdgeMask(chart);
                }

                chart.Linked = true;

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        private void Link_FeatureAttribute(DetectionChart chart)
        {
            if (chart.Feature == null) { return; }


            foreach (DCC.Feature Feature in chart.Feature)
            {
                Link_ATTF(chart, Feature);
                Link_NATF(chart, Feature);

                Feature.Attribute += "=\n";
            }
        }

        private void Link_ATTF(DetectionChart chart, DCC.Feature feature)
        {
            if (feature.ATTF == null) { return; }


            int RYRMGV = 0;
            string VALACM = "";
            string VALMAG = "";

            StringBuilder Total_Builder = new StringBuilder();
            int ATTF_Count = 0;

            foreach (DCC.ATTF ATTF in feature.ATTF)
            {
                if (AttributeCatalogue.Catalogue.TryGetValue(ATTF.ATTL, out ENC.Attribute Attribute))
                {
                    //if (feature.Update_Type == 2)
                    //{
                    //    feature.Valid_Date.Start = "1111";
                    //    feature.Valid_Date.End = "1111";
                    //}


                    bool Attribute_Date = false;

                    if (ATTF.ATVL.Length > 0)
                    {
                        switch (ATTF.ATTL)
                        {
                            case 85:
                            case 118:
                                if (ATTF.ATVL[0].Contains("9999"))
                                {
                                    feature.Valid_Date.End = "0";
                                }
                                else
                                {
                                    feature.Valid_Date.End = ATTF.ATVL[0];
                                    Attribute_Date = true;
                                }
                                break;
                            case 86:
                            case 119:
                                if (ATTF.ATVL[0].Contains("9999"))
                                {
                                    feature.Valid_Date.Start = "0";
                                }
                                else
                                {
                                    feature.Valid_Date.Start = ATTF.ATVL[0];
                                    Attribute_Date = true;
                                }
                                break;
                            case 79:
                            case 115:
                            case 128:
                            case 147:
                            case 151:
                            case 152:
                                {
                                    Attribute_Date = true;
                                }
                                break;
                        }
                    }


                    if (feature.FRID.OBJL == 138)
                    {
                        Total_Builder.Append((ATTF.ATTL == 159) ? $" [{Attribute.Attribute_Name}({Attribute.Acronym})]\r\n" : $" [{Attribute.Attribute_Name}({Attribute.Acronym})] : ");
                    }
                    else
                    {
                        Total_Builder.Append(string.IsNullOrEmpty(Attribute.Attribute_Name) ? " [?] : " : $" [{Attribute.Attribute_Name}({Attribute.Acronym})] : ");
                    }

                    string Attribute_Sentence = "";
                    string Information_Sentence = "";

                    switch (Attribute.Attribute_Type)
                    {
                        case "E":
                            if (ATTF.ATVL.Length > 0)
                            {
                                int.TryParse(ATTF.ATVL[0], out int EM);

                                if (EM == 9999)
                                {
                                    Attribute_Sentence = "<unknown>";
                                }
                                else if (EM > 50)
                                {
                                    Attribute_Sentence = "?";
                                }
                                else if ((0 < EM) && (EM <= Attribute.Attribute_Element.Length))
                                {
                                    Attribute_Sentence = Attribute.Attribute_Element[EM - 1];
                                }

                                Information_Sentence = Attribute_Sentence;
                            }
                            break;
                        case "L":
                            if (ATTF.ATVL.Length > 0)
                            {
                                StringBuilder Information_Builder = new StringBuilder();

                                for (int i = 0; i < ATTF.ATVL.Length; i++)
                                {
                                    if (int.TryParse(ATTF.ATVL[i], out int EM))
                                    {
                                        if ((EM == 0) || (EM == 9999))
                                        {
                                            Information_Builder.Append("<unknown>");
                                        }
                                        else
                                        {
                                            if ((0 < EM) && (EM <= Attribute.Attribute_Element.Length))
                                            {
                                                Information_Builder.Append(Attribute.Attribute_Element[EM - 1]);
                                            }
                                            else
                                            {
                                                Information_Builder.Append('?');
                                            }
                                        }

                                        if (i < (ATTF.ATVL.Length - 1)) { Information_Builder.Append(','); }
                                    }
                                }

                                Attribute_Sentence = Information_Builder.ToString();
                                Information_Sentence = Information_Builder.ToString();
                            }
                            break;
                        case "F":
                            if ((ATTF.ATVL.Length == 0) || ATTF.ATVL[0].Contains("9999"))
                            {
                                Attribute_Sentence = "<unknown>";
                            }
                            else
                            {
                                string Unit = "";

                                switch (ATTF.ATTL)
                                {
                                    case 78: { Unit = "mm"; } break;
                                    case 84: { Unit = "kt"; } break;
                                    case 91: case 177: case 178: { Unit = "M"; } break;
                                    case 106: { Unit = "t"; } break;
                                    case 126: { Unit = "m"; } break;
                                    case 117: case 136: case 137: case 176: { Unit = "degree"; } break;
                                    case 138: { Unit = "minute"; } break;
                                    case 139: { Unit = "Hz"; } break;
                                    case 175: { Unit = "'"; } break;
                                    case 142: case 143: { Unit = "s"; } break;
                                    case 87: case 88: case 144: case 174: case 179:
                                        switch (chart.DSPM.DUNI) {
                                            case 1: { Unit = "m"; } break;
                                            case 2: { Unit = "fathoms and feet"; } break;
                                            case 3: { Unit = "feet"; } break;
                                            case 4: { Unit = "fathoms and fractions"; } break;
                                        } break;
                                    case 5: case 90: case 95: case 97: case 98: case 99: case 100: case 101: case 127: case 145: case 146: case 180: case 181: case 182: case 183: case 184: case 186:
                                        switch (chart.DSPM.HUNI) {
                                            case 1: { Unit = "m"; } break;
                                            case 2: { Unit = "feet"; } break;
                                        } break;
                                    case 401:
                                        switch (chart.DSPM.PUNI) {
                                            case 1: { Unit = "m"; } break;
                                            case 2: { Unit = "degree of arc"; } break;
                                            case 3: { Unit = "milimeters"; } break;
                                            case 4: { Unit = "feet"; } break;
                                            case 5: { Unit = "cables"; } break;
                                        } break;
                                }

                                Attribute_Sentence = $"{ATTF.ATVL[0]} {Unit}";
                                Information_Sentence = ATTF.ATVL[0];

                                if (feature.FRID.OBJL == 81)
                                {
                                    switch (ATTF.ATTL)
                                    {
                                        case 173: { VALACM = ATTF.ATVL[0]; } break;
                                        case 176: { VALMAG = ATTF.ATVL[0]; } break;
                                    }
                                }
                            }
                            break;
                        case "S":
                            if (ATTF.ATVL.Length > 0)
                            {
                                switch (ATTF.ATTL)
                                {
                                    case 158:
                                        if (ATTF.ATVL[0].Contains("9999"))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            Information_Sentence = ATTF.ATVL[0];

                                            FileInfo Attribute_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.ENC_Directory, chart.Name, ATTF.ATVL[0]));

                                            if (!Attribute_FileInfo.Exists)
                                            {
                                                Total_Builder.Append("\r\n"); // 이 때 어차피 무조건 비어있어서

                                                feature.Attribute = Total_Builder.ToString();
                                                feature.Set_AttributeInformation(ATTF.ATTL, ATTF.ATVL[0]);

                                                continue;
                                            }
                                            else
                                            {
                                                if (Attribute_FileInfo.Extension.ToUpper() == ".TXT")
                                                {
                                                    using (StreamReader Attribute_Reader = new StreamReader(Attribute_FileInfo.OpenRead()))//using (StreamReader Attribute_Reader = new StreamReader(Attribute_FileInfo.OpenRead(), TextEncoding.RCP))
                                                    {
                                                        Attribute_Sentence = Attribute_Reader.ReadToEnd();
                                                    }


                                                    FileInfo Destination_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "ATTRIBUTE", chart.Name, Attribute_FileInfo.Name));

                                                    if (Destination_FileInfo.Directory?.Exists == false) { Destination_FileInfo.Directory.Create(); }

                                                    try
                                                    {
                                                        File.Copy(Attribute_FileInfo.FullName, Destination_FileInfo.FullName, true);
                                                    }
                                                    catch (Exception e)
                                                    {

                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case 120:
                                        {
                                            FileInfo Attribute_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.ENC_Directory, chart.Name, ATTF.ATVL[0]));

                                            if (Attribute_FileInfo.Exists)
                                            {
                                                FileInfo Destination_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "ATTRIBUTE", chart.Name, Attribute_FileInfo.Name));

                                                if (Destination_FileInfo.Directory?.Exists == false) { Destination_FileInfo.Directory.Create(); }

                                                try
                                                {
                                                    File.Copy(Attribute_FileInfo.FullName, Destination_FileInfo.FullName, true);
                                                }
                                                catch (Exception e)
                                                {

                                                }
                                            }

                                            Attribute_Sentence = ATTF.ATVL[0];
                                        }
                                        break;
                                    default:
                                        if (!ATTF.ATVL[0].Contains("9999"))
                                        {
                                            Attribute_Sentence = ATTF.ATVL[0];
                                        }
                                        break;
                                }

                                Information_Sentence = Attribute_Sentence;
                            }
                            break;
                        default:
                            if (feature.FRID.OBJL == 138)
                            {
                                if (ATTF.ATVL.Length == 29)
                                {
                                    Total_Builder.Append("------------------------------------------------------------------------------------------------------\r\n");

                                    string HL = "";
                                    int Index = 0;
                                    int Hours = -6;

                                    for (int i = 0; i < 16; i++)
                                    {
                                        if (i == 0)
                                        {
                                            Total_Builder.Append($"Tidal Station : {ATTF.ATVL[Index++]}\r\n");
                                            Total_Builder.Append("------------------------------------------------------------------------------------------------------\r\n");
                                        }
                                        else if (i == 1)
                                        {
                                            Total_Builder.Append($"Tidal Station Identifier : {ATTF.ATVL[Index++]}\r\n");
                                            Total_Builder.Append("------------------------------------------------------------------------------------------------------\r\n");
                                            Total_Builder.Append("              | Hours |  Direction of stream(degrees) | Rates at spring tide(knots) \r\n");
                                            Total_Builder.Append("------------------------------------------------------------------------------------------------------\r\n");
                                        }
                                        else if (i == 2)
                                        {
                                            HL = ATTF.ATVL[Index++];
                                        }
                                        else
                                        {
                                            if (Hours < 0)
                                            {
                                                Total_Builder.Append("Before");
                                            }
                                            else if (Hours == 0)
                                            {
                                                Total_Builder.Append($"{HL}    ");
                                            }
                                            else
                                            {
                                                Total_Builder.Append("After ");
                                            }

                                            Total_Builder.Append($"   | {Hours}    |   {ATTF.ATVL[Index]}                                        |   {ATTF.ATVL[Index + 1]}\r\n");
                                            Total_Builder.Append("------------------------------------------------------------------------------------------------------\r\n");

                                            Hours++;
                                            Index += 2;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (ATTF.ATVL.Length > 0)
                                {
                                    Attribute_Sentence = ATTF.ATVL[0].Contains("9999") ? "<unknown>" : ATTF.ATVL[0];
                                    Information_Sentence = Attribute_Sentence;

                                    if ((feature.FRID.OBJL == 81) && (ATTF.ATTL == 130))
                                    {
                                        int.TryParse(Information_Sentence, out RYRMGV);
                                    }
                                }
                            }
                            break;
                    }

                    if (ATTF.ATTL == 158)
                    {
                        Total_Builder.Append(Attribute_Sentence);
                    }
                    else
                    {
                        if (Attribute_Date && (feature.Valid_Date.Start != "1111"))
                        {
                            if ((Attribute_Sentence.Length >= 8) && int.TryParse(Attribute_Sentence[4..6], out int Month))
                            {
                                string Month_Name = Month switch {
                                    1 => "JAN",
                                    2 => "FEB",
                                    3 => "MAR",
                                    4 => "APR",
                                    5 => "MAY",
                                    6 => "JUN",
                                    7 => "JUL",
                                    8 => "AUG",
                                    9 => "SEP",
                                    10 => "OCT",
                                    11 => "NOV",
                                    12 => "DEC",
                                    _ => ""
                                };

                                Attribute_Sentence = $"{Attribute_Sentence[6..8]}-{Month_Name}-{Attribute_Sentence[0..4]}";
                            }
                            else
                            {
                                Attribute_Sentence = "";
                            }
                        }

                        Total_Builder.Append($"{Attribute_Sentence}\r\n");
                    }

                    if (ATTF_Count == (feature.ATTF.Count - 1))
                    {
                        if (feature.FRID.OBJL == 81)
                        {
                            string VALMAG_Direction = "E";

                            if (VALMAG.Contains('-'))
                            {
                                VALMAG_Direction = "W";
                                VALMAG = VALMAG.Remove(0, 1);
                            }

                            string VALMAG_Degree;
                            int Dot_Index = VALMAG.IndexOf('.');

                            if (Dot_Index != -1)
                            {
                                VALMAG_Degree = $"{VALMAG.Substring(0, Dot_Index)}\u00B0{VALMAG.Substring(Dot_Index + 1, VALMAG.Length - Dot_Index - 1)}{VALMAG_Direction}";
                            }
                            else
                            {
                                VALMAG_Degree = $"{VALMAG}\u00B0{VALMAG_Direction}";
                            }

                            VALMAG_Degree += $" {RYRMGV} ";

                            string VALACM_Direction = "E";

                            if (VALACM.Contains('-'))
                            {
                                VALACM_Direction = "W";
                                VALACM = VALACM.Remove(0, 1);
                            }

                            Total_Builder.Append($"{VALMAG_Degree}({VALACM}'{VALACM_Direction})\r\n");
                        }
                    }

                    feature.Attribute = Total_Builder.ToString();
                    feature.Set_AttributeInformation(ATTF.ATTL, Information_Sentence);

                    if (ATTF.ATTL > 402 && !((8194 <= ATTF.ATTL) && (ATTF.ATTL <= 8224)))
                    {

                    }

                    ATTF_Count++;
                }
            }
        }

        private void Link_NATF(DetectionChart chart, DCC.Feature feature)
        {
            if (feature.NATF == null) { return; }


            foreach (DCC.NATF NATF in feature.NATF)
            {
                if (AttributeCatalogue.Catalogue.TryGetValue(NATF.ATTL, out ENC.Attribute Attribute))
                {
                    if (NATF.ATTL == 304)
                    {
                        if (!NATF.ATVL.Contains("9999"))
                        {
                            FileInfo Attribute_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.ENC_Directory, chart.Name, NATF.ATVL));

                            if (!Attribute_FileInfo.Exists)
                            {
                                feature.Attribute += $" [{Attribute.Acronym}] : {NATF.ATVL}\r\n";
                                feature.Set_AttributeInformation(NATF.ATTL, NATF.ATVL);
                            }
                            else
                            {
                                if (Attribute_FileInfo.Extension.ToUpper() == ".TXT")
                                {
                                    feature.Attribute = $" [{Attribute.Acronym}] : ";

                                    using (StreamReader Attribute_Reader = new StreamReader(Attribute_FileInfo.OpenRead()))//using (StreamReader Attribute_Reader = new StreamReader(Attribute_FileInfo.OpenRead(), TextEncoding.RCP))
                                    {
                                        feature.Attribute += $"{Attribute_Reader.ReadToEnd()}\r\n";
                                    }

                                    feature.Set_AttributeInformation(NATF.ATTL, NATF.ATVL);
                                }
                            }
                        }
                    }
                    else
                    {
                        feature.Attribute += $" [{Attribute.Acronym}] : {NATF.ATVL}\r\n";
                        feature.Set_AttributeInformation(NATF.ATTL, NATF.ATVL);
                    }
                }
            }
        }


        private void Link_FeatureInformation(DetectionChart chart)
        {
            if (chart.Feature == null) { return; }


            DirectoryInfo FeatureAttribute_DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "ATTRIBUTE", chart.Name));
            FileInfo FeatureAttribute_FileInfo = new FileInfo(Path.Combine(DirectoryDefinition.AppBase_Directory, DirectoryDefinition.Chart_Directory, "ATTRIBUTE", chart.Name, $"{chart.Name}.atr"));
            FileInfo NewFeatureAttribute_FileInfo = new FileInfo(Path.Combine(DirectoryConfiguration.encAttribute, chart.Name, $"{chart.Name}.atr"));

            if (!FeatureAttribute_DirectoryInfo.Exists) { FeatureAttribute_DirectoryInfo.Create(); }
            if (FeatureAttribute_FileInfo.Exists) { FeatureAttribute_FileInfo.Delete(); }
            if (NewFeatureAttribute_FileInfo.Exists) { NewFeatureAttribute_FileInfo.Delete(); }

            foreach (DCC.Feature Feature in chart.Feature)
            {
                DCC.FeatureLinker Feature_Linker = new DCC.FeatureLinker();
                Feature_Linker.FRID = Feature.FRID;

                if (ObjectCatalogue.Catalogue.TryGetValue(Feature.FRID.OBJL, out ENC.Object Object))
                {
                    Feature_Linker.Object_Acronym = Object.Acronym;
                    Feature_Linker.Object_Name = Object.Object_Name;
                }

                switch (Feature.FRID.PRIM)
                {
                    case 1: { if (!Link_FeaturePoint(chart, Feature, Feature_Linker)) { continue; } } break;
                    case 2: { if (!Link_FeatureLine(chart, Feature, Feature_Linker)) { continue; } } break;
                    case 3: { if (!Link_FeatureArea(chart, Feature, Feature_Linker)) { continue; } } break;
                }

                Link_Lookup(Feature, Feature_Linker);
                LegacyExport_FeatureAttribute(chart, Feature, Feature_Linker);

                chart.FeatureLinker_Collection ??= new Dictionary<uint, DCC.FeatureLinker>(); // 이거 꼭 필요할까? feature와 통합해도 별 관계없어 보이는데
                chart.FeatureLinker_Collection.TryAdd(Feature_Linker.FRID.RCID, Feature_Linker);
            }
        }

        public bool Link_FeaturePoint(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            bool Result = false;

            if (feature.FSPT?.Count > 0)
            {
                byte RCNM = feature.FSPT[0].KEY1;
                uint RCID = feature.FSPT[0].KEY2;

                if (chart.Get_Vector(RCNM, RCID, out DCC.Vector Vector))
                {
                    DCC.UpdateRecord? updateRecord = chart.Update_Record?.FirstOrDefault(Record => Record.VRID.RCID == RCID);

                    if (updateRecord != null)
                    {
                        if (updateRecord.FRID.RCID != 0u)
                        {
                            chart.Update_Record!.Add(new DCC.UpdateRecord() {
                                VRID = updateRecord.VRID,
                                FRID = feature.FRID,
                                SG2D = (updateRecord.SG2D != null) ? new List<DCC.SG2D>(updateRecord.SG2D) : null,
                            });
                        }
                        else
                        {
                            updateRecord.FRID = feature.FRID;
                        }
                    }

                    if (!Vector.Bound)
                    {
                        feature.FRID.RUIN = 2;

                        return false;
                    }

                    //if (Vector.VRID.RUIN == 2) { return false; }

                    DCC.ShapeLinker Shape_Linker = new DCC.ShapeLinker();

                    if (feature.FRID.OBJL == 129)
                    {
                        Shape_Linker.Vector_3D.RCNM = RCNM;
                        Shape_Linker.Vector_3D.RCID = RCID;

                        if ((Vector.ATTV?.Count > 0) && (Vector.ATTV[0].ATTL == 402))
                        {
                            uint.TryParse(Vector.ATTV[0].ATVL, out Shape_Linker.Vector_3D.ATVL);

                            if ((1 < Shape_Linker.Vector_3D.ATVL) && (Shape_Linker.Vector_3D.ATVL < 10)) { Shape_Linker.Vector_3D.ATVL = 0; }
                        }

                        if (Vector.SG3D?.Count > 0)
                        {
                            foreach (DCC.SG3D SG3D in Vector.SG3D)
                            {
                                Shape_Linker.Vector_3D.SG3D ??= new List<DCC.SG3D>();
                                Shape_Linker.Vector_3D.SG3D.Add(SG3D);
                            }

                            linker.Pivot = (X: Vector.SG3D[0].XCOO, Y: Vector.SG3D[0].YCOO);
                        }
                    }
                    else
                    {
                        Shape_Linker.Vector_2D.RCNM = RCNM;
                        Shape_Linker.Vector_2D.RCID = RCID;

                        if ((Vector.ATTV?.Count > 0) && (Vector.ATTV[0].ATTL == 402))
                        {
                            uint.TryParse(Vector.ATTV[0].ATVL, out Shape_Linker.Vector_2D.ATVL);

                            if ((1 < Shape_Linker.Vector_2D.ATVL) && (Shape_Linker.Vector_2D.ATVL < 10)) { Shape_Linker.Vector_2D.ATVL = 0; }
                        }

                        if (Vector.SG2D?.Count > 0)
                        {
                            foreach (DCC.SG2D SG2D in Vector.SG2D)
                            {
                                Shape_Linker.Vector_2D.SG2D ??= new List<DCC.SG2D>();
                                Shape_Linker.Vector_2D.SG2D.Add(SG2D);
                            }

                            linker.Pivot = (X: Vector.SG2D[0].XCOO, Y: Vector.SG2D[0].YCOO);
                        }
                    }

                    Vector.Linked_Feature ??= new List<DCC.Feature>();
                    Vector.Linked_Feature.Add(feature);

                    linker.Shape ??= new List<DCC.ShapeLinker>();
                    linker.Shape.Add(Shape_Linker);

                    Result = true;
                }
            }

            return Result;
        }

        public bool Link_FeatureLine(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            bool Result = false;

            if (feature.FSPT?.Count > 0)
            {
                DCC.ShapeLinker Shape_Linker = new DCC.ShapeLinker();

                foreach (DCC.FSPT FSPT in feature.FSPT)
                {
                    byte RCNM = FSPT.KEY1;
                    uint RCID = FSPT.KEY2;

                    if (chart.Get_Vector(RCNM, RCID, out DCC.Vector Vector))
                    {
                        DCC.UpdateRecord? updateRecord = chart.Update_Record?.FirstOrDefault(Record => Record.VRID.RCID == RCID);

                        if (updateRecord != null)
                        {
                            if (updateRecord.FRID.RCID != 0u)
                            {
                                chart.Update_Record!.Add(new DCC.UpdateRecord() {
                                    VRID = updateRecord.VRID,
                                    FRID = feature.FRID,
                                    SG2D = (updateRecord.SG2D != null) ? new List<DCC.SG2D>(updateRecord.SG2D) : null,
                                });
                            }
                            else
                            {
                                updateRecord.FRID = feature.FRID;
                            }
                        }

                        //if (Vector.VRID.RUIN == 2) { continue; }

                        if (Vector.VRPT?.Count > 1)
                        {
                            DCC.EdgeLinker Edge_Linker = new DCC.EdgeLinker();
                            Edge_Linker.RCNM = RCNM;
                            Edge_Linker.RCID = RCID;
                            Edge_Linker.ORNT = FSPT.ORNT;
                            Edge_Linker.USAG = FSPT.USAG;
                            Edge_Linker.MASK = (FSPT.USAG == 3) ? (byte)1 : FSPT.MASK;
                            
                            if ((Vector.ATTV?.Count > 0) && (Vector.ATTV[0].ATTL == 402))
                            {
                                uint.TryParse(Vector.ATTV[0].ATVL, out Edge_Linker.ATVL);

                                if ((1 < Edge_Linker.ATVL) && (Edge_Linker.ATVL < 10)) { Edge_Linker.ATVL = 0; }
                            }


                            (byte RCNM, uint RCID) VRPT_1;
                            (byte RCNM, uint RCID) VRPT_2;

                            if (Edge_Linker.ORNT == 2)
                            {
                                VRPT_1.RCNM = Vector.VRPT[1].KEY1;
                                VRPT_1.RCID = Vector.VRPT[1].KEY2;
                                VRPT_2.RCNM = Vector.VRPT[0].KEY1;
                                VRPT_2.RCID = Vector.VRPT[0].KEY2;
                            }
                            else
                            {
                                VRPT_1.RCNM = Vector.VRPT[0].KEY1;
                                VRPT_1.RCID = Vector.VRPT[0].KEY2;
                                VRPT_2.RCNM = Vector.VRPT[1].KEY1;
                                VRPT_2.RCID = Vector.VRPT[1].KEY2;
                            }

                            if (chart.Get_Vector(VRPT_1.RCNM, VRPT_1.RCID, out DCC.Vector Vector_1) && (Vector_1.SG2D?.Count > 0))
                            {
                                Edge_Linker.SG2D ??= new List<DCC.SG2D>();
                                Edge_Linker.SG2D.Add(Vector_1.SG2D[0]);

                                if (Vector.SG2D?.Count > 0)
                                {
                                    foreach (DCC.SG2D SG2D in Vector.SG2D)
                                    {
                                        Edge_Linker.SG2D.Add(SG2D);
                                    }
                                }
                                
                                if (chart.Get_Vector(VRPT_2.RCNM, VRPT_2.RCID, out DCC.Vector Vector_2) && (Vector_2.SG2D?.Count > 0))
                                {
                                    Edge_Linker.SG2D.Add(Vector_2.SG2D[0]);
                                }
                                else
                                {
                                    Edge_Linker.SG2D.Add(Vector_1.SG2D[0]);
                                }
                            }
                            else
                            {
                                if (chart.Get_Vector(VRPT_2.RCNM, VRPT_2.RCID, out DCC.Vector Vector_2) && (Vector_2.SG2D?.Count > 0))
                                {
                                    Edge_Linker.SG2D ??= new List<DCC.SG2D>();
                                    Edge_Linker.SG2D.Add(Vector_2.SG2D[0]);

                                    if (Vector.SG2D?.Count > 0)
                                    {
                                        foreach (DCC.SG2D SG2D in Vector.SG2D)
                                        {
                                            Edge_Linker.SG2D.Add(SG2D);
                                        }
                                    }

                                    Edge_Linker.SG2D.Add(Vector_2.SG2D[0]);
                                }
                            }

                            Shape_Linker.Edge ??= new List<DCC.EdgeLinker>();
                            Shape_Linker.Edge.Add(Edge_Linker);


                            int Total = (Vector.SG2D?.Count ?? 0) + 2;
                            Shape_Linker.Point ??= new List<DCC.SG2D>();

                            for (int i = 0; i < Total; i++)
                            {
                                if (Edge_Linker.ORNT == 2)
                                {
                                    if (i == 0)
                                    {
                                        if (Edge_Linker.SG2D?.Count > (Total - 1))
                                        {
                                            Shape_Linker.Point.Add(Edge_Linker.SG2D[Total - 1]);
                                        }
                                    }
                                    else if (i == (Total - 1))
                                    {
                                        if (Edge_Linker.SG2D?.Count > 0)
                                        {
                                            Shape_Linker.Point.Add(Edge_Linker.SG2D[0]);
                                        }
                                    }
                                    else
                                    {
                                        if (Edge_Linker.SG2D?.Count > i)
                                        {
                                            Shape_Linker.Point.Add(Edge_Linker.SG2D[i]);
                                        }
                                    }
                                }
                                else
                                {
                                    if (Edge_Linker.SG2D?.Count > i)
                                    {
                                        Shape_Linker.Point.Add(Edge_Linker.SG2D[i]);
                                    }
                                }
                            }
                        }

                        Vector.Linked_Feature ??= new List<DCC.Feature>();
                        Vector.Linked_Feature.Add(feature);
                    }
                }

                if (Shape_Linker.Point?.Count > 0)
                {
                    if (Calculate_Pivot(feature.FRID.PRIM, feature.FRID.OBJL, Shape_Linker.Point, out (int X, int Y) Pivot))
                    {
                        linker.Pivot = Pivot;
                    }
                }

                linker.Shape ??= new List<DCC.ShapeLinker>();
                linker.Shape.Add(Shape_Linker);

                Result = true;
            }

            return Result;
        }

        public bool Link_FeatureArea(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            bool Result = false;

            if (feature.FSPT?.Count > 0)
            {
                bool On_Reference = false;
                DCC.SG2D Reference = new DCC.SG2D();

                int Shape_Count = 0;
                List<int> ShapeCount_Collection = new List<int>();

                foreach (DCC.FSPT FSPT in feature.FSPT)
                {
                    Shape_Count++;

                    byte RCNM = FSPT.KEY1;
                    uint RCID = FSPT.KEY2;

                    if (chart.Get_Vector(RCNM, RCID, out DCC.Vector Vector))
                    {
                        Vector.Linked_Feature ??= new List<DCC.Feature>();
                        Vector.Linked_Feature.Add(feature);

                        DCC.UpdateRecord? updateRecord = chart.Update_Record?.FirstOrDefault(Record => Record.VRID.RCID == RCID);

                        if (updateRecord != null)
                        {
                            if (updateRecord.FRID.RCID != 0u)
                            {
                                chart.Update_Record!.Add(new DCC.UpdateRecord() {
                                    VRID = updateRecord.VRID,
                                    FRID = feature.FRID,
                                    SG2D = (updateRecord.SG2D != null) ? new List<DCC.SG2D>(updateRecord.SG2D) : null,
                                });
                            }
                            else
                            {
                                updateRecord.FRID = feature.FRID;
                            }
                        }

                        if (!On_Reference)
                        {
                            On_Reference = true;

                            (byte RCNM, uint RCID) VRPT_1 = (RCNM: 0, RCID: 0);

                            if ((FSPT.ORNT == 1) || (FSPT.ORNT == 255))
                            {
                                if (Vector.VRPT?.Count > 0)
                                {
                                    VRPT_1.RCNM = Vector.VRPT[0].KEY1;
                                    VRPT_1.RCID = Vector.VRPT[0].KEY2;
                                }
                            }
                            else
                            {
                                if (Vector.VRPT?.Count > 1)
                                {
                                    VRPT_1.RCNM = Vector.VRPT[1].KEY1;
                                    VRPT_1.RCID = Vector.VRPT[1].KEY2;
                                }
                            }

                            if (chart.Get_Vector(VRPT_1.RCNM, VRPT_1.RCID, out DCC.Vector Vector_1))
                            {
                                if (Vector_1.SG2D?.Count > 0)
                                {
                                    Reference = Vector_1.SG2D[0];
                                }
                            }
                        }

                        (byte RCNM, uint RCID) VRPT_2 = (RCNM: 0, RCID: 0);

                        if ((FSPT.ORNT == 1) || (FSPT.ORNT == 255))
                        {
                            if (Vector.VRPT?.Count > 1)
                            {
                                VRPT_2.RCNM = Vector.VRPT[1].KEY1;
                                VRPT_2.RCID = Vector.VRPT[1].KEY2;
                            }
                        }
                        else
                        {
                            if (Vector.VRPT?.Count > 0)
                            {
                                VRPT_2.RCNM = Vector.VRPT[0].KEY1;
                                VRPT_2.RCID = Vector.VRPT[0].KEY2;
                            }
                        }

                        if (chart.Get_Vector(VRPT_2.RCNM, VRPT_2.RCID, out DCC.Vector Vector_2))
                        {
                            if ((Vector_2.SG2D?.Count > 0) && (Reference.XCOO == Vector_2.SG2D[0].XCOO) && (Reference.YCOO == Vector_2.SG2D[0].YCOO))
                            {
                                ShapeCount_Collection.Add(Shape_Count);

                                Shape_Count = 0;
                                On_Reference = false;
                            }
                        }
                    }
                }


                int Index = 0;

                foreach (int Edge in ShapeCount_Collection)
                {
                    DCC.ShapeLinker Shape_Linker = new DCC.ShapeLinker();

                    for (int i = 0; i < Edge; i++)
                    {
                        DCC.FSPT FSPT = feature.FSPT[Index]; // 앞쪽 수집 단계 방식에 의해 index 범위 방어가 되고 있음
                        byte RCNM = FSPT.KEY1;
                        uint RCID = FSPT.KEY2;

                        if (chart.Get_Vector(RCNM, RCID, out DCC.Vector Vector))
                        {
                            //if (Vector.VRID.RUIN == 2) { continue; }

                            if (Vector.VRPT?.Count > 1)
                            {
                                DCC.EdgeLinker Edge_Linker = new DCC.EdgeLinker();
                                Edge_Linker.RCNM = RCNM;
                                Edge_Linker.RCID = RCID;
                                Edge_Linker.ORNT = FSPT.ORNT;
                                Edge_Linker.USAG = FSPT.USAG;
                                Edge_Linker.MASK = (FSPT.USAG == 3) ? (byte)1 : FSPT.MASK;
                                
                                if ((Vector.ATTV?.Count > 0) && (Vector.ATTV[0].ATTL == 402))
                                {
                                    uint.TryParse(Vector.ATTV[0].ATVL, out Edge_Linker.ATVL);

                                    if ((1 < Edge_Linker.ATVL) && (Edge_Linker.ATVL < 10)) { Edge_Linker.ATVL = 0; }
                                }


                                (byte RCNM, uint RCID) VRPT_1;
                                (byte RCNM, uint RCID) VRPT_2;

                                if ((Edge_Linker.ORNT == 1) || (Edge_Linker.ORNT == 255))
                                {
                                    VRPT_1.RCNM = Vector.VRPT[0].KEY1;
                                    VRPT_1.RCID = Vector.VRPT[0].KEY2;
                                    VRPT_2.RCNM = Vector.VRPT[1].KEY1;
                                    VRPT_2.RCID = Vector.VRPT[1].KEY2;
                                }
                                else
                                {
                                    VRPT_1.RCNM = Vector.VRPT[1].KEY1;
                                    VRPT_1.RCID = Vector.VRPT[1].KEY2;
                                    VRPT_2.RCNM = Vector.VRPT[0].KEY1;
                                    VRPT_2.RCID = Vector.VRPT[0].KEY2;
                                }

                                if (chart.Get_Vector(VRPT_1.RCNM, VRPT_1.RCID, out DCC.Vector Vector_1) && (Vector_1.SG2D?.Count > 0)) // 이쪽 이후 부분에 예외처리 철저하게 안 해도 별다른 문제가 없는 상태란 얘기
                                {
                                    Edge_Linker.SG2D ??= new List<DCC.SG2D>();
                                    Edge_Linker.SG2D.Add(Vector_1.SG2D[0]);

                                    if (i == 0)
                                    {
                                        Shape_Linker.Point ??= new List<DCC.SG2D>();
                                        Shape_Linker.Point.Add(Vector_1.SG2D[0]);
                                    }
                                }

                                if ((Edge_Linker.ORNT == 1) || (Edge_Linker.ORNT == 255))
                                {
                                    if (Vector.SG2D?.Count > 0)
                                    {
                                        for (int j = 0; j < Vector.SG2D.Count; j++)
                                        {
                                            Edge_Linker.SG2D ??= new List<DCC.SG2D>();
                                            Shape_Linker.Point ??= new List<DCC.SG2D>();

                                            Edge_Linker.SG2D.Add(Vector.SG2D[j]);
                                            Shape_Linker.Point.Add(Vector.SG2D[j]);
                                        }
                                    }
                                }
                                else
                                {
                                    if (Vector.SG2D?.Count > 0)
                                    {
                                        for (int j = (Vector.SG2D.Count - 1); j >= 0; j--)
                                        {
                                            Edge_Linker.SG2D ??= new List<DCC.SG2D>();
                                            Shape_Linker.Point ??= new List<DCC.SG2D>();

                                            Edge_Linker.SG2D.Add(Vector.SG2D[j]);
                                            Shape_Linker.Point.Add(Vector.SG2D[j]);
                                        }
                                    }
                                }

                                if (chart.Get_Vector(VRPT_2.RCNM, VRPT_2.RCID, out DCC.Vector Vector_2) && (Vector_2.SG2D?.Count > 0))
                                {
                                    Edge_Linker.SG2D ??= new List<DCC.SG2D>();
                                    Shape_Linker.Point ??= new List<DCC.SG2D>();

                                    Edge_Linker.SG2D.Add(Vector_2.SG2D[0]);
                                    Shape_Linker.Point.Add(Vector_2.SG2D[0]);
                                }
                                else
                                {
                                    if (Vector_1.SG2D?.Count > 0)
                                    {
                                        Edge_Linker.SG2D ??= new List<DCC.SG2D>();
                                        Edge_Linker.SG2D.Add(Vector_1.SG2D[0]);
                                    }
                                }

                                Shape_Linker.Edge ??= new List<DCC.EdgeLinker>();
                                Shape_Linker.Edge.Add(Edge_Linker);
                            }
                        }

                        Index++;
                    }

                    linker.Shape ??= new List<DCC.ShapeLinker>();
                    linker.Shape.Add(Shape_Linker);
                }


                if (linker.Shape?.Count > 0)
                {
                    DCC.ShapeLinker Shape_Linker = linker.Shape[0];

                    if ((Shape_Linker.Point?.Count > 0) && Calculate_Pivot(feature.FRID.PRIM, feature.FRID.OBJL, Shape_Linker.Point, out (int X, int Y) Pivot))
                    {
                        linker.Pivot = Pivot;
                    }
                }

                Result = true;
            }

            return Result;
        }

        public void Link_Lookup(DCC.Feature feature, DCC.FeatureLinker linker)
        {
            byte Type = 0;
            byte Loop = 0;

            switch (linker.FRID.PRIM)
            {
                case 1:
                    {
                        Type = 0;
                        Loop = 2;
                    }
                    break;
                case 2:
                    {
                        Type = 2;
                        Loop = 1;
                    }
                    break;
                case 3:
                    {
                        Type = 3;
                        Loop = (byte)(Using_Chart1 ? 1 : 2);
                    }
                    break;
            }

            for (int i = 0; i < Loop; i++, Type++)
            {
                if (LookupTable.Get_IndexSize(Type, linker.Object_Acronym, Using_Chart1, out (int Index, int Size) Index_Size))
                {
                    if (Index_Size.Size < 1)
                    {
                        feature.Error = true;

                        return;
                    }

                    if (i == 0)
                    {
                        linker.PL.Index0 = Index_Size.Index;
                    }
                    else
                    {
                        linker.PL.Index1 = Index_Size.Index;
                    }

                    if (Index_Size.Size == 1) { continue; }


                    bool Match = false;
                    int Index = Index_Size.Index + 1;

                    for (int j = Index; j < (Index + Index_Size.Size - 1); j++)
                    {
                        if (LookupTable.Get_Lookup(Type, j, Using_Chart1, out ENC.Lookup Lookup))
                        {
                            foreach ((string Acronym, string[] Element) Lookup_Attribute in Lookup.Attribute)
                            {
                                Match = false;

                                if (feature.ATTF != null)
                                {
                                    foreach (DCC.ATTF ATTF in feature.ATTF)
                                    {
                                        if (AttributeCatalogue.Catalogue.TryGetValue(ATTF.ATTL, out ENC.Attribute Attribute) && (Attribute.Acronym == Lookup_Attribute.Acronym))
                                        {
                                            if (Attribute.Attribute_Type.Contains('E')) // 열거형
                                            {
                                                if (Lookup_Attribute.Element.Length < 1)
                                                {
                                                    Match = true;
                                                }
                                                else
                                                {
                                                    if ((ATTF.ATVL.Length > 0) && (Lookup_Attribute.Element[0] == ATTF.ATVL[0])) // 여기 예외처리를 안 해도 문제없는 이유?
                                                    {
                                                        Match = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            else if (Attribute.Attribute_Type.Contains('L')) // 리스트형
                                            {
                                                bool Sub_Match = false;

                                                if (Attribute.Acronym.Contains("COLOUR"))
                                                {
                                                    if (Lookup_Attribute.Element.Length == 1)
                                                    {
                                                        foreach (string ATVL in ATTF.ATVL)
                                                        {
                                                            if (Lookup_Attribute.Element[0] == ATVL)
                                                            {
                                                                Sub_Match = true;
                                                                break;
                                                            }
                                                        }

                                                        if (Sub_Match)
                                                        {
                                                            Match = true;
                                                            break;
                                                        }
                                                    }
                                                    else if (Lookup_Attribute.Element.Length == ATTF.ATVL.Length)
                                                    {
                                                        Sub_Match = true;

                                                        for (int k = 0; k < Lookup_Attribute.Element.Length; k++)
                                                        {
                                                            if (Lookup_Attribute.Element[k] != ATTF.ATVL[k])
                                                            {
                                                                Sub_Match = false;
                                                                break;
                                                            }
                                                        }

                                                        if (Sub_Match)
                                                        {
                                                            Match = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (Lookup_Attribute.Element.Length < 1)
                                                    {
                                                        Sub_Match = true;
                                                    }
                                                    else
                                                    {
                                                        foreach (string Element in Lookup_Attribute.Element)
                                                        {
                                                            Sub_Match = false;

                                                            foreach (string ATVL in ATTF.ATVL)
                                                            {
                                                                if (ATVL == Element)
                                                                {
                                                                    Sub_Match = true;
                                                                    break;
                                                                }
                                                            }

                                                            if (!Sub_Match) { break; }
                                                        }
                                                    }

                                                    if (Sub_Match)
                                                    {
                                                        Match = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (Lookup_Attribute.Element.Length < 1)
                                                {
                                                    if (Attribute.Acronym.Contains("ORIENT"))
                                                    {
                                                        if ((ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] != "9999"))
                                                        {
                                                            Match = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Match = true;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    if ((Lookup_Attribute.Element[0] == "?") && (ATTF.ATVL.Length > 0) && (ATTF.ATVL[0] == "9999"))
                                                    {
                                                        Match = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!Match)
                                {
                                    break;
                                }
                            }

                            if (Match)
                            {
                                if (i == 0)
                                {
                                    linker.PL.Index0 = j;
                                }
                                else
                                {
                                    linker.PL.Index1 = j;
                                }

                                break;
                            }
                        }
                    }
                }
                else
                {
                    feature.Error = true;

                    return;
                }
            }
        }

        private bool Calculate_Pivot(byte prim, ushort objl, List<DCC.SG2D> SG2D, out (int X, int Y) Pivot)
        {
            bool Result = false;

            if (objl == 8193)
            {
                DCC.SG2D Last_SG2D = SG2D.Last();

                Pivot = (X: Last_SG2D.XCOO, Y: Last_SG2D.YCOO);
                Result = true;
            }
            else
            {
                if (prim == 2)
                {
                    Pivot = Calculate_COL(SG2D);
                    Result = true;
                }
                else if (prim == 3)
                {
                    if (SG2D.Count > 2)
                    {
                        Pivot = Calculate_COG(SG2D);
                        Result = true;
                    }
                    else
                    {
                        Pivot = Calculate_COL(SG2D);
                        Result = true;
                    }
                }
                else
                {
                    Pivot = (X: 0, Y: 0);
                }
            }

            return Result;
        }

        private (int X, int Y) Calculate_COL(List<DCC.SG2D> SG2D)
        {
            if (SG2D.Count < 2)
            {
                return (X: SG2D[0].XCOO, Y: SG2D[0].YCOO);
            }
            else if (SG2D.Count == 2)
            {
                if ((SG2D[0].XCOO == SG2D[1].XCOO) && (SG2D[0].YCOO == SG2D[1].YCOO))
                {
                    return (X: SG2D[0].XCOO, Y: SG2D[0].YCOO);
                }
                else
                {
                    (double X, double Y) Average = (
                        Math.Abs(SG2D[0].XCOO - SG2D[1].XCOO) / 2.0,
                        Math.Abs(SG2D[0].YCOO - SG2D[1].YCOO) / 2.0
                    );

                    return (
                        X: (int)(((SG2D[0].XCOO > SG2D[1].XCOO) ? SG2D[1].XCOO : SG2D[0].XCOO) + Average.X),
                        Y: (int)(((SG2D[0].YCOO > SG2D[1].YCOO) ? SG2D[1].YCOO : SG2D[0].YCOO) + Average.Y)
                    );
                }
            }
            else
            {
                double Center_Length = 0.0;

                for (int i = 1; i < SG2D.Count; i++)
                {
                    (double X, double Y) Difference = (
                        SG2D[i].XCOO - SG2D[i - 1].XCOO,
                        SG2D[i].YCOO - SG2D[i - 1].YCOO
                    );

                    Center_Length += Math.Sqrt((Difference.X * Difference.X) + (Difference.Y * Difference.Y));
                }

                Center_Length /= 2.0;

                double Sum = 0.0;
                int Index = 0;
                double FirstIndex_Length = 0.0;
                double SecondIndex_Length = 0.0;

                for (int i = 1; i < SG2D.Count; i++)
                {
                    (double X, double Y) Difference = (
                        SG2D[i].XCOO - SG2D[i - 1].XCOO,
                        SG2D[i].YCOO - SG2D[i - 1].YCOO
                    );

                    double Scalar = Math.Sqrt((Difference.X * Difference.X) + (Difference.Y * Difference.Y));
                    Sum += Scalar;

                    if (Sum > Center_Length)
                    {
                        Index = i - 1;
                        FirstIndex_Length = Sum - Scalar;
                        SecondIndex_Length = Sum;

                        break;
                    }
                }

                double α = Center_Length - FirstIndex_Length;
                double β = SecondIndex_Length - FirstIndex_Length;

                if (β != 0.0)
                {
                    (double X, double Y) Difference = (
                        SG2D[Index + 1].XCOO - SG2D[Index].XCOO,
                        SG2D[Index + 1].YCOO - SG2D[Index].YCOO
                    );

                    return (
                        X: (int)((α * Difference.X / β) + SG2D[Index].XCOO),
                        Y: (int)((α * Difference.Y / β) + SG2D[Index].YCOO)
                    );
                }
                else
                {
                    return (X: SG2D[Index].XCOO, Y: SG2D[Index].YCOO);
                }
            }
        }

        private (int X, int Y) Calculate_COG(List<DCC.SG2D> SG2D)
        {
            double Area = 0.0;
            double CX = 0.0;
            double CY = 0.0;

            (double X, double Y) Max = (X: -180.0, Y: -90.0);
            (double X, double Y) Min = (X: 180.0, Y: 90.0);

            for (int i = 0; i < SG2D.Count; i++)
            {
                int Index = (i == (SG2D.Count - 1)) ? 0 : (i + 1);

                (double X, double Y) Current = (
                    SG2D[i].XCOO / 10000000.0,
                    SG2D[i].YCOO / 10000000.0
                );
                (double X, double Y) Next = (
                    SG2D[Index].XCOO / 10000000.0,
                    SG2D[Index].YCOO / 10000000.0
                );

                double Determinant = (Current.X * Next.Y) - (Next.X * Current.Y);

                Area += Determinant;
                CX += (Current.X + Next.X) * Determinant;
                CY += (Current.Y + Next.Y) * Determinant;

                if (Current.X > Max.X) { Max.X = Current.X; }
                if (Current.Y > Max.Y) { Max.Y = Current.Y; }
                if (Current.X < Min.X) { Min.X = Current.X; }
                if (Current.Y < Min.Y) { Min.Y = Current.Y; }
            }

            Area /= 2.0;

            if (Area != 0.0)
            {
                CX /= Area * 6.0;
                CY /= Area * 6.0;
            }

            if ((CX < Min.X) || (Max.X < CX)) { CX = Min.X + ((Max.X - Min.X) / 2.0); }
            if ((CY < Min.Y) || (Max.Y < CY)) { CY = Min.Y + ((Max.Y - Min.Y) / 2.0); }

            return (
                X: (int)(CX * 10000000.0),
                Y: (int)(CY * 10000000.0)
            );
        }

        // test
        //public ChartAttributeComposer CAC { get; private set; } = new ChartAttributeComposer();
        //private void Export_FeatureAttribute(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        //{
        //    //
        //    LegacyExport_FeatureAttribute(chart, feature, linker);
        //    //
        //    FileInfo FeatureAttribute_FileInfo = new FileInfo(Path.Combine(DirectoryConfiguration.encAttribute, chart.Name, $"{chart.Name}.atr"));
        //    FeatureAttributeRecord faRecord = new FeatureAttributeRecord(linker.FRID.RCID, linker.FRID.OBJL);

        //    if (linker.FRID.OBJL == 999)
        //    {
        //        faRecord.objectName = "UNKNOWN";
        //        faRecord.objectAcronym = "UNKNOWN";
        //    }
        //    else
        //    {
        //        faRecord.objectName = linker.Object_Name;
        //        faRecord.objectAcronym = linker.Object_Acronym;
        //    }

        //    faRecord.position = new FeatureAttributeRecord.Position(Convert_LatitudeText(linker.Pivot.Y), Convert_LongitudeText(linker.Pivot.X));

        //    switch (linker.FRID.PRIM)
        //    {
        //        case 1: { faRecord.primitive = "Point"; } break;
        //        case 2: { faRecord.primitive = "Line"; } break;
        //        case 3: { faRecord.primitive = "Area"; } break;
        //    }

        //    faRecord.attribute.Add(feature.Attribute); // feature attribute 만들 때, 개행 포함해서 하나의 문자열로 구성하지 않고 List<string>으로 모아서 오는 걸로 수정할 계획


        //    CAC.ComposeFeatureAttribute(FeatureAttribute_FileInfo.FullName, faRecord);
        //}
        //
        private void LegacyExport_FeatureAttribute(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker)
        {
            FileInfo FeatureAttribute_FileInfo = new FileInfo(Path.Combine(DirectoryConfiguration.encAttribute, $"{chart.Name}.atr"));

            try
            {
                using (StreamWriter FeatureAttribute_Writer = new StreamWriter(FeatureAttribute_FileInfo.Open(FileMode.Append)))
                {
                    FeatureAttribute_Writer.WriteLine($"-{linker.FRID.RCID}-");

                    if (linker.FRID.OBJL == 999)
                    {
                        FeatureAttribute_Writer.WriteLine(" >> UNKNOWN");
                    }
                    else
                    {
                        FeatureAttribute_Writer.WriteLine($" >> {linker.Object_Name}({linker.Object_Acronym})");
                    }

                    FeatureAttribute_Writer.WriteLine($" Position : {Convert_LatitudeText(linker.Pivot.Y)} , {Convert_LongitudeText(linker.Pivot.X)}");

                    switch (linker.FRID.PRIM)
                    {
                        case 1: { FeatureAttribute_Writer.WriteLine(" Primitive : Point"); } break;
                        case 2: { FeatureAttribute_Writer.WriteLine(" Primitive : Line"); } break;
                        case 3: { FeatureAttribute_Writer.WriteLine(" Primitive : Area"); } break;
                    }

                    FeatureAttribute_Writer.WriteLine(feature.Attribute);
                    FeatureAttribute_Writer.Flush();
                }
            }
            catch (Exception e)
            {

            }
        }

        public string Convert_LatitudeText(int latitude)
        {
            double Latitude = Math.Abs(latitude) / 10000000.0;

            double Remain;
            int Degree = (int)Latitude;
            Remain = (Latitude - Degree) * 60.0;
            int Minute = (int)Remain;
            Remain = (Remain - Minute) * 60.0;
            int Second = (int)Remain;
            Remain = (Remain - Second) * 10.0;

            return $"{Degree:00} {Minute:00}\' {Second:00}.{(int)Remain:00}\" {((latitude < 0) ? "S" : "N")}";
        }

        public string Convert_LongitudeText(int longitude)
        {
            double Longitude = Math.Abs(longitude) / 10000000.0;

            double Remain;
            int Degree = (int)Longitude;
            Remain = (Longitude - Degree) * 60.0;
            int Minute = (int)Remain;
            Remain = (Remain - Minute) * 60.0;
            int Second = (int)Remain;
            Remain = (Remain - Second) * 10.0;

            return $"{Degree:000} {Minute:00}\' {Second:00}.{(int)Remain:00}\" {((longitude < 0) ? "W" : "E")}";
        }


        public void Link_PL(DetectionChart chart)
        {
            if (chart.Feature == null) { return; }


            Dictionary<uint, DCC.UndGroup> UndGroup_Collection = Get_UndGroupCollection(chart);

            foreach (DCC.Feature Feature in chart.Feature)
            {
                if ((chart.FeatureLinker_Collection?.TryGetValue(Feature.FRID.RCID, out DCC.FeatureLinker? Linker) == true) && (Linker != null))
                {
                    if ((Linker.Shape != null) && (Feature.FRID.OBJL == 306) && !Feature.Get_ATVL(109, out string ATVL))
                    {
                        foreach (DCC.ShapeLinker Shape_Linker in Linker.Shape)
                        {
                            if (Shape_Linker.Edge != null)
                            {
                                foreach (DCC.EdgeLinker Edge_Linker in Shape_Linker.Edge)
                                {
                                    if (chart.Get_Vector(Edge_Linker.RCNM, Edge_Linker.RCID, out DCC.Vector Vector) && (Vector.Linked_Feature != null))
                                    {
                                        IEnumerable<DCC.Feature> Feature_Enumeration = Vector.Linked_Feature.Where(Feature => Feature.FRID.OBJL == 71);

                                        if (Feature_Enumeration.Count() > 0)
                                        {
                                            Edge_Linker.MASK = 1;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Compose_FeatureInformation(chart, Feature, Linker, UndGroup_Collection);
                }
            }
        }

        private Dictionary<uint, DCC.UndGroup> Get_UndGroupCollection(DetectionChart chart)
        {
            Dictionary<uint, DCC.UndGroup> UndGroup_Collection = new Dictionary<uint, DCC.UndGroup>();

            if (chart.Feature == null) { return UndGroup_Collection; }


            List<DCC.Feature> DangerFeature_Collection = new List<DCC.Feature>();
            List<DCC.SkinDanger> SkinDanger_Collection = new List<DCC.SkinDanger>();

            for (int i = 0; i < chart.Feature.Count; i++)
            {
                DCC.Feature Feature = chart.Feature[i];
                ushort OBJL = Feature.FRID.OBJL;
                
                if ((Feature.FRID.PRIM == 3) && ((OBJL == 42) || (OBJL == 46) || (OBJL == 154)))
                {
                    if (OBJL == 154)
                    {
                        DCC.SkinDanger Skin_Danger = new DCC.SkinDanger();
                        Skin_Danger.Feature_RCID = Feature.FRID.RCID;
                        Skin_Danger.Depth.Minimum = float.MaxValue;
                        Skin_Danger.Depth.Maximum = float.MaxValue;

                        SkinDanger_Collection.Add(Skin_Danger);
                    }
                    else
                    {
                        if (Feature.Get_ATVL(87, out string DRVAL1) && (DRVAL1 != "9999"))
                        {
                            DCC.SkinDanger Skin_Danger = new DCC.SkinDanger();
                            Skin_Danger.Feature_RCID = Feature.FRID.RCID;
                            Skin_Danger.Depth.Minimum = float.MaxValue;
                            Skin_Danger.Depth.Maximum = float.MaxValue;

                            float.TryParse(DRVAL1, out Skin_Danger.Depth.Minimum);

                            SkinDanger_Collection.Add(Skin_Danger);
                        }
                    }
                }
                else if ((OBJL == 86) || (OBJL == 153) || (OBJL == 159))
                {
                    DangerFeature_Collection.Add(Feature);
                }
            }

            foreach (DCC.Feature Danger_Feature in DangerFeature_Collection)
            {
                if ((chart.FeatureLinker_Collection?.TryGetValue(Danger_Feature.FRID.RCID, out DCC.FeatureLinker? Danger_Linker) == true) && (Danger_Linker != null)) // 사실 수집 방법을 보면 없을 수가 없음
                {
                    (float Least, float Maximum) Depth = (-1.0f, float.MaxValue);

                    foreach (DCC.SkinDanger Skin_Danger in SkinDanger_Collection)
                    {
                        if ((chart.FeatureLinker_Collection?.TryGetValue(Skin_Danger.Feature_RCID, out DCC.FeatureLinker? Skin_Linker) == true) && (Skin_Linker != null))
                        {
                            if (Check_FeatureCovered(Danger_Linker, Skin_Linker))
                            {
                                if ((Skin_Linker.FRID.OBJL == 154) && (Depth.Least == -1.0))
                                {
                                    Depth.Least = float.MaxValue;
                                }
                                else
                                {
                                    float DRVAL1 = Skin_Danger.Depth.Minimum;

                                    if (DRVAL1 != float.MaxValue)
                                    {
                                        if (Depth.Maximum == float.MaxValue)
                                        {
                                            Depth.Least = DRVAL1;
                                            Depth.Maximum = DRVAL1;
                                        }
                                        else
                                        {
                                            if (Depth.Maximum > DRVAL1)
                                            {
                                                Depth.Least = DRVAL1;
                                                Depth.Maximum = DRVAL1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    DCC.UndGroup Und_Group = new DCC.UndGroup();
                    Und_Group.Cover = (Depth.Least == float.MaxValue);
                    Und_Group.Depth = Depth;

                    UndGroup_Collection.TryAdd(Danger_Feature.FRID.RCID, Und_Group);
                }
            }

            return UndGroup_Collection;
        }

        private bool Check_FeatureCovered(DCC.FeatureLinker danger_linker, DCC.FeatureLinker skin_linker)
        {
            bool Result = false;

            JHLib.Util.Struct.Float2D[][] Skin;

            if (skin_linker.Shape != null)
            {
                List<DCC.ShapeLinker> Shape_Linker = skin_linker.Shape;
                Skin = new JHLib.Util.Struct.Float2D[Shape_Linker.Count][];

                for (int i = 0; i < Shape_Linker.Count; i++)
                {
                    List<DCC.SG2D>? Point = Shape_Linker[i].Point;
                    JHLib.Util.Struct.Float2D[] Shape;

                    if (Point != null)
                    {
                        Shape = new JHLib.Util.Struct.Float2D[Point.Count];

                        for (int j = 0; j < Point.Count; j++)
                        {
                            Shape[j].X = Point[j].XCOO;
                            Shape[j].Y = Point[j].YCOO;
                        }
                    }
                    else
                    {
                        Shape = Array.Empty<JHLib.Util.Struct.Float2D>();
                    }

                    Skin[i] = Shape;
                }
            }
            else
            {
                Skin = Array.Empty<JHLib.Util.Struct.Float2D[]>();
            }

            switch (danger_linker.FRID.PRIM)
            {
                case 1:
                    {
                        if (skin_linker.FRID.PRIM != 3) { return false; }

                        JHLib.Util.Struct.Float2D Pivot = new JHLib.Util.Struct.Float2D(danger_linker.Pivot.X, danger_linker.Pivot.Y);

                        Result = JHLib.Util.Geometry.GeometryHelper.PointInGeometry(Pivot, Skin);
                    }
                    break;
                case 2:
                    {
                        JHLib.Util.Geometry.Clipper2.Clipper2 Clipper = new JHLib.Util.Geometry.Clipper2.Clipper2();
                        JHLib.Util.Struct.Float2D[] Line;

                        if (danger_linker.Shape?.Count > 0)
                        {
                            List<DCC.SG2D>? Point = danger_linker.Shape[0].Point;

                            if (Point != null)
                            {
                                Line = new JHLib.Util.Struct.Float2D[Point.Count];

                                for (int i = 0; i < Point.Count; i++)
                                {
                                    Line[i].X = Point[i].XCOO;
                                    Line[i].Y = Point[i].YCOO;
                                }
                            }
                            else
                            {
                                Line = Array.Empty<JHLib.Util.Struct.Float2D>();
                            }
                        }
                        else
                        {
                            Line = Array.Empty<JHLib.Util.Struct.Float2D>();
                        }

                        Clipper.AddSubject(Line, true);
                        Clipper.AddClip(Skin);

                        Result = Clipper.Execute(0, 0);
                    }
                    break;
                case 3:
                    {
                        if (skin_linker.FRID.PRIM == 3)
                        {
                            JHLib.Util.Struct.Float2D[][] Danger;

                            if (danger_linker.Shape != null)
                            {
                                List<DCC.ShapeLinker> Shape_Linker = danger_linker.Shape;
                                Danger = new JHLib.Util.Struct.Float2D[Shape_Linker.Count][];

                                for (int i = 0; i < Shape_Linker.Count; i++)
                                {
                                    List<DCC.SG2D>? Point = Shape_Linker[i].Point;
                                    JHLib.Util.Struct.Float2D[] Shape;

                                    if (Point != null)
                                    {
                                        Shape = new JHLib.Util.Struct.Float2D[Point.Count];

                                        for (int j = 0; j < Point.Count; j++)
                                        {
                                            Shape[j].X = Point[j].XCOO;
                                            Shape[j].Y = Point[j].YCOO;
                                        }
                                    }
                                    else
                                    {
                                        Shape = Array.Empty<JHLib.Util.Struct.Float2D>();
                                    }

                                    Danger[i] = Shape;
                                }
                            }
                            else
                            {
                                Danger = Array.Empty<JHLib.Util.Struct.Float2D[]>();
                            }

                            JHLib.Util.Geometry.Clipper2.Clipper2 Clipper = new JHLib.Util.Geometry.Clipper2.Clipper2();
                            Clipper.AddSubject(Danger);
                            Clipper.AddClip(Skin);

                            Result = Clipper.Execute(0, 0);
                        }
                    }
                    break;
            }

            return Result;
        }

        private void Compose_FeatureInformation(DetectionChart chart, DCC.Feature feature, DCC.FeatureLinker linker, Dictionary<uint, DCC.UndGroup> und_group)
        {
            byte Type = 0;
            byte Loop = 0;

            switch (feature.FRID.PRIM)
            {
                case 1:
                    {
                        Type = 0;
                        Loop = 2;
                    }
                    break;
                case 2:
                    {
                        Type = 2;
                        Loop = 1;
                    }
                    break;
                case 3:
                    {
                        Type = 3;
                        Loop = (byte)(Using_Chart1 ? 1 : 2);
                    }
                    break;
            }

            if (feature.Error)
            {
                linker.Display_Group = 5;
                linker.Display_Category = 1;
                linker.Group_Layer = PL_401.Get_Group(21010);
                linker.Radar_Overlay = 0;

                switch (feature.FRID.PRIM)
                {
                    case 1:
                        for (int i = 0; i < Loop; i++)
                        {
                            DCC.DrawCommand Draw_Command = new DCC.DrawCommand();
                            Draw_Command.SY = new List<DCC.SY>() {
                                new DCC.SY() {
                                    Index = SymbolTable.Table.TryGetValue("QUESMRK1", out int Symbol_Index) ? Symbol_Index : -1,
                                },
                            };

                            linker.Draw_Command ??= new List<DCC.DrawCommand>();
                            linker.Draw_Command.Add(Draw_Command);
                        }
                        break;
                    case 2:
                        for (int i = 0; i < Loop; i++)
                        {
                            DCC.DrawCommand Draw_Command = new DCC.DrawCommand();
                            Draw_Command.LC = new List<DCC.LC>() {
                                new DCC.LC() {
                                    Index = LineTable.Table.TryGetValue("QUESMRK1", out int Line_Index) ? Line_Index : -1,
                                },
                            };

                            linker.Draw_Command ??= new List<DCC.DrawCommand>();
                            linker.Draw_Command.Add(Draw_Command);
                        }
                        break;
                    case 3:
                        for (int i = 0; i < Loop; i++)
                        {
                            DCC.DrawCommand Draw_Command = new DCC.DrawCommand();
                            Draw_Command.SY = new List<DCC.SY>() {
                                new DCC.SY() {
                                    Index = SymbolTable.Table.TryGetValue("QUESMRK1", out int Symbol_Index) ? Symbol_Index : -1,
                                },
                            };
                            Draw_Command.LC = new List<DCC.LC>() {
                                new DCC.LC() {
                                    Index = LineTable.Table.TryGetValue("QUESMRK1", out int Line_Index) ? Line_Index : -1,
                                },
                            };

                            linker.Draw_Command ??= new List<DCC.DrawCommand>();
                            linker.Draw_Command.Add(Draw_Command);
                        }
                        break;
                }
            }
            else
            {
                if (LookupTable.Get_Lookup(Type, linker.PL.Index0, Using_Chart1, out ENC.Lookup Lookup))
                {
                    linker.Display_Group = Lookup.Display_Group;
                    linker.Display_Category = Lookup.Display_Category;
                    linker.Group_Layer = PL_401.Get_Group(Lookup.Group_Layer);
                    linker.Radar_Overlay = Lookup.Radar_Overlay;

                    DCC.DrawCommand CS_Command = new DCC.DrawCommand();
                    int Resare_Type = -1;

                    if (Lookup.CS != null)
                    {
                        foreach (ENC.CS CS in Lookup.CS)
                        {
                            Resare_Type = Select_CS(chart, feature, linker, und_group, CS, CS_Command); // 그럼 resare는 등장할 때 항상 마지막에 나타나야 할 텐데
                            linker.CS = true;
                        }
                    }

                    for (int i = 0; i < Loop; i++)
                    {
                        DCC.DrawCommand Draw_Command = new DCC.DrawCommand();

                        if (LookupTable.Get_Lookup((byte)(Type + i), (i == 0) ? linker.PL.Index0 : linker.PL.Index1, Using_Chart1, out ENC.Lookup Linking_Lookup))
                        {
                            if (Linking_Lookup.SY != null)
                            {
                                foreach (ENC.SY Lookup_SY in Linking_Lookup.SY)
                                {
                                    DCC.SY SY = new DCC.SY()
                                    {
                                        Index = SymbolTable.Table.TryGetValue(Lookup_SY.Acronym, out int Symbol_Index) ? Symbol_Index : -1,
                                    };

                                    if (Lookup_SY.Degree?.Contains("ORIENT") == true)
                                    {
                                        if (feature.Get_ATVL(117, out string ORIENT) && float.TryParse(ORIENT, out float Orient))
                                        {
                                            SY.Angle = Orient;
                                        }
                                        else
                                        {
                                            SY.Angle = 0.0f;
                                        }
                                    }
                                    else
                                    {
                                        SY.Angle = float.TryParse(Lookup_SY.Degree, out float Orient) ? Orient : 0.0f;
                                    }

                                    Draw_Command.SY ??= new List<DCC.SY>();
                                    Draw_Command.SY.Add(SY);
                                }
                            }
                            // 여기서도 마찬가지로 느껴지는 게, acronym을 index로 바꾸는 거 외에 거의 동일한 형태인데 전체를 복사하다시피 하니 비효율적?
                            if (Linking_Lookup.TX != null)
                            {
                                foreach (ENC.TX Lookup_TX in Linking_Lookup.TX)
                                {
                                    string Text;
                                    string NationalText;

                                    if (!string.IsNullOrEmpty(Lookup_TX.Element))
                                    {
                                        if (Lookup_TX.Element == "NATSUR")
                                        {
                                            if ((feature.ATTF?.Count > 0) && (feature.ATTF[0].ATTL == 113) && (feature.ATTF[0].ATVL?.Length > 0))
                                            {
                                                Text = feature.ATTF[0].ATVL[0] switch {
                                                    "1" => "M",
                                                    "2" => "Cy",
                                                    "3" => "Si",
                                                    "4" => "S",
                                                    "5" => "St",
                                                    "6" => "G",
                                                    "7" => "P",
                                                    "8" => "Cb",
                                                    "9" => "R",
                                                    "11" => "R",
                                                    "14" => "Co",
                                                    "17" => "Sh",
                                                    "18" => "R",
                                                    _ => "",
                                                };

                                                if (feature.ATTF[0].ATVL.Length > 1)
                                                {
                                                    for (int j = 1; j < feature.ATTF[0].ATVL.Length; j++)
                                                    {
                                                        Text += $" {feature.ATTF[0].ATVL[j] switch {
                                                            "1" => "M",
                                                            "2" => "Cy",
                                                            "3" => "Si",
                                                            "4" => "S",
                                                            "5" => "St",
                                                            "6" => "G",
                                                            "7" => "P",
                                                            "8" => "Cb",
                                                            "9" => "R",
                                                            "11" => "R",
                                                            "14" => "Co",
                                                            "17" => "Sh",
                                                            "18" => "R",
                                                            _ => "",
                                                        }}";
                                                    }
                                                }

                                                NationalText = Text;
                                            }
                                            else
                                            {
                                                Text = "";
                                                NationalText = "";
                                            }
                                        }
                                        else
                                        {
                                            (string Text, string NationalText, int Type) Attribute_Information = feature.Get_AttributeInformation(Lookup_TX.Element);

                                            Text = Attribute_Information.Text;
                                            NationalText = !string.IsNullOrEmpty(Attribute_Information.NationalText) ? Attribute_Information.NationalText : Attribute_Information.Text;
                                        }
                                    }
                                    else
                                    {
                                        Text = Lookup_TX.Text;
                                        NationalText = Lookup_TX.Text;
                                    }
                                    
                                    if (!string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(NationalText))
                                    {
                                        Draw_Command.TX ??= new List<DCC.TX>();
                                        Draw_Command.TX.Add(new DCC.TX() {
                                            Text = Text.Replace("\r", null).Replace("\n", null),
                                            NationalText = NationalText.Replace("\r", null).Replace("\n", null),
                                            Align = (byte)((Lookup_TX.Font_HJUST * 10) + Lookup_TX.Font_VJUST),
                                            Offset = Lookup_TX.Font_Offset,
                                            Text_Group = (byte)Lookup_TX.Font_Group,
                                            Text_ColorIndex = (byte)(ColorTable.Table.TryGetValue(Lookup_TX.Font_ColorAcronym, out int Color_Index) ? Color_Index : 255),
                                        });
                                    }
                                }
                            }

                            if (Linking_Lookup.TE != null)
                            {
                                foreach (ENC.TE Lookup_TE in Linking_Lookup.TE)
                                {
                                    if (!string.IsNullOrEmpty(Lookup_TE.Element))
                                    {
                                        string Text;
                                        string NationalText;

                                        if (Lookup_TE.Element == "NATSUR")
                                        {
                                            if ((feature.ATTF?.Count > 0) && (feature.ATTF[0].ATTL == 113) && (feature.ATTF[0].ATVL?.Length > 0))
                                            {
                                                Text = feature.ATTF[0].ATVL[0] switch {
                                                    "1" => "M",
                                                    "2" => "Cy",
                                                    "3" => "Si",
                                                    "4" => "S",
                                                    "5" => "St",
                                                    "6" => "G",
                                                    "7" => "P",
                                                    "8" => "Cb",
                                                    "9" => "R",
                                                    "11" => "R",
                                                    "14" => "Co",
                                                    "17" => "Sh",
                                                    "18" => "R",
                                                    _ => "",
                                                };

                                                if (feature.ATTF[0].ATVL.Length > 1) {
                                                    for (int j = 1; j < feature.ATTF[0].ATVL.Length; j++)
                                                    {
                                                        Text += $" {feature.ATTF[0].ATVL[j] switch {
                                                            "1" => "M",
                                                            "2" => "Cy",
                                                            "3" => "Si",
                                                            "4" => "S",
                                                            "5" => "St",
                                                            "6" => "G",
                                                            "7" => "P",
                                                            "8" => "Cb",
                                                            "9" => "R",
                                                            "11" => "R",
                                                            "14" => "Co",
                                                            "17" => "Sh",
                                                            "18" => "R",
                                                            _ => "",
                                                        }}";
                                                    }
                                                }

                                                NationalText = Text;
                                            }
                                            else
                                            {
                                                Text = "";
                                                NationalText = "";
                                            }
                                        }
                                        else
                                        {
                                            (string Text, string NationalText, int Type) Attribute_Information = feature.Get_AttributeInformation(Lookup_TE.Element);

                                            if (Attribute_Information.Type == 0)
                                            {
                                                Text = !string.IsNullOrEmpty(Attribute_Information.Text) ? Interpret_TE(Lookup_TE.Format, Attribute_Information.Text) : "";
                                                NationalText = !string.IsNullOrEmpty(Attribute_Information.NationalText) ? Interpret_TE(Lookup_TE.Format, Attribute_Information.NationalText) : Text;
                                            }
                                            else
                                            {
                                                Text = !string.IsNullOrEmpty(Attribute_Information.Text) ? Interpret_TE(Lookup_TE.Format, Attribute_Information.Text, Attribute_Information.Type) : "";
                                                NationalText = Text;
                                            }
                                        }
                                        
                                        if (!string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(NationalText))
                                        {
                                            Draw_Command.TX ??= new List<DCC.TX>();
                                            Draw_Command.TX.Add(new DCC.TX() {
                                                Text = Text.Replace("\r", null).Replace("\n", null),
                                                NationalText = NationalText.Replace("\r", null).Replace("\n", null),
                                                Align = (byte)((Lookup_TE.Font_HJUST * 10) + Lookup_TE.Font_VJUST),
                                                Offset = Lookup_TE.Font_Offset,
                                                Text_Group = (byte)Lookup_TE.Font_Group,
                                                Text_ColorIndex = (byte)(ColorTable.Table.TryGetValue(Lookup_TE.Font_ColorAcronym, out int Color_Index) ? Color_Index : 255),
                                            });
                                        }
                                    }
                                }
                            }

                            if (Linking_Lookup.LS != null)
                            {
                                foreach (ENC.LS Lookup_LS in Linking_Lookup.LS)
                                {
                                    Draw_Command.LS ??= new List<DCC.LS>();
                                    Draw_Command.LS.Add(new DCC.LS() {
                                        Pen_Type = Lookup_LS.Pen_Type,
                                        Pen_Width = Lookup_LS.Pen_Width,
                                        Pen_ColorIndex = ColorTable.Table.TryGetValue(Lookup_LS.Pen_ColorAcronym, out int Color_Index) ? Color_Index : -1,
                                    });
                                }
                            }

                            if (Linking_Lookup.LC != null)
                            {
                                foreach (ENC.LC Lookup_LC in Linking_Lookup.LC)
                                {
                                    Draw_Command.LC ??= new List<DCC.LC>();
                                    Draw_Command.LC.Add(new DCC.LC() {
                                        Index = LineTable.Table.TryGetValue(Lookup_LC.Acronym, out int Line_Index) ? Line_Index : -1,
                                    });
                                }
                            }

                            if (Linking_Lookup.AC != null)
                            {
                                foreach (ENC.AC Lookup_AC in Linking_Lookup.AC)
                                {
                                    Draw_Command.AC ??= new List<DCC.AC>();
                                    Draw_Command.AC.Add(new DCC.AC() {
                                        Index = ColorTable.Table.TryGetValue(Lookup_AC.Acronym, out int Color_Index) ? Color_Index : -1,
                                        Trans = Lookup_AC.Trans,
                                    });
                                }
                            }

                            if (Linking_Lookup.AP != null)
                            {
                                foreach (ENC.AP Lookup_AP in Linking_Lookup.AP)
                                {
                                    Draw_Command.AP ??= new List<DCC.AP>();
                                    Draw_Command.AP.Add(new DCC.AP() {
                                        Index = PatternTable.Table.TryGetValue(Lookup_AP.Acronym, out int Pattern_Index) ? Pattern_Index : -1,
                                    });
                                }
                            }

                            if (Linking_Lookup.CS?.Count > 0)
                            {
                                if (CS_Command.AC != null)
                                {
                                    Draw_Command.AC ??= new List<DCC.AC>();
                                    Draw_Command.AC.AddRange(CS_Command.AC);
                                }

                                if (CS_Command.AP != null)
                                {
                                    Draw_Command.AP ??= new List<DCC.AP>();
                                    Draw_Command.AP.AddRange(CS_Command.AP);
                                }

                                if (CS_Command.LC != null)
                                {
                                    Draw_Command.LC ??= new List<DCC.LC>();
                                    Draw_Command.LC.AddRange(CS_Command.LC);
                                }

                                if (CS_Command.LS != null)
                                {
                                    Draw_Command.LS ??= new List<DCC.LS>();
                                    Draw_Command.LS.AddRange(CS_Command.LS);
                                }

                                if (CS_Command.SY != null)
                                {
                                    Draw_Command.SY ??= new List<DCC.SY>();
                                    Draw_Command.SY.AddRange(CS_Command.SY);
                                }

                                if (CS_Command.TX != null)
                                {
                                    Draw_Command.TX ??= new List<DCC.TX>();
                                    Draw_Command.TX.AddRange(CS_Command.TX);
                                }
                            }

                            if (Draw_Command.Commanding)
                            {
                                linker.Draw_Command ??= new List<DCC.DrawCommand>();
                                linker.Draw_Command.Add(Draw_Command);
                            }
                        }
                    }
                    
                    if ((0 < Resare_Type) && (Resare_Type < 7))
                    {
                        DCC.LS LS = new DCC.LS()
                        {
                            Pen_Type = 1,
                            Pen_Width = 2,
                            Pen_ColorIndex = ColorTable.Table.TryGetValue("CHMGD", out int Color_Index) ? Color_Index : -1,
                        };

                        if (Using_Chart1)
                        {
                            DCC.LC LC = new DCC.LC()
                            {
                                Index = LineTable.Table.TryGetValue(Resare_Type switch {
                                    1 => "ENTRES51",
                                    2 => "ACHRES51",
                                    3 => "FSHRES51",
                                    4 or 5 or 6 => "CTYARE51",
                                    _ => "",
                                }, out int Line_Index) ? Line_Index : -1,
                            };

                            if (linker.Draw_Command?.Count > 0)
                            {
                                DCC.DrawCommand Draw_Command = linker.Draw_Command[0];

                                Draw_Command.LC ??= new List<DCC.LC>();
                                Draw_Command.LC.Add(LC);
                            }
                            else
                            {
                                linker.Draw_Command ??= new List<DCC.DrawCommand>();
                                linker.Draw_Command.Add(new DCC.DrawCommand() {
                                    LC = new List<DCC.LC>() { LC },
                                });
                            }
                        }
                        else
                        {
                            if (linker.Draw_Command?.Count > 0)
                            {
                                DCC.DrawCommand Draw_Command = linker.Draw_Command[0];

                                Draw_Command.LS ??= new List<DCC.LS>();
                                Draw_Command.LS.Add(LS);

                                if (linker.Draw_Command.Count > 1)
                                {
                                    Draw_Command = linker.Draw_Command[1];

                                    Draw_Command.LC ??= new List<DCC.LC>();
                                    Draw_Command.LC.Add(new DCC.LC() {
                                        Index = LineTable.Table.TryGetValue(Resare_Type switch {
                                            1 => "ENTRES51",
                                            2 => "ACHRES51",
                                            3 => "FSHRES51",
                                            4 or 5 or 6 => "CTYARE51",
                                            _ => "",
                                        }, out int Line_Index) ? Line_Index : -1,
                                    });
                                }
                            }
                            else
                            {
                                for (int i = 0; i < Loop; i++)
                                {
                                    linker.Draw_Command ??= new List<DCC.DrawCommand>();
                                    linker.Draw_Command.Add(new DCC.DrawCommand() {
                                        LS = new List<DCC.LS>() { LS },
                                    });
                                }
                            }
                        }
                    }
                }

                if (feature.Get_ATVL(133, out string ATVL) && (ATVL != "9999"))
                {
                    int.TryParse(ATVL, out linker.Minimum_Scale);
                }
                else
                {
                    linker.Minimum_Scale = int.MaxValue;
                }
            }
        }


        public void Link_EdgeMask(DetectionChart chart)
        {
            if (chart.Feature == null) { return; }


            foreach (DCC.Feature Feature in chart.Feature)
            {
                if ((chart.FeatureLinker_Collection?.TryGetValue(Feature.FRID.RCID, out DCC.FeatureLinker? Linker) == true) && (Linker != null))
                {
                    if ((Linker.FRID.OBJL == 42) || (Linker.FRID.OBJL == 46)) { continue; }

                    if (Linker.Shape != null)
                    {
                        int Edge_Number = 0;
                        
                        foreach (DCC.ShapeLinker Shape_Linker in Linker.Shape)
                        {
                            if (Shape_Linker.Edge != null)
                            {
                                foreach (DCC.EdgeLinker Edge_Linker in Shape_Linker.Edge)
                                {
                                    Edge_Number++;

                                    if (chart.Get_Vector(Edge_Linker.RCNM, Edge_Linker.RCID, out DCC.Vector Vector) && (Vector.Linked_Feature != null))
                                    {
                                        foreach (DCC.Feature Linked_Feature in Vector.Linked_Feature)
                                        {
                                            if ((Linked_Feature.FRID.RCID != Linker.FRID.RCID) && chart.FeatureLinker_Collection.TryGetValue(Linked_Feature.FRID.RCID, out DCC.FeatureLinker? Mask) && (Mask != null))
                                            {
                                                if (Mask.FRID.OBJL == 43) { continue; }

                                                if (Linker.Display_Group < Mask.Display_Group)
                                                {
                                                    bool ManualUpdate_LC = false;

                                                    if (Mask.Draw_Command != null)
                                                    {
                                                        foreach (DCC.DrawCommand Draw_Command in Mask.Draw_Command)
                                                        {
                                                            if (Draw_Command.LC != null)
                                                            {
                                                                IEnumerable<DCC.LC> LC_Enumeration = Draw_Command.LC.Where(LC => ((LC.Index == 6) || (LC.Index == 7)));

                                                                if (LC_Enumeration.Count() > 0)
                                                                {
                                                                    ManualUpdate_LC = true;
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    
                                                    if (!ManualUpdate_LC)
                                                    {
                                                        Edge_Linker.MASK = 1;
                                                        Linker.Edge_Masked = true;
                                                    }
                                                }
                                                else
                                                {
                                                    switch (Mask.FRID.OBJL)
                                                    {
                                                        //case 42: // DEPARE에 대해서는 처리 안해야 맞음(160830) 이라고 함
                                                        case 46:
                                                        case 86:
                                                        case 159:
                                                            {
                                                                Linker.Edge_Mask ??= new List<DCC.EdgeMask>();
                                                                Linker.Edge_Mask.Add(new DCC.EdgeMask()
                                                                {
                                                                    RCID = Linked_Feature.FRID.RCID,
                                                                    Number = Edge_Number - 1,
                                                                    Type = Mask.FRID.OBJL switch {
                                                                        //42 => 0, // DEPARE에 대해서는 처리 안해야 맞음(160830) 이라고 함
                                                                        46 => 1,
                                                                        86 => 2,
                                                                        159 => 3,
                                                                        _ => 255,
                                                                    },
                                                                });
                                                            }
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}