using JHLib.Util.Struct;
using System.Runtime.InteropServices;

namespace JHLib.S57.Chart
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_OBJ_HEADER
    {
        public byte Priority;
        public short OBJL;
        public uint Start;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_EDGE_INFO
    {
        public int Start;
        public int Count;
        public byte Mask;
        // true = Quality좋음, false = Quality않좋음
        public bool Quapos;
        // 각 Edge별로 점 배열이 Reverse되어 있는지 아닌지를 판별할 변수 추가함(2026.04.20)
        public bool Reverse;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_SHAPE_INFO
    {
        public int EdgeCount;
        public int PointCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_POINTS_HEADER
    {
        public int Pt;
        public int Edge;
        public int Shape;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_POINTS_VEC
    {
        public Int2D[] PathPoint;
        public ST_EDGE_INFO[] EdgeArr;
        public ST_SHAPE_INFO[] ShapeArr;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_POINTS
    {
        public ST_POINTS_HEADER PointsHeader;
        public ST_POINTS_VEC Shape;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_COM_INFO
    {
        public int SY;
        public int LS;
        public int LC;
        public int AC;
        public int AP;
        public int TX;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_COM_TOTAL_INFO
    {
        public int TotalSY;
        public int TotalLS;
        public int TotalLC;
        public int TotalAC;
        public int TotalAP;
        public int TotalTX;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_COM_SY
    {
        public short Index;
        public float Angle;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_COM_LS
    {
        public byte ColorIndex;
        public byte Style;
        public byte Width;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_COM_AC
    {
        public byte ColorIndex;
        public byte Trans;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_COM_TX
    {
        public Int2D Offset;
        public byte TextAlign;
        public byte TextGroup;
        public byte TextColorIndex;
        public string Text;
        public string NationalText;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_COM
    {
        public ST_COM_INFO[] ArrComInfo;
        public ST_COM_TOTAL_INFO TotalInfo;
        public ST_COM_SY[] ArrSY;
        public ST_COM_LS[] ArrLS;
        public byte[] ArrLC;
        public ST_COM_AC[] ArrAC;
        public byte[] ArrAP;
        public ST_COM_TX[] ArrTX;
        public int ComSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_EDGE_ATTR
    {
        public bool UNSAFE;
        public float VALDCO;
        public float DRVAL1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_EDGE_COM_CS
    {
        public float VALDCO;
        public ST_COM_LS ComLS;
        public int EdgeIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_EDGE_COM
    {
        public bool SY;
        public short ComSyIndex;
        public bool LS;
        public ST_COM_LS ComLS;
        public bool LC;
        public byte ComLcIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DEPARE_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public float DRVAL1;
        public float DRVAL2;
        public byte UpdateType;

        public ST_DEPARE_HEADER(byte radarOverlay, int rcid, float drval1, float drval2, byte updateType)
        {
            this.RadarOverlay = radarOverlay;
            this.RCID = rcid;
            this.DRVAL1 = drval1;
            this.DRVAL2 = drval2;
            this.UpdateType = updateType;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_CS
    {
        public byte LayerNum;            // CS가 처리된 Layer번호
        public int Index;                    // CS가 처리된 어레이 번호 
        public int RCID;                    // 검색을 위해서 추가함
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_LNDARE_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public byte PRIM;
        public Int2D Pivot;
        public int ScaleMin;
        public bool Highlight;
        public byte UpdateType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DRGARE_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public float DRVAL1;
        public bool Highlight;
        public byte UpdateType;

        public ST_DRGARE_HEADER(byte radarOverlay, int rcid, float drval1, bool heighlight, byte updateType)
        {
            this.RadarOverlay = radarOverlay;
            this.RCID = rcid;
            this.DRVAL1 = drval1;
            this.Highlight = heighlight;
            this.UpdateType = updateType;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_UNSARE_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public byte UpdateType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DEPCNT_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public int ScaleMin;
        public float VALDCO;
        public byte UpdateType;

        public ST_DEPCNT_HEADER(byte radarOverlay, int rcid, int scaleMin, float valdco, byte updateType)
        {
            this.RadarOverlay = radarOverlay;
            this.RCID = rcid;
            this.ScaleMin = scaleMin;
            this.VALDCO = valdco;
            this.UpdateType = updateType;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_OBSTRN_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public byte PRIM;
        public short OBJL;
        public Int2D Pivot;
        public int ScaleMin;
        public bool Highlight;
        public byte ViewingGroup;
        public byte UpdateType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DANGER_ATTR
    {
        public bool Accuracy;
        public bool WATLEV_1_2;
        public float DEPTH_VALUE;
        public float DRVAL1;
        public float VALSOU;
        public bool Sound;
        public byte Soundg1;
        public byte Soundg2;
        public byte Soundg3;
        public byte Soundg4;
        public byte Soundg5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_LIGHTS_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public Int2D Pivot;
        public int ScaleMin;
        public bool Highlight;
        // 1 = Insert, 2 = Delete, 3 = Modify
        public byte UpdateType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_LIGHTS_ATTR
    {
        public bool ALL_ROUND_LIGHT;
        public bool CATLIT_1_16;
        public bool CATLIT_8_11;
        public bool CATLIT_9;
        public bool LITVIS_3_7_8;
        public byte COLOUR_ATTR;
        public bool EXTENDED_ARC_RADIUS;
        public bool FLARE_AT_45_DEGREES;
        public float ORIENT;
        public float VALNMR;      //NM이므로 실거리 계산으로 사용할것 
        public float SECTR1;
        public float SECTR2;
        public bool Radius26;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_SOUNDG_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public int ScaleMin;
        // 1 = Insert, 2 = Delete, 3 = Modify
        public byte UpdateType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_SOUND
    {
        public int XCOO;
        public int YCOO;
        public float Soundg;
        public byte Sound1;
        public byte Sound2;
        public byte Sound3;
        public byte Sound4;
        public byte Sound5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_SLCONS_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public byte PRIM;
        public Int2D Pivot;
        public int ScaleMin;
        public bool Highlight;
        public byte UpdateType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_OBJECT_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public byte PRIM;
        public short OBJL;
        public Int2D Pivot;
        public int StartDate;
        public int EndDate;
        public bool Null;
        public byte UpdateType;
        public byte GroupLayer;
        public int ScaleMin;
        public byte Highlight;
        public bool Reverse;
    }

    // LinkEdgeMask시에 Priority가 CS에 의해 변경되는 Object에 대해서 Edge정보를 가지고 있을 구조체
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_EDGE_MASK
    {
        public uint LinkRCID;
        public byte Type;        // 0 = DEPARE, 1 = DRGARE, 2 = OBSTRN, 3 = WRECKS
        public int EdgeNum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_META_HEADER
    {
        public byte RadarOverlay;
        public int RCID;
        public byte PRIM;
        public short OBJL;
        public byte ViewingGroup;
        public bool LowAccuracy;
        public byte Highlight;
        public int Cscale;
        public byte UpdateType;
    }

    public struct ST_OVER_TEXT
    {
        public Float2D Pivot;
        public ST_COM_TX ComTX;
    }

    public struct ST_OVER_INFORM
    {
        // 0 = Highlight Inform, 1 = Highlight Document, 2 = Highlight Date Dependent, 3 = Manual Update Official Delete 
        public byte Type;
        public byte UpdateType;
        public Float2D Pivot;
        public bool ManualUpdateReview;
    }

    // Update 정보를 저장할 구조체 
    public struct ST_UPDATE
    {
        public int RCID;
        public byte PRIM;
        // 1 = Insert, 2 = Delete, 3 = Modify
        public byte RUIN;

        public Int2D[] PathPT;
    }

    public struct ST_OVER
    {
        public List<ST_OVER_TEXT> ListText;
        public List<ST_OVER_INFORM> ListInform;
        //public ST_UPDATE[] ListUpdate;
        public int Agency;
    }

    public struct ST_OVERLAP
    {
        public int RCID;
        public Float2D Pivot;
        public bool Overlap;
    };

    //================================================================================================================
    // Detection 구조체
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DETECT_SIZE
    {
        public byte Type;
        public uint Start;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DETECT_POINTS
    {
        public Float2D[] PathPT;
        public Float2D[][] PathsShape;
        public int[] ArrShape;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DETECT_SAFETY
    {
        public int RCID;
        public short OBJL;
        public byte PRIM;
        public float DRVAL1;
        public ST_DETECT_POINTS Points;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DETECT_SPECIAL
    {
        public int RCID;
        public short OBJL;
        public byte PRIM;
        public byte RESARE;
        public ST_DETECT_POINTS Points;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DETECT_HAZARD_SOUND
    {
        public float Sound;
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ST_DETECT_HAZARD
    {
        public int RCID;
        public short OBJL;
        public byte PRIM;
        public float DEPTH_VALUE;
        public ST_DETECT_HAZARD_SOUND[] ArrSoundg;
        public ST_DETECT_POINTS Points;
    }
    //================================================================================================================
    // 안전수심값 저장 구조체
    public struct ST_SAFETY_CONTOUR
    {
        public float ShallowContour;
        public float SafetyContour;
        public float DeepContour;
        public float SafetyDepth;
    }

    // PL4.0.1에 맞도록 옵션을 변경함
    public struct ST_CHART_OPTION
    {
        public bool FourDepthShade;
        public bool ChartCatalogue;
        public bool ShallowPattern;
        public bool ScaleMin;
        //public bool bScalePattern;
        //public bool bScaleBoundary;
        public bool FullLightLine;
        //public bool bGridLine;
        public bool LowAccuracy;
        public bool ContourLabel;
        public bool NationalLanguage;
        public bool ShallowWaterDangers;
        public bool HighlightInfo;
        public bool HighlightDocument;
        public bool HighlightDateDependent;
        public bool UnknownObject;
        public bool UpdateReview;
        public bool PaperSimple;                      // false = Paper, true = Simple
        public bool PlainSymbolized;                  // false = Plain Boundary, true = Symbolized Boundary
        public bool OverScalePattern;

        public byte DateDependentType;           // 0 = Current Date, 1 = Set Date
        public int StartDate;
        public int EndDate;
    }

    // Text Group 구조체
    // PL4.0.1에서 Text Groupings가 바뀌었다.
    public struct ST_TEXT_GROUP
    {
        public bool ImportantText;
        public bool OtherText;
        public bool Names;
        public bool LightDescription;
        public bool AllOther;
    }

    // PL4.0.1에 맞도록 Category정리
    public struct ST_CHART_CATEGORY
    {
        // Standard
        public bool DryingLine;                   // 2
        public bool AllBuoyBeacons;               // 3+4+5
        public bool BuoysBeacons;             // 4
        public bool Lights;                           // 5
        public bool BoundariesLimits;         // 6
        public bool ProhitbitedRestricted;        // 7
        public bool ChartScaleBoundaries;     // 8
        public bool CautionaryNotes;          // 9
        public bool TrafficRoute;                 // 10
        public bool ArchipelgicSeaLanes;      // 11
        public bool StandardMiscellaneous;    // 12

        // Other
        public bool SpotSoundings;                // 13
        public bool CablesPipelines;              // 14
        public bool AllIsolatedDangers;           // 15
        public bool MagnaticVariation;            // 16
        public bool DepthContours;                // 17
        public bool Seabed;                       // 18
        public bool Tidal;                            // 19
        public bool OthersMiscellaneous;      // 20
    }
}

