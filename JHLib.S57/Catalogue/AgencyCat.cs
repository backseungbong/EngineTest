using System.IO;

namespace JHLib.S57.Catalogue
{
    public class AgencyInfo
    {
        public AgencyInfo(int id, string acnm, string name, string info) 
        {
            this.Id = id;
            this.Acnm = acnm;
            this.Name = name;
            this.Info = info;
        }

        public int Id = 0;
        public string Acnm = null;
        public string Name = null;
        public string Info = null;
    }

    public static class AgencyCat
    {
        public static Dictionary<int, AgencyInfo> DicAgency = new();

        public static void Init(string exePath)
        {
            var catPath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.catalogueDir, "agency.cat");
            ParseAgency(catPath);
        }

        private static void ParseAgency(string filePath)
        {
            if (File.Exists(filePath) == false) return;

            try
            {
                DicAgency.Clear();

                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var info = line.Split('#');
                    if(info.Length < 4) continue;

                    var id = int.Parse(info[0]);
                    DicAgency.TryAdd(id, new AgencyInfo(id, info[1], info[2], info[3]));
                }
            }
            catch(Exception ex)
            {
                var msg = ex.Message;
            }
        }

        public static string GetName(int id)
        {
            return DicAgency.TryGetValue(id, out var value) ? value.Name : string.Empty;
        }
    }
}
