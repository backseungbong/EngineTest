namespace JHLib.ChartManager.Record
{
    public class ChartRecord
    {
        public string name { get; set; }

        public int? usage { get; set; } = null;
        public int? scale { get; set; } = null;
        public int? COMF { get; set; } = null;

        public ChartRecord.BaseVersion? baseVersion { get; set; } = null;
        public int? updateVersion { get; set; } = null;
        public string? issueDate { get; set; } = null;
        public string? updateDate { get; set; } = null;
        public string? standardVersion { get; set; } = null;
        public int? agency { get; set; } = null;

        public ChartRecord.Position? centerPosition { get; set; } = null;

        public int? HDAT { get; set; } = null;
        public int? VDAT { get; set; } = null;
        public int? SDAT { get; set; } = null;
        public int? DUNI { get; set; } = null;
        public int? HUNI { get; set; } = null;
        public int? PUNI { get; set; } = null;

        public ChartRecord.Boundary? boundary { get; set; } = null;

        public string? referenceDate { get; set; } = null;
        public bool overlapped { get; set; } = false;
        public Lifecycle lifecycle { get; set; } = Lifecycle.Unknown;

        public bool IsChart1 { get; set; } = false;

        public ChartRecord(string name)
        {
            IsChart1 = name.Contains("CHART1_");

            // CHART1_을 제거하기 위해서
            var temp = name.Split("_");
            if (temp.Length >= 2) this.name = temp[1];
            else this.name = name;
        }

        public class BaseVersion
        {
            public int EDTN { get; set; }
            public int UPDN { get; set; }



            public BaseVersion(int EDTN, int UPDN)
            {
                this.EDTN = EDTN;
                this.UPDN = UPDN;
            }
        }

        public class Position
        {
            public float x { get; set; }
            public float y { get; set; }



            public Position(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public class Boundary
        {
            public float north { get; set; }
            public float south { get; set; }
            public float east { get; set; }
            public float west { get; set; }



            public Boundary(float north, float south, float east, float west)
            {
                this.north = north;
                this.south = south;
                this.east = east;
                this.west = west;
            }
        }

        public enum Lifecycle
        {
            Unknown = -1,
            UpToDate = 0,
            Outdated = 1,
            Canceled = 2,
        }
    }
}