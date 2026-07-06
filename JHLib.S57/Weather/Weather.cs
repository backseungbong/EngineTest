using JHLib.S57;
using System.IO;

namespace JHLib.Weather
{
    public class ColorRGB
    {
        public ColorRGB(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    public static class WeatherColor
    {
        static WeatherColor() 
        {
            ListWeatherColors[0] = new();
            ListWeatherColors[1] = new();
            ListWeatherColors[2] = new();
        }

        // 현재 Weather 값을 저장할 변수 (0=Day, 1=Dusk, 2=Night)
        public static byte WeatherIndex = 0;
        // Weather 색상 명에 따른 Index 번호를 저장할 어레이
        public static Dictionary<string, int> DicColorACNM = new();
        // Weather Color 값을 저장할 리스트
        public static List<ColorRGB>[] ListWeatherColors = new List<ColorRGB>[3];

        public static void Init(string exePath)
        {
            var acnmFilePath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.colorDir, "acnm.txt");
            ParseColorACNM(acnmFilePath);

            var dataFilePath = Path.Combine(exePath, S57PathName.s57Dir, S57PathName.colorDir, "24Weathercolor.dat");
            ParseWeatherColors(dataFilePath);
        }

        private static void ParseColorACNM(string filePath)
        {
            if (File.Exists(filePath) == false) return;

            try
            {
                DicColorACNM.Clear();

                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // '#'을 기준으로 데이터 분리 
                    string[] names = line.Split('#');
                    if (names.Length < 2) continue;

                    int index = 0;
                    foreach (var name in names)
                    {
                        if (string.IsNullOrEmpty(name) == false)
                        {
                            DicColorACNM.TryAdd(name, index);
                            index++;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                var msg = ex.Message;
            }
        }

        private static void ParseWeatherColors(string filePath)
        {
            if (File.Exists(filePath) == false) return;

            try
            {
                ListWeatherColors[0].Clear();
                ListWeatherColors[1].Clear();
                ListWeatherColors[2].Clear();

                string[] lines = File.ReadAllLines(filePath);

                int weatherIndex = 0;
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // '#'을 기준으로 데이터 분리 
                    string[] parts = line.Split('#');
                    if (parts.Length < 2) continue;

                    // DAY, DUSK, NIGHT Index 받기 
                    if(int.TryParse(parts[0], out weatherIndex) == true)
                    {
                        // 첫 번째 요소(Label)를 제외한 나머지 RGB 문자열 처리 
                        for (int i = 1; i < parts.Length; i++)
                        {
                            string[] rgb = parts[i].Split(',');
                            if (rgb.Length == 3)
                            {
                                ListWeatherColors[weatherIndex].Add(new ColorRGB(byte.Parse(rgb[0]), byte.Parse(rgb[1]), byte.Parse(rgb[2])));
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                var msg = ex.Message;
            }
        }

        public static ColorRGB GetColor(string colorName, byte weatherIndex)
        {
            if (weatherIndex < 0 || weatherIndex > 2) return null;
            if (ListWeatherColors[weatherIndex].Count <= 0) return null;

            ColorRGB color = null;

            if(DicColorACNM.TryGetValue(colorName, out int acnmIndex) == true)
            {
                if(acnmIndex >=0 && acnmIndex < ListWeatherColors[weatherIndex].Count)
                {
                    color = ListWeatherColors[weatherIndex][acnmIndex];
                }
            }

            return color;
        }

        public static ColorRGB GetColor(string colorName)
        {
            if (WeatherIndex < 0 || WeatherIndex > 2) return null;
            if (ListWeatherColors[WeatherIndex].Count <= 0) return null;

            ColorRGB color = null;

            if (DicColorACNM.TryGetValue(colorName, out int acnmIndex) == true)
            {
                if (acnmIndex >= 0 && acnmIndex < ListWeatherColors[WeatherIndex].Count)
                {
                    color = ListWeatherColors[WeatherIndex][acnmIndex];
                }
            }

            return color;
        }

        public static ColorRGB GetColor(int acnmIndex)
        {
            if (WeatherIndex < 0 || WeatherIndex > 2) return null;
            if (ListWeatherColors[WeatherIndex].Count <= 0) return null;

            ColorRGB color = null;

            if (acnmIndex >= 0 && acnmIndex < ListWeatherColors[WeatherIndex].Count)
            {
                color = ListWeatherColors[WeatherIndex][acnmIndex];
            }

            return color;
        }

        public static int GetNameIndex(string colorName)
        {
            int acnmIndex = -1;

            DicColorACNM.TryGetValue(colorName, out acnmIndex);

            return acnmIndex;
        }
    }
}
