using System.IO;

namespace JHLib.S57.Chart
{
    public class AttributeInfo
    {
        public string Info = "";
        public string BmpPath = "";
    }

    public class ChartAttribute
    {
        public ChartAttribute(string exePath, string chartName) 
        {
            var filePath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.encDir, S57PathName.attributeDir, chartName, chartName + S57PathName.attributeExt);
            ParseAttribute(filePath);
        }

        public Dictionary<int, AttributeInfo> DicAttribute = new();

        private void ParseAttribute(string filePath)
        {
            if (File.Exists(filePath) == false) return;

            try
            {
                DicAttribute.Clear();

                bool isData = true;
                int rcid = 0;
                string resultData = "";
                var att = new AttributeInfo();

                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    if (isData)
                    {
                        var data = line.Replace("-", "");
                        int.TryParse(data, out rcid);
                        att.Info = "";
                        att.BmpPath = "";
                        resultData = "";
                        isData = false;
                    }
                    else
                    {
                        if (line == "=")
                        {
                            att.Info = resultData;
                            DicAttribute.TryAdd(rcid, att);
                            isData = true;
                        }
                        else
                        {
                            resultData += line;
                            resultData += "\r\n";

                            if (line.Contains("[Pictorial representation(PICREP)]") == true)
                            {
                                var cnt = line.IndexOf(':');
                                if (cnt > 0 && cnt < line.Length)
                                {
                                    att.BmpPath = line.Substring(cnt + 1);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
        }

        public bool GetAttribute(int rcid, out AttributeInfo att)
        {
            return DicAttribute.TryGetValue(rcid, out att);
        }
    }
}
