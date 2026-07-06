using System.IO;
using System.Windows.Shapes;

namespace JHLib.S57.Chart
{
    public class VCT
    {
        public string type = null;
        public string colorType = null;
        public int value1 = 0;
        public int value2 = 0;
        public int value3 = 0;
    }

    public class DaiSymbol
    {
        public string synm = null;
        public string sydf = null;          
        public int sycl = -10000;  
        public int syrw = -10000; 
        public int syhl = 0;         
        public int syvl = 0;         
        public int sbxc = 0;      
        public int sbxr = 0;      
        public string sxpo = null;

        public Dictionary<string, string> dicScrf = new();
        public List<VCT> listSymbolVCT = new();
    }

    public class DaiLine
    {
        public string linm = null;  
        public int licl = -10000;    
        public int lirw = -10000;   
        public int lihl = 0;           
        public int livl = 0;           
        public int lbxc= -1;         
        public int lbxr = -1;         
        public string lxpo = null;  

        public Dictionary<string, string> dicLcrf = new();
        public List<VCT> listLineVCT = new();
    }

    public class DaiPattern
    {
        public string panm = null;   
        public string patp = null;    
        public string pasp = null;    
        public int pami = -1;          
        public int pama;      
        public int pacl;        
        public int parw;       
        public int pahl;        
        public int pavl;        
        public int pbxc;       
        public int pbxr;       
        public string pxpo = null;    

        public Dictionary<string, string> dicPcrf = new();
        public List<VCT> listPatternVCT = new();
    }

    public static class DaiPL4
    {
        // Symbol 정보 저장 어레이
        public static Dictionary<int, DaiSymbol> dicSymbol = new();
        public static Dictionary<string, int> dicSymbolACNM = new();
        // Line 정보 저장 어레이
        public static Dictionary<int, DaiLine> dicLine = new();
        public static Dictionary<string, int> dicLineACNM = new();
        // Pattern 정보 저장 어레이
        public static Dictionary<int, DaiPattern> dicPattern = new();
        public static Dictionary<string, int> dicPatternACNM = new();

        public static Dictionary<string, List<int>> dicSymbolLine = new();

        public static void Init(string exePath)
        {
            ParseSymbolLine(exePath);
            ParseDai(exePath);
        }

        private static void ParseSymbolLine(string exePath)
        {
            try
            {
                var filePath = System.IO.Path.Combine(exePath, S57PathName.s57Dir, S57PathName.symbolDir, "line.pos");
                if (File.Exists(filePath) == false) return;

                dicSymbolLine.Clear();

                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if(string.IsNullOrEmpty(line)) continue;

                    string[] temps = line.Split(',');
                    if (temps.Length <= 0) continue;

                    int index = 0;
                    var name = temps[index]; 
                    index += 3;
                    if(int.TryParse(temps[index], out int count) == true && count > 0)
                    {
                        List<int> listInfo = new();
                        for (int i = 0; i < count; i++)
                        {
                            index++;
                            if (int.TryParse(temps[index], out int value) == true) listInfo.Add(value);
                        }

                        dicSymbolLine.TryAdd(name, listInfo);
                    }
                }
            }
            catch(Exception ex)
            {
                var msg = ex.Message;
            }
        }

        private static void ParseDai(string exePath)
        {
            try
            {
                var filePath = System.IO.Path.Combine(exePath, S57PathName.s57Dir, S57PathName.symbolDir, "daiPL4.0.3.dai");
                if (File.Exists(filePath) == false) return;

                dicSymbol.Clear();
                dicSymbolACNM.Clear();
                dicLine.Clear();
                dicLineACNM.Clear();
                dicPattern.Clear();
                dicPatternACNM.Clear();

                var stream = File.OpenText(filePath);
                while (stream.EndOfStream == false)
                {
                    var line = stream.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (IsName(line, "LBID") == true) continue;
                    if (IsName(line, "CCIE") == true) continue;

                    if (IsName(line, "SYMB") == true)
                    {
                        ParseSymbol(stream);
                        continue;
                    }

                    if (IsName(line, "LNST") == true)
                    {
                        ParseLine(stream);
                        continue;
                    }

                    if (IsName(line, "PATT") == true)
                    {
                        ParsePattern(stream);
                        continue;
                    }
                }

                stream.Close();
            }
            catch(Exception ex)
            {
                var msg = ex.Message;
            }
        }

