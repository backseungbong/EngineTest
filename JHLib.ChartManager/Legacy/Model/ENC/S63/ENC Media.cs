namespace Legacy.ECM_Core.ENC
{
    public struct Media
    {
        public MediaHeader Header;
        public List<MediaRecord> Record;
    }



    public struct MediaHeader
    {
        public string Server_ID;               // Data Server ID                   :     2  Bytes   -   ex) GB or PR.....
        public string Week_Of_Issue;           // Week of issue                    :    10  Bytes   -   ex) WKNN_YY     WK27_07
        public string Date_Of_Issue;           // Date                             :     8  Bytes   -   ex) YYYYMMDD    20110930
        public string Media_Type;              // Media Type                       :    10  Bytes   -   ex) BASE or UPDATE
        public string MLI;                     // Media Label ID                   :     6  Bytes   -   ex) M[01-99]X[01-99]    M01X03
        public string Media_ID;                // Media ID                         :   2-3  Bytes   -   ex) M1,M2 ro M11.....
        public string MRMN;                    // Machine Readable Media Name      : 0-100  Bytes   -   ex) TEXT.... 'UKHO Week 27_07 BASE MEDIA 1'
        public string Region;                  // Regional Information [option]    : 0-100  Bytes   -   ex) TEXT.... 'Europe, Africa, and Middle East'
        public int Week;
    }

    public struct MediaRecord
    {
        public string Location;
        public string Folder;
        public string Date;
        public string Media_Number;
        public string Region;
        public string Reserved;
        public string Comment;
    }
}