namespace Legacy.ECM_Core.Definition
{
    public static class DirectoryDefinition
    {
        public static string AppBase_Directory { get; internal set; } = "AppData";

        public static string ENC_Directory { get; internal set; } = "DATA\\ENC";

        public static string Chart_Directory { get; internal set; } = "S57";

        public static string SystemCatalogue_Directory { get; internal set; } = "System\\Catalogue";
        public static string SystemS63_Directory { get; internal set; } = "System\\S63";
    }
}