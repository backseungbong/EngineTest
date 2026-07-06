namespace Legacy.ECM_Core.ENC
{
    public struct Product
    {
        public string Date;
        public string Time;
        public int Version;
        public int Content_Type;    // 0 = FULL, 1 = HALF
        public int Chart_Type;      // 0 = ENC,  1 = ECS

        public Dictionary<string, ProductRecord> Record;
    }



    public struct ProductRecord
    {
        public string Name;
        public string Issue_Date;
        public string Edition_Number;
        public string Update_Date;
        public string Update_Number;
        public string File_Size;

        public (double North, double South, double East, double West) Boundary;

        public int Compression;
        public int Encryption;
        public string Base_UpdateNumber;
        public string PreEdition_UpdateNumber;
        public string Reserve;
        public string Comment;

        public string SSE27;
    }
}