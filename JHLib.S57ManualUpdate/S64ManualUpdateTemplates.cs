using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JHLib.S57ManualUpdate
{
    public static class S64ManualUpdateTemplates
    {
        public static List<ManualUpdateTemplate> ListTemplates = new();

        public static void LoadManualUpdateTemplates(string exePath)
        {
            try
            {
                var filePath = Path.Combine(exePath, "S57", "ENC", "ManualUpdateTemplates.JSON");
                string jsonString = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    // JSON의 속성명 대소문자를 구분하지 않고 매핑 (권장)
                    PropertyNameCaseInsensitive = true,
                    // Enum 문자열 변환 지원
                    Converters = { new JsonStringEnumConverter() }
                };
                ListTemplates = JsonSerializer.Deserialize<List<ManualUpdateTemplate>>(jsonString, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading templates: {ex.Message}");
                return;
            }
        }
    }
}
