namespace JHLib.S57.Chart
{
    public class S57ChartOption
    {
        public static ChartOption Option;

        public static bool FourDepthShade => Option.FourDepthShade;
        public static bool ChartCatalogue => Option.ChartCatalogue;
        public static bool ShallowPattern => Option.ShallowPattern;
        public static bool ScaleMin => Option.ScaleMin;
        public static bool FullLightLine => Option.FullLightLine;
        public static bool LowAccuracy => Option.LowAccuracy;
        public static bool ContourLabel => Option.ContourLabel;
        public static bool NationalLanguage => Option.NationalLanguage;
        public static bool ShallowWaterDangers => Option.ShallowWaterDangers;
        public static bool HighlightInfo => Option.HighlightInfo;
        public static bool HighlightDocument => Option.HighlightDocument;
        public static bool HighlightDateDependent => Option.HighlightDateDependent;
        public static bool UnknownObject => Option.UnknownObject;
        public static bool UpdateReview => Option.UpdateReview;
        public static bool PaperSimple => Option.PaperSimple;            // false = Paper, true = Simple
        public static bool PlainSymbolized => Option.PlainSymbolized;  // false = Plain Boundary, true = Symbolized Boundary
        public static bool OverScalePattern => Option.OverScalePattern;

        public static byte DateDependentType => Option.DateDependentType;           // 0 = Current Date, 1 = Set Date
        public static int StartDate => Option.StartDate;
        public static int EndDate => Option.EndDate;

        public static bool UsedWorldMap => Option.UsedWorldMap;
        public static bool UsedChart1 => Option.UsedChart1;
        public static bool RadarOverlayOn => Option.RadarOverlayOn;
    }

    public class ChartOption
    {
        public bool FourDepthShade = false;
        public bool ChartCatalogue = false;
        public bool ShallowPattern = false;
        public bool ScaleMin = false;
        public bool FullLightLine = false;
        public bool LowAccuracy = false;
        public bool ContourLabel = false;
        public bool NationalLanguage = false;
        public bool ShallowWaterDangers = false;
        public bool HighlightInfo = false;
        public bool HighlightDocument = false;
        public bool HighlightDateDependent = false;
        public bool UnknownObject;
        public bool UpdateReview = false;
        public bool PaperSimple = false;                      // false = Paper, true = Simple
        public bool PlainSymbolized = true;                  // false = Plain Boundary, true = Symbolized Boundary
        public bool OverScalePattern = false;

        public byte DateDependentType = 0;           // 0 = Current Date, 1 = Set Date
        public int StartDate = 99999999;
        public int EndDate = 99999999;

        public bool UsedWorldMap = true;
        public bool UsedChart1 = false;
        public bool RadarOverlayOn = false;

        //public static bool ScalePattern;
        //public static bool ScaleBoundary;
        //public static bool GridLine;
    }

    public class S57ChartCategory
    {
        public static ChartCategory Category;

        // Standard
        public static bool DryingLine => Category.DryingLine;                   // 2
        public static bool AllBuoyBeacons => Category.AllBuoyBeacons;               // 3+4+5
        public static bool BuoysBeacons => Category.BuoysBeacons;             // 4
        public static bool Lights => Category.Lights;                           // 5
        public static bool BoundariesLimits => Category.BoundariesLimits;         // 6
        public static bool ProhitbitedRestricted => Category.ProhitbitedRestricted;        // 7
        public static bool ChartScaleBoundaries => Category.ChartScaleBoundaries;     // 8
        public static bool CautionaryNotes => Category.CautionaryNotes;          // 9
        public static bool TrafficRoute => Category.TrafficRoute;                 // 10
        public static bool ArchipelagicSeaLanes => Category.ArchipelagicSeaLanes;      // 11
        public static bool StandardMiscellaneous => Category.StandardMiscellaneous;    // 12

        // Other
        public static bool SpotSoundings => Category.SpotSoundings;                // 13
        public static bool CablesPipelines => Category.CablesPipelines;              // 14
        public static bool AllIsolatedDangers => Category.AllIsolatedDangers;           // 15
        public static bool MagnaticVariation => Category.MagnaticVariation;            // 16
        public static bool DepthContours => Category.DepthContours;                // 17
        public static bool Seabed => Category.Seabed;                       // 18
        public static bool Tidal => Category.Tidal;                            // 19
        public static bool OthersMiscellaneous => Category.OthersMiscellaneous;      // 20
    }

    public class ChartCategory
    {
        // Standard
        public bool DryingLine = true;                   // 2
        public bool AllBuoyBeacons = true;               // 3+4+5
        public bool BuoysBeacons = true;             // 4
        public bool Lights = true;                           // 5
        public bool BoundariesLimits = true;         // 6
        public bool ProhitbitedRestricted = true;        // 7
        public bool ChartScaleBoundaries = true;     // 8
        public bool CautionaryNotes = true;          // 9
        public bool TrafficRoute = true;                 // 10
        public bool ArchipelagicSeaLanes = true;      // 11
        public bool StandardMiscellaneous = true;    // 12

        // Other
        public bool SpotSoundings = false;                // 13
        public bool CablesPipelines = false;              // 14
        public bool AllIsolatedDangers = false;           // 15
        public bool MagnaticVariation = false;            // 16
        public bool DepthContours = false;                // 17
        public bool Seabed = false;                       // 18
        public bool Tidal = false;                            // 19
        public bool OthersMiscellaneous = false;      // 20
    }

    public class S57TextGroup
    {
        public static TextGroup Text;

        public static bool ImportantText => Text.ImportantText;
        public static bool OtherText => Text.OtherText;
        public static bool Names => Text.Names;
        public static bool LightDescription => Text.LightDescription;
        public static bool AllOther => Text.AllOther;
    }

    public class TextGroup
    {
        public bool ImportantText = false;
        public bool OtherText = false;
        public bool Names = false;
        public bool LightDescription = false;
        public bool AllOther = false;
    }

    public class S57ChartSafetyValue
    {
        public static ChartSafetyValue Safetyvalue;

        public static float ShallowContour => Safetyvalue.ShallowContour;
        public static float SafetyContour => Safetyvalue.SafetyContour;
        public static float DeepContour => Safetyvalue.DeepContour;
        public static float SafetyDepth => Safetyvalue.SafetyDepth;
    }

    public class ChartSafetyValue
    {
        public float ShallowContour = 2.0f;
        public float SafetyContour = 10.0f;
        public float DeepContour = 30.0f;
        public float SafetyDepth = 10.0f;
    }

    public class S57ChartQueryOptions
    {
        public static ChartQueryOption Option;

        public static bool QueryPointOn => Option.QueryPointOn;
        public static bool QueryLineOn => Option.QueryLineOn;
        public static bool QueryAreaOn => Option.QueryAreaOn;
    }

    public class ChartQueryOption
    {
        public bool QueryPointOn = true;
        public bool QueryLineOn = true;
        public bool QueryAreaOn = true;
    }

    public static class S63UserPermit
    {
        public static string UserPermit = "";
    }
}
