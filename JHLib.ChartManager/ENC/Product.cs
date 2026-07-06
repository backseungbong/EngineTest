namespace JHLib.ChartManager.ENC
{
    public class Product
    {
        public string? date = null;
        public string? time = null;
        public int? version = null;
        public int? contentType = null;    // 0 = FULL, 1 = HALF
        public int? chartType = null;      // 0 = ENC,  1 = ECS

        public Dictionary<string, Product.Item> item = new Dictionary<string, Item>();



        public class Item
        {
            public string? name = null;
            public string? issueDate = null;
            public string? editionNumber = null;
            public string? updateDate = null;
            public string? updateNumber = null;
            public string? fileSize = null;

            public (double north, double south, double east, double west)? boundary = null;

            public int? compression = null;
            public int? encryption = null;
            public string? baseUpdateNumber = null;
            public string? preEditionUpdateNumber = null;
            public string? reserve = null;
            public string? comment = null;

            public string? SSE27 = null;
        }
    }
}