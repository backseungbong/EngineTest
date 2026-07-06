namespace JHLib.ChartManager.ENC
{
    public class Media
    {
        public string filePath;

        public Media.Header header = new Media.Header();
        public List<Media.Record> record = new List<Media.Record>();



        public Media(string filePath)
        {
            this.filePath = filePath;
        }



        public class Header
        {
            public string? dataServerID = null;          // Data Server ID                   :     2  Bytes   -   ex) GB or PR.....
            public string? weekOfIssue = null;           // Week of issue                    :    10  Bytes   -   ex) WKNN_YY     WK27_07
            public string? dateOfIssue = null;           // Date                             :     8  Bytes   -   ex) YYYYMMDD    20110930
            public string? mediaType = null;             // Media Type                       :    10  Bytes   -   ex) BASE or UPDATE
            public string? MLI = null;                   // Media Label ID                   :     6  Bytes   -   ex) M[01-99]X[01-99]    M01X03
            public string? mediaID = null;               // Media ID                         :   2-3  Bytes   -   ex) M1,M2 ro M11.....
            public string? MRMN = null;                  // Machine Readable Media Name      : 0-100  Bytes   -   ex) TEXT.... 'UKHO Week 27_07 BASE MEDIA 1'
            public string? region = null;                // Regional Information [option]    : 0-100  Bytes   -   ex) TEXT.... 'Europe, Africa, and Middle East'
            public int? week = null;
        }

        public class Record
        {
            public string? location = null;
            public string? folder = null;
            public string? date = null;
            public string? mediaNumber = null;
            public string? region = null;
            public string? reserved = null;
            public string? comment = null;
        }
    }
}