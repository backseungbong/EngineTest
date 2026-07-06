namespace Legacy.ECM_Core.DCC
{
    public class FeatureLinker
    {
        public FRID FRID;

        public string Object_Acronym = "";
        public string Object_Name = "";

        public (int X, int Y) Pivot = (0, 0);
        public (int Index0, int Index1) PL = (-1, -1);

        public byte Display_Group = 0;
        public byte Display_Category = 0;
        public byte Group_Layer = 0;
        public byte Radar_Overlay = 0;

        public int Minimum_Scale = int.MaxValue;

        #region [[ Light Attribute ]]
        public float ORIENT = float.MaxValue;
        public float VALNMR = float.MaxValue;
        public float SECTR1 = float.MaxValue;
        public float SECTR2 = float.MaxValue;

        public bool CATLIT_8_11 = false;
        public bool CATLIT_9 = false;
        public bool CATLIT_1_16 = false;
        public bool LITVIS_3_7_8 = false;

        public byte COLOUR = 255;

        public bool Flare_At_45_Degrees = false;
        public bool All_Round_Light = false;
        public string LITDSN = "";
        public bool Extended_Arc_Radius = false;
        public bool Radius_26mm = false;
        #endregion

        #region [[ Danger Attribute ]]
        public float Danger_DEPTH = float.MaxValue;
        public float DRVAL1 = float.MaxValue;
        public float VALSOU = float.MaxValue;
        public bool Danger_Accuracy = false;
        public bool Danger_WATLEV_1_2 = false;
        public bool Sounding = false;
        public string Sounding_Symbol = "";
        #endregion

        public bool CS = false;
        public bool Edge_Masked = false;

        public List<ShapeLinker>? Shape;
        public List<Sound>? Sound;

        public List<DrawCommand>? Draw_Command;
        public List<EdgeMask>? Edge_Mask;
    }
}