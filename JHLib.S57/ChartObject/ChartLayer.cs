namespace JHLib.S57.ChartObject
{
    public class ChartLayer
    {
        // Depare List
        public List<DEPARE> ListDepare = new();
        // Lndare List
        public List<LNDARE> ListLndare = new();
        // Drgare List
        public List<DRGARE> ListDrgare = new();
        // Unsare List
        public List<UNSARE> ListUnsare = new();
        // Depcnt List
        public List<DEPCNT> ListDepcnt = new();
        // Obstrn List
        public List<OBSTRN> ListObstrn = new();
        // Wrecks List
        public List<OBSTRN> ListWrecks = new();
        // Lights List
        public List<LIGHTS> ListLights = new();
        // Soundgs List
        public List<SOUNDG> ListSoundg = new();
        // Slcons List
        public List<SLCONS> ListSlcons = new();
        // Object List
        public List<OBJECT> ListObject = new();
        // Meta List
        public List<META> ListMeta = new();

        public byte GetGroupLayer(uint rcid, byte type)
        {
            byte rtnGroupLayer = 0;

            switch(type)
            {
                case 2:
                    var obstrn = ListObstrn.Where(p => p.Header.RCID == rcid).FirstOrDefault();
                    if(obstrn != null) rtnGroupLayer = obstrn.Header.ViewingGroup;
                    break;
                case 3:
                    var wrecks = ListWrecks.Where(p => p.Header.RCID == rcid).FirstOrDefault();
                    if (wrecks != null) rtnGroupLayer = wrecks.Header.ViewingGroup;
                    break;
            }

            return rtnGroupLayer;
        }


        public void Dispose()
        {
            foreach (var obj in ListDepare) obj.Dispose();
            ListDepare.Clear();
            foreach (var obj in ListLndare) obj.Dispose();
            ListLndare.Clear();
            foreach (var obj in ListDrgare) obj.Dispose();
            ListDrgare.Clear();
            foreach (var obj in ListUnsare) obj.Dispose();
            ListUnsare.Clear();
            foreach (var obj in ListDepcnt) obj.Dispose();
            ListDepcnt.Clear();
            foreach (var obj in ListObstrn) obj.Dispose();
            ListObstrn.Clear();
            foreach (var obj in ListWrecks) obj.Dispose();
            ListWrecks.Clear();
            foreach (var obj in ListLights) obj.Dispose();
            ListLights.Clear();
            foreach (var obj in ListSoundg) obj.Dispose();
            ListSoundg.Clear();
            foreach (var obj in ListSlcons) obj.Dispose();
            ListSlcons.Clear();
            foreach (var obj in ListObject) obj.Dispose();
            ListObject.Clear();
            foreach (var obj in ListMeta) obj.Dispose();
            ListMeta.Clear();
        }
    }
}
