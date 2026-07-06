namespace Legacy.ECM_Core.Chart
{
    public class SencChart
    {
        public List<SencDepare>? DEPARE { get; internal set; }
        public List<SencLndare>? LNDARE { get; internal set; }
        public List<SencDrgare>? DRGARE { get; internal set; }
        public List<SencUnsare>? UNSARE { get; internal set; }
        public List<SencDepcnt>? DEPCNT { get; internal set; }
        public List<SencObstrn>? OBSTRN_P { get; internal set; }
        public List<SencObstrn>? OBSTRN_L { get; internal set; }
        public List<SencObstrn>? OBSTRN_A { get; internal set; }
        public List<SencObstrn>? WRECKS_P { get; internal set; }
        public List<SencObstrn>? WRECKS_A { get; internal set; }
        public List<SencLights>? LIGHTS { get; internal set; }
        public List<SencSoundg>? SOUNDG { get; internal set; }
        public List<SencSlcons>? SLCONS_P { get; internal set; }
        public List<SencSlcons>? SLCONS_L { get; internal set; }
        public List<SencSlcons>? SLCONS_A { get; internal set; }
        public List<SencObject>? OBJECT_P { get; internal set; }
        public List<SencObject>? OBJECT_L { get; internal set; }
        public List<SencObject>? OBJECT_A { get; internal set; }

        public List<SencMeta>? Meta { get; internal set; }
        public List<SencText>? Text { get; internal set; }

        public List<SencSafety>? Safety { get; internal set; }
        public List<SencSafety>? Safety_Depth { get; internal set; }
        public List<SencSpecial>? Special { get; internal set; }
        public List<SencHazard>? Hazard { get; internal set; }
        public List<SencHazard>? Hazard_Depth { get; internal set; }
        public List<SencHazard>? Hazard_Sound { get; internal set; }

        public List<SencCover>? Cover { get; internal set; }

        public string Name { get; internal set; } = "";
        public bool Not_UpToDate { get; internal set; } = false;
    }
}