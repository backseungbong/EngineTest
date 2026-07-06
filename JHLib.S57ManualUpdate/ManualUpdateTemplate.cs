using System.Text.Json.Serialization;

namespace JHLib.S57ManualUpdate
{
    public enum EnumGeoType { Point, Line, Area, Official }

    public class ManualUpdateTemplate
    {
        public string ID { get; set; }  
        public string DisplayName { get; set; }  // 사용자에게 보여줄 이름 (예: "암초", "케이블")
        public string IconPath { get; set; }     // UI에 표시할 S-52 기반 아이콘 경로
        public string ObjectClass { get; set; } // S-57 OBJL 코드 (예: "WRECKS", "CBLSUB")

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EnumGeoType Primitive { get; set; } // Point, Line, Area 구분
        public Dictionary<string, ManualUpdateTemplate> SubTemplate { get; set; } // 추가 Object
    }
}
