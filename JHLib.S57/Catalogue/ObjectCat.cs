using Legacy.ECM_Core.Definition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JHLib.S57.Catalogue
{
    public class ObjectInfo
    {
        public string ACNM = "";
        public int Code = 0;
        public string Name = "";
    }

    public static class ObjectCat
    {
        public static Dictionary<int, ObjectInfo> DicObjectCat = new();
        public static Dictionary<string, int> DicAcnmToCode = new();

        public static void Init(string exePath)
        {
            var catPath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.catalogueDir, "object.cat");
            ParseObjectCatalogue(catPath);
        }

        public static void ParseObjectCatalogue(string filePath)
        {
            if (File.Exists(filePath) == false) return;

            try
            {
                DicObjectCat.Clear();
                DicAcnmToCode.Clear();

                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    var catInfos = line.Split('#');
                    if (catInfos.Length < 5) continue;

                    var cat = new ObjectInfo();
                    cat.ACNM = catInfos[0];
                    int.TryParse(catInfos[1], out cat.Code);
                    cat.Name = catInfos[4];

                    DicObjectCat.TryAdd(cat.Code, cat);
                    DicAcnmToCode.TryAdd(cat.ACNM, cat.Code);
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
        }

        public static string GetObjectName(int objectCode)
        {
            return DicObjectCat.TryGetValue(objectCode, out var value) ? value.Name : "";
        }

        public static string GetObjectACNM(int objectCode)
        {
            return DicObjectCat.TryGetValue(objectCode, out var value) ? value.ACNM : "";
        }


        public static short GetObjectID(string acnm)
        {
            return DicAcnmToCode.TryGetValue(acnm, out var code) ? (short)code : (short)0;
        }
    }
}
