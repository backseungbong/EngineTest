using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JHLib.S57.Catalogue
{
    public class Attribute
    {
        public string ACNM = "";
        public int AttributeCode = -1;
        public string ObjectType = "";
        public string AttributeType = "";
        public string AttributeName = "";
        public List<string> Elements = new();
    }

    public static class AttributeCat
    {

        public static Dictionary<int, Attribute> DicAttribute = new();
        public static Dictionary<string, int> DicAttCode = new();

        public static void Init(string exePath)
        {
            var catPath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.catalogueDir, "attribute.cat");
            ParseAttribute(catPath);
        }

        public static void ParseAttribute(string filePath)
        {
            if (File.Exists(filePath) == false) return;

            try
            {
                DicAttribute.Clear();
                DicAttCode.Clear();

                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var info = line.Split("#");
                    var att = new Attribute();
                    if (info.Length > 0) att.ACNM = info[0];
                    if (info.Length > 1) int.TryParse(info[1], out att.AttributeCode);
                    if (info.Length > 2) att.ObjectType = info[2];
                    if (info.Length > 3) att.AttributeType = info[3];
                    if (info.Length > 4) att.AttributeName = info[4];
                    if (info.Length > 5)
                    {
                        if(att.AttributeType == "E" || att.AttributeType == "L")
                        {
                            var elements = info[5].Split("|");
                            foreach(var value in elements)  att.Elements.Add(value);
                        }
                    }

                    DicAttribute.TryAdd(att.AttributeCode, att);
                    DicAttCode.TryAdd(att.ACNM, att.AttributeCode);
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
        }

        public static Attribute GetAttribute(string acnm)
        {
            Attribute? rtn = null;

            if(DicAttCode.TryGetValue(acnm, out var code))
            {
                DicAttribute.TryGetValue(code, out rtn);
            }

            return rtn;
        }

        public static Attribute GetAttribute(int code)
        {
            Attribute? rtn = null;

            DicAttribute.TryGetValue(code, out rtn);

            return rtn;
        }
    }
}
