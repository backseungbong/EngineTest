using System.Globalization;
using System.IO;

namespace JHLib.ChartManager.ENC
{
    public class SerialEnc
    {
        public string? provider = null;
        public int? week = null;
        public DateTime? issueDate = null;
        public string? type = null;

        public int? total = null;
        public int? current = null;



        public SerialEnc(FileInfo serialFile)
        {
            string[] readLines = File.ReadAllLines(serialFile.FullName);

            if (readLines.Length > 0)
            {
                if (readLines.Length > 1)
                {
                    int debug = 0;
                }

                string read = readLines[0];

                if (!string.IsNullOrEmpty(read))
                {
                    if (read.Length > 29)
                    {
                        this.provider = read[0..2].ToUpper();
                        this.type = read[20..30].ToUpper().Trim();

                        string weekData = read[2..12].ToUpper().Trim();

                        if (int.TryParse(weekData[5..] + weekData[2..4], out int week))
                        {
                            this.week = week;
                        }

                        if (DateTime.TryParseExact(
                            read[12..20],
                            "yyyyMMdd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal,
                            out DateTime issueDate))
                        {
                            this.issueDate = issueDate.ToUniversalTime();
                        }
                    }

                    if (read.Length > 41)
                    {
                        string numbers = read[35..].Trim();

                        if (numbers.Length > 5)
                        {
                            string currentNumber = numbers[1..3];
                            string totalNumber = numbers[4..6];

                            if (int.TryParse(currentNumber, out int current))
                            {
                                this.current = current;
                            }

                            if (int.TryParse(totalNumber, out int total))
                            {
                                this.total = total;
                            }
                        }
                    }
                }
            }
        }
    }
}