using System.Printing;

namespace JHLib.S57ManualUpdate.ManualUpdate
{
    public class MUmain
    {
        public MUmain() { }

        public MUmain(EnumGeoType geoType, string name, string objClass, string comment = "", int startDate = 0, int endDate = 0, bool isDelete = false, int id = 0)
        {
            ID = id;
            GeoType = geoType;
            DisplayName = name;
            ObjectClass = objClass;
            Comment = comment;
            StartDate = startDate;
            EndDate = endDate;
            IsDelete = isDelete;
        }

        public int ID { get; set; }
        public EnumGeoType GeoType { get; set; }
        public string DisplayName { get; set; }
        public string ObjectClass { get; set; }
        public string Comment { get; set; }
        public int StartDate { get; set; }
        public int EndDate { get; set; }
        public bool IsDelete { get; set; }
        public bool IsReview { get; set; }

        // 삭제된 Object는 3개월을 유지하고 Review시에 보여지도록 S-64에서 규정하고 있음
        // 3개월 이후에는 삭제한다.
        public int RemoveDate { get; set; }

        public MUpoint PointObj { get; set; } = new();
        public MUline LineObj { get; set; } = new();
        public MUarea AreaObj { get; set; } = new();

        public MUmain Clone()
        {
            return new MUmain
            {
                ID = this.ID,
                GeoType = this.GeoType,
                DisplayName = this.DisplayName,
                ObjectClass = this.ObjectClass,
                Comment = this.Comment,
                StartDate = this.StartDate,
                EndDate = this.EndDate,
                IsDelete = this.IsDelete,
                IsReview = this.IsReview,
                RemoveDate = this.RemoveDate,
                PointObj = this.PointObj?.Clone() ?? new MUpoint(),
                LineObj = this.LineObj?.Clone() ?? new MUline(),
                AreaObj = this.AreaObj?.Clone() ?? new MUarea()
            };
        }
    }
}
