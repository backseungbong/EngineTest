using JHLib.Util.Config;
using System.IO;

namespace JHLib.S57.Chart
{
    public static class ChartSetupManager
    {
        private static string _exePath = "";
        private static string _chartOptionPath = "";
        private static string _chartCategoryPath = "";
        private static string _chartTextGroupPath = "";
        private static string _chartSafetyPath = "";
        private static string _chartQueryOptionPath = "";

        public static void Init(string exePath)
        {
            _exePath = exePath;
            _chartOptionPath = Path.Combine(_exePath, S57PathName.s57Dir, S57PathName.setupDir, "Option.ini");
            _chartCategoryPath = Path.Combine(_exePath, S57PathName.s57Dir, S57PathName.setupDir, "Category.ini");
            _chartTextGroupPath = Path.Combine(_exePath, S57PathName.s57Dir, S57PathName.setupDir, "TextGroup.ini");
            _chartSafetyPath = Path.Combine(_exePath, S57PathName.s57Dir, S57PathName.setupDir, "Safety.ini");
            _chartQueryOptionPath = Path.Combine(_exePath, S57PathName.s57Dir, S57PathName.setupDir, "QueryOption.ini");

            var dir = Path.Combine(_exePath, S57PathName.s57Dir, S57PathName.setupDir);
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

            LoadS57ChartOption();
            LoadS57ChartCategory();
            LoadS57ChartTextGroup();
            LoadS57ChartSafetyValue();
            LoadS57ChartQueryOption();
        }

        public static void LoadS57ChartOption()
        {
            S57ChartOption.Option = JsonConfig.Load<ChartOption>(_chartOptionPath);
        }

        public static void SaveS57ChartOption()
        {
            JsonConfig.Save(_chartOptionPath, S57ChartOption.Option);
        }

        public static void LoadS57ChartCategory()
        {
            S57ChartCategory.Category = JsonConfig.Load<ChartCategory>(_chartCategoryPath);
        }

        public static void SaveS57ChartCategory()
        {
            JsonConfig.Save(_chartCategoryPath, S57ChartCategory.Category);
        }

        public static void LoadS57ChartTextGroup()
        {
            S57TextGroup.Text = JsonConfig.Load<TextGroup>(_chartTextGroupPath);
        }

        public static void SaveS57ChartTextGroup()
        {
            JsonConfig.Save(_chartTextGroupPath, S57TextGroup.Text);
        }

        public static void LoadS57ChartSafetyValue()
        {
            S57ChartSafetyValue.Safetyvalue = JsonConfig.Load<ChartSafetyValue>(_chartSafetyPath);
        }

        public static void SaveS57ChartSafetyValue()
        {
            JsonConfig.Save(_chartSafetyPath, S57ChartSafetyValue.Safetyvalue);
        }

        public static void LoadS57ChartQueryOption()
        {
            S57ChartQueryOptions.Option = JsonConfig.Load<ChartQueryOption>(_chartQueryOptionPath);
        }

        public static void SaveS57ChartQueryOption()
        {
            JsonConfig.Save(_chartQueryOptionPath, S57ChartQueryOptions.Option);
        }
    }
}
