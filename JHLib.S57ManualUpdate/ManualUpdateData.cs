using JHLib.Util.Struct;

namespace JHLib.S57ManualUpdate
{
    public enum EnumStatus { Insert, Delete }

    public class ManualUpdateData
    {
        public EnumGeoType Type = EnumGeoType.Point;
        public string ObjectName = "";
        public EnumStatus Status = EnumStatus.Insert;
        public string Comment = "";
        public int StartDate = 0;
        public int EndDate = 0;
        public List<string> SymbolNames = new();
        public List<Float2D> Points = new();
    }
}
