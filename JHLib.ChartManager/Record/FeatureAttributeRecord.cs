using JHLib.ChartManager.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace JHLib.ChartManager.Record
{
    public class FeatureAttributeRecord
    {
        public FeatureAttributeRecord.FRID frid { get; set; }

        public string? objectName { get; set; } = null;
        public string? objectAcronym { get; set; } = null;

        public FeatureAttributeRecord.Position? position { get; set; } = null;
        public string? primitive { get; set; } = null;

        public List<FeatureAttributeRecord.Attribute> attribute { get; set; } = new List<FeatureAttributeRecord.Attribute>();



        public FeatureAttributeRecord(uint rcid, ushort objl)
        {
            this.frid = new FeatureAttributeRecord.FRID(rcid, objl);
        }

        [JsonConstructor]
        public FeatureAttributeRecord(FeatureAttributeRecord.FRID frid)
        {
            this.frid = frid;
        }

        public bool IsPicture = false;
        public string TextValue = "";

        public string ToReportText(string chartName)
        {
            StringBuilder textBuilder = new StringBuilder();

            if (frid.objl == 999)
            {
                textBuilder.AppendLine(" >> UNKNOWN");
            }
            else
            {
                textBuilder.AppendLine($" >> {this.objectName} ({this.objectAcronym})");
            }

            textBuilder.AppendLine($" Position : {this.position?.latitude} , {this.position?.longitude}");
            textBuilder.AppendLine($" Primitive : {this.primitive}");

            var isTidal = this.frid.objl >= 136 && this.frid.objl <= 138;
            if(isTidal == false)
            {
                var attr = GetFirstPICREP();
                if(attr != null)
                {
                    var fileInfo = attr.GetPictorialRepresentation(chartName);
                    if(fileInfo != null)
                    {
                        IsPicture = true;
                        TextValue = fileInfo.FullName;
                    }
                }
            }

            foreach (FeatureAttributeRecord.Attribute attributeItem in this.attribute)
            {
                if (attributeItem.acronym == "TXTDSC" || (isTidal && (attributeItem.acronym == "TS_TSP" || attributeItem.acronym == "T_VAHC" || attributeItem.acronym == "T_MTOD")))
                {
                    textBuilder.AppendLine($" [{attributeItem.name}({attributeItem.acronym})]");
                    if(IsPicture == false)
                    {
                        TextValue = attributeItem.value;
                    }
                }
                else
                {
                    textBuilder.AppendLine($" [{attributeItem.name}({attributeItem.acronym})] : {attributeItem.value}");
                }
            }

            return textBuilder.ToString();
        }

        public List<FeatureAttributeRecord.Attribute> GetPICREP()
        {
            return this.attribute.Where(attributeItem => attributeItem.acronym == "PICREP").ToList();
        }

        public List<FeatureAttributeRecord.Attribute> GetTXTDSC()
        {
            return this.attribute.Where(attributeItem => attributeItem.acronym == "TXTDSC").ToList();
        }

        public FeatureAttributeRecord.Attribute? GetFirstPICREP()
        {
            return this.attribute.FirstOrDefault(attributeItem => attributeItem.acronym == "PICREP");
        }

        public FeatureAttributeRecord.Attribute? GetFirstTXTDSC()
        {
            return this.attribute.FirstOrDefault(attributeItem => attributeItem.acronym == "TXTDSC");
        }



        public class FRID
        {
            public uint rcid { get; set; }
            public ushort objl { get; set; }



            public FRID(uint rcid, ushort objl)
            {
                this.rcid = rcid;
                this.objl = objl;
            }
        }

        public class Position
        {
            public string latitude { get; set; }
            public string longitude { get; set; }



            public Position(string latitude, string longitude)
            {
                this.latitude = latitude;
                this.longitude = longitude;
            }
        }

        public class Attribute
        {
            public string? name { get; set; } = null;
            public string? acronym { get; set; } = null;

            public string? value { get; set; } = null;



            public string ToReportText()
            {
                return $" [{this.name ?? "?"} ({this.acronym})] : {this.value}";
            }

            public FileInfo? GetPictorialRepresentation(string chartName)
            {
                if (!string.IsNullOrEmpty(this.value) && (this.acronym == "PICREP"))
                {
                    return new FileInfo(Path.Combine(DirectoryConfiguration.encAttribute, chartName, this.value));
                }
                else
                {
                    return null;
                }
            }
        }
    }
}