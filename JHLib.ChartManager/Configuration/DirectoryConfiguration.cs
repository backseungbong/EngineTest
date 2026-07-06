using System.IO;

namespace JHLib.ChartManager.Configuration
{
    public static class DirectoryConfiguration
    {
        public static string root = "S57";

        private static string _catalogue = "Catalogue";
        public static string catalogue
        {
            get => Path.Combine(DirectoryConfiguration.root, DirectoryConfiguration._catalogue);
            set { DirectoryConfiguration._catalogue = value; }
        }

        private static string _enc = "ENC";
        public static string enc
        {
            get => Path.Combine(DirectoryConfiguration.root, DirectoryConfiguration._enc);
            set { DirectoryConfiguration._enc = value; }
        }

        private static string _encDownload = "DOWNLOAD";
        public static string encDownload
        {
            get => Path.Combine(DirectoryConfiguration.root, DirectoryConfiguration._enc, DirectoryConfiguration._encDownload);
            set { DirectoryConfiguration._encDownload = value; }
        }

        private static string _encAttribute = "ATTRIBUTE";
        public static string encAttribute
        {
            get => Path.Combine(DirectoryConfiguration.root, DirectoryConfiguration._enc, DirectoryConfiguration._encAttribute);
            set { DirectoryConfiguration._encAttribute = value; }
        }

        private static string _encCoverage = "COVERAGE";
        public static string encCoverage
        {
            get => $"{DirectoryConfiguration.enc}\\{DirectoryConfiguration._encCoverage}";
            set { DirectoryConfiguration._encCoverage = value; }
        }

        private static string _encSenc = "SENC";
        public static string encSenc
        {
            get => $"{DirectoryConfiguration.enc}\\{DirectoryConfiguration._encSenc}";
            set { DirectoryConfiguration._encSenc = value; }
        }

        private static string _encDetect = "DETECTION";
        public static string encDetect
        {
            get => $"{DirectoryConfiguration.enc}\\{DirectoryConfiguration._encDetect}";
            set { DirectoryConfiguration._encDetect = value; }
        }

        private static string _encUpdate = "UPDATE";
        public static string encUpdate
        {
            get => $"{DirectoryConfiguration.enc}\\{DirectoryConfiguration._encUpdate}";
            set { DirectoryConfiguration._encUpdate = value; }
        }
    }
}