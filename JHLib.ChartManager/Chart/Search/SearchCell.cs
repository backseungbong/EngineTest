using System.IO;

namespace JHLib.ChartManager.Chart.Search
{
    public class SearchCell
    {
        public FileInfo file;

        public int? EDTN = null;
        public int? UPDN = null;
        public string? CRC = null;

        public string? provider = null;
        public bool compression = false;
        public bool encryption = false;

        public (bool signature, bool necessary) validation = (signature: false, necessary: false);



        public SearchCell(string filePath)
        {
            this.file = new FileInfo(filePath);
        }
        
        public SearchCell(FileInfo file)
        {
            this.file = file;
        }
    }
}