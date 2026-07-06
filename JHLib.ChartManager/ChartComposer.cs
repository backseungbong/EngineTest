using JHLib.ChartManager.Catalogue;
using JHLib.ChartManager.Chart.Detection;
using JHLib.ChartManager.Configuration;
using JHLib.ChartManager.Record;
using JHLib.ChartManager.Report;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace JHLib.ChartManager
{
    public class ChartComposer(ChartManagerLogger logger)
    {
        public ChartAttributeComposer CAC { get; private set; } = new ChartAttributeComposer();
        public ChartSencComposer CSC { get; private set; } = new ChartSencComposer();

        private ChartManagerLogger logger = logger;



        public void DetectChart(string chartName)
        {
            DirectoryInfo storageDirectory = new DirectoryInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName));

            if (storageDirectory.Exists)
            {
                FileInfo[] storageFile = storageDirectory.GetFiles("*", SearchOption.TopDirectoryOnly);

                List<(int UPDN, FileInfo cellFile)> cellFileCollection = new List<(int UPDN, FileInfo cellFile)>();
                FileInfo? baseCellFile = null;

                foreach (FileInfo file in storageFile)
                {
                    if (file.Name.StartsWith(chartName) &&
                        int.TryParse(file.Extension.Replace(".", ""), out int UPDN))
                    {
                        if ((baseCellFile == null) && (UPDN == 0))
                        {
                            baseCellFile = file;
                        }

                        cellFileCollection.Add((UPDN, file));
                    }
                }

                if (baseCellFile != null)
                {
                    DetectionCell baseCell = new DetectionCell();
                    FileInfo boundaryFile = new FileInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName, "BOUNDARY"));

                    if (boundaryFile.Exists)
                    {

                    }




                }
                else
                {

                }
            }
            else
            {

            }
        }

        public void LinkChart()
        {

        }

        public (Legacy.ECM_Core.Chart.DetectionChart? detectionChart, ChartDetectionReport? detectionReport) LegacyDetectChart(string chartName, ENC.SerialEnc? serialEnc)
        {
            DirectoryInfo storageDirectory = new DirectoryInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName));

            if (storageDirectory.Exists)
            {
                FileInfo[] Cell_FileInfo = storageDirectory.GetFiles("*", SearchOption.TopDirectoryOnly);
                List<(int UPDN, FileInfo File)> Cell_Collection = new List<(int UPDN, FileInfo File)>();

                foreach (FileInfo Cell in Cell_FileInfo)
                {
                    if (Cell.Name.StartsWith(chartName) && int.TryParse(Cell.Extension.Replace(".", ""), out int UPDN))
                    {
                        Cell_Collection.Add((UPDN, Cell));
                    }
                }

                (int UPDN, FileInfo? File) BaseCell = Cell_Collection.FirstOrDefault(Cell => Cell.UPDN == 0);

                if (BaseCell.File != null)
                {
                    FileInfo Boundary_FileInfo = new FileInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName, "BOUNDARY"));

                    Legacy.ECM_Core.Chart.DetectionCell Base_Cell = new Legacy.ECM_Core.Chart.DetectionCell();

                    if (Boundary_FileInfo.Exists)
                    {
                        string[] Boundary = Boundary_FileInfo.OpenText().ReadToEnd().Split(',');

                        if (Boundary.Length > 3)
                        {
                            (double North, double South, double East, double West) Bound = (
                                double.TryParse(Boundary[0], out double North) ? North : 0.0,
                                double.TryParse(Boundary[1], out double South) ? South : 0.0,
                                double.TryParse(Boundary[2], out double East) ? East : 0.0,
                                double.TryParse(Boundary[3], out double West) ? West : 0.0
                            );

                            Base_Cell.Boundary = Bound;
                        }
                    }

                    Base_Cell.Read(BaseCell.File.OpenRead());

                    int Reference = ((2 * Base_Cell.Update_Number) + Cell_Collection.Count - 1) * Cell_Collection.Count / 2;
                    int Sum = Base_Cell.Update_Number + Cell_Collection.Select(Cell => Cell.UPDN).Sum();

                    if ((Base_Cell.DSID.DSNM == BaseCell.File.Name) && (Reference == Sum))
                    {
                        Legacy.ECM_Core.Chart.DetectionChart Detection_Chart = new Legacy.ECM_Core.Chart.DetectionChart(Base_Cell);
                        Detection_Chart.Name = chartName;
                        Detection_Chart.serialEnc = serialEnc;

                        if (ChartCatalogue.catalogue.TryGetValue(chartName, out ChartRecord? chartRecord))
                        {
                            Detection_Chart.UpdateReference = chartRecord.updateVersion ?? 0;
                        }

                        foreach ((int UPDN, FileInfo File) Cell in Cell_Collection.Where(Cell => Cell.UPDN > 0).OrderBy(Cell => Cell.UPDN))
                        {
                            Legacy.ECM_Core.Chart.DetectionCell Update_Cell = new Legacy.ECM_Core.Chart.DetectionCell();
                            Update_Cell.DSPM = Base_Cell.DSPM;
                            Update_Cell.Boundary = Detection_Chart.Boundary;
                            Update_Cell.Read(Cell.File.OpenRead());

                            if ((Update_Cell.DSID.DSNM == Cell.File.Name) && (Update_Cell.Update_Number == Cell.UPDN) && (Update_Cell.Edition_Number == Detection_Chart.Base.EDTN) && (Update_Cell.Update_Number > Detection_Chart.Update))
                            {
                                Detection_Chart.Absorb(Update_Cell);
                            }
                            else
                            {
                                break;
                            }
                        }

                        return (Detection_Chart, null);
                    }
                    else if ((Base_Cell.DSID.DSNM == BaseCell.File.Name) && (Reference != Sum))
                    {
                        Legacy.ECM_Core.Chart.DetectionChart Detection_Chart = new Legacy.ECM_Core.Chart.DetectionChart(Base_Cell);
                        Detection_Chart.Name = chartName;
                        Detection_Chart.serialEnc = serialEnc;

                        if (ChartCatalogue.catalogue.TryGetValue(chartName, out ChartRecord? chartRecord))
                        {
                            Detection_Chart.UpdateReference = chartRecord.updateVersion ?? 0;
                        }

                        List<(int UPDN, FileInfo File)> orderedCellCollection = Cell_Collection.Where(Cell => Cell.UPDN > 0).OrderBy(Cell => Cell.UPDN).ToList();
                        int lastUpdate = 0;

                        foreach ((int UPDN, FileInfo File) cell in orderedCellCollection)
                        {
                            if (cell.UPDN == (lastUpdate + 1))
                            {
                                Legacy.ECM_Core.Chart.DetectionCell Update_Cell = new Legacy.ECM_Core.Chart.DetectionCell();
                                Update_Cell.DSPM = Base_Cell.DSPM;
                                Update_Cell.Boundary = Detection_Chart.Boundary;
                                Update_Cell.Read(cell.File.OpenRead());

                                if ((Update_Cell.DSID.DSNM == cell.File.Name) && (Update_Cell.Update_Number == cell.UPDN) && (Update_Cell.Edition_Number == Detection_Chart.Base.EDTN) && (Update_Cell.Update_Number > Detection_Chart.Update))
                                {
                                    Detection_Chart.Absorb(Update_Cell);
                                    lastUpdate++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        for (int i = lastUpdate; i < orderedCellCollection.Count; i++)
                        {
                            FileInfo updateFile = orderedCellCollection[i].File;

                            SSE.InvokeStandardError(SSE.StandardError.ERROR_23, updateFile.Name);
                            this.logger.Info($"SSE23|{updateFile.Name}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_23, updateFile.Name)}");

                            if (updateFile.Exists)
                            {
                                updateFile.Delete();
                            }
                        }

                        return (Detection_Chart, null);
                    }
                    else
                    {
                        ChartDetectionReport detectionReport = new ChartDetectionReport(chartName) {
                            reason = $"Invalid DSNM ({chartName})",
                        };
                        this.logger.Info($"     |{chartName}|AUTO|{detectionReport.reason}");

                        return (null, detectionReport);
                    }
                }
                else
                {
                    ChartDetectionReport detectionReport = new ChartDetectionReport(chartName);
                    detectionReport.reason = $"Not Exsists BaseCell ({chartName})";

                    SSE.InvokeStandardError(SSE.StandardError.ERROR_23, chartName);
                    this.logger.Info($"SSE23|{chartName}|AUTO|{SSE.GetMessage(SSE.StandardError.ERROR_23, chartName)}");

                    return (null, detectionReport);
                }
            }
            else
            {
                return (null, null);
            }
        }

        public bool LegacyLinkChart(Legacy.ECM_Core.ECM_CORE legacyCore, Legacy.ECM_Core.Chart.DetectionChart chart)
        {
            try
            {
                LegacyLinkFeatureAttribute(chart);
                LegacyLinkFeatureInformation(legacyCore, chart);
                legacyCore.Chart_Composer.Link_PL(chart);

                if (!chart.Name.Contains("KRINDEX"))
                {
                    legacyCore.Chart_Composer.Link_EdgeMask(chart);
                }

                chart.Linked = true;

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private void LegacyLinkFeatureAttribute(Legacy.ECM_Core.Chart.DetectionChart chart)
        {
            if (chart.Feature == null) { return; }

            foreach (Legacy.ECM_Core.DCC.Feature feature in chart.Feature)
            {
                FeatureAttributeRecord faRecord = new FeatureAttributeRecord(feature.FRID.RCID, feature.FRID.OBJL);

                LegacyLinkATTF(chart, feature, faRecord);
                LegacyLinkNATF(chart, feature);

                feature.faRecord = faRecord;
            }
        }

        private void LegacyLinkATTF(Legacy.ECM_Core.Chart.DetectionChart chart, Legacy.ECM_Core.DCC.Feature feature, FeatureAttributeRecord faRecord)
        {
            if (feature.ATTF == null) { return; }

            foreach (Legacy.ECM_Core.DCC.ATTF ATTF in feature.ATTF)
            {
                if (Legacy.ECM_Core.Catalogue.AttributeCatalogue.Catalogue.TryGetValue(ATTF.ATTL, out Legacy.ECM_Core.ENC.Attribute attribute))
                {
                    //if (feature.Update_Type == 2)
                    //{
                    //    feature.Valid_Date.Start = "1111";
                    //    feature.Valid_Date.End = "1111";
                    //}

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
                                }
                                break;
                        }
                    }

                    FeatureAttributeRecord.Attribute attributeRecord = new FeatureAttributeRecord.Attribute() {
                        name = attribute.Attribute_Name,
                        acronym = attribute.Acronym,
                    };

                    switch (attribute.Attribute_Type)
                    {
                        case "E":
                            if (ATTF.ATVL.Length > 0)
                            {
                                attributeRecord.value = LegacyLinkAttributeTypeE(feature, ATTF, attribute);
                            }
                            break;
                        case "L":
                            if (ATTF.ATVL.Length > 0)
                            {
                                attributeRecord.value = LegacyLinkAttributeTypeL(feature, ATTF, attribute);
                            }
                            break;
                        case "F":
                            if (ATTF.ATVL.Length > 0)
                            {
                                attributeRecord.value = LegacyLinkAttributeTypeF(feature, ATTF, attribute, chart.DSPM.DUNI);
                            }
                            break;
                        case "S":
                            if (ATTF.ATVL.Length > 0)
                            {
                                attributeRecord.value = LegacyLinkAttributeTypeS(feature, ATTF, attribute, chart.Name);
                            }
                            break;
                        default:
                            if (feature.FRID.OBJL == 138)
                            {
                                attributeRecord.value = LegacyLinkAttributeTypeTidal(ATTF);
                            }
                            else if (ATTF.ATVL.Length > 0)
                            {
                                attributeRecord.value = LegacyLinkAttributeTypeGeneric(feature, ATTF);
                            }
                            break;
                    }

                    switch (ATTF.ATTL)
                    {
                        case 85:
                        case 118:
                        case 86:
                        case 119:
                        case 79:
                        case 115:
                        case 128:
                        case 147:
                        case 151:
                        case 152:
                            if ((feature.Valid_Date.Start != "1111") &&
                                (attributeRecord.value?.Length >= 8) &&
                                int.TryParse(attributeRecord.value[4..6], out int month))
                            {
                                string monthName = month switch {
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

                                attributeRecord.value = $"{attributeRecord.value[6..8]}-{monthName}-{attributeRecord.value[0..4]}";
                            }
                            break;
                    }

                    if (ATTF.ATTL > 402 && !((8194 <= ATTF.ATTL) && (ATTF.ATTL <= 8224)))
                    {
                        // ???
                    }

                    faRecord.attribute.Add(attributeRecord);
                }
            }

            if (feature.FRID.OBJL == 81)
            {
                string VALACM = feature.VALACM;
                string VALMAG = feature.VALMAG;
                string RYRMGV = feature.RYRMGV;

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

                faRecord.attribute.Add(new FeatureAttributeRecord.Attribute() {
                    value = $"{VALMAG_Degree}({VALACM}'{VALACM_Direction})",
                });
            }
        }

        private string LegacyLinkAttributeTypeE(Legacy.ECM_Core.DCC.Feature feature, Legacy.ECM_Core.DCC.ATTF ATTF, Legacy.ECM_Core.ENC.Attribute attribute)
        {
            string attributeValue = "";

            if (int.TryParse(ATTF.ATVL[0], out int EM))
            {
                if (EM == 9999)
                {
                    attributeValue = "<unknown>";
                }
                else if (EM > 50)
                {
                    attributeValue = "?";
                }
                else if ((0 < EM) && (EM <= attribute.Attribute_Element.Length))
                {
                    attributeValue = attribute.Attribute_Element[EM - 1];
                }
            }

            feature.Set_AttributeInformation(ATTF.ATTL, attributeValue);

            return attributeValue;
        }

        private string LegacyLinkAttributeTypeL(Legacy.ECM_Core.DCC.Feature feature, Legacy.ECM_Core.DCC.ATTF ATTF, Legacy.ECM_Core.ENC.Attribute attribute)
        {
            StringBuilder attributeValueBuilder = new StringBuilder();

            for (int i = 0; i < ATTF.ATVL.Length; i++)
            {
                if (int.TryParse(ATTF.ATVL[i], out int EM))
                {
                    if ((EM == 0) || (EM == 9999))
                    {
                        attributeValueBuilder.Append("<unknown>");
                    }
                    else
                    {
                        if ((0 < EM) && (EM <= attribute.Attribute_Element.Length))
                        {
                            attributeValueBuilder.Append(attribute.Attribute_Element[EM - 1]);
                        }
                        else
                        {
                            attributeValueBuilder.Append("?");
                        }
                    }

                    if (i < (ATTF.ATVL.Length - 1)) { attributeValueBuilder.Append(','); }
                }
            }

            string attributeValue = attributeValueBuilder.ToString();

            feature.Set_AttributeInformation(ATTF.ATTL, attributeValue);

            return attributeValue;
        }

        private string LegacyLinkAttributeTypeF(Legacy.ECM_Core.DCC.Feature feature, Legacy.ECM_Core.DCC.ATTF ATTF, Legacy.ECM_Core.ENC.Attribute attribute, byte DUNI)
        {
            string attributeValue = "";

            if ((ATTF.ATVL.Length == 0) || ATTF.ATVL[0].Contains("9999"))
            {
                attributeValue = "<unknown>";
            }
            else
            {
                string attributeUnit = ATTF.ATTL switch {
                    78 => "mm",
                    84 => "kr",
                    91 or 177 or 178 => "M",
                    106 => "t",
                    126 => "m",
                    117 or 136 or 137 or 176 => "degree",
                    138 => "minute",
                    139 => "Hz",
                    175 => "'",
                    142 or 143 => "s",
                    87 or 88 or 144 or 174 or 179 => DUNI switch {
                        1 => "m",
                        2 => "fathoms and feet",
                        3 => "feet",
                        4 => "fathoms and fractions",
                        _ => "",
                    },
                    5 or 90 or 95 or 97 or 98 or 99 or 100 or 101 or
                    127 or 145 or 146 or 180 or 181 or 182 or 183 or 184 or 186 => DUNI switch {
                        1 => "m",
                        2 => "feet",
                        _ => "",
                    },
                    401 => DUNI switch {
                        1 => "m",
                        2 => "degree of arc",
                        3 => "milimeters",
                        4 => "feet",
                        5 => "cables",
                        _ => "",
                    },
                    _ => "",
                };

                attributeValue = $"{ATTF.ATVL[0]} {attributeUnit}";

                feature.Set_AttributeInformation(ATTF.ATTL, ATTF.ATVL[0]);
            }

            return attributeValue;
        }

        private string LegacyLinkAttributeTypeS(Legacy.ECM_Core.DCC.Feature feature, Legacy.ECM_Core.DCC.ATTF ATTF, Legacy.ECM_Core.ENC.Attribute attribute, string chartName)
        {
            string attributeValue = "";

            switch (ATTF.ATTL)
            {
                case 158:
                    if (!ATTF.ATVL[0].Contains("9999"))
                    {
                        FileInfo subContentFile = new FileInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName, ATTF.ATVL[0]));

                        if (subContentFile.Exists)
                        {
                            using (StreamReader reader = new StreamReader(subContentFile.OpenRead()))
                            {
                                attributeValue = reader.ReadToEnd();
                            }

                            feature.Set_AttributeInformation(ATTF.ATTL, attributeValue);
                        }
                        else
                        {
                            attributeValue = ATTF.ATVL[0];

                            feature.Set_AttributeInformation(ATTF.ATTL, ATTF.ATVL[0]);
                        }
                    }
                    break;
                case 120:
                    if (!ATTF.ATVL[0].Contains("9999"))
                    {
                        FileInfo subContentFile = new FileInfo(Path.Combine(DirectoryConfiguration.encDownload, chartName, ATTF.ATVL[0]));

                        if (subContentFile.Exists)
                        {
                            FileInfo destination = new FileInfo(Path.Combine(DirectoryConfiguration.encAttribute, chartName, ATTF.ATVL[0]));

                            if (destination.Directory?.Exists == false) { destination.Directory.Create(); }

                            try
                            {
                                File.Copy(subContentFile.FullName, destination.FullName, true);
                            }
                            catch (Exception e)
                            {

                            }
                        }

                        attributeValue = ATTF.ATVL[0];

                        feature.Set_AttributeInformation(ATTF.ATTL, ATTF.ATVL[0]);
                    }
                    break;
                default:
                    if (!ATTF.ATVL[0].Contains("9999"))
                    {
                        attributeValue = ATTF.ATVL[0];

                        feature.Set_AttributeInformation(ATTF.ATTL, ATTF.ATVL[0]);
                    }
                    break;
            }

            return attributeValue;
        }

        private string LegacyLinkAttributeTypeTidal(Legacy.ECM_Core.DCC.ATTF ATTF)
        {
            StringBuilder attributeValueBuilder = new StringBuilder();

            if (ATTF.ATVL.Length == 29)
            {
                attributeValueBuilder.AppendLine("------------------------------------------------------------------------------------------------------");

                string HL = "";
                int Index = 0;
                int Hours = -6;

                for (int i = 0; i < 16; i++)
                {
                    if (i == 0)
                    {
                        attributeValueBuilder.AppendLine($"Tidal Station : {ATTF.ATVL[Index++]}");
                        attributeValueBuilder.AppendLine("------------------------------------------------------------------------------------------------------");
                    }
                    else if (i == 1)
                    {
                        attributeValueBuilder.AppendLine($"Tidal Station Identifier : {ATTF.ATVL[Index++]}");
                        attributeValueBuilder.AppendLine("------------------------------------------------------------------------------------------------------");
                        attributeValueBuilder.AppendLine("              | Hours |  Direction of stream(degrees) | Rates at spring tide(knots) ");
                        attributeValueBuilder.AppendLine("------------------------------------------------------------------------------------------------------");
                    }
                    else if (i == 2)
                    {
                        HL = ATTF.ATVL[Index++];
                    }
                    else
                    {
                        if (Hours < 0)
                        {
                            attributeValueBuilder.Append("Before");
                        }
                        else if (Hours == 0)
                        {
                            attributeValueBuilder.Append($"{HL}    ");
                        }
                        else
                        {
                            attributeValueBuilder.Append("After ");
                        }

                        attributeValueBuilder.AppendLine($"   | {Hours}    |   {ATTF.ATVL[Index]}                                        |   {ATTF.ATVL[Index + 1]}");
                        attributeValueBuilder.AppendLine("------------------------------------------------------------------------------------------------------");

                        Hours++;
                        Index += 2;
                    }
                }
            }

            return attributeValueBuilder.ToString();
        }

        private string LegacyLinkAttributeTypeGeneric(Legacy.ECM_Core.DCC.Feature feature, Legacy.ECM_Core.DCC.ATTF ATTF)
        {
            string attributeValue = ATTF.ATVL[0].Contains("9999") ? "<unknown>" : ATTF.ATVL[0];

            feature.Set_AttributeInformation(ATTF.ATTL, attributeValue);

            return attributeValue;
        }

        private void LegacyLinkNATF(Legacy.ECM_Core.Chart.DetectionChart chart, Legacy.ECM_Core.DCC.Feature feature)
        {
            if (feature.NATF == null) { return; }

            foreach (Legacy.ECM_Core.DCC.NATF NATF in feature.NATF)
            {
                if (Legacy.ECM_Core.Catalogue.AttributeCatalogue.Catalogue.TryGetValue(NATF.ATTL, out Legacy.ECM_Core.ENC.Attribute attribute))
                {
                    if (NATF.ATTL == 304)
                    {
                        if (!NATF.ATVL.Contains("9999"))
                        {
                            FileInfo Attribute_FileInfo = new FileInfo(Path.Combine(Path.Combine(DirectoryConfiguration.encDownload, chart.Name, NATF.ATVL)));

                            if (!Attribute_FileInfo.Exists)
                            {
                                feature.faRecord?.attribute.Add(new FeatureAttributeRecord.Attribute() {
                                    name = attribute.Attribute_Name,
                                    acronym = attribute.Acronym,
                                    value = NATF.ATVL,
                                });

                                feature.Set_AttributeInformation(NATF.ATTL, NATF.ATVL);
                            }
                            else
                            {
                                if (Attribute_FileInfo.Extension.ToUpper() == ".TXT")
                                {
                                    FeatureAttributeRecord.Attribute attributeRecord = new FeatureAttributeRecord.Attribute() {
                                        name = attribute.Attribute_Name,
                                        acronym = attribute.Acronym,
                                    };

                                    using (StreamReader Attribute_Reader = new StreamReader(Attribute_FileInfo.OpenRead()))
                                    {
                                        attributeRecord.value = Attribute_Reader.ReadToEnd();
                                    }

                                    feature.faRecord?.attribute.Add(attributeRecord);

                                    feature.Set_AttributeInformation(NATF.ATTL, NATF.ATVL);
                                }
                            }
                        }
                    }
                    else
                    {
                        FeatureAttributeRecord.Attribute attributeRecord = new FeatureAttributeRecord.Attribute() {
                            name = attribute.Attribute_Name,
                            acronym = attribute.Acronym,
                            value = NATF.ATVL,
                        };

                        feature.faRecord?.attribute.Add(attributeRecord);

                        feature.Set_AttributeInformation(NATF.ATTL, NATF.ATVL);
                    }
                }
            }
        }

        private void LegacyLinkFeatureInformation(Legacy.ECM_Core.ECM_CORE legacyCore, Legacy.ECM_Core.Chart.DetectionChart chart)
        {
            if (chart.Feature == null) { return; }

            FileInfo faFile = new FileInfo(Path.Combine(DirectoryConfiguration.encAttribute, chart.Name, $"{chart.Name}.atr"));

            if (faFile.Exists) { faFile.Delete(); }

            foreach (Legacy.ECM_Core.DCC.Feature feature in chart.Feature)
            {
                Legacy.ECM_Core.DCC.FeatureLinker linker = new Legacy.ECM_Core.DCC.FeatureLinker();
                linker.FRID = feature.FRID;

                if (Legacy.ECM_Core.Catalogue.ObjectCatalogue.Catalogue.TryGetValue(feature.FRID.OBJL, out Legacy.ECM_Core.ENC.Object encObject))
                {
                    linker.Object_Acronym = encObject.Acronym;
                    linker.Object_Name = encObject.Object_Name;
                }

                switch (feature.FRID.PRIM)
                {
                    case 1: { if (!legacyCore.Chart_Composer.Link_FeaturePoint(chart, feature, linker)) { continue; } } break;
                    case 2: { if (!legacyCore.Chart_Composer.Link_FeatureLine(chart, feature, linker)) { continue; } } break;
                    case 3: { if (!legacyCore.Chart_Composer.Link_FeatureArea(chart, feature, linker)) { continue; } } break;
                }

                legacyCore.Chart_Composer.Link_Lookup(feature, linker);

                if (feature.faRecord != null)
                {
                    LegacyExportFeatureAttribute(legacyCore, chart, feature, linker);
                }

                chart.FeatureLinker_Collection ??= new Dictionary<uint, Legacy.ECM_Core.DCC.FeatureLinker>(); // 이거 꼭 필요할까? feature와 통합해도 별 관계없어 보이는데
                chart.FeatureLinker_Collection.TryAdd(linker.FRID.RCID, linker);
            }
        }

        private void LegacyExportFeatureAttribute(Legacy.ECM_Core.ECM_CORE legacyCore, Legacy.ECM_Core.Chart.DetectionChart chart, Legacy.ECM_Core.DCC.Feature feature, Legacy.ECM_Core.DCC.FeatureLinker linker)
        {
            FileInfo FeatureAttribute_FileInfo = new FileInfo(Path.Combine(DirectoryConfiguration.encAttribute, chart.Name, $"{chart.Name}.atr"));

            if (linker.FRID.OBJL == 999)
            {
                feature.faRecord!.objectName = "UNKNOWN";
                feature.faRecord!.objectAcronym = "UNKNOWN";
            }
            else
            {
                feature.faRecord!.objectName = linker.Object_Name;
                feature.faRecord!.objectAcronym = linker.Object_Acronym;
            }

            feature.faRecord.position = new FeatureAttributeRecord.Position(legacyCore.Chart_Composer.Convert_LatitudeText(linker.Pivot.Y), legacyCore.Chart_Composer.Convert_LongitudeText(linker.Pivot.X));

            switch (linker.FRID.PRIM)
            {
                case 1: { feature.faRecord.primitive = "Point"; } break;
                case 2: { feature.faRecord.primitive = "Line"; } break;
                case 3: { feature.faRecord.primitive = "Area"; } break;
            }

            CAC.ComposeFeatureAttribute(FeatureAttribute_FileInfo.FullName, feature.faRecord);
        }

        public void LegacyConvertEncChart(Legacy.ECM_Core.ECM_CORE legacyCore, Legacy.ECM_Core.Chart.DetectionChart chart, bool indexchart)
        {
            Legacy.ECM_Core.Chart.SencChart Senc_Chart = new Legacy.ECM_Core.Chart.SencChart()
            {
                Name = chart.Name,
            };

            (double North, double South, double East, double West) Coverage = (
                North: -90.0 * 10000000.0,
                South: 90.0 * 10000000.0,
                East: -180.0 * 10000000.0,
                West: 180.0 * 10000000.0
            );

            if ((chart.Feature != null) && (chart.FeatureLinker_Collection != null))
            {
                for (int i = 0; i < chart.Feature.Count; i++)
                {
                    Legacy.ECM_Core.DCC.Feature Feature = chart.Feature[i];
                    Legacy.ECM_Core.DCC.FeatureLinker Linker = chart.FeatureLinker_Collection[Feature.FRID.RCID];

                    if (Linker.FRID.OBJL == 302)
                    {
                        Coverage = legacyCore.Chart_Composer.Compose_Coverage(Senc_Chart, Feature, Linker, Coverage);
                    }
                    else
                    {
                        if (!indexchart)
                        {
                            legacyCore.Chart_Composer.Compose_Layer(Senc_Chart, Feature, Linker);
                        }
                        else
                        {
                            legacyCore.Chart_Composer.Compose_IndexLayer(Senc_Chart, Feature, Linker);
                        }
                    }
                }
            }

            double Width = Math.Abs(Coverage.East - Coverage.West) / 2.0;
            double Height = Math.Abs(Coverage.North - Coverage.South) / 2.0;

            (int X, int Y) Pivot = (
                X: (int)(Coverage.West + Width),
                Y: (int)(Coverage.South + Height)
            );

            chart.Boundary = Coverage;

            LegacySerializeSenc(legacyCore, Senc_Chart);
            LegacySerializeSearch(legacyCore, Senc_Chart);
            LegacySerializeUpdate(chart, indexchart);

            Legacy.ECM_Core.Catalogue.ChartCatalogue.Set_Catalogue(chart, Pivot);
            ChartCatalogue.Add(new ChartRecord(chart.Name) {
                usage = chart.DSID.INTU,
                scale = (int)chart.DSPM.CSCL,
                COMF = (int)chart.DSPM.COMF,
                baseVersion = new ChartRecord.BaseVersion(chart.Base.EDTN, chart.Base.UPDN),
                updateVersion = chart.Update,
                issueDate = chart.DSID.ISDT,
                updateDate = chart.Update_Date,
                standardVersion = chart.DSID.STED,
                agency = chart.DSID.AGEN,
                centerPosition = new ChartRecord.Position(Pivot.X, Pivot.Y),
                HDAT = chart.DSPM.HDAT,
                VDAT = chart.DSPM.VDAT,
                SDAT = chart.DSPM.SDAT,
                DUNI = chart.DSPM.DUNI,
                HUNI = chart.DSPM.HUNI,
                PUNI = chart.DSPM.PUNI,
                boundary = new ChartRecord.Boundary(
                    (float)chart.Boundary.North,
                    (float)chart.Boundary.South,
                    (float)chart.Boundary.East,
                    (float)chart.Boundary.West
                ),
                referenceDate = chart.serialEnc?.issueDate?.ToString("yyyyMMdd"),
                lifecycle = ChartRecord.Lifecycle.UpToDate,
            });

            this.logger.Info($"INSTALL|{chart.Name}|AUTO|EDTN={chart.Base.EDTN},UPDN={chart.Update},STED={chart.DSID.STED},AGEN={chart.DSID.AGEN},ISDT={chart.DSID.ISDT},UPDT={chart.Update_Date},RFDT={chart.serialEnc?.issueDate?.ToString("yyyyMMdd")}");
        }

        public void LegacyConvertEncChart1(Legacy.ECM_Core.ECM_CORE legacyCore, Legacy.ECM_Core.Chart.DetectionChart chart)
        {
            Legacy.ECM_Core.Chart.SencChart Senc_Chart = new Legacy.ECM_Core.Chart.SencChart()
            {
                Name = chart.Name,
            };

            (double North, double South, double East, double West) Coverage = (
                North: -90.0 * 10000000.0,
                South: 90.0 * 10000000.0,
                East: -180.0 * 10000000.0,
                West: 180.0 * 10000000.0
            );

            if ((chart.Feature != null) && (chart.FeatureLinker_Collection != null))
            {
                for (int i = 0; i < chart.Feature.Count; i++)
                {
                    Legacy.ECM_Core.DCC.Feature Feature = chart.Feature[i];
                    Legacy.ECM_Core.DCC.FeatureLinker Linker = chart.FeatureLinker_Collection[Feature.FRID.RCID];

                    if (Linker.FRID.OBJL == 302)
                    {
                        Coverage = legacyCore.Chart_Composer.Compose_Coverage(Senc_Chart, Feature, Linker, Coverage);
                    }
                    else
                    {
                        legacyCore.Chart_Composer.Compose_Layer(Senc_Chart, Feature, Linker);
                    }
                }
            }

            double Width = Math.Abs(Coverage.East - Coverage.West) / 2.0;
            double Height = Math.Abs(Coverage.North - Coverage.South) / 2.0;

            (int X, int Y) Pivot = (
                X: (int)(Coverage.West + Width),
                Y: (int)(Coverage.South + Height)
            );

            chart.Boundary = Coverage;

            LegacySerializeSenc(legacyCore, Senc_Chart);

            Legacy.ECM_Core.Catalogue.Chart1Catalogue.Set_Catalogue(chart, Pivot);
        }

        public void LegacySerializeSenc(Legacy.ECM_Core.ECM_CORE legacyCore, Legacy.ECM_Core.Chart.SencChart chart)
        {
            DirectoryInfo SENC_DirectoryInfo = new DirectoryInfo(DirectoryConfiguration.encSenc);
            FileInfo SENC_FileInfo = new FileInfo(Path.Combine(SENC_DirectoryInfo.FullName, $"{chart.Name}.enc"));

            if (!SENC_DirectoryInfo.Exists) { SENC_DirectoryInfo.Create(); }
            if (SENC_FileInfo.Exists) { SENC_FileInfo.Delete(); }

            using (FileStream SENC_Stream = new FileStream(SENC_FileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
            using (BinaryWriter SENC_Writer = new BinaryWriter(SENC_Stream))
            {
                int[] digits = DateTime.Now.ToString("yyyyMMdd").Select(c => c - '0').ToArray();

                foreach (int digit in digits)
                {
                    SENC_Writer.Write(digit);
                }

                switch (chart.Name)
                {
                    case string _ when chart.Name.Contains("KRINDEX1"):
                    case string _ when chart.Name.Contains("KRINDEX2"):
                    case string _ when chart.Name.Contains("KRINDEX3"):
                    case string _ when chart.Name.Contains("KRINDEX4"):
                    case string _ when chart.Name.Contains("KRINDEX5"):
                    case string _ when chart.Name.Contains("KRINDEX6"):
                        {
                            legacyCore.Chart_Composer.Serialize_IndexObjectSize(SENC_Writer, chart);

                            legacyCore.Chart_Composer.Serialize_LNDARE(SENC_Writer, chart);
                        }
                        break;
                    default:
                        {
                            legacyCore.Chart_Composer.Serialize_ObjectSize(SENC_Writer, chart);

                            legacyCore.Chart_Composer.Serialize_DEPARE(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_LNDARE(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_DRGARE(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_UNSARE(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_DEPCNT(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_OBSTRN(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_WRECKS(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_LIGHTS(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_SOUNDG(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_SLCONS(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_Meta(SENC_Writer, chart);
                            legacyCore.Chart_Composer.Serialize_OBJECT(SENC_Writer, chart);

                            SENC_Writer.Write(chart.Not_UpToDate);
                        }
                        break;
                }
            }
        }

        public void LegacySerializeSearch(Legacy.ECM_Core.ECM_CORE legacyCore, Legacy.ECM_Core.Chart.SencChart chart, bool coverage = false)
        {
            DirectoryInfo Detection_DirectoryInfo = new DirectoryInfo(DirectoryConfiguration.encDetect);
            DirectoryInfo Coverage_DirectoryInfo = new DirectoryInfo(DirectoryConfiguration.encCoverage);
            FileInfo Detection_FileInfo = new FileInfo(Path.Combine(Detection_DirectoryInfo.FullName, $"{chart.Name}.det"));
            FileInfo Coverage_FileInfo = new FileInfo(Path.Combine(Coverage_DirectoryInfo.FullName, $"{chart.Name}.age"));

            if (!Detection_DirectoryInfo.Exists) { Detection_DirectoryInfo.Create(); }
            if (!Coverage_DirectoryInfo.Exists) { Coverage_DirectoryInfo.Create(); }
            if (Detection_FileInfo.Exists) { Detection_FileInfo.Delete(); }
            if (Coverage_FileInfo.Exists) { Coverage_FileInfo.Delete(); }

            if (!coverage)
            {
                using (FileStream Detection_Stream = new FileStream(Detection_FileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
                using (BinaryWriter Detection_Writer = new BinaryWriter(Detection_Stream))
                {
                    legacyCore.Chart_Composer.Serialize_DetectionSize(Detection_Writer, chart);
                    legacyCore.Chart_Composer.Serialize_Detection(Detection_Writer, chart);
                }
            }

            using (FileStream Coverage_Stream = new FileStream(Coverage_FileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
            using (BinaryWriter Coverage_Writer = new BinaryWriter(Coverage_Stream))
            {
                legacyCore.Chart_Composer.Serialize_CoverageSize(Coverage_Writer, chart);
                legacyCore.Chart_Composer.Serialize_Coverage(Coverage_Writer, chart);
            }
        }

        public void LegacySerializeUpdate(Legacy.ECM_Core.Chart.DetectionChart chart, bool indexchart)
        {
            DirectoryInfo Update_DirectoryInfo = new DirectoryInfo(DirectoryConfiguration.encUpdate);
            FileInfo Update_FileInfo = new FileInfo(Path.Combine(Update_DirectoryInfo.FullName, $"{chart.Name}.new"));

            if (!Update_DirectoryInfo.Exists) { Update_DirectoryInfo.Create(); }
            if (Update_FileInfo.Exists) { Update_FileInfo.Delete(); }

            if (!indexchart)
            {
                int removed = chart.Update_Record?.RemoveAll(record => record.FRID.RCID == 0u) ?? 0;

                if (chart.Update_Record?.Count > 0)
                {
                    using (FileStream Update_Stream = new FileStream(Update_FileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
                    using (BinaryWriter Update_Writer = new BinaryWriter(Update_Stream))
                    {
                        Update_Writer.Write(chart.Update_Record.Count);

                        foreach (Legacy.ECM_Core.DCC.UpdateRecord Update_Record in chart.Update_Record)
                        {
                            Update_Writer.Write(Update_Record.FRID.RCID);
                            Update_Writer.Write(Update_Record.FRID.PRIM);
                            Update_Writer.Write(Update_Record.VRID.RUIN);

                            if (Update_Record.SG2D?.Count > 0)
                            {
                                Update_Writer.Write(Update_Record.SG2D.Count);

                                foreach (Legacy.ECM_Core.DCC.SG2D SG2D in Update_Record.SG2D)
                                {
                                    Update_Writer.Write(SG2D.XCOO);
                                    Update_Writer.Write(SG2D.YCOO);
                                }
                            }
                            else
                            {
                                Update_Writer.Write(0);
                            }
                        }
                    }
                }
            }
        }



        public class ChartAttributeComposer
        {




            public void ComposeFeatureAttribute(string filePath, FeatureAttributeRecord faRecord, bool append = true)
            {
                FileInfo faFile = new FileInfo(filePath);

                if (faFile.Directory?.Exists == false) { faFile.Directory.Create(); }

                using (StreamWriter writer = new StreamWriter(filePath, append))
                {
                    this.ComposeFeatureAttribute(writer, faRecord);
                }
            }

            public void ComposeFeatureAttribute(Stream fileStream, FeatureAttributeRecord faRecord)
            {
                fileStream.Seek(0, SeekOrigin.End);

                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    this.ComposeFeatureAttribute(writer, faRecord);
                }
            }

            private void ComposeFeatureAttribute(StreamWriter writer, FeatureAttributeRecord faRecord)
            {
                writer.WriteLine(JsonConvert.SerializeObject(faRecord));
                writer.Flush();
            }
        }

        public class ChartSencComposer
        {

        }
    }
}