namespace Legacy.ECM_Core.Chart
{
    public struct SencLights
    {
        public uint RCID;

        public (int X, int Y) Pivot;

        public int Minimum_Scale;
        public byte Display_Group;
        public byte Radar_Overlay;
        public bool Information;

        public byte Update_Type;

        #region [[ Light Attribute ]]
        public float ORIENT;
        public float VALNMR;
        public float SECTR1;
        public float SECTR2;

        public bool CATLIT_8_11;
        public bool CATLIT_9;
        public bool CATLIT_1_16;
        public bool LITVIS_3_7_8;

        public byte COLOUR;

        public bool Flare_At_45_Degrees;
        public bool All_Round_Light;
        public string LITDSN;
        public bool Extended_Arc_Radius;
        public bool Radius_26mm;
        #endregion
    }
}