        private static void ParseSymbol(StreamReader stream)
        {
            var sym = new DaiSymbol();
            while (stream.EndOfStream == false)
            {
                var line = stream.ReadLine();
                if (string.IsNullOrEmpty(line) == true) continue;

                if (ParseSymd(line, sym) == true) continue;
                else if (ParseSxpo(line, sym) == true) continue;
                else if (ParseScrf(line, sym) == true) continue;
                else if (IsName(line, "SBTM") == true) continue;
                else if (ParseSvct(line, sym) == true) continue;
                else
                {
                    string name = "";
                    line = ParseField(line, ref name, 4, 0x1F);
                    if(name.Contains("****") == true)
                    {
                        dicSymbolACNM.TryAdd(sym.synm, dicSymbol.Count);
                        dicSymbol.TryAdd(dicSymbol.Count, sym);
                        return;
                    }
                }
            }
        }

        private static bool ParseSymd(string data, DaiSymbol sym)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "SYMD")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                data = ParseField(data, ref name, 8, 0x1F);
                sym.synm = name;
                data = ParseField(data, ref name, 1, 0x1F);
                sym.sydf = name;
                data = ParseField(data, ref name, 5, 0x1F);
                sym.sycl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                sym.syrw = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                sym.syhl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                sym.syvl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                sym.sbxc = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                sym.sbxr = int.Parse(name);
                return true;
            }

            return false;
        }

        private static bool ParseSxpo(string data, DaiSymbol sym)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "SXPO")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                data = ParseField(data, ref name, 128, 0x1F);
                sym.sxpo = name;
                return true;
            }
            return false;
        }

        private static bool ParseScrf(string data, DaiSymbol sym)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "SCRF")
            {
                data = ParseField(data, ref name, 5, 0x1F);

                while(data.Length > 0)
                {
                    string type = "";
                    string colorname = "";
                    data = ParseField(data, ref type, 1, 0x1F);
                    data = ParseField(data, ref colorname, 5, 0x1F);
                    sym.dicScrf.TryAdd(type, colorname);
                }

                return true;
            }
            return false;
        }

        private static bool ParseSvct(string data, DaiSymbol sym)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if(name == "SVCT")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                int count = int.Parse(name);

                while(data.Length >0)
                {
                    data = ParseField(data, ref name, 2, 0x1F);
                    if(name =="SP")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.colorType = name;
                        sym.listSymbolVCT.Add(vct);
                    }
                    else if(name == "ST" || name == "SW")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        sym.listSymbolVCT.Add(vct);
                    }
                    else if (name == "PU")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ',');
                        vct.value1 = int.Parse(name);
                        data = ParseField(data, ref name, count, ';');
                        vct.value2 = int.Parse(name);
                        sym.listSymbolVCT.Add(vct);
                    }
                    else if (name == "PD")
                    {
                        string temp = "";
                        data = ParseField(data, ref temp, count, ';');
                        while(temp.IndexOf(',') != -1)
                        {
                            var vct = new VCT();
                            vct.type = "PD";
                            temp = ParseField(temp, ref name, 10, ',');
                            vct.value1 = int.Parse(name);
                            temp = ParseField(temp, ref name, 10, ',');
                            vct.value2 = int.Parse(name);
                            sym.listSymbolVCT.Add(vct);
                        }
                    }
                    else if (name == "CI")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        sym.listSymbolVCT.Add(vct);
                    }
                    else if (name == "PM")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        sym.listSymbolVCT.Add(vct);
                    }
                    else if (name == "EP" || name == "FP")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        sym.listSymbolVCT.Add(vct);
                    }
                    else if (name == "SC")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        sym.listSymbolVCT.Add(vct);
                    }
                }

                return true;
            }

            return false;
        }

        private static void ParseLine(StreamReader stream)
        {
            var line = new DaiLine();

            while(stream.EndOfStream == false)
            {
                var data = stream.ReadLine();
                if (string.IsNullOrEmpty(data) == true) continue;

                if (ParseLind(data, line) == true) continue;
                else if (ParseLxpo(data, line) == true) continue;
                else if (ParseLcrf(data, line) == true) continue;
                else if (ParseLvct(data, line) == true) continue;
                else
                {
                    string name = "";
                    data = ParseField(data, ref name, 4, 0x1F);
                    if (name.Contains("****") == true)
                    {
                        dicLineACNM.TryAdd(line.linm, dicLine.Count);
                        dicLine.TryAdd(dicLine.Count, line);
                        return;
                    }
                }
            }
        }

        private static bool ParseLind(string data, DaiLine line)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if(name == "LIND")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                data = ParseField(data, ref name, 8, 0x1F);
                line.linm = name;
                data = ParseField(data, ref name, 5, 0x1F);
                line.licl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                line.lirw = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                line.lihl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                line.livl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                line.lbxc = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                line.lbxr = int.Parse(name);
                return true;
            }

            return false;
        }

        private static bool ParseLxpo(string data, DaiLine line)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "LXPO")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                data = ParseField(data, ref name, 128, 0x1F);
                line.lxpo = name;
                return true;
            }

            return false;
        }

        private static bool ParseLcrf(string data, DaiLine line)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "LCRF")
            {
                data = ParseField(data, ref name, 5, 0x1F);

                while(data.Length > 0)
                {
                    string type = "";
                    string colorName = "";
                    data = ParseField(data, ref type, 1, 0x1F);
                    data = ParseField(data, ref colorName, 5, 0x1F);
                    line.dicLcrf.TryAdd(type, colorName);
                }

                return true;
            }

            return false;
        }

        private static bool ParseLvct(string data, DaiLine line)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "LVCT")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                var count = int.Parse(name);

                while (data.Length > 0)
                {
                    data = ParseField(data, ref name, 2, 0x1F);

                    if(name == "SS")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        line.listLineVCT.Add(vct);
                    }
                    else if (name == "SP")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.colorType = name;
                        line.listLineVCT.Add(vct);
                    }
                    else if (name == "ST" || name == "SW")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        line.listLineVCT.Add(vct);
                    }
                    else if (name == "PU")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ',');
                        vct.value1 = int.Parse(name);
                        data = ParseField(data, ref name, count, ';');
                        vct.value2 = int.Parse(name);
                        line.listLineVCT.Add(vct);
                    }
                    else if (name == "PD")
                    {
                        string temp = "";
                        data = ParseField(data, ref temp, count, ';');
                        while(temp.IndexOf(',') != -1)
                        {
                            var vct = new VCT();
                            vct.type = "PD";
                            temp = ParseField(temp, ref name, 10, ',');
                            vct.value1 = int.Parse(name);
                            temp = ParseField(temp, ref name, 10, ',');
                            vct.value2 = int.Parse(name);
                            line.listLineVCT.Add(vct);
                        }
                    }
                    else if (name == "CI" || name == "PM")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        line.listLineVCT.Add(vct);
                    }
                    else if (name == "EP" || name == "FP")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        line.listLineVCT.Add(vct);
                    }
                    else if (name == "SC")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ',');
                        vct.colorType = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        line.listLineVCT.Add(vct);
                    }
                }

                return true;
            }

            return false;
        }

        private static void ParsePattern(StreamReader stream)
        {
            var pattern = new DaiPattern();

            while (stream.EndOfStream == false)
            {
                var data = stream.ReadLine();
                if (string.IsNullOrEmpty(data) == true) continue;

                if (ParsePatd(data, pattern) == true) continue;
                else if (ParsePxpo(data, pattern) == true) continue;
                else if (ParsePcrf(data, pattern) == true) continue;
                else if (ParsePvct(data, pattern) == true) continue;
                else
                {
                    string name = "";
                    data = ParseField(data, ref name, 4, 0x1F);
                    if (name.Contains("****") == true)
                    {
                        dicPatternACNM.TryAdd(pattern.panm, dicPattern.Count);
                        dicPattern.TryAdd(dicPattern.Count, pattern);
                        return;
                    }
                }
            }
        }

        private static bool ParsePatd(string data, DaiPattern pattern)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "PATD")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                data = ParseField(data, ref name, 8, 0x1F);
                pattern.panm = name;
                data = ParseField(data, ref name, 1, 0x1F);
                data = ParseField(data, ref name, 3, 0x1F);
                pattern.patp = name;
                data = ParseField(data, ref name, 3, 0x1F);
                pattern.pasp = name;
                data = ParseField(data, ref name, 5, 0x1F);
                pattern.pami = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                pattern.pama = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                pattern.pacl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                pattern.parw = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                pattern.pahl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                pattern.pavl = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                pattern.pbxc = int.Parse(name);
                data = ParseField(data, ref name, 5, 0x1F);
                pattern.pbxr = int.Parse(name);
                return true;
            }

            return false;
        }

        private static bool ParsePxpo(string data, DaiPattern pattern)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "PXPO")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                data = ParseField(data, ref name, 128, 0x1F);
                pattern.pxpo = name;
                return true;
            }

            return false;
        }

        private static bool ParsePcrf(string data, DaiPattern pattern)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "PCRF")
            {
                data = ParseField(data, ref name, 5, 0x1F);

                while (data.Length > 0)
                {
                    string type = "";
                    string colorName = "";
                    data = ParseField(data, ref type, 1, 0x1F);
                    data = ParseField(data, ref colorName, 5, 0x1F);
                    pattern.dicPcrf.TryAdd(type, colorName);
                }

                return true;
            }

            return false;
        }

        private static bool ParsePvct(string data, DaiPattern pattern)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == "PVCT")
            {
                data = ParseField(data, ref name, 5, 0x1F);
                var count = int.Parse(name);

                while (data.Length > 0)
                {
                    data = ParseField(data, ref name, 2, 0x1F);

                    if (name == "SP")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.colorType = name;
                        pattern.listPatternVCT.Add(vct);
                    }
                    else if (name == "ST" || name == "SW")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        pattern.listPatternVCT.Add(vct);
                    }
                    else if (name == "PU")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ',');
                        vct.value1 = int.Parse(name);
                        data = ParseField(data, ref name, count, ';');
                        vct.value2 = int.Parse(name);
                        pattern.listPatternVCT.Add(vct);
                    }
                    else if (name == "PD")
                    {
                        string temp = "";
                        data = ParseField(data, ref temp, count, ';');
                        while (temp.IndexOf(',') != -1)
                        {
                            var vct = new VCT();
                            vct.type = "PD";
                            temp = ParseField(temp, ref name, 10, ',');
                            vct.value1 = int.Parse(name);
                            temp = ParseField(temp, ref name, 10, ',');
                            vct.value2 = int.Parse(name);
                            pattern.listPatternVCT.Add(vct);
                        }
                    }
                    else if (name == "CI" || name == "PM")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        pattern.listPatternVCT.Add(vct);
                    }
                    else if (name == "EP" || name == "FP")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ';');
                        pattern.listPatternVCT.Add(vct);
                    }
                    else if (name == "SC")
                    {
                        var vct = new VCT();
                        vct.type = name;
                        data = ParseField(data, ref name, count, ',');
                        vct.colorType = name;
                        data = ParseField(data, ref name, count, ';');
                        vct.value1 = int.Parse(name);
                        pattern.listPatternVCT.Add(vct);
                    }
                }

                return true;
            }

            return false;
        }

        private static bool IsName(string data, string checkName)
        {
            string name = "";
            data = ParseField(data, ref name, 4, 0x1F);
            if (name == checkName) return true;

            return false;
        }

        private static string ParseField(string cur, ref string name, int count, int delimiter)
        {
            name = "";

            int index = 0;
            for (int i = 0; i < count; i++)
            {
                if(i < cur.Length)
                {
                    if (cur[i] == delimiter)
                    {
                        index++;
                        break;
                    }

                    if (cur[i] == '\0') break;

                    name += cur[i];
                    index++;
                }
            }

            cur = cur.Remove(0, index);

            return cur;
        }

        public static int GetSymbolIndex(string acnm)
        {
            if (dicSymbolACNM.TryGetValue(acnm, out var index) == true)
            {
                return index;
            }

            return -1;
        }

        public static int GetLineIndex(string acnm)
        {
            if (dicLineACNM.TryGetValue(acnm, out var index) == true)
            {
                return index;
            }

            return -1;
        }

        public static int GetPatternIndex(string acnm)
        {
            if (dicPatternACNM.TryGetValue(acnm, out var index) == true)
            {
                return index;
            }

            return -1;
        }
    }
}